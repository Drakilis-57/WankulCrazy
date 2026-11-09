#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// Intelligence artificielle pour le moteur de duel Wankul TCG.
/// N'utilise aucun composant Unity et n'appelle que l'API publique de DuelEngine.
/// </summary>
public sealed class DuelAI
{
    public PlayerId Player { get; }
    private readonly IRandom _rng;

    public DuelAI(PlayerId player, IRandom? rng = null)
    {
        Player = player;
        _rng = rng ?? new SystemRandomAdapter();
    }

    /// <summary>
    /// Exécute les actions du tour de l'IA jusqu'à EndTurn.
    /// </summary>
    public void PlayTurn(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;
        if (engine.State.ActivePlayer != Player) return;

        void OnEngineEvent(IDuelEvent evt)
        {
            if (engine.State.IsGameOver) return;

            if (evt is PlayerChoiceRequiredEvent choiceEvt && choiceEvt.Player == Player)
            {
                ResolveCardChoice(engine, choiceEvt);
            }
            else if (evt is PlayerSlotChoiceRequiredEvent slotEvt && slotEvt.Player == Player)
            {
                ResolveSlotChoice(engine, slotEvt);
            }
        }

        engine.OnEvent += OnEngineEvent;
        try
        {
            // 1. Pose obligatoire de terrain si requis
            if (engine.State.TerrainRequirementPending)
            {
                TryPlayRequiredTerrain(engine);
                if (engine.State.TerrainRequirementPending)
                {
                    // Si toujours pending (cas anormal sans terrain trouvé), on ne peut pas continuer
                    return;
                }
            }

            // 2. Pose facultative d'un terrain si on en a plusieurs en main et qu'il y a un slot libre
            TryPlayOptionalTerrain(engine);

            // 3. Poser des personnages normaux sur des terrains existants
            PlayCharactersToReinforce(engine);

            // 4. Poser un Scoreur si on mène sur un terrain actif
            TryPlayScoreurToWin(engine);

            // 5. Déclencher l'autoscore (110+ Force) si aucun Scoreur n'est disponible en main
            TryTriggerAutoScoreToWin(engine);

            // 6. Terminer le tour
            if (!engine.State.IsGameOver && engine.State.ActivePlayer == Player)
            {
                engine.EndTurn(Player);
            }
        }
        finally
        {
            engine.OnEvent -= OnEngineEvent;
        }
    }

    private void ResolveCardChoice(DuelEngine engine, PlayerChoiceRequiredEvent choiceEvt)
    {
        if (choiceEvt.OnCardSelected != null && choiceEvt.Candidates != null && choiceEvt.Candidates.Count > 0)
        {
            DuelCard best = choiceEvt.Candidates[0];
            for (int i = 1; i < choiceEvt.Candidates.Count; i++)
            {
                var c = choiceEvt.Candidates[i];
                if (c.Force > best.Force)
                {
                    best = c;
                }
            }
            choiceEvt.OnCardSelected.Invoke(best);
            return;
        }

        if (string.Equals(choiceEvt.ChoiceContext, "move_character", StringComparison.OrdinalIgnoreCase))
        {
            TryResolveMoveCharacterChoice(engine, choiceEvt.SourceCard);
        }
    }

    private void TryResolveMoveCharacterChoice(DuelEngine engine, DuelCard? sourceCard)
    {
        int fromSlot = -1;
        DuelCard? charToMove = null;

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            var s = engine.State.Slots[i];
            if (s.IsEmpty) continue;
            var myChars = s.GetCharacters(Player);
            for (int j = 0; j < myChars.Count; j++)
            {
                if (sourceCard != null && myChars[j].Id == sourceCard.Id) continue;
                fromSlot = i;
                charToMove = myChars[j];
                break;
            }
            if (charToMove != null) break;
        }

