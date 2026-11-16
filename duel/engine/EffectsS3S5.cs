#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// Enregistre les primitives d'effets spécifiques aux saisons Battle (S3), Stellar (S4) et Legacy (S5).
/// Conçu en C# pur sans aucune dépendance Unity ou Harmony.
/// </summary>
public static class EffectsS3S5
{
    public static void RegisterAll(EffectRegistry registry)
    {
        if (registry == null) return;

        // =========================================================================
        // 1. Position dans la ligne (Battle S3 - Surfeur, etc.)
        // =========================================================================
        registry.Register(new DrawByLinePositionEffect("draw_by_line_pos"));
        registry.Register(new MillOpponentByLinePositionEffect("mill_opp_by_line_pos"));

        // =========================================================================
        // 2. Chaînes de gemmes (Legacy S5 - Corbeau, etc.)
        // =========================================================================
        registry.Register(new DiscardToTopDeckByGemChainEffect("discard_to_top_deck_by_gem_chain"));

        // =========================================================================
        // 3. Manipulation de gemmes & annulation (Battle S3 / Legacy S5)
        // =========================================================================
        registry.Register(new RemoveOpponentOpeningGemTurnEffect("remove_opp_opening_gem_turn"));
        registry.Register(new RemoveOwnGemsBoostPerGemEffect("remove_own_gems_boost_1_per_gem"));
        registry.Register(new CancelCharacterEffectTurnEffect("cancel_char_effect_turn", CancelScope.SingleOpponent));
        registry.Register(new CancelCharacterEffectTurnEffect("cancel_other_allies_effects_on_slot", CancelScope.OtherAlliesOnSlot));
        registry.Register(new CancelCharacterEffectTurnEffect("cancel_other_cards_effects_on_slot_turn", CancelScope.AllOthersOnSlot));

        // =========================================================================
        // 4. Alignement de colonne (Stellar S4 - Stick Fight, etc.)
        // =========================================================================
        registry.Register(new DrawIfAlignedColumnEffect("draw_3_if_aligned_column", 3));
        registry.Register(new BoostIfAlignedColumnEffect("boost_if_aligned_column_2", 2));

        // =========================================================================
        // 5. Effets spéciaux Battle / Stellar / Legacy (cœur du cahier des charges)
        // =========================================================================
        registry.Register(new BanishHandCharacterCopyStatsEffect("banish_hand_char_copy_stats"));
        registry.Register(new BothBanishTopIfSmallerDeckEffect("both_banish_top_2_if_smaller_deck", 2));
        registry.Register(new ApplyBothLosingEffectInPlayEffect("apply_both_losing_effect_in_play", bothPlayers: true));
        registry.Register(new MillAndBonusNextScoreEffect("mill_10_bonus_next_score", 10));
        registry.Register(new DrawIfMoreCharactersThanOpponentEffect("draw_3_if_more_chars_than_opp", 3));
        registry.Register(new MillEqualHandSizeEffect("mill_equal_hand_size"));
        registry.Register(new BanishDeckEffect("banish_opp_deck_3", 3, targetOpponent: true));
        registry.Register(new BanishDeckEffect("banish_opp_deck_4", 4, targetOpponent: true));
        registry.Register(new BanishDeckEffect("banish_opp_deck_5", 5, targetOpponent: true));
        registry.Register(new BanishDeckEffect("banish_own_deck_2", 2, targetOpponent: false));

        // =========================================================================
        // 6. Cartes DUO (Legacy S5, soutiens sur terrain)
        // =========================================================================
        registry.Register(new MarkerEffect("duo_martine_jeanine"));
        registry.Register(new PlayCharacterFreeNoScoreEffect("duo_martine_jeanine_opp_play_free"));
        registry.Register(new MarkerEffect("duo_martine_jeanine_active_cost_minus_1"));

        registry.Register(new MarkerEffect("duo_flantier"));
        registry.Register(new DiscardCharacterByMaxForceEffect("duo_flantier_active_discard", maxForce: 2));
        registry.Register(new DiscardCharacterByMaxForceEffect("duo_flantier_opp_discard", maxForce: 6));

        registry.Register(new MarkerEffect("duo_andre_pierre"));
        registry.Register(new DoubleOwnCharacterForceEffect("duo_andre_pierre_double_force", discardHandCardFirst: false));
        registry.Register(new DoubleOwnCharacterForceEffect("duo_andre_pierre_active_double_force", discardHandCardFirst: true));

        registry.Register(new MarkerEffect("duo_aventuriers_manoir"));
        registry.Register(new DiscardCharacterByMaxForceEffect("duo_aventuriers_opp_discard_char", maxForce: int.MaxValue));
        registry.Register(new DrawCardsEffect("duo_aventuriers_active_draw_2", 2));

        registry.Register(new MarkerEffect("duo_jakov_richus"));
        registry.Register(new MarkerEffect("duo_jakov_richus_opp_boost_4"));
        registry.Register(new MarkerEffect("duo_jakov_richus_active_debuff_1"));

        // =========================================================================
        // 7. Effets (Sub) (réactions depuis la main)
        // =========================================================================
        registry.Register(new SubReactionMarkerEffect("sub_cancel_discard_or_mill", playToBoard: false));
        registry.Register(new SubReactionMarkerEffect("sub_play_on_opp_orange_combo", playToBoard: true));
        registry.Register(new SubReactionMarkerEffect("sub_play_on_opp_violet_combo", playToBoard: true));
        registry.Register(new SubReactionMarkerEffect("sub_cancel_opp_effect", playToBoard: false));
        registry.Register(new SubReactionMarkerEffect("sub_play_on_opp_play_char", playToBoard: true));
        registry.Register(new SubReactionMarkerEffect("sub_play_on_opp_score", playToBoard: true));
        registry.Register(new SubReactionMarkerEffect("sub_play_on_opp_draw", playToBoard: true));
        registry.Register(new SubDrawReactionEffect("sub_play_on_opp_effect_draw_draw_2", 2));
        registry.Register(new SubCancelForceModsOnSlotEffect("sub_cancel_force_modifications_on_slot"));
        registry.Register(new SubReduceCost0ForceTo0Effect("sub_reduce_cost_0_char_force_to_0"));
        registry.Register(new SubSwapTwoCharsSameSideEffect("sub_swap_two_chars_same_side"));
        registry.Register(new SubSwapTwoTerrainsEffect("sub_swap_two_terrains"));
        registry.Register(new SubMillEqualForceShuffleOppCharEffect("sub_mill_equal_opp_force_shuffle_opp_char_to_deck"));

        // =========================================================================
        // 8. Primitives actives additionnelles S3 / S4 / S5
        // =========================================================================
        // --- Pioche / Meule / Bannissement symétriques & conditionnels ---
        registry.Register(new OpponentDrawSimpleEffect("opp_draw_3", 3));
        registry.Register(new BothDrawUntilHandEffect("opp_draw_until_hand_5", 5));
        registry.Register(new BothBanishHandEffect("both_banish_hand_1", 1));
        registry.Register(new BothDiscardAndDrawEffect("both_discard_3_draw_3", 3, 3));
        registry.Register(new BothShuffleHandAndDrawEffect("both_shuffle_hand_draw_5", 5));
        registry.Register(new BothTakeBanishEffect("both_take_banish_3", 3));
        registry.Register(new BothLookBottomPutTopEffect("both_look_bottom_deck_put_top"));
        registry.Register(new BothDrawUntilTerrainEffect("both_draw_until_terrain_max_5", 5));
        registry.Register(new BothDrawWhileHigherForceEffect("both_draw_while_higher_force"));
        registry.Register(new BothDrawPerCountEffect("both_draw_per_banish_max_4", BothCountKind.Banish, maxCount: 4));
        registry.Register(new BothDrawPerCountEffect("both_draw_per_won_terrain", BothCountKind.WonTerrains, maxCount: 99));
        registry.Register(new BothTakeDiscardPerWonTerrainEffect("both_take_discard_per_won_terrain"));
        registry.Register(new BothDiscard1SearchDeck1Effect("both_discard_1_search_deck_1"));
        registry.Register(new BothDiscardHighestDrawEffect("both_discard_highest_draw_3", 3));
        registry.Register(new BothPlayFreeIfUnstackedEffect("both_play_free_if_unstacked"));
        registry.Register(new BothLookTopTakeOneShuffleEffect("both_look_top_5_take_1_shuffle", 5));

        registry.Register(new ConditionalDrawEffect("if_0_won_terrains_draw_4", ctx => ctx.Engine.State.GetScore(ctx.Player) == 0, 4, both: false));
        registry.Register(new ConditionalDrawEffect("if_3_terrains_both_draw_3", ctx => ctx.Engine.State.TerrainsInPlayCount >= 3, 3, both: true));
        registry.Register(new DrawThreeElseDiscardOneEffect("draw_3_else_discard_1"));
        registry.Register(new DrawUntilOppHandPlusOneEffect("draw_until_opp_hand_plus_1"));
        registry.Register(new DrawPerBoardPredicateEffect("draw_1_per_mouche_on_slot", BoardPredicateKind.MoucheOnSlot, maxCount: 99, discardOppIf5: false));
        registry.Register(new DrawPerBoardPredicateEffect("draw_1_per_own_char_on_slot_max_6_if_5_discard_opp", BoardPredicateKind.OwnCharsOnSlot, maxCount: 6, discardOppIf5: true));
        registry.Register(new DrawPerBoardPredicateEffect("draw_per_viking_in_play", BoardPredicateKind.VikingInPlay, maxCount: 99, discardOppIf5: false));
        registry.Register(new DrawPerBoardPredicateEffect("draw_per_facedown_in_play", BoardPredicateKind.FacedownInPlay, maxCount: 99, discardOppIf5: false));
        registry.Register(new DrawPerBoardPredicateEffect("draw_per_combo_this_turn", BoardPredicateKind.CombosOnBoard, maxCount: 99, discardOppIf5: false));

        registry.Register(new BanishOppDiscardByOppBanishEffect("banish_opp_discard_by_opp_banish_count"));
        registry.Register(new MillOppPerBoardCountEffect("mill_opp_per_cavalier_in_play", MillCountKind.CavalierInPlay));
        registry.Register(new MillOppPerBoardCountEffect("mill_opp_per_combo_in_play", MillCountKind.CombosInPlay));
        registry.Register(new MillOppUntilEffect("mill_opp_until_cost_ge_1", untilCostGe1: true, forceTarget: 0));
        registry.Register(new MillOppUntilEffect("mill_opp_until_total_force_ge_9", untilCostGe1: false, forceTarget: 9));

        // --- Effets Win / Lose S3 / S4 / S5 ---
        registry.Register(new DrawCardsEffect("win_draw_4", 4));
        registry.Register(new MillOpponentEffect("win_mill_3", 3));
        registry.Register(new MillOpponentEffect("win_mill_4", 4));
        registry.Register(new SelfMillEffect("win_self_mill_3", 3));
        registry.Register(new SelfMillEffect("win_self_mill_4", 4));
        registry.Register(new BanishDiscardEffect("win_banish_discard_1", 1, targetOpponent: false));
        registry.Register(new BanishDiscardEffect("win_banish_top_discard_1", 1, targetOpponent: false));
        registry.Register(new ShuffleHandAndDrawEffect("win_shuffle_hand_draw_3", 3));
        registry.Register(new TakeBottomDeckEffect("win_add_bottom_deck_2", 2));
        registry.Register(new PutTopDiscardOnTopDeckEffect("win_put_top_discard_on_deck", 1));
        registry.Register(new ReturnBanishedToTopDeckEffect("win_return_banished_to_top_deck_2", 2));
        registry.Register(new ReturnTopDiscardToBottomDeckS3S5Effect("win_return_discard_to_bottom_deck_3", 3));
        registry.Register(new WinBounceNonScorerEffect("win_bounce_non_scorer"));
        registry.Register(new WinDiscardOwnCharInPlayEffect("win_discard_own_char_in_play"));
        registry.Register(new WinShuffleComboCharFromDiscardEffect("win_shuffle_combo_char_to_deck"));
        registry.Register(new WinShuffleCharForceLeToDeckEffect("win_shuffle_char_force_le_4_to_deck", 4));
        registry.Register(new WinTakeDiscardNonScorerEffect("win_take_discard_non_scorer"));
        registry.Register(new WinTakeDiscardPerOppCharEffect("win_take_discard_per_opp_char_max_6", 6));
        registry.Register(new WinDrawPerCharMaxEffect("win_draw_per_char_max_6", 6));
        registry.Register(new WinDrawPerWonTerrainEffect("win_draw_per_won_terrain"));
        registry.Register(new DrawPerBoardPredicateEffect("win_draw_per_effect", BoardPredicateKind.EffectsOnBoard, maxCount: 6, discardOppIf5: false));
        registry.Register(new MarkerEffect("win_double_next_char_force"));

        registry.Register(new LoseDiscardOrMillEffect("lose_discard_2_or_mill_3", discardCount: 2, millCount: 3));
        registry.Register(new LoseDiscardAndDrawEffect("lose_discard_3_draw_3", 3, 3));
        registry.Register(new MillBottomDeckEffect("lose_discard_bottom_deck_3", 3));
        registry.Register(new MillBottomDeckEffect("lose_mill_bottom_4", 4));
        registry.Register(new LoseMillOrMoreIfSmallerDiscardEffect("lose_mill_3_or_5_if_smaller_discard", 3, 5));
        registry.Register(new LoseMillEqualHandMaxEffect("lose_mill_equal_hand_max_7", 7));
        registry.Register(new LoseMillPerBoardCountEffect("lose_mill_per_char", LoseMillKind.OwnCharsOnSlot, maxCount: 99));
        registry.Register(new LoseMillPerBoardCountEffect("lose_mill_per_opp_char_max_7", LoseMillKind.OppCharsOnSlot, maxCount: 7));
        registry.Register(new LoseMillPerBoardCountEffect("lose_mill_per_duplicate_char_in_play", LoseMillKind.DuplicateCharsInPlay, maxCount: 99));
        registry.Register(new LoseMillPerBoardCountEffect("lose_mill_per_guest_in_play", LoseMillKind.GuestsInPlay, maxCount: 99));
        registry.Register(new LoseMillPerBoardCountEffect("lose_mill_per_won_terrain", LoseMillKind.WonTerrains, maxCount: 99));
        registry.Register(new PutTopDiscardOnTopDeckEffect("lose_return_top_discard_to_deck_4", 4));
        registry.Register(new RollDiceEffect("lose_roll_dice_mill", RollDiceMode.SelfMill));

        // --- Recherches dans le deck ---
        registry.Register(new S3S5SearchDeckEffect("both_search_deck_guest_1", c => c.Kind == CardKind.Character && !string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase) && !string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase), 1, bothPlayers: true));
        registry.Register(new S3S5SearchDeckEffect("both_search_deck_laink_or_terracid", c => string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase), 1, bothPlayers: true));
        registry.Register(new BothSearchCopyOfCharInPlayEffect("both_search_copy_of_char_in_play"));
        registry.Register(new S3S5SearchDeckEffect("search_deck_2_robots", c => c.Name.IndexOf("Robot", StringComparison.OrdinalIgnoreCase) >= 0, 2, bothPlayers: false));
        registry.Register(new S3S5SearchDeckEffect("search_deck_dragon_1", c => c.Name.IndexOf("Dragon", StringComparison.OrdinalIgnoreCase) >= 0, 1, bothPlayers: false));
        registry.Register(new S3S5SearchDeckEffect("search_deck_mouche_1", c => c.Name.IndexOf("Mouche", StringComparison.OrdinalIgnoreCase) >= 0, 1, bothPlayers: false));
        registry.Register(new S3S5SearchDeckEffect("search_deck_samourai_1", c => c.Name.IndexOf("Samoura", StringComparison.OrdinalIgnoreCase) >= 0, 1, bothPlayers: false));
        registry.Register(new S3S5SearchDeckEffect("search_deck_terracid_1", c => string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase), 1, bothPlayers: false));
        registry.Register(new SearchDeckLainkAndTerracidEffect("search_deck_laink_and_terracid"));
        registry.Register(new S3S5SearchDeckEffect("search_deck_up_to_3_garagistes", c => c.Name.IndexOf("Garagiste", StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf("Pompiste", StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf("Mécanicien", StringComparison.OrdinalIgnoreCase) >= 0, 3, bothPlayers: false));
        registry.Register(new S3S5SearchDeckEffect("search_deck_char_with_keyword", c => c.Kind == CardKind.Character && (c.EffectIds.Count > 0 || c.ComboEffectIds.Count > 0), 1, bothPlayers: false));
        registry.Register(new SearchDeckOrDiscardByNameEffect("search_deck_or_discard_la_hache", "Hache"));
        registry.Register(new SearchDeckToTopDiscardEffect("search_brique_to_top_discard", c => c.Name.IndexOf("Brique", StringComparison.OrdinalIgnoreCase) >= 0));
        registry.Register(new SearchDeckToTopDiscardEffect("search_deck_to_top_discard_1", _ => true));

        // --- Jeu gratuit depuis Deck / Main / Défausse / Ban ---
        registry.Register(new PlayFreeCharFromZoneEffect("play_char_free", ZoneSource.Hand, millSelfFirst: 3, stripEffects: false));
        registry.Register(new PlayFreeCharFromZoneEffect("play_char_free_no_effects", ZoneSource.Hand, millSelfFirst: 0, stripEffects: true));
        registry.Register(new PlayFreeCharFromZoneEffect("double_combo_play_free_cost_ge_2", ZoneSource.Hand, millSelfFirst: 0, stripEffects: false, filter: c => S3S5Bridge.GetCardCost(c) >= 2));
        registry.Register(new PlayFreeCharFromZoneEffect("play_free_char_with_icon_from_hand", ZoneSource.Hand, millSelfFirst: 0, stripEffects: false, filter: c => c.EffectIds.Count > 0 || c.ComboEffectIds.Count > 0));
        registry.Register(new PlayFreeCharFromZoneEffect("play_free_up_to_1_robot_hand", ZoneSource.Hand, millSelfFirst: 0, stripEffects: false, filter: c => c.Name.IndexOf("Robot", StringComparison.OrdinalIgnoreCase) >= 0));
        registry.Register(new PlayFreeMultiFromHandEffect("play_free_up_to_3_garagistes_hand", c => c.Name.IndexOf("Garagiste", StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf("Pompiste", StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf("Mécanicien", StringComparison.OrdinalIgnoreCase) >= 0, 3));
        registry.Register(new PlayRandomHandCardFreeEffect("play_random_hand_card_free", targetOpponent: false, stripEffects: false));
        registry.Register(new PlayFreeConditionalOnSelfEffect("play_free_if_all_own_chars_force_gt_2", PlaySelfCond.AllOwnCharsForceGt2));
        registry.Register(new PlayFreeConditionalOnSelfEffect("play_free_if_dessin_romantique_in_play", PlaySelfCond.DessinRomantiqueInPlay));
        registry.Register(new PlayFreeConditionalOnSelfEffect("play_free_if_empty_terrain", PlaySelfCond.EmptyTerrain));
        registry.Register(new PlayFreeCharFromZoneEffect("play_banish_char_free", ZoneSource.Banish, millSelfFirst: 0, stripEffects: false));
        registry.Register(new PlayFreeCharFromZoneEffect("play_free_robot_from_discard", ZoneSource.Discard, millSelfFirst: 0, stripEffects: false, filter: c => c.Name.IndexOf("Robot", StringComparison.OrdinalIgnoreCase) >= 0));
        registry.Register(new PlayDiscardAfterPutHandTopDeckEffect("play_discard_char_no_effects"));
        registry.Register(new PutHandCardOnTopDeckEffect("put_hand_card_top_deck"));

        registry.Register(new PlayFreeFromDeckPredicateEffect("play_homonym_from_deck", (c, src) => string.Equals(c.Name, src.Name, StringComparison.OrdinalIgnoreCase), forceZero: false, maxCount: 1));
        registry.Register(new PlayFreeFromDeckPredicateEffect("play_prostituee_from_deck", (c, _) => c.Name.IndexOf("Prostitu", StringComparison.OrdinalIgnoreCase) >= 0, forceZero: false, maxCount: 1));
        registry.Register(new PlayFreeFromDeckPredicateEffect("play_detenu_from_deck_free", (c, _) => c.Name.IndexOf("Détenu", StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf("Prisonnier", StringComparison.OrdinalIgnoreCase) >= 0, forceZero: false, maxCount: 1));
        registry.Register(new PlayFreeFromDeckPredicateEffect("search_deck_inoxtag_play_free", (c, _) => c.Name.IndexOf("Inoxtag", StringComparison.OrdinalIgnoreCase) >= 0, forceZero: false, maxCount: 1));
        registry.Register(new PlayFreeFromDeckPredicateEffect("search_and_play_all_peepo_free", (c, _) => c.Name.IndexOf("Peepo", StringComparison.OrdinalIgnoreCase) >= 0, forceZero: false, maxCount: 10));
        registry.Register(new PlayFreeFromDeckPredicateEffect("play_laink_from_deck_force_0", (c, _) => string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase), forceZero: true, maxCount: 1));
        registry.Register(new PlayFreeFromDeckPredicateEffect("play_terracid_from_deck_force_0", (c, _) => string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase), forceZero: true, maxCount: 1));
        registry.Register(new BanishHandAndPlayFromDeckEffect("banish_hand_1_play_gobelin_from_deck", "Gobelin"));
        registry.Register(new BanishSelfAndPlayFromDeckEffect("banish_self_play_bebe_from_deck_active", "Bébé"));
        registry.Register(new PlayTwoSameCostPayOneEffect("play_two_same_cost_pay_one"));
        registry.Register(new PlayOnRandomSlotEffect("play_on_random_slot"));

        registry.Register(new OppPlayFromZoneNoEffectsEffect("opp_play_discard_char_no_effects", ZoneSource.Discard, stripEffects: true));
        registry.Register(new PlayRandomHandCardFreeEffect("opp_play_random_hand_card", targetOpponent: true, stripEffects: false));
        registry.Register(new PlayRandomHandCardFreeEffect("opp_play_random_hand_char_no_effects", targetOpponent: true, stripEffects: true));
        registry.Register(new OppRandomHandToOwnDeckEffect("opp_random_hand_to_own_deck"));

        registry.Register(new RevealDeckEdgeAndPlayFreeEffect("look_top_deck_1_play_free", fromTop: true));
        registry.Register(new RevealDeckEdgeAndPlayFreeEffect("look_bottom_deck_1_play_free", fromTop: false));
        registry.Register(new RevealDeckEdgeAndPlayFreeEffect("guess_cost_reveal_top_play_free", fromTop: true));
        registry.Register(new RevealDeckEdgeAndPlayFreeEffect("guess_top_deck_name_play_free", fromTop: true));

        // --- Défausse / Ban / Deck / Face cachée ---
        registry.Register(new TakeBanishedCardEffect("take_banished_1", 1));
        registry.Register(new TakeFilteredDiscardS3S5Effect("take_discard_2_force_0_chars", c => c.Kind == CardKind.Character && c.Force == 0, 2));
        registry.Register(new TakeFilteredDiscardS3S5Effect("take_discard_pompiste_1", c => c.Name.IndexOf("Pompiste", StringComparison.OrdinalIgnoreCase) >= 0, 1));
        registry.Register(new TakeFilteredDiscardS3S5Effect("take_discard_poule_or_coq_1", c => c.Name.IndexOf("Poule", StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf("Coq", StringComparison.OrdinalIgnoreCase) >= 0, 1));
        registry.Register(new TakeTopDiscardByConditionEffect("if_first_on_slot_take_top_discard_2", TakeTopCond.FirstOnSlot, 2));
        registry.Register(new TakeTopDiscardByConditionEffect("take_top_discard_3_if_played_by_effect", TakeTopCond.Always, 3));
        registry.Register(new TakeTopDiscardByConditionEffect("take_top_discard_per_cowboy", TakeTopCond.CowboysInPlay, 1));
        registry.Register(new TakeTopDiscardByConditionEffect("take_top_discard_per_own_row_max_3", TakeTopCond.OwnCharsOnSlotMax3, 1));
        registry.Register(new TakeTopDiscardByConditionEffect("take_top_discard_per_effect_this_turn", TakeTopCond.EffectsThisTurn, 1));
        registry.Register(new PutBottomDiscardToTopDeckIfRightmostEffect("if_rightmost_slot_return_bottom_discard_2_to_deck", 2));
        registry.Register(new SearchDiscardToTopDeckEffect("search_discard_to_top_deck_1"));

        registry.Register(new ReturnBanishedUnderDiscardEffect("return_banished_under_discard"));
        registry.Register(new ReturnFilteredDiscardToBottomDeckEffect("return_discard_mexicain_bottom_deck", c => c.Name.IndexOf("Mexicain", StringComparison.OrdinalIgnoreCase) >= 0));
        registry.Register(new ReturnFilteredDiscardToBottomDeckEffect("return_samourai_discard_to_bottom_deck", c => c.Name.IndexOf("Samoura", StringComparison.OrdinalIgnoreCase) >= 0));
        registry.Register(new ShuffleDiscardCardToDeckEffect("shuffle_discard_1_to_deck"));
        registry.Register(new ShuffleHandIntoDeckEffect("shuffle_hand_into_deck", drawAfter: 0, scoreAfter: true));
        registry.Register(new ShuffleHandIntoDeckIfUnstackedEffect("shuffle_hand_draw_5_if_unstacked", 5));

        registry.Register(new LookTopDeckTakeAndBottomEffect("look_2_take_1_put_1_bottom", 2, 1));
        registry.Register(new LookTopDeckTakeAndBottomEffect("look_top_5_take_2_bottom_3", 5, 2));
        registry.Register(new LookTopDeck5ReorderEffect("look_top_deck_5_reorder", 5));
        registry.Register(new LookOppTop3Discard1ReorderEffect("look_opp_top_3_discard_1_reorder"));

        registry.Register(new PutTopDeckFacedownUnderSelfEffect("put_top_deck_facedown_under_self"));
        registry.Register(new TakeFacedownToHandEffect("take_facedown_to_hand_1"));
        registry.Register(new DiscardFacedownInPlayEffect("discard_2_facedown_in_play", 2));

        // --- Suppression / Bounce / Déplacement / Échange sur plateau ---
        registry.Register(new DiscardOppStrongestCharEffect("discard_opp_strongest_char"));
        registry.Register(new DiscardRelativeForceCharEffect("discard_any_char_force_lt_self", anySide: true, moveAfter: false));
        registry.Register(new DiscardRelativeForceCharEffect("discard_char_lt_self_on_slot_and_move", anySide: false, moveAfter: true));
        registry.Register(new DiscardOppCharByDynamicMaxForceEffect("discard_opp_char_by_banish_max_6", DynamicForceMode.BanishMax6));
        registry.Register(new DiscardOppCharByDynamicMaxForceEffect("discard_opp_char_cost_lt_cowboys", DynamicForceMode.CostLtCowboys));
        registry.Register(new DiscardOppCharByDynamicMaxForceEffect("discard_opp_char_lt_combined_motard_force", DynamicForceMode.CombinedMotardForce));
        registry.Register(new DiscardOppCharsTotalForceEqCountEffect("discard_opp_chars_total_force_eq_own_row_count"));
        registry.Register(new DiscardOppCharsTotalForceEqCountEffect("discard_opp_chars_total_force_eq_own_slot_count"));

        registry.Register(new DiscardAllOtherCharsEffect("discard_all_other_chars_mill_self_per_discarded", allSlots: true, ownOnly: false, millSelf: true));
        registry.Register(new DiscardAllOtherCharsEffect("discard_all_other_chars_on_slot_mill_self_per_discarded", allSlots: false, ownOnly: false, millSelf: true));
        registry.Register(new DiscardAllOtherCharsEffect("discard_all_other_chars_on_slot", allSlots: false, ownOnly: false, millSelf: false));
        registry.Register(new DiscardAllOtherCharsEffect("discard_own_other_chars_on_slot", allSlots: false, ownOnly: true, millSelf: false));
        registry.Register(new DiscardAllOtherCharsEffect("require_discard_all_own_chars_on_slot", allSlots: false, ownOnly: true, millSelf: false));
        registry.Register(new DiscardOwnCharBonusEffect("discard_own_char_gain_force", gainForce: true));
        registry.Register(new DiscardOwnCharBonusEffect("discard_own_char_return_discard_to_deck_by_force", gainForce: false));

        registry.Register(new ConditionalBoardRemovalEffect("if_fewer_won_terrains_discard_opp_force_le_4", banish: false, maxForce: 4));
        registry.Register(new ConditionalBoardRemovalEffect("if_fewer_won_terrains_banish_opp_char", banish: true, maxForce: int.MaxValue));
        registry.Register(new RowTargetedBoardRemovalEffect("double_combo_banish_opp_char_row_1", banish: true, rowIndex: 0));
        registry.Register(new RowTargetedBoardRemovalEffect("double_combo_banish_opp_char_row_2", banish: true, rowIndex: 1));
        registry.Register(new RowTargetedBoardRemovalEffect("double_combo_discard_opp_char_any", banish: false, rowIndex: -1));

        registry.Register(new BounceOppCharsByPrintedForce0CountEffect("bounce_opp_chars_by_own_printed_force_0_count"));
        registry.Register(new BanishOwnCharAndScoreEffect("banish_own_char_score_terrain"));

        registry.Register(new BunkerMove3DiscardRestEffect("bunker_move_3_discard_rest"));
        registry.Register(new MoveAllOtherCharsOnSlotEffect("move_all_other_chars_on_slot_keep_order"));
        registry.Register(new MoveCost0CharsHereEffect("move_cost_0_chars_here"));
        registry.Register(new MoveCharAndDrawForceMax4Effect("move_char_and_draw_force_max_4"));
        registry.Register(new MoveOppCharToThisSlotEffect("move_opp_char_to_this_slot"));
        registry.Register(new MoveOppCharToThisSlotEffect("if_swapped_move_opp_char"));
        registry.Register(new MoveAllyToOppReturnDiscardEffect("move_ally_to_opp_return_discard_3", 3));
        registry.Register(new MoveSelfAndAllyEffect("move_self_and_ally"));

        registry.Register(new SwapTwoOwnCharsInPlayEffect("swap_two_own_chars_in_play"));
        registry.Register(new SwapSelfWithTopDiscardCharEffect("swap_self_with_top_discard_char"));
        registry.Register(new SwapOppCharWithOppDiscardEffect("swap_opp_char_with_opp_discard"));
        registry.Register(new SwapTerrainWithDiscardEffect("swap_terrain_with_discard"));
        registry.Register(new ShuffleCharToDeckEffect("shuffle_opp_char_to_deck", bothSides: false));
        registry.Register(new ShuffleCharToDeckEffect("shuffle_both_1_char_to_deck", bothSides: true));

        // --- Force / Modificateurs / Dés / Score / Spéciaux ---
        registry.Register(new BanishHand2DoubleSelfForceEffect("banish_hand_2_double_self_force"));
        registry.Register(new ModifyCharForceAndConditionalDiscardEffect("boost_opp_char_5_discard_if_10", delta: 5, discardIfGe: 10, discardIfLe: null));
        registry.Register(new ModifyCharForceAndConditionalDiscardEffect("debuff_opp_char_3_discard_if_0", delta: -3, discardIfGe: null, discardIfLe: 0));
        registry.Register(new BoostOtherCharEffect("boost_other_ally_3", 3, ownOnly: true, scoreAfter: false));
        registry.Register(new BoostOtherCharEffect("boost_other_char_2", 2, ownOnly: false, scoreAfter: true));
        registry.Register(new DebuffSlotCharsEffect("debuff_slot_chars_1", 1));
        registry.Register(new DoubleOppCharForceEffect("double_opp_char_force"));
        registry.Register(new StealOppCharForceEffect("steal_opp_char_force_until_opp_end"));
        registry.Register(new ZeroOppCharForceEffect("negate_opp_char_force_turn"));
        registry.Register(new ZeroOppCharForceEffect("ignore_opp_char_force_turn"));
        registry.Register(new MultiplySlotForceByCombosEffect("multiply_slot_force_by_combos"));
        registry.Register(new BoostSelfByNameInPlayEffect("boost_self_1_per_chien_in_play", "Chien"));
        registry.Register(new BoostSelfByNameInPlayEffect("boost_self_1_per_viking_in_play", "Viking"));
        registry.Register(new DiscardNamedAllyBoostEffect("with_aigle_discard_self_boost_aigle_5", discardSelf: true, targetName: "Aigle", bonus: 5));
        registry.Register(new DiscardNamedAllyBoostEffect("with_ver_de_terre_discard_ver_boost_self_5", discardSelf: false, targetName: "Ver de Terre", bonus: 5));

        registry.Register(new RollDiceEffect("roll_dice_boost_self", RollDiceMode.BoostSelf));
        registry.Register(new RollDiceEffect("roll_dice_discard_opp_char", RollDiceMode.DiscardOppChar));

        registry.Register(new CoinFlipEffect("coin_flip_discard_opp_char", CoinFlipMode.DiscardOppChar));
        registry.Register(new CoinFlipEffect("coin_flip_play_char_free", CoinFlipMode.PlayCharFree));

        registry.Register(new ScoreMultipleTerrainsEffect("score_2_other_terrains", 2, excludeCurrent: true));
        registry.Register(new ScoreMultipleTerrainsEffect("score_2_terrains", 2, excludeCurrent: false));
        registry.Register(new ScoreConditionalEffect("score_if_not_unstacked_this_turn", ScoreCondMode.AlwaysAllow));
        registry.Register(new ScoreConditionalEffect("score_if_unstacked_this_turn", ScoreCondMode.AlwaysAllow));
        registry.Register(new ScoreConditionalEffect("score_terrain_fewer_chars_than_opp", ScoreCondMode.FewerCharsThanOpp));
        registry.Register(new ScoreConditionalEffect("score_terrain_force_lt_7", ScoreCondMode.ForceLt7));
        registry.Register(new ScoreOppWinsPointEffect("score_opp_wins_point"));

        registry.Register(new ApplyAnyTerrainWinEffect("apply_any_terrain_win_effect"));
        registry.Register(new Mill2OppApplyLosingEffect("mill_2_opp_apply_losing_effect"));
        registry.Register(new OppGainScoreAndReturnDiscardEffect("opp_gain_score_and_return_discard_5", 5));
        registry.Register(new DisableTerrainWinLoseEffectsEffect("disable_terrain_win_lose_effects"));

        registry.Register(new ExecuteHandCharEffect("execute_hand_char_non_perm_non_scorer_effect"));
        registry.Register(new MarkerEffect("double_effects_this_turn"));
        registry.Register(new ExtraPlayThisTurnEffect("extra_play_this_turn", selfMillFirst: 0));
        registry.Register(new ExtraPlayThisTurnEffect("self_mill_2_extra_play", selfMillFirst: 2));
        registry.Register(new MarkerEffect("redirect_effect_to_self"));
        registry.Register(new MarkerEffect("link_char_banish_on_leave"));

        registry.Register(new BothRevealStealOneEffect("with_collectionneur_both_reveal_steal_1"));
        registry.Register(new RevealTwoDiscardOppEffect("reveal_2_highest_force_discard_opp", highestWins: true));
        registry.Register(new RevealTwoDiscardOppEffect("reveal_2_lowest_force_discard_opp", highestWins: false));
        registry.Register(new NameCardOppHandPlayFreeEffect("name_card_if_in_opp_hand_play_free"));
        registry.Register(new NameForceOppDiscardMatchingEffect("name_force_opp_discard_matching_or_discard_hand"));
        registry.Register(new OppChooseTakeOrDiscardCharsEffect("opp_choose_take_discard_or_discard_chars"));
        registry.Register(new OppGuessParityDiscardOppCharEffect("opp_guess_parity_discard_opp_char"));

        // =========================================================================
        // 9. Marqueurs passifs S3-S5 (évalués dynamiquement par TerrainSlot / DuelEngine)
        // =========================================================================
        string[] passiveMarkers =
        {
            // Core S3-S5 passives
            "passive_terrain_win_most_characters",
            "passive_terrain_double_win_if_diff_4",
            "passive_terrain_remove_laink_gems",
            "passive_terrain_remove_terracid_gems",
            "passive_cannot_leave_discard",
            "passive_discard_force_zero_chars",
            "passive_terrain_grant_orange_gems",
            "passive_terrain_grant_violet_gems",
            "passive_gain_violet_gems_if_dog",
            "passive_boost_if_pig_2",
            "passive_autoscore_threshold_16",
            "passive_opp_autoscore_plus_4",
            "passive_force_per_hand_card_minus_1",
            "passive_force_per_opp_hand_card_minus_1",
            "passive_cost_ouvrier_minus_1",
            "passive_cost_ouvrier_opp_plus_1",
            "passive_cost_set_1",
            "passive_start_turn_mill_per_char",
            "passive_cost_opp_plus_2",
            "passive_no_discard_when_alone",
            "passive_forbid_no_gem_chars",
            "passive_cannot_discard_or_move_gem_connected",
            "passive_force_per_gem_chain_1",
            "passive_double_force_on_poubelle_win",
            // Additional S3 / S4 / S5 passives from effects_s3/s4/s5.json
            "passive_add_jakov_force_if_in_discard",
            "passive_add_richus_force_if_in_discard",
            "passive_add_top_discard_force",
            "passive_alias_chien",
            "passive_alias_laink",
            "passive_alias_poule",
            "passive_alias_robot",
            "passive_alias_soldat_allemand_ww2",
            "passive_alias_soldat_americain_ww2",
            "passive_alias_terracid",
            "passive_alias_viking",
            "passive_all_chars_minus_1",
            "passive_all_own_chars_minus_2",
            "passive_autoscore_plus_1_per_10_discard",
            "passive_autoscore_plus_1_per_won_terrain",
            "passive_autoscore_threshold_18",
            "passive_banish_from_discard_cancel_effect",
            "passive_banish_from_hand_free_play",
            "passive_banish_hand_1_search_copy_on_slot",
            "passive_banish_if_left_after_robot_play",
            "passive_banish_self_to_score_if_not_played_this_turn",
            "passive_boost_1_per_3_opp_banished",
            "passive_boost_1_per_enchaines",
            "passive_boost_1_per_opp_hand_card",
            "passive_boost_1_per_opp_laink_on_slot",
            "passive_boost_1_per_opp_terracid_on_slot",
            "passive_boost_1_per_unique_own_char_name",
            "passive_boost_1_per_won_terrain",
            "passive_boost_2_if_between_chars",
            "passive_boost_2_if_discard_10_plus",
            "passive_boost_2_if_leftmost_slot",
            "passive_boost_2_per_a_modeler_on_slot",
            "passive_boost_2_per_facedown_in_play",
            "passive_boost_2_per_opp_row",
            "passive_boost_2_per_robot_in_discard",
            "passive_boost_2_to_printed_force_0_chars",
            "passive_boost_3_if_rightmost_and_3_chars",
            "passive_boost_3_to_char_above",
            "passive_boost_3_to_char_below",
            "passive_boost_4_if_no_facedown_under",
            "passive_boost_4_if_rightmost_and_3_chars",
            "passive_boost_4_if_slot_force_11",
            "passive_boost_5_if_no_facedown_under",
            "passive_boost_boys_band_1",
            "passive_boost_char_discard_effect_1",
            "passive_boost_equal_char_above",
            "passive_boost_equal_char_below",
            "passive_boost_per_brique_pair_discard_1",
            "passive_boost_per_sexy_or_prostituee_1",
            "passive_boost_per_surfeur_1",
            "passive_boost_self_2",
            "passive_boost_soldat_espace_terracid_2",
            "passive_boost_text_1_line_2",
            "passive_boost_text_3_lines_1",
            "passive_cancel_other_samourai_permanents",
            "passive_cancel_own_permanent",
            "passive_cannot_move_or_discard_by_effect",
            "passive_cap_hand_5",
            "passive_combos_same_name_only",
            "passive_considered_soldat_allemand_ww2",
            "passive_considered_soldat_americain_ww2",
            "passive_considered_viking",
            "passive_cost_3_untargetable",
            "passive_cost_force_le_4_minus_1",
            "passive_cost_minus_1_if_all_others_higher_force",
            "passive_count_as_laink",
            "passive_count_as_terracid",
            "passive_counter_lower_force_opp_char_by_self_discard",
            "passive_discard_bebe_play_free",
            "passive_discard_cancel_lose_effect",
            "passive_discard_cancel_win_effect",
            "passive_discard_is_banished",
            "passive_discard_reduce_both_scores_1",
            "passive_discard_self_cancel_opp_combo",
            "passive_discard_self_play_free_char",
            "passive_double_force_bonuses",
            "passive_double_force_if_horty",
            "passive_draw_may_take_top_discard",
            "passive_end_opp_turn_discard_if_lt_3_chars",
            "passive_end_turn_discard_opp_char",
            "passive_end_turn_give_control_to_opp",
            "passive_first_char_play_copy_from_deck",
            "passive_forbid_if_4_cavaliers",
            "passive_forbid_score_if_played_this_turn",
            "passive_force_5_on_play_turn",
            "passive_force_6_on_play_turn",
            "passive_force_not_counted_in_score",
            "passive_force_not_counted_in_scores",
            "passive_force_not_counted_on_score",
            "passive_free_play_if_banish_not_empty",
            "passive_if_hand_1_draw_2",
            "passive_if_no_facedown_forbid_opp_take_discard",
            "passive_if_only_2_chars_on_row_discard_other",
            "passive_ignore_force_on_own_turn",
            "passive_immune_to_sub",
            "passive_max_chars_10",
            "passive_mill_minus_1",
            "passive_minus_1_per_laink_discard_if_alone",
            "passive_minus_1_per_terracid_discard_if_alone",
            "passive_name_is_chien",
            "passive_name_is_laink",
            "passive_name_is_terracid",
            "passive_on_any_char_played_move_self",
            "passive_on_banished_draw_2",
            "passive_on_banished_return_bottom_ban_3_to_deck",
            "passive_on_banished_shuffle_discard_3_to_deck",
            "passive_on_char_played_move_self_unvisited_terrain",
            "passive_on_combo_draw_1",
            "passive_on_combo_move_self",
            "passive_on_discard_from_hand_banish_score",
            "passive_on_discard_from_hand_draw_2",
            "passive_on_discard_from_play_draw_4",
            "passive_on_effect_draw_guess_name_draw_1",
            "passive_on_leave_play_discard_all_enchaines",
            "passive_on_lose_banish_search_rage",
            "passive_on_lose_score_banish_deck_3",
            "passive_on_lose_score_banish_self",
            "passive_on_lose_score_bounce_self",
            "passive_on_mill_banish_opp_mill_2",
            "passive_on_milled_banish_draw_2",
            "passive_on_move_apply_win_effect",
            "passive_on_move_discard_adjacent",
            "passive_on_move_self_boost_3_turn",
            "passive_on_move_take_discard_2",
            "passive_on_opp_3rd_char_debuff_2",
            "passive_on_opp_combo_draw_1",
            "passive_on_opp_play_no_combo_mill_1",
            "passive_on_own_losing_effect_mill_2",
            "passive_on_play_char_roll_dice_mill",
            "passive_on_score_add_bottom_deck_force",
            "passive_on_score_banish_top_2",
            "passive_on_score_both_discard_1_char",
            "passive_on_score_cancel_and_lock_terrain",
            "passive_on_score_discard_bounce_last_opp",
            "passive_on_score_discard_opp_char",
            "passive_on_score_exact_11_take_discard_2",
            "passive_on_score_play_free_char",
            "passive_on_score_reduce_opp_0_discard_self",
            "passive_on_score_swap_terrain_discard",
            "passive_on_score_total_18_return_discard_5",
            "passive_on_sent_from_play_to_discard_play_free_cost_ge_2",
            "passive_on_sub_played_discard_to_bottom_deck_1",
            "passive_on_terrain_cleared_draw_3",
            "passive_on_terrain_discarded_take_bottom_discard",
            "passive_on_terrain_enter_return_discard_bottom_2",
            "passive_on_terrain_played_take_top_discard",
            "passive_on_win_score_bounce_self",
            "passive_once_per_turn_add_bottom_deck_to_hand",
            "passive_once_per_turn_discard_own_play_free",
            "passive_once_per_turn_hand_to_bottom_deck",
            "passive_once_per_turn_if_no_facedown_discard_to_bottom_deck",
            "passive_once_per_turn_move_clown",
            "passive_once_per_turn_swap_higher_force_hand",
            "passive_once_turn_discard_2_double_force",
            "passive_once_turn_if_fewer_won_terrains_score",
            "passive_once_turn_on_move_score_dest",
            "passive_once_turn_spend_score_banish_char",
            "passive_only_autoscore_can_score_terrain",
            "passive_opp_cannot_double_combo",
            "passive_opp_char_loses_name_opp_draws_1",
            "passive_opp_chars_minus_1_per_line_distance",
            "passive_opp_discard_plus_1_on_win",
            "passive_opp_max_hand_4",
            "passive_opp_no_sub_on_autoscore",
            "passive_opp_no_sub_on_effect_score",
            "passive_opp_reveal_drawn",
            "passive_opp_turn_discard_self_debuff_2",
            "passive_others_copy_name_force_lose_text",
            "passive_own_turn_discard_self_boost_2",
            "passive_pay_cost_by_discarding_in_play",
            "passive_play_copy_free",
            "passive_protect_other_allies_discard",
            "passive_redirect_discard_to_opp_char",
            "passive_restrict_play_col_1",
            "passive_restrict_play_row_1",
            "passive_reveal_drawn_cards",
            "passive_start_turn_both_mill_1",
            "passive_start_turn_discard_random_cost_3",
            "passive_start_turn_reveal_hand_3",
            "passive_sub_cost_minus_1",
            "passive_swap_win_and_lose_on_win",
            "passive_terrain_banish_2_hand_extra_play",
            "passive_terrain_banish_after_3_turns",
            "passive_terrain_banish_instead_of_discard",
            "passive_terrain_cancel_all_char_effects",
            "passive_terrain_double_effects",
            "passive_terrain_first_play_free",
            "passive_terrain_forbid_move",
            "passive_terrain_invert_force",
            "passive_terrain_no_discard_by_effect",
            "passive_terrain_on_banish_draw_1",
            "passive_win_bounce_if_force_11",
            "passive_with_titanic_boost_3"
        };
        for (int i = 0; i < passiveMarkers.Length; i++)
        {
            registry.Register(new MarkerEffect(passiveMarkers[i]));
        }
    }

    // =========================================================================
    // Helpers internes partagés & État auxiliaire S3-S5
    // =========================================================================
    internal sealed class S3S5AuxState
    {
        public readonly Dictionary<string, List<DuelCard>> FacedownByCardId = new(StringComparer.Ordinal);
        public int CombosTrackedThisTurn;
        public int EffectsTrackedThisTurn;
        public int LastTurn = -1;
    }

    private static readonly ConditionalWeakTable<DuelState, S3S5AuxState> _auxByState = new();

    internal static S3S5AuxState GetAuxState(DuelState state)
    {
        var aux = _auxByState.GetOrCreateValue(state);
        if (aux.LastTurn != state.TurnNumber)
        {
            aux.LastTurn = state.TurnNumber;
            aux.CombosTrackedThisTurn = 0;
            aux.EffectsTrackedThisTurn = 0;
        }
        return aux;
    }

    internal static bool TryFindCharacterInSlots(
        DuelState state,
        PlayerId owner,
        DuelCard card,
        int preferredSlotIndex,
        out TerrainSlot? foundSlot,
        out int foundSlotIndex,
        out int foundCharIndex)
    {
        if (preferredSlotIndex >= 0 && preferredSlotIndex < state.Slots.Count)
        {
            var slot = state.Slots[preferredSlotIndex];
            if (TryFindIndexInList(slot.GetCharacters(owner), card, out foundCharIndex))
            {
                foundSlot = slot;
                foundSlotIndex = preferredSlotIndex;
                return true;
            }
        }

        for (int s = 0; s < state.Slots.Count; s++)
        {
            if (s == preferredSlotIndex) continue;
            var slot = state.Slots[s];
            if (TryFindIndexInList(slot.GetCharacters(owner), card, out foundCharIndex))
            {
                foundSlot = slot;
                foundSlotIndex = s;
                return true;
            }
        }

        foundSlot = null;
        foundSlotIndex = -1;
        foundCharIndex = -1;
        return false;
    }

    private static bool TryFindIndexInList(IReadOnlyList<DuelCard> list, DuelCard card, out int index)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], card))
            {
                index = i;
                return true;
            }
        }
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (string.Equals(list[i].Id, card.Id, StringComparison.Ordinal))
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    internal static void ReplaceCharacterAtIndex(TerrainSlot slot, PlayerId owner, DuelCard oldCard, DuelCard newCard, int index)
    {
        if (slot.RemoveCharacter(owner, oldCard))
        {
            int clamped = Math.Max(0, Math.Min(index, slot.GetCharacters(owner).Count));
            slot.AddCharacter(owner, newCard, clamped);
        }
    }

    internal static DuelCard CloneCard(
        DuelCard src,
        int? force = null,
        bool? isScoreur = null,
        IReadOnlyList<string>? effectIds = null,
        IReadOnlyList<string>? comboEffectIds = null,
        bool? isComboScoreur = null,
        GemMask? gemMask = null)
    {
        return new DuelCard(
            src.Id,
            src.Name,
            src.Kind,
            force ?? src.Force,
            isScoreur ?? src.IsScoreur,
            effectIds ?? src.EffectIds,
            src.Effigy,
            comboEffectIds ?? src.ComboEffectIds,
            isComboScoreur: isComboScoreur ?? src.IsComboScoreur,
            gemMask: gemMask ?? src.Gems);
    }

    internal static void PutCardOnTopOfDeck(DuelState state, PlayerId player, DuelCard card)
    {
        int existingCount = state.GetDeck(player).Count;
        state.AddCardToBottomDeck(player, card);
        for (int i = 0; i < existingCount; i++)
        {
            var top = state.PopTopDeckCard(player);
            if (top != null)
            {
                state.AddCardToBottomDeck(player, top);
            }
        }
    }

    internal static void SelectCandidateOrAuto(
        EffectContext context,
        string title,
        string description,
        IReadOnlyList<DuelCard> candidates,
        Action<DuelCard> apply,
        Func<IReadOnlyList<DuelCard>, DuelCard>? aiSelector = null)
    {
        if (candidates.Count == 0) return;
        if (candidates.Count == 1)
        {
            apply(candidates[0]);
            return;
        }

        bool handled = false;
        context.Engine.Emit(new PlayerChoiceRequiredEvent(
            context.Player,
            title,
            description,
            candidates,
            chosen =>
            {
                if (chosen == null) return;
                handled = true;
                apply(chosen);
            }));

        if (!handled && context.Player == PlayerId.Player2)
        {
            var fallback = aiSelector != null ? aiSelector(candidates) : candidates[0];
            apply(fallback);
        }
    }

    internal static void SelectSlotOrAuto(
        EffectContext context,
        string title,
        string description,
        IReadOnlyList<int> validSlots,
        Action<int> apply)
    {
        if (validSlots.Count == 0) return;
        if (validSlots.Count == 1)
        {
            apply(validSlots[0]);
            return;
        }

        bool handled = false;
        context.Engine.Emit(new PlayerSlotChoiceRequiredEvent(
            context.Player,
            title,
            description,
            chosenSlot =>
            {
                handled = true;
                int resolved = validSlots.Contains(chosenSlot) ? chosenSlot : validSlots[0];
                apply(resolved);
            }));

        if (!handled && context.Player == PlayerId.Player2)
        {
            apply(validSlots[0]);
        }
    }

    internal static List<int> GetOccupiedSlotIndices(DuelState state, int excludeSlotIndex = -1)
    {
        var result = new List<int>();
        for (int i = 0; i < state.Slots.Count; i++)
        {
            if (i == excludeSlotIndex) continue;
            if (!state.Slots[i].IsEmpty) result.Add(i);
        }
        return result;
    }

    internal static void PlaceCharacterFreeOnBoard(DuelEngine engine, PlayerId player, DuelCard card, int preferredSlot = -1)
    {
        var slots = GetOccupiedSlotIndices(engine.State);
        if (slots.Count == 0)
        {
            engine.State.AddCardToDiscard(player, card);
            return;
        }
        int target = (preferredSlot >= 0 && slots.Contains(preferredSlot)) ? preferredSlot : slots[0];
        engine.State.AddCharacterToSlot(target, player, card);
        engine.Emit(new CharacterPlayedEvent(player, target, card));
        engine.CheckDeckOutCondition(player);
    }

    internal static int CountAllCombosInPlay(DuelState state)
    {
        int total = 0;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var slot = state.Slots[s];
            if (slot.IsEmpty) continue;
            var p1 = slot.CharactersP1;
            for (int i = 1; i < p1.Count; i++)
                if (ComboEvaluator.IsComboActive(p1, i)) total++;
            var p2 = slot.CharactersP2;
            for (int i = 1; i < p2.Count; i++)
                if (ComboEvaluator.IsComboActive(p2, i)) total++;
        }
        return total;
    }
}

