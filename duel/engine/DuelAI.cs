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

    private void HandleEngineEvent(DuelEngine engine, IDuelEvent evt)
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
        else if (evt is PlayerCostPaymentRequiredEvent payEvt && payEvt.Player == Player)
        {
            var ids = SelectBestPaymentCardIds(payEvt.PayableHandCards, string.Empty, payEvt.RequiredCount);
            if (ids != null) payEvt.OnConfirmPayment?.Invoke(ids);
            else payEvt.OnCancelPayment?.Invoke();
        }
        else if (evt is SubReactionPromptEvent subEvt && subEvt.ReactingPlayer == Player)
        {
            subEvt.OnAcceptSub?.Invoke();
        }
    }

    private T ExecuteWithEngineEvents<T>(DuelEngine engine, Func<T> action)
    {
        void OnEngineEvent(IDuelEvent evt) => HandleEngineEvent(engine, evt);
        engine.OnEvent += OnEngineEvent;
        try
        {
            return action();
        }
        finally
        {
            engine.OnEvent -= OnEngineEvent;
        }
    }

    /// <summary>
    /// Exécute les actions du tour de l'IA jusqu'à EndTurn.
    /// </summary>
    public void PlayTurn(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;
        if (engine.State.ActivePlayer != Player) return;

        ExecuteWithEngineEvents(engine, () =>
        {
            // 1. Pose obligatoire de terrain si requis
            if (engine.State.TerrainRequirementPending)
            {
                TryPlayRequiredTerrain(engine);
                if (engine.State.TerrainRequirementPending)
                {
                    // Si toujours pending (cas anormal sans terrain trouvé), on ne peut pas continuer
                    return true;
                }
            }

            // 2. Pose facultative d'un terrain si on en a plusieurs en main et qu'il y a un slot libre
            TryPlayOptionalTerrain(engine);

            // 3. Poser des personnages normaux sur des terrains existants
            PlayCharactersToReinforce(engine);

            // 4. Poser un Scoreur si on mène sur un terrain actif
            TryPlayScoreurToWin(engine);

            // 4b. Activer les passifs sûrs "une fois par tour"
            for (int guard = 0; guard < 8 && TryActivateOncePerTurnPassives(engine); guard++)
            {
            }

            // 5. Déclencher l'autoscore (seuil effectif) si aucun Scoreur n'est disponible en main
            TryTriggerAutoScoreToWin(engine);

            // 6. Terminer le tour
            if (!engine.State.IsGameOver && engine.State.ActivePlayer == Player)
            {
                engine.EndTurn(Player);
            }
            return true;
        });
    }

    /// <summary>
    /// Exécute une seule action élémentaire du tour de l'IA (pose d'un terrain, d'un personnage ou autoscore).
    /// Retourne true si une action a été effectuée, false si l'IA a fini ses actions et peut passer la main.
    /// Permet à la vue 3D d'espacer visuellement chaque action de l'IA.
    /// </summary>
    public bool PlaySingleStep(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return false;
        if (engine.State.ActivePlayer != Player) return false;

        return ExecuteWithEngineEvents(engine, () =>
        {
            // 1. Pose obligatoire de terrain si requis
            if (engine.State.TerrainRequirementPending)
            {
                int beforeTerrains = engine.State.TerrainsInPlayCount;
                TryPlayRequiredTerrain(engine);
                return engine.State.TerrainsInPlayCount > beforeTerrains && !engine.State.TerrainRequirementPending;
            }

            // 2. Pose facultative d'un terrain
            int terrainsBeforeOpt = engine.State.TerrainsInPlayCount;
            TryPlayOptionalTerrain(engine);
            if (engine.State.TerrainsInPlayCount > terrainsBeforeOpt)
            {
                return true;
            }

            // 3. Poser un Scoreur si on peut gagner un terrain actif immédiatement
            int charsBeforeScoreur = engine.State.CharactersPlayedThisTurn;
            TryPlayScoreurToWin(engine);
            if (engine.State.CharactersPlayedThisTurn > charsBeforeScoreur)
            {
                return true;
            }

            // 4. Poser un seul personnage régulier (ou DUO à coût 0) pour renforcer
            if (!engine.State.IsGameOver &&
                engine.State.CharactersPlayedThisTurn < engine.Rules.MaxCharactersPerTurn)
            {
                var availableSlots = GetSlotsWithTerrain(engine);
                if (availableSlots.Count > 0 &&
                    TrySelectBestReinforcePlay(engine, availableSlots, out var bestChar, out int targetSlot, out var paidIds) &&
                    bestChar != null &&
                    targetSlot != -1)
                {
                    var res = engine.PlayCharacter(Player, bestChar.Id, targetSlot, -1, paidIds);
                    if (res.Success)
                    {
                        return true;
                    }
                }
            }

            // 4b. Activer un passif sûr "une fois par tour"
            if (TryActivateOncePerTurnPassives(engine))
            {
                return true;
            }

            // 5. Déclencher l'autoscore (seuil effectif) sur un terrain actif où l'IA mène
            int scoreBeforeAuto = engine.State.GetScore(Player);
            TryTriggerAutoScoreToWin(engine);
            if (engine.State.GetScore(Player) > scoreBeforeAuto)
            {
                return true;
            }

            return false;
        });
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
                int adv = s.GetForce(Player, engine.State) - s.GetForce(Player.Opponent(), engine.State);
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

            if (!TrySelectBestReinforcePlay(engine, availableSlots, out var bestChar, out int targetSlot, out var paidIds) ||
                bestChar == null ||
                targetSlot == -1)
            {
                break;
            }

            var result = engine.PlayCharacter(Player, bestChar.Id, targetSlot, -1, paidIds);
            if (!result.Success) break;
        }
    }

    private void TryTriggerAutoScoreToWin(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;

        // Ne déclencher l'autoscore (11+ Force) que si on n'a pas déjà un Scoreur jouable et payable ce tour
        if (engine.State.CharactersPlayedThisTurn < engine.Rules.MaxCharactersPerTurn)
        {
            var hand = engine.State.GetHand(Player);
            for (int i = 0; i < hand.Count; i++)
            {
                var c = hand[i];
                if (c.Kind != CardKind.Character || !c.IsScoreur) continue;
                for (int s = 0; s < engine.State.Slots.Count; s++)
                {
                    var slot = engine.State.Slots[s];
                    if (!slot.IsEmpty && slot.IsActive(engine.State.TurnNumber) &&
                        CanAffordCardOnSlot(engine, hand, c, s, out _))
                    {
                        return;
                    }
                }
            }
        }

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (engine.State.IsGameOver) break;

            var slot = engine.State.Slots[i];
            if (!slot.IsEmpty && slot.IsActive(engine.State.TurnNumber))
            {
                var myChars = slot.GetCharacters(Player);
                if (myChars.Count > 0)
                {
                    int myForce = slot.GetForce(Player, engine.State);
                    int oppForce = slot.GetForce(Player.Opponent(), engine.State);
                    int oppCharsCount = slot.GetCharacters(Player.Opponent()).Count;

                    bool wouldWin = oppCharsCount == 0 || myForce > oppForce;
                    int threshold = slot.GetEffectiveAutoScoreThreshold(Player, engine.Rules.AutoScoreForceThreshold, engine.State);
                    if (myForce >= threshold && wouldWin)
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
        int bestSlotToScore = -1;
        int bestCost = int.MaxValue;
        int maxAdvantage = -1;

        for (int h = 0; h < hand.Count; h++)
        {
            var candidate = hand[h];
            if (candidate.Kind != CardKind.Character) continue;
            if (!candidate.IsScoreur && !candidate.IsComboScoreur) continue;

            for (int i = 0; i < engine.State.Slots.Count; i++)
            {
                var slot = engine.State.Slots[i];
                if (!slot.IsEmpty && slot.IsActive(engine.State.TurnNumber))
                {
                    if (!CanAffordCardOnSlot(engine, hand, candidate, i, out int effCost))
                    {
                        continue;
                    }

                    var myChars = slot.GetCharacters(Player);
                    if (!candidate.IsScoreur && candidate.IsComboScoreur)
                    {
                        // Un Scoreur de Combo nécessite un allié avec une gemme d'ouverture compatible déjà présent sur ce terrain
                        if (myChars.Count == 0 || !slot.CanConnectOnSlot(Player, myChars[myChars.Count - 1], candidate))
                        {
                            continue;
                        }
                    }

                    int myForce = slot.GetForce(Player, engine.State) + candidate.Force;
                    int oppForce = slot.GetForce(Player.Opponent(), engine.State);

                    if (myForce > oppForce)
                    {
                        int advantage = myForce - oppForce;
                        if (advantage > maxAdvantage || (advantage == maxAdvantage && effCost < bestCost))
                        {
                            maxAdvantage = advantage;
                            bestCost = effCost;
                            bestSlotToScore = i;
                            scoreur = candidate;
                        }
                    }
                }
            }
        }

        if (bestSlotToScore != -1 && scoreur != null)
        {
            var paidIds = SelectBestPaymentCardIds(hand, scoreur.Id, bestCost);
            engine.PlayCharacter(Player, scoreur.Id, bestSlotToScore, -1, paidIds);
        }
    }

    private bool CanAffordCardOnSlot(DuelEngine engine, IReadOnlyList<DuelCard> hand, DuelCard card, int slotIndex, out int effectiveCost)
    {
        effectiveCost = engine.GetEffectivePlayCost(Player, card, slotIndex);
        return hand.Count - 1 >= effectiveCost;
    }

    private static int EvaluateDiscardFodderScore(DuelCard card)
    {
        // Plus le score est bas, plus la carte est une bonne candidate à défausser pour payer un coût.
        int score = card.Force * 10;
        if (card.IsScoreur || card.IsComboScoreur) score += 450;
        if (card.IsDuo) score += 260;
        if (card.Kind == CardKind.Terrain) score += 160;
        if (card.EffectIds.Count > 0 || card.ComboEffectIds.Count > 0 || card.PassiveEffectIds.Count > 0) score += 45;
        if (card.Cost > 0) score -= card.Cost * 15;
        return score;
    }

    private static IReadOnlyList<string>? SelectBestPaymentCardIds(
        IReadOnlyList<DuelCard> pool,
        string excludedCardId,
        int requiredCount)
    {
        if (requiredCount <= 0) return Array.Empty<string>();

        var candidates = new List<DuelCard>();
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Id != excludedCardId)
            {
                candidates.Add(pool[i]);
            }
        }

        if (candidates.Count < requiredCount) return null;

        candidates.Sort((a, b) =>
        {
            int cmp = EvaluateDiscardFodderScore(a).CompareTo(EvaluateDiscardFodderScore(b));
            if (cmp != 0) return cmp;
            cmp = a.Force.CompareTo(b.Force);
            if (cmp != 0) return cmp;
            return string.CompareOrdinal(a.Id, b.Id);
        });

        var result = new List<string>(requiredCount);
        for (int i = 0; i < requiredCount; i++)
        {
            result.Add(candidates[i].Id);
        }
        return result;
    }

    private bool TrySelectBestReinforcePlay(
        DuelEngine engine,
        List<int> availableSlots,
        out DuelCard? bestCard,
        out int bestSlot,
        out IReadOnlyList<string>? bestPaidIds)
    {
        bestCard = null;
        bestSlot = -1;
        bestPaidIds = null;

        var hand = engine.State.GetHand(Player);
        int bestUtility = int.MinValue;

        for (int h = 0; h < hand.Count; h++)
        {
            var card = hand[h];
            if (card.Kind != CardKind.Character || card.IsScoreur) continue;

            for (int s = 0; s < availableSlots.Count; s++)
            {
                int slotIdx = availableSlots[s];
                if (!CanAffordCardOnSlot(engine, hand, card, slotIdx, out int effCost))
                {
                    continue;
                }

                var paidIds = SelectBestPaymentCardIds(hand, card.Id, effCost);
                if (paidIds == null) continue;

                var slot = engine.State.Slots[slotIdx];
                var myChars = slot.GetCharacters(Player);
                bool formsCombo = card.HasClosingGem &&
                                  myChars.Count > 0 &&
                                  slot.CanConnectOnSlot(Player, myChars[myChars.Count - 1], card);

                int oppForce = slot.GetForce(Player.Opponent(), engine.State);
                int myForce = slot.GetForce(Player, engine.State);

                // Évite d'écraser sa main (ex: sacrifier des Scoreurs ou trop de cartes pour un personnage faible)
                if (effCost > 0)
                {
                    int sacrificeCostPenalty = 0;
                    bool sacrificesScoreur = false;
                    for (int p = 0; p < paidIds.Count; p++)
                    {
                        for (int k = 0; k < hand.Count; k++)
                        {
                            if (hand[k].Id == paidIds[p])
                            {
                                if (hand[k].IsScoreur || hand[k].IsComboScoreur) sacrificesScoreur = true;
                                sacrificeCostPenalty += Math.Max(15, hand[k].Force * 4);
                                break;
                            }
                        }
                    }

                    if (sacrificesScoreur && card.Force < 70 && !formsCombo)
                    {
                        continue;
                    }

                    if (effCost >= 2 && hand.Count - 1 - effCost == 0 && !formsCombo && card.Force < 45)
                    {
                        continue;
                    }

                    int rawValue = card.Force * 10 + (formsCombo ? 70 : 0) + (card.EffectIds.Count > 0 ? 45 : 0);
                    if (rawValue <= sacrificeCostPenalty && hand.Count < 5)
                    {
                        continue;
                    }
                }

                int utility = card.Force * 10
                    + (card.IsDuo ? 120 : 0)
                    + (formsCombo ? 85 : 0)
                    + (card.EffectIds.Count > 0 || card.ComboEffectIds.Count > 0 || card.PassiveEffectIds.Count > 0 ? 35 : 0)
                    + Math.Min(60, oppForce)
                    - effCost * 55;

                if (myForce > oppForce + 40 && oppForce == 0)
                {
                    utility -= 15;
                }

                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestCard = card;
                    bestSlot = slotIdx;
                    bestPaidIds = paidIds;
                }
            }
        }

        return bestCard != null && bestSlot != -1;
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

        // 1. Si la carte possède une gemme de fermeture de Combo, privilégier un terrain où notre dernier perso a une gemme d'ouverture compatible
        if (cardToPlay != null && cardToPlay.HasClosingGem)
        {
            for (int i = 0; i < availableSlots.Count; i++)
            {
                int slotIdx = availableSlots[i];
                var slot = engine.State.Slots[slotIdx];
                var myChars = slot.GetCharacters(Player);
                if (myChars.Count > 0 && ComboEvaluator.CanConnect(myChars[myChars.Count - 1], cardToPlay))
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
            int oppForce = slot.GetForce(Player.Opponent(), engine.State);
            if (oppForce > highestOppForce)
            {
                highestOppForce = oppForce;
                bestSlot = slotIdx;
            }
        }

        return bestSlot;
    }

    private bool TryActivateOncePerTurnPassives(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return false;
        var state = engine.State;

        for (int s = 0; s < state.Slots.Count; s++)
        {
            var slot = state.Slots[s];
            if (slot.IsEmpty) continue;

            var myChars = new List<DuelCard>(slot.GetCharacters(Player));
            for (int i = 0; i < myChars.Count; i++)
            {
                var ch = myChars[i];
                if (HasCardPassiveOrEffect(ch, "passive_once_turn_if_fewer_won_terrains_score") &&
                    slot.IsActive(state.TurnNumber) &&
                    state.GetScore(Player) < state.GetScore(Player.Opponent()) &&
                    slot.GetForce(Player, state) > slot.GetForce(Player.Opponent(), state))
                {
                    if (engine.ActivateCardPassive(Player, ch.Id, s).Success) return true;
                }

                if (HasCardPassiveOrEffect(ch, "passive_once_per_turn_add_bottom_deck_to_hand") &&
                    state.GetDeck(Player).Count > 0)
                {
                    if (engine.ActivateCardPassive(Player, ch.Id, s).Success) return true;
                }

                if (HasCardPassiveOrEffect(ch, "passive_once_per_turn_if_no_facedown_discard_to_bottom_deck") &&
                    state.GetDiscard(Player).Count > 0)
                {
                    if (engine.ActivateCardPassive(Player, ch.Id, s).Success) return true;
                }

                if (HasCardPassiveOrEffect(ch, "passive_once_turn_if_alone_discard_to_bottom_deck") &&
                    state.GetDiscard(Player).Count > 0)
                {
                    if (engine.ActivateCardPassive(Player, ch.Id, s).Success) return true;
                }

                if (HasCardPassiveOrEffect(ch, "duo_flantier") &&
                    HasOpponentCharacterWithMaxForce(state, Player.Opponent(), 2))
                {
                    if (engine.ActivateCardPassive(Player, ch.Id, s).Success) return true;
                }
            }
        }

        return false;
    }

    private static bool HasCardPassiveOrEffect(DuelCard card, string effectId)
    {
        for (int i = 0; i < card.PassiveEffectIds.Count; i++)
        {
            if (string.Equals(card.PassiveEffectIds[i], effectId, StringComparison.Ordinal)) return true;
        }
        for (int i = 0; i < card.EffectIds.Count; i++)
        {
            if (string.Equals(card.EffectIds[i], effectId, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static bool HasOpponentCharacterWithMaxForce(DuelState state, PlayerId opponent, int maxForce)
    {
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var chars = state.Slots[s].GetCharacters(opponent);
            for (int i = 0; i < chars.Count; i++)
            {
                if (chars[i].Force <= maxForce) return true;
            }
        }
        return false;
    }
}
