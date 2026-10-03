#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public sealed class DuelActionResult
{
    public bool Success { get; }
    public string? ErrorReason { get; }

    public DuelActionResult(bool success, string? errorReason = null)
    {
        Success = success;
        ErrorReason = errorReason;
    }

    public static DuelActionResult Ok() => new DuelActionResult(true);
    public static DuelActionResult Refused(string reason) => new DuelActionResult(false, reason);
}

public sealed class DuelEngine
{
    private readonly IRandom _rng;
    public DuelRules Rules { get; }
    public DuelState State { get; }
    public EffectRegistry Effects { get; }

    public event Action<IDuelEvent>? OnEvent;

    public DuelEngine(IRandom rng, DuelRules? rules = null, EffectRegistry? effects = null)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        Rules = rules ?? new DuelRules();
        State = new DuelState(Rules);
        Effects = effects ?? new EffectRegistry();
    }

    private void Emit(IDuelEvent evt) => OnEvent?.Invoke(evt);

    /// <summary>
    /// Initialise le duel avec les decks des deux joueurs, mélange les pioches,
    /// pioche la main de départ et détermine le premier joueur.
    /// </summary>
    public DuelActionResult StartDuel(
        IEnumerable<DuelCard> deckP1,
        IEnumerable<DuelCard> deckP2,
        PlayerId? fixedStartingPlayer = null)
    {
        if (State.TurnNumber > 0 || State.IsGameOver)
            return DuelActionResult.Refused("Duel is already started or finished.");

        if (deckP1 == null || deckP2 == null)
            return DuelActionResult.Refused("Decks cannot be null.");

        State.AddDeckCards(PlayerId.Player1, deckP1);
        State.AddDeckCards(PlayerId.Player2, deckP2);

        State.ShuffleDeck(PlayerId.Player1, _rng);
        State.ShuffleDeck(PlayerId.Player2, _rng);

        // Premier joueur : au hasard (50/50) ou fixé
        PlayerId startingPlayer = fixedStartingPlayer ?? (_rng.Next(2) == 0 ? PlayerId.Player1 : PlayerId.Player2);
        State.SetActivePlayer(startingPlayer);

        Emit(new DuelStartedEvent(startingPlayer));

        // Pioche de la main de départ pour chaque joueur
        if (!DrawInitialHand(PlayerId.Player1))
            return DuelActionResult.Ok(); // Partie terminée par meule dès le départ

        if (!DrawInitialHand(PlayerId.Player2))
            return DuelActionResult.Ok();

        return DuelActionResult.Ok();
    }

    private bool DrawInitialHand(PlayerId player)
    {
        var drawn = DrawCards(player, Rules.StartingHandSize);
        return !State.IsGameOver;
    }

    /// <summary>
    /// Démarre le tour : TurnNumber global incrémenté, réinitialise le compteur de personnages posés,
    /// pioche, redresse les terrains du tour précédent, et vérifie la présence de terrains en jeu.
    /// </summary>
    public DuelActionResult StartTurn()
    {
        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        State.IncrementTurnNumber();
        State.ResetCharactersPlayed();
        Emit(new TurnStartedEvent(State.ActivePlayer, State.TurnNumber));

        // 1) Pioche
        bool shouldDraw = State.TurnNumber > 1 || Rules.FirstPlayerDrawsOnTurn1;
        if (shouldDraw)
        {
            DrawCards(State.ActivePlayer, Rules.CardsDrawnPerTurn);
            if (State.IsGameOver) return DuelActionResult.Ok();
        }

        // 2) Redressage des terrains placés au tour précédent (PlacedOnTurn == TurnNumber - 1)
        var newlyReadiedIndices = new List<int>();
        for (int i = 0; i < State.Slots.Count; i++)
        {
            var slot = State.Slots[i];
            if (!slot.IsEmpty && slot.PlacedOnTurn.HasValue && slot.PlacedOnTurn.Value == State.TurnNumber - 1)
            {
                newlyReadiedIndices.Add(i);
            }
        }
        if (newlyReadiedIndices.Count > 0)
        {
            Emit(new TerrainsReadiedEvent(State.TurnNumber, newlyReadiedIndices));
        }

        // 3) Vérification de la présence de terrains en jeu
        if (State.TerrainsInPlayCount <= Rules.MinTerrainsThreshold)
        {
            CheckTerrainRequirement();
        }

        return DuelActionResult.Ok();
    }

    private void CheckTerrainRequirement()
    {
        var hand = State.GetHand(State.ActivePlayer);
        bool hasTerrainInHand = false;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Terrain)
            {
                hasTerrainInHand = true;
                break;
            }
        }

        if (hasTerrainInHand)
        {
            State.SetTerrainRequirementPending(true);
            Emit(new TerrainPlacementRequiredEvent(State.ActivePlayer));
        }
        else
        {
            // Aucun terrain en main -> Dépilage automatique de la pioche
            ExecuteTerrainDig(State.ActivePlayer);
        }
    }

    private void ExecuteTerrainDig(PlayerId player)
    {
        var revealedCards = new List<DuelCard>();
        DuelCard? foundTerrain = null;

        while (true)
        {
            var card = State.PopTopDeckCard(player);
            if (card == null) break; // Deck épuisé

            if (card.Kind == CardKind.Terrain)
            {
                foundTerrain = card;
                break;
            }
            revealedCards.Add(card);
        }

        if (foundTerrain != null)
        {
            // Remettre les cartes révélées non-terrain sous le deck dans l'ordre de révélation
            State.AddCardsToBottomDeck(player, revealedCards);

            // Trouver le premier slot libre
            int freeSlotIndex = -1;
            for (int i = 0; i < State.Slots.Count; i++)
            {
                if (State.Slots[i].IsEmpty)
                {
                    freeSlotIndex = i;
                    break;
                }
            }

            if (freeSlotIndex != -1)
            {
                State.SetSlot(freeSlotIndex, foundTerrain, State.TurnNumber, player);
                State.SetTerrainRequirementPending(false);
                Emit(new TerrainRevealedByDigEvent(player, freeSlotIndex, foundTerrain, revealedCards));
            }
        }
        else
        {
            // Aucun terrain trouvé dans tout le deck : reconstitution exacte sous le deck, pas de meule
            State.AddCardsToBottomDeck(player, revealedCards);
            State.SetTerrainRequirementPending(false);
            Emit(new TerrainDigFailedEvent(player));
        }
    }

    /// <summary>
    /// Pose manuelle d'un terrain depuis la main sur un slot du plateau.
    /// </summary>
    public DuelActionResult PlayTerrain(PlayerId player, string cardId, int slotIndex)
    {
        if (State.TurnNumber == 0)
            return DuelActionResult.Refused("Game has not started yet.");

        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (State.ActivePlayer != player)
            return DuelActionResult.Refused("Not this player's turn.");

        if (slotIndex < 0 || slotIndex >= State.Slots.Count)
            return DuelActionResult.Refused($"Invalid slot index: {slotIndex}.");

        var slot = State.Slots[slotIndex];
        if (!slot.IsEmpty)
            return DuelActionResult.Refused($"Slot {slotIndex} is already occupied by {slot.Card!.Name}.");

        if (State.TerrainsInPlayCount >= Rules.MaxTerrainsOnBoard)
            return DuelActionResult.Refused("Maximum terrains already in play.");

        var hand = State.GetHand(player);
        DuelCard? cardToPlay = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Id == cardId)
            {
                cardToPlay = hand[i];
                break;
            }
        }

        if (cardToPlay == null)
            return DuelActionResult.Refused($"Card {cardId} not found in player's hand.");

        if (cardToPlay.Kind != CardKind.Terrain)
            return DuelActionResult.Refused($"Card {cardId} is not a Terrain.");

        State.RemoveCardFromHand(player, cardToPlay);
        State.SetSlot(slotIndex, cardToPlay, State.TurnNumber, player);
        State.SetTerrainRequirementPending(false);

        Emit(new TerrainPlayedEvent(player, slotIndex, cardToPlay));
        return DuelActionResult.Ok();
    }

    /// <summary>
    /// Pose un personnage depuis la main sur un terrain existant (max 4 personnages par tour).
    /// Si la carte est un Scoreur et que le terrain est actif, déclenche immédiatement la résolution du duel.
    /// </summary>
    public DuelActionResult PlayCharacter(PlayerId player, string cardId, int slotIndex)
    {
        if (State.TurnNumber == 0)
            return DuelActionResult.Refused("Game has not started yet.");

        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (State.ActivePlayer != player)
            return DuelActionResult.Refused("Not this player's turn.");

        if (State.TerrainRequirementPending)
            return DuelActionResult.Refused("Must place a terrain before playing characters.");

        if (State.CharactersPlayedThisTurn >= Rules.MaxCharactersPerTurn)
            return DuelActionResult.Refused($"Cannot play more than {Rules.MaxCharactersPerTurn} characters per turn.");

        if (slotIndex < 0 || slotIndex >= State.Slots.Count)
            return DuelActionResult.Refused($"Invalid slot index: {slotIndex}.");

        var slot = State.Slots[slotIndex];
        if (slot.IsEmpty)
            return DuelActionResult.Refused($"Cannot play a character on an empty slot ({slotIndex}). A Terrain is required.");

        var hand = State.GetHand(player);
        DuelCard? cardToPlay = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Id == cardId)
            {
                cardToPlay = hand[i];
                break;
            }
        }

        if (cardToPlay == null)
            return DuelActionResult.Refused($"Card {cardId} not found in player's hand.");

        if (cardToPlay.Kind != CardKind.Character)
            return DuelActionResult.Refused($"Card {cardId} is not a Character.");

        State.RemoveCardFromHand(player, cardToPlay);
        State.AddCharacterToSlot(slotIndex, player, cardToPlay);
        State.IncrementCharactersPlayed();

        Emit(new CharacterPlayedEvent(player, slotIndex, cardToPlay));

        // Exécution des effets de la carte (IEffect)
        if (cardToPlay.EffectIds != null && cardToPlay.EffectIds.Count > 0)
        {
            var ctx = new EffectContext(this, player, cardToPlay, slotIndex);
            for (int i = 0; i < cardToPlay.EffectIds.Count; i++)
            {
                string effectId = cardToPlay.EffectIds[i];
                if (Effects.TryGetEffect(effectId, out var effect) && effect != null)
                {
                    Emit(new EffectTriggeredEvent(player, effectId, cardToPlay, slotIndex));
                    effect.Execute(ctx);
                    if (State.IsGameOver) break;
                }
            }
        }

        // Déclenchement automatique du scoring si Scoreur posé sur un terrain actif
        if (!State.IsGameOver && cardToPlay.IsScoreur && slot.IsActive(State.TurnNumber))
        {
            ResolveScore(slotIndex);
        }

        return DuelActionResult.Ok();
    }

    /// <summary>
    /// Résout le combat sur un terrain : compare la force totale de chaque camp,
    /// attribue 1 point de victoire au gagnant (0 si égalité), envoie toutes les cartes
    /// engagées dans les défausses respectives, et vide le slot.
    /// </summary>
    public void ResolveScore(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= State.Slots.Count) return;
        var slot = State.Slots[slotIndex];
        if (slot.IsEmpty) return;

        int forceP1 = slot.GetForce(PlayerId.Player1);
        int forceP2 = slot.GetForce(PlayerId.Player2);

        PlayerId? winner = null;
        if (forceP1 > forceP2)
        {
            winner = PlayerId.Player1;
            State.AddScore(PlayerId.Player1, 1);
        }
        else if (forceP2 > forceP1)
        {
            winner = PlayerId.Player2;
            State.AddScore(PlayerId.Player2, 1);
        }

        Emit(new ScoreResolvedEvent(slotIndex, forceP1, forceP2, winner));

        // Nettoyage : tous les personnages et le terrain vont en défausse
        var charsP1 = new List<DuelCard>(slot.CharactersP1);
        var charsP2 = new List<DuelCard>(slot.CharactersP2);
        var terrainCard = slot.Card!;
        var terrainPlacedBy = slot.PlacedBy ?? State.ActivePlayer;

        State.AddCardsToDiscard(PlayerId.Player1, charsP1);
        State.AddCardsToDiscard(PlayerId.Player2, charsP2);
        State.AddCardToDiscard(terrainPlacedBy, terrainCard);

        State.ClearSlot(slotIndex);
        Emit(new TerrainClearedEvent(slotIndex, terrainCard, charsP1, charsP2));

        // Vérification de victoire à X terrains remportés
        if (winner.HasValue && State.GetScore(winner.Value) >= Rules.TerrainsToWin)
        {
            EndGame(winner.Value, GameOverReason.FiveTerrains);
        }
    }

    /// <summary>
    /// Tente de piocher count cartes.
    /// En cas d'épuisement (deck < count), toutes les cartes encore disponibles sont piochées en main,
    /// puis l'épuisement déclenche DeckExhaustedEvent et la défaite immédiate par DeckOut.
    /// </summary>
    public IReadOnlyList<DuelCard> DrawCards(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        var drawn = new List<DuelCard>();
        int needed = count;

        while (needed > 0)
        {
            var card = State.PopTopDeckCard(player);
            if (card == null) break;
            State.AddCardToHand(player, card);
            drawn.Add(card);
            needed--;
        }

        if (drawn.Count > 0)
        {
            Emit(new CardsDrawnEvent(player, drawn));
        }

        // Si le joueur devait piocher plus que ce qui restait dans le deck
        if (needed > 0)
        {
            Emit(new DeckExhaustedEvent(player));
            EndGame(player.Opponent(), GameOverReason.DeckOut);
        }

        return drawn;
    }

    /// <summary>
    /// Meule count cartes depuis le dessus du deck du joueur vers sa défausse.
    /// Si le deck contient moins de cartes que demandé, toutes les cartes restantes vont en défausse
    /// puis défaite immédiate par DeckOut (avec Opponent déclaré vainqueur).
    /// </summary>
    public IReadOnlyList<DuelCard> MillCards(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        var milled = new List<DuelCard>();
        int needed = count;

        while (needed > 0)
        {
            var card = State.PopTopDeckCard(player);
            if (card == null) break;
            State.AddCardToDiscard(player, card);
            milled.Add(card);
            needed--;
        }

        if (milled.Count > 0)
        {
            Emit(new CardsMilledEvent(player, milled));
        }

        if (needed > 0)
        {
            Emit(new DeckExhaustedEvent(player));
            EndGame(player.Opponent(), GameOverReason.DeckOut);
        }

        return milled;
    }

    /// <summary>
    /// Défausse count cartes de la main d'un joueur vers sa défausse (du début de la main).
    /// </summary>
    public IReadOnlyList<DuelCard> DiscardFromHand(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        var discarded = new List<DuelCard>();
        var hand = State.GetHand(player);

        int toDiscard = Math.Min(count, hand.Count);
        for (int i = 0; i < toDiscard; i++)
        {
            var card = hand[0];
            State.RemoveCardFromHand(player, card);
            State.AddCardToDiscard(player, card);
            discarded.Add(card);
        }

        if (discarded.Count > 0)
        {
            Emit(new CardsDiscardedEvent(player, discarded));
        }

        return discarded;
    }

    public DuelActionResult EndTurn(PlayerId player)
    {
        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (State.ActivePlayer != player)
            return DuelActionResult.Refused("Not this player's turn.");

        if (State.TerrainRequirementPending)
            return DuelActionResult.Refused("Must place a terrain before ending turn.");

        Emit(new TurnEndedEvent(player, State.TurnNumber));
        State.SetActivePlayer(player.Opponent());
        return DuelActionResult.Ok();
    }

    private void EndGame(PlayerId winner, GameOverReason reason)
    {
        State.SetGameOver(winner, reason);
        Emit(new DuelEndedEvent(winner, reason));
    }
}