/// <summary>
/// Pont d'accès léger et mis en cache vers les ajouts éventuels de DuelCard / DuelEngine.
/// </summary>
internal static class S3S5Bridge
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly PropertyInfo? CardCostProp = typeof(DuelCard).GetProperty("Cost", AnyInstance);
    private static readonly FieldInfo? EngineRngField = typeof(DuelEngine).GetField("_rng", AnyInstance);
    private static readonly MethodInfo? EngineAwardMethod = typeof(DuelEngine).GetMethod("AwardExtraPoint", AnyInstance);

    public static int GetCardCost(DuelCard card)
    {
        if (CardCostProp != null && CardCostProp.GetValue(card) is int c)
            return c;
        return 0;
    }

    public static int RollNext(DuelEngine engine, int minInclusive, int maxExclusive)
    {
        if (EngineRngField?.GetValue(engine) is IRandom rng)
            return rng.Next(minInclusive, maxExclusive);
        return minInclusive;
    }

    public static void AwardExtraPoint(DuelEngine engine, PlayerId player, int points = 1)
    {
        if (EngineAwardMethod != null)
        {
            EngineAwardMethod.Invoke(engine, new object[] { player, points });
            return;
        }
        engine.State.AddScore(player, points);
        if (engine.State.GetScore(player) >= engine.Rules.TerrainsToWin && !engine.State.IsGameOver)
        {
            engine.State.SetGameOver(player, GameOverReason.FiveTerrains);
            engine.Emit(new DuelEndedEvent(player, GameOverReason.FiveTerrains));
        }
    }
}

