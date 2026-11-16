#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// Représente un slot d'emplacement de terrain sur le plateau partagé.
/// Ne stocke pas de booléen IsActive : l'activation est calculée dynamiquement par rapport au tour global.
/// Conserve le joueur ayant posé le terrain pour renvoyer la carte dans sa défausse lors du nettoyage.
/// </summary>
public sealed class TerrainSlot
{
    public int SlotIndex { get; }
    public DuelCard? Card { get; }
    public int? PlacedOnTurn { get; }
    public PlayerId? PlacedBy { get; }

    private readonly List<DuelCard> _charactersP1;
    private readonly List<DuelCard> _charactersP2;

    public IReadOnlyList<DuelCard> CharactersP1 => _charactersP1;
    public IReadOnlyList<DuelCard> CharactersP2 => _charactersP2;

    public bool IsEmpty => Card == null;

    public TerrainSlot(
        int slotIndex,
        DuelCard? card = null,
        int? placedOnTurn = null,
        PlayerId? placedBy = null,
        IEnumerable<DuelCard>? charactersP1 = null,
        IEnumerable<DuelCard>? charactersP2 = null)
    {
        SlotIndex = slotIndex;
        Card = card;
        PlacedOnTurn = placedOnTurn;
        PlacedBy = placedBy;
        _charactersP1 = charactersP1 != null ? new List<DuelCard>(charactersP1) : new List<DuelCard>();
        _charactersP2 = charactersP2 != null ? new List<DuelCard>(charactersP2) : new List<DuelCard>();
    }

    public bool IsActive(int currentTurn) =>
        Card != null && PlacedOnTurn.HasValue && PlacedOnTurn.Value < currentTurn;

    public IReadOnlyList<DuelCard> GetCharacters(PlayerId player) =>
        player == PlayerId.Player1 ? CharactersP1 : CharactersP2;

    private static bool HasEffect(IReadOnlyList<string>? effects, string target) =>
        PassiveCardHelper.ListContains(effects, target);

    private static bool HasCardPassiveOrEffect(DuelCard? card, string target) =>
        PassiveCardHelper.RawCardHasEffect(card, target);

    private bool HasCharacterEffect(DuelState? state, PlayerId owner, DuelCard card, string target, bool comboActive) =>
        PassiveCardHelper.CardHasPassive(state, this, owner, card, target, comboActive);

    private static bool TryParseBoostSelf(string eff, out int boostVal)
    {
        const string Prefix = "boost_self_";
        const string PassivePrefix = "passive_boost_self_";
        boostVal = 0;
        if (eff == null) return false;

        int startIdx;
        if (eff.Length > Prefix.Length && eff.StartsWith(Prefix, StringComparison.Ordinal))
        {
            startIdx = Prefix.Length;
        }
        else if (eff.Length > PassivePrefix.Length && eff.StartsWith(PassivePrefix, StringComparison.Ordinal))
        {
            startIdx = PassivePrefix.Length;
        }
        else
        {
            return false;
        }

        int result = 0;
        for (int i = startIdx; i < eff.Length; i++)
        {
            char ch = eff[i];
            if (ch < '0' || ch > '9') return false;
            result = (result * 10) + (ch - '0');
        }
        boostVal = result;
        return true;
    }