        if (charToMove == null || fromSlot == -1) return;

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (i != fromSlot && !engine.State.Slots[i].IsEmpty)
            {
                engine.MoveCharacter(Player, charToMove.Id, fromSlot, i);
                break;
            }
        }
    }

    private void ResolveSlotChoice(DuelEngine engine, PlayerSlotChoiceRequiredEvent slotEvt)
    {
        if (slotEvt.OnSlotSelected == null) return;

        int chosenSlot = -1;
        int bestAdvantage = int.MinValue;

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            var s = engine.State.Slots[i];
            if (!s.IsEmpty && s.IsActive(engine.State.TurnNumber))
            {
                int adv = s.GetForce(Player) - s.GetForce(Player.Opponent());
                if (adv > bestAdvantage)
                {
                    bestAdvantage = adv;
                    chosenSlot = i;
                }
            }
        }

        if (chosenSlot == -1)
        {
            for (int i = 0; i < engine.State.Slots.Count; i++)
            {
                if (!engine.State.Slots[i].IsEmpty)
                {
                    chosenSlot = i;
                    break;
                }
            }
        }

        if (chosenSlot != -1)
        {
            slotEvt.OnSlotSelected.Invoke(chosenSlot);
        }
    }

    private void TryPlayRequiredTerrain(DuelEngine engine)
    {
        var hand = engine.State.GetHand(Player);
        DuelCard? terrain = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Terrain)
            {
                terrain = hand[i];
                break;
            }
        }

        if (terrain == null) return;

        int freeSlot = FindFirstFreeSlot(engine);
        if (freeSlot != -1)
        {
            engine.PlayTerrain(Player, terrain.Id, freeSlot);
        }
    }

    private void TryPlayOptionalTerrain(DuelEngine engine)
    {
        if (engine.State.TerrainsInPlayCount >= engine.Rules.MaxTerrainsOnBoard) return;

        var hand = engine.State.GetHand(Player);
        int terrainCountInHand = 0;
        DuelCard? candidate = null;

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Terrain)
            {
                terrainCountInHand++;
                if (candidate == null) candidate = hand[i];
            }
        }

        // Si on a plus d'1 terrain en main et qu'il y a de la place sur le plateau
        if (terrainCountInHand > 1 && candidate != null)
        {
            int freeSlot = FindFirstFreeSlot(engine);
            if (freeSlot != -1)
            {
                engine.PlayTerrain(Player, candidate.Id, freeSlot);
            }
        }
    }

    private void PlayCharactersToReinforce(DuelEngine engine)
    {
        while (!engine.State.IsGameOver &&
               engine.State.CharactersPlayedThisTurn < engine.Rules.MaxCharactersPerTurn)
        {
            var availableSlots = GetSlotsWithTerrain(engine);
            if (availableSlots.Count == 0) break;

            var hand = engine.State.GetHand(Player);
            DuelCard? bestChar = null;

            // On cherche un personnage régulier (non scoreur)
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind == CardKind.Character && !hand[i].IsScoreur)
                {
                    if (bestChar == null || hand[i].Force > bestChar.Force)
                    {
                        bestChar = hand[i];
                    }
                }
            }

            if (bestChar == null) break;

            // Choix du slot : préférer un terrain où la carte forme un Combo, sinon où l'adversaire a des forces
            int targetSlot = ChooseBestSlotForCharacter(engine, availableSlots, bestChar);
            if (targetSlot == -1) break;

            var result = engine.PlayCharacter(Player, bestChar.Id, targetSlot);
            if (!result.Success) break;
        }
    }

    private void TryTriggerAutoScoreToWin(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;

        // Ne déclencher l'autoscore (110+ Force) que si on n'a pas de Scoreur en main
        // ET que la réserve de terrains est épuisée (ou qu'un autoscore offre directement le point de la victoire)
        var hand = engine.State.GetHand(Player);
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Character && hand[i].IsScoreur)
            {
                return;
            }
        }

        bool isGameWinningPoint = engine.State.GetScore(Player) + 1 >= engine.Rules.TerrainsToWin;
        bool hasScoreurInDeck = false;
        var deck = engine.State.GetDeck(Player);
        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].Kind == CardKind.Character && deck[i].IsScoreur)
            {
                hasScoreurInDeck = true;
                break;
            }
        }

        // Si le deck contient encore des Scoreurs et que ce n'est pas le point de la victoire,
        // l'IA garde ses forces sur le terrain plutôt que de liquider le terrain par autoscore prématuré.
        if (hasScoreurInDeck && !isGameWinningPoint) return;

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (engine.State.IsGameOver) break;

            var slot = engine.State.Slots[i];
            if (!slot.IsEmpty && slot.IsActive(engine.State.TurnNumber))
            {
                var myChars = slot.GetCharacters(Player);
                if (myChars.Count > 0)
                {
                    int myForce = slot.GetForce(Player);
                    int oppForce = slot.GetForce(Player.Opponent());
                    int oppCharsCount = slot.GetCharacters(Player.Opponent()).Count;

                    bool wouldWin = oppCharsCount == 0 || myForce > oppForce;
                    if (myForce >= engine.Rules.AutoScoreForceThreshold && wouldWin)
                    {
                        engine.TriggerAutoScore(Player, i);
                    }
                }
            }
        }
    }

    private void TryPlayScoreurToWin(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;
        if (engine.State.CharactersPlayedThisTurn >= engine.Rules.MaxCharactersPerTurn) return;

        var hand = engine.State.GetHand(Player);
        DuelCard? scoreur = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Character && hand[i].IsScoreur)
            {
                scoreur = hand[i];
                break;
            }
        }

        if (scoreur == null) return;

        // Trouver un terrain actif où IA_Force + scoreur.Force > Opponent_Force
        int bestSlotToScore = -1;
        int maxAdvantage = -1;

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            var slot = engine.State.Slots[i];
            if (!slot.IsEmpty && slot.IsActive(engine.State.TurnNumber))
            {
                int myForce = slot.GetForce(Player) + scoreur.Force;
                int oppForce = slot.GetForce(Player.Opponent());

                if (myForce > oppForce)
                {
                    int advantage = myForce - oppForce;
                    if (advantage > maxAdvantage)
                    {
                        maxAdvantage = advantage;
                        bestSlotToScore = i;
                    }
                }
            }
        }

        if (bestSlotToScore != -1)
        {
            engine.PlayCharacter(Player, scoreur.Id, bestSlotToScore);
        }
    }

    private static int FindFirstFreeSlot(DuelEngine engine)
    {
        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (engine.State.Slots[i].IsEmpty) return i;
        }
        return -1;
    }

    private static List<int> GetSlotsWithTerrain(DuelEngine engine)
    {
        var list = new List<int>();
        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (!engine.State.Slots[i].IsEmpty) list.Add(i);
        }
        return list;
    }

    private int ChooseBestSlotForCharacter(DuelEngine engine, List<int> availableSlots, DuelCard? cardToPlay = null)
    {
        if (availableSlots.Count == 0) return -1;

        // 1. Si la carte possède une gemme de fermeture de Combo, privilégier un terrain où notre dernier perso a une gemme d'ouverture
        if (cardToPlay != null && cardToPlay.HasClosingGem)
        {
            for (int i = 0; i < availableSlots.Count; i++)
            {
                int slotIdx = availableSlots[i];
                var slot = engine.State.Slots[slotIdx];
                var myChars = slot.GetCharacters(Player);
                if (myChars.Count > 0 && myChars[myChars.Count - 1].HasOpeningGem)
                {
                    return slotIdx;
                }
            }
        }

        // 2. Sinon, préférer un terrain où l'adversaire a des forces pour rivaliser
        int bestSlot = availableSlots[0];
        int highestOppForce = -1;

        for (int i = 0; i < availableSlots.Count; i++)
        {
            int slotIdx = availableSlots[i];
            var slot = engine.State.Slots[slotIdx];
            int oppForce = slot.GetForce(Player.Opponent());
            if (oppForce > highestOppForce)
            {
                highestOppForce = oppForce;
                bestSlot = slotIdx;
            }
        }

        return bestSlot;
    }
}