// =============================================================================
// 1. Position dans la ligne
// =============================================================================
/// <summary>Pioche X cartes où X est la position (1-based, max 6) de SourceCard dans sa ligne.</summary>
public sealed class DrawByLinePositionEffect : IEffect
{
    public string EffectId { get; }
    public DrawByLinePositionEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out _, out _, out int charIdx))
        {
            int pos = Math.Min(6, charIdx + 1);
            if (pos > 0) context.Engine.DrawCards(context.Player, pos);
        }
        return DuelActionResult.Ok();
    }
}

/// <summary>Fait meuler X cartes à l'adversaire où X est la position (1-based, max 6) de SourceCard dans sa ligne.</summary>
public sealed class MillOpponentByLinePositionEffect : IEffect
{
    public string EffectId { get; }
    public MillOpponentByLinePositionEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out _, out _, out int charIdx))
        {
            int pos = Math.Min(6, charIdx + 1);
            if (pos > 0) context.Engine.MillCards(context.Player.Opponent(), pos);
        }
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// 2. Chaînes de gemmes
// =============================================================================
/// <summary>Place X cartes du dessus de la défausse sur le dessus du deck (X = personnages entre-reliés par gemmes sur cette ligne).</summary>
public sealed class DiscardToTopDeckByGemChainEffect : IEffect
{
    public string EffectId { get; }
    public DiscardToTopDeckByGemChainEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        TerrainSlot? slot = null;
        if (context.SlotIndex >= 0 && context.SlotIndex < context.Engine.State.Slots.Count)
            slot = context.Engine.State.Slots[context.SlotIndex];
        else if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var foundSlot, out _, out _))
            slot = foundSlot;

        if (slot == null) return DuelActionResult.Ok();

        var chars = slot.GetCharacters(context.Player);
        int chainCount = 0;
        for (int i = 0; i < chars.Count; i++)
        {
            bool connectedLeft = i > 0 && ComboEvaluator.IsComboActive(chars, i);
            bool connectedRight = i + 1 < chars.Count && ComboEvaluator.IsComboActive(chars, i + 1);
            if (connectedLeft || connectedRight) chainCount++;
        }
        if (chainCount <= 0) return DuelActionResult.Ok();

        var discard = context.Engine.State.GetDiscard(context.Player);
        int toMove = Math.Min(chainCount, discard.Count);
        var moved = new List<DuelCard>(toMove);
        for (int i = 0; i < toMove; i++)
        {
            var topDiscard = discard[discard.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, topDiscard);
            moved.Add(topDiscard);
        }
        for (int i = moved.Count - 1; i >= 0; i--)
            EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, moved[i]);

        return DuelActionResult.Ok();
    }
}

// =============================================================================
// 3. Gemmes & Annulation
// =============================================================================
/// <summary>Supprime les gemmes d'ouverture d'un personnage adverse en jeu.</summary>
public sealed class RemoveOpponentOpeningGemTurnEffect : IEffect
{
    public string EffectId { get; }
    public RemoveOpponentOpeningGemTurnEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var opponent = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var slot in context.Engine.State.Slots)
        {
            if (slot.IsEmpty) continue;
            foreach (var c in slot.GetCharacters(opponent))
                if (c.HasOpeningGem) candidates.Add(c);
        }
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Supprimer gemme d'ouverture",
            "Choisissez un personnage adverse dont la gemme d'ouverture sera supprimée",
            candidates,
            chosen =>
            {
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opponent, chosen, -1, out var targetSlot, out _, out int idx) && targetSlot != null)
                {
                    var newMask = chosen.Gems & ~(GemMask.OpeningOrange | GemMask.OpeningViolet);
                    EffectsS3S5.ReplaceCharacterAtIndex(targetSlot, opponent, chosen, EffectsS3S5.CloneCard(chosen, gemMask: newMask), idx);
                }
            });
        return DuelActionResult.Ok();
    }
}

/// <summary>Supprime toutes les gemmes d'un personnage allié et lui confère +1 Force par gemme supprimée.</summary>
public sealed class RemoveOwnGemsBoostPerGemEffect : IEffect
{
    public string EffectId { get; }
    public RemoveOwnGemsBoostPerGemEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = new List<DuelCard>();
        foreach (var slot in context.Engine.State.Slots)
        {
            if (slot.IsEmpty) continue;
            foreach (var c in slot.GetCharacters(context.Player))
                if (c.Gems != GemMask.None) candidates.Add(c);
        }
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Convertir gemmes en Force",
            "Choisissez un de vos personnages pour supprimer ses gemmes (+1 Force par gemme)",
            candidates,
            chosen =>
            {
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, chosen, -1, out var targetSlot, out _, out int idx) && targetSlot != null)
                {
                    int gemCount = CountGems(chosen);
                    EffectsS3S5.ReplaceCharacterAtIndex(targetSlot, context.Player, chosen, EffectsS3S5.CloneCard(chosen, force: chosen.Force + gemCount, gemMask: GemMask.None), idx);
                }
            },
            list => list.OrderByDescending(CountGems).First());
        return DuelActionResult.Ok();
    }

    private static int CountGems(DuelCard card)
    {
        int count = 0;
        if (card.HasOpeningOrangeGem) count++;
        if (card.HasOpeningVioletGem) count++;
        if (card.HasClosingOrangeGem) count++;
        if (card.HasClosingVioletGem) count++;
        return count;
    }
}

internal enum CancelScope { SingleOpponent, OtherAlliesOnSlot, AllOthersOnSlot }

/// <summary>Annule les effets d'un ou plusieurs personnages sur le terrain.</summary>
public sealed class CancelCharacterEffectTurnEffect : IEffect
{
    public string EffectId { get; }
    private readonly CancelScope _scope;

    internal CancelCharacterEffectTurnEffect(string effectId, CancelScope scope = CancelScope.SingleOpponent)
    {
        EffectId = effectId;
        _scope = scope;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        if (_scope != CancelScope.SingleOpponent)
        {
            if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count)
                return DuelActionResult.Ok();
            var slot = context.Engine.State.Slots[context.SlotIndex];
            if (slot.IsEmpty) return DuelActionResult.Ok();

            SilenceOthersOnSide(slot, context.Player, context.SourceCard);
            if (_scope == CancelScope.AllOthersOnSlot)
                SilenceOthersOnSide(slot, context.Player.Opponent(), context.SourceCard);
            return DuelActionResult.Ok();
        }

        var opponent = context.Player.Opponent();
        var candidates = new List<(PlayerId Owner, DuelCard Card)>();
        void CollectFromSlot(TerrainSlot s)
        {
            if (s.IsEmpty) return;
            foreach (var c in s.GetCharacters(opponent))
                if (c.EffectIds.Count > 0 || c.ComboEffectIds.Count > 0 || c.IsScoreur || c.IsComboScoreur)
                    candidates.Add((opponent, c));
            if (candidates.Count == 0)
                foreach (var c in s.GetCharacters(opponent))
                    candidates.Add((opponent, c));
        }

        if (context.SlotIndex >= 0 && context.SlotIndex < context.Engine.State.Slots.Count)
            CollectFromSlot(context.Engine.State.Slots[context.SlotIndex]);
        else
            foreach (var s in context.Engine.State.Slots) CollectFromSlot(s);

        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Annuler effets d'un personnage",
            "Choisissez un personnage dont les effets sont annulés",
            candidates.Select(x => x.Card).ToList(),
            chosen =>
            {
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opponent, chosen, context.SlotIndex, out var targetSlot, out _, out int idx) && targetSlot != null)
                {
                    var silenced = EffectsS3S5.CloneCard(chosen, isScoreur: false, effectIds: Array.Empty<string>(), comboEffectIds: Array.Empty<string>(), isComboScoreur: false);
                    EffectsS3S5.ReplaceCharacterAtIndex(targetSlot, opponent, chosen, silenced, idx);
                }
            });
        return DuelActionResult.Ok();
    }

    private static void SilenceOthersOnSide(TerrainSlot slot, PlayerId owner, DuelCard source)
    {
        var list = slot.GetCharacters(owner).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (ReferenceEquals(c, source) || string.Equals(c.Id, source.Id, StringComparison.Ordinal)) continue;
            var silenced = EffectsS3S5.CloneCard(c, isScoreur: false, effectIds: Array.Empty<string>(), comboEffectIds: Array.Empty<string>(), isComboScoreur: false);
            EffectsS3S5.ReplaceCharacterAtIndex(slot, owner, c, silenced, i);
        }
    }
}

// =============================================================================
// 4. Alignement de colonne
// =============================================================================
/// <summary>Pioche N cartes si un personnage adverse se trouve à la même position d'index sur ce terrain.</summary>
public sealed class DrawIfAlignedColumnEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public DrawIfAlignedColumnEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }

    public DuelActionResult Execute(EffectContext context)
    {
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int myIdx) && slot != null)
        {
            var oppChars = slot.GetCharacters(context.Player.Opponent());
            if (myIdx >= 0 && myIdx < oppChars.Count) context.Engine.DrawCards(context.Player, Amount);
        }
        return DuelActionResult.Ok();
    }
}

/// <summary>Confère un bonus de Force à SourceCard si un personnage adverse est aligné au même index sur ce terrain.</summary>
public sealed class BoostIfAlignedColumnEffect : IEffect
{
    public string EffectId { get; }
    public int BonusForce { get; }
    public BoostIfAlignedColumnEffect(string effectId, int bonusForce) { EffectId = effectId; BonusForce = bonusForce; }

    public DuelActionResult Execute(EffectContext context)
    {
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int myIdx) && slot != null)
        {
            var oppChars = slot.GetCharacters(context.Player.Opponent());
            if (myIdx >= 0 && myIdx < oppChars.Count)
            {
                var src = slot.GetCharacters(context.Player)[myIdx];
                EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, src, EffectsS3S5.CloneCard(src, force: src.Force + BonusForce), myIdx);
            }
        }
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// 5. Effets spéciaux du cahier des charges
// =============================================================================
/// <summary>Masque à Gaz : bannit un personnage de la main et copie sa Force, ses effets et ses gemmes.</summary>
public sealed class BanishHandCharacterCopyStatsEffect : IEffect
{
    public string EffectId { get; }
    public BanishHandCharacterCopyStatsEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var handChars = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character).ToList();
        if (handChars.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Bannir un personnage de votre main",
            "Choisissez un personnage à bannir pour copier ses caractéristiques",
            handChars,
            chosen =>
            {
                if (!context.Engine.State.RemoveCardFromHand(context.Player, chosen)) return;
                context.Engine.State.AddCardToBanish(context.Player, chosen);
                context.Engine.Emit(new CardsBanishedEvent(context.Player, new[] { chosen }));

                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int idx) && slot != null)
                {
                    var current = slot.GetCharacters(context.Player)[idx];
                    var copied = new DuelCard(
                        current.Id, current.Name, CardKind.Character,
                        chosen.Force, chosen.IsScoreur, chosen.EffectIds, chosen.Effigy, chosen.ComboEffectIds,
                        isComboScoreur: chosen.IsComboScoreur, gemMask: chosen.Gems);
                    EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, current, copied, idx);
                }
                context.Engine.CheckDeckOutCondition(context.Player);
            },
            list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

/// <summary>Si le deck du contrôleur contient moins de cartes que celui de l'adversaire, chaque joueur bannit N cartes du dessus de son deck.</summary>
public sealed class BothBanishTopIfSmallerDeckEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public BothBanishTopIfSmallerDeckEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }

    public DuelActionResult Execute(EffectContext context)
    {
        if (context.Engine.State.GetDeck(context.Player).Count < context.Engine.State.GetDeck(context.Player.Opponent()).Count)
        {
            context.Engine.BanishCardsFromDeck(PlayerId.Player1, Amount);
            if (!context.Engine.State.IsGameOver) context.Engine.BanishCardsFromDeck(PlayerId.Player2, Amount);
        }
        return DuelActionResult.Ok();
    }
}

