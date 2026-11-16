#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

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

    public PlayerId? FreeCharactersTurnPlayer { get; set; }
    public PlayerId? DoubleNextWinEffectPlayer { get; set; }
    public PlayerId? DoubleNextLoseEffectPlayer { get; set; }
    public bool FirstCharacterPlayedThisTurn { get; set; }
    public bool RequireInteractiveCostPayment { get; set; } = false;

    public event Action<IDuelEvent>? OnEvent;

    public DuelEngine(IRandom rng, DuelRules? rules = null, EffectRegistry? effects = null)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        Rules = rules ?? new DuelRules();
        State = new DuelState(Rules);
        Effects = effects ?? new EffectRegistry();
        if (effects == null)
        {
            RegisterExtensionEffects(Effects);
        }
    }

    private static void RegisterExtensionEffects(EffectRegistry registry)
    {
        string[] typeNames =
        {
            "WankulCrazy.Duel.Engine.EffectsS1S2",
            "WankulCrazy.Duel.Engine.EffectsS3S5"
        };

        var asm = typeof(DuelEngine).Assembly;
        for (int i = 0; i < typeNames.Length; i++)
        {
            try
            {
                var type = asm.GetType(typeNames[i]) ?? Type.GetType(typeNames[i]);
                var method = type?.GetMethod("RegisterAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                method?.Invoke(null, new object[] { registry });
            }
            catch
            {
                // Extension optionnelle non disponible ou non chargée
            }
        }
    }

    public void AwardExtraPoint(PlayerId player, int points = 1)
    {
        if (points <= 0 || State.IsGameOver) return;
        State.AddScore(player, points);
        if (State.GetScore(player) >= Rules.TerrainsToWin)
        {
            EndGame(player, GameOverReason.FiveTerrains);
        }
    }

    private void ResetTurnFlags()
    {
        FreeCharactersTurnPlayer = null;
        DoubleNextWinEffectPlayer = null;
        DoubleNextLoseEffectPlayer = null;
        FirstCharacterPlayedThisTurn = false;
        PassiveCardHelper.GetState(State).ResetTurn();
    }

    public DuelActionResult ActivateCardPassive(PlayerId player, string cardId, int slotIndex = -1) =>
        PassiveLifecycle.ActivateCardPassive(this, player, cardId, slotIndex);

    public bool CanDoubleCombo(PlayerId player) =>
        PassiveLifecycle.CanDoubleCombo(this, player);

    private static bool ListContainsEffect(IReadOnlyList<string>? list, string target) =>
        PassiveCardHelper.ListContains(list, target);

    private static bool CardHasEffect(DuelCard? card, string effectId) =>
        PassiveCardHelper.RawCardHasEffect(card, effectId);

    public int GetEffectivePlayCost(PlayerId player, DuelCard card, int slotIndex) =>
        PassiveLifecycle.ComputeEffectivePlayCost(this, player, card, slotIndex);

    internal void Emit(IDuelEvent evt) => OnEvent?.Invoke(evt);

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
        ResetTurnFlags();
        Emit(new TurnStartedEvent(State.ActivePlayer, State.TurnNumber));

        // 1) Pioche
        bool shouldDraw = State.TurnNumber > 1 || Rules.FirstPlayerDrawsOnTurn1;
        if (shouldDraw)
        {
            DrawCards(State.ActivePlayer, Rules.CardsDrawnPerTurn);
            if (State.IsGameOver) return DuelActionResult.Ok();

            // 1b) Bonus de pioche début de tour accordé par des Terrains actifs (Rust, Golf)
            int extraDraw = 0;
            for (int i = 0; i < State.Slots.Count; i++)
            {
                var s = State.Slots[i];
                if (!s.IsEmpty && s.IsActive(State.TurnNumber) && s.Card != null)
                {
                    if (CardHasEffect(s.Card, "terrain_draw_1_turn_start"))
                    {
                        extraDraw++;
                        Emit(new EffectTriggeredEvent(State.ActivePlayer, "terrain_draw_1_turn_start", s.Card, i));
                    }
                }
            }
            if (extraDraw > 0)
            {
                DrawCards(State.ActivePlayer, extraDraw);
                if (State.IsGameOver) return DuelActionResult.Ok();
            }
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

        // 2b) Passifs de début de tour (Infecté, Téléachat, DUO)
        ExecuteStartTurnPassives();
        if (State.IsGameOver) return DuelActionResult.Ok();

        // 3) Vérification de la présence de terrains en jeu
        if (State.TerrainsInPlayCount <= Rules.MinTerrainsThreshold)
        {
            CheckTerrainRequirement();
        }

        CheckDeckOutCondition(State.ActivePlayer);

        return DuelActionResult.Ok();
    }

    private void ExecuteStartTurnPassives()
    {
        var active = State.ActivePlayer;
        var opp = active.Opponent();

        int activeCharCount = 0;
        for (int s = 0; s < State.Slots.Count; s++)
        {
            activeCharCount += State.Slots[s].GetCharacters(active).Count;
        }

        for (int s = 0; s < State.Slots.Count; s++)
        {
            if (State.IsGameOver) return;
            var slot = State.Slots[s];
            var activeChars = new List<DuelCard>(slot.GetCharacters(active));
            for (int i = 0; i < activeChars.Count; i++)
            {
                var c = activeChars[i];
                if (CardHasEffect(c, "passive_start_turn_infecte"))
                {
                    var discard = State.GetDiscard(active);
                    var deck = State.GetDeck(active);
                    if (discard.Count > deck.Count && discard.Count > 0)
                    {
                        var top = discard[discard.Count - 1];
                        State.RemoveCardFromDiscard(active, top);
                        State.AddCardToHand(active, top);
                        Emit(new EffectTriggeredEvent(active, "passive_start_turn_infecte", c, s));
                    }
                }

                if (CardHasEffect(c, "passive_start_turn_mill_per_char") && activeCharCount > 0)
                {
                    Emit(new EffectTriggeredEvent(active, "passive_start_turn_mill_per_char", c, s));
                    MillCards(active, activeCharCount);
                    if (State.IsGameOver) return;
                }

                TriggerDuoStartTurnEffects(active, c, s, isActiveTurn: true);
                if (State.IsGameOver) return;
            }

            var oppChars = new List<DuelCard>(slot.GetCharacters(opp));
            for (int i = 0; i < oppChars.Count; i++)
            {
                TriggerDuoStartTurnEffects(opp, oppChars[i], s, isActiveTurn: false);
                if (State.IsGameOver) return;
            }
        }

        if (!State.IsGameOver)
        {
            PassiveLifecycle.ExecuteStartTurnPassives(this, _rng);
        }
    }

    private void TriggerDuoStartTurnEffects(PlayerId owner, DuelCard card, int slotIndex, bool isActiveTurn)
    {
        void CheckList(IReadOnlyList<string>? ids)
        {
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (!id.StartsWith("duo_", StringComparison.Ordinal)) continue;

                if (!isActiveTurn && string.Equals(id, "duo_martine_jeanine", StringComparison.Ordinal))
                {
                    if (Effects.TryGetEffect("duo_martine_jeanine_opp_play_free", out var martineEff) && martineEff != null)
                    {
                        Emit(new EffectTriggeredEvent(owner, "duo_martine_jeanine_opp_play_free", card, slotIndex));
                        martineEff.Execute(new EffectContext(this, owner, card, slotIndex));
                    }
                }

                if (Effects.TryGetEffect(id + "_turn_start", out var startEff) && startEff != null)
                {
                    Emit(new EffectTriggeredEvent(owner, id + "_turn_start", card, slotIndex));
                    startEff.Execute(new EffectContext(this, owner, card, slotIndex));
                }
            }
        }

        CheckList(card.PassiveEffectIds);
        CheckList(card.EffectIds);
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
                PassiveLifecycle.OnTerrainEnteredPlay(this, player, freeSlotIndex, foundTerrain, wasPlayedFromHand: false);
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

        var terrainRestriction = PassiveLifecycle.ValidatePlayTerrain(this, player);
        if (terrainRestriction != null)
            return terrainRestriction;

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
        PassiveLifecycle.OnTerrainEnteredPlay(this, player, slotIndex, cardToPlay, wasPlayedFromHand: true);

        // Exécution des effets OnPlay du terrain posé
        if (cardToPlay.EffectIds != null && cardToPlay.EffectIds.Count > 0)
        {
            var ctx = new EffectContext(this, player, cardToPlay, slotIndex);
            for (int i = 0; i < cardToPlay.EffectIds.Count; i++)
            {
                string effectId = cardToPlay.EffectIds[i];
                // Les effets win_ et lose_ sont résolus uniquement lors du score.
                if (effectId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) ||
                    effectId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Effects.TryGetEffect(effectId, out var effect) && effect != null)
                {
                    Emit(new EffectTriggeredEvent(player, effectId, cardToPlay, slotIndex));
                    effect.Execute(ctx);
                    if (State.IsGameOver) break;
                }
            }
        }

        CheckDeckOutCondition(player);
        return DuelActionResult.Ok();
    }

    /// <summary>
    /// Pose un personnage depuis la main sur un terrain existant (max 4 personnages par tour).
    /// Si la carte est un Scoreur et que le terrain est actif, déclenche immédiatement la résolution du duel.
    /// </summary>

    public DuelActionResult MoveCharacter(PlayerId player, string cardId, int fromSlotIndex, int toSlotIndex, int insertIndex = -1)
    {
        if (State.TurnNumber == 0)
            return DuelActionResult.Refused("Game has not started yet.");

        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (fromSlotIndex < 0 || fromSlotIndex >= State.Slots.Count)
            return DuelActionResult.Refused("Invalid fromSlotIndex.");

        if (toSlotIndex < 0 || toSlotIndex >= State.Slots.Count)
            return DuelActionResult.Refused("Invalid toSlotIndex.");

        if (fromSlotIndex == toSlotIndex)
            return DuelActionResult.Refused("Cannot move to the same slot.");

        var fromSlot = State.Slots[fromSlotIndex];
        var chars = fromSlot.GetCharacters(player);
        var targetCard = chars.FirstOrDefault(c => c.Id == cardId);

        if (targetCard == null)
            return DuelActionResult.Refused("Character not found in the specified slot.");

        if (!PassiveLifecycle.CanMoveCharacter(this, player, targetCard, fromSlotIndex, byEffect: false))
            return DuelActionResult.Refused("Movement forbidden by passive effect.");

        if (State.MoveCharacter(player, targetCard, fromSlotIndex, toSlotIndex, insertIndex))
        {
            Emit(new CharacterMovedSlotEvent(player, targetCard, fromSlotIndex, toSlotIndex));
            CheckCombosAfterPlayOrMove(player, fromSlotIndex);
            CheckCombosAfterPlayOrMove(player, toSlotIndex);

            // Déclenchement des effets de terrain sur déplacement (ex: ROAD TRIP S5)
            for (int i = 0; i < State.Slots.Count; i++)
            {
                var s = State.Slots[i];
                if (!s.IsEmpty && s.Card != null && s.Card.EffectIds != null &&
                    s.Card.EffectIds.Contains("terrain_on_move_draw_1"))
                {
                    Emit(new EffectTriggeredEvent(player, "terrain_on_move_draw_1", s.Card, i));
                    DrawCards(player, 1);
                    if (State.IsGameOver) break;
                }
            }

            if (!State.IsGameOver)
            {
                PassiveLifecycle.OnCharacterMoved(this, player, targetCard, fromSlotIndex, toSlotIndex);
            }

            return DuelActionResult.Ok();
        }

        return DuelActionResult.Refused("Failed to move character.");
    }

    private void CheckCombosAfterPlayOrMove(PlayerId player, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= State.Slots.Count) return;
        var slot = State.Slots[slotIndex];
        var chars = slot.GetCharacters(player);
        bool anyComboFormed = false;

        for (int i = 1; i < chars.Count; i++)
        {
            if (slot.IsComboActiveOnSlot(player, i, State))
            {
                anyComboFormed = true;
                Emit(new ComboFormedEvent(player, slotIndex, chars[i - 1], chars[i]));
                if (!State.IsGameOver)
                {
                    var leftGems = slot.GetEffectiveGems(player, chars[i - 1], State);
                    var rightGems = slot.GetEffectiveGems(player, chars[i], State);
                    bool orangeCombo = (leftGems & GemMask.OpeningOrange) != 0 && (rightGems & GemMask.ClosingOrange) != 0;
                    bool violetCombo = (leftGems & GemMask.OpeningViolet) != 0 && (rightGems & GemMask.ClosingViolet) != 0;
                    if (orangeCombo)
                    {
                        TryTriggerSubReaction(player.Opponent(), "opp_orange_combo", chars[i], () => { });
                    }
                    if (!State.IsGameOver && violetCombo)
                    {
                        TryTriggerSubReaction(player.Opponent(), "opp_violet_combo", chars[i], () => { });
                    }
                }
            }
        }

        PassiveLifecycle.OnAfterComboCheck(this, player, slotIndex, anyComboFormed);
    }

    public DuelActionResult PlayCharacter(
        PlayerId player,
        string cardId,
        int slotIndex,
        int insertIndex = -1,
        IReadOnlyList<string>? paidCardIds = null,
        bool bypassCost = false)
    {
        if (State.TurnNumber == 0)
            return DuelActionResult.Refused("Game has not started yet.");

        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (State.ActivePlayer != player)
            return DuelActionResult.Refused("Not this player's turn.");

        if (State.TerrainRequirementPending)
            return DuelActionResult.Refused("Must place a terrain before playing characters.");

        if (slotIndex < 0 || slotIndex >= State.Slots.Count)
            return DuelActionResult.Refused($"Invalid slot index: {slotIndex}.");

        var slot = State.Slots[slotIndex];
        if (slot.IsEmpty)
            return DuelActionResult.Refused($"Cannot play a character on an empty slot ({slotIndex}). A Terrain is required.");

        var pState = PassiveCardHelper.GetState(State);
        bool isFirstFreeOnSlot = PassiveLifecycle.IsFirstPlayFreeOnSlot(this, slot, slotIndex);
        if (!isFirstFreeOnSlot && State.CharactersPlayedThisTurn >= Rules.MaxCharactersPerTurn + pState.BonusCharPlaysThisTurn)
            return DuelActionResult.Refused($"Cannot play more than {Rules.MaxCharactersPerTurn} characters per turn.");

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

        var playRestriction = PassiveLifecycle.ValidatePlayCharacter(this, player, cardToPlay, slot, slotIndex, ref insertIndex);
        if (playRestriction != null)
            return playRestriction;

        int effectiveCost = bypassCost ? 0 : GetEffectivePlayCost(player, cardToPlay, slotIndex);
        if (effectiveCost > 0)
        {
            if (PassiveCardHelper.RawCardHasEffect(cardToPlay, "passive_pay_cost_by_discarding_in_play"))
            {
                var boardTargets = new List<(TerrainSlot Slot, int SlotIdx, DuelCard Card)>();
                for (int s = 0; s < State.Slots.Count; s++)
                {
                    var sl = State.Slots[s];
                    var chars = sl.GetCharacters(player);
                    for (int k = 0; k < chars.Count; k++)
                    {
                        boardTargets.Add((sl, s, chars[k]));
                    }
                }
                if (boardTargets.Count < effectiveCost)
                {
                    return DuelActionResult.Refused($"Not enough characters in play to pay cost {effectiveCost} for {cardToPlay.Name}.");
                }
                for (int k = 0; k < effectiveCost; k++)
                {
                    var t = boardTargets[k];
                    PassiveLifecycle.DiscardCharacterFromPlay(this, player, t.Slot, t.SlotIdx, t.Card, byEffect: false);
                }
            }
            else if (PassiveCardHelper.RawCardHasEffect(cardToPlay, "passive_discard_bebe_play_free") &&
                     hand.Count - 1 < effectiveCost &&
                     TryDiscardNamedAllyForFreePlay(player, "Bébé"))
            {
                // Cost paid by discarding a Bébé character in play
            }
            else
            {
                var payableCards = new List<DuelCard>();
                for (int i = 0; i < hand.Count; i++)
                {
                    if (!ReferenceEquals(hand[i], cardToPlay))
                    {
                        payableCards.Add(hand[i]);
                    }
                }

                // Téléachat (#300052): opponent may pay the +2 surcharge by milling 2 instead
                bool hasMillSurchargeOption = paidCardIds == null &&
                    payableCards.Count < effectiveCost &&
                    slot.PlacedBy == player.Opponent() &&
                    PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_cost_opp_plus_2") &&
                    payableCards.Count >= Math.Max(0, effectiveCost - 2) &&
                    State.GetDeck(player).Count >= 2;

                int handCostNeeded = hasMillSurchargeOption ? Math.Max(0, effectiveCost - 2) : effectiveCost;
                if (payableCards.Count < handCostNeeded)
                {
                    return DuelActionResult.Refused($"Not enough cards in hand to pay cost {effectiveCost} for {cardToPlay.Name}.");
                }

                if (hasMillSurchargeOption)
                {
                    MillCards(player, 2);
                    if (State.IsGameOver) return DuelActionResult.Ok();
                }

                var cardsToDiscard = new List<DuelCard>(handCostNeeded);
                if (handCostNeeded > 0)
                {
                    if (paidCardIds != null)
                    {
                        var remaining = new List<DuelCard>(payableCards);
                        for (int i = 0; i < paidCardIds.Count && cardsToDiscard.Count < handCostNeeded; i++)
                        {
                            string payId = paidCardIds[i];
                            for (int j = 0; j < remaining.Count; j++)
                            {
                                if (string.Equals(remaining[j].Id, payId, StringComparison.Ordinal))
                                {
                                    cardsToDiscard.Add(remaining[j]);
                                    remaining.RemoveAt(j);
                                    break;
                                }
                            }
                        }

                        if (cardsToDiscard.Count < handCostNeeded)
                        {
                            return DuelActionResult.Refused($"Insufficient valid payment cards provided ({cardsToDiscard.Count}/{handCostNeeded}).");
                        }
                    }
                    else if (RequireInteractiveCostPayment && player == PlayerId.Player1)
                    {
                        if (payableCards.Count == handCostNeeded)
                        {
                            cardsToDiscard.AddRange(payableCards);
                        }
                        else
                        {
                            Emit(new PlayerCostPaymentRequiredEvent(
                                player,
                                cardToPlay,
                                slotIndex,
                                insertIndex,
                                handCostNeeded,
                                payableCards,
                                selectedIds => PlayCharacter(player, cardId, slotIndex, insertIndex, selectedIds, bypassCost: false),
                                () => { }));
                            return DuelActionResult.Ok();
                        }
                    }
                    else
                    {
                        payableCards.Sort((a, b) =>
                        {
                            int scoreRankA = a.IsScoreur ? 2 : (a.IsComboScoreur ? 1 : 0);
                            int scoreRankB = b.IsScoreur ? 2 : (b.IsComboScoreur ? 1 : 0);
                            int cmp = scoreRankA.CompareTo(scoreRankB);
                            if (cmp != 0) return cmp;
                            cmp = a.Cost.CompareTo(b.Cost);
                            if (cmp != 0) return cmp;
                            return a.Force.CompareTo(b.Force);
                        });
                        for (int i = 0; i < handCostNeeded; i++)
                        {
                            cardsToDiscard.Add(payableCards[i]);
                        }
                    }

                    var actuallyDiscarded = new List<DuelCard>(cardsToDiscard.Count);
                    for (int i = 0; i < cardsToDiscard.Count; i++)
                    {
                        var payCard = cardsToDiscard[i];
                        if (State.RemoveCardFromHand(player, payCard))
                        {
                            actuallyDiscarded.Add(payCard);
                        }
                    }
                    if (actuallyDiscarded.Count > 0)
                    {
                        PassiveLifecycle.SendCardsToDiscardOrBanish(this, player, actuallyDiscarded);
                    }
                }
            }
        }

        PassiveLifecycle.OnBeforeCharacterAdded(this, player, cardToPlay);
        bool wasFirstCharThisTurn = !FirstCharacterPlayedThisTurn;

        State.RemoveCardFromHand(player, cardToPlay);
        State.AddCharacterToSlot(slotIndex, player, cardToPlay, insertIndex);
        if (isFirstFreeOnSlot)
        {
            pState.FirstPlayFreeUsedSlots.Add(slotIndex);
        }
        else
        {
            State.IncrementCharactersPlayed();
        }
        FirstCharacterPlayedThisTurn = true;

        Emit(new CharacterPlayedEvent(player, slotIndex, cardToPlay));

        CheckCombosAfterPlayOrMove(player, slotIndex);
        if (!State.IsGameOver)
        {
            PassiveLifecycle.OnCharacterPlayed(this, player, slotIndex, cardToPlay, _rng, wasFirstCharThisTurn);
        }

        // Effets passifs du terrain sur lequel le personnage est joué (ex: TERRADOLLAR / BRASSERIE S5)
        if (!State.IsGameOver && slot.Card != null)
        {
            if (CardHasEffect(slot.Card, "terrain_on_char_played_draw_1"))
            {
                Emit(new EffectTriggeredEvent(player, "terrain_on_char_played_draw_1", slot.Card, slotIndex));
                DrawCards(player, 1);
            }
            if (!State.IsGameOver && CardHasEffect(slot.Card, "terrain_on_char_played_banish_top_1"))
            {
                Emit(new EffectTriggeredEvent(player, "terrain_on_char_played_banish_top_1", slot.Card, slotIndex));
                BanishCardsFromDeck(player, 1);
            }
        }

        // Déterminer l'index réel du personnage posé et si son Combo est actif
        var charsOnSlot = slot.GetCharacters(player);
        int placedIndex = -1;
        for (int i = charsOnSlot.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(charsOnSlot[i], cardToPlay))
            {
                placedIndex = i;
                break;
            }
        }
        bool isComboActiveForCard = placedIndex >= 0 && slot.IsComboActiveOnSlot(player, placedIndex, State);

        // Exécution des effets OnPlay de la carte (IEffect)
        if (!State.IsGameOver &&
            !PassiveCardHelper.AreCardPassivesSuppressed(State, slot, player, cardToPlay, isComboActiveForCard) &&
            cardToPlay.EffectIds != null && cardToPlay.EffectIds.Count > 0)
        {
            var ctx = new EffectContext(this, player, cardToPlay, slotIndex);
            for (int i = 0; i < cardToPlay.EffectIds.Count; i++)
            {
                string effectId = cardToPlay.EffectIds[i];
                if (effectId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) ||
                    effectId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Effects.TryGetEffect(effectId, out var effect) && effect != null)
                {
                    Emit(new EffectTriggeredEvent(player, effectId, cardToPlay, slotIndex));
                    effect.Execute(ctx);
                    if (State.IsGameOver) break;
                }
            }
        }

        // Exécution des effets actifs de Combo si le personnage forme un Combo à la pose
        if (!State.IsGameOver && isComboActiveForCard &&
            !PassiveCardHelper.AreCardPassivesSuppressed(State, slot, player, cardToPlay, isComboActiveForCard) &&
            cardToPlay.ComboEffectIds != null && cardToPlay.ComboEffectIds.Count > 0)
        {
            var ctx = new EffectContext(this, player, cardToPlay, slotIndex);
            for (int i = 0; i < cardToPlay.ComboEffectIds.Count; i++)
            {
                string comboEffectId = cardToPlay.ComboEffectIds[i];
                if (comboEffectId.StartsWith("boost_", StringComparison.OrdinalIgnoreCase) ||
                    comboEffectId.StartsWith("passive_boost_", StringComparison.OrdinalIgnoreCase) ||
                    comboEffectId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) ||
                    comboEffectId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Effects.TryGetEffect(comboEffectId, out var comboEffect) && comboEffect != null)
                {
                    Emit(new EffectTriggeredEvent(player, comboEffectId, cardToPlay, slotIndex));
                    comboEffect.Execute(ctx);
                    if (State.IsGameOver) break;
                }
            }
        }

        if (!State.IsGameOver)
        {
            TryTriggerSubReaction(player.Opponent(), "opp_play_char", cardToPlay, () => { });
        }

        // Déclenchement automatique du scoring si Scoreur (ou Scoreur en Combo actif)
        // OU si la Force totale sur ce terrain actif atteint le seuil automatique (11+ ou modifié) en menant le duel
        bool autoScoreReady = false;
        if (!slot.IsEmpty && slot.IsActive(State.TurnNumber))
        {
            int myForce = slot.GetForce(player, State, forScoring: true);
            int oppForce = slot.GetForce(player.Opponent(), State, forScoring: true);
            int oppChars = slot.GetCharacters(player.Opponent()).Count;
            int threshold = slot.GetEffectiveAutoScoreThreshold(player, Rules.AutoScoreForceThreshold, State);
            autoScoreReady = myForce >= threshold && (oppChars == 0 || myForce > oppForce);
        }

        bool scorerTriggers = cardToPlay.IsScoreur || (cardToPlay.IsComboScoreur && isComboActiveForCard);
        bool triggersScore = (scorerTriggers && PassiveLifecycle.CanScoreSlot(this, player, slot, slotIndex, isAutoScore: false)) ||
                             (autoScoreReady && PassiveLifecycle.CanScoreSlot(this, player, slot, slotIndex, isAutoScore: true));
        if (!State.IsGameOver && !slot.IsEmpty && triggersScore && slot.IsActive(State.TurnNumber))
        {
            ResolveScoreInternal(slotIndex, player, isAutoScore: !scorerTriggers && autoScoreReady);
        }

        CheckDeckOutCondition(player);
        return DuelActionResult.Ok();
    }

    private bool TryDiscardNamedAllyForFreePlay(PlayerId player, string namePart)
    {
        for (int s = 0; s < State.Slots.Count; s++)
        {
            var sl = State.Slots[s];
            var chars = sl.GetCharacters(player);
            for (int i = 0; i < chars.Count; i++)
            {
                if (PassiveCardHelper.CardMatchesName(State, player, chars[i], namePart, sl))
                {
                    return PassiveLifecycle.DiscardCharacterFromPlay(this, player, sl, s, chars[i], byEffect: false);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Règle officielle Wankul TCG (Score automatique) :
    /// Lorsqu'un joueur totalise au moins 11 points (110 Force) sur un terrain actif où il a au moins 1 personnage,
    /// il peut déclencher le score sans avoir besoin d'un Scoreur.
    /// </summary>
    public DuelActionResult TriggerAutoScore(PlayerId player, int slotIndex)
    {
        if (State.TurnNumber == 0)
            return DuelActionResult.Refused("Game has not started yet.");

        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (State.ActivePlayer != player)
            return DuelActionResult.Refused("Not player's turn.");

        if (State.TerrainRequirementPending)
            return DuelActionResult.Refused("Must place a terrain first.");

        if (slotIndex < 0 || slotIndex >= State.Slots.Count)
            return DuelActionResult.Refused($"Invalid slot index: {slotIndex}.");

        var slot = State.Slots[slotIndex];
        if (slot.IsEmpty)
            return DuelActionResult.Refused("Slot is empty.");

        if (!slot.IsActive(State.TurnNumber))
            return DuelActionResult.Refused("Terrain is not active yet.");

        var chars = slot.GetCharacters(player);
        if (chars == null || chars.Count == 0)
            return DuelActionResult.Refused("Must have at least one character on the terrain.");

        if (!PassiveLifecycle.CanScoreSlot(this, player, slot, slotIndex, isAutoScore: true))
            return DuelActionResult.Refused("Scoring on this terrain is currently forbidden.");

        int force = slot.GetForce(player, State, forScoring: true);
        int threshold = slot.GetEffectiveAutoScoreThreshold(player, Rules.AutoScoreForceThreshold, State);
        if (force < threshold)
            return DuelActionResult.Refused($"Insufficient force for auto score: {force} < {threshold}.");

        ResolveScoreInternal(slotIndex, player, isAutoScore: true);
        return DuelActionResult.Ok();
    }

    /// <summary>
    /// Résout le combat sur un terrain : compare la force totale de chaque camp.
    /// Règle officielle Wankul :
    /// - Pour gagner, un joueur doit avoir au moins 1 personnage sur le terrain.
    /// - En cas d'égalité stricte de Force, l'attaquant perd et le défenseur gagne le terrain.
    /// </summary>
    public void ResolveScore(int slotIndex, PlayerId? initiator = null) =>
        ResolveScoreInternal(slotIndex, initiator, isAutoScore: false);

    private void ResolveScoreInternal(int slotIndex, PlayerId? initiator, bool isAutoScore)
    {
        if (slotIndex < 0 || slotIndex >= State.Slots.Count) return;
        var slot = State.Slots[slotIndex];
        if (slot.IsEmpty) return;

        var attacker = initiator ?? State.ActivePlayer;
        var defender = attacker.Opponent();

        if (!PassiveLifecycle.CanScoreSlot(this, attacker, slot, slotIndex, isAutoScore)) return;

        if (PassiveLifecycle.ShouldBlockDefenderSubOnScore(this, slot, isAutoScore))
        {
            ResolveScoreCore(slotIndex, initiator);
            return;
        }

        TryTriggerSubReaction(defender, "opp_score", slot.Card, () => ResolveScoreCore(slotIndex, initiator));
    }

    private void ResolveScoreCore(int slotIndex, PlayerId? initiator)
    {
        if (State.IsGameOver || slotIndex < 0 || slotIndex >= State.Slots.Count) return;
        var slot = State.Slots[slotIndex];
        if (slot.IsEmpty) return;

        var attacker = initiator ?? State.ActivePlayer;
        var defender = attacker.Opponent();

        PassiveLifecycle.OnBeforeResolveScore(this, slotIndex, attacker, defender, out bool cancelled);
        if (cancelled || State.IsGameOver) return;

        slot = State.Slots[slotIndex];
        if (slot.IsEmpty) return;

        int forceP1 = slot.GetForce(PlayerId.Player1, State, forScoring: true);
        int forceP2 = slot.GetForce(PlayerId.Player2, State, forScoring: true);
        int charCountP1 = slot.CharactersP1.Count;
        int charCountP2 = slot.CharactersP2.Count;

        PlayerId? winner = null;
        bool winMostChars = CardHasEffect(slot.Card, "passive_terrain_win_most_characters");

        if (charCountP1 > 0 && charCountP2 == 0)
        {
            winner = PlayerId.Player1;
        }
        else if (charCountP2 > 0 && charCountP1 == 0)
        {
            winner = PlayerId.Player2;
        }
        else if (charCountP1 == 0 && charCountP2 == 0)
        {
            winner = null;
        }
        else if (winMostChars)
        {
            if (charCountP1 > charCountP2) winner = PlayerId.Player1;
            else if (charCountP2 > charCountP1) winner = PlayerId.Player2;
            else winner = defender;
        }
        else
        {
            if (forceP1 > forceP2)
            {
                winner = PlayerId.Player1;
            }
            else if (forceP2 > forceP1)
            {
                winner = PlayerId.Player2;
            }
            else
            {
                winner = defender;
            }
        }

        if (winner.HasValue)
        {
            State.AddScore(winner.Value, 1);
        }

        var pState = PassiveCardHelper.GetState(State);
        pState.ScoresResolvedThisTurn++;

        Emit(new ScoreResolvedEvent(slotIndex, forceP1, forceP2, winner));

        var charsP1 = new List<DuelCard>(slot.CharactersP1);
        var charsP2 = new List<DuelCard>(slot.CharactersP2);
        var terrainCard = slot.Card!;
        var terrainPlacedBy = slot.PlacedBy ?? State.ActivePlayer;

        PassiveLifecycle.CleanupSlotOnScore(this, slotIndex, slot, terrainCard, terrainPlacedBy, winner, forceP1, forceP2);

        if (winner.HasValue && State.GetScore(winner.Value) >= Rules.TerrainsToWin)
        {
            EndGame(winner.Value, GameOverReason.FiveTerrains);
            return;
        }

        if (winner.HasValue && !State.IsGameOver && terrainCard != null)
        {
            var winPlayer = winner.Value;
            var losePlayer = winPlayer.Opponent();

            bool doubleDiff4 = CardHasEffect(terrainCard, "passive_terrain_double_win_if_diff_4") &&
                               Math.Abs(forceP1 - forceP2) > 4;
            bool doubleNextWin = DoubleNextWinEffectPlayer == winPlayer;
            if (doubleNextWin) DoubleNextWinEffectPlayer = null;
            int winRepeat = (doubleDiff4 ? 2 : 1) * (doubleNextWin ? 2 : 1);

            bool doubleNextLose = DoubleNextLoseEffectPlayer == losePlayer;
            if (doubleNextLose) DoubleNextLoseEffectPlayer = null;
            int loseRepeat = doubleNextLose ? 2 : 1;

            PassiveLifecycle.ExecuteScoreWinLoseEffects(
                this, slotIndex, terrainCard, winPlayer, losePlayer, charsP1, charsP2, winRepeat, loseRepeat);

            if (!State.IsGameOver)
            {
                PassiveLifecycle.OnAfterResolveScore(
                    this, slotIndex, terrainCard, winPlayer, losePlayer, forceP1, forceP2, charsP1, charsP2);
            }
        }
    }

    /// <summary>
    /// Tente de piocher count cartes.
    /// En cas d'épuisement (deck &lt; count), toutes les cartes encore disponibles sont piochées en main,
    /// puis l'épuisement déclenche DeckExhaustedEvent. La défaite par DeckOut ne survient que si pioche ET main sont vides.
    /// </summary>
    public IReadOnlyList<DuelCard> DrawCards(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        if (!PassiveLifecycle.CanDrawCards(this, player))
            return Array.Empty<DuelCard>();

        var pState = PassiveCardHelper.GetState(State);
        bool hadOppPlus1Discard1 = pState.DrawDepth == 0 &&
            PassiveCardHelper.BoardHasCardWithPassive(State, "passive_opp_draw_plus_1_discard_1", player.Opponent());

        int adjustedCount = PassiveLifecycle.AdjustDrawCount(this, player, count);
        if (adjustedCount <= 0)
            return Array.Empty<DuelCard>();

        var drawn = new List<DuelCard>();
        int needed = adjustedCount;

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
            CheckDeckOutCondition(player);
        }

        PassiveLifecycle.OnAfterDrawCards(this, player, drawn, hadOppPlus1Discard1);

        return drawn;
    }

    /// <summary>
    /// Meule count cartes depuis le dessus du deck du joueur vers sa défausse.
    /// Si le deck contient moins de cartes que demandé, toutes les cartes restantes vont en défausse,
    /// puis DeckExhaustedEvent est émis. La défaite par DeckOut ne survient que si pioche ET main sont vides.
    /// </summary>
    public IReadOnlyList<DuelCard> MillCards(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        IReadOnlyList<DuelCard> result = Array.Empty<DuelCard>();
        TryTriggerSubReaction(player, "mill", null, () =>
        {
            result = ExecuteMillCards(player, count);
        });
        return result;
    }

    private IReadOnlyList<DuelCard> ExecuteMillCards(PlayerId player, int count)
    {
        int adjusted = PassiveLifecycle.AdjustMillCount(this, count);
        if (adjusted <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        var milled = new List<DuelCard>();
        int needed = adjusted;

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
            PassiveLifecycle.OnAfterMilledCards(this, player, milled);
        }

        if (needed > 0)
        {
            Emit(new DeckExhaustedEvent(player));
            CheckDeckOutCondition(player);
        }

        return milled;
    }

    /// <summary>
    /// Défausse count cartes de la main d'un joueur vers sa défausse (du début de la main).
    /// </summary>

    public IReadOnlyList<DuelCard> BanishCardsFromDeck(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver) return Array.Empty<DuelCard>();
        var banished = new List<DuelCard>();
        for (int i = 0; i < count; i++)
        {
            var card = State.PopTopDeckCard(player);
            if (card == null) break;
            banished.Add(card);
        }
        if (banished.Count > 0) PassiveLifecycle.SendToBanish(this, player, banished);
        if (banished.Count < count)
        {
            Emit(new DeckExhaustedEvent(player));
            CheckDeckOutCondition(player);
        }
        return banished;
    }

    public IReadOnlyList<DuelCard> BanishCardsFromDiscard(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver) return Array.Empty<DuelCard>();
        var banished = new List<DuelCard>();
        var discard = State.GetDiscard(player);
        int toBanish = Math.Min(count, discard.Count);
        for (int i = 0; i < toBanish; i++)
        {
            if (discard.Count == 0) break;
            var card = discard[discard.Count - 1];
            State.RemoveCardFromDiscard(player, card);
            banished.Add(card);
        }
        if (banished.Count > 0) PassiveLifecycle.SendToBanish(this, player, banished);
        return banished;
    }

    public IReadOnlyList<DuelCard> BanishCardsFromHand(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver) return Array.Empty<DuelCard>();
        var banished = new List<DuelCard>();
        var hand = State.GetHand(player);
        int toBanish = Math.Min(count, hand.Count);
        for (int i = 0; i < toBanish; i++)
        {
            var card = hand[0];
            State.RemoveCardFromHand(player, card);
            banished.Add(card);
        }
        if (banished.Count > 0) PassiveLifecycle.SendToBanish(this, player, banished);
        CheckDeckOutCondition(player);
        return banished;
    }

    public IReadOnlyList<DuelCard> SetAsideCardsFromDeck(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver) return Array.Empty<DuelCard>();
        var setAside = new List<DuelCard>();
        for (int i = 0; i < count; i++)
        {
            var card = State.PopTopDeckCard(player);
            if (card == null) break;
            State.AddCardToSetAside(player, card);
            setAside.Add(card);
        }
        if (setAside.Count > 0) Emit(new CardsSetAsideEvent(player, setAside));
        if (setAside.Count < count)
        {
            Emit(new DeckExhaustedEvent(player));
            CheckDeckOutCondition(player);
        }
        return setAside;
    }

    public IReadOnlyList<DuelCard> DiscardFromHand(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        IReadOnlyList<DuelCard> result = Array.Empty<DuelCard>();
        TryTriggerSubReaction(player, "discard", null, () =>
        {
            result = ExecuteDiscardFromHand(player, count);
        });
        return result;
    }

    private IReadOnlyList<DuelCard> ExecuteDiscardFromHand(PlayerId player, int count)
    {
        if (count <= 0 || State.IsGameOver)
            return Array.Empty<DuelCard>();

        if (PassiveLifecycle.TryRedirectHandDiscardToMill(this, player, count))
            return Array.Empty<DuelCard>();

        int adjustedCount = PassiveLifecycle.AdjustHandDiscardCount(this, player, count);
        var discarded = new List<DuelCard>();
        var hand = State.GetHand(player);

        int toDiscard = Math.Min(adjustedCount, hand.Count);
        for (int i = 0; i < toDiscard; i++)
        {
            int idx = PassiveLifecycle.SelectHandDiscardIndex(this, player, _rng);
            var card = hand[idx];
            State.RemoveCardFromHand(player, card);
            discarded.Add(card);
        }

        if (discarded.Count > 0)
        {
            PassiveLifecycle.SendCardsToDiscardOrBanish(this, player, discarded);
            PassiveLifecycle.OnAfterHandDiscarded(this, player, discarded);
        }

        CheckDeckOutCondition(player);
        return discarded;
    }

    public void TryTriggerSubReaction(
        PlayerId reactingPlayer,
        string triggerKind,
        DuelCard? triggerCard,
        Action onProceed,
        Action? onCancelled = null)
    {
        if (State.IsGameOver)
        {
            onProceed?.Invoke();
            return;
        }

        // Vérifier si un terrain interdit les effets (Sub)
        for (int s = 0; s < State.Slots.Count; s++)
        {
            if (CardHasEffect(State.Slots[s].Card, "passive_terrain_disable_sub"))
            {
                onProceed?.Invoke();
                return;
            }
        }

        var hand = State.GetHand(reactingPlayer);
        DuelCard? matchedCard = null;
        string? matchedSubId = null;

        for (int i = 0; i < hand.Count; i++)
        {
            var c = hand[i];
            if (c.SubEffectIds == null || c.SubEffectIds.Count == 0) continue;
            for (int j = 0; j < c.SubEffectIds.Count; j++)
            {
                string subId = c.SubEffectIds[j];
                if (DoesSubMatchTrigger(subId, triggerKind))
                {
                    matchedCard = c;
                    matchedSubId = subId;
                    break;
                }
            }
            if (matchedCard != null) break;
        }

        if (matchedCard == null || matchedSubId == null)
        {
            onProceed?.Invoke();
            return;
        }

        void AcceptSub()
        {
            bool stillInHand = false;
            var currentHand = State.GetHand(reactingPlayer);
            for (int i = 0; i < currentHand.Count; i++)
            {
                if (ReferenceEquals(currentHand[i], matchedCard))
                {
                    stillInHand = true;
                    break;
                }
            }

            if (!stillInHand)
            {
                onProceed?.Invoke();
                return;
            }

            Emit(new EffectTriggeredEvent(reactingPlayer, matchedSubId, matchedCard, -1));

            if (Effects.TryGetEffect(matchedSubId, out var subEff) && subEff != null)
            {
                subEff.Execute(new EffectContext(this, reactingPlayer, matchedCard, -1));
            }

            if (!State.IsGameOver)
            {
                PassiveLifecycle.OnSubAccepted(this, reactingPlayer);
            }

            // Si la carte Sub n'a pas été posée sur le plateau par son effet, la défausser
            currentHand = State.GetHand(reactingPlayer);
            for (int i = 0; i < currentHand.Count; i++)
            {
                if (ReferenceEquals(currentHand[i], matchedCard))
                {
                    State.RemoveCardFromHand(reactingPlayer, matchedCard);
                    State.AddCardToDiscard(reactingPlayer, matchedCard);
                    Emit(new CardsDiscardedEvent(reactingPlayer, new[] { matchedCard }));
                    break;
                }
            }

            if (matchedSubId.StartsWith("sub_cancel_", StringComparison.OrdinalIgnoreCase))
            {
                onCancelled?.Invoke();
            }
            else
            {
                onProceed?.Invoke();
            }
        }

        void DeclineSub()
        {
            onProceed?.Invoke();
        }

        if (reactingPlayer == PlayerId.Player2)
        {
            AcceptSub();
        }
        else if (!RequireInteractiveCostPayment)
        {
            DeclineSub();
        }
        else
        {
            string desc = triggerCard != null
                ? $"Réagir avec {matchedCard.Name} suite à {triggerCard.Name} ({triggerKind}) ?"
                : $"Activer la réaction (Sub) {matchedCard.Name} ({triggerKind}) ?";
            Emit(new SubReactionPromptEvent(reactingPlayer, matchedCard, desc, AcceptSub, DeclineSub));
        }
    }

    private static bool DoesSubMatchTrigger(string subId, string triggerKind)
    {
        if (string.IsNullOrEmpty(subId) || string.IsNullOrEmpty(triggerKind)) return false;
        if (string.Equals(subId, triggerKind, StringComparison.OrdinalIgnoreCase)) return true;

        if (string.Equals(triggerKind, "discard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(triggerKind, "mill", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(triggerKind, "discard_or_mill", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(subId, "sub_cancel_discard_or_mill", StringComparison.OrdinalIgnoreCase) ||
                   subId.IndexOf("discard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   subId.IndexOf("mill", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (string.Equals(triggerKind, "opp_orange_combo", StringComparison.OrdinalIgnoreCase))
        {
            return subId.IndexOf("orange_combo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (string.Equals(triggerKind, "opp_violet_combo", StringComparison.OrdinalIgnoreCase))
        {
            return subId.IndexOf("violet_combo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (string.Equals(triggerKind, "opp_score", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(triggerKind, "score", StringComparison.OrdinalIgnoreCase))
        {
            return subId.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   subId.StartsWith("sub_reduce_", StringComparison.OrdinalIgnoreCase) ||
                   subId.StartsWith("sub_swap_", StringComparison.OrdinalIgnoreCase) ||
                   subId.StartsWith("sub_cancel_force", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(triggerKind, "opp_play_char", StringComparison.OrdinalIgnoreCase))
        {
            return subId.IndexOf("play_char", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (string.Equals(triggerKind, "opp_effect", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(subId, "sub_cancel_opp_effect", StringComparison.OrdinalIgnoreCase);
        }

        return subId.IndexOf(triggerKind, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public DuelActionResult EndTurn(PlayerId player)
    {
        if (State.IsGameOver)
            return DuelActionResult.Refused("Game is already over.");

        if (State.ActivePlayer != player)
            return DuelActionResult.Refused("Not this player's turn.");

        if (State.TerrainRequirementPending)
            return DuelActionResult.Refused("Must place a terrain before ending turn.");

        PassiveLifecycle.ExecuteEndTurnPassives(this, player);
        if (State.IsGameOver)
            return DuelActionResult.Ok();

        Emit(new TurnEndedEvent(player, State.TurnNumber));
        ResetTurnFlags();
        State.SetActivePlayer(player.Opponent());
        return DuelActionResult.Ok();
    }

    internal void CheckDeckOutCondition(PlayerId player)
    {
        if (State.IsGameOver) return;
        if (State.GetDeck(player).Count == 0 && State.GetHand(player).Count == 0)
        {
            EndGame(player.Opponent(), GameOverReason.DeckOut);
        }
    }

    private void EndGame(PlayerId winner, GameOverReason reason)
    {
        State.SetGameOver(winner, reason);
        Emit(new DuelEndedEvent(winner, reason));
    }
}
