#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// Primitives d'effets IEffect pour les saisons Origins (S1) et Campus (S2).
/// C# pur sans aucune dépendance Unity ou Harmony.
/// </summary>
public static class EffectsS1S2
{
    public static void RegisterAll(EffectRegistry registry)
    {
        if (registry == null) throw new ArgumentNullException(nameof(registry));

        // 1. Défausses sur plateau
        for (int f = 1; f <= 6; f++)
        {
            registry.Register(new BoardDiscardCharacterEffect($"discard_opp_char_force{f}", TargetSide.Opponent, f));
        }
        registry.Register(new BoardDiscardCharacterEffect("discard_opp_char_any", TargetSide.Opponent, int.MaxValue));
        registry.Register(new BoardDiscardCharacterEffect("discard_any_char_force2", TargetSide.Any, 2));
        registry.Register(new BoardDiscardCharacterEffect("discard_any_char_any", TargetSide.Any, int.MaxValue));
        registry.Register(new BoardDiscardCharacterEffect("discard_own_char_any", TargetSide.Own, int.MaxValue));
        registry.Register(new DiscardBothSidesCharEffect("discard_both_sides_1_char"));

        // 2. Bounce (renvoi en main)
        registry.Register(new BoardBounceCharacterEffect("bounce_own_char_1", TargetSide.Own, drawAfterBounce: false));
        registry.Register(new BoardBounceCharacterEffect("bounce_opp_char_1", TargetSide.Opponent, drawAfterBounce: false));
        registry.Register(new BounceBothSidesCharEffect("bounce_both_char_1"));
        registry.Register(new BoardBounceCharacterEffect("bounce_own_char_draw_1", TargetSide.Own, drawAfterBounce: true));
        registry.Register(new BounceLastOpponentCharEffect("bounce_last_opp_char"));

        // 3. Échange main / plateau
        registry.Register(new SwapSelfWithHandCharEffect("swap_self_with_hand_char"));
        registry.Register(new SwapTwoCharsInPlayEffect("swap_two_chars_in_play"));

        // 4. Récupération & manipulation Défausse
        registry.Register(new TakeTopDiscardEffect("take_top_discard_1", 1));
        registry.Register(new TakeTopDiscardEffect("take_top_discard_2", 2));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "take_discard_terrain_1",
            "Choisissez un Terrain en défausse",
            c => c.Kind == CardKind.Terrain,
            preferHighestForce: false));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "take_discard_char_1",
            "Choisissez un Personnage en défausse",
            c => c.Kind == CardKind.Character,
            preferHighestForce: true));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "take_discard_char_force_le_2",
            "Choisissez un Personnage (Force <= 2) en défausse",
            c => c.Kind == CardKind.Character && c.Force <= 2,
            preferHighestForce: true));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "take_discard_cost_le_1",
            "Choisissez une carte (Coût <= 1) en défausse",
            c => EffectEngineBridge.GetCardCost(c) <= 1,
            preferHighestForce: true));

        for (int n = 1; n <= 3; n++)
        {
            registry.Register(new ReturnTopDiscardToTopDeckEffect($"return_top_discard_to_deck_{n}", n));
        }
        for (int n = 1; n <= 5; n++)
        {
            registry.Register(new ReturnTopDiscardToBottomDeckEffect($"return_discard_to_bottom_deck_{n}", n));
        }

        registry.Register(new DiscardAllHandEffect("discard_hand_all"));
        registry.Register(new DiscardOpponentHandEffect("opp_discard_hand_1", 1));
        registry.Register(new DiscardOpponentHandEffect("opp_discard_hand_2", 2));
        registry.Register(new DiscardBothPlayersEffect("both_discard_hand_1", 1));
        for (int n = 1; n <= 3; n++)
        {
            registry.Register(new BothMillEffect($"both_mill_{n}", n));
        }
        registry.Register(new OpponentDrawEffect("opp_draw_1", 1));
        registry.Register(new OpponentDrawEffect("opp_draw_2", 2));

        // 5. Deck / Recherche / Révélation / Pioche conditionnelle
        for (int n = 1; n <= 3; n++)
        {
            registry.Register(new LookTopDeckEffect($"look_top_deck_{n}", n));
        }
        registry.Register(new LookOpponentTopDeckEffect("look_opp_top_deck_1", 1));
        registry.Register(new LookOpponentTopDeckEffect("look_opp_top_deck_3", 3));
        registry.Register(new LookOpponentHandEffect("look_opp_hand"));

        registry.Register(new SearchFilteredDeckEffect(
            "search_deck_terrain_1",
            "Cherchez un Terrain dans votre deck",
            c => c.Kind == CardKind.Terrain,
            preferHighestForce: false));
        registry.Register(new SearchFilteredDeckEffect(
            "search_deck_char_1",
            "Cherchez un Personnage dans votre deck",
            c => c.Kind == CardKind.Character,
            preferHighestForce: true));
        registry.Register(new SearchFilteredDeckEffect(
            "search_deck_char_no_gems",
            "Cherchez un Personnage sans gemme dans votre deck",
            c => c.Kind == CardKind.Character && c.Gems == GemMask.None,
            preferHighestForce: true));
        registry.Register(new SearchFilteredDeckEffect(
            "search_deck_any_1",
            "Cherchez une carte dans votre deck",
            _ => true,
            preferHighestForce: true));
        registry.Register(new SearchFilteredDeckEffect(
            "search_deck_laink_or_terracid",
            "Cherchez un Laink ou Terracid dans votre deck",
            c => string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase)
              || string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase),
            preferHighestForce: true));

        registry.Register(new DrawUntilCardsEffect("draw_until_hand_5", 5));
        registry.Register(new DrawUntilCardsEffect("draw_until_hand_6", 6));
        registry.Register(new DrawUntilCardsEffect("draw_until_hand_7", 7));
        registry.Register(new DrawUntilCardsEffect("win_draw_until_5", 5));

        registry.Register(new DrawPerOppCharOnSlotEffect("draw_1_per_opp_char_on_slot"));
        registry.Register(new DrawBothPlayersEffect("draw_each_opp_draws_1", 1));

        for (int n = 1; n <= 10; n++)
        {
            registry.Register(new DrawCardsEffect($"draw_{n}", n));
            registry.Register(new MillOpponentEffect($"mill_{n}", n));
        }
        for (int n = 1; n <= 6; n++)
        {
            registry.Register(new SelfMillEffect($"self_mill_{n}", n));
        }

        // 6. Score & Modificateurs de tour
        registry.Register(new ScoreCurrentTerrainEffect("score_current_terrain"));
        registry.Register(new ScoreAnyActiveTerrainEffect("score_any_terrain"));
        registry.Register(new SetTurnFlagEffect("double_next_win_effect", TurnFlagKind.DoubleNextWin));
        registry.Register(new SetTurnFlagEffect("double_next_lose_effect", TurnFlagKind.DoubleNextLose));
        registry.Register(new SetTurnFlagEffect("free_chars_this_turn", TurnFlagKind.FreeCharsThisTurn));
        registry.Register(new ScoreAnyActiveTerrainEffect("win_score_extra_point_if_possible"));
        registry.Register(new OpponentDrawEffect("lose_opp_draw_1", 1));
        registry.Register(new DiscardAllHandEffect("lose_discard_hand_all"));

        // 7. Bannissement & Mise de côté (Campus S2)
        for (int n = 1; n <= 3; n++)
        {
            registry.Register(new SetAsideTopDeckEffect($"set_aside_top_{n}", n));
            registry.Register(new BanishDiscardEffect($"banish_own_discard_{n}", n, targetOpponent: false));
        }
        registry.Register(new TakeSetAsideCardsEffect("take_setaside_all"));
        registry.Register(new BanishSetAsideEffect("banish_setaside_all"));

        for (int n = 1; n <= 5; n++)
        {
            registry.Register(new BanishDiscardEffect($"banish_opp_discard_{n}", n, targetOpponent: true));
            registry.Register(new BanishDeckEffect($"banish_top_opp_deck_{n}", n, targetOpponent: true));
            registry.Register(new BanishDeckEffect($"banish_top_deck_{n}", n, targetOpponent: false));
        }

        registry.Register(new BoardBanishCharacterEffect("banish_opp_char_any", TargetSide.Opponent, int.MaxValue));
        registry.Register(new BoardBanishCharacterEffect("banish_opp_char_force2", TargetSide.Opponent, 2));
        registry.Register(new BoardBanishCharacterEffect("banish_own_char_any", TargetSide.Own, int.MaxValue));
        registry.Register(new BoardBanishCharacterEffect("banish_any_char_any", TargetSide.Any, int.MaxValue));

        // 8. Boosts S1/S2 échelle 1..10 & effets additionnels Campus demandés par map-s2
        for (int n = 1; n <= 10; n++)
        {
            registry.Register(new BoostSelfEffect($"boost_self_{n}", n));
        }
        registry.Register(new MarkerEffect("boost_laink_1"));
        registry.Register(new MarkerEffect("boost_terracid_1"));
        registry.Register(new MarkerEffect("boost_laink_terracid_3"));
        registry.Register(new MarkerEffect("terrain_combo_boost_1"));

        registry.Register(new BothTakeDiscardUntilHandEffect("both_take_discard_until_hand_5", 5));
        registry.Register(new BothPlayTopDiscardCharNoEffectsEffect("both_play_top_discard_char_no_effects"));
        registry.Register(new DiscardMostHandTieEffect("discard_most_hand_1_tie_2"));
        registry.Register(new ScoreAnyActiveTerrainEffect("score_any_terrain_no_sub"));
        registry.Register(new OpponentDiscardSubCardEffect("opp_discard_sub_card"));
        registry.Register(new SwapTwoTerrainsEffect("swap_two_terrains"));
        registry.Register(new BanishTerrainAndCharsEffect("banish_terrain_and_chars"));
        registry.Register(new SearchDeckByNameEffect("search_deck_name_viking_1", "Viking", 1));
        registry.Register(new SearchDeckByNameEffect("search_deck_name_chien_2", "Chien", 2));
        registry.Register(new MarkerEffect("cancel_char_combo_turn"));
        registry.Register(new StealOppCharToOwnSideEffect("steal_opp_char_while_on_slot"));
        registry.Register(new MarkerEffect("hide_hand_card_copy_force_on_score"));

        // 9. Effets additionnels Origins (S1) utilisés dans effects_s1.json
        registry.Register(new BothDrawUntilEffect("both_draw_until_5", 5));
        registry.Register(new BothPlayFreeCharEffect("both_play_free_char"));
        registry.Register(new SetTurnFlagEffect("next_char_free_this_turn", TurnFlagKind.FreeCharsThisTurn));
        registry.Register(new ScoreAllTerrainsEffect("score_all_terrains", onlyIfWinning: false));
        registry.Register(new ScoreUpToNTerrainsEffect("score_up_to_2_terrains", 2));
        registry.Register(new SearchDeckAnyMultipleEffect("search_deck_any_2", 2));
        registry.Register(new SacrificeCharMillOppByForceEffect("sacrifice_char_mill_opp_by_force"));
        registry.Register(new SelfMillCompareDiscardOppEffect("self_mill_1_discard_opp_chars_gt_force", greaterThanMilled: true));
        registry.Register(new SelfMillCompareDiscardOppEffect("self_mill_1_discard_opp_chars_lt_force", greaterThanMilled: false));
        registry.Register(new SwapBoardCharWithHandLeForceEffect("swap_board_char_with_hand_le_force"));
        registry.Register(new TakeTopDiscardIfHandCountEffect("take_top_discard_1_if_hand_3", 1, c => c == 3));
        registry.Register(new TakeTopDiscardIfHandCountEffect("take_top_discard_1_if_hand_4", 1, c => c == 4));
        registry.Register(new TakeTopDiscardIfHandCountEffect("take_top_discard_2_if_hand_3_or_5", 2, c => c == 3 || c == 5));
        registry.Register(new TakeTopDiscardIfFirstOnTerrainEffect("take_top_discard_3_if_first_on_terrain", 3));
        registry.Register(new TakeTopDiscardIfFirstOnTerrainEffect("take_top_discard_4_if_first_on_terrain", 4));
        registry.Register(new OppDiscardHandToMatchEffect("opp_discard_hand_to_match", strictBelow: false));
        registry.Register(new OppDiscardHandToMatchEffect("opp_discard_hand_below", strictBelow: true));
        registry.Register(new Look3Draw1Play1Top1Effect("look_3_draw_1_play_1_top_1"));
        registry.Register(new ClashTopDeckForceTakeEffect("clash_top_deck_force_take"));
        registry.Register(new BidNumberDiscardVsTakeEffect("bid_number_discard_vs_take"));
        registry.Register(new GuessForcePlayFreeEffect("guess_force_play_free_loop", repeatOnSuccess: true));
        registry.Register(new GuessForcePlayFreeEffect("guess_force_play_free_or_draw", repeatOnSuccess: false));
        registry.Register(new MarkerEffect("discard_self_end_of_turn"));
        registry.Register(new MarkerEffect("double_other_chars_force_turn"));
        registry.Register(new MarkerEffect("on_lose_score_turn_opp_discard_2"));
        registry.Register(new MarkerEffect("on_lose_score_turn_opp_discard_char"));

        registry.Register(new TakeFilteredDiscardCardEffect(
            "win_bounce_own_char",
            "Reprenez un personnage de votre défausse",
            c => c.Kind == CardKind.Character,
            preferHighestForce: true));
        registry.Register(new LookOpponentTopDeckEffect("win_look_opp_top_deck_5", 5));
        registry.Register(new MoveCharacterEffect("win_move_character"));
        registry.Register(new MoveCharacterEffect("win_move_own_character"));
        registry.Register(new PutHandOnTopDeckEffect("win_put_hand_on_deck"));
        registry.Register(new ReplaceOtherTerrainEffect("win_replace_other_terrain"));
        registry.Register(new ScoreAnyActiveTerrainEffect("win_score_other_terrain"));
        registry.Register(new SelfMillEffect("win_self_mill_1", 1));
        registry.Register(new SelfMillEffect("win_self_mill_2", 2));
        registry.Register(new SwapDeckDiscardAndDiscardHandEffect("win_swap_deck_discard_and_discard_hand"));
        registry.Register(new TakeDiscardPerEffigyEffect("win_take_discard_per_laink", "Laink"));
        registry.Register(new TakeDiscardPerEffigyEffect("win_take_discard_per_terracid", "Terracid"));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "win_take_discard_terrain_1",
            "Reprenez un Terrain de votre défausse",
            c => c.Kind == CardKind.Terrain,
            preferHighestForce: false));
        registry.Register(new LoseDiscardPerTerrainEffect("lose_discard_2_per_terrain", 2));
        registry.Register(new LoseDiscardUntilCountEffect("lose_discard_until_3", 3));
        registry.Register(new LoseMillPerHandCardEffect("lose_mill_per_hand_card"));
        registry.Register(new LoseMillUntilEffigyEffect("lose_mill_until_laink", "Laink"));
        registry.Register(new LoseMillUntilEffigyEffect("lose_mill_until_terracid", "Terracid"));

        // 10. Effets additionnels Campus (S2) utilisés dans effects_s2.json
        registry.Register(new RelativeForceBoardEffect("banish_any_char_force_lt_self", TargetSide.Any, banish: true, greaterThanSelf: false, maxCount: 1));
        registry.Register(new RelativeForceBoardEffect("banish_opp_chars_up_to_2_force_gt_self", TargetSide.Opponent, banish: true, greaterThanSelf: true, maxCount: 2));
        registry.Register(new BounceAllOppCharsOnSlotEffect("bounce_all_opp_chars_on_slot"));
        registry.Register(new RelativeForceBoardEffect("discard_opp_char_force_gt_self", TargetSide.Opponent, banish: false, greaterThanSelf: true, maxCount: 1));
        registry.Register(new RelativeForceBoardEffect("discard_opp_char_force_lt_self", TargetSide.Opponent, banish: false, greaterThanSelf: false, maxCount: 1));
        registry.Register(new RelativeForceBoardEffect("discard_opp_char_if_higher_force", TargetSide.Opponent, banish: false, greaterThanSelf: true, maxCount: 1));
        registry.Register(new RelativeForceBoardEffect("discard_opp_chars_up_to_2_force_lt_self", TargetSide.Opponent, banish: false, greaterThanSelf: false, maxCount: 2));
        registry.Register(new DiscardOppCharsSumForceEffect("discard_opp_chars_sum_force_3", 3));
        registry.Register(new DiscardOppCharsSumForceEffect("discard_opp_chars_sum_force_4", 4));
        registry.Register(new DrawPerCompletedComboEffect("draw_per_completed_combo"));
        registry.Register(new MillOppPerIncompleteComboEffect("mill_opp_per_incomplete_combo"));
        registry.Register(new TopDiscardForceEffect("draw_top_discard_force", millOpponent: false));
        registry.Register(new TopDiscardForceEffect("mill_opp_top_discard_force", millOpponent: true));
        registry.Register(new DrawUntilCharacterEffect("draw_until_character"));
        registry.Register(new PutOneDiscardCardOnTopDeckEffect("put_discard_card_on_top"));
        registry.Register(new ScoreAllTerrainsEffect("score_all_winning_terrains", onlyIfWinning: true));
        registry.Register(new ScoreAnyTerrainPutUnderDeckOnWinEffect("score_any_terrain_if_win_put_under_deck"));
        registry.Register(new ScoreTerrainWithoutNameEffect("score_terrain_without_marseillais", "Marseillais"));
        registry.Register(new SearchAndBanishOppDeckEffect("search_and_banish_opp_deck_1"));
        registry.Register(new ShuffleDiscardScorerToDeckEffect("shuffle_discard_scorer_to_deck"));
        registry.Register(new TakeBottomDiscardEffect("take_bottom_discard_2_if_strongest", 2));
        registry.Register(new HomonymDuoDiscardEffect("take_top_discard_2_per_homonym_duo", toBottomDeck: false));
        registry.Register(new HomonymDuoDiscardEffect("put_top_discard_2_under_deck_per_homonym_duo", toBottomDeck: true));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "win_recover_discarded_char_1",
            "Reprenez un personnage défaussé",
            c => c.Kind == CardKind.Character,
            preferHighestForce: true));
        registry.Register(new OppDiscardHandIfHighestCostEffect("opp_discard_hand_1_if_highest_cost"));
        registry.Register(new PlayOppDiscardCostLe1NoEffectsEffect("play_opp_discard_cost_le_1_no_effects"));
        registry.Register(new StealOppHandCardPlayAndBanishSelfEffect("steal_opp_hand_card_play_and_banish_self"));
        registry.Register(new GuessForceDrawPlayOrDiscardEffect("guess_force_draw_play_or_discard"));
        registry.Register(new OppChoiceTakeDiscardEffect("opp_choice_take_discard_1_or_opp_discard_1", 1));
        registry.Register(new OppChoiceTakeDiscardEffect("opp_choice_take_discard_2_or_discard_strongest", 2));
        registry.Register(new OppChoiceTakeDiscardEffect("opp_choice_take_discard_2_or_play_free_no_effects", 2));
        registry.Register(new CopyCharOnPlayEffect("copy_char_onplay_effect", costZeroOnly: false));
        registry.Register(new CopyCharOnPlayEffect("copy_cost_0_char_onplay_effect", costZeroOnly: true));
        registry.Register(new MarkerEffect("debuff_char_force_2_turn"));
        registry.Register(new MarkerEffect("set_char_force_0_turn"));
        registry.Register(new MarkerEffect("passive_add_force_to_left_terrain"));
        registry.Register(new MarkerEffect("passive_add_force_to_right_terrain"));
        registry.Register(new MarkerEffect("passive_copy_left_force"));
        registry.Register(new MarkerEffect("passive_discard_hand_banish_discard_instead"));
        registry.Register(new MarkerEffect("passive_discarded_opp_char_banished"));
        registry.Register(new MarkerEffect("passive_ignore_terrain_special_effects"));
        registry.Register(new MarkerEffect("passive_mcfly_carlito_indestructible"));
        registry.Register(new MarkerEffect("passive_on_opp_combo_mill_1"));
        registry.Register(new MarkerEffect("passive_on_opp_play_char_cycle_hand"));
        registry.Register(new MarkerEffect("passive_once_turn_search_deck_to_top"));
        registry.Register(new MarkerEffect("passive_sacrifice_cancel_sub"));

        // 11. Effets combo additionnels Origins (S1) suite maj #24
        registry.Register(new DrawByDiscardedCharForceEffect("draw_by_discarded_char_force"));
        registry.Register(new LookBottomDeckOptionalTopEffect("look_bottom_deck_optional_top"));
        registry.Register(new MulliganHandBottomDrawEffect("mulligan_hand_bottom_draw"));
        registry.Register(new PlayFreeCharSingleEffect("play_free_char"));
        registry.Register(new ReturnDiscardByNameToTopDeckEffect("return_discard_policier_to_top_deck", "Policier"));
        registry.Register(new TakeFilteredDiscardCardEffect(
            "take_discard_wcdo_1",
            "Reprenez une carte WCDO de votre défausse",
            c => c.Name.IndexOf("WCDO", StringComparison.OrdinalIgnoreCase) >= 0
              || c.Name.IndexOf("W.C.D.O", StringComparison.OrdinalIgnoreCase) >= 0,
            preferHighestForce: true));
        registry.Register(new MarkerEffect("passive_draw_on_opp_discard_ge_2"));
        registry.Register(new MarkerEffect("passive_force_per_combo_1"));
        registry.Register(new MarkerEffect("passive_own_chars_plus_1"));
        registry.Register(new SubBanishOppLastHandCardsEffect("sub_banish_on_opp_effect_draw", 2));
        registry.Register(new SubBanishOppLastHandCardsEffect("sub_banish_on_opp_take_discard", 2));
    }

    internal enum TargetSide
    {
        Own,
        Opponent,
        Any
    }

    internal enum TurnFlagKind
    {
        DoubleNextWin,
        DoubleNextLose,
        FreeCharsThisTurn
    }

    internal readonly struct SlotCharEntry
    {
        public int SlotIndex { get; }
        public PlayerId Owner { get; }
        public DuelCard Card { get; }
        public int CharIndex { get; }

        public SlotCharEntry(int slotIndex, PlayerId owner, DuelCard card, int charIndex)
        {
            SlotIndex = slotIndex;
            Owner = owner;
            Card = card;
            CharIndex = charIndex;
        }
    }

    /// <summary>
    /// Pont interne vers les propriétés publiques de DuelCard/DuelEngine.
    /// </summary>
    internal static class EffectEngineBridge
    {
        public static int GetCardCost(DuelCard card) => card.Cost;

        public static bool HasSubEffects(DuelCard card)
        {
            if (card.SubEffectIds != null && card.SubEffectIds.Count > 0)
            {
                return true;
            }
            if (card.EffectIds != null)
            {
                for (int i = 0; i < card.EffectIds.Count; i++)
                {
                    if (card.EffectIds[i].StartsWith("sub_", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        public static bool IsInteractiveUI(DuelEngine engine) => engine.RequireInteractiveCostPayment;

        public static void SetTurnPlayerFlag(DuelEngine engine, TurnFlagKind kind, PlayerId player)
        {
            switch (kind)
            {
                case TurnFlagKind.FreeCharsThisTurn:
                    engine.FreeCharactersTurnPlayer = player;
                    break;
                case TurnFlagKind.DoubleNextWin:
                    engine.DoubleNextWinEffectPlayer = player;
                    break;
                case TurnFlagKind.DoubleNextLose:
                    engine.DoubleNextLoseEffectPlayer = player.Opponent();
                    break;
            }
        }
    }

    internal static class BoardTargetHelper
    {
        public static List<SlotCharEntry> CollectBoardCharacters(
            DuelState state,
            PlayerId controller,
            TargetSide side,
            int maxForce = int.MaxValue,
            DuelCard? excludeCard = null)
        {
            var result = new List<SlotCharEntry>();
            PlayerId opponent = controller.Opponent();

            for (int slotIdx = 0; slotIdx < state.Slots.Count; slotIdx++)
            {
                var slot = state.Slots[slotIdx];
                if (slot.IsEmpty) continue;

                if (side == TargetSide.Own || side == TargetSide.Any)
                {
                    AddEligible(result, slotIdx, controller, slot.GetCharacters(controller), maxForce, excludeCard);
                }
                if (side == TargetSide.Opponent || side == TargetSide.Any)
                {
                    AddEligible(result, slotIdx, opponent, slot.GetCharacters(opponent), maxForce, excludeCard);
                }
            }

            return result;
        }

        private static void AddEligible(
            List<SlotCharEntry> result,
            int slotIdx,
            PlayerId owner,
            IReadOnlyList<DuelCard> chars,
            int maxForce,
            DuelCard? excludeCard)
        {
            for (int i = 0; i < chars.Count; i++)
            {
                var c = chars[i];
                if (excludeCard != null && ReferenceEquals(c, excludeCard)) continue;
                if (c.Force <= maxForce)
                {
                    result.Add(new SlotCharEntry(slotIdx, owner, c, i));
                }
            }
        }

        public static SlotCharEntry AutoPickBoardEntry(List<SlotCharEntry> entries, PlayerId controller, TargetSide side)
        {
            if (side == TargetSide.Any)
            {
                var oppEntries = new List<SlotCharEntry>();
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Owner != controller) oppEntries.Add(entries[i]);
                }
                if (oppEntries.Count > 0)
                {
                    return PickExtreme(oppEntries, highestForce: true);
                }
                return PickExtreme(entries, highestForce: false);
            }

            return PickExtreme(entries, highestForce: side == TargetSide.Opponent);
        }

        private static SlotCharEntry PickExtreme(List<SlotCharEntry> entries, bool highestForce)
        {
            SlotCharEntry best = entries[0];
            for (int i = 1; i < entries.Count; i++)
            {
                var cur = entries[i];
                if (highestForce ? cur.Card.Force > best.Card.Force : cur.Card.Force < best.Card.Force)
                {
                    best = cur;
                }
            }
            return best;
        }

        public static DuelCard AutoPickCard(IReadOnlyList<DuelCard> candidates, bool highestForce)
        {
            DuelCard best = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                var cur = candidates[i];
                if (highestForce ? cur.Force > best.Force : cur.Force < best.Force)
                {
                    best = cur;
                }
            }
            return best;
        }

        public static void SelectBoardEntry(
            DuelEngine engine,
            PlayerId player,
            TargetSide side,
            List<SlotCharEntry> entries,
            string title,
            string description,
            Action<SlotCharEntry> applyEntry)
        {
            if (entries.Count == 0) return;
            if (entries.Count == 1)
            {
                applyEntry(entries[0]);
                return;
            }

            if (player == PlayerId.Player1)
            {
                var cards = new List<DuelCard>(entries.Count);
                for (int i = 0; i < entries.Count; i++) cards.Add(entries[i].Card);

                bool resolved = false;
                engine.Emit(new PlayerChoiceRequiredEvent(
                    player,
                    title,
                    description,
                    cards,
                    chosen =>
                    {
                        if (resolved) return;
                        resolved = true;
                        for (int i = 0; i < entries.Count; i++)
                        {
                            if (ReferenceEquals(entries[i].Card, chosen) || entries[i].Card.Id == chosen.Id)
                            {
                                applyEntry(entries[i]);
                                return;
                            }
                        }
                        applyEntry(AutoPickBoardEntry(entries, player, side));
                    }));

                if (!resolved && !EffectEngineBridge.IsInteractiveUI(engine))
                {
                    resolved = true;
                    applyEntry(AutoPickBoardEntry(entries, player, side));
                }
            }
            else
            {
                applyEntry(AutoPickBoardEntry(entries, player, side));
            }
        }

        public static void SelectCard(
            DuelEngine engine,
            PlayerId player,
            IReadOnlyList<DuelCard> candidates,
            string title,
            string description,
            bool preferHighestForce,
            Action<DuelCard> applyCard)
        {
            if (candidates.Count == 0) return;
            if (candidates.Count == 1)
            {
                applyCard(candidates[0]);
                return;
            }

            if (player == PlayerId.Player1)
            {
                bool resolved = false;
                engine.Emit(new PlayerChoiceRequiredEvent(
                    player,
                    title,
                    description,
                    candidates,
                    chosen =>
                    {
                        if (resolved) return;
                        resolved = true;
                        applyCard(chosen);
                    }));

                if (!resolved && !EffectEngineBridge.IsInteractiveUI(engine))
                {
                    resolved = true;
                    applyCard(AutoPickCard(candidates, preferHighestForce));
                }
            }
            else
            {
                applyCard(AutoPickCard(candidates, preferHighestForce));
            }
        }

        public static void PutCardsOnTopOfDeck(DuelState state, PlayerId player, List<DuelCard> cardsToPutOnTop)
        {
            if (cardsToPutOnTop.Count == 0) return;
            var existing = new List<DuelCard>(state.GetDeck(player));
            while (state.PopTopDeckCard(player) != null) { }
            state.AddDeckCards(player, cardsToPutOnTop);
            state.AddDeckCards(player, existing);
        }
    }

    // =========================================================================
    // 1. Défausses sur plateau
    // =========================================================================

    internal sealed class BoardDiscardCharacterEffect : IEffect
    {
        public string EffectId { get; }
        private readonly TargetSide _side;
        private readonly int _maxForce;

        public BoardDiscardCharacterEffect(string effectId, TargetSide side, int maxForce)
        {
            EffectId = effectId;
            _side = side;
            _maxForce = maxForce;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var candidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, _side, _maxForce);
            if (candidates.Count == 0) return DuelActionResult.Ok();

            string title = _side == TargetSide.Own
                ? "Défaussez un de vos personnages"
                : "Défaussez un personnage en jeu";
            string desc = _maxForce < int.MaxValue
                ? $"Sélectionnez un personnage (Force <= {_maxForce}) à défausser"
                : "Sélectionnez un personnage à défausser";

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                _side,
                candidates,
                title,
                desc,
                entry =>
                {
                    var slot = state.Slots[entry.SlotIndex];
                    if (slot.RemoveCharacter(entry.Owner, entry.Card))
                    {
                        state.AddCardToDiscard(entry.Owner, entry.Card);
                    }
                });

            return DuelActionResult.Ok();
        }
    }

    internal sealed class DiscardBothSidesCharEffect : IEffect
    {
        public string EffectId { get; }
        public DiscardBothSidesCharEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var ownCandidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Own);

            void DiscardOpp()
            {
                var oppCandidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Opponent);
                BoardTargetHelper.SelectBoardEntry(
                    context.Engine,
                    context.Player,
                    TargetSide.Opponent,
                    oppCandidates,
                    "Défaussez un personnage adverse",
                    "Sélectionnez un personnage adverse à défausser",
                    oppEntry =>
                    {
                        var oppSlot = state.Slots[oppEntry.SlotIndex];
                        if (oppSlot.RemoveCharacter(oppEntry.Owner, oppEntry.Card))
                        {
                            state.AddCardToDiscard(oppEntry.Owner, oppEntry.Card);
                        }
                    });
            }

            if (ownCandidates.Count == 0)
            {
                DiscardOpp();
                return DuelActionResult.Ok();
            }

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                TargetSide.Own,
                ownCandidates,
                "Défaussez un de vos personnages",
                "Sélectionnez un personnage allié à défausser",
                ownEntry =>
                {
                    var ownSlot = state.Slots[ownEntry.SlotIndex];
                    if (ownSlot.RemoveCharacter(ownEntry.Owner, ownEntry.Card))
                    {
                        state.AddCardToDiscard(ownEntry.Owner, ownEntry.Card);
                    }
                    DiscardOpp();
                });

            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 2. Bounce (renvoi en main)
    // =========================================================================

    internal sealed class BoardBounceCharacterEffect : IEffect
    {
        public string EffectId { get; }
        private readonly TargetSide _side;
        private readonly bool _drawAfterBounce;

        public BoardBounceCharacterEffect(string effectId, TargetSide side, bool drawAfterBounce)
        {
            EffectId = effectId;
            _side = side;
            _drawAfterBounce = drawAfterBounce;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var candidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, _side);
            if (candidates.Count == 0) return DuelActionResult.Ok();

            string title = _side == TargetSide.Own
                ? "Renvoyez un de vos personnages en main"
                : "Renvoyez un personnage adverse en main";

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                _side,
                candidates,
                title,
                "Sélectionnez un personnage à renvoyer en main",
                entry =>
                {
                    var slot = state.Slots[entry.SlotIndex];
                    if (slot.RemoveCharacter(entry.Owner, entry.Card))
                    {
                        state.AddCardToHand(entry.Owner, entry.Card);
                        if (_drawAfterBounce && !state.IsGameOver)
                        {
                            context.Engine.DrawCards(context.Player, 1);
                        }
                    }
                });

            return DuelActionResult.Ok();
        }
    }

    internal sealed class BounceBothSidesCharEffect : IEffect
    {
        public string EffectId { get; }
        public BounceBothSidesCharEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var ownCandidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Own);

            void BounceOpp()
            {
                var oppCandidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Opponent);
                BoardTargetHelper.SelectBoardEntry(
                    context.Engine,
                    context.Player,
                    TargetSide.Opponent,
                    oppCandidates,
                    "Renvoyez un personnage adverse en main",
                    "Sélectionnez un personnage adverse à renvoyer en main",
                    oppEntry =>
                    {
                        var oppSlot = state.Slots[oppEntry.SlotIndex];
                        if (oppSlot.RemoveCharacter(oppEntry.Owner, oppEntry.Card))
                        {
                            state.AddCardToHand(oppEntry.Owner, oppEntry.Card);
                        }
                    });
            }

            if (ownCandidates.Count == 0)
            {
                BounceOpp();
                return DuelActionResult.Ok();
            }

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                TargetSide.Own,
                ownCandidates,
                "Renvoyez un de vos personnages en main",
                "Sélectionnez un personnage allié à renvoyer en main",
                ownEntry =>
                {
                    var ownSlot = state.Slots[ownEntry.SlotIndex];
                    if (ownSlot.RemoveCharacter(ownEntry.Owner, ownEntry.Card))
                    {
                        state.AddCardToHand(ownEntry.Owner, ownEntry.Card);
                    }
                    BounceOpp();
                });

            return DuelActionResult.Ok();
        }
    }

    internal sealed class BounceLastOpponentCharEffect : IEffect
    {
        public string EffectId { get; }
        public BounceLastOpponentCharEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opponent = context.Player.Opponent();

            if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
            {
                var slot = state.Slots[context.SlotIndex];
                var chars = slot.GetCharacters(opponent);
                if (chars.Count > 0)
                {
                    var last = chars[chars.Count - 1];
                    if (slot.RemoveCharacter(opponent, last))
                    {
                        state.AddCardToHand(opponent, last);
                    }
                    return DuelActionResult.Ok();
                }
            }

            for (int i = state.Slots.Count - 1; i >= 0; i--)
            {
                var slot = state.Slots[i];
                if (slot.IsEmpty) continue;
                var chars = slot.GetCharacters(opponent);
                if (chars.Count > 0)
                {
                    var last = chars[chars.Count - 1];
                    if (slot.RemoveCharacter(opponent, last))
                    {
                        state.AddCardToHand(opponent, last);
                    }
                    break;
                }
            }

            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 3. Échange main / plateau
    // =========================================================================

    internal sealed class SwapSelfWithHandCharEffect : IEffect
    {
        public string EffectId { get; }
        public SwapSelfWithHandCharEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var handChars = new List<DuelCard>();
            var hand = state.GetHand(context.Player);
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind == CardKind.Character) handChars.Add(hand[i]);
            }
            if (handChars.Count == 0) return DuelActionResult.Ok();

            var ownOnBoard = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Own);
            if (ownOnBoard.Count == 0) return DuelActionResult.Ok();

            SlotCharEntry? selfEntry = null;
            for (int i = 0; i < ownOnBoard.Count; i++)
            {
                if (ReferenceEquals(ownOnBoard[i].Card, context.SourceCard) || ownOnBoard[i].Card.Id == context.SourceCard.Id)
                {
                    selfEntry = ownOnBoard[i];
                    break;
                }
            }

            void PerformSwap(SlotCharEntry boardEntry)
            {
                BoardTargetHelper.SelectCard(
                    context.Engine,
                    context.Player,
                    handChars,
                    "Échangez avec un personnage de votre main",
                    $"Choisissez un personnage de votre main à échanger avec {boardEntry.Card.Name}",
                    preferHighestForce: true,
                    handCard =>
                    {
                        var slot = state.Slots[boardEntry.SlotIndex];
                        if (!slot.RemoveCharacter(boardEntry.Owner, boardEntry.Card)) return;

                        state.RemoveCardFromHand(context.Player, handCard);
                        state.AddCharacterToSlot(boardEntry.SlotIndex, boardEntry.Owner, handCard, boardEntry.CharIndex);
                        state.AddCardToHand(context.Player, boardEntry.Card);
                        context.Engine.Emit(new CharacterPlayedEvent(context.Player, boardEntry.SlotIndex, handCard));
                    });
            }

            if (selfEntry.HasValue)
            {
                PerformSwap(selfEntry.Value);
            }
            else
            {
                BoardTargetHelper.SelectBoardEntry(
                    context.Engine,
                    context.Player,
                    TargetSide.Own,
                    ownOnBoard,
                    "Choisissez le personnage en jeu à échanger",
                    "Sélectionnez un de vos personnages en jeu",
                    PerformSwap);
            }

            return DuelActionResult.Ok();
        }
    }

    internal sealed class SwapTwoCharsInPlayEffect : IEffect
    {
        public string EffectId { get; }
        public SwapTwoCharsInPlayEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var allChars = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Any, excludeCard: context.SourceCard);
            if (allChars.Count < 2)
            {
                allChars = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Any);
            }
            if (allChars.Count < 2) return DuelActionResult.Ok();

            void SwapEntries(SlotCharEntry a, SlotCharEntry b)
            {
                if (ReferenceEquals(a.Card, b.Card) && a.SlotIndex == b.SlotIndex && a.Owner == b.Owner) return;

                var slotA = state.Slots[a.SlotIndex];
                var slotB = state.Slots[b.SlotIndex];

                if (a.SlotIndex == b.SlotIndex && a.Owner == b.Owner)
                {
                    int idxA = FindCharIndex(slotA.GetCharacters(a.Owner), a.Card);
                    int idxB = FindCharIndex(slotB.GetCharacters(b.Owner), b.Card);
                    if (idxA < 0 || idxB < 0 || idxA == idxB) return;

                    if (idxA < idxB)
                    {
                        slotA.RemoveCharacter(a.Owner, b.Card);
                        slotA.RemoveCharacter(a.Owner, a.Card);
                        state.AddCharacterToSlot(a.SlotIndex, a.Owner, b.Card, idxA);
                        state.AddCharacterToSlot(a.SlotIndex, a.Owner, a.Card, idxB);
                    }
                    else
                    {
                        slotA.RemoveCharacter(a.Owner, a.Card);
                        slotA.RemoveCharacter(a.Owner, b.Card);
                        state.AddCharacterToSlot(a.SlotIndex, a.Owner, a.Card, idxB);
                        state.AddCharacterToSlot(a.SlotIndex, a.Owner, b.Card, idxA);
                    }
                    return;
                }

                int posA = FindCharIndex(slotA.GetCharacters(a.Owner), a.Card);
                int posB = FindCharIndex(slotB.GetCharacters(b.Owner), b.Card);
                if (posA < 0 || posB < 0) return;

                if (slotA.RemoveCharacter(a.Owner, a.Card) && slotB.RemoveCharacter(b.Owner, b.Card))
                {
                    state.AddCharacterToSlot(b.SlotIndex, b.Owner, a.Card, posB);
                    state.AddCharacterToSlot(a.SlotIndex, a.Owner, b.Card, posA);
                }
            }

            if (context.Player == PlayerId.Player1)
            {
                BoardTargetHelper.SelectBoardEntry(
                    context.Engine,
                    context.Player,
                    TargetSide.Own,
                    allChars,
                    "Échangez deux personnages (1/2)",
                    "Sélectionnez le premier personnage à échanger",
                    first =>
                    {
                        var remaining = new List<SlotCharEntry>(allChars.Count - 1);
                        for (int i = 0; i < allChars.Count; i++)
                        {
                            if (!ReferenceEquals(allChars[i].Card, first.Card))
                            {
                                remaining.Add(allChars[i]);
                            }
                        }
                        if (remaining.Count == 0) return;

                        BoardTargetHelper.SelectBoardEntry(
                            context.Engine,
                            context.Player,
                            TargetSide.Opponent,
                            remaining,
                            "Échangez deux personnages (2/2)",
                            $"Sélectionnez le second personnage à échanger avec {first.Card.Name}",
                            second => SwapEntries(first, second));
                    });
            }
            else
            {
                var own = new List<SlotCharEntry>();
                var opp = new List<SlotCharEntry>();
                for (int i = 0; i < allChars.Count; i++)
                {
                    if (allChars[i].Owner == context.Player) own.Add(allChars[i]);
                    else opp.Add(allChars[i]);
                }

                if (own.Count > 0 && opp.Count > 0)
                {
                    var weakestOwn = BoardTargetHelper.AutoPickBoardEntry(own, context.Player, TargetSide.Own);
                    var strongestOpp = BoardTargetHelper.AutoPickBoardEntry(opp, context.Player, TargetSide.Opponent);
                    SwapEntries(weakestOwn, strongestOpp);
                }
                else
                {
                    SwapEntries(allChars[0], allChars[1]);
                }
            }

            return DuelActionResult.Ok();
        }

        private static int FindCharIndex(IReadOnlyList<DuelCard> chars, DuelCard card)
        {
            for (int i = 0; i < chars.Count; i++)
            {
                if (ReferenceEquals(chars[i], card)) return i;
            }
            return -1;
        }
    }

    // =========================================================================
    // 4. Récupération & manipulation Défausse
    // =========================================================================

    internal sealed class TakeFilteredDiscardCardEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _title;
        private readonly Func<DuelCard, bool> _predicate;
        private readonly bool _preferHighestForce;

        public TakeFilteredDiscardCardEffect(
            string effectId,
            string title,
            Func<DuelCard, bool> predicate,
            bool preferHighestForce)
        {
            EffectId = effectId;
            _title = title;
            _predicate = predicate;
            _preferHighestForce = preferHighestForce;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            var matches = new List<DuelCard>();
            for (int i = 0; i < discard.Count; i++)
            {
                if (_predicate(discard[i])) matches.Add(discard[i]);
            }
            if (matches.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                matches,
                _title,
                "Sélectionnez une carte à reprendre en main",
                _preferHighestForce,
                chosen =>
                {
                    state.RemoveCardFromDiscard(context.Player, chosen);
                    state.AddCardToHand(context.Player, chosen);
                });

            return DuelActionResult.Ok();
        }
    }

    internal sealed class ReturnTopDiscardToTopDeckEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;

        public ReturnTopDiscardToTopDeckEffect(string effectId, int amount)
        {
            EffectId = effectId;
            _amount = amount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            int toReturn = Math.Min(_amount, discard.Count);
            if (toReturn <= 0) return DuelActionResult.Ok();

            var cards = new List<DuelCard>(toReturn);
            for (int i = 0; i < toReturn; i++)
            {
                var top = discard[discard.Count - 1];
                state.RemoveCardFromDiscard(context.Player, top);
                cards.Add(top);
            }

            BoardTargetHelper.PutCardsOnTopOfDeck(state, context.Player, cards);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ReturnTopDiscardToBottomDeckEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;

        public ReturnTopDiscardToBottomDeckEffect(string effectId, int amount)
        {
            EffectId = effectId;
            _amount = amount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            int toReturn = Math.Min(_amount, discard.Count);
            for (int i = 0; i < toReturn; i++)
            {
                var top = discard[discard.Count - 1];
                state.RemoveCardFromDiscard(context.Player, top);
                state.AddCardToBottomDeck(context.Player, top);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class DiscardAllHandEffect : IEffect
    {
        public string EffectId { get; }
        public DiscardAllHandEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var hand = context.Engine.State.GetHand(context.Player);
            if (hand.Count > 0)
            {
                context.Engine.DiscardFromHand(context.Player, hand.Count);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class BothMillEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public BothMillEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            context.Engine.MillCards(PlayerId.Player1, _amount);
            if (!context.Engine.State.IsGameOver)
            {
                context.Engine.MillCards(PlayerId.Player2, _amount);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class OpponentDrawEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public OpponentDrawEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            context.Engine.DrawCards(context.Player.Opponent(), _amount);
            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 5. Deck / Recherche / Révélation / Pioche conditionnelle
    // =========================================================================

    internal sealed class LookOpponentTopDeckEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public LookOpponentTopDeckEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            var opp = context.Player.Opponent();
            var deck = context.Engine.State.GetDeck(opp);
            int count = Math.Min(_amount, deck.Count);
            if (count > 0)
            {
                var revealed = new List<DuelCard>(count);
                for (int i = 0; i < count; i++) revealed.Add(deck[i]);
                context.Engine.Emit(new CardsRevealedEvent(context.Player, revealed));
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class LookOpponentHandEffect : IEffect
    {
        public string EffectId { get; }
        public LookOpponentHandEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var oppHand = context.Engine.State.GetHand(context.Player.Opponent());
            if (oppHand.Count > 0)
            {
                context.Engine.Emit(new CardsRevealedEvent(context.Player, new List<DuelCard>(oppHand)));
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SearchFilteredDeckEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _title;
        private readonly Func<DuelCard, bool> _predicate;
        private readonly bool _preferHighestForce;

        public SearchFilteredDeckEffect(
            string effectId,
            string title,
            Func<DuelCard, bool> predicate,
            bool preferHighestForce)
        {
            EffectId = effectId;
            _title = title;
            _predicate = predicate;
            _preferHighestForce = preferHighestForce;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var deck = state.GetDeck(context.Player);
            var matches = new List<DuelCard>();
            for (int i = 0; i < deck.Count; i++)
            {
                if (_predicate(deck[i])) matches.Add(deck[i]);
            }
            if (matches.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                matches,
                _title,
                "Sélectionnez une carte à ajouter à votre main",
                _preferHighestForce,
                chosen =>
                {
                    if (state.RemoveCardFromDeck(context.Player, chosen))
                    {
                        state.AddCardToHand(context.Player, chosen);
                        context.Engine.CheckDeckOutCondition(context.Player);
                    }
                });

            return DuelActionResult.Ok();
        }
    }

    internal sealed class DrawPerOppCharOnSlotEffect : IEffect
    {
        public string EffectId { get; }
        public DrawPerOppCharOnSlotEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (context.SlotIndex < 0 || context.SlotIndex >= state.Slots.Count) return DuelActionResult.Ok();
            int oppCount = state.Slots[context.SlotIndex].GetCharacters(context.Player.Opponent()).Count;
            if (oppCount > 0)
            {
                context.Engine.DrawCards(context.Player, oppCount);
            }
            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 6. Score & Modificateurs de tour
    // =========================================================================

    internal sealed class ScoreCurrentTerrainEffect : IEffect
    {
        public string EffectId { get; }
        public ScoreCurrentTerrainEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (state.IsGameOver) return DuelActionResult.Ok();
            if (context.SlotIndex < 0 || context.SlotIndex >= state.Slots.Count) return DuelActionResult.Ok();

            var slot = state.Slots[context.SlotIndex];
            if (!slot.IsEmpty && slot.IsActive(state.TurnNumber))
            {
                context.Engine.ResolveScore(context.SlotIndex, context.Player);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ScoreAnyActiveTerrainEffect : IEffect
    {
        public string EffectId { get; }
        public ScoreAnyActiveTerrainEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (state.IsGameOver) return DuelActionResult.Ok();

            var activeSlots = new List<int>();
            for (int i = 0; i < state.Slots.Count; i++)
            {
                var s = state.Slots[i];
                if (!s.IsEmpty && s.IsActive(state.TurnNumber))
                {
                    activeSlots.Add(i);
                }
            }

            if (activeSlots.Count == 0) return DuelActionResult.Ok();
            if (activeSlots.Count == 1)
            {
                context.Engine.ResolveScore(activeSlots[0], context.Player);
                return DuelActionResult.Ok();
            }

            int PickBestSlot()
            {
                int bestSlot = activeSlots[0];
                int bestAdv = int.MinValue;
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    var s = state.Slots[activeSlots[i]];
                    int adv = s.GetForce(context.Player) - s.GetForce(context.Player.Opponent());
                    if (adv > bestAdv)
                    {
                        bestAdv = adv;
                        bestSlot = activeSlots[i];
                    }
                }
                return bestSlot;
            }

            if (context.Player == PlayerId.Player1)
            {
                bool resolved = false;
                context.Engine.Emit(new PlayerSlotChoiceRequiredEvent(
                    context.Player,
                    "Choisissez un Terrain à scorer",
                    "Sélectionnez un Terrain actif à scorer",
                    slotIdx =>
                    {
                        if (resolved) return;
                        if (slotIdx >= 0 && slotIdx < state.Slots.Count &&
                            !state.Slots[slotIdx].IsEmpty &&
                            state.Slots[slotIdx].IsActive(state.TurnNumber))
                        {
                            resolved = true;
                            context.Engine.ResolveScore(slotIdx, context.Player);
                        }
                    }));

                if (!resolved && !EffectEngineBridge.IsInteractiveUI(context.Engine))
                {
                    resolved = true;
                    context.Engine.ResolveScore(PickBestSlot(), context.Player);
                }
            }
            else
            {
                context.Engine.ResolveScore(PickBestSlot(), context.Player);
            }

            return DuelActionResult.Ok();
        }
    }

    internal sealed class SetTurnFlagEffect : IEffect
    {
        public string EffectId { get; }
        private readonly TurnFlagKind _flag;

        public SetTurnFlagEffect(string effectId, TurnFlagKind flag)
        {
            EffectId = effectId;
            _flag = flag;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            EffectEngineBridge.SetTurnPlayerFlag(context.Engine, _flag, context.Player);
            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 7. Bannissement & Mise de côté (Campus S2)
    // =========================================================================

    internal sealed class SetAsideTopDeckEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;

        public SetAsideTopDeckEffect(string effectId, int amount)
        {
            EffectId = effectId;
            _amount = amount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            context.Engine.SetAsideCardsFromDeck(context.Player, _amount);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class BoardBanishCharacterEffect : IEffect
    {
        public string EffectId { get; }
        private readonly TargetSide _side;
        private readonly int _maxForce;

        public BoardBanishCharacterEffect(string effectId, TargetSide side, int maxForce)
        {
            EffectId = effectId;
            _side = side;
            _maxForce = maxForce;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var candidates = BoardTargetHelper.CollectBoardCharacters(state, context.Player, _side, _maxForce);
            if (candidates.Count == 0) return DuelActionResult.Ok();

            string desc = _maxForce < int.MaxValue
                ? $"Sélectionnez un personnage (Force <= {_maxForce}) à bannir"
                : "Sélectionnez un personnage à bannir";

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                _side,
                candidates,
                "Bannissez un personnage en jeu",
                desc,
                entry =>
                {
                    var slot = state.Slots[entry.SlotIndex];
                    if (slot.RemoveCharacter(entry.Owner, entry.Card))
                    {
                        state.AddCardToBanish(entry.Owner, entry.Card);
                        context.Engine.Emit(new CardsBanishedEvent(entry.Owner, new[] { entry.Card }));
                    }
                });

            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 8. Effets additionnels Campus S2 (demandés par map-s2)
    // =========================================================================

    internal sealed class BothTakeDiscardUntilHandEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _targetCount;

        public BothTakeDiscardUntilHandEffect(string effectId, int targetCount)
        {
            EffectId = effectId;
            _targetCount = targetCount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            TakeForPlayer(state, PlayerId.Player1);
            TakeForPlayer(state, PlayerId.Player2);
            return DuelActionResult.Ok();
        }

        private void TakeForPlayer(DuelState state, PlayerId player)
        {
            var hand = state.GetHand(player);
            var discard = state.GetDiscard(player);
            while (hand.Count < _targetCount && discard.Count > 0)
            {
                var top = discard[discard.Count - 1];
                state.RemoveCardFromDiscard(player, top);
                state.AddCardToHand(player, top);
            }
        }
    }

    internal sealed class BothPlayTopDiscardCharNoEffectsEffect : IEffect
    {
        public string EffectId { get; }
        public BothPlayTopDiscardCharNoEffectsEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int targetSlot = -1;
            if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
            {
                targetSlot = context.SlotIndex;
            }
            else
            {
                for (int i = 0; i < state.Slots.Count; i++)
                {
                    if (!state.Slots[i].IsEmpty) { targetSlot = i; break; }
                }
            }
            if (targetSlot < 0) return DuelActionResult.Ok();

            PlayTopChar(context.Engine, PlayerId.Player1, targetSlot);
            PlayTopChar(context.Engine, PlayerId.Player2, targetSlot);
            return DuelActionResult.Ok();
        }

        private static void PlayTopChar(DuelEngine engine, PlayerId player, int slotIndex)
        {
            var discard = engine.State.GetDiscard(player);
            for (int i = discard.Count - 1; i >= 0; i--)
            {
                if (discard[i].Kind == CardKind.Character)
                {
                    var card = discard[i];
                    engine.State.RemoveCardFromDiscard(player, card);
                    engine.State.AddCharacterToSlot(slotIndex, player, card);
                    engine.Emit(new CharacterPlayedEvent(player, slotIndex, card));
                    break;
                }
            }
        }
    }

    internal sealed class DiscardMostHandTieEffect : IEffect
    {
        public string EffectId { get; }
        public DiscardMostHandTieEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            int p1Count = context.Engine.State.HandP1.Count;
            int p2Count = context.Engine.State.HandP2.Count;
            if (p1Count > p2Count)
            {
                context.Engine.DiscardFromHand(PlayerId.Player1, 1);
            }
            else if (p2Count > p1Count)
            {
                context.Engine.DiscardFromHand(PlayerId.Player2, 1);
            }
            else if (p1Count > 0)
            {
                context.Engine.DiscardFromHand(PlayerId.Player1, 2);
                if (!context.Engine.State.IsGameOver)
                {
                    context.Engine.DiscardFromHand(PlayerId.Player2, 2);
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class OpponentDiscardSubCardEffect : IEffect
    {
        public string EffectId { get; }
        public OpponentDiscardSubCardEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var opp = context.Player.Opponent();
            var oppHand = context.Engine.State.GetHand(opp);
            for (int i = 0; i < oppHand.Count; i++)
            {
                if (EffectEngineBridge.HasSubEffects(oppHand[i]))
                {
                    var card = oppHand[i];
                    context.Engine.State.RemoveCardFromHand(opp, card);
                    context.Engine.State.AddCardToDiscard(opp, card);
                    context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { card }));
                    context.Engine.CheckDeckOutCondition(opp);
                    break;
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SwapTwoTerrainsEffect : IEffect
    {
        public string EffectId { get; }
        public SwapTwoTerrainsEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var occupied = new List<int>();
            for (int i = 0; i < state.Slots.Count; i++)
            {
                if (!state.Slots[i].IsEmpty) occupied.Add(i);
            }
            if (occupied.Count < 2) return DuelActionResult.Ok();

            int a = occupied[0];
            int b = occupied[1];
            var slotA = state.Slots[a];
            var slotB = state.Slots[b];

            var cardA = slotA.Card;
            var turnA = slotA.PlacedOnTurn;
            var byA = slotA.PlacedBy;
            var charsP1A = new List<DuelCard>(slotA.CharactersP1);
            var charsP2A = new List<DuelCard>(slotA.CharactersP2);

            var cardB = slotB.Card;
            var turnB = slotB.PlacedOnTurn;
            var byB = slotB.PlacedBy;
            var charsP1B = new List<DuelCard>(slotB.CharactersP1);
            var charsP2B = new List<DuelCard>(slotB.CharactersP2);

            state.SetSlot(a, cardB, turnB, byB);
            foreach (var c in charsP1A) state.AddCharacterToSlot(a, PlayerId.Player1, c);
            foreach (var c in charsP2A) state.AddCharacterToSlot(a, PlayerId.Player2, c);

            state.SetSlot(b, cardA, turnA, byA);
            foreach (var c in charsP1B) state.AddCharacterToSlot(b, PlayerId.Player1, c);
            foreach (var c in charsP2B) state.AddCharacterToSlot(b, PlayerId.Player2, c);

            return DuelActionResult.Ok();
        }
    }

    internal sealed class BanishTerrainAndCharsEffect : IEffect
    {
        public string EffectId { get; }
        public BanishTerrainAndCharsEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var occupied = new List<int>();
            for (int i = 0; i < state.Slots.Count; i++)
            {
                if (!state.Slots[i].IsEmpty) occupied.Add(i);
            }
            if (occupied.Count == 0) return DuelActionResult.Ok();

            void BanishSlot(int slotIdx)
            {
                if (slotIdx < 0 || slotIdx >= state.Slots.Count) return;
                var slot = state.Slots[slotIdx];
                if (slot.IsEmpty || slot.Card == null) return;

                var p1 = new List<DuelCard>(slot.CharactersP1);
                var p2 = new List<DuelCard>(slot.CharactersP2);
                var terrain = slot.Card;
                var owner = slot.PlacedBy ?? context.Player;

                state.ClearSlot(slotIdx);

                state.AddCardToBanish(owner, terrain);
                context.Engine.Emit(new CardsBanishedEvent(owner, new[] { terrain }));
                if (p1.Count > 0)
                {
                    state.AddCardsToBanish(PlayerId.Player1, p1);
                    context.Engine.Emit(new CardsBanishedEvent(PlayerId.Player1, p1));
                }
                if (p2.Count > 0)
                {
                    state.AddCardsToBanish(PlayerId.Player2, p2);
                    context.Engine.Emit(new CardsBanishedEvent(PlayerId.Player2, p2));
                }
            }

            if (occupied.Count == 1)
            {
                BanishSlot(occupied[0]);
                return DuelActionResult.Ok();
            }

            if (context.Player == PlayerId.Player1)
            {
                bool resolved = false;
                context.Engine.Emit(new PlayerSlotChoiceRequiredEvent(
                    context.Player,
                    "Bannissez un Terrain",
                    "Sélectionnez un Terrain et tous ses personnages à bannir",
                    slotIdx =>
                    {
                        if (resolved) return;
                        if (slotIdx >= 0 && slotIdx < state.Slots.Count && !state.Slots[slotIdx].IsEmpty)
                        {
                            resolved = true;
                            BanishSlot(slotIdx);
                        }
                    }));

                if (!resolved && !EffectEngineBridge.IsInteractiveUI(context.Engine))
                {
                    resolved = true;
                    BanishSlot(occupied[0]);
                }
            }
            else
            {
                int bestSlot = occupied[0];
                int bestScore = int.MinValue;
                for (int i = 0; i < occupied.Count; i++)
                {
                    var s = state.Slots[occupied[i]];
                    int oppAdvantage = s.GetForce(context.Player.Opponent()) - s.GetForce(context.Player);
                    if (oppAdvantage > bestScore)
                    {
                        bestScore = oppAdvantage;
                        bestSlot = occupied[i];
                    }
                }
                BanishSlot(bestSlot);
            }

            return DuelActionResult.Ok();
        }
    }

    internal sealed class SearchDeckByNameEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _nameFragment;
        private readonly int _maxCount;

        public SearchDeckByNameEffect(string effectId, string nameFragment, int maxCount)
        {
            EffectId = effectId;
            _nameFragment = nameFragment;
            _maxCount = maxCount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var deck = state.GetDeck(context.Player);
            var matches = new List<DuelCard>();
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Name.IndexOf(_nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(deck[i]);
                }
            }

            int taken = 0;
            for (int i = 0; i < matches.Count && taken < _maxCount; i++)
            {
                if (state.RemoveCardFromDeck(context.Player, matches[i]))
                {
                    state.AddCardToHand(context.Player, matches[i]);
                    taken++;
                }
            }
            if (taken > 0)
            {
                context.Engine.CheckDeckOutCondition(context.Player);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class StealOppCharToOwnSideEffect : IEffect
    {
        public string EffectId { get; }
        public StealOppCharToOwnSideEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (context.SlotIndex < 0 || context.SlotIndex >= state.Slots.Count) return DuelActionResult.Ok();

            var slot = state.Slots[context.SlotIndex];
            var opp = context.Player.Opponent();
            var oppChars = slot.GetCharacters(opp);
            if (oppChars.Count == 0) return DuelActionResult.Ok();

            var candidates = new List<SlotCharEntry>(oppChars.Count);
            for (int i = 0; i < oppChars.Count; i++)
            {
                candidates.Add(new SlotCharEntry(context.SlotIndex, opp, oppChars[i], i));
            }

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                TargetSide.Opponent,
                candidates,
                "Volez un personnage adverse",
                "Sélectionnez un personnage adverse à déplacer de votre côté",
                entry =>
                {
                    if (slot.RemoveCharacter(opp, entry.Card))
                    {
                        state.AddCharacterToSlot(context.SlotIndex, context.Player, entry.Card);
                    }
                });

            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 9. Classes d'effets additionnels Origins (S1)
    // =========================================================================

    internal sealed class BothDrawUntilEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _target;
        public BothDrawUntilEffect(string effectId, int target) { EffectId = effectId; _target = target; }

        public DuelActionResult Execute(EffectContext context)
        {
            DrawUpTo(context.Engine, PlayerId.Player1, _target);
            if (!context.Engine.State.IsGameOver)
            {
                DrawUpTo(context.Engine, PlayerId.Player2, _target);
            }
            return DuelActionResult.Ok();
        }

        private static void DrawUpTo(DuelEngine engine, PlayerId player, int target)
        {
            int missing = target - engine.State.GetHand(player).Count;
            if (missing > 0) engine.DrawCards(player, missing);
        }
    }

    internal sealed class BothPlayFreeCharEffect : IEffect
    {
        public string EffectId { get; }
        public BothPlayFreeCharEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            PlayOneFree(context.Engine, context.Player, context.SlotIndex);
            if (!context.Engine.State.IsGameOver)
            {
                PlayOneFree(context.Engine, context.Player.Opponent(), context.SlotIndex);
            }
            return DuelActionResult.Ok();
        }

        internal static void PlayOneFree(DuelEngine engine, PlayerId player, int preferredSlot)
        {
            var state = engine.State;
            int slotIdx = (preferredSlot >= 0 && preferredSlot < state.Slots.Count && !state.Slots[preferredSlot].IsEmpty)
                ? preferredSlot
                : FindFirstOccupiedSlot(state);
            if (slotIdx < 0) return;

            var hand = state.GetHand(player);
            var chars = new List<DuelCard>();
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind == CardKind.Character && !hand[i].IsScoreur) chars.Add(hand[i]);
            }
            if (chars.Count == 0)
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    if (hand[i].Kind == CardKind.Character) chars.Add(hand[i]);
                }
            }
            if (chars.Count == 0) return;

            BoardTargetHelper.SelectCard(
                engine,
                player,
                chars,
                "Jouez un personnage gratuitement",
                "Sélectionnez un personnage de votre main à poser gratuitement",
                preferHighestForce: true,
                chosen =>
                {
                    if (state.RemoveCardFromHand(player, chosen))
                    {
                        state.AddCharacterToSlot(slotIdx, player, chosen);
                        engine.Emit(new CharacterPlayedEvent(player, slotIdx, chosen));
                        engine.CheckDeckOutCondition(player);
                    }
                });
        }

        internal static int FindFirstOccupiedSlot(DuelState state)
        {
            for (int i = 0; i < state.Slots.Count; i++)
            {
                if (!state.Slots[i].IsEmpty) return i;
            }
            return -1;
        }
    }

    internal sealed class ScoreAllTerrainsEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _onlyIfWinning;
        public ScoreAllTerrainsEffect(string effectId, bool onlyIfWinning)
        {
            EffectId = effectId;
            _onlyIfWinning = onlyIfWinning;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            for (int i = 0; i < state.Slots.Count; i++)
            {
                if (state.IsGameOver) break;
                var slot = state.Slots[i];
                if (slot.IsEmpty || !slot.IsActive(state.TurnNumber)) continue;
                if (_onlyIfWinning && slot.GetForce(context.Player) <= slot.GetForce(context.Player.Opponent()))
                {
                    continue;
                }
                context.Engine.ResolveScore(i, context.Player);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ScoreUpToNTerrainsEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _maxCount;
        public ScoreUpToNTerrainsEffect(string effectId, int maxCount) { EffectId = effectId; _maxCount = maxCount; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int scored = 0;
            var candidates = new List<int>();
            for (int i = 0; i < state.Slots.Count; i++)
            {
                var s = state.Slots[i];
                if (!s.IsEmpty && s.IsActive(state.TurnNumber)) candidates.Add(i);
            }
            candidates.Sort((a, b) =>
            {
                int advA = state.Slots[a].GetForce(context.Player) - state.Slots[a].GetForce(context.Player.Opponent());
                int advB = state.Slots[b].GetForce(context.Player) - state.Slots[b].GetForce(context.Player.Opponent());
                return advB.CompareTo(advA);
            });
            for (int i = 0; i < candidates.Count && scored < _maxCount && !state.IsGameOver; i++)
            {
                var s = state.Slots[candidates[i]];
                if (!s.IsEmpty && s.IsActive(state.TurnNumber))
                {
                    context.Engine.ResolveScore(candidates[i], context.Player);
                    scored++;
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SearchDeckAnyMultipleEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public SearchDeckAnyMultipleEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            for (int k = 0; k < _amount; k++)
            {
                var deck = state.GetDeck(context.Player);
                if (deck.Count == 0) break;
                var picked = BoardTargetHelper.AutoPickCard(deck, highestForce: true);
                if (state.RemoveCardFromDeck(context.Player, picked))
                {
                    state.AddCardToHand(context.Player, picked);
                }
            }
            context.Engine.CheckDeckOutCondition(context.Player);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SacrificeCharMillOppByForceEffect : IEffect
    {
        public string EffectId { get; }
        public SacrificeCharMillOppByForceEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var own = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Own);
            if (own.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                TargetSide.Own,
                own,
                "Sacrifiez un personnage",
                "Sélectionnez un de vos personnages à défausser pour meuler sa Force",
                entry =>
                {
                    var slot = state.Slots[entry.SlotIndex];
                    if (slot.RemoveCharacter(entry.Owner, entry.Card))
                    {
                        state.AddCardToDiscard(entry.Owner, entry.Card);
                        if (entry.Card.Force > 0 && !state.IsGameOver)
                        {
                            context.Engine.MillCards(context.Player.Opponent(), entry.Card.Force);
                        }
                    }
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SelfMillCompareDiscardOppEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _greaterThanMilled;
        public SelfMillCompareDiscardOppEffect(string effectId, bool greaterThanMilled)
        {
            EffectId = effectId;
            _greaterThanMilled = greaterThanMilled;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var milled = context.Engine.MillCards(context.Player, 1);
            if (milled.Count == 0 || context.Engine.State.IsGameOver) return DuelActionResult.Ok();
            int refForce = milled[0].Force;
            var state = context.Engine.State;
            var opp = context.Player.Opponent();

            for (int i = 0; i < state.Slots.Count; i++)
            {
                var slot = state.Slots[i];
                if (slot.IsEmpty) continue;
                var toDiscard = new List<DuelCard>();
                var oppChars = slot.GetCharacters(opp);
                for (int j = 0; j < oppChars.Count; j++)
                {
                    bool match = _greaterThanMilled ? oppChars[j].Force > refForce : oppChars[j].Force < refForce;
                    if (match) toDiscard.Add(oppChars[j]);
                }
                for (int j = 0; j < toDiscard.Count; j++)
                {
                    if (slot.RemoveCharacter(opp, toDiscard[j]))
                    {
                        state.AddCardToDiscard(opp, toDiscard[j]);
                    }
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SwapBoardCharWithHandLeForceEffect : IEffect
    {
        public string EffectId { get; }
        public SwapBoardCharWithHandLeForceEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var own = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Own);
            if (own.Count == 0) return DuelActionResult.Ok();

            var hand = state.GetHand(context.Player);
            var validPairs = new List<(SlotCharEntry board, DuelCard handCard)>();
            for (int i = 0; i < own.Count; i++)
            {
                for (int j = 0; j < hand.Count; j++)
                {
                    if (hand[j].Kind == CardKind.Character && hand[j].Force <= own[i].Card.Force)
                    {
                        validPairs.Add((own[i], hand[j]));
                    }
                }
            }
            if (validPairs.Count == 0) return DuelActionResult.Ok();

            var (board, handCard) = validPairs[0];
            if (state.Slots[board.SlotIndex].RemoveCharacter(board.Owner, board.Card))
            {
                state.RemoveCardFromHand(context.Player, handCard);
                state.AddCharacterToSlot(board.SlotIndex, board.Owner, handCard, board.CharIndex);
                state.AddCardToHand(context.Player, board.Card);
                context.Engine.Emit(new CharacterPlayedEvent(context.Player, board.SlotIndex, handCard));
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class TakeTopDiscardIfHandCountEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        private readonly Func<int, bool> _condition;

        public TakeTopDiscardIfHandCountEffect(string effectId, int amount, Func<int, bool> condition)
        {
            EffectId = effectId;
            _amount = amount;
            _condition = condition;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (!_condition(state.GetHand(context.Player).Count)) return DuelActionResult.Ok();
            new TakeTopDiscardEffect(EffectId, _amount).Execute(context);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class TakeTopDiscardIfFirstOnTerrainEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public TakeTopDiscardIfFirstOnTerrainEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (context.SlotIndex < 0 || context.SlotIndex >= state.Slots.Count) return DuelActionResult.Ok();
            if (state.Slots[context.SlotIndex].GetCharacters(context.Player).Count == 1)
            {
                new TakeTopDiscardEffect(EffectId, _amount).Execute(context);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class OppDiscardHandToMatchEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _strictBelow;
        public OppDiscardHandToMatchEffect(string effectId, bool strictBelow) { EffectId = effectId; _strictBelow = strictBelow; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opp = context.Player.Opponent();
            int targetMax = _strictBelow ? Math.Max(0, state.GetHand(context.Player).Count - 1) : state.GetHand(context.Player).Count;
            int excess = state.GetHand(opp).Count - targetMax;
            if (excess > 0)
            {
                context.Engine.DiscardFromHand(opp, excess);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class Look3Draw1Play1Top1Effect : IEffect
    {
        public string EffectId { get; }
        public Look3Draw1Play1Top1Effect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var popped = new List<DuelCard>();
            for (int i = 0; i < 3; i++)
            {
                var c = state.PopTopDeckCard(context.Player);
                if (c == null) break;
                popped.Add(c);
            }
            if (popped.Count == 0) return DuelActionResult.Ok();

            context.Engine.Emit(new CardsRevealedEvent(context.Player, popped));

            DuelCard? toPlay = null;
            for (int i = 0; i < popped.Count; i++)
            {
                if (popped[i].Kind == CardKind.Character && (toPlay == null || popped[i].Force > toPlay.Force))
                {
                    toPlay = popped[i];
                }
            }
            if (toPlay != null)
            {
                int slotIdx = (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
                    ? context.SlotIndex
                    : BothPlayFreeCharEffect.FindFirstOccupiedSlot(state);
                if (slotIdx >= 0)
                {
                    popped.Remove(toPlay);
                    state.AddCharacterToSlot(slotIdx, context.Player, toPlay);
                    context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, toPlay));
                }
            }

            if (popped.Count > 0)
            {
                state.AddCardToHand(context.Player, popped[0]);
                popped.RemoveAt(0);
            }
            if (popped.Count > 0)
            {
                BoardTargetHelper.PutCardsOnTopOfDeck(state, context.Player, popped);
            }
            context.Engine.CheckDeckOutCondition(context.Player);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ClashTopDeckForceTakeEffect : IEffect
    {
        public string EffectId { get; }
        public ClashTopDeckForceTakeEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var p1Card = state.PopTopDeckCard(PlayerId.Player1);
            var p2Card = state.PopTopDeckCard(PlayerId.Player2);
            if (p1Card == null || p2Card == null)
            {
                if (p1Card != null) state.AddCardToHand(PlayerId.Player1, p1Card);
                if (p2Card != null) state.AddCardToHand(PlayerId.Player2, p2Card);
                context.Engine.CheckDeckOutCondition(PlayerId.Player1);
                context.Engine.CheckDeckOutCondition(PlayerId.Player2);
                return DuelActionResult.Ok();
            }

            if (p1Card.Force > p2Card.Force)
            {
                state.AddCardToHand(PlayerId.Player1, p1Card);
                state.AddCardToDiscard(PlayerId.Player2, p2Card);
            }
            else if (p2Card.Force > p1Card.Force)
            {
                state.AddCardToHand(PlayerId.Player2, p2Card);
                state.AddCardToDiscard(PlayerId.Player1, p1Card);
            }
            else
            {
                state.AddCardToDiscard(PlayerId.Player1, p1Card);
                state.AddCardToDiscard(PlayerId.Player2, p2Card);
            }
            context.Engine.CheckDeckOutCondition(PlayerId.Player1);
            context.Engine.CheckDeckOutCondition(PlayerId.Player2);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class BidNumberDiscardVsTakeEffect : IEffect
    {
        public string EffectId { get; }
        public BidNumberDiscardVsTakeEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opp = context.Player.Opponent();
            int bid = Math.Min(1, state.GetHand(context.Player).Count);
            if (bid > 0)
            {
                context.Engine.DiscardFromHand(opp, bid);
                new TakeTopDiscardEffect(EffectId, bid).Execute(context);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class GuessForcePlayFreeEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _repeatOnSuccess;
        public GuessForcePlayFreeEffect(string effectId, bool repeatOnSuccess)
        {
            EffectId = effectId;
            _repeatOnSuccess = repeatOnSuccess;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int slotIdx = (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
                ? context.SlotIndex
                : BothPlayFreeCharEffect.FindFirstOccupiedSlot(state);

            int iterations = _repeatOnSuccess ? 3 : 1;
            for (int i = 0; i < iterations; i++)
            {
                var deck = state.GetDeck(context.Player);
                if (deck.Count == 0) break;
                int guessedForce = GuessModalForce(deck);
                var drawn = context.Engine.DrawCards(context.Player, 1);
                if (drawn.Count == 0 || state.IsGameOver) break;

                var card = drawn[0];
                if (card.Kind == CardKind.Character && card.Force == guessedForce && slotIdx >= 0)
                {
                    state.RemoveCardFromHand(context.Player, card);
                    state.AddCharacterToSlot(slotIdx, context.Player, card);
                    context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, card));
                }
                else
                {
                    break;
                }
            }
            return DuelActionResult.Ok();
        }

        private static int GuessModalForce(IReadOnlyList<DuelCard> deck)
        {
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Kind == CardKind.Character) return deck[i].Force;
            }
            return 2;
        }
    }

    internal sealed class PutHandOnTopDeckEffect : IEffect
    {
        public string EffectId { get; }
        public PutHandOnTopDeckEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var hand = state.GetHand(context.Player);
            if (hand.Count == 0) return DuelActionResult.Ok();

            var toMove = new List<DuelCard>(hand);
            for (int i = 0; i < toMove.Count; i++)
            {
                state.RemoveCardFromHand(context.Player, toMove[i]);
            }
            BoardTargetHelper.PutCardsOnTopOfDeck(state, context.Player, toMove);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ReplaceOtherTerrainEffect : IEffect
    {
        public string EffectId { get; }
        public ReplaceOtherTerrainEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int targetSlot = -1;
            for (int i = 0; i < state.Slots.Count; i++)
            {
                if (i != context.SlotIndex && !state.Slots[i].IsEmpty)
                {
                    targetSlot = i;
                    break;
                }
            }
            if (targetSlot < 0) return DuelActionResult.Ok();

            var slot = state.Slots[targetSlot];
            var oldTerrain = slot.Card;
            var oldBy = slot.PlacedBy ?? context.Player;
            var p1 = new List<DuelCard>(slot.CharactersP1);
            var p2 = new List<DuelCard>(slot.CharactersP2);

            if (oldTerrain != null) state.AddCardToDiscard(oldBy, oldTerrain);
            state.AddCardsToDiscard(PlayerId.Player1, p1);
            state.AddCardsToDiscard(PlayerId.Player2, p2);
            state.ClearSlot(targetSlot);

            var hand = state.GetHand(context.Player);
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind == CardKind.Terrain)
                {
                    var newT = hand[i];
                    state.RemoveCardFromHand(context.Player, newT);
                    state.SetSlot(targetSlot, newT, state.TurnNumber, context.Player);
                    context.Engine.Emit(new TerrainPlayedEvent(context.Player, targetSlot, newT));
                    break;
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SwapDeckDiscardAndDiscardHandEffect : IEffect
    {
        public string EffectId { get; }
        public SwapDeckDiscardAndDiscardHandEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var deckCopy = new List<DuelCard>(state.GetDeck(context.Player));
            var discardCopy = new List<DuelCard>(state.GetDiscard(context.Player));

            while (state.PopTopDeckCard(context.Player) != null) { }
            for (int i = 0; i < discardCopy.Count; i++) state.RemoveCardFromDiscard(context.Player, discardCopy[i]);

            state.AddDeckCards(context.Player, discardCopy);
            state.AddCardsToDiscard(context.Player, deckCopy);

            var hand = state.GetHand(context.Player);
            if (hand.Count > 0)
            {
                context.Engine.DiscardFromHand(context.Player, hand.Count);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class TakeDiscardPerEffigyEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _effigy;
        public TakeDiscardPerEffigyEffect(string effectId, string effigy) { EffectId = effectId; _effigy = effigy; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int count = 0;
            for (int i = 0; i < state.Slots.Count; i++)
            {
                var s = state.Slots[i];
                if (s.IsEmpty) continue;
                var chars = s.GetCharacters(context.Player);
                for (int j = 0; j < chars.Count; j++)
                {
                    if (string.Equals(chars[j].Effigy, _effigy, StringComparison.OrdinalIgnoreCase)) count++;
                }
            }
            if (count > 0)
            {
                new TakeTopDiscardEffect(EffectId, count).Execute(context);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class LoseDiscardPerTerrainEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _perTerrain;
        public LoseDiscardPerTerrainEffect(string effectId, int perTerrain) { EffectId = effectId; _perTerrain = perTerrain; }

        public DuelActionResult Execute(EffectContext context)
        {
            int count = context.Engine.State.TerrainsInPlayCount * _perTerrain;
            if (count > 0) context.Engine.DiscardFromHand(context.Player, count);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class LoseDiscardUntilCountEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _maxHand;
        public LoseDiscardUntilCountEffect(string effectId, int maxHand) { EffectId = effectId; _maxHand = maxHand; }

        public DuelActionResult Execute(EffectContext context)
        {
            int excess = context.Engine.State.GetHand(context.Player).Count - _maxHand;
            if (excess > 0) context.Engine.DiscardFromHand(context.Player, excess);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class LoseMillPerHandCardEffect : IEffect
    {
        public string EffectId { get; }
        public LoseMillPerHandCardEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            int count = context.Engine.State.GetHand(context.Player).Count;
            if (count > 0) context.Engine.MillCards(context.Player, count);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class LoseMillUntilEffigyEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _effigy;
        public LoseMillUntilEffigyEffect(string effectId, string effigy) { EffectId = effectId; _effigy = effigy; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var milled = new List<DuelCard>();
            while (state.GetDeck(context.Player).Count > 0)
            {
                var c = state.PopTopDeckCard(context.Player);
                if (c == null) break;
                state.AddCardToDiscard(context.Player, c);
                milled.Add(c);
                if (string.Equals(c.Effigy, _effigy, StringComparison.OrdinalIgnoreCase)) break;
            }
            if (milled.Count > 0)
            {
                context.Engine.Emit(new CardsMilledEvent(context.Player, milled));
            }
            context.Engine.CheckDeckOutCondition(context.Player);
            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 10. Classes d'effets additionnels Campus (S2)
    // =========================================================================

    internal sealed class RelativeForceBoardEffect : IEffect
    {
        public string EffectId { get; }
        private readonly TargetSide _side;
        private readonly bool _banish;
        private readonly bool _greaterThanSelf;
        private readonly int _maxCount;

        public RelativeForceBoardEffect(string effectId, TargetSide side, bool banish, bool greaterThanSelf, int maxCount)
        {
            EffectId = effectId;
            _side = side;
            _banish = banish;
            _greaterThanSelf = greaterThanSelf;
            _maxCount = maxCount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int selfForce = context.SourceCard.Force;

            void RunStep(int remaining)
            {
                if (remaining <= 0) return;
                var all = BoardTargetHelper.CollectBoardCharacters(state, context.Player, _side, excludeCard: context.SourceCard);
                var filtered = new List<SlotCharEntry>();
                for (int i = 0; i < all.Count; i++)
                {
                    bool match = _greaterThanSelf ? all[i].Card.Force > selfForce : all[i].Card.Force < selfForce;
                    if (match) filtered.Add(all[i]);
                }
                if (filtered.Count == 0) return;

                string title = _banish ? "Bannissez un personnage" : "Défaussez un personnage";
                string desc = _greaterThanSelf
                    ? $"Sélectionnez un personnage de Force > {selfForce}"
                    : $"Sélectionnez un personnage de Force < {selfForce}";

                BoardTargetHelper.SelectBoardEntry(
                    context.Engine,
                    context.Player,
                    _side,
                    filtered,
                    title,
                    desc,
                    entry =>
                    {
                        var slot = state.Slots[entry.SlotIndex];
                        if (slot.RemoveCharacter(entry.Owner, entry.Card))
                        {
                            if (_banish)
                            {
                                state.AddCardToBanish(entry.Owner, entry.Card);
                                context.Engine.Emit(new CardsBanishedEvent(entry.Owner, new[] { entry.Card }));
                            }
                            else
                            {
                                state.AddCardToDiscard(entry.Owner, entry.Card);
                            }
                        }
                        if (remaining > 1) RunStep(remaining - 1);
                    });
            }

            RunStep(_maxCount);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class BounceAllOppCharsOnSlotEffect : IEffect
    {
        public string EffectId { get; }
        public BounceAllOppCharsOnSlotEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            if (context.SlotIndex < 0 || context.SlotIndex >= state.Slots.Count) return DuelActionResult.Ok();
            var slot = state.Slots[context.SlotIndex];
            var opp = context.Player.Opponent();
            var oppChars = new List<DuelCard>(slot.GetCharacters(opp));
            for (int i = 0; i < oppChars.Count; i++)
            {
                if (slot.RemoveCharacter(opp, oppChars[i]))
                {
                    state.AddCardToHand(opp, oppChars[i]);
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class DiscardOppCharsSumForceEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _maxSum;
        public DiscardOppCharsSumForceEffect(string effectId, int maxSum) { EffectId = effectId; _maxSum = maxSum; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int remainingSum = _maxSum;
            while (remainingSum > 0)
            {
                var oppChars = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Opponent, maxForce: remainingSum);
                if (oppChars.Count == 0) break;
                var best = BoardTargetHelper.AutoPickBoardEntry(oppChars, context.Player, TargetSide.Opponent);
                if (state.Slots[best.SlotIndex].RemoveCharacter(best.Owner, best.Card))
                {
                    state.AddCardToDiscard(best.Owner, best.Card);
                    remainingSum -= Math.Max(0, best.Card.Force);
                }
                else
                {
                    break;
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class DrawPerCompletedComboEffect : IEffect
    {
        public string EffectId { get; }
        public DrawPerCompletedComboEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int combos = 0;
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var chars = state.Slots[s].GetCharacters(context.Player);
                for (int i = 1; i < chars.Count; i++)
                {
                    if (ComboEvaluator.IsComboActive(chars, i)) combos++;
                }
            }
            if (combos > 0) context.Engine.DrawCards(context.Player, combos);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class MillOppPerIncompleteComboEffect : IEffect
    {
        public string EffectId { get; }
        public MillOppPerIncompleteComboEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int incomplete = 0;
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var chars = state.Slots[s].GetCharacters(context.Player);
                for (int i = 0; i < chars.Count; i++)
                {
                    bool right = ComboEvaluator.IsComboActive(chars, i);
                    bool left = (i + 1 < chars.Count) && ComboEvaluator.IsComboActive(chars, i + 1);
                    if ((chars[i].HasOpeningGem || chars[i].HasClosingGem) && !right && !left)
                    {
                        incomplete++;
                    }
                }
            }
            if (incomplete > 0) context.Engine.MillCards(context.Player.Opponent(), incomplete);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class TopDiscardForceEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _millOpponent;
        public TopDiscardForceEffect(string effectId, bool millOpponent) { EffectId = effectId; _millOpponent = millOpponent; }

        public DuelActionResult Execute(EffectContext context)
        {
            var discard = context.Engine.State.GetDiscard(context.Player);
            int force = 0;
            for (int i = discard.Count - 1; i >= 0; i--)
            {
                if (discard[i].Kind == CardKind.Character)
                {
                    force = discard[i].Force;
                    break;
                }
            }
            if (force > 0)
            {
                if (_millOpponent) context.Engine.MillCards(context.Player.Opponent(), force);
                else context.Engine.DrawCards(context.Player, force);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class DrawUntilCharacterEffect : IEffect
    {
        public string EffectId { get; }
        public DrawUntilCharacterEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            while (!state.IsGameOver && state.GetDeck(context.Player).Count > 0)
            {
                var drawn = context.Engine.DrawCards(context.Player, 1);
                if (drawn.Count == 0 || drawn[0].Kind == CardKind.Character) break;
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class PutOneDiscardCardOnTopDeckEffect : IEffect
    {
        public string EffectId { get; }
        public PutOneDiscardCardOnTopDeckEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            if (discard.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                new List<DuelCard>(discard),
                "Reposez une carte de votre défausse sur le deck",
                "Sélectionnez une carte à placer sur le dessus de votre deck",
                preferHighestForce: true,
                chosen =>
                {
                    state.RemoveCardFromDiscard(context.Player, chosen);
                    BoardTargetHelper.PutCardsOnTopOfDeck(state, context.Player, new List<DuelCard> { chosen });
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ScoreAnyTerrainPutUnderDeckOnWinEffect : IEffect
    {
        public string EffectId { get; }
        public ScoreAnyTerrainPutUnderDeckOnWinEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            int beforeScore = context.Engine.State.GetScore(context.Player);
            new ScoreAnyActiveTerrainEffect(EffectId).Execute(context);
            if (!context.Engine.State.IsGameOver && context.Engine.State.GetScore(context.Player) > beforeScore)
            {
                var discard = context.Engine.State.GetDiscard(context.Player);
                if (discard.Count > 0)
                {
                    var top = discard[discard.Count - 1];
                    context.Engine.State.RemoveCardFromDiscard(context.Player, top);
                    context.Engine.State.AddCardToBottomDeck(context.Player, top);
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ScoreTerrainWithoutNameEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _excludedName;
        public ScoreTerrainWithoutNameEffect(string effectId, string excludedName)
        {
            EffectId = effectId;
            _excludedName = excludedName;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            for (int i = 0; i < state.Slots.Count; i++)
            {
                var slot = state.Slots[i];
                if (slot.IsEmpty || !slot.IsActive(state.TurnNumber)) continue;
                bool hasExcluded = false;
                foreach (var c in slot.CharactersP1)
                {
                    if (c.Name.IndexOf(_excludedName, StringComparison.OrdinalIgnoreCase) >= 0) { hasExcluded = true; break; }
                }
                if (!hasExcluded)
                {
                    foreach (var c in slot.CharactersP2)
                    {
                        if (c.Name.IndexOf(_excludedName, StringComparison.OrdinalIgnoreCase) >= 0) { hasExcluded = true; break; }
                    }
                }
                if (!hasExcluded)
                {
                    context.Engine.ResolveScore(i, context.Player);
                    break;
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SearchAndBanishOppDeckEffect : IEffect
    {
        public string EffectId { get; }
        public SearchAndBanishOppDeckEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opp = context.Player.Opponent();
            var oppDeck = state.GetDeck(opp);
            if (oppDeck.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                new List<DuelCard>(oppDeck),
                "Bannissez une carte du deck adverse",
                "Sélectionnez une carte du deck adverse à bannir",
                preferHighestForce: true,
                chosen =>
                {
                    if (state.RemoveCardFromDeck(opp, chosen))
                    {
                        state.AddCardToBanish(opp, chosen);
                        context.Engine.Emit(new CardsBanishedEvent(opp, new[] { chosen }));
                        context.Engine.CheckDeckOutCondition(opp);
                    }
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ShuffleDiscardScorerToDeckEffect : IEffect
    {
        public string EffectId { get; }
        public ShuffleDiscardScorerToDeckEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            var scorers = new List<DuelCard>();
            for (int i = 0; i < discard.Count; i++)
            {
                if (discard[i].IsScoreur || discard[i].IsComboScoreur) scorers.Add(discard[i]);
            }
            if (scorers.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                scorers,
                "Remettez un Scoreur dans votre deck",
                "Sélectionnez un Scoreur de votre défausse",
                preferHighestForce: true,
                chosen =>
                {
                    state.RemoveCardFromDiscard(context.Player, chosen);
                    state.AddCardToBottomDeck(context.Player, chosen);
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class TakeBottomDiscardEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public TakeBottomDiscardEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            int toTake = Math.Min(_amount, discard.Count);
            for (int i = 0; i < toTake; i++)
            {
                var card = discard[0];
                state.RemoveCardFromDiscard(context.Player, card);
                state.AddCardToHand(context.Player, card);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class HomonymDuoDiscardEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _toBottomDeck;
        public HomonymDuoDiscardEffect(string effectId, bool toBottomDeck) { EffectId = effectId; _toBottomDeck = toBottomDeck; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var slot = state.Slots[s];
                if (slot.IsEmpty) continue;
                CountNames(slot.CharactersP1, nameCounts);
                CountNames(slot.CharactersP2, nameCounts);
            }
            int duos = 0;
            foreach (var kv in nameCounts) duos += kv.Value / 2;
            int amount = duos * 2;
            if (amount <= 0) return DuelActionResult.Ok();

            var discard = state.GetDiscard(context.Player);
            int toMove = Math.Min(amount, discard.Count);
            for (int i = 0; i < toMove; i++)
            {
                var top = discard[discard.Count - 1];
                state.RemoveCardFromDiscard(context.Player, top);
                if (_toBottomDeck) state.AddCardToBottomDeck(context.Player, top);
                else state.AddCardToHand(context.Player, top);
            }
            return DuelActionResult.Ok();
        }

        private static void CountNames(IReadOnlyList<DuelCard> chars, Dictionary<string, int> counts)
        {
            for (int i = 0; i < chars.Count; i++)
            {
                string name = chars[i].Name;
                counts[name] = counts.TryGetValue(name, out int cur) ? cur + 1 : 1;
            }
        }
    }

    internal sealed class OppDiscardHandIfHighestCostEffect : IEffect
    {
        public string EffectId { get; }
        public OppDiscardHandIfHighestCostEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opp = context.Player.Opponent();
            var oppHand = state.GetHand(opp);
            if (oppHand.Count == 0) return DuelActionResult.Ok();

            int myMax = 0;
            var myHand = state.GetHand(context.Player);
            for (int i = 0; i < myHand.Count; i++)
            {
                int cost = EffectEngineBridge.GetCardCost(myHand[i]);
                if (cost > myMax) myMax = cost;
            }
            int oppMax = 0;
            for (int i = 0; i < oppHand.Count; i++)
            {
                int cost = EffectEngineBridge.GetCardCost(oppHand[i]);
                if (cost > oppMax) oppMax = cost;
            }
            if (oppMax >= myMax)
            {
                context.Engine.DiscardFromHand(opp, 1);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class PlayOppDiscardCostLe1NoEffectsEffect : IEffect
    {
        public string EffectId { get; }
        public PlayOppDiscardCostLe1NoEffectsEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            int slotIdx = (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
                ? context.SlotIndex
                : BothPlayFreeCharEffect.FindFirstOccupiedSlot(state);
            if (slotIdx < 0) return DuelActionResult.Ok();

            var opp = context.Player.Opponent();
            var oppDiscard = state.GetDiscard(opp);
            var candidates = new List<DuelCard>();
            for (int i = 0; i < oppDiscard.Count; i++)
            {
                if (oppDiscard[i].Kind == CardKind.Character && EffectEngineBridge.GetCardCost(oppDiscard[i]) <= 1)
                {
                    candidates.Add(oppDiscard[i]);
                }
            }
            if (candidates.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                candidates,
                "Jouez un personnage de la défausse adverse",
                "Sélectionnez un personnage de coût <= 1 à poser de votre côté sans effets",
                preferHighestForce: true,
                chosen =>
                {
                    state.RemoveCardFromDiscard(opp, chosen);
                    state.AddCharacterToSlot(slotIdx, context.Player, chosen);
                    context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, chosen));
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class StealOppHandCardPlayAndBanishSelfEffect : IEffect
    {
        public string EffectId { get; }
        public StealOppHandCardPlayAndBanishSelfEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opp = context.Player.Opponent();
            var oppHand = state.GetHand(opp);
            var oppChars = new List<DuelCard>();
            for (int i = 0; i < oppHand.Count; i++)
            {
                if (oppHand[i].Kind == CardKind.Character) oppChars.Add(oppHand[i]);
            }
            if (oppChars.Count > 0)
            {
                int slotIdx = (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
                    ? context.SlotIndex
                    : BothPlayFreeCharEffect.FindFirstOccupiedSlot(state);
                if (slotIdx >= 0)
                {
                    BoardTargetHelper.SelectCard(
                        context.Engine,
                        context.Player,
                        oppChars,
                        "Volez un personnage de la main adverse",
                        "Sélectionnez un personnage de la main adverse à jouer pour vous",
                        preferHighestForce: true,
                        chosen =>
                        {
                            if (state.RemoveCardFromHand(opp, chosen))
                            {
                                state.AddCharacterToSlot(slotIdx, context.Player, chosen);
                                context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, chosen));
                                context.Engine.CheckDeckOutCondition(opp);
                            }
                        });
                }
            }

            if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
            {
                var slot = state.Slots[context.SlotIndex];
                if (slot.RemoveCharacter(context.Player, context.SourceCard))
                {
                    state.AddCardToBanish(context.Player, context.SourceCard);
                    context.Engine.Emit(new CardsBanishedEvent(context.Player, new[] { context.SourceCard }));
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class GuessForceDrawPlayOrDiscardEffect : IEffect
    {
        public string EffectId { get; }
        public GuessForceDrawPlayOrDiscardEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var deck = state.GetDeck(context.Player);
            if (deck.Count == 0) return DuelActionResult.Ok();

            int guessedForce = deck[0].Force;
            var drawn = context.Engine.DrawCards(context.Player, 1);
            if (drawn.Count == 0 || state.IsGameOver) return DuelActionResult.Ok();

            var card = drawn[0];
            int slotIdx = (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
                ? context.SlotIndex
                : BothPlayFreeCharEffect.FindFirstOccupiedSlot(state);

            if (card.Kind == CardKind.Character && card.Force == guessedForce && slotIdx >= 0)
            {
                state.RemoveCardFromHand(context.Player, card);
                state.AddCharacterToSlot(slotIdx, context.Player, card);
                context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, card));
                context.Engine.DrawCards(context.Player, 2);
            }
            else
            {
                context.Engine.DiscardFromHand(context.Player, 1);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class OppChoiceTakeDiscardEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _amount;
        public OppChoiceTakeDiscardEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

        public DuelActionResult Execute(EffectContext context)
        {
            var opp = context.Player.Opponent();
            var discard = context.Engine.State.GetDiscard(opp);
            int toTake = Math.Min(_amount, discard.Count);
            for (int i = 0; i < toTake; i++)
            {
                var top = discard[discard.Count - 1];
                context.Engine.State.RemoveCardFromDiscard(opp, top);
                context.Engine.State.AddCardToHand(opp, top);
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class CopyCharOnPlayEffect : IEffect
    {
        public string EffectId { get; }
        private readonly bool _costZeroOnly;
        public CopyCharOnPlayEffect(string effectId, bool costZeroOnly) { EffectId = effectId; _costZeroOnly = costZeroOnly; }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var all = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Any, excludeCard: context.SourceCard);
            var candidates = new List<DuelCard>();
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i].Card;
                if (c.EffectIds == null || c.EffectIds.Count == 0) continue;
                if (_costZeroOnly && EffectEngineBridge.GetCardCost(c) != 0) continue;
                candidates.Add(c);
            }
            if (candidates.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                candidates,
                "Copiez l'effet d'un personnage",
                "Sélectionnez un personnage dont copier l'effet d'arrivée",
                preferHighestForce: true,
                chosen =>
                {
                    for (int i = 0; i < chosen.EffectIds.Count; i++)
                    {
                        string effId = chosen.EffectIds[i];
                        if (effId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) ||
                            effId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase) ||
                            effId.StartsWith("passive_", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(effId, EffectId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        if (context.Engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
                        {
                            eff.Execute(context);
                        }
                    }
                });
            return DuelActionResult.Ok();
        }
    }

    // =========================================================================
    // 11. Classes d'effets combo additionnels Origins (S1)
    // =========================================================================

    internal sealed class DrawByDiscardedCharForceEffect : IEffect
    {
        public string EffectId { get; }
        public DrawByDiscardedCharForceEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var own = BoardTargetHelper.CollectBoardCharacters(state, context.Player, TargetSide.Own);
            if (own.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectBoardEntry(
                context.Engine,
                context.Player,
                TargetSide.Own,
                own,
                "Défaussez un personnage pour piocher",
                "Sélectionnez un de vos personnages à défausser pour piocher autant de cartes que sa Force",
                entry =>
                {
                    if (state.Slots[entry.SlotIndex].RemoveCharacter(entry.Owner, entry.Card))
                    {
                        state.AddCardToDiscard(entry.Owner, entry.Card);
                        if (entry.Card.Force > 0 && !state.IsGameOver)
                        {
                            context.Engine.DrawCards(context.Player, entry.Card.Force);
                        }
                    }
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class LookBottomDeckOptionalTopEffect : IEffect
    {
        public string EffectId { get; }
        public LookBottomDeckOptionalTopEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var deck = state.GetDeck(context.Player);
            if (deck.Count == 0) return DuelActionResult.Ok();

            var bottom = deck[deck.Count - 1];
            context.Engine.Emit(new CardsRevealedEvent(context.Player, new[] { bottom }));
            if (bottom.Kind == CardKind.Character && bottom.Force >= 2)
            {
                var popped = state.PopBottomDeckCard(context.Player);
                if (popped != null)
                {
                    BoardTargetHelper.PutCardsOnTopOfDeck(state, context.Player, new List<DuelCard> { popped });
                }
            }
            return DuelActionResult.Ok();
        }
    }

    internal sealed class MulliganHandBottomDrawEffect : IEffect
    {
        public string EffectId { get; }
        public MulliganHandBottomDrawEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var hand = new List<DuelCard>(state.GetHand(context.Player));
            if (hand.Count == 0) return DuelActionResult.Ok();

            for (int i = 0; i < hand.Count; i++)
            {
                state.RemoveCardFromHand(context.Player, hand[i]);
                state.AddCardToBottomDeck(context.Player, hand[i]);
            }
            context.Engine.DrawCards(context.Player, hand.Count);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class PlayFreeCharSingleEffect : IEffect
    {
        public string EffectId { get; }
        public PlayFreeCharSingleEffect(string effectId) => EffectId = effectId;

        public DuelActionResult Execute(EffectContext context)
        {
            BothPlayFreeCharEffect.PlayOneFree(context.Engine, context.Player, context.SlotIndex);
            return DuelActionResult.Ok();
        }
    }

    internal sealed class ReturnDiscardByNameToTopDeckEffect : IEffect
    {
        public string EffectId { get; }
        private readonly string _nameFragment;
        public ReturnDiscardByNameToTopDeckEffect(string effectId, string nameFragment)
        {
            EffectId = effectId;
            _nameFragment = nameFragment;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var discard = state.GetDiscard(context.Player);
            var matches = new List<DuelCard>();
            for (int i = 0; i < discard.Count; i++)
            {
                if (discard[i].Name.IndexOf(_nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(discard[i]);
                }
            }
            if (matches.Count == 0) return DuelActionResult.Ok();

            BoardTargetHelper.SelectCard(
                context.Engine,
                context.Player,
                matches,
                $"Replacez un {_nameFragment} sur le deck",
                "Sélectionnez une carte de votre défausse à reposer sur votre deck",
                preferHighestForce: true,
                chosen =>
                {
                    state.RemoveCardFromDiscard(context.Player, chosen);
                    BoardTargetHelper.PutCardsOnTopOfDeck(state, context.Player, new List<DuelCard> { chosen });
                });
            return DuelActionResult.Ok();
        }
    }

    internal sealed class SubBanishOppLastHandCardsEffect : IEffect
    {
        public string EffectId { get; }
        private readonly int _maxCount;

        public SubBanishOppLastHandCardsEffect(string effectId, int maxCount)
        {
            EffectId = effectId;
            _maxCount = maxCount;
        }

        public DuelActionResult Execute(EffectContext context)
        {
            var state = context.Engine.State;
            var opp = context.Player.Opponent();
            var oppHand = state.GetHand(opp);
            int toBanish = Math.Min(_maxCount, oppHand.Count);
            if (toBanish <= 0) return DuelActionResult.Ok();

            var banished = new List<DuelCard>(toBanish);
            for (int i = 0; i < toBanish; i++)
            {
                var last = oppHand[oppHand.Count - 1];
                if (state.RemoveCardFromHand(opp, last))
                {
                    state.AddCardToBanish(opp, last);
                    banished.Add(last);
                }
            }
            if (banished.Count > 0)
            {
                context.Engine.Emit(new CardsBanishedEvent(opp, banished));
                context.Engine.CheckDeckOutCondition(opp);
            }
            return DuelActionResult.Ok();
        }
    }
}