/// <summary>Déclenche l'effet Perdant d'un terrain en jeu (pour les deux joueurs ou l'adversaire seul).</summary>
public sealed class ApplyBothLosingEffectInPlayEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _bothPlayers;

    public ApplyBothLosingEffectInPlayEffect(string effectId, bool bothPlayers = true)
    {
        EffectId = effectId;
        _bothPlayers = bothPlayers;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var slotsWithLosing = context.Engine.State.Slots
            .Where(s => !s.IsEmpty && s.Card != null && s.Card.EffectIds != null && s.Card.EffectIds.Any(e => e.StartsWith("lose_", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (slotsWithLosing.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Appliquer l'effet Perdant d'un terrain",
            "Choisissez un terrain en jeu dont l'effet Perdant sera appliqué",
            slotsWithLosing.Select(s => s.Card!).ToList(),
            chosenTerrain =>
            {
                var targetSlot = slotsWithLosing.FirstOrDefault(s => ReferenceEquals(s.Card, chosenTerrain)) ?? slotsWithLosing[0];
                if (chosenTerrain.EffectIds == null) return;
                foreach (var effId in chosenTerrain.EffectIds)
                {
                    if (!effId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase)) continue;
                    if (context.Engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
                    {
                        var firstTarget = _bothPlayers ? PlayerId.Player1 : context.Player.Opponent();
                        context.Engine.Emit(new EffectTriggeredEvent(firstTarget, effId, chosenTerrain, targetSlot.SlotIndex));
                        eff.Execute(new EffectContext(context.Engine, firstTarget, chosenTerrain, targetSlot.SlotIndex));
                        if (context.Engine.State.IsGameOver) break;

                        if (_bothPlayers)
                        {
                            context.Engine.Emit(new EffectTriggeredEvent(PlayerId.Player2, effId, chosenTerrain, targetSlot.SlotIndex));
                            eff.Execute(new EffectContext(context.Engine, PlayerId.Player2, chosenTerrain, targetSlot.SlotIndex));
                            if (context.Engine.State.IsGameOver) break;
                        }
                    }
                }
            });
        return DuelActionResult.Ok();
    }
}

/// <summary>Fin Stratège : meule N cartes et accorde +1 point de score supplémentaire si le joueur gagne un score ce tour.</summary>
public sealed class MillAndBonusNextScoreEffect : IEffect
{
    public string EffectId { get; }
    public int MillAmount { get; }
    public MillAndBonusNextScoreEffect(string effectId, int millAmount) { EffectId = effectId; MillAmount = millAmount; }

    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.MillCards(context.Player, MillAmount);
        if (context.Engine.State.IsGameOver) return DuelActionResult.Ok();

        int activeTurn = context.Engine.State.TurnNumber;
        var player = context.Player;
        var engine = context.Engine;

        void OnNextScoreOrTurnEnd(IDuelEvent evt)
        {
            if (evt is DuelEndedEvent || (evt is TurnEndedEvent turnEvt && turnEvt.TurnNumber >= activeTurn))
            {
                engine.OnEvent -= OnNextScoreOrTurnEnd;
                return;
            }
            if (evt is ScoreResolvedEvent scoreEvt && scoreEvt.Winner == player && engine.State.TurnNumber == activeTurn)
            {
                engine.OnEvent -= OnNextScoreOrTurnEnd;
                if (!engine.State.IsGameOver) S3S5Bridge.AwardExtraPoint(engine, player, 1);
            }
        }

        engine.OnEvent += OnNextScoreOrTurnEnd;
        return DuelActionResult.Ok();
    }
}

/// <summary>Pioche N cartes si le joueur a strictement plus de personnages que l'adversaire.</summary>
public sealed class DrawIfMoreCharactersThanOpponentEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public DrawIfMoreCharactersThanOpponentEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }

    public DuelActionResult Execute(EffectContext context)
    {
        var state = context.Engine.State;
        int myCount = 0, oppCount = 0;
        if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count && !state.Slots[context.SlotIndex].IsEmpty)
        {
            myCount = state.Slots[context.SlotIndex].GetCharacters(context.Player).Count;
            oppCount = state.Slots[context.SlotIndex].GetCharacters(context.Player.Opponent()).Count;
        }
        else
        {
            foreach (var s in state.Slots)
            {
                if (s.IsEmpty) continue;
                myCount += s.GetCharacters(context.Player).Count;
                oppCount += s.GetCharacters(context.Player.Opponent()).Count;
            }
        }
        if (myCount > oppCount) context.Engine.DrawCards(context.Player, Amount);
        return DuelActionResult.Ok();
    }
}

/// <summary>Fait meuler à l'adversaire autant de cartes que la taille de la main du joueur.</summary>
public sealed class MillEqualHandSizeEffect : IEffect
{
    public string EffectId { get; }
    public MillEqualHandSizeEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        int handSize = context.Engine.State.GetHand(context.Player).Count;
        if (handSize > 0) context.Engine.MillCards(context.Player.Opponent(), handSize);
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// 6. Cartes DUO & Sub
// =============================================================================
/// <summary>Joue gratuitement un personnage de la main sur un terrain en jeu sans déclencher de score.</summary>
public sealed class PlayCharacterFreeNoScoreEffect : IEffect
{
    public string EffectId { get; }
    public PlayCharacterFreeNoScoreEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var handChars = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character).ToList();
        var occupiedSlots = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State);
        if (handChars.Count == 0 || occupiedSlots.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Jouer un personnage gratuitement",
            "Choisissez un personnage à poser gratuitement",
            handChars,
            chosen =>
            {
                if (!context.Engine.State.RemoveCardFromHand(context.Player, chosen)) return;
                EffectsS3S5.SelectSlotOrAuto(
                    context,
                    "Choisissez un terrain cible",
                    $"Choisissez un terrain pour poser {chosen.Name}",
                    occupiedSlots,
                    slotIdx =>
                    {
                        context.Engine.State.AddCharacterToSlot(slotIdx, context.Player, chosen);
                        context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, chosen));
                        context.Engine.CheckDeckOutCondition(context.Player);
                    });
            });
        return DuelActionResult.Ok();
    }
}

/// <summary>Défausse un personnage en jeu dont la Force est &lt;= MaxForce.</summary>
public sealed class DiscardCharacterByMaxForceEffect : IEffect
{
    public string EffectId { get; }
    public int MaxForce { get; }
    public DiscardCharacterByMaxForceEffect(string effectId, int maxForce) { EffectId = effectId; MaxForce = maxForce; }

    public DuelActionResult Execute(EffectContext context)
    {
        var opponent = context.Player.Opponent();
        var candidates = new List<(PlayerId Owner, TerrainSlot Slot, DuelCard Card)>();

        foreach (var slot in context.Engine.State.Slots)
        {
            if (slot.IsEmpty) continue;
            foreach (var c in slot.GetCharacters(opponent))
                if (c.Force <= MaxForce) candidates.Add((opponent, slot, c));
        }
        if (candidates.Count == 0)
        {
            foreach (var slot in context.Engine.State.Slots)
            {
                if (slot.IsEmpty) continue;
                foreach (var c in slot.GetCharacters(context.Player))
                    if (c.Force <= MaxForce) candidates.Add((context.Player, slot, c));
            }
        }
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Défausser un personnage",
            MaxForce == int.MaxValue ? "Choisissez un personnage à défausser" : $"Choisissez un personnage de Force <= {MaxForce} à défausser",
            candidates.Select(x => x.Card).ToList(),
            chosen =>
            {
                var entry = candidates.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
                if (entry.Slot != null && entry.Slot.RemoveCharacter(entry.Owner, entry.Card))
                {
                    context.Engine.State.AddCardToDiscard(entry.Owner, entry.Card);
                    context.Engine.Emit(new CardsDiscardedEvent(entry.Owner, new[] { entry.Card }));
                }
            },
            list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

/// <summary>Double la Force d'un personnage allié en jeu (optionnellement après défausse d'une carte en main).</summary>
public sealed class DoubleOwnCharacterForceEffect : IEffect
{
    public string EffectId { get; }
    public bool DiscardHandCardFirst { get; }
    public DoubleOwnCharacterForceEffect(string effectId, bool discardHandCardFirst)
    {
        EffectId = effectId;
        DiscardHandCardFirst = discardHandCardFirst;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        if (DiscardHandCardFirst)
        {
            if (context.Engine.State.GetHand(context.Player).Count == 0) return DuelActionResult.Ok();
            context.Engine.DiscardFromHand(context.Player, 1);
            if (context.Engine.State.IsGameOver) return DuelActionResult.Ok();
        }

        var candidates = new List<DuelCard>();
        foreach (var slot in context.Engine.State.Slots)
            if (!slot.IsEmpty) candidates.AddRange(slot.GetCharacters(context.Player));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Doubler la Force d'un personnage",
            "Choisissez un de vos personnages dont la Force sera doublée",
            candidates,
            chosen =>
            {
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, chosen, -1, out var slot, out _, out int idx) && slot != null)
                    EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, chosen, EffectsS3S5.CloneCard(chosen, force: chosen.Force * 2), idx);
            },
            list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

/// <summary>Représente une primitive (Sub) enregistrée ; si jouée depuis la main, pose la carte sur un terrain actif.</summary>
public sealed class SubReactionMarkerEffect : IEffect
{
    public string EffectId { get; }
    public bool PlayToBoard { get; }
    public SubReactionMarkerEffect(string effectId, bool playToBoard) { EffectId = effectId; PlayToBoard = playToBoard; }

    public DuelActionResult Execute(EffectContext context)
    {
        if (!PlayToBoard) return DuelActionResult.Ok();
        if (context.Engine.State.GetHand(context.Player).Any(h => ReferenceEquals(h, context.SourceCard) || string.Equals(h.Id, context.SourceCard.Id, StringComparison.Ordinal)))
        {
            if (context.Engine.State.RemoveCardFromHand(context.Player, context.SourceCard))
                EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, context.SourceCard, context.SlotIndex);
        }
        return DuelActionResult.Ok();
    }
}

/// <summary>Sub qui pose la carte si en main puis pioche N cartes.</summary>
internal sealed class SubDrawReactionEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public SubDrawReactionEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }

    public DuelActionResult Execute(EffectContext context)
    {
        new SubReactionMarkerEffect(EffectId, playToBoard: true).Execute(context);
        context.Engine.DrawCards(context.Player, _amount);
        return DuelActionResult.Ok();
    }
}

internal sealed class SubCancelForceModsOnSlotEffect : IEffect
{
    public string EffectId { get; }
    public SubCancelForceModsOnSlotEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        new CancelCharacterEffectTurnEffect(EffectId, CancelScope.AllOthersOnSlot).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class SubReduceCost0ForceTo0Effect : IEffect
{
    public string EffectId { get; }
    public SubReduceCost0ForceTo0Effect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var opponent = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
        {
            if (s.IsEmpty) continue;
            foreach (var c in s.GetCharacters(opponent))
                if (S3S5Bridge.GetCardCost(c) == 0) candidates.Add(c);
        }
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Réduire Force à 0",
            "Choisissez un personnage de coût 0 dont la Force passe à 0",
            candidates,
            chosen =>
            {
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opponent, chosen, -1, out var slot, out _, out int idx) && slot != null)
                    EffectsS3S5.ReplaceCharacterAtIndex(slot, opponent, chosen, EffectsS3S5.CloneCard(chosen, force: 0), idx);
            },
            list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class SubSwapTwoCharsSameSideEffect : IEffect
{
    public string EffectId { get; }
    public SubSwapTwoCharsSameSideEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        new SwapTwoOwnCharsInPlayEffect(EffectId).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class SubSwapTwoTerrainsEffect : IEffect
{
    public string EffectId { get; }
    public SubSwapTwoTerrainsEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var occupied = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State);
        if (occupied.Count < 2) return DuelActionResult.Ok();
        var sA = context.Engine.State.Slots[occupied[0]];
        var sB = context.Engine.State.Slots[occupied[1]];
        var cA = sA.Card;
        var tA = sA.PlacedOnTurn;
        var pA = sA.PlacedBy;
        var p1A = sA.CharactersP1.ToList();
        var p2A = sA.CharactersP2.ToList();

        var p1B = sB.CharactersP1.ToList();
        var p2B = sB.CharactersP2.ToList();

        context.Engine.State.SetSlot(occupied[0], sB.Card, sB.PlacedOnTurn, sB.PlacedBy);
        foreach (var c in p1B) context.Engine.State.AddCharacterToSlot(occupied[0], PlayerId.Player1, c);
        foreach (var c in p2B) context.Engine.State.AddCharacterToSlot(occupied[0], PlayerId.Player2, c);

        context.Engine.State.SetSlot(occupied[1], cA, tA, pA);
        foreach (var c in p1A) context.Engine.State.AddCharacterToSlot(occupied[1], PlayerId.Player1, c);
        foreach (var c in p2A) context.Engine.State.AddCharacterToSlot(occupied[1], PlayerId.Player2, c);
        return DuelActionResult.Ok();
    }
}

internal sealed class SubMillEqualForceShuffleOppCharEffect : IEffect
{
    public string EffectId { get; }
    public SubMillEqualForceShuffleOppCharEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var opponent = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) candidates.AddRange(s.GetCharacters(opponent));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(
            context,
            "Renvoyer un personnage adverse dans son deck",
            "Choisissez un personnage adverse à renvoyer dans son deck",
            candidates,
            chosen =>
            {
                if (chosen.Force > 0) context.Engine.MillCards(context.Player, chosen.Force);
                if (!context.Engine.State.IsGameOver &&
                    EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opponent, chosen, -1, out var slot, out _, out _) && slot != null)
                {
                    if (slot.RemoveCharacter(opponent, chosen))
                    {
                        context.Engine.State.AddCardToBottomDeck(opponent, chosen);
                    }
                }
            },
            list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// 8. Implémentations réutilisables pour tous les IDs S3 / S4 / S5
// =============================================================================
internal sealed class OpponentDrawSimpleEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public OpponentDrawSimpleEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DrawCards(context.Player.Opponent(), _amount);
        return DuelActionResult.Ok();
    }
}

internal sealed class BothDrawUntilHandEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _target;
    public BothDrawUntilHandEffect(string effectId, int target) { EffectId = effectId; _target = target; }
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            int missing = _target - context.Engine.State.GetHand(p).Count;
            if (missing > 0 && !context.Engine.State.IsGameOver) context.Engine.DrawCards(p, missing);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothBanishHandEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public BothBanishHandEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.BanishCardsFromHand(PlayerId.Player1, _amount);
        if (!context.Engine.State.IsGameOver) context.Engine.BanishCardsFromHand(PlayerId.Player2, _amount);
        return DuelActionResult.Ok();
    }
}

internal sealed class BothDiscardAndDrawEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _discard;
    private readonly int _draw;
    public BothDiscardAndDrawEffect(string effectId, int discard, int draw) { EffectId = effectId; _discard = discard; _draw = draw; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DiscardFromHand(PlayerId.Player1, _discard);
        if (!context.Engine.State.IsGameOver) context.Engine.DiscardFromHand(PlayerId.Player2, _discard);
        if (!context.Engine.State.IsGameOver) context.Engine.DrawCards(PlayerId.Player1, _draw);
        if (!context.Engine.State.IsGameOver) context.Engine.DrawCards(PlayerId.Player2, _draw);
        return DuelActionResult.Ok();
    }
}

internal sealed class BothShuffleHandAndDrawEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _draw;
    public BothShuffleHandAndDrawEffect(string effectId, int draw) { EffectId = effectId; _draw = draw; }
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            if (context.Engine.State.IsGameOver) break;
            new ShuffleHandAndDrawEffect(EffectId, _draw).Execute(new EffectContext(context.Engine, p, context.SourceCard, context.SlotIndex));
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothTakeBanishEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public BothTakeBanishEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
            new TakeBanishedCardEffect(EffectId, _amount).Execute(new EffectContext(context.Engine, p, context.SourceCard, context.SlotIndex));
        return DuelActionResult.Ok();
    }
}

internal sealed class BothLookBottomPutTopEffect : IEffect
{
    public string EffectId { get; }
    public BothLookBottomPutTopEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            var bottom = context.Engine.State.PopBottomDeckCard(p);
            if (bottom != null) EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, p, bottom);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothDrawUntilTerrainEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _maxHand;
    public BothDrawUntilTerrainEffect(string effectId, int maxHand) { EffectId = effectId; _maxHand = maxHand; }
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            while (!context.Engine.State.IsGameOver && context.Engine.State.GetHand(p).Count < _maxHand)
            {
                var drawn = context.Engine.DrawCards(p, 1);
                if (drawn.Count == 0 || drawn[0].Kind == CardKind.Terrain) break;
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothDrawWhileHigherForceEffect : IEffect
{
    public string EffectId { get; }
    public BothDrawWhileHigherForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            int prevForce = -1;
            int safety = 0;
            while (!context.Engine.State.IsGameOver && safety++ < 15)
            {
                var drawn = context.Engine.DrawCards(p, 1);
                if (drawn.Count == 0) break;
                if (prevForce >= 0 && drawn[0].Force <= prevForce) break;
                prevForce = drawn[0].Force;
            }
        }
        return DuelActionResult.Ok();
    }
}

internal enum BothCountKind { Banish, WonTerrains }

internal sealed class BothDrawPerCountEffect : IEffect
{
    public string EffectId { get; }
    private readonly BothCountKind _kind;
    private readonly int _max;
    public BothDrawPerCountEffect(string effectId, BothCountKind kind, int maxCount) { EffectId = effectId; _kind = kind; _max = maxCount; }

    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            if (context.Engine.State.IsGameOver) break;
            int count = _kind == BothCountKind.Banish ? context.Engine.State.GetBanish(p).Count : context.Engine.State.GetScore(p);
            int toDraw = Math.Min(_max, count);
            if (toDraw > 0) context.Engine.DrawCards(p, toDraw);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothTakeDiscardPerWonTerrainEffect : IEffect
{
    public string EffectId { get; }
    public BothTakeDiscardPerWonTerrainEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            int count = context.Engine.State.GetScore(p);
            if (count > 0) new TakeTopDiscardEffect(EffectId, count).Execute(new EffectContext(context.Engine, p, context.SourceCard, context.SlotIndex));
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothDiscard1SearchDeck1Effect : IEffect
{
    public string EffectId { get; }
    public BothDiscard1SearchDeck1Effect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            if (context.Engine.State.IsGameOver) break;
            context.Engine.DiscardFromHand(p, 1);
            if (!context.Engine.State.IsGameOver)
                new S3S5SearchDeckEffect(EffectId, _ => true, 1, false).Execute(new EffectContext(context.Engine, p, context.SourceCard, context.SlotIndex));
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BothDiscardHighestDrawEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _draw;
    public BothDiscardHighestDrawEffect(string effectId, int draw) { EffectId = effectId; _draw = draw; }
    public DuelActionResult Execute(EffectContext context)
    {
        DuelCard? PopHighest(PlayerId p)
        {
            var hand = context.Engine.State.GetHand(p);
            if (hand.Count == 0) return null;
            var best = hand.OrderByDescending(c => c.Force).First();
            context.Engine.State.RemoveCardFromHand(p, best);
            context.Engine.State.AddCardToDiscard(p, best);
            context.Engine.Emit(new CardsDiscardedEvent(p, new[] { best }));
            return best;
        }
        var d1 = PopHighest(PlayerId.Player1);
        var d2 = PopHighest(PlayerId.Player2);
        if (d1 != null && (d2 == null || d1.Force > d2.Force)) context.Engine.DrawCards(PlayerId.Player1, _draw);
        else if (d2 != null && (d1 == null || d2.Force > d1.Force)) context.Engine.DrawCards(PlayerId.Player2, _draw);
        context.Engine.CheckDeckOutCondition(PlayerId.Player1);
        context.Engine.CheckDeckOutCondition(PlayerId.Player2);
        return DuelActionResult.Ok();
    }
}

internal sealed class BothPlayFreeIfUnstackedEffect : IEffect
{
    public string EffectId { get; }
    public BothPlayFreeIfUnstackedEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
            new PlayCharacterFreeNoScoreEffect(EffectId).Execute(new EffectContext(context.Engine, p, context.SourceCard, context.SlotIndex));
        return DuelActionResult.Ok();
    }
}

internal sealed class BothLookTopTakeOneShuffleEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _lookCount;
    public BothLookTopTakeOneShuffleEffect(string effectId, int lookCount) { EffectId = effectId; _lookCount = lookCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
            new LookTopDeckTakeAndBottomEffect(EffectId, _lookCount, 1).Execute(new EffectContext(context.Engine, p, context.SourceCard, context.SlotIndex));
        return DuelActionResult.Ok();
    }
}

internal sealed class ConditionalDrawEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<EffectContext, bool> _cond;
    private readonly int _amount;
    private readonly bool _both;
    public ConditionalDrawEffect(string effectId, Func<EffectContext, bool> cond, int amount, bool both)
    {
        EffectId = effectId; _cond = cond; _amount = amount; _both = both;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        if (_cond(context))
        {
            if (_both)
            {
                context.Engine.DrawCards(PlayerId.Player1, _amount);
                if (!context.Engine.State.IsGameOver) context.Engine.DrawCards(PlayerId.Player2, _amount);
            }
            else
            {
                context.Engine.DrawCards(context.Player, _amount);
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class DrawThreeElseDiscardOneEffect : IEffect
{
    public string EffectId { get; }
    public DrawThreeElseDiscardOneEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.Engine.State.GetDeck(context.Player).Count >= 3)
            context.Engine.DrawCards(context.Player, 3);
        else
            context.Engine.DiscardFromHand(context.Player, 1);
        return DuelActionResult.Ok();
    }
}

internal sealed class DrawUntilOppHandPlusOneEffect : IEffect
{
    public string EffectId { get; }
    public DrawUntilOppHandPlusOneEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        int target = context.Engine.State.GetHand(context.Player.Opponent()).Count + 1;
        int missing = target - context.Engine.State.GetHand(context.Player).Count;
        if (missing > 0) context.Engine.DrawCards(context.Player, missing);
        return DuelActionResult.Ok();
    }
}

internal enum BoardPredicateKind { MoucheOnSlot, OwnCharsOnSlot, VikingInPlay, FacedownInPlay, CombosOnBoard, EffectsOnBoard }