    private bool HasAnySlotEffect(PlayerId player, string target, bool includeOpponentChars = false, DuelState? state = null)
    {
        if (HasCardPassiveOrEffect(Card, target)) return true;
        var ownChars = GetCharacters(player);
        for (int i = 0; i < ownChars.Count; i++)
        {
            if (PassiveCardHelper.CardHasPassive(state, this, player, ownChars[i], target)) return true;
        }
        if (includeOpponentChars)
        {
            var opp = player.Opponent();
            var oppChars = GetCharacters(opp);
            for (int i = 0; i < oppChars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, this, opp, oppChars[i], target)) return true;
            }
        }
        return false;
    }

    public GemMask GetEffectiveGems(PlayerId player, DuelCard? card) =>
        GetEffectiveGems(player, card, null);

    internal GemMask GetEffectiveGems(PlayerId player, DuelCard? card, DuelState? state)
    {
        if (card == null || card.Kind != CardKind.Character)
        {
            return GemMask.None;
        }

        bool isLaink = PassiveCardHelper.IsEffigy(card, "Laink");
        bool isTerracid = PassiveCardHelper.IsEffigy(card, "Terracid");

        if (isLaink && HasAnySlotEffect(player, "passive_terrain_remove_laink_gems", includeOpponentChars: true, state))
        {
            return GemMask.None;
        }
        if (isTerracid && HasAnySlotEffect(player, "passive_terrain_remove_terracid_gems", includeOpponentChars: true, state))
        {
            return GemMask.None;
        }

        GemMask mask = card.Gems;
        bool grantOrange = HasAnySlotEffect(player, "passive_terrain_grant_orange_gems", state: state) ||
                           (state != null && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_grant_orange_gems", player));
        if (grantOrange)
        {
            mask |= GemMask.OpeningOrange | GemMask.ClosingOrange;
        }

        bool grantViolet = HasAnySlotEffect(player, "passive_terrain_grant_violet_gems", state: state) ||
                           (state != null && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_grant_violet_gems", player));
        if (!grantViolet && HasCharacterEffect(state, player, card, "passive_gain_violet_gems_if_dog", comboActive: false))
        {
            if (state != null)
            {
                if (PassiveCardHelper.CountCharsInPlay(state, null, "Chien") > 0) grantViolet = true;
            }
            else
            {
                for (int i = 0; i < CharactersP1.Count; i++)
                    if (PassiveCardHelper.CardMatchesName(null, PlayerId.Player1, CharactersP1[i], "Chien", this)) grantViolet = true;
                for (int i = 0; i < CharactersP2.Count; i++)
                    if (PassiveCardHelper.CardMatchesName(null, PlayerId.Player2, CharactersP2[i], "Chien", this)) grantViolet = true;
            }
        }
        if (grantViolet)
        {
            mask |= GemMask.OpeningViolet | GemMask.ClosingViolet;
        }

        return mask;
    }

    public bool CanConnectOnSlot(PlayerId player, DuelCard? leftCard, DuelCard? rightCard) =>
        CanConnectOnSlot(player, leftCard, rightCard, null);

    internal bool CanConnectOnSlot(PlayerId player, DuelCard? leftCard, DuelCard? rightCard, DuelState? state)
    {
        if (leftCard == null || rightCard == null) return false;

        if (HasCardPassiveOrEffect(Card, "passive_combos_same_name_only"))
        {
            string n1 = PassiveCardHelper.GetEffectiveName(state, player, leftCard, this);
            string n2 = PassiveCardHelper.GetEffectiveName(state, player, rightCard, this);
            if (string.IsNullOrEmpty(n1) || !string.Equals(n1, n2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        GemMask leftGems = GetEffectiveGems(player, leftCard, state);
        GemMask rightGems = GetEffectiveGems(player, rightCard, state);

        bool leftOpenOrange = (leftGems & GemMask.OpeningOrange) != 0;
        bool leftOpenViolet = (leftGems & GemMask.OpeningViolet) != 0;
        bool rightCloseOrange = (rightGems & GemMask.ClosingOrange) != 0;
        bool rightCloseViolet = (rightGems & GemMask.ClosingViolet) != 0;

        if (HasAnySlotEffect(player, "passive_terrain_daltonisme", includeOpponentChars: true, state))
        {
            return (leftOpenOrange && rightCloseViolet) || (leftOpenViolet && rightCloseOrange);
        }

        return (leftOpenOrange && rightCloseOrange) || (leftOpenViolet && rightCloseViolet);
    }

    public bool IsComboActiveOnSlot(PlayerId player, int characterIndex) =>
        IsComboActiveOnSlot(player, characterIndex, null);

    internal bool IsComboActiveOnSlot(PlayerId player, int characterIndex, DuelState? state)
    {
        var chars = GetCharacters(player);
        if (chars == null || characterIndex <= 0 || characterIndex >= chars.Count)
        {
            return false;
        }
        return CanConnectOnSlot(player, chars[characterIndex - 1], chars[characterIndex], state);
    }

    public int GetGemChainCount(PlayerId player) => GetGemChainCount(player, null);

    internal int GetGemChainCount(PlayerId player, DuelState? state)
    {
        var chars = GetCharacters(player);
        int count = chars.Count;
        if (count < 2) return 0;

        int connected = 0;
        for (int i = 0; i < count; i++)
        {
            bool leftConn = i > 0 && IsComboActiveOnSlot(player, i, state);
            bool rightConn = i + 1 < count && IsComboActiveOnSlot(player, i + 1, state);
            if (leftConn || rightConn) connected++;
        }
        return connected;
    }

    public int GetEffectiveAutoScoreThreshold(PlayerId player, int baseThreshold) =>
        GetEffectiveAutoScoreThreshold(player, baseThreshold, null);

    public int GetEffectiveAutoScoreThreshold(PlayerId player, int baseThreshold, DuelState? state)
    {
        int threshold = baseThreshold;
        if (HasCardPassiveOrEffect(Card, "passive_autoscore_threshold_9")) threshold = 9;
        else if (HasCardPassiveOrEffect(Card, "passive_autoscore_threshold_13")) threshold = 13;
        else if (HasCardPassiveOrEffect(Card, "passive_autoscore_threshold_16")) threshold = 16;
        else if (HasCardPassiveOrEffect(Card, "passive_autoscore_threshold_18")) threshold = 18;

        bool oppPlus4 = false;
        if (PlacedBy.HasValue && PlacedBy.Value != player && HasCardPassiveOrEffect(Card, "passive_opp_autoscore_plus_4"))
        {
            oppPlus4 = true;
        }
        else
        {
            var oppChars = GetCharacters(player.Opponent());
            for (int i = 0; i < oppChars.Count; i++)
            {
                if (HasCardPassiveOrEffect(oppChars[i], "passive_opp_autoscore_plus_4"))
                {
                    oppPlus4 = true;
                    break;
                }
            }
        }
        if (oppPlus4) threshold += 4;

        if (state != null)
        {
            if (HasCardPassiveOrEffect(Card, "passive_autoscore_plus_1_per_10_discard"))
            {
                threshold += state.GetDiscard(player).Count / 10;
            }
            if (HasAnySlotEffect(player, "passive_autoscore_plus_1_per_won_terrain", includeOpponentChars: true, state))
            {
                threshold += state.GetScore(player);
            }
        }

        return threshold;
    }

    public int GetForce(PlayerId player) => GetForce(player, null, forScoring: false, includeAdjacentTransfers: true);

    public int GetForce(PlayerId player, DuelState? state) => GetForce(player, state, forScoring: false, includeAdjacentTransfers: true);

    internal int GetForce(PlayerId player, DuelState? state, bool forScoring, bool includeAdjacentTransfers = true)
    {
        int total = 0;
        var chars = GetCharacters(player);
        for (int i = 0; i < chars.Count; i++)
        {
            total += GetCharacterEffectiveForce(player, i, state, forScoring, recursionDepth: 0);
        }

        if (includeAdjacentTransfers && state != null)
        {
            if (SlotIndex + 1 < state.Slots.Count)
            {
                var rightSlot = state.Slots[SlotIndex + 1];
                var rightChars = rightSlot.GetCharacters(player);
                for (int i = 0; i < rightChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, rightSlot, player, rightChars[i], "passive_add_force_to_left_terrain"))
                    {
                        total += rightSlot.GetCharacterEffectiveForce(player, i, state, forScoring, recursionDepth: 1);
                    }
                }
            }
            if (SlotIndex - 1 >= 0)
            {
                var leftSlot = state.Slots[SlotIndex - 1];
                var leftChars = leftSlot.GetCharacters(player);
                for (int i = 0; i < leftChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, leftSlot, player, leftChars[i], "passive_add_force_to_right_terrain"))
                    {
                        total += leftSlot.GetCharacterEffectiveForce(player, i, state, forScoring, recursionDepth: 1);
                    }
                }
            }
        }

        return total;
    }

    internal int GetCharacterEffectiveForce(PlayerId player, int charIndex, DuelState? state, bool forScoring, int recursionDepth = 0)
    {
        var chars = GetCharacters(player);
        if (charIndex < 0 || charIndex >= chars.Count) return 0;

        var c = chars[charIndex];
        if (recursionDepth > 2) return Math.Max(0, c.Force);

        PassiveCardHelper.TryGetState(state, out var pState);
        bool comboActiveRight = IsComboActiveOnSlot(player, charIndex, state);
        bool comboActiveLeft = (charIndex + 1 < chars.Count) && IsComboActiveOnSlot(player, charIndex + 1, state);
        bool suppressed = PassiveCardHelper.AreCardPassivesSuppressed(state, this, player, c, comboActiveRight);

        if (forScoring)
        {
            if (pState != null && pState.ForceZeroThisScoreCardIds.Contains(c.Id)) return 0;
            if (!suppressed)
            {
                if (HasCharacterEffect(state, player, c, "passive_force_not_counted_in_scores", comboActiveRight) ||
                    HasCharacterEffect(state, player, c, "passive_force_not_counted_in_score", comboActiveRight) ||
                    HasCharacterEffect(state, player, c, "passive_force_not_counted_on_score", comboActiveRight))
                {
                    return 0;
                }
                if (state != null && state.ActivePlayer == player &&
                    HasCharacterEffect(state, player, c, "passive_ignore_force_on_own_turn", comboActiveRight))
                {
                    return 0;
                }
            }
        }

        int baseForce = c.Force;
        var boss = PassiveCardHelper.FindBossDesSaviors(state, this, player, c);
        if (boss != null)
        {
            baseForce = boss.Force;
        }
        else if (!suppressed)
        {
            if (pState != null && state != null &&
                pState.PlayedOnTurnByCardId.TryGetValue(c.Id, out int playedTurn) && playedTurn == state.TurnNumber)
            {
                if (HasCharacterEffect(state, player, c, "passive_force_5_on_play_turn", comboActiveRight)) baseForce = 5;
                else if (HasCharacterEffect(state, player, c, "passive_force_6_on_play_turn", comboActiveRight)) baseForce = 6;
            }

            if (HasCharacterEffect(state, player, c, "passive_copy_left_force", comboActiveRight))
            {
                baseForce = charIndex > 0
                    ? GetCharacterEffectiveForce(player, charIndex - 1, state, forScoring, recursionDepth + 1)
                    : 0;
            }
            else if (HasCharacterEffect(state, player, c, "passive_copy_right_force", comboActiveRight))
            {
                baseForce = charIndex + 1 < chars.Count
                    ? GetCharacterEffectiveForce(player, charIndex + 1, state, forScoring, recursionDepth + 1)
                    : 0;
            }
        }

        int delta = 0;
        var opp = player.Opponent();
        var oppChars = GetCharacters(opp);
        bool isLaink = PassiveCardHelper.IsEffigy(c, "Laink");
        bool isTerracid = PassiveCardHelper.IsEffigy(c, "Terracid");

        // Temp force from turn/card effects
        if (pState != null && pState.TempForceByCardId.TryGetValue(c.Id, out int tempDelta))
        {
            delta += tempDelta;
        }

        // Slot terrain & board-wide auras
        if (isLaink)
        {
            if (HasCardPassiveOrEffect(Card, "boost_laink_1") || HasCardPassiveOrEffect(Card, "passive_boost_laink_1")) delta += 1;
            if (HasCardPassiveOrEffect(Card, "boost_laink_20")) delta += 20;
            delta += CountSlotPassives(state, player, chars, "passive_boost_laink_1") + CountSlotPassives(state, player, chars, "boost_laink_1");
        }
        if (isTerracid)
        {
            if (HasCardPassiveOrEffect(Card, "boost_terracid_1") || HasCardPassiveOrEffect(Card, "passive_boost_terracid_1")) delta += 1;
            if (HasCardPassiveOrEffect(Card, "boost_terracid_20")) delta += 20;
            delta += CountSlotPassives(state, player, chars, "passive_boost_terracid_1") + CountSlotPassives(state, player, chars, "boost_terracid_1");
        }
        if (isLaink || isTerracid)
        {
            if (HasCardPassiveOrEffect(Card, "boost_laink_terracid_3") || HasCardPassiveOrEffect(Card, "passive_boost_laink_terracid_3")) delta += 3;
            if (HasCardPassiveOrEffect(Card, "boost_laink_terracid_30")) delta += 30;
            delta += 3 * (CountSlotPassives(state, player, chars, "passive_boost_laink_terracid_3") + CountSlotPassives(state, player, chars, "boost_laink_terracid_3"));
        }

        if (comboActiveRight || comboActiveLeft)
        {
            if (HasCardPassiveOrEffect(Card, "terrain_combo_boost_1")) delta += 1;
            if (HasCardPassiveOrEffect(Card, "terrain_combo_boost_10")) delta += 10;
        }

        if (HasCardPassiveOrEffect(Card, "passive_terrain_boost_all_1")) delta += 1;
        if (HasCardPassiveOrEffect(Card, "passive_all_chars_minus_1")) delta -= 1;

        if (HasAnySlotEffect(player, "passive_opp_chars_minus_1_per_line_distance", includeOpponentChars: false, state) == false &&
            HasOppSlotPassive(state, opp, oppChars, "passive_opp_chars_minus_1_per_line_distance"))
        {
            delta -= charIndex;
        }

        // Positional neighbor buffs on same slot
        if (charIndex > 0 && PassiveCardHelper.CardHasPassive(state, this, player, chars[charIndex - 1], "passive_boost_3_to_char_below"))
            delta += 3;
        if (charIndex + 1 < chars.Count && PassiveCardHelper.CardHasPassive(state, this, player, chars[charIndex + 1], "passive_boost_3_to_char_above"))
            delta += 3;

        if (state != null)
        {
            int ownHand = state.GetHand(player).Count;
            int oppHand = state.GetHand(opp).Count;
            int ownDisc = state.GetDiscard(player).Count;

            delta += PassiveCardHelper.CountBoardPassives(state, "passive_unique_trophy", player);
            delta -= 2 * PassiveCardHelper.CountBoardPassives(state, "passive_all_own_chars_minus_2", player);
            delta -= PassiveCardHelper.CountBoardPassives(state, "passive_opp_chars_minus_1", opp);

            if (pState != null && pState.ScoresResolvedThisTurn > 1 &&
                PassiveCardHelper.BoardHasCardWithPassive(state, "passive_boost_all_1_if_multi_score_turn", player))
            {
                delta += 1;
            }

            if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_most_hand_boost_1") && ownHand > oppHand)
            {
                delta += 1;
            }

            if (ownDisc >= 10 && HasCardPassiveOrEffect(Card, "passive_boost_2_if_discard_10_plus"))
            {
                delta += 2;
            }

            if (PassiveCardHelper.IsBoysBandEligible(state, player, c, this))
            {
                delta += PassiveCardHelper.CountBoardPassives(state, "passive_boost_boys_band_1", player);
            }
            if (PassiveCardHelper.HasCharDiscardEffect(c))
            {
                delta += PassiveCardHelper.CountBoardPassives(state, "passive_boost_char_discard_effect_1", player);
            }
            int effCount = (c.EffectIds?.Count ?? 0) + (c.ComboEffectIds?.Count ?? 0);
            int totalEffCount = effCount + (c.PassiveEffectIds?.Count ?? 0);
            if (effCount <= 1) delta += 2 * PassiveCardHelper.CountBoardPassives(state, "passive_boost_text_1_line_2", player);
            if (totalEffCount >= 3) delta += PassiveCardHelper.CountBoardPassives(state, "passive_boost_text_3_lines_1", player);
            if (c.Force == 0) delta += 2 * PassiveCardHelper.CountBoardPassives(state, "passive_boost_2_to_printed_force_0_chars", player);

            // duo_jakov_richus
            if (state.ActivePlayer == opp && state.GetDeck(opp).Count < 25 &&
                PassiveCardHelper.BoardHasCardWithPassive(state, "duo_jakov_richus", player))
            {
                delta += 4;
            }
            if (state.ActivePlayer == opp && state.GetDeck(opp).Count < 25 &&
                PassiveCardHelper.BoardHasCardWithPassive(state, "duo_jakov_richus", opp))
            {
                delta -= 1;
            }
        }

        // Self-specific additive boosts (skipped if suppressed)
        if (!suppressed)
        {
            delta += ComputeSelfAdditiveBoosts(player, charIndex, chars, oppChars, state, forScoring, recursionDepth, comboActiveRight);

            // passive_double_force_bonuses (#300062 PRISONNIER)
            if (delta > 0 && HasCharacterEffect(state, player, c, "passive_double_force_bonuses", comboActiveRight))
            {
                delta += delta;
            }
        }

        int charForce = baseForce + delta;

        // Multipliers
        if (!suppressed)
        {
            if (isLaink && HasAnyBoardOrSlot(state, player, "passive_double_force_laink")) charForce *= 2;
            if (isTerracid && HasAnyBoardOrSlot(state, player, "passive_double_force_terracid")) charForce *= 2;
            if (CountSlotPassives(state, player, chars, "passive_double_force_own_on_slot") > 0) charForce *= 2;

            if (!isLaink && !isTerracid && state != null &&
                HasOtherOnBoardWithPassive(state, player, c, "passive_triple_force_non_laink_terracid"))
            {
                charForce *= 3;
            }

            if (HasCharacterEffect(state, player, c, "passive_double_force_if_carlito", comboActiveRight) &&
                HasNamedInPlay(state, player, "Carlito"))
                charForce *= 2;
            if (HasCharacterEffect(state, player, c, "passive_double_force_if_mcfly", comboActiveRight) &&
                HasNamedInPlay(state, player, "McFly"))
                charForce *= 2;
            if (HasCharacterEffect(state, player, c, "passive_double_force_if_horty", comboActiveRight) &&
                HasNamedInPlay(state, player, "Horty"))
                charForce *= 2;
            if (oppChars.Count == 1 && HasCharacterEffect(state, player, c, "passive_double_force_if_single_opp_char", comboActiveRight))
                charForce *= 2;

            if (state != null && HasCharacterEffect(state, player, c, "passive_double_force_per_copy", comboActiveRight))
            {
                int copies = PassiveCardHelper.CountCharsInPlay(state, player, c.Name);
                for (int k = 1; k < copies; k++) charForce *= 2;
            }
        }

        if (oppChars.Count > 0 && CountSlotPassives(state, opp, oppChars, "passive_halve_force_opp_on_slot") > 0)
        {
            charForce = Math.Max(0, charForce) / 2;
        }

        if (pState != null && pState.DoubledThisTurnCardIds.Contains(c.Id))
        {
            charForce *= 2;
        }

        if (charForce < 0) charForce = 0;

        if (HasCardPassiveOrEffect(Card, "passive_terrain_invert_force"))
        {
            charForce = -charForce;
        }

        return charForce;
    }

    private int ComputeSelfAdditiveBoosts(
        PlayerId player,
        int charIndex,
        IReadOnlyList<DuelCard> chars,
        IReadOnlyList<DuelCard> oppChars,
        DuelState? state,
        bool forScoring,
        int recursionDepth,
        bool comboActiveRight)
    {
        var c = chars[charIndex];
        int bonus = 0;

        void ScanBoostSelf(IReadOnlyList<string>? ids)
        {
            if (ids == null) return;
            for (int j = 0; j < ids.Count; j++)
            {
                if (TryParseBoostSelf(ids[j], out int val)) bonus += val;
            }
        }

        ScanBoostSelf(c.EffectIds);
        ScanBoostSelf(c.PassiveEffectIds);
        if (comboActiveRight) ScanBoostSelf(c.ComboEffectIds);

        if (chars.Count == 1 && HasCharacterEffect(state, player, c, "passive_boost_self_5_if_alone", comboActiveRight))
            bonus += 5;

        if (charIndex > 0 && charIndex < chars.Count - 1 &&
            HasCharacterEffect(state, player, c, "passive_boost_2_if_between_chars", comboActiveRight))
            bonus += 2;

        if (charIndex == chars.Count - 1 && chars.Count >= 3)
        {
            if (HasCharacterEffect(state, player, c, "passive_boost_3_if_rightmost_and_3_chars", comboActiveRight)) bonus += 3;
            if (HasCharacterEffect(state, player, c, "passive_boost_4_if_rightmost_and_3_chars", comboActiveRight)) bonus += 4;
        }

        if (HasCharacterEffect(state, player, c, "passive_force_per_gem_chain_1", comboActiveRight))
            bonus += GetGemChainCount(player, state);

        if (HasCharacterEffect(state, player, c, "passive_boost_2_per_a_modeler_on_slot", comboActiveRight))
        {
            for (int i = 0; i < chars.Count; i++)
            {
                if (i != charIndex && PassiveCardHelper.CardMatchesName(state, player, chars[i], "à Modeler", this))
                    bonus += 2;
            }
        }

        if (HasCharacterEffect(state, player, c, "passive_boost_equal_char_above", comboActiveRight) && charIndex + 1 < chars.Count)
            bonus += Math.Max(0, GetCharacterEffectiveForce(player, charIndex + 1, state, forScoring, recursionDepth + 1));

        if (HasCharacterEffect(state, player, c, "passive_boost_equal_char_below", comboActiveRight) && charIndex > 0)
            bonus += Math.Max(0, GetCharacterEffectiveForce(player, charIndex - 1, state, forScoring, recursionDepth + 1));

        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_opp_laink_on_slot", comboActiveRight))
        {
            for (int i = 0; i < oppChars.Count; i++)
                if (PassiveCardHelper.IsEffigy(oppChars[i], "Laink")) bonus += 1;
        }
        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_opp_terracid_on_slot", comboActiveRight))
        {
            for (int i = 0; i < oppChars.Count; i++)
                if (PassiveCardHelper.IsEffigy(oppChars[i], "Terracid")) bonus += 1;
        }

        if (HasCharacterEffect(state, player, c, "passive_boost_2_per_opp_row", comboActiveRight))
            bonus += 2 * oppChars.Count;

        if (state == null) return bonus;

        var opp = player.Opponent();
        int ownHandCount = state.GetHand(player).Count;
        int oppHandCount = state.GetHand(opp).Count;
        var ownDiscard = state.GetDiscard(player);

        if (HasCharacterEffect(state, player, c, "passive_force_per_hand_card_1", comboActiveRight)) bonus += ownHandCount;
        if (HasCharacterEffect(state, player, c, "passive_force_per_hand_card_minus_1", comboActiveRight)) bonus -= ownHandCount;
        if (HasCharacterEffect(state, player, c, "passive_force_per_opp_hand_card_minus_1", comboActiveRight)) bonus -= oppHandCount;
        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_opp_hand_card", comboActiveRight)) bonus += oppHandCount;
        if (HasCharacterEffect(state, player, c, "passive_force_per_discard_card_1", comboActiveRight)) bonus += ownDiscard.Count;
        if (HasCharacterEffect(state, player, c, "passive_force_per_5_discard_1", comboActiveRight)) bonus += ownDiscard.Count / 5;
        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_3_opp_banished", comboActiveRight)) bonus += state.GetBanish(opp).Count / 3;
        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_won_terrain", comboActiveRight)) bonus += state.GetScore(player);

        if (HasCharacterEffect(state, player, c, "passive_boost_2_if_leftmost_slot", comboActiveRight))
        {
            int leftmost = -1;
            for (int s = 0; s < state.Slots.Count; s++)
            {
                if (!state.Slots[s].IsEmpty) { leftmost = s; break; }
            }
            if (leftmost == SlotIndex) bonus += 2;
        }

        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_wcdo", comboActiveRight))
            bonus += PassiveCardHelper.CountCharsInPlay(state, player, "WcDo");

        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_enchaines", comboActiveRight))
            bonus += PassiveCardHelper.CountCharsInPlay(state, player, "Encha");

        if (HasCharacterEffect(state, player, c, "passive_boost_per_surfeur_1", comboActiveRight))
            bonus += PassiveCardHelper.CountCharsInPlay(state, null, "Surfeur");

        if (HasCharacterEffect(state, player, c, "passive_boost_per_sexy_or_prostituee_1", comboActiveRight))
            bonus += PassiveCardHelper.CountCharsInPlay(state, null, "Sexy") + PassiveCardHelper.CountCharsInPlay(state, null, "Prostitu");

        if (HasCharacterEffect(state, player, c, "passive_boost_soldat_espace_terracid_2", comboActiveRight) &&
            PassiveCardHelper.CountCharsInPlay(state, player, "Soldat de l'espace", "Terracid") > 0)
            bonus += 2;

        if (HasCharacterEffect(state, player, c, "passive_boost_if_pig_2", comboActiveRight) &&
            (PassiveCardHelper.CountCharsInPlay(state, null, "Cochon") > 0 || PassiveCardHelper.CountCharsInPlay(state, null, "Porc") > 0))
            bonus += 2;

        if (HasCharacterEffect(state, player, c, "passive_boost_4_if_only_guest", comboActiveRight) &&
            PassiveCardHelper.CountCharsInPlay(state, player, null, "Guest") == 1)
            bonus += 4;

        if (HasCharacterEffect(state, player, c, "passive_with_titanic_boost_3", comboActiveRight) &&
            PassiveCardHelper.CountCharsInPlay(state, player, "Titanic") > 0)
            bonus += 3;

        if (HasCharacterEffect(state, player, c, "passive_boost_1_per_unique_own_char_name", comboActiveRight))
        {
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var lst = state.Slots[s].GetCharacters(player);
                for (int k = 0; k < lst.Count; k++)
                {
                    var n = PassiveCardHelper.GetEffectiveName(state, player, lst[k], state.Slots[s]);
                    if (!string.IsNullOrEmpty(n)) unique.Add(n);
                }
            }
            bonus += unique.Count;
        }

        if (HasCharacterEffect(state, player, c, "passive_force_per_completed_combo_1", comboActiveRight))
            bonus += CountCompletedCombos(state, player);

        if (HasCharacterEffect(state, player, c, "passive_force_per_opp_completed_combo_1", comboActiveRight))
            bonus += CountCompletedCombos(state, opp);

        if (HasCharacterEffect(state, player, c, "passive_boost_per_brique_pair_discard_1", comboActiveRight))
        {
            var briqueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int k = 0; k < ownDiscard.Count; k++)
            {
                if (ownDiscard[k].Name.IndexOf("Brique", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    briqueCounts[ownDiscard[k].Name] = (briqueCounts.TryGetValue(ownDiscard[k].Name, out int cnt) ? cnt : 0) + 1;
                }
            }
            foreach (var kv in briqueCounts) bonus += kv.Value / 2;
        }

        if (HasCharacterEffect(state, player, c, "passive_boost_2_per_robot_in_discard", comboActiveRight))
        {
            for (int k = 0; k < ownDiscard.Count; k++)
                if (PassiveCardHelper.CardMatchesName(state, player, ownDiscard[k], "Robot")) bonus += 2;
        }

        if (HasCharacterEffect(state, player, c, "passive_add_jakov_force_if_in_discard", comboActiveRight))
            bonus += MaxNamedForceInDiscard(ownDiscard, "Jakov");

        if (HasCharacterEffect(state, player, c, "passive_add_richus_force_if_in_discard", comboActiveRight))
            bonus += MaxNamedForceInDiscard(ownDiscard, "Richus");

        if (HasCharacterEffect(state, player, c, "passive_add_top_discard_force", comboActiveRight) && ownDiscard.Count > 0)
            bonus += ownDiscard[ownDiscard.Count - 1].Force;

        if (HasCharacterEffect(state, player, c, "passive_boost_2_per_facedown_in_play", comboActiveRight))
            bonus += 2 * PassiveCardHelper.GetTotalFacedownCount(state);

        int myFacedown = PassiveCardHelper.GetTotalFacedownCount(state, c.Id);
        if (myFacedown == 0)
        {
            if (HasCharacterEffect(state, player, c, "passive_boost_4_if_no_facedown_under", comboActiveRight)) bonus += 4;
            if (HasCharacterEffect(state, player, c, "passive_boost_5_if_no_facedown_under", comboActiveRight)) bonus += 5;
        }

        if (HasCharacterEffect(state, player, c, "passive_minus_1_per_laink_discard_if_alone", comboActiveRight))
            bonus -= PassiveCardHelper.CountCharsInPlay(state, null, null, "Laink");
        if (HasCharacterEffect(state, player, c, "passive_minus_1_per_terracid_discard_if_alone", comboActiveRight))
            bonus -= PassiveCardHelper.CountCharsInPlay(state, null, null, "Terracid");

        if (recursionDepth == 0 && HasCharacterEffect(state, player, c, "passive_boost_4_if_slot_force_11", comboActiveRight))
        {
            int slotSumWithoutSelf = 0;
            for (int i = 0; i < chars.Count; i++)
            {
                if (i == charIndex) slotSumWithoutSelf += Math.Max(0, c.Force + bonus);
                else slotSumWithoutSelf += GetCharacterEffectiveForce(player, i, state, forScoring, recursionDepth + 1);
            }
            if (slotSumWithoutSelf >= 11) bonus += 4;
        }

        return bonus;
    }

    private static int MaxNamedForceInDiscard(IReadOnlyList<DuelCard> discard, string namePart)
    {
        for (int i = discard.Count - 1; i >= 0; i--)
        {
            if (discard[i].Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return discard[i].Force;
            }
        }
        return 0;
    }

    private static int CountCompletedCombos(DuelState state, PlayerId player)
    {
        int count = 0;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var sl = state.Slots[s];
            var chars = sl.GetCharacters(player);
            for (int i = 1; i < chars.Count; i++)
            {
                if (sl.IsComboActiveOnSlot(player, i, state)) count++;
            }
        }
        return count;
    }

    private static bool HasOppSlotPassive(DuelState? state, PlayerId opp, IReadOnlyList<DuelCard> oppChars, string target)
    {
        for (int i = 0; i < oppChars.Count; i++)
        {
            if (PassiveCardHelper.CardHasPassive(state, null, opp, oppChars[i], target)) return true;
        }
        return false;
    }

    private int CountSlotPassives(DuelState? state, PlayerId owner, IReadOnlyList<DuelCard> chars, string target)
    {
        int count = 0;
        for (int i = 0; i < chars.Count; i++)
        {
            if (PassiveCardHelper.CardHasPassive(state, this, owner, chars[i], target)) count++;
        }
        return count;
    }

    private bool HasAnyBoardOrSlot(DuelState? state, PlayerId owner, string target)
    {
        if (state != null) return PassiveCardHelper.BoardHasCardWithPassive(state, target, owner);
        var chars = GetCharacters(owner);
        for (int i = 0; i < chars.Count; i++)
        {
            if (PassiveCardHelper.CardHasPassive(null, this, owner, chars[i], target)) return true;
        }
        return false;
    }

    private static bool HasOtherOnBoardWithPassive(DuelState state, PlayerId owner, DuelCard target, string effectId)
    {
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var chars = state.Slots[s].GetCharacters(owner);
            for (int i = 0; i < chars.Count; i++)
            {
                if (!ReferenceEquals(chars[i], target) &&
                    PassiveCardHelper.CardHasPassive(state, state.Slots[s], owner, chars[i], effectId))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool HasNamedInPlay(DuelState? state, PlayerId owner, string namePart)
    {
        if (state != null) return PassiveCardHelper.CountCharsInPlay(state, owner, namePart) > 0;
        var chars = GetCharacters(owner);
        for (int i = 0; i < chars.Count; i++)
        {
            if (PassiveCardHelper.CardMatchesName(null, owner, chars[i], namePart, this)) return true;
        }
        return false;
    }

    internal void AddCharacter(PlayerId player, DuelCard card, int insertIndex = -1)
    {
        var list = player == PlayerId.Player1 ? _charactersP1 : _charactersP2;
        if (insertIndex >= 0 && insertIndex <= list.Count)
        {
            list.Insert(insertIndex, card);
        }
        else
        {
            list.Add(card);
        }
    }

    internal bool RemoveCharacter(PlayerId player, DuelCard card)
    {
        var list = player == PlayerId.Player1 ? _charactersP1 : _charactersP2;
        return list.Remove(card);
    }

    public override string ToString()
    {
        if (IsEmpty) return $"Slot {SlotIndex}: [Empty]";
        return $"Slot {SlotIndex}: {Card!.Name} (Placed on Turn {PlacedOnTurn} by {PlacedBy}) - P1 Force: {GetForce(PlayerId.Player1)}, P2 Force: {GetForce(PlayerId.Player2)}";
    }
}