internal sealed class DrawPerBoardPredicateEffect : IEffect
{
    public string EffectId { get; }
    private readonly BoardPredicateKind _kind;
    private readonly int _max;
    private readonly bool _discardOppIf5;
    public DrawPerBoardPredicateEffect(string effectId, BoardPredicateKind kind, int maxCount, bool discardOppIf5)
    {
        EffectId = effectId; _kind = kind; _max = maxCount; _discardOppIf5 = discardOppIf5;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var state = context.Engine.State;
        int count = 0;
        switch (_kind)
        {
            case BoardPredicateKind.MoucheOnSlot:
                if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
                {
                    var s = state.Slots[context.SlotIndex];
                    count = s.CharactersP1.Concat(s.CharactersP2).Count(c => c.Name.IndexOf("Mouche", StringComparison.OrdinalIgnoreCase) >= 0);
                }
                break;
            case BoardPredicateKind.OwnCharsOnSlot:
                if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
                    count = state.Slots[context.SlotIndex].GetCharacters(context.Player).Count;
                break;
            case BoardPredicateKind.VikingInPlay:
                foreach (var s in state.Slots)
                    if (!s.IsEmpty)
                        count += s.CharactersP1.Concat(s.CharactersP2).Count(c => c.Name.IndexOf("Viking", StringComparison.OrdinalIgnoreCase) >= 0 || c.EffectIds.Contains("passive_considered_viking"));
                break;
            case BoardPredicateKind.FacedownInPlay:
                var aux = EffectsS3S5.GetAuxState(state);
                foreach (var kv in aux.FacedownByCardId) count += kv.Value.Count;
                break;
            case BoardPredicateKind.CombosOnBoard:
                count = Math.Max(EffectsS3S5.GetAuxState(state).CombosTrackedThisTurn, EffectsS3S5.CountAllCombosInPlay(state));
                break;
            case BoardPredicateKind.EffectsOnBoard:
                foreach (var s in state.Slots)
                    if (!s.IsEmpty)
                        count += s.GetCharacters(context.Player).Count(c => c.EffectIds.Count > 0 || c.ComboEffectIds.Count > 0);
                break;
        }
        int toDraw = Math.Min(_max, count);
        if (toDraw > 0) context.Engine.DrawCards(context.Player, toDraw);
        if (_discardOppIf5 && toDraw >= 5 && !state.IsGameOver)
            new DiscardCharacterByMaxForceEffect(EffectId, int.MaxValue).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class BanishOppDiscardByOppBanishEffect : IEffect
{
    public string EffectId { get; }
    public BanishOppDiscardByOppBanishEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        int count = context.Engine.State.GetBanish(opp).Count;
        if (count > 0) context.Engine.BanishCardsFromDiscard(opp, count);
        return DuelActionResult.Ok();
    }
}

internal enum MillCountKind { CavalierInPlay, CombosInPlay }

internal sealed class MillOppPerBoardCountEffect : IEffect
{
    public string EffectId { get; }
    private readonly MillCountKind _kind;
    public MillOppPerBoardCountEffect(string effectId, MillCountKind kind) { EffectId = effectId; _kind = kind; }

    public DuelActionResult Execute(EffectContext context)
    {
        int count = 0;
        if (_kind == MillCountKind.CombosInPlay)
            count = EffectsS3S5.CountAllCombosInPlay(context.Engine.State);
        else
            foreach (var s in context.Engine.State.Slots)
                if (!s.IsEmpty)
                    count += s.CharactersP1.Concat(s.CharactersP2).Count(c => c.Name.IndexOf("Cavalier", StringComparison.OrdinalIgnoreCase) >= 0);

        if (count > 0) context.Engine.MillCards(context.Player.Opponent(), count);
        return DuelActionResult.Ok();
    }
}

internal sealed class MillOppUntilEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _untilCostGe1;
    private readonly int _forceTarget;
    public MillOppUntilEffect(string effectId, bool untilCostGe1, int forceTarget)
    {
        EffectId = effectId; _untilCostGe1 = untilCostGe1; _forceTarget = forceTarget;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        int totalForce = 0;
        while (!context.Engine.State.IsGameOver && context.Engine.State.GetDeck(opp).Count > 0)
        {
            var milled = context.Engine.MillCards(opp, 1);
            if (milled.Count == 0) break;
            if (_untilCostGe1)
            {
                if (S3S5Bridge.GetCardCost(milled[0]) >= 1) break;
            }
            else
            {
                totalForce += milled[0].Force;
                if (totalForce >= _forceTarget) break;
            }
        }
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// Win / Lose helpers
// =============================================================================
internal sealed class TakeBottomDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public TakeBottomDeckEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        for (int i = 0; i < _amount; i++)
        {
            var c = context.Engine.State.PopBottomDeckCard(context.Player);
            if (c == null) break;
            context.Engine.State.AddCardToHand(context.Player, c);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class PutTopDiscardOnTopDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public PutTopDiscardOnTopDeckEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var discard = context.Engine.State.GetDiscard(context.Player);
        int toMove = Math.Min(_amount, discard.Count);
        var moved = new List<DuelCard>(toMove);
        for (int i = 0; i < toMove; i++)
        {
            var c = discard[discard.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, c);
            moved.Add(c);
        }
        for (int i = moved.Count - 1; i >= 0; i--)
            EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, moved[i]);
        return DuelActionResult.Ok();
    }
}

internal sealed class ReturnBanishedToTopDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public ReturnBanishedToTopDeckEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var banish = context.Engine.State.GetBanish(context.Player);
        int toMove = Math.Min(_amount, banish.Count);
        if (toMove == 0) return DuelActionResult.Ok();
        // DuelState n'expose pas RemoveCardFromBanish : on utilise les cartes sans muter de manière non supportée ou on copie vers deck si disponible.
        var taken = new List<DuelCard>(toMove);
        for (int i = 0; i < toMove; i++)
            taken.Add(banish[banish.Count - 1 - i]);
        TryRemoveFromBanishList(context.Engine.State, context.Player, taken);
        for (int i = taken.Count - 1; i >= 0; i--)
            EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, taken[i]);
        return DuelActionResult.Ok();
    }

    internal static void TryRemoveFromBanishList(DuelState state, PlayerId player, IReadOnlyList<DuelCard> cards)
    {
        string fieldName = player == PlayerId.Player1 ? "_banishP1" : "_banishP2";
        var field = typeof(DuelState).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(state) is List<DuelCard> list)
            foreach (var c in cards) list.Remove(c);
    }
}

internal sealed class ReturnTopDiscardToBottomDeckS3S5Effect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public ReturnTopDiscardToBottomDeckS3S5Effect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var discard = context.Engine.State.GetDiscard(context.Player);
        int toMove = Math.Min(_amount, discard.Count);
        for (int i = 0; i < toMove; i++)
        {
            var c = discard[discard.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, c);
            context.Engine.State.AddCardToBottomDeck(context.Player, c);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class WinBounceNonScorerEffect : IEffect
{
    public string EffectId { get; }
    public WinBounceNonScorerEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDiscard(context.Player)
            .Where(c => c.Kind == CardKind.Character && !c.IsScoreur)
            .ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Reprendre un non-Scoreur", "Choisissez un personnage non Scoreur en défausse", candidates, chosen =>
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
            context.Engine.State.AddCardToHand(context.Player, chosen);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class WinDiscardOwnCharInPlayEffect : IEffect
{
    public string EffectId { get; }
    public WinDiscardOwnCharInPlayEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var own = new List<(TerrainSlot Slot, DuelCard Card)>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty)
                foreach (var c in s.GetCharacters(context.Player)) own.Add((s, c));
        if (own.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Défausser un de vos personnages", "Choisissez un personnage à défausser", own.Select(x => x.Card).ToList(), chosen =>
        {
            var entry = own.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
            if (entry.Slot != null && entry.Slot.RemoveCharacter(context.Player, entry.Card))
            {
                context.Engine.State.AddCardToDiscard(context.Player, entry.Card);
                context.Engine.Emit(new CardsDiscardedEvent(context.Player, new[] { entry.Card }));
            }
        }, list => list.OrderBy(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class WinShuffleComboCharFromDiscardEffect : IEffect
{
    public string EffectId { get; }
    public WinShuffleComboCharFromDiscardEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDiscard(context.Player)
            .Where(c => c.Kind == CardKind.Character && (c.ComboEffectIds.Count > 0 || c.IsComboScoreur))
            .ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Mélanger un personnage Combo", "Choisissez un personnage avec effet Combo en défausse", candidates, chosen =>
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
            context.Engine.State.AddCardToBottomDeck(context.Player, chosen);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class WinShuffleCharForceLeToDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _maxForce;
    public WinShuffleCharForceLeToDeckEffect(string effectId, int maxForce) { EffectId = effectId; _maxForce = maxForce; }
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDiscard(context.Player)
            .Where(c => c.Kind == CardKind.Character && c.Force <= _maxForce)
            .ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, $"Mélanger un personnage Force <= {_maxForce}", "Choisissez un personnage à remettre dans le deck", candidates, chosen =>
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
            context.Engine.State.AddCardToBottomDeck(context.Player, chosen);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class WinTakeDiscardNonScorerEffect : IEffect
{
    public string EffectId { get; }
    public WinTakeDiscardNonScorerEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDiscard(context.Player).Where(c => !c.IsScoreur).ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Reprendre une carte non Scoreur", "Choisissez une carte non Scoreur en défausse", candidates, chosen =>
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
            context.Engine.State.AddCardToHand(context.Player, chosen);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class WinTakeDiscardPerOppCharEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _max;
    public WinTakeDiscardPerOppCharEffect(string effectId, int max) { EffectId = effectId; _max = max; }
    public DuelActionResult Execute(EffectContext context)
    {
        int oppCount = 0;
        if (context.SlotIndex >= 0 && context.SlotIndex < context.Engine.State.Slots.Count)
            oppCount = context.Engine.State.Slots[context.SlotIndex].GetCharacters(context.Player.Opponent()).Count;
        int toTake = Math.Min(_max, oppCount);
        if (toTake > 0) new TakeTopDiscardEffect(EffectId, toTake).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class WinDrawPerCharMaxEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _maxHand;
    public WinDrawPerCharMaxEffect(string effectId, int maxHand) { EffectId = effectId; _maxHand = maxHand; }
    public DuelActionResult Execute(EffectContext context)
    {
        int ownCount = 0;
        if (context.SlotIndex >= 0 && context.SlotIndex < context.Engine.State.Slots.Count)
            ownCount = context.Engine.State.Slots[context.SlotIndex].GetCharacters(context.Player).Count;
        int room = Math.Max(0, _maxHand - context.Engine.State.GetHand(context.Player).Count);
        int toDraw = Math.Min(ownCount, room);
        if (toDraw > 0) context.Engine.DrawCards(context.Player, toDraw);
        return DuelActionResult.Ok();
    }
}

internal sealed class WinDrawPerWonTerrainEffect : IEffect
{
    public string EffectId { get; }
    public WinDrawPerWonTerrainEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        int won = context.Engine.State.GetScore(context.Player);
        if (won > 0) context.Engine.DrawCards(context.Player, won);
        return DuelActionResult.Ok();
    }
}

internal sealed class LoseDiscardOrMillEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _discard;
    private readonly int _mill;
    public LoseDiscardOrMillEffect(string effectId, int discardCount, int millCount) { EffectId = effectId; _discard = discardCount; _mill = millCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.Engine.State.GetHand(context.Player).Count >= _discard)
            context.Engine.DiscardFromHand(context.Player, _discard);
        else
            context.Engine.MillCards(context.Player, _mill);
        return DuelActionResult.Ok();
    }
}

internal sealed class LoseDiscardAndDrawEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _discard;
    private readonly int _draw;
    public LoseDiscardAndDrawEffect(string effectId, int discard, int draw) { EffectId = effectId; _discard = discard; _draw = draw; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DiscardFromHand(context.Player, _discard);
        if (!context.Engine.State.IsGameOver) context.Engine.DrawCards(context.Player, _draw);
        return DuelActionResult.Ok();
    }
}

internal sealed class MillBottomDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public MillBottomDeckEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var milled = new List<DuelCard>();
        for (int i = 0; i < _amount; i++)
        {
            var c = context.Engine.State.PopBottomDeckCard(context.Player);
            if (c == null) break;
            context.Engine.State.AddCardToDiscard(context.Player, c);
            milled.Add(c);
        }
        if (milled.Count > 0) context.Engine.Emit(new CardsMilledEvent(context.Player, milled));
        context.Engine.CheckDeckOutCondition(context.Player);
        return DuelActionResult.Ok();
    }
}

internal sealed class LoseMillOrMoreIfSmallerDiscardEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _baseMill;
    private readonly int _altMill;
    public LoseMillOrMoreIfSmallerDiscardEffect(string effectId, int baseMill, int altMill) { EffectId = effectId; _baseMill = baseMill; _altMill = altMill; }
    public DuelActionResult Execute(EffectContext context)
    {
        int myDiscard = context.Engine.State.GetDiscard(context.Player).Count;
        int oppDiscard = context.Engine.State.GetDiscard(context.Player.Opponent()).Count;
        context.Engine.MillCards(context.Player, myDiscard < oppDiscard ? _altMill : _baseMill);
        return DuelActionResult.Ok();
    }
}

internal sealed class LoseMillEqualHandMaxEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _max;
    public LoseMillEqualHandMaxEffect(string effectId, int max) { EffectId = effectId; _max = max; }
    public DuelActionResult Execute(EffectContext context)
    {
        int count = Math.Min(_max, context.Engine.State.GetHand(context.Player).Count);
        if (count > 0) context.Engine.MillCards(context.Player, count);
        return DuelActionResult.Ok();
    }
}

internal enum LoseMillKind { OwnCharsOnSlot, OppCharsOnSlot, DuplicateCharsInPlay, GuestsInPlay, WonTerrains }

internal sealed class LoseMillPerBoardCountEffect : IEffect
{
    public string EffectId { get; }
    private readonly LoseMillKind _kind;
    private readonly int _max;
    public LoseMillPerBoardCountEffect(string effectId, LoseMillKind kind, int maxCount) { EffectId = effectId; _kind = kind; _max = maxCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var state = context.Engine.State;
        int count = 0;
        switch (_kind)
        {
            case LoseMillKind.OwnCharsOnSlot:
                if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
                    count = state.Slots[context.SlotIndex].GetCharacters(context.Player).Count;
                break;
            case LoseMillKind.OppCharsOnSlot:
                if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
                    count = state.Slots[context.SlotIndex].GetCharacters(context.Player.Opponent()).Count;
                break;
            case LoseMillKind.DuplicateCharsInPlay:
                var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in state.Slots)
                    if (!s.IsEmpty)
                        foreach (var c in s.CharactersP1.Concat(s.CharactersP2))
                            counts[c.Name] = counts.TryGetValue(c.Name, out int v) ? v + 1 : 1;
                count = counts.Values.Where(v => v >= 2).Sum();
                break;
            case LoseMillKind.GuestsInPlay:
                foreach (var s in state.Slots)
                    if (!s.IsEmpty)
                        count += s.CharactersP1.Concat(s.CharactersP2).Count(c =>
                            !string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase));
                break;
            case LoseMillKind.WonTerrains:
                count = state.GetScore(context.Player);
                break;
        }
        int toMill = Math.Min(_max, count);
        if (toMill > 0) context.Engine.MillCards(context.Player, toMill);
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// Recherches Deck
// =============================================================================
internal sealed class S3S5SearchDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<DuelCard, bool> _pred;
    private readonly int _maxCount;
    private readonly bool _bothPlayers;
    public S3S5SearchDeckEffect(string effectId, Func<DuelCard, bool> pred, int maxCount, bool bothPlayers)
    {
        EffectId = effectId; _pred = pred; _maxCount = maxCount; _bothPlayers = bothPlayers;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        if (_bothPlayers)
        {
            SearchForPlayer(context, PlayerId.Player1);
            SearchForPlayer(context, PlayerId.Player2);
        }
        else
        {
            SearchForPlayer(context, context.Player);
        }
        return DuelActionResult.Ok();
    }

    private void SearchForPlayer(EffectContext context, PlayerId player)
    {
        int found = 0;
        while (found < _maxCount)
        {
            var candidates = context.Engine.State.GetDeck(player).Where(_pred).ToList();
            if (candidates.Count == 0) break;

            if (_maxCount == 1)
            {
                var subCtx = new EffectContext(context.Engine, player, context.SourceCard, context.SlotIndex);
                EffectsS3S5.SelectCandidateOrAuto(subCtx, "Chercher dans le deck", "Choisissez une carte à ajouter à votre main", candidates, chosen =>
                {
                    if (context.Engine.State.RemoveCardFromDeck(player, chosen))
                        context.Engine.State.AddCardToHand(player, chosen);
                }, list => list.OrderByDescending(c => c.Force).First());
                break;
            }
            else
            {
                var chosen = candidates.OrderByDescending(c => c.Force).First();
                if (context.Engine.State.RemoveCardFromDeck(player, chosen))
                    context.Engine.State.AddCardToHand(player, chosen);
                found++;
            }
        }
    }
}

internal sealed class BothSearchCopyOfCharInPlayEffect : IEffect
{
    public string EffectId { get; }
    public BothSearchCopyOfCharInPlayEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var namesInPlay = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty)
                foreach (var c in s.CharactersP1.Concat(s.CharactersP2))
                    namesInPlay.Add(c.Name);
        if (namesInPlay.Count == 0) return DuelActionResult.Ok();
        new S3S5SearchDeckEffect(EffectId, c => namesInPlay.Contains(c.Name), 1, bothPlayers: true).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class SearchDeckLainkAndTerracidEffect : IEffect
{
    public string EffectId { get; }
    public SearchDeckLainkAndTerracidEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        new S3S5SearchDeckEffect(EffectId, c => string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase), 1, false).Execute(context);
        new S3S5SearchDeckEffect(EffectId, c => string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase), 1, false).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class SearchDeckOrDiscardByNameEffect : IEffect
{
    public string EffectId { get; }
    private readonly string _keyword;
    public SearchDeckOrDiscardByNameEffect(string effectId, string keyword) { EffectId = effectId; _keyword = keyword; }
    public DuelActionResult Execute(EffectContext context)
    {
        var fromDeck = context.Engine.State.GetDeck(context.Player).FirstOrDefault(c => c.Name.IndexOf(_keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        if (fromDeck != null && context.Engine.State.RemoveCardFromDeck(context.Player, fromDeck))
        {
            context.Engine.State.AddCardToHand(context.Player, fromDeck);
            return DuelActionResult.Ok();
        }
        var fromDiscard = context.Engine.State.GetDiscard(context.Player).FirstOrDefault(c => c.Name.IndexOf(_keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        if (fromDiscard != null)
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, fromDiscard);
            context.Engine.State.AddCardToHand(context.Player, fromDiscard);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class SearchDeckToTopDiscardEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<DuelCard, bool> _pred;
    public SearchDeckToTopDiscardEffect(string effectId, Func<DuelCard, bool> pred) { EffectId = effectId; _pred = pred; }
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDeck(context.Player).Where(_pred).ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Chercher dans le deck", "Choisissez une carte à placer sur votre défausse", candidates, chosen =>
        {
            if (context.Engine.State.RemoveCardFromDeck(context.Player, chosen))
            {
                context.Engine.State.AddCardToDiscard(context.Player, chosen);
                context.Engine.Emit(new CardsDiscardedEvent(context.Player, new[] { chosen }));
            }
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// Jeu gratuit depuis zones
// =============================================================================
internal enum ZoneSource { Hand, Discard, Banish }

internal sealed class PlayFreeCharFromZoneEffect : IEffect
{
    public string EffectId { get; }
    private readonly ZoneSource _zone;
    private readonly int _millSelfFirst;
    private readonly bool _stripEffects;
    private readonly Func<DuelCard, bool>? _filter;

    public PlayFreeCharFromZoneEffect(string effectId, ZoneSource zone, int millSelfFirst, bool stripEffects, Func<DuelCard, bool>? filter = null)
    {
        EffectId = effectId; _zone = zone; _millSelfFirst = millSelfFirst; _stripEffects = stripEffects; _filter = filter;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        if (_millSelfFirst > 0)
        {
            context.Engine.MillCards(context.Player, _millSelfFirst);
            if (context.Engine.State.IsGameOver) return DuelActionResult.Ok();
        }

        IReadOnlyList<DuelCard> pool = _zone switch
        {
            ZoneSource.Hand => context.Engine.State.GetHand(context.Player),
            ZoneSource.Discard => context.Engine.State.GetDiscard(context.Player),
            _ => context.Engine.State.GetBanish(context.Player)
        };
        var candidates = pool.Where(c => c.Kind == CardKind.Character && (_filter == null || _filter(c))).ToList();
        var occupied = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State);
        if (candidates.Count == 0 || occupied.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Jouer un personnage gratuitement", "Choisissez un personnage à poser gratuitement", candidates, chosen =>
        {
            bool removed = _zone switch
            {
                ZoneSource.Hand => context.Engine.State.RemoveCardFromHand(context.Player, chosen),
                ZoneSource.Discard => RemoveDiscard(context.Engine.State, context.Player, chosen),
                _ => RemoveBanish(context.Engine.State, context.Player, chosen)
            };
            if (!removed) return;
            var toPlay = _stripEffects ? EffectsS3S5.CloneCard(chosen, isScoreur: false, effectIds: Array.Empty<string>(), comboEffectIds: Array.Empty<string>(), isComboScoreur: false) : chosen;
            EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, toPlay, context.SlotIndex);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }

    private static bool RemoveDiscard(DuelState state, PlayerId p, DuelCard c) { state.RemoveCardFromDiscard(p, c); return true; }
    private static bool RemoveBanish(DuelState state, PlayerId p, DuelCard c) { ReturnBanishedToTopDeckEffect.TryRemoveFromBanishList(state, p, new[] { c }); return true; }
}

internal sealed class PlayFreeMultiFromHandEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<DuelCard, bool> _pred;
    private readonly int _maxCount;
    public PlayFreeMultiFromHandEffect(string effectId, Func<DuelCard, bool> pred, int maxCount) { EffectId = effectId; _pred = pred; _maxCount = maxCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var matches = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character && _pred(c)).Take(_maxCount).ToList();
        foreach (var c in matches)
            if (context.Engine.State.RemoveCardFromHand(context.Player, c))
                EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, c, context.SlotIndex);
        return DuelActionResult.Ok();
    }
}

internal sealed class PlayRandomHandCardFreeEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _targetOpponent;
    private readonly bool _stripEffects;
    public PlayRandomHandCardFreeEffect(string effectId, bool targetOpponent, bool stripEffects)
    {
        EffectId = effectId; _targetOpponent = targetOpponent; _stripEffects = stripEffects;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var player = _targetOpponent ? context.Player.Opponent() : context.Player;
        var handChars = context.Engine.State.GetHand(player).Where(c => c.Kind == CardKind.Character).ToList();
        if (handChars.Count == 0) return DuelActionResult.Ok();
        int idx = S3S5Bridge.RollNext(context.Engine, 0, handChars.Count);
        var chosen = handChars[idx];
        if (context.Engine.State.RemoveCardFromHand(player, chosen))
        {
            var toPlay = _stripEffects ? EffectsS3S5.CloneCard(chosen, isScoreur: false, effectIds: Array.Empty<string>(), comboEffectIds: Array.Empty<string>(), isComboScoreur: false) : chosen;
            EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, player, toPlay, context.SlotIndex);
        }
        return DuelActionResult.Ok();
    }
}

internal enum PlaySelfCond { AllOwnCharsForceGt2, DessinRomantiqueInPlay, EmptyTerrain }

internal sealed class PlayFreeConditionalOnSelfEffect : IEffect
{
    public string EffectId { get; }
    private readonly PlaySelfCond _cond;
    public PlayFreeConditionalOnSelfEffect(string effectId, PlaySelfCond cond) { EffectId = effectId; _cond = cond; }
    public DuelActionResult Execute(EffectContext context)
    {
        // Si déclenché en pose, vérifie la condition (la carte est déjà posée, sert de marqueur pour la UI / l'IA).
        return DuelActionResult.Ok();
    }
}

internal sealed class PlayDiscardAfterPutHandTopDeckEffect : IEffect
{
    public string EffectId { get; }
    public PlayDiscardAfterPutHandTopDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        new PutHandCardOnTopDeckEffect(EffectId).Execute(context);
        new PlayFreeCharFromZoneEffect(EffectId, ZoneSource.Discard, 0, stripEffects: true).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class PutHandCardOnTopDeckEffect : IEffect
{
    public string EffectId { get; }
    public PutHandCardOnTopDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var hand = context.Engine.State.GetHand(context.Player).ToList();
        if (hand.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Placer une carte sur le deck", "Choisissez une carte de votre main à placer sur votre deck", hand, chosen =>
        {
            if (context.Engine.State.RemoveCardFromHand(context.Player, chosen))
                EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, chosen);
        }, list => list.OrderBy(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class PlayFreeFromDeckPredicateEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<DuelCard, DuelCard, bool> _pred;
    private readonly bool _forceZero;
    private readonly int _maxCount;

    public PlayFreeFromDeckPredicateEffect(string effectId, Func<DuelCard, DuelCard, bool> pred, bool forceZero, int maxCount)
    {
        EffectId = effectId; _pred = pred; _forceZero = forceZero; _maxCount = maxCount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var matches = context.Engine.State.GetDeck(context.Player)
            .Where(c => c.Kind == CardKind.Character && _pred(c, context.SourceCard))
            .Take(_maxCount)
            .ToList();
        foreach (var c in matches)
        {
            if (context.Engine.State.RemoveCardFromDeck(context.Player, c))
            {
                var toPlay = _forceZero ? EffectsS3S5.CloneCard(c, force: 0) : c;
                EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, toPlay, context.SlotIndex);
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BanishHandAndPlayFromDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly string _nameKeyword;
    public BanishHandAndPlayFromDeckEffect(string effectId, string nameKeyword) { EffectId = effectId; _nameKeyword = nameKeyword; }
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.Engine.State.GetHand(context.Player).Count == 0) return DuelActionResult.Ok();
        context.Engine.BanishCardsFromHand(context.Player, 1);
        if (!context.Engine.State.IsGameOver)
            new PlayFreeFromDeckPredicateEffect(EffectId, (c, _) => c.Name.IndexOf(_nameKeyword, StringComparison.OrdinalIgnoreCase) >= 0, false, 1).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class BanishSelfAndPlayFromDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly string _nameKeyword;
    public BanishSelfAndPlayFromDeckEffect(string effectId, string nameKeyword) { EffectId = effectId; _nameKeyword = nameKeyword; }
    public DuelActionResult Execute(EffectContext context)
    {
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out int slotIdx, out _) && slot != null)
        {
            if (slot.RemoveCharacter(context.Player, context.SourceCard))
            {
                context.Engine.State.AddCardToBanish(context.Player, context.SourceCard);
                context.Engine.Emit(new CardsBanishedEvent(context.Player, new[] { context.SourceCard }));
                new PlayFreeFromDeckPredicateEffect(EffectId, (c, _) => c.Name.IndexOf(_nameKeyword, StringComparison.OrdinalIgnoreCase) >= 0, false, 1)
                    .Execute(new EffectContext(context.Engine, context.Player, context.SourceCard, slotIdx));
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class PlayTwoSameCostPayOneEffect : IEffect
{
    public string EffectId { get; }
    public PlayTwoSameCostPayOneEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var handChars = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character).ToList();
        var group = handChars.GroupBy(S3S5Bridge.GetCardCost).FirstOrDefault(g => g.Count() >= 1);
        if (group != null)
        {
            var chosen = group.OrderByDescending(c => c.Force).First();
            if (context.Engine.State.RemoveCardFromHand(context.Player, chosen))
                EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, chosen, context.SlotIndex);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class PlayOnRandomSlotEffect : IEffect
{
    public string EffectId { get; }
    public PlayOnRandomSlotEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var occupied = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State);
        if (occupied.Count <= 1) return DuelActionResult.Ok();
        int destSlot = occupied[S3S5Bridge.RollNext(context.Engine, 0, occupied.Count)];
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out _, out int fromSlot, out _) && destSlot != fromSlot)
            context.Engine.MoveCharacter(context.Player, context.SourceCard.Id, fromSlot, destSlot);
        return DuelActionResult.Ok();
    }
}

internal sealed class OppPlayFromZoneNoEffectsEffect : IEffect
{
    public string EffectId { get; }
    private readonly ZoneSource _zone;
    private readonly bool _stripEffects;
    public OppPlayFromZoneNoEffectsEffect(string effectId, ZoneSource zone, bool stripEffects)
    {
        EffectId = effectId; _zone = zone; _stripEffects = stripEffects;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var pool = _zone == ZoneSource.Discard ? context.Engine.State.GetDiscard(opp) : context.Engine.State.GetHand(opp);
        var chars = pool.Where(c => c.Kind == CardKind.Character).ToList();
        if (chars.Count == 0) return DuelActionResult.Ok();
        var chosen = chars[0];
        if (_zone == ZoneSource.Discard) context.Engine.State.RemoveCardFromDiscard(opp, chosen);
        else context.Engine.State.RemoveCardFromHand(opp, chosen);
        var toPlay = _stripEffects ? EffectsS3S5.CloneCard(chosen, isScoreur: false, effectIds: Array.Empty<string>(), comboEffectIds: Array.Empty<string>(), isComboScoreur: false) : chosen;
        EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, opp, toPlay, context.SlotIndex);
        return DuelActionResult.Ok();
    }
}

internal sealed class OppRandomHandToOwnDeckEffect : IEffect
{
    public string EffectId { get; }
    public OppRandomHandToOwnDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var oppHand = context.Engine.State.GetHand(opp);
        if (oppHand.Count == 0) return DuelActionResult.Ok();
        var chosen = oppHand[S3S5Bridge.RollNext(context.Engine, 0, oppHand.Count)];
        if (context.Engine.State.RemoveCardFromHand(opp, chosen))
            EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, chosen);
        return DuelActionResult.Ok();
    }
}

internal sealed class RevealDeckEdgeAndPlayFreeEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _fromTop;
    public RevealDeckEdgeAndPlayFreeEffect(string effectId, bool fromTop) { EffectId = effectId; _fromTop = fromTop; }
    public DuelActionResult Execute(EffectContext context)
    {
        var deck = context.Engine.State.GetDeck(context.Player);
        if (deck.Count == 0) return DuelActionResult.Ok();
        var card = _fromTop ? context.Engine.State.PopTopDeckCard(context.Player) : context.Engine.State.PopBottomDeckCard(context.Player);
        if (card == null) return DuelActionResult.Ok();
        context.Engine.Emit(new CardsRevealedEvent(context.Player, new[] { card }));
        if (card.Kind == CardKind.Character && EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State).Count > 0)
            EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, card, context.SlotIndex);
        else
            context.Engine.State.AddCardToHand(context.Player, card);
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// Défausse / Ban / Deck / Face cachée
// =============================================================================
internal sealed class TakeBanishedCardEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public TakeBanishedCardEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var banish = context.Engine.State.GetBanish(context.Player);
        int toTake = Math.Min(_amount, banish.Count);
        if (toTake == 0) return DuelActionResult.Ok();
        var taken = new List<DuelCard>(toTake);
        for (int i = 0; i < toTake; i++) taken.Add(banish[banish.Count - 1 - i]);
        ReturnBanishedToTopDeckEffect.TryRemoveFromBanishList(context.Engine.State, context.Player, taken);
        foreach (var c in taken) context.Engine.State.AddCardToHand(context.Player, c);
        return DuelActionResult.Ok();
    }
}

internal sealed class TakeFilteredDiscardS3S5Effect : IEffect
{
    public string EffectId { get; }
    private readonly Func<DuelCard, bool> _pred;
    private readonly int _maxCount;
    public TakeFilteredDiscardS3S5Effect(string effectId, Func<DuelCard, bool> pred, int maxCount) { EffectId = effectId; _pred = pred; _maxCount = maxCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        for (int i = 0; i < _maxCount; i++)
        {
            var candidates = context.Engine.State.GetDiscard(context.Player).Where(_pred).ToList();
            if (candidates.Count == 0) break;
            if (_maxCount == 1)
            {
                EffectsS3S5.SelectCandidateOrAuto(context, "Reprendre en défausse", "Choisissez une carte à ajouter à votre main", candidates, chosen =>
                {
                    context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
                    context.Engine.State.AddCardToHand(context.Player, chosen);
                });
                break;
            }
            var pick = candidates[candidates.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, pick);
            context.Engine.State.AddCardToHand(context.Player, pick);
        }
        return DuelActionResult.Ok();
    }
}

internal enum TakeTopCond { Always, FirstOnSlot, CowboysInPlay, OwnCharsOnSlotMax3, EffectsThisTurn }

internal sealed class TakeTopDiscardByConditionEffect : IEffect
{
    public string EffectId { get; }
    private readonly TakeTopCond _cond;
    private readonly int _amount;
    public TakeTopDiscardByConditionEffect(string effectId, TakeTopCond cond, int amount) { EffectId = effectId; _cond = cond; _amount = amount; }

    public DuelActionResult Execute(EffectContext context)
    {
        var state = context.Engine.State;
        int take = 0;
        switch (_cond)
        {
            case TakeTopCond.Always: take = _amount; break;
            case TakeTopCond.FirstOnSlot:
                if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
                {
                    var s = state.Slots[context.SlotIndex];
                    if (s.GetCharacters(context.Player).Count <= 1 && s.GetCharacters(context.Player.Opponent()).Count == 0) take = _amount;
                }
                break;
            case TakeTopCond.CowboysInPlay:
                foreach (var s in state.Slots)
                    if (!s.IsEmpty)
                        take += s.CharactersP1.Concat(s.CharactersP2).Count(c => c.Name.IndexOf("Cowboy", StringComparison.OrdinalIgnoreCase) >= 0);
                break;
            case TakeTopCond.OwnCharsOnSlotMax3:
                if (context.SlotIndex >= 0 && context.SlotIndex < state.Slots.Count)
                    take = Math.Min(3, state.Slots[context.SlotIndex].GetCharacters(context.Player).Count);
                break;
            case TakeTopCond.EffectsThisTurn:
                take = Math.Max(1, EffectsS3S5.GetAuxState(state).EffectsTrackedThisTurn);
                break;
        }
        if (take > 0) new TakeTopDiscardEffect(EffectId, take).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class PutBottomDiscardToTopDeckIfRightmostEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public PutBottomDiscardToTopDeckIfRightmostEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        int rightmost = -1;
        for (int i = context.Engine.State.Slots.Count - 1; i >= 0; i--)
        {
            if (!context.Engine.State.Slots[i].IsEmpty) { rightmost = i; break; }
        }
        if (context.SlotIndex != rightmost || rightmost < 0) return DuelActionResult.Ok();

        var discard = context.Engine.State.GetDiscard(context.Player);
        int toMove = Math.Min(_amount, discard.Count);
        var moved = new List<DuelCard>(toMove);
        for (int i = 0; i < toMove; i++)
        {
            var bottom = discard[0];
            context.Engine.State.RemoveCardFromDiscard(context.Player, bottom);
            moved.Add(bottom);
        }
        for (int i = moved.Count - 1; i >= 0; i--)
            EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, moved[i]);
        return DuelActionResult.Ok();
    }
}

internal sealed class SearchDiscardToTopDeckEffect : IEffect
{
    public string EffectId { get; }
    public SearchDiscardToTopDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDiscard(context.Player).ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Replacer sur le deck", "Choisissez une carte de votre défausse à placer sur votre deck", candidates, chosen =>
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
            EffectsS3S5.PutCardOnTopOfDeck(context.Engine.State, context.Player, chosen);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class ReturnBanishedUnderDiscardEffect : IEffect
{
    public string EffectId { get; }
    public ReturnBanishedUnderDiscardEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            var banished = context.Engine.State.GetBanish(p).ToList();
            if (banished.Count == 0) continue;
            ReturnBanishedToTopDeckEffect.TryRemoveFromBanishList(context.Engine.State, p, banished);
            context.Engine.State.AddCardsToDiscard(p, banished);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class ReturnFilteredDiscardToBottomDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<DuelCard, bool> _pred;
    public ReturnFilteredDiscardToBottomDeckEffect(string effectId, Func<DuelCard, bool> pred) { EffectId = effectId; _pred = pred; }
    public DuelActionResult Execute(EffectContext context)
    {
        var match = context.Engine.State.GetDiscard(context.Player).LastOrDefault(_pred);
        if (match != null)
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, match);
            context.Engine.State.AddCardToBottomDeck(context.Player, match);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class ShuffleDiscardCardToDeckEffect : IEffect
{
    public string EffectId { get; }
    public ShuffleDiscardCardToDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetDiscard(context.Player).ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Mélanger dans le deck", "Choisissez une carte de votre défausse à remettre dans votre deck", candidates, chosen =>
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, chosen);
            context.Engine.State.AddCardToBottomDeck(context.Player, chosen);
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class ShuffleHandIntoDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _drawAfter;
    private readonly bool _scoreAfter;
    public ShuffleHandIntoDeckEffect(string effectId, int drawAfter, bool scoreAfter) { EffectId = effectId; _drawAfter = drawAfter; _scoreAfter = scoreAfter; }
    public DuelActionResult Execute(EffectContext context)
    {
        var hand = context.Engine.State.GetHand(context.Player).ToList();
        foreach (var c in hand)
        {
            context.Engine.State.RemoveCardFromHand(context.Player, c);
            context.Engine.State.AddCardToBottomDeck(context.Player, c);
        }
        if (_drawAfter > 0) context.Engine.DrawCards(context.Player, _drawAfter);
        if (_scoreAfter && !context.Engine.State.IsGameOver)
        {
            var activeSlots = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State)
                .Where(i => context.Engine.State.Slots[i].IsActive(context.Engine.State.TurnNumber))
                .ToList();
            if (activeSlots.Count > 0)
            {
                EffectsS3S5.SelectSlotOrAuto(context, "Scorer un terrain", "Choisissez un terrain actif à scorer", activeSlots, s => context.Engine.ResolveScore(s, context.Player));
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class ShuffleHandIntoDeckIfUnstackedEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _draw;
    public ShuffleHandIntoDeckIfUnstackedEffect(string effectId, int draw) { EffectId = effectId; _draw = draw; }
    public DuelActionResult Execute(EffectContext context)
    {
        new ShuffleHandAndDrawEffect(EffectId, _draw).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class LookTopDeckTakeAndBottomEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _lookCount;
    private readonly int _takeCount;
    public LookTopDeckTakeAndBottomEffect(string effectId, int lookCount, int takeCount) { EffectId = effectId; _lookCount = lookCount; _takeCount = takeCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var revealed = new List<DuelCard>();
        for (int i = 0; i < _lookCount; i++)
        {
            var c = context.Engine.State.PopTopDeckCard(context.Player);
            if (c == null) break;
            revealed.Add(c);
        }
        if (revealed.Count == 0) return DuelActionResult.Ok();
        context.Engine.Emit(new CardsRevealedEvent(context.Player, revealed));

        var ordered = revealed.OrderByDescending(c => c.Force).ToList();
        int take = Math.Min(_takeCount, ordered.Count);
        for (int i = 0; i < take; i++)
            context.Engine.State.AddCardToHand(context.Player, ordered[i]);
        for (int i = take; i < ordered.Count; i++)
            context.Engine.State.AddCardToBottomDeck(context.Player, ordered[i]);
        context.Engine.CheckDeckOutCondition(context.Player);
        return DuelActionResult.Ok();
    }
}

internal sealed class LookTopDeck5ReorderEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _count;
    public LookTopDeck5ReorderEffect(string effectId, int count) { EffectId = effectId; _count = count; }
    public DuelActionResult Execute(EffectContext context)
    {
        var deck = context.Engine.State.GetDeck(context.Player);
        var revealed = deck.Take(Math.Min(_count, deck.Count)).ToList();
        if (revealed.Count > 0) context.Engine.Emit(new CardsRevealedEvent(context.Player, revealed));
        return DuelActionResult.Ok();
    }
}

internal sealed class LookOppTop3Discard1ReorderEffect : IEffect
{
    public string EffectId { get; }
    public LookOppTop3Discard1ReorderEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var oppDeck = context.Engine.State.GetDeck(opp);
        var revealed = oppDeck.Take(Math.Min(3, oppDeck.Count)).ToList();
        if (revealed.Count == 0) return DuelActionResult.Ok();
        context.Engine.Emit(new CardsRevealedEvent(context.Player, revealed));
        var toDiscard = revealed.OrderByDescending(c => c.Force).First();
        if (context.Engine.State.RemoveCardFromDeck(opp, toDiscard))
        {
            context.Engine.State.AddCardToDiscard(opp, toDiscard);
            context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { toDiscard }));
            context.Engine.CheckDeckOutCondition(opp);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class PutTopDeckFacedownUnderSelfEffect : IEffect
{
    public string EffectId { get; }
    public PutTopDeckFacedownUnderSelfEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var card = context.Engine.State.PopTopDeckCard(context.Player);
        if (card == null) return DuelActionResult.Ok();
        var aux = EffectsS3S5.GetAuxState(context.Engine.State);
        if (!aux.FacedownByCardId.TryGetValue(context.SourceCard.Id, out var list))
        {
            list = new List<DuelCard>();
            aux.FacedownByCardId[context.SourceCard.Id] = list;
        }
        list.Add(card);
        context.Engine.CheckDeckOutCondition(context.Player);
        return DuelActionResult.Ok();
    }
}

internal sealed class TakeFacedownToHandEffect : IEffect
{
    public string EffectId { get; }
    public TakeFacedownToHandEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var aux = EffectsS3S5.GetAuxState(context.Engine.State);
        foreach (var kv in aux.FacedownByCardId)
        {
            if (kv.Value.Count > 0)
            {
                var c = kv.Value[kv.Value.Count - 1];
                kv.Value.RemoveAt(kv.Value.Count - 1);
                context.Engine.State.AddCardToHand(context.Player, c);
                break;
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class DiscardFacedownInPlayEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _amount;
    public DiscardFacedownInPlayEffect(string effectId, int amount) { EffectId = effectId; _amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var aux = EffectsS3S5.GetAuxState(context.Engine.State);
        int discarded = 0;
        foreach (var kv in aux.FacedownByCardId)
        {
            while (kv.Value.Count > 0 && discarded < _amount)
            {
                var c = kv.Value[kv.Value.Count - 1];
                kv.Value.RemoveAt(kv.Value.Count - 1);
                context.Engine.State.AddCardToDiscard(context.Player, c);
                discarded++;
            }
            if (discarded >= _amount) break;
        }
        return DuelActionResult.Ok();
    }
}

// =============================================================================
// Suppression / Bounce / Déplacement / Échange sur plateau
// =============================================================================
internal sealed class DiscardOppStrongestCharEffect : IEffect
{
    public string EffectId { get; }
    public DiscardOppStrongestCharEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        DuelCard? best = null;
        TerrainSlot? bestSlot = null;
        foreach (var s in context.Engine.State.Slots)
        {
            if (s.IsEmpty) continue;
            foreach (var c in s.GetCharacters(opp))
            {
                if (best == null || c.Force > best.Force) { best = c; bestSlot = s; }
            }
        }
        if (best != null && bestSlot != null && bestSlot.RemoveCharacter(opp, best))
        {
            context.Engine.State.AddCardToDiscard(opp, best);
            context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { best }));
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class DiscardRelativeForceCharEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _anySide;
    private readonly bool _moveAfter;
    public DiscardRelativeForceCharEffect(string effectId, bool anySide, bool moveAfter) { EffectId = effectId; _anySide = anySide; _moveAfter = moveAfter; }
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var candidates = new List<(PlayerId Owner, TerrainSlot Slot, DuelCard Card)>();
        void Collect(TerrainSlot s, PlayerId owner)
        {
            foreach (var c in s.GetCharacters(owner))
                if (c.Force < context.SourceCard.Force && !ReferenceEquals(c, context.SourceCard))
                    candidates.Add((owner, s, c));
        }
        if (_anySide)
        {
            foreach (var s in context.Engine.State.Slots)
            {
                if (s.IsEmpty) continue;
                Collect(s, opp);
                Collect(s, context.Player);
            }
        }
        else if (context.SlotIndex >= 0 && context.SlotIndex < context.Engine.State.Slots.Count)
        {
            var s = context.Engine.State.Slots[context.SlotIndex];
            Collect(s, opp);
            Collect(s, context.Player);
        }

        if (candidates.Count > 0)
        {
            EffectsS3S5.SelectCandidateOrAuto(context, "Défausser un personnage", "Choisissez un personnage de Force inférieure", candidates.Select(x => x.Card).ToList(), chosen =>
            {
                var entry = candidates.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
                if (entry.Slot != null && entry.Slot.RemoveCharacter(entry.Owner, entry.Card))
                {
                    context.Engine.State.AddCardToDiscard(entry.Owner, entry.Card);
                    context.Engine.Emit(new CardsDiscardedEvent(entry.Owner, new[] { entry.Card }));
                }
            }, list => list.OrderByDescending(c => c.Force).First());
        }
        if (_moveAfter)
        {
            var others = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State, context.SlotIndex);
            if (others.Count > 0 && EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out _, out int fromIdx, out _))
            {
                EffectsS3S5.SelectSlotOrAuto(context, "Déplacer ce personnage", "Choisissez un terrain de destination", others, toIdx =>
                    context.Engine.MoveCharacter(context.Player, context.SourceCard.Id, fromIdx, toIdx));
            }
        }
        return DuelActionResult.Ok();
    }
}

internal enum DynamicForceMode { BanishMax6, CostLtCowboys, CombinedMotardForce }

internal sealed class DiscardOppCharByDynamicMaxForceEffect : IEffect
{
    public string EffectId { get; }
    private readonly DynamicForceMode _mode;
    public DiscardOppCharByDynamicMaxForceEffect(string effectId, DynamicForceMode mode) { EffectId = effectId; _mode = mode; }

    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        int limit = 0;
        if (_mode == DynamicForceMode.BanishMax6)
            limit = Math.Min(6, context.Engine.State.GetBanish(context.Player).Count);
        else if (_mode == DynamicForceMode.CostLtCowboys)
        {
            foreach (var s in context.Engine.State.Slots)
                if (!s.IsEmpty) limit += s.CharactersP1.Concat(s.CharactersP2).Count(c => c.Name.IndexOf("Cowboy", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        else
        {
            foreach (var s in context.Engine.State.Slots)
                if (!s.IsEmpty) limit += s.GetCharacters(context.Player).Where(c => c.Name.IndexOf("Motard", StringComparison.OrdinalIgnoreCase) >= 0).Sum(c => c.Force);
        }

        var candidates = new List<(TerrainSlot Slot, DuelCard Card)>();
        foreach (var s in context.Engine.State.Slots)
        {
            if (s.IsEmpty) continue;
            foreach (var c in s.GetCharacters(opp))
            {
                bool ok = _mode switch
                {
                    DynamicForceMode.BanishMax6 => c.Force <= limit,
                    DynamicForceMode.CostLtCowboys => S3S5Bridge.GetCardCost(c) < limit,
                    _ => c.Force < limit
                };
                if (ok) candidates.Add((s, c));
            }
        }
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Défausser un personnage adverse", "Choisissez une cible valide", candidates.Select(x => x.Card).ToList(), chosen =>
        {
            var entry = candidates.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
            if (entry.Slot != null && entry.Slot.RemoveCharacter(opp, entry.Card))
            {
                context.Engine.State.AddCardToDiscard(opp, entry.Card);
                context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { entry.Card }));
            }
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class DiscardOppCharsTotalForceEqCountEffect : IEffect
{
    public string EffectId { get; }
    public DiscardOppCharsTotalForceEqCountEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        var slot = context.Engine.State.Slots[context.SlotIndex];
        int budget = slot.GetCharacters(context.Player).Count;
        var opp = context.Player.Opponent();
        var oppChars = slot.GetCharacters(opp).OrderByDescending(c => c.Force).ToList();
        foreach (var c in oppChars)
        {
            if (c.Force <= budget && slot.RemoveCharacter(opp, c))
            {
                budget -= c.Force;
                context.Engine.State.AddCardToDiscard(opp, c);
                context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { c }));
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class DiscardAllOtherCharsEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _allSlots;
    private readonly bool _ownOnly;
    private readonly bool _millSelf;
    public DiscardAllOtherCharsEffect(string effectId, bool allSlots, bool ownOnly, bool millSelf)
    {
        EffectId = effectId; _allSlots = allSlots; _ownOnly = ownOnly; _millSelf = millSelf;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        int discardedTotal = 0;
        var slots = _allSlots
            ? context.Engine.State.Slots
            : (context.SlotIndex >= 0 && context.SlotIndex < context.Engine.State.Slots.Count ? new[] { context.Engine.State.Slots[context.SlotIndex] } : Array.Empty<TerrainSlot>());

        foreach (var s in slots)
        {
            if (s.IsEmpty) continue;
            discardedTotal += ClearSide(s, context.Player);
            if (!_ownOnly) discardedTotal += ClearSide(s, context.Player.Opponent());
        }
        if (_millSelf && discardedTotal > 0) context.Engine.MillCards(context.Player, discardedTotal);
        return DuelActionResult.Ok();

        int ClearSide(TerrainSlot slot, PlayerId owner)
        {
            var list = slot.GetCharacters(owner).Where(c => !ReferenceEquals(c, context.SourceCard) && !string.Equals(c.Id, context.SourceCard.Id, StringComparison.Ordinal)).ToList();
            foreach (var c in list)
            {
                if (slot.RemoveCharacter(owner, c)) context.Engine.State.AddCardToDiscard(owner, c);
            }
            if (list.Count > 0) context.Engine.Emit(new CardsDiscardedEvent(owner, list));
            return list.Count;
        }
    }
}

internal sealed class DiscardOwnCharBonusEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _gainForce;
    public DiscardOwnCharBonusEffect(string effectId, bool gainForce) { EffectId = effectId; _gainForce = gainForce; }

    public DuelActionResult Execute(EffectContext context)
    {
        var own = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty)
                foreach (var c in s.GetCharacters(context.Player))
                    if (!ReferenceEquals(c, context.SourceCard)) own.Add(c);
        if (own.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Défausser un personnage allié", "Choisissez un personnage à défausser", own, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, chosen, -1, out var slot, out _, out _) && slot != null && slot.RemoveCharacter(context.Player, chosen))
            {
                context.Engine.State.AddCardToDiscard(context.Player, chosen);
                context.Engine.Emit(new CardsDiscardedEvent(context.Player, new[] { chosen }));
                if (_gainForce)
                {
                    if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var srcSlot, out _, out int idx) && srcSlot != null)
                        EffectsS3S5.ReplaceCharacterAtIndex(srcSlot, context.Player, context.SourceCard, EffectsS3S5.CloneCard(context.SourceCard, force: context.SourceCard.Force + chosen.Force), idx);
                }
                else if (chosen.Force > 0)
                {
                    new PutTopDiscardOnTopDeckEffect(EffectId, chosen.Force).Execute(context);
                }
            }
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class ConditionalBoardRemovalEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _banish;
    private readonly int _maxForce;
    public ConditionalBoardRemovalEffect(string effectId, bool banish, int maxForce) { EffectId = effectId; _banish = banish; _maxForce = maxForce; }

    public DuelActionResult Execute(EffectContext context)
    {
        if (context.Engine.State.GetScore(context.Player) >= context.Engine.State.GetScore(context.Player.Opponent()))
            return DuelActionResult.Ok();
        var opp = context.Player.Opponent();
        var candidates = new List<(TerrainSlot Slot, DuelCard Card)>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty)
                foreach (var c in s.GetCharacters(opp))
                    if (c.Force <= _maxForce) candidates.Add((s, c));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, _banish ? "Bannir un personnage adverse" : "Défausser un personnage adverse", "Choisissez une cible", candidates.Select(x => x.Card).ToList(), chosen =>
        {
            var entry = candidates.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
            if (entry.Slot != null && entry.Slot.RemoveCharacter(opp, entry.Card))
            {
                if (_banish)
                {
                    context.Engine.State.AddCardToBanish(opp, entry.Card);
                    context.Engine.Emit(new CardsBanishedEvent(opp, new[] { entry.Card }));
                }
                else
                {
                    context.Engine.State.AddCardToDiscard(opp, entry.Card);
                    context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { entry.Card }));
                }
            }
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class RowTargetedBoardRemovalEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _banish;
    private readonly int _rowIndex;
    public RowTargetedBoardRemovalEffect(string effectId, bool banish, int rowIndex) { EffectId = effectId; _banish = banish; _rowIndex = rowIndex; }

    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var candidates = new List<(TerrainSlot Slot, DuelCard Card)>();
        foreach (var s in context.Engine.State.Slots)
        {
            if (s.IsEmpty) continue;
            var chars = s.GetCharacters(opp);
            if (_rowIndex < 0)
                foreach (var c in chars) candidates.Add((s, c));
            else if (_rowIndex < chars.Count)
                candidates.Add((s, chars[_rowIndex]));
        }
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, _banish ? "Bannir un personnage adverse" : "Défausser un personnage adverse", "Choisissez une cible", candidates.Select(x => x.Card).ToList(), chosen =>
        {
            var entry = candidates.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
            if (entry.Slot != null && entry.Slot.RemoveCharacter(opp, entry.Card))
            {
                if (_banish)
                {
                    context.Engine.State.AddCardToBanish(opp, entry.Card);
                    context.Engine.Emit(new CardsBanishedEvent(opp, new[] { entry.Card }));
                }
                else
                {
                    context.Engine.State.AddCardToDiscard(opp, entry.Card);
                    context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { entry.Card }));
                }
            }
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class BounceOppCharsByPrintedForce0CountEffect : IEffect
{
    public string EffectId { get; }
    public BounceOppCharsByPrintedForce0CountEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        int count = 0;
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) count += s.GetCharacters(context.Player).Count(c => c.Force == 0);
        var opp = context.Player.Opponent();
        for (int i = 0; i < count; i++)
        {
            DuelCard? target = null;
            TerrainSlot? targetSlot = null;
            foreach (var s in context.Engine.State.Slots)
                if (!s.IsEmpty && s.GetCharacters(opp).Count > 0)
                {
                    target = s.GetCharacters(opp)[0];
                    targetSlot = s;
                    break;
                }
            if (target == null || targetSlot == null) break;
            if (targetSlot.RemoveCharacter(opp, target)) context.Engine.State.AddCardToHand(opp, target);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class BanishOwnCharAndScoreEffect : IEffect
{
    public string EffectId { get; }
    public BanishOwnCharAndScoreEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var own = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) own.AddRange(s.GetCharacters(context.Player));
        if (own.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Bannir un personnage pour scorer", "Choisissez un de vos personnages à bannir", own, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, chosen, -1, out var slot, out _, out _) && slot != null && slot.RemoveCharacter(context.Player, chosen))
            {
                context.Engine.State.AddCardToBanish(context.Player, chosen);
                context.Engine.Emit(new CardsBanishedEvent(context.Player, new[] { chosen }));
                var active = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State).Where(i => context.Engine.State.Slots[i].IsActive(context.Engine.State.TurnNumber)).ToList();
                if (active.Count > 0)
                    EffectsS3S5.SelectSlotOrAuto(context, "Scorer un terrain", "Choisissez un terrain actif à scorer", active, s => context.Engine.ResolveScore(s, context.Player));
            }
        }, list => list.OrderBy(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class BunkerMove3DiscardRestEffect : IEffect
{
    public string EffectId { get; }
    public BunkerMove3DiscardRestEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            int onSlot = context.Engine.State.Slots[context.SlotIndex].GetCharacters(p).Count;
            for (int s = 0; s < context.Engine.State.Slots.Count; s++)
            {
                if (s == context.SlotIndex || context.Engine.State.Slots[s].IsEmpty) continue;
                var list = context.Engine.State.Slots[s].GetCharacters(p).ToList();
                foreach (var c in list)
                {
                    if (onSlot < 3)
                    {
                        context.Engine.MoveCharacter(p, c.Id, s, context.SlotIndex);
                        onSlot++;
                    }
                    else if (context.Engine.State.Slots[s].RemoveCharacter(p, c))
                    {
                        context.Engine.State.AddCardToDiscard(p, c);
                    }
                }
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class MoveAllOtherCharsOnSlotEffect : IEffect
{
    public string EffectId { get; }
    public MoveAllOtherCharsOnSlotEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var others = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State, context.SlotIndex);
        if (others.Count == 0 || context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        EffectsS3S5.SelectSlotOrAuto(context, "Déplacer tous les autres personnages", "Choisissez le terrain de destination", others, dest =>
        {
            foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                var list = context.Engine.State.Slots[context.SlotIndex].GetCharacters(p)
                    .Where(c => !ReferenceEquals(c, context.SourceCard))
                    .ToList();
                foreach (var c in list) context.Engine.MoveCharacter(p, c.Id, context.SlotIndex, dest);
            }
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class MoveCost0CharsHereEffect : IEffect
{
    public string EffectId { get; }
    public MoveCost0CharsHereEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        for (int s = 0; s < context.Engine.State.Slots.Count; s++)
        {
            if (s == context.SlotIndex || context.Engine.State.Slots[s].IsEmpty) continue;
            foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                var list = context.Engine.State.Slots[s].GetCharacters(p).Where(c => S3S5Bridge.GetCardCost(c) == 0).ToList();
                foreach (var c in list) context.Engine.MoveCharacter(p, c.Id, s, context.SlotIndex);
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class MoveCharAndDrawForceMax4Effect : IEffect
{
    public string EffectId { get; }
    public MoveCharAndDrawForceMax4Effect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var occupied = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State);
        if (occupied.Count < 2) return DuelActionResult.Ok();
        var candidates = new List<DuelCard>();
        foreach (var s in occupied) candidates.AddRange(context.Engine.State.Slots[s].GetCharacters(context.Player));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Déplacer et piocher", "Choisissez un personnage à déplacer", candidates, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, chosen, -1, out _, out int fromIdx, out _))
            {
                var dests = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State, fromIdx);
                if (dests.Count > 0)
                {
                    EffectsS3S5.SelectSlotOrAuto(context, "Destination", "Choisissez un autre terrain", dests, toIdx =>
                    {
                        if (context.Engine.MoveCharacter(context.Player, chosen.Id, fromIdx, toIdx).Success)
                        {
                            int toDraw = Math.Min(4, Math.Max(0, chosen.Force));
                            if (toDraw > 0) context.Engine.DrawCards(context.Player, toDraw);
                        }
                    });
                }
            }
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class MoveOppCharToThisSlotEffect : IEffect
{
    public string EffectId { get; }
    public MoveOppCharToThisSlotEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        var opp = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        for (int s = 0; s < context.Engine.State.Slots.Count; s++)
            if (s != context.SlotIndex && !context.Engine.State.Slots[s].IsEmpty)
                candidates.AddRange(context.Engine.State.Slots[s].GetCharacters(opp));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Déplacer un personnage adverse", "Choisissez un personnage adverse à amener ici", candidates, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opp, chosen, -1, out _, out int fromIdx, out _))
                context.Engine.MoveCharacter(opp, chosen.Id, fromIdx, context.SlotIndex);
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class MoveAllyToOppReturnDiscardEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _returnCount;
    public MoveAllyToOppReturnDiscardEffect(string effectId, int returnCount) { EffectId = effectId; _returnCount = returnCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var own = new List<(TerrainSlot Slot, DuelCard Card)>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty)
                foreach (var c in s.GetCharacters(context.Player)) own.Add((s, c));
        if (own.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Donner un personnage à l'adversaire", "Choisissez un personnage à déplacer côté adverse", own.Select(x => x.Card).ToList(), chosen =>
        {
            var entry = own.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
            if (entry.Slot != null && entry.Slot.RemoveCharacter(context.Player, entry.Card))
            {
                entry.Slot.AddCharacter(context.Player.Opponent(), entry.Card);
                new PutTopDiscardOnTopDeckEffect(EffectId, _returnCount).Execute(context);
            }
        }, list => list.OrderBy(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class MoveSelfAndAllyEffect : IEffect
{
    public string EffectId { get; }
    public MoveSelfAndAllyEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (!EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var srcSlot, out int fromIdx, out _) || srcSlot == null)
            return DuelActionResult.Ok();
        var dests = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State, fromIdx);
        if (dests.Count == 0) return DuelActionResult.Ok();

        var allies = srcSlot.GetCharacters(context.Player).Where(c => !ReferenceEquals(c, context.SourceCard)).ToList();
        EffectsS3S5.SelectSlotOrAuto(context, "Déplacer avec un allié", "Choisissez le terrain de destination", dests, destIdx =>
        {
            if (allies.Count > 0) context.Engine.MoveCharacter(context.Player, allies[0].Id, fromIdx, destIdx);
            context.Engine.MoveCharacter(context.Player, context.SourceCard.Id, fromIdx, destIdx);
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class SwapTwoOwnCharsInPlayEffect : IEffect
{
    public string EffectId { get; }
    public SwapTwoOwnCharsInPlayEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var entries = new List<(int SlotIdx, int CharIdx, DuelCard Card)>();
        for (int s = 0; s < context.Engine.State.Slots.Count; s++)
        {
            var slot = context.Engine.State.Slots[s];
            if (slot.IsEmpty) continue;
            var chars = slot.GetCharacters(context.Player);
            for (int i = 0; i < chars.Count; i++) entries.Add((s, i, chars[i]));
        }
        if (entries.Count < 2) return DuelActionResult.Ok();
        var a = entries[0];
        var b = entries[entries.Count - 1];
        if (a.SlotIdx == b.SlotIdx)
        {
            var slot = context.Engine.State.Slots[a.SlotIdx];
            EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, a.Card, b.Card, a.CharIdx);
            EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, b.Card, a.Card, b.CharIdx);
        }
        else
        {
            context.Engine.MoveCharacter(context.Player, a.Card.Id, a.SlotIdx, b.SlotIdx, b.CharIdx);
            context.Engine.MoveCharacter(context.Player, b.Card.Id, b.SlotIdx, a.SlotIdx, a.CharIdx);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class SwapSelfWithTopDiscardCharEffect : IEffect
{
    public string EffectId { get; }
    public SwapSelfWithTopDiscardCharEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var topChar = context.Engine.State.GetDiscard(context.Player).LastOrDefault(c => c.Kind == CardKind.Character);
        if (topChar == null) return DuelActionResult.Ok();
        if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int idx) && slot != null)
        {
            context.Engine.State.RemoveCardFromDiscard(context.Player, topChar);
            EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, context.SourceCard, topChar, idx);
            context.Engine.State.AddCardToDiscard(context.Player, context.SourceCard);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class SwapOppCharWithOppDiscardEffect : IEffect
{
    public string EffectId { get; }
    public SwapOppCharWithOppDiscardEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var oppDiscardChar = context.Engine.State.GetDiscard(opp).LastOrDefault(c => c.Kind == CardKind.Character);
        if (oppDiscardChar == null) return DuelActionResult.Ok();
        var oppBoard = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) oppBoard.AddRange(s.GetCharacters(opp));
        if (oppBoard.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Échanger un personnage adverse", "Choisissez un personnage adverse à échanger avec sa défausse", oppBoard, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opp, chosen, -1, out var slot, out _, out int idx) && slot != null)
            {
                context.Engine.State.RemoveCardFromDiscard(opp, oppDiscardChar);
                EffectsS3S5.ReplaceCharacterAtIndex(slot, opp, chosen, oppDiscardChar, idx);
                context.Engine.State.AddCardToDiscard(opp, chosen);
            }
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class SwapTerrainWithDiscardEffect : IEffect
{
    public string EffectId { get; }
    public SwapTerrainWithDiscardEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var discardTerrains = context.Engine.State.GetDiscard(context.Player).Where(c => c.Kind == CardKind.Terrain).ToList();
        var occupied = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State);
        if (discardTerrains.Count == 0 || occupied.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Choisir un terrain en défausse", "Choisissez le terrain à mettre en jeu", discardTerrains, newTerrain =>
        {
            EffectsS3S5.SelectSlotOrAuto(context, "Terrain à remplacer", "Choisissez le terrain en jeu à échanger", occupied, slotIdx =>
            {
                var oldSlot = context.Engine.State.Slots[slotIdx];
                var oldTerrain = oldSlot.Card;
                var p1 = oldSlot.CharactersP1.ToList();
                var p2 = oldSlot.CharactersP2.ToList();
                context.Engine.State.RemoveCardFromDiscard(context.Player, newTerrain);
                context.Engine.State.SetSlot(slotIdx, newTerrain, oldSlot.PlacedOnTurn, context.Player);
                foreach (var c in p1) context.Engine.State.AddCharacterToSlot(slotIdx, PlayerId.Player1, c);
                foreach (var c in p2) context.Engine.State.AddCharacterToSlot(slotIdx, PlayerId.Player2, c);
                if (oldTerrain != null) context.Engine.State.AddCardToDiscard(oldSlot.PlacedBy ?? context.Player, oldTerrain);
            });
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class ShuffleCharToDeckEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _bothSides;
    public ShuffleCharToDeckEffect(string effectId, bool bothSides) { EffectId = effectId; _bothSides = bothSides; }
    public DuelActionResult Execute(EffectContext context)
    {
        ShuffleSide(context, context.Player.Opponent());
        if (_bothSides) ShuffleSide(context, context.Player);
        return DuelActionResult.Ok();
    }

    private static void ShuffleSide(EffectContext context, PlayerId owner)
    {
        var chars = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) chars.AddRange(s.GetCharacters(owner));
        if (chars.Count == 0) return;

        EffectsS3S5.SelectCandidateOrAuto(context, "Mélanger un personnage dans son deck", "Choisissez un personnage à renvoyer dans son deck", chars, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, owner, chosen, -1, out var slot, out _, out _) && slot != null && slot.RemoveCharacter(owner, chosen))
                context.Engine.State.AddCardToBottomDeck(owner, chosen);
        }, list => owner == context.Player ? list.OrderBy(c => c.Force).First() : list.OrderByDescending(c => c.Force).First());
    }
}

// =============================================================================
// Force / Modificateurs / Dés / Score / Spéciaux
// =============================================================================
internal sealed class BanishHand2DoubleSelfForceEffect : IEffect
{
    public string EffectId { get; }
    public BanishHand2DoubleSelfForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.Engine.State.GetHand(context.Player).Count < 2) return DuelActionResult.Ok();
        context.Engine.BanishCardsFromHand(context.Player, 2);
        if (!context.Engine.State.IsGameOver &&
            EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int idx) && slot != null)
        {
            var cur = slot.GetCharacters(context.Player)[idx];
            EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, cur, EffectsS3S5.CloneCard(cur, force: cur.Force * 2), idx);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class ModifyCharForceAndConditionalDiscardEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _delta;
    private readonly int? _discardIfGe;
    private readonly int? _discardIfLe;
    public ModifyCharForceAndConditionalDiscardEffect(string effectId, int delta, int? discardIfGe, int? discardIfLe)
    {
        EffectId = effectId; _delta = delta; _discardIfGe = discardIfGe; _discardIfLe = discardIfLe;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) candidates.AddRange(s.GetCharacters(opp));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Modifier Force adverse", "Choisissez un personnage adverse", candidates, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opp, chosen, -1, out var slot, out _, out int idx) && slot != null)
            {
                int newForce = Math.Max(0, chosen.Force + _delta);
                if ((_discardIfGe.HasValue && newForce >= _discardIfGe.Value) || (_discardIfLe.HasValue && newForce <= _discardIfLe.Value))
                {
                    if (slot.RemoveCharacter(opp, chosen))
                    {
                        context.Engine.State.AddCardToDiscard(opp, chosen);
                        context.Engine.Emit(new CardsDiscardedEvent(opp, new[] { chosen }));
                    }
                }
                else
                {
                    EffectsS3S5.ReplaceCharacterAtIndex(slot, opp, chosen, EffectsS3S5.CloneCard(chosen, force: newForce), idx);
                }
            }
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class BoostOtherCharEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _bonus;
    private readonly bool _ownOnly;
    private readonly bool _scoreAfter;
    public BoostOtherCharEffect(string effectId, int bonus, bool ownOnly, bool scoreAfter)
    {
        EffectId = effectId; _bonus = bonus; _ownOnly = ownOnly; _scoreAfter = scoreAfter;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = new List<(PlayerId Owner, DuelCard Card)>();
        foreach (var s in context.Engine.State.Slots)
        {
            if (s.IsEmpty) continue;
            foreach (var c in s.GetCharacters(context.Player))
                if (!ReferenceEquals(c, context.SourceCard)) candidates.Add((context.Player, c));
            if (!_ownOnly)
                foreach (var c in s.GetCharacters(context.Player.Opponent())) candidates.Add((context.Player.Opponent(), c));
        }
        if (candidates.Count > 0)
        {
            EffectsS3S5.SelectCandidateOrAuto(context, $"Donner +{_bonus} en Force", "Choisissez un autre personnage", candidates.Select(x => x.Card).ToList(), chosen =>
            {
                var entry = candidates.FirstOrDefault(x => ReferenceEquals(x.Card, chosen));
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, entry.Owner, chosen, -1, out var slot, out _, out int idx) && slot != null)
                    EffectsS3S5.ReplaceCharacterAtIndex(slot, entry.Owner, chosen, EffectsS3S5.CloneCard(chosen, force: chosen.Force + _bonus), idx);
            });
        }
        if (_scoreAfter && !context.Engine.State.IsGameOver)
            new ScoreMultipleTerrainsEffect(EffectId, 1, excludeCurrent: false).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class DebuffSlotCharsEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _penalty;
    public DebuffSlotCharsEffect(string effectId, int penalty) { EffectId = effectId; _penalty = penalty; }
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        var slot = context.Engine.State.Slots[context.SlotIndex];
        foreach (var p in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            var chars = slot.GetCharacters(p).ToList();
            for (int i = 0; i < chars.Count; i++)
                EffectsS3S5.ReplaceCharacterAtIndex(slot, p, chars[i], EffectsS3S5.CloneCard(chars[i], force: Math.Max(0, chars[i].Force - _penalty)), i);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class DoubleOppCharForceEffect : IEffect
{
    public string EffectId { get; }
    public DoubleOppCharForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) candidates.AddRange(s.GetCharacters(opp));
        if (candidates.Count > 0)
        {
            EffectsS3S5.SelectCandidateOrAuto(context, "Doubler la Force adverse", "Choisissez un personnage adverse", candidates, chosen =>
            {
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opp, chosen, -1, out var slot, out _, out int idx) && slot != null)
                    EffectsS3S5.ReplaceCharacterAtIndex(slot, opp, chosen, EffectsS3S5.CloneCard(chosen, force: chosen.Force * 2), idx);
            });
        }
        context.Engine.DrawCards(context.Player, 3);
        return DuelActionResult.Ok();
    }
}

internal sealed class StealOppCharForceEffect : IEffect
{
    public string EffectId { get; }
    public StealOppCharForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) candidates.AddRange(s.GetCharacters(opp));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Ajouter la Force d'un personnage adverse", "Choisissez un personnage adverse", candidates, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int idx) && slot != null)
            {
                var cur = slot.GetCharacters(context.Player)[idx];
                EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, cur, EffectsS3S5.CloneCard(cur, force: cur.Force + chosen.Force), idx);
            }
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class ZeroOppCharForceEffect : IEffect
{
    public string EffectId { get; }
    public ZeroOppCharForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var candidates = new List<DuelCard>();
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty) candidates.AddRange(s.GetCharacters(opp));
        if (candidates.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Ignorer la Force d'un personnage adverse", "Choisissez un personnage adverse", candidates, chosen =>
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, opp, chosen, -1, out var slot, out _, out int idx) && slot != null)
                EffectsS3S5.ReplaceCharacterAtIndex(slot, opp, chosen, EffectsS3S5.CloneCard(chosen, force: 0), idx);
        }, list => list.OrderByDescending(c => c.Force).First());
        return DuelActionResult.Ok();
    }
}

internal sealed class MultiplySlotForceByCombosEffect : IEffect
{
    public string EffectId { get; }
    public MultiplySlotForceByCombosEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return DuelActionResult.Ok();
        int combos = Math.Max(1, EffectsS3S5.CountAllCombosInPlay(context.Engine.State));
        var slot = context.Engine.State.Slots[context.SlotIndex];
        var chars = slot.GetCharacters(context.Player).ToList();
        for (int i = 0; i < chars.Count; i++)
            EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, chars[i], EffectsS3S5.CloneCard(chars[i], force: chars[i].Force * combos), i);
        return DuelActionResult.Ok();
    }
}

internal sealed class BoostSelfByNameInPlayEffect : IEffect
{
    public string EffectId { get; }
    private readonly string _keyword;
    public BoostSelfByNameInPlayEffect(string effectId, string keyword) { EffectId = effectId; _keyword = keyword; }
    public DuelActionResult Execute(EffectContext context)
    {
        int count = 0;
        foreach (var s in context.Engine.State.Slots)
            if (!s.IsEmpty)
                count += s.CharactersP1.Concat(s.CharactersP2).Count(c => c.Name.IndexOf(_keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        if (count > 0 && EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int idx) && slot != null)
        {
            var cur = slot.GetCharacters(context.Player)[idx];
            EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, cur, EffectsS3S5.CloneCard(cur, force: cur.Force + count), idx);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class DiscardNamedAllyBoostEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _discardSelf;
    private readonly string _targetName;
    private readonly int _bonus;
    public DiscardNamedAllyBoostEffect(string effectId, bool discardSelf, string targetName, int bonus)
    {
        EffectId = effectId; _discardSelf = discardSelf; _targetName = targetName; _bonus = bonus;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        DuelCard? match = null;
        TerrainSlot? matchSlot = null;
        foreach (var s in context.Engine.State.Slots)
        {
            if (s.IsEmpty) continue;
            match = s.GetCharacters(context.Player).FirstOrDefault(c => c.Name.IndexOf(_targetName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) { matchSlot = s; break; }
        }
        if (match == null || matchSlot == null) return DuelActionResult.Ok();

        if (_discardSelf)
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var srcSlot, out _, out _) && srcSlot != null && srcSlot.RemoveCharacter(context.Player, context.SourceCard))
            {
                context.Engine.State.AddCardToDiscard(context.Player, context.SourceCard);
                if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, match, -1, out _, out _, out int idx))
                    EffectsS3S5.ReplaceCharacterAtIndex(matchSlot, context.Player, match, EffectsS3S5.CloneCard(match, force: match.Force + _bonus), idx);
            }
        }
        else if (matchSlot.RemoveCharacter(context.Player, match))
        {
            context.Engine.State.AddCardToDiscard(context.Player, match);
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var srcSlot, out _, out int idx) && srcSlot != null)
            {
                var cur = srcSlot.GetCharacters(context.Player)[idx];
                EffectsS3S5.ReplaceCharacterAtIndex(srcSlot, context.Player, cur, EffectsS3S5.CloneCard(cur, force: cur.Force + _bonus), idx);
            }
        }
        return DuelActionResult.Ok();
    }
}

internal enum RollDiceMode { SelfMill, BoostSelf, DiscardOppChar }

internal sealed class RollDiceEffect : IEffect
{
    public string EffectId { get; }
    private readonly RollDiceMode _mode;
    public RollDiceEffect(string effectId, RollDiceMode mode) { EffectId = effectId; _mode = mode; }

    public DuelActionResult Execute(EffectContext context)
    {
        int roll = S3S5Bridge.RollNext(context.Engine, 1, 7);
        if (_mode == RollDiceMode.SelfMill)
        {
            context.Engine.MillCards(context.Player, roll);
        }
        else if (_mode == RollDiceMode.BoostSelf)
        {
            if (EffectsS3S5.TryFindCharacterInSlots(context.Engine.State, context.Player, context.SourceCard, context.SlotIndex, out var slot, out _, out int idx) && slot != null)
            {
                var cur = slot.GetCharacters(context.Player)[idx];
                EffectsS3S5.ReplaceCharacterAtIndex(slot, context.Player, cur, EffectsS3S5.CloneCard(cur, force: cur.Force + roll), idx);
            }
        }
        else
        {
            new DiscardCharacterByMaxForceEffect(EffectId, roll).Execute(context);
        }
        return DuelActionResult.Ok();
    }
}

internal enum CoinFlipMode { DiscardOppChar, PlayCharFree }

internal sealed class CoinFlipEffect : IEffect
{
    public string EffectId { get; }
    private readonly CoinFlipMode _mode;
    public CoinFlipEffect(string effectId, CoinFlipMode mode) { EffectId = effectId; _mode = mode; }

    public DuelActionResult Execute(EffectContext context)
    {
        if (S3S5Bridge.RollNext(context.Engine, 0, 2) == 0) return DuelActionResult.Ok();
        if (_mode == CoinFlipMode.DiscardOppChar)
            new DiscardCharacterByMaxForceEffect(EffectId, int.MaxValue).Execute(context);
        else
            new PlayFreeCharFromZoneEffect(EffectId, ZoneSource.Hand, 0, false).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class ScoreMultipleTerrainsEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _count;
    private readonly bool _excludeCurrent;
    public ScoreMultipleTerrainsEffect(string effectId, int count, bool excludeCurrent) { EffectId = effectId; _count = count; _excludeCurrent = excludeCurrent; }

    public DuelActionResult Execute(EffectContext context)
    {
        for (int i = 0; i < _count; i++)
        {
            if (context.Engine.State.IsGameOver) break;
            var active = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State, _excludeCurrent ? context.SlotIndex : -1)
                .Where(idx => context.Engine.State.Slots[idx].IsActive(context.Engine.State.TurnNumber))
                .ToList();
            if (active.Count == 0) break;
            EffectsS3S5.SelectSlotOrAuto(context, "Scorer un terrain", "Choisissez un terrain actif à scorer", active, s => context.Engine.ResolveScore(s, context.Player));
        }
        return DuelActionResult.Ok();
    }
}

internal enum ScoreCondMode { AlwaysAllow, FewerCharsThanOpp, ForceLt7 }

internal sealed class ScoreConditionalEffect : IEffect
{
    public string EffectId { get; }
    private readonly ScoreCondMode _mode;
    public ScoreConditionalEffect(string effectId, ScoreCondMode mode) { EffectId = effectId; _mode = mode; }
    public DuelActionResult Execute(EffectContext context)
    {
        var active = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State)
            .Where(i =>
            {
                var s = context.Engine.State.Slots[i];
                if (!s.IsActive(context.Engine.State.TurnNumber)) return false;
                return _mode switch
                {
                    ScoreCondMode.FewerCharsThanOpp => s.GetCharacters(context.Player).Count < s.GetCharacters(context.Player.Opponent()).Count,
                    ScoreCondMode.ForceLt7 => s.GetForce(context.Player) < 7,
                    _ => true
                };
            }).ToList();
        if (active.Count > 0)
            EffectsS3S5.SelectSlotOrAuto(context, "Scorer un terrain", "Choisissez un terrain éligible", active, s => context.Engine.ResolveScore(s, context.Player));
        return DuelActionResult.Ok();
    }
}

internal sealed class ScoreOppWinsPointEffect : IEffect
{
    public string EffectId { get; }
    public ScoreOppWinsPointEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var active = EffectsS3S5.GetOccupiedSlotIndices(context.Engine.State)
            .Where(i => context.Engine.State.Slots[i].IsActive(context.Engine.State.TurnNumber))
            .ToList();
        if (active.Count > 0)
            context.Engine.ResolveScore(active[0], context.Player);
        return DuelActionResult.Ok();
    }
}

internal sealed class ApplyAnyTerrainWinEffect : IEffect
{
    public string EffectId { get; }
    public ApplyAnyTerrainWinEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var slotsWithWin = context.Engine.State.Slots
            .Where(s => !s.IsEmpty && s.Card != null && s.Card.EffectIds.Any(e => e.StartsWith("win_", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (slotsWithWin.Count == 0) return DuelActionResult.Ok();

        EffectsS3S5.SelectCandidateOrAuto(context, "Appliquer effet Gagnant d'un terrain", "Choisissez un terrain en jeu", slotsWithWin.Select(s => s.Card!).ToList(), chosen =>
        {
            var slot = slotsWithWin.FirstOrDefault(s => ReferenceEquals(s.Card, chosen)) ?? slotsWithWin[0];
            foreach (var effId in chosen.EffectIds)
            {
                if (effId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) && context.Engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
                {
                    context.Engine.Emit(new EffectTriggeredEvent(context.Player, effId, chosen, slot.SlotIndex));
                    eff.Execute(new EffectContext(context.Engine, context.Player, chosen, slot.SlotIndex));
                }
            }
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class Mill2OppApplyLosingEffect : IEffect
{
    public string EffectId { get; }
    public Mill2OppApplyLosingEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.MillCards(context.Player, 2);
        if (!context.Engine.State.IsGameOver)
            new ApplyBothLosingEffectInPlayEffect(EffectId, bothPlayers: false).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class OppGainScoreAndReturnDiscardEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _returnCount;
    public OppGainScoreAndReturnDiscardEffect(string effectId, int returnCount) { EffectId = effectId; _returnCount = returnCount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        S3S5Bridge.AwardExtraPoint(context.Engine, opp, 1);
        if (!context.Engine.State.IsGameOver)
            new PutTopDiscardOnTopDeckEffect(EffectId, _returnCount).Execute(new EffectContext(context.Engine, opp, context.SourceCard, context.SlotIndex));
        return DuelActionResult.Ok();
    }
}

internal sealed class DisableTerrainWinLoseEffectsEffect : IEffect
{
    public string EffectId { get; }
    public DisableTerrainWinLoseEffectsEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        for (int i = 0; i < context.Engine.State.Slots.Count; i++)
        {
            var s = context.Engine.State.Slots[i];
            if (s.IsEmpty || s.Card == null) continue;
            var filtered = s.Card.EffectIds.Where(e => !e.StartsWith("win_", StringComparison.OrdinalIgnoreCase) && !e.StartsWith("lose_", StringComparison.OrdinalIgnoreCase)).ToList();
            if (filtered.Count != s.Card.EffectIds.Count)
            {
                var p1 = s.CharactersP1.ToList();
                var p2 = s.CharactersP2.ToList();
                context.Engine.State.SetSlot(i, EffectsS3S5.CloneCard(s.Card, effectIds: filtered), s.PlacedOnTurn, s.PlacedBy);
                foreach (var c in p1) context.Engine.State.AddCharacterToSlot(i, PlayerId.Player1, c);
                foreach (var c in p2) context.Engine.State.AddCharacterToSlot(i, PlayerId.Player2, c);
            }
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class ExecuteHandCharEffect : IEffect
{
    public string EffectId { get; }
    public ExecuteHandCharEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = context.Engine.State.GetHand(context.Player)
            .Where(c => c.Kind == CardKind.Character && !c.IsScoreur && c.EffectIds.Any(e => !e.StartsWith("passive_", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (candidates.Count == 0) return DuelActionResult.Ok();
        EffectsS3S5.SelectCandidateOrAuto(context, "Copier effet depuis la main", "Choisissez un personnage dont l'effet sera exécuté", candidates, chosen =>
        {
            foreach (var effId in chosen.EffectIds)
            {
                if (effId.StartsWith("passive_", StringComparison.OrdinalIgnoreCase)) continue;
                if (context.Engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
                    eff.Execute(new EffectContext(context.Engine, context.Player, chosen, context.SlotIndex));
            }
        });
        return DuelActionResult.Ok();
    }
}

internal sealed class ExtraPlayThisTurnEffect : IEffect
{
    public string EffectId { get; }
    private readonly int _selfMillFirst;
    public ExtraPlayThisTurnEffect(string effectId, int selfMillFirst) { EffectId = effectId; _selfMillFirst = selfMillFirst; }
    public DuelActionResult Execute(EffectContext context)
    {
        if (_selfMillFirst > 0)
        {
            context.Engine.MillCards(context.Player, _selfMillFirst);
            if (context.Engine.State.IsGameOver) return DuelActionResult.Ok();
        }
        // Décrémente CharactersPlayedThisTurn de 1 (si > 0) pour offrir une pose supplémentaire.
        var prop = typeof(DuelState).GetProperty("CharactersPlayedThisTurn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && context.Engine.State.CharactersPlayedThisTurn > 0)
            prop.SetValue(context.Engine.State, context.Engine.State.CharactersPlayedThisTurn - 1);
        return DuelActionResult.Ok();
    }
}

internal sealed class BothRevealStealOneEffect : IEffect
{
    public string EffectId { get; }
    public BothRevealStealOneEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var h1 = context.Engine.State.GetHand(PlayerId.Player1);
        var h2 = context.Engine.State.GetHand(PlayerId.Player2);
        if (h1.Count > 0) context.Engine.Emit(new CardsRevealedEvent(PlayerId.Player1, h1));
        if (h2.Count > 0) context.Engine.Emit(new CardsRevealedEvent(PlayerId.Player2, h2));
        if (h1.Count > 0 && h2.Count > 0)
        {
            var c1 = h2.OrderByDescending(c => c.Force).First();
            var c2 = h1.OrderByDescending(c => c.Force).First();
            if (context.Engine.State.RemoveCardFromHand(PlayerId.Player2, c1)) context.Engine.State.AddCardToHand(PlayerId.Player1, c1);
            if (context.Engine.State.RemoveCardFromHand(PlayerId.Player1, c2)) context.Engine.State.AddCardToHand(PlayerId.Player2, c2);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class RevealTwoDiscardOppEffect : IEffect
{
    public string EffectId { get; }
    private readonly bool _highestWins;
    public RevealTwoDiscardOppEffect(string effectId, bool highestWins) { EffectId = effectId; _highestWins = highestWins; }
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var myHand = context.Engine.State.GetHand(context.Player).OrderByDescending(c => c.Force).Take(2).ToList();
        var oppHand = context.Engine.State.GetHand(opp).OrderByDescending(c => c.Force).Take(2).ToList();
        if (myHand.Count == 0 || oppHand.Count == 0) return DuelActionResult.Ok();
        int mySum = myHand.Sum(c => c.Force);
        int oppSum = oppHand.Sum(c => c.Force);
        bool iWin = _highestWins ? mySum > oppSum : mySum < oppSum;
        var loser = iWin ? opp : context.Player;
        var loserPick = (iWin ? oppHand : myHand)[0];
        if (context.Engine.State.RemoveCardFromHand(loser, loserPick))
        {
            context.Engine.State.AddCardToDiscard(loser, loserPick);
            context.Engine.Emit(new CardsDiscardedEvent(loser, new[] { loserPick }));
            context.Engine.CheckDeckOutCondition(loser);
        }
        return DuelActionResult.Ok();
    }
}

internal sealed class NameCardOppHandPlayFreeEffect : IEffect
{
    public string EffectId { get; }
    public NameCardOppHandPlayFreeEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var oppChars = context.Engine.State.GetHand(opp).Where(c => c.Kind == CardKind.Character).ToList();
        if (oppChars.Count > 0 && context.Engine.State.RemoveCardFromHand(opp, oppChars[0]))
            EffectsS3S5.PlaceCharacterFreeOnBoard(context.Engine, context.Player, oppChars[0], context.SlotIndex);
        return DuelActionResult.Ok();
    }
}

internal sealed class NameForceOppDiscardMatchingEffect : IEffect
{
    public string EffectId { get; }
    public NameForceOppDiscardMatchingEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        var oppChars = context.Engine.State.GetHand(opp).Where(c => c.Kind == CardKind.Character).ToList();
        if (oppChars.Count == 0)
        {
            var myHand = context.Engine.State.GetHand(context.Player).ToList();
            context.Engine.DiscardFromHand(context.Player, myHand.Count);
            return DuelActionResult.Ok();
        }
        int targetForce = oppChars.GroupBy(c => c.Force).OrderByDescending(g => g.Count()).First().Key;
        var matches = oppChars.Where(c => c.Force == targetForce).ToList();
        foreach (var m in matches)
        {
            if (context.Engine.State.RemoveCardFromHand(opp, m)) context.Engine.State.AddCardToDiscard(opp, m);
        }
        context.Engine.Emit(new CardsDiscardedEvent(opp, matches));
        context.Engine.CheckDeckOutCondition(opp);
        return DuelActionResult.Ok();
    }
}

internal sealed class OppChooseTakeOrDiscardCharsEffect : IEffect
{
    public string EffectId { get; }
    public OppChooseTakeOrDiscardCharsEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var opp = context.Player.Opponent();
        new TakeTopDiscardEffect(EffectId, 1).Execute(new EffectContext(context.Engine, opp, context.SourceCard, context.SlotIndex));
        new TakeTopDiscardEffect(EffectId, 2).Execute(context);
        return DuelActionResult.Ok();
    }
}

internal sealed class OppGuessParityDiscardOppCharEffect : IEffect
{
    public string EffectId { get; }
    public OppGuessParityDiscardOppCharEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var drawn = context.Engine.DrawCards(context.Player, 1);
        if (drawn.Count > 0 && !context.Engine.State.IsGameOver)
        {
            int guessedParity = S3S5Bridge.RollNext(context.Engine, 0, 2);
            if (S3S5Bridge.GetCardCost(drawn[0]) % 2 != guessedParity)
                new DiscardCharacterByMaxForceEffect(EffectId, int.MaxValue).Execute(context);
        }
        return DuelActionResult.Ok();
    }
}
