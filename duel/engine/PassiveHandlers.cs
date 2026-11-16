#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// État auxiliaire pur C# par DuelState pour le suivi des passifs de tour, temporaires,
/// de cartes face cachée, de noms modifiés et d'activations "une fois par tour".
/// </summary>
internal sealed class PassiveState
{
    public readonly Dictionary<string, int> PlayedOnTurnByCardId = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> TempForceByCardId = new(StringComparer.Ordinal);
    public readonly HashSet<string> ForceZeroThisScoreCardIds = new(StringComparer.Ordinal);
    public readonly HashSet<string> DoubledThisTurnCardIds = new(StringComparer.Ordinal);
    public readonly HashSet<string> NamelessCardIds = new(StringComparer.Ordinal);
    public readonly HashSet<string> BanishOnLeaveCardIds = new(StringComparer.Ordinal);
    public readonly Dictionary<int, int> LockedTerrainsUntilTurn = new();
    public readonly Dictionary<string, HashSet<int>> VisitedSlotsThisTurn = new(StringComparer.Ordinal);
    public readonly HashSet<string> UsedOncePerTurn = new(StringComparer.Ordinal);
    public readonly HashSet<int> FirstPlayFreeUsedSlots = new();
    public readonly Dictionary<int, List<DuelCard>> CharsPlayedOnSlotThisTurnP1 = new();
    public readonly Dictionary<int, List<DuelCard>> CharsPlayedOnSlotThisTurnP2 = new();
    public readonly HashSet<string> NamesPlayedThisTurnP1 = new(StringComparer.OrdinalIgnoreCase);
    public readonly HashSet<string> NamesPlayedThisTurnP2 = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<PlayerId, DuelCard?> LastPlayedCharByPlayer = new();

    public bool TerrainPlayedThisTurn;
    public int ScoresResolvedThisTurn;
    public PlayerId? CannotScoreThisTurnPlayer;
    public int BonusCharPlaysThisTurn;

    // Gardes anti-récursion
    public int DrawDepth;
    public int MillDepth;
    public int DiscardDepth;
    public int BanishDepth;
    public int LeavePlayDepth;
    public int MoveDepth;
    public int ForceZeroCheckDepth;
    public int TriggerDepth;

    public void ResetTurn()
    {
        TempForceByCardId.Clear();
        ForceZeroThisScoreCardIds.Clear();
        DoubledThisTurnCardIds.Clear();
        VisitedSlotsThisTurn.Clear();
        UsedOncePerTurn.Clear();
        FirstPlayFreeUsedSlots.Clear();
        CharsPlayedOnSlotThisTurnP1.Clear();
        CharsPlayedOnSlotThisTurnP2.Clear();
        NamesPlayedThisTurnP1.Clear();
        NamesPlayedThisTurnP2.Clear();
        TerrainPlayedThisTurn = false;
        ScoresResolvedThisTurn = 0;
        CannotScoreThisTurnPlayer = null;
        BonusCharPlaysThisTurn = 0;
    }

    public List<DuelCard> GetPlayedOnSlotThisTurn(PlayerId player, int slotIndex)
    {
        var map = player == PlayerId.Player1 ? CharsPlayedOnSlotThisTurnP1 : CharsPlayedOnSlotThisTurnP2;
        if (!map.TryGetValue(slotIndex, out var list))
        {
            list = new List<DuelCard>();
            map[slotIndex] = list;
        }
        return list;
    }

    public HashSet<string> GetNamesPlayedThisTurn(PlayerId player) =>
        player == PlayerId.Player1 ? NamesPlayedThisTurnP1 : NamesPlayedThisTurnP2;
}

/// <summary>
/// Utilitaires statiques partagés entre TerrainSlot et DuelEngine (C# pur).
/// </summary>
internal static class PassiveCardHelper
{
    private static readonly ConditionalWeakTable<DuelState, PassiveState> _stateMap = new();

    private static readonly FieldInfo? _banishP1Field = typeof(DuelState).GetField("_banishP1", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _banishP2Field = typeof(DuelState).GetField("_banishP2", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _deckP1Field = typeof(DuelState).GetField("_deckP1", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _deckP2Field = typeof(DuelState).GetField("_deckP2", BindingFlags.Instance | BindingFlags.NonPublic);

    private static MethodInfo? _s3s5GetAuxMethod;
    private static FieldInfo? _s3s5FacedownField;
    private static bool _s3s5Probed;

    private static readonly string[] BoysBandKeywords =
    {
        "Motard", "Indien", "Ouvrier", "Policier", "Cowboy", "Soldat"
    };

    private static readonly string[] CharDiscardEffectKeywords =
    {
        "discard_opp_char", "discard_any_char", "discard_own_char",
        "discard_all_other_chars", "discard_other_chars", "discard_char",
        "discard_opp_strongest"
    };

    public static PassiveState GetState(DuelState state) => _stateMap.GetOrCreateValue(state);

    public static bool TryGetState(DuelState? state, out PassiveState? pState)
    {
        if (state == null)
        {
            pState = null;
            return false;
        }
        pState = _stateMap.GetOrCreateValue(state);
        return true;
    }

    public static bool ListContains(IReadOnlyList<string>? list, string target)
    {
        if (list == null) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], target, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public static bool RawCardHasEffect(DuelCard? card, string effectId)
    {
        if (card == null) return false;
        return ListContains(card.PassiveEffectIds, effectId) || ListContains(card.EffectIds, effectId);
    }

    public static bool RawCardHasAnyEffect(DuelCard? card, string effectId, bool comboActive)
    {
        if (RawCardHasEffect(card, effectId)) return true;
        return comboActive && card != null && ListContains(card.ComboEffectIds, effectId);
    }

    /// <summary>
    /// Indique si les effets permanents/actifs d'un personnage sur un terrain sont annulés
    /// (ex: SCENE DE CONCERT, SAMOURABSTRAIT, BOSS DES SAVIORS, ou combo AUTOSTOPPEUR).
    /// </summary>
    public static bool AreCardPassivesSuppressed(DuelState? state, TerrainSlot? slot, PlayerId owner, DuelCard card, bool comboActive = false)
    {
        if (card == null || card.Kind == CardKind.Terrain) return false;

        if (slot != null && RawCardHasEffect(slot.Card, "passive_terrain_cancel_all_char_effects"))
        {
            return true;
        }

        if (comboActive && ListContains(card.ComboEffectIds, "passive_cancel_own_permanent"))
        {
            return true;
        }

        if (state != null)
        {
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var sl = state.Slots[s];
                if (HasBossSaviorsOverride(sl.GetCharacters(owner), card))
                {
                    return true;
                }

                if (card.Name.IndexOf("Samoura", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (HasOtherWithEffect(sl.CharactersP1, card, "passive_cancel_other_samourai_permanents") ||
                        HasOtherWithEffect(sl.CharactersP2, card, "passive_cancel_other_samourai_permanents"))
                    {
                        return true;
                    }
                }
            }
        }
        else if (slot != null)
        {
            if (HasBossSaviorsOverride(slot.GetCharacters(owner), card)) return true;
            if (card.Name.IndexOf("Samoura", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (HasOtherWithEffect(slot.CharactersP1, card, "passive_cancel_other_samourai_permanents") ||
                 HasOtherWithEffect(slot.CharactersP2, card, "passive_cancel_other_samourai_permanents")))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasBossSaviorsOverride(IReadOnlyList<DuelCard> chars, DuelCard target)
    {
        for (int i = 0; i < chars.Count; i++)
        {
            if (!ReferenceEquals(chars[i], target) &&
                RawCardHasEffect(chars[i], "passive_others_copy_name_force_lose_text"))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasOtherWithEffect(IReadOnlyList<DuelCard> chars, DuelCard target, string effectId)
    {
        for (int i = 0; i < chars.Count; i++)
        {
            if (!ReferenceEquals(chars[i], target) && RawCardHasEffect(chars[i], effectId))
            {
                return true;
            }
        }
        return false;
    }

    public static bool CardHasPassive(DuelState? state, TerrainSlot? slot, PlayerId owner, DuelCard? card, string effectId, bool comboActive = false)
    {
        if (card == null) return false;
        if (card.Kind == CardKind.Character && AreCardPassivesSuppressed(state, slot, owner, card, comboActive))
        {
            return false;
        }
        return RawCardHasAnyEffect(card, effectId, comboActive);
    }

    public static DuelCard? FindBossDesSaviors(DuelState? state, TerrainSlot? slot, PlayerId owner, DuelCard exceptCard)
    {
        if (state != null)
        {
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var chars = state.Slots[s].GetCharacters(owner);
                for (int i = 0; i < chars.Count; i++)
                {
                    if (!ReferenceEquals(chars[i], exceptCard) &&
                        RawCardHasEffect(chars[i], "passive_others_copy_name_force_lose_text"))
                    {
                        return chars[i];
                    }
                }
            }
        }
        else if (slot != null)
        {
            var chars = slot.GetCharacters(owner);
            for (int i = 0; i < chars.Count; i++)
            {
                if (!ReferenceEquals(chars[i], exceptCard) &&
                    RawCardHasEffect(chars[i], "passive_others_copy_name_force_lose_text"))
                {
                    return chars[i];
                }
            }
        }
        return null;
    }

    public static string GetEffectiveName(DuelState? state, PlayerId owner, DuelCard card, TerrainSlot? slot = null)
    {
        if (card == null) return string.Empty;
        if (TryGetState(state, out var pState) && pState != null && pState.NamelessCardIds.Contains(card.Id))
        {
            return string.Empty;
        }

        var boss = FindBossDesSaviors(state, slot, owner, card);
        if (boss != null)
        {
            return boss.Name;
        }

        return card.Name;
    }

    public static bool CardMatchesName(DuelState? state, PlayerId owner, DuelCard card, string query, TerrainSlot? slot = null)
    {
        if (card == null || string.IsNullOrEmpty(query)) return false;
        if (TryGetState(state, out var pState) && pState != null && pState.NamelessCardIds.Contains(card.Id))
        {
            return false;
        }

        var effName = GetEffectiveName(state, owner, card, slot);
        if (!string.IsNullOrEmpty(effName) && effName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (FindBossDesSaviors(state, slot, owner, card) != null)
        {
            return false;
        }

        if (query.IndexOf("Chien", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (RawCardHasEffect(card, "passive_name_is_chien") || RawCardHasEffect(card, "passive_alias_chien")))
            return true;

        if (query.IndexOf("Laink", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (RawCardHasEffect(card, "passive_name_is_laink") ||
             RawCardHasEffect(card, "passive_count_as_laink") ||
             RawCardHasEffect(card, "passive_alias_laink") ||
             RawCardHasEffect(card, "passive_considered_soldat_americain_ww2") ||
             RawCardHasEffect(card, "passive_alias_soldat_americain_ww2")))
            return true;

        if (query.IndexOf("Terracid", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (RawCardHasEffect(card, "passive_name_is_terracid") ||
             RawCardHasEffect(card, "passive_count_as_terracid") ||
             RawCardHasEffect(card, "passive_alias_terracid") ||
             RawCardHasEffect(card, "passive_considered_soldat_allemand_ww2") ||
             RawCardHasEffect(card, "passive_alias_soldat_allemand_ww2")))
            return true;

        if (query.IndexOf("Viking", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (RawCardHasEffect(card, "passive_considered_viking") || RawCardHasEffect(card, "passive_alias_viking")))
            return true;

        if (query.IndexOf("Poule", StringComparison.OrdinalIgnoreCase) >= 0 &&
            RawCardHasEffect(card, "passive_alias_poule"))
            return true;

        if (query.IndexOf("Robot", StringComparison.OrdinalIgnoreCase) >= 0 &&
            RawCardHasEffect(card, "passive_alias_robot"))
            return true;

        if (query.IndexOf("Soldat", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (RawCardHasEffect(card, "passive_considered_soldat_allemand_ww2") ||
             RawCardHasEffect(card, "passive_alias_soldat_allemand_ww2") ||
             RawCardHasEffect(card, "passive_considered_soldat_americain_ww2") ||
             RawCardHasEffect(card, "passive_alias_soldat_americain_ww2")))
            return true;

        return false;
    }

    public static bool IsEffigy(DuelCard card, string effigyName)
    {
        if (card == null) return false;
        if (string.Equals(card.Effigy, effigyName, StringComparison.OrdinalIgnoreCase)) return true;

        if (string.Equals(effigyName, "Laink", StringComparison.OrdinalIgnoreCase))
        {
            return RawCardHasEffect(card, "passive_name_is_laink") ||
                   RawCardHasEffect(card, "passive_count_as_laink") ||
                   RawCardHasEffect(card, "passive_alias_laink") ||
                   RawCardHasEffect(card, "passive_considered_soldat_americain_ww2") ||
                   RawCardHasEffect(card, "passive_alias_soldat_americain_ww2");
        }
        if (string.Equals(effigyName, "Terracid", StringComparison.OrdinalIgnoreCase))
        {
            return RawCardHasEffect(card, "passive_name_is_terracid") ||
                   RawCardHasEffect(card, "passive_count_as_terracid") ||
                   RawCardHasEffect(card, "passive_alias_terracid") ||
                   RawCardHasEffect(card, "passive_considered_soldat_allemand_ww2") ||
                   RawCardHasEffect(card, "passive_alias_soldat_allemand_ww2");
        }
        return false;
    }

    public static bool IsBoysBandEligible(DuelState? state, PlayerId owner, DuelCard card, TerrainSlot? slot = null)
    {
        for (int i = 0; i < BoysBandKeywords.Length; i++)
        {
            if (CardMatchesName(state, owner, card, BoysBandKeywords[i], slot))
            {
                return true;
            }
        }
        return false;
    }

    public static bool HasCharDiscardEffect(DuelCard card)
    {
        if (card.EffectIds == null) return false;
        for (int i = 0; i < card.EffectIds.Count; i++)
        {
            string e = card.EffectIds[i];
            for (int k = 0; k < CharDiscardEffectKeywords.Length; k++)
            {
                if (e.IndexOf(CharDiscardEffectKeywords[k], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static int GetTotalFacedownCount(DuelState? state, string? onlyForCardId = null)
    {
        if (state == null) return 0;
        if (!_s3s5Probed)
        {
            _s3s5Probed = true;
            var asm = typeof(DuelEngine).Assembly;
            var s35 = asm.GetType("WankulCrazy.Duel.Engine.EffectsS3S5");
            _s3s5GetAuxMethod = s35?.GetMethod("GetAuxState", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var auxType = s35?.GetNestedType("S3S5AuxState", BindingFlags.NonPublic | BindingFlags.Public);
            _s3s5FacedownField = auxType?.GetField("FacedownByCardId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (_s3s5GetAuxMethod == null || _s3s5FacedownField == null) return 0;
        try
        {
            var aux = _s3s5GetAuxMethod.Invoke(null, new object[] { state });
            if (aux != null && _s3s5FacedownField.GetValue(aux) is Dictionary<string, List<DuelCard>> map)
            {
                if (onlyForCardId != null)
                {
                    return map.TryGetValue(onlyForCardId, out var lst) ? lst.Count : 0;
                }
                int sum = 0;
                foreach (var kv in map) sum += kv.Value.Count;
                return sum;
            }
        }
        catch { }
        return 0;
    }

    public static bool TryTakeFacedownUnderCard(DuelState state, string cardId, out List<DuelCard> cards)
    {
        cards = new List<DuelCard>();
        GetTotalFacedownCount(state);
        if (_s3s5GetAuxMethod == null || _s3s5FacedownField == null) return false;
        try
        {
            var aux = _s3s5GetAuxMethod.Invoke(null, new object[] { state });
            if (aux != null && _s3s5FacedownField.GetValue(aux) is Dictionary<string, List<DuelCard>> map &&
                map.TryGetValue(cardId, out var list) && list.Count > 0)
            {
                cards.AddRange(list);
                list.Clear();
                map.Remove(cardId);
                return true;
            }
        }
        catch { }
        return false;
    }

    public static List<DuelCard>? GetMutableBanish(DuelState state, PlayerId player)
    {
        var field = player == PlayerId.Player1 ? _banishP1Field : _banishP2Field;
        return field?.GetValue(state) as List<DuelCard>;
    }

    public static List<DuelCard>? GetMutableDeck(DuelState state, PlayerId player)
    {
        var field = player == PlayerId.Player1 ? _deckP1Field : _deckP2Field;
        return field?.GetValue(state) as List<DuelCard>;
    }

    public static void PutCardOnTopOfDeck(DuelState state, PlayerId player, DuelCard card)
    {
        var deck = GetMutableDeck(state, player);
        if (deck != null)
        {
            deck.Insert(0, card);
        }
        else
        {
            state.AddCardToBottomDeck(player, card);
        }
    }

    public static bool BoardHasCardWithPassive(DuelState state, string effectId, PlayerId? onlyOwner = null)
    {
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var slot = state.Slots[s];
            if (!onlyOwner.HasValue && RawCardHasEffect(slot.Card, effectId)) return true;

            if (!onlyOwner.HasValue || onlyOwner.Value == PlayerId.Player1)
            {
                for (int i = 0; i < slot.CharactersP1.Count; i++)
                {
                    if (CardHasPassive(state, slot, PlayerId.Player1, slot.CharactersP1[i], effectId)) return true;
                }
            }
            if (!onlyOwner.HasValue || onlyOwner.Value == PlayerId.Player2)
            {
                for (int i = 0; i < slot.CharactersP2.Count; i++)
                {
                    if (CardHasPassive(state, slot, PlayerId.Player2, slot.CharactersP2[i], effectId)) return true;
                }
            }
        }
        return false;
    }

    public static int CountBoardPassives(DuelState state, string effectId, PlayerId? onlyOwner = null)
    {
        int count = 0;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var slot = state.Slots[s];
            if (!onlyOwner.HasValue && RawCardHasEffect(slot.Card, effectId)) count++;

            if (!onlyOwner.HasValue || onlyOwner.Value == PlayerId.Player1)
            {
                for (int i = 0; i < slot.CharactersP1.Count; i++)
                {
                    if (CardHasPassive(state, slot, PlayerId.Player1, slot.CharactersP1[i], effectId)) count++;
                }
            }
            if (!onlyOwner.HasValue || onlyOwner.Value == PlayerId.Player2)
            {
                for (int i = 0; i < slot.CharactersP2.Count; i++)
                {
                    if (CardHasPassive(state, slot, PlayerId.Player2, slot.CharactersP2[i], effectId)) count++;
                }
            }
        }
        return count;
    }

    public static int CountCharsInPlay(DuelState state, PlayerId? player = null, string? nameContains = null, string? effigy = null)
    {
        int count = 0;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var slot = state.Slots[s];
            if (!player.HasValue || player.Value == PlayerId.Player1)
            {
                for (int i = 0; i < slot.CharactersP1.Count; i++)
                {
                    var c = slot.CharactersP1[i];
                    if ((nameContains == null || CardMatchesName(state, PlayerId.Player1, c, nameContains, slot)) &&
                        (effigy == null || IsEffigy(c, effigy)))
                    {
                        count++;
                    }
                }
            }
            if (!player.HasValue || player.Value == PlayerId.Player2)
            {
                for (int i = 0; i < slot.CharactersP2.Count; i++)
                {
                    var c = slot.CharactersP2[i];
                    if ((nameContains == null || CardMatchesName(state, PlayerId.Player2, c, nameContains, slot)) &&
                        (effigy == null || IsEffigy(c, effigy)))
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    public static bool CanCardLeaveDiscard(DuelState state, PlayerId player)
    {
        if (BoardHasCardWithPassive(state, "passive_cannot_leave_discard"))
        {
            return false;
        }
        var opp = player.Opponent();
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var oppChars = state.Slots[s].GetCharacters(opp);
            for (int i = 0; i < oppChars.Count; i++)
            {
                var c = oppChars[i];
                if (CardHasPassive(state, state.Slots[s], opp, c, "passive_if_no_facedown_forbid_opp_take_discard") &&
                    GetTotalFacedownCount(state, c.Id) == 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static bool IsDiscardRedirectedToBanish(DuelState state, TerrainSlot? slot = null)
    {
        if (slot != null && (RawCardHasEffect(slot.Card, "passive_terrain_banish_instead_of_discard") ||
                             RawCardHasEffect(slot.Card, "passive_discard_is_banished")))
        {
            return true;
        }
        return BoardHasCardWithPassive(state, "passive_discard_is_banished") ||
               BoardHasCardWithPassive(state, "passive_terrain_banish_instead_of_discard");
    }

    public static bool HasSlotEffectAny(TerrainSlot slot, string target) => slot.HasSlotEffectAny(target);

    public static bool ContainsCard(IReadOnlyList<DuelCard> list, DuelCard card)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], card)) return true;
        }
        return false;
    }
}

/// <summary>
/// Gestionnaire centralisé des passifs S1–S5 appelés par les points de cycle de vie de DuelEngine.
/// </summary>
internal static class PassiveLifecycle
{
    public static DuelActionResult? ValidatePlayTerrain(DuelEngine engine, PlayerId player)
    {
        if (PassiveCardHelper.BoardHasCardWithPassive(engine.State, "passive_terrain_forbid_other_terrains"))
        {
            return DuelActionResult.Refused("Another terrain forbids playing additional terrains.");
        }
        return null;
    }

    public static DuelActionResult? ValidatePlayCharacter(
        DuelEngine engine,
        PlayerId player,
        DuelCard cardToPlay,
        TerrainSlot slot,
        int slotIndex,
        ref int insertIndex)
    {
        var state = engine.State;
        var opp = player.Opponent();

        // passive_max_chars_10
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_max_chars_10") &&
            slot.CharactersP1.Count + slot.CharactersP2.Count >= 10)
        {
            return DuelActionResult.Refused("Terrain character limit (10) reached.");
        }

        // passive_force_play_on_this_slot
        if (!PassiveCardHelper.HasSlotEffectAny(slot, "passive_force_play_on_this_slot") &&
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_force_play_on_this_slot"))
        {
            return DuelActionResult.Refused("Must play character on the terrain forcing placement.");
        }

        // passive_forbid_opp_char_force_ge_2_on_slot
        if (cardToPlay.Force >= 2)
        {
            var oppChars = slot.GetCharacters(opp);
            for (int i = 0; i < oppChars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, slot, opp, oppChars[i], "passive_forbid_opp_char_force_ge_2_on_slot"))
                {
                    return DuelActionResult.Refused("Opponent passive forbids characters with Force >= 2 on this slot.");
                }
            }
        }

        // passive_forbid_no_gem_chars
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_forbid_no_gem_chars") &&
            cardToPlay.Gems == GemMask.None)
        {
            return DuelActionResult.Refused("Cannot play characters without gems on this terrain.");
        }

        // passive_forbid_if_4_cavaliers
        if (PassiveCardHelper.RawCardHasEffect(cardToPlay, "passive_forbid_if_4_cavaliers") &&
            PassiveCardHelper.CountCharsInPlay(state, null, "Cavalier de l'Apocalypse") >= 4)
        {
            return DuelActionResult.Refused("Cannot play while 4 Cavalier de l'Apocalypse are in play.");
        }

        // passive_terrain_forbid_duplicate_names
        if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_forbid_duplicate_names"))
        {
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var sl = state.Slots[s];
                for (int i = 0; i < sl.CharactersP1.Count; i++)
                {
                    if (PassiveCardHelper.CardMatchesName(state, PlayerId.Player1, sl.CharactersP1[i], cardToPlay.Name, sl))
                        return DuelActionResult.Refused("Duplicate character name forbidden in play.");
                }
                for (int i = 0; i < sl.CharactersP2.Count; i++)
                {
                    if (PassiveCardHelper.CardMatchesName(state, PlayerId.Player2, sl.CharactersP2[i], cardToPlay.Name, sl))
                        return DuelActionResult.Refused("Duplicate character name forbidden in play.");
                }
            }
        }

        // passive_unique_name_on_slot
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_unique_name_on_slot") ||
            PassiveCardHelper.HasSlotEffectAny(slot, "passive_unique_name_on_slot"))
        {
            for (int i = 0; i < slot.CharactersP1.Count; i++)
            {
                if (string.Equals(PassiveCardHelper.GetEffectiveName(state, PlayerId.Player1, slot.CharactersP1[i], slot), cardToPlay.Name, StringComparison.OrdinalIgnoreCase))
                    return DuelActionResult.Refused("Character with this name is already on this slot.");
            }
            for (int i = 0; i < slot.CharactersP2.Count; i++)
            {
                if (string.Equals(PassiveCardHelper.GetEffectiveName(state, PlayerId.Player2, slot.CharactersP2[i], slot), cardToPlay.Name, StringComparison.OrdinalIgnoreCase))
                    return DuelActionResult.Refused("Character with this name is already on this slot.");
            }
        }

        // passive_unique_in_play
        if (PassiveCardHelper.RawCardHasEffect(cardToPlay, "passive_unique_in_play") &&
            PassiveCardHelper.CountCharsInPlay(state, player, cardToPlay.Name) > 0)
        {
            return DuelActionResult.Refused("Only one copy of this character can be in play.");
        }

        // passive_restrict_play_col_1 -> force insert at column 0
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_restrict_play_col_1"))
        {
            insertIndex = 0;
        }

        // passive_restrict_play_row_1 -> limit to 1 row (4 characters max per side on slot)
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_restrict_play_row_1") &&
            slot.GetCharacters(player).Count >= 4)
        {
            return DuelActionResult.Refused("Terrain restricts play to the first row only.");
        }

        return null;
    }

    public static bool IsFirstPlayFreeOnSlot(DuelEngine engine, TerrainSlot slot, int slotIndex)
    {
        if (!PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_first_play_free")) return false;
        var pState = PassiveCardHelper.GetState(engine.State);
        return !pState.FirstPlayUsedSlotsContains(slotIndex);
    }

    public static int ComputeEffectivePlayCost(DuelEngine engine, PlayerId player, DuelCard card, int slotIndex)
    {
        var state = engine.State;
        if (card == null || card.Kind == CardKind.Terrain || card.IsDuo || engine.FreeCharactersTurnPlayer == player)
        {
            return 0;
        }

        if (PassiveCardHelper.RawCardHasEffect(card, "passive_pay_cost_by_discarding_in_play"))
        {
            return Math.Max(0, card.Cost);
        }

        if (PassiveCardHelper.RawCardHasEffect(card, "passive_free_play_if_banish_not_empty") &&
            state.GetBanish(player).Count > 0)
        {
            return 0;
        }

        var pState = PassiveCardHelper.GetState(state);
        if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_play_copy_free") &&
            pState.GetNamesPlayedThisTurn(player).Contains(card.Name))
        {
            return 0;
        }

        int cost = card.Cost;
        var opp = player.Opponent();
        TerrainSlot? slot = (slotIndex >= 0 && slotIndex < state.Slots.Count) ? state.Slots[slotIndex] : null;

        if (slot != null)
        {
            bool hasSet1 = PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_cost_set_1");
            var ownSlotChars = slot.GetCharacters(player);
            if (!hasSet1)
            {
                for (int i = 0; i < ownSlotChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, slot, player, ownSlotChars[i], "passive_cost_set_1"))
                    {
                        hasSet1 = true;
                        break;
                    }
                }
            }
            if (hasSet1)
            {
                cost = 1;
            }

            if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_cost_self_minus_1"))
            {
                cost -= 1;
            }
            if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_cost_plus_2"))
            {
                cost += 2;
            }
            for (int i = 0; i < ownSlotChars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, slot, player, ownSlotChars[i], "passive_cost_self_minus_1"))
                {
                    cost -= 1;
                }
                if (PassiveCardHelper.CardHasPassive(state, slot, player, ownSlotChars[i], "passive_cost_minus_1_if_all_others_higher_force"))
                {
                    bool allHigher = ownSlotChars.Count > 1;
                    for (int k = 0; k < ownSlotChars.Count; k++)
                    {
                        if (k != i && ownSlotChars[k].Force <= ownSlotChars[i].Force)
                        {
                            allHigher = false;
                            break;
                        }
                    }
                    if (allHigher) cost -= 1;
                }
            }

            if (slot.PlacedBy == opp && PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_cost_opp_plus_2"))
            {
                cost += 2;
            }
            var oppSlotChars = slot.GetCharacters(opp);
            for (int i = 0; i < oppSlotChars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, slot, opp, oppSlotChars[i], "passive_cost_opp_plus_2"))
                {
                    cost += 2;
                }
            }

            if (HasDiscardNamed(state.GetDiscard(player), "Ouvrier"))
            {
                for (int i = 0; i < ownSlotChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, slot, player, ownSlotChars[i], "passive_cost_ouvrier_minus_1"))
                        cost -= 1;
                }
            }
            if (HasDiscardNamed(state.GetDiscard(opp), "Ouvrier"))
            {
                for (int i = 0; i < oppSlotChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, slot, opp, oppSlotChars[i], "passive_cost_ouvrier_opp_plus_1"))
                        cost += 1;
                }
            }
        }

        if (PassiveCardHelper.RawCardHasEffect(card, "passive_cost_self_minus_1"))
        {
            cost -= 1;
        }

        // Board-wide cost modifiers
        cost -= PassiveCardHelper.CountBoardPassives(state, "passive_cost_all_minus_1");
        cost += PassiveCardHelper.CountBoardPassives(state, "passive_cost_all_plus_1");

        if (card.Force <= 4)
        {
            cost -= PassiveCardHelper.CountBoardPassives(state, "passive_cost_force_le_4_minus_1");
        }

        int ownBoardCount = PassiveCardHelper.CountCharsInPlay(state, player);
        int oppBoardCount = PassiveCardHelper.CountCharsInPlay(state, opp);
        if (ownBoardCount < oppBoardCount &&
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_cost_self_minus_2_if_fewer_chars", player))
        {
            cost -= 2;
        }

        if (slot != null && slot.PlacedBy == opp && PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_cost_opp_plus_1"))
        {
            cost += 1;
        }
        cost += PassiveCardHelper.CountBoardPassives(state, "passive_cost_opp_plus_1", opp);

        if (!engine.FirstCharacterPlayedThisTurn &&
            (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_first_char_cost_minus_1", player) ||
             PassiveCardHelper.BoardHasCardWithPassive(state, "duo_martine_jeanine", player) ||
             PassiveCardHelper.BoardHasCardWithPassive(state, "duo_martine_jeanine_active_cost_minus_1", player)))
        {
            cost -= 1;
        }

        return Math.Max(0, cost);
    }

    private static bool HasDiscardNamed(IReadOnlyList<DuelCard> discard, string part)
    {
        for (int i = 0; i < discard.Count; i++)
        {
            if (discard[i].Name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    public static void OnTerrainEnteredPlay(DuelEngine engine, PlayerId player, int slotIndex, DuelCard terrainCard, bool wasPlayedFromHand)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (wasPlayedFromHand)
        {
            pState.TerrainPlayedThisTurn = true;
        }

        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];
            if (wasPlayedFromHand && PassiveCardHelper.RawCardHasEffect(sl.Card, "terrain_on_terrain_played_draw_1"))
            {
                engine.Emit(new EffectTriggeredEvent(player, "terrain_on_terrain_played_draw_1", sl.Card!, s));
                engine.DrawCards(player, 1);
            }

            foreach (PlayerId side in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                var chars = new List<DuelCard>(sl.GetCharacters(side));
                for (int i = 0; i < chars.Count && !state.IsGameOver; i++)
                {
                    var ch = chars[i];
                    if (wasPlayedFromHand && PassiveCardHelper.CardHasPassive(state, sl, side, ch, "passive_on_terrain_played_take_top_discard"))
                    {
                        var disc = state.GetDiscard(side);
                        if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, side))
                        {
                            var top = disc[disc.Count - 1];
                            state.RemoveCardFromDiscard(side, top);
                            state.AddCardToHand(side, top);
                            engine.Emit(new EffectTriggeredEvent(side, "passive_on_terrain_played_take_top_discard", ch, s));
                        }
                    }

                    if (PassiveCardHelper.CardHasPassive(state, sl, side, ch, "passive_on_terrain_enter_return_discard_bottom_2"))
                    {
                        var disc = state.GetDiscard(side);
                        int take = Math.Min(2, disc.Count);
                        if (take > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, side))
                        {
                            for (int k = 0; k < take; k++)
                            {
                                var top = disc[disc.Count - 1];
                                state.RemoveCardFromDiscard(side, top);
                                state.AddCardToBottomDeck(side, top);
                            }
                            engine.Emit(new EffectTriggeredEvent(side, "passive_on_terrain_enter_return_discard_bottom_2", ch, s));
                        }
                    }
                }
            }
        }
    }

    public static void OnBeforeCharacterAdded(DuelEngine engine, PlayerId player, DuelCard cardToPlay)
    {
        if (!PassiveCardHelper.RawCardHasEffect(cardToPlay, "passive_unique_trophy")) return;
        var state = engine.State;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var sl = state.Slots[s];
            var chars = new List<DuelCard>(sl.GetCharacters(player));
            for (int i = 0; i < chars.Count; i++)
            {
                if (PassiveCardHelper.RawCardHasEffect(chars[i], "passive_unique_trophy"))
                {
                    DiscardCharacterFromPlay(engine, player, sl, s, chars[i], byEffect: false);
                }
            }
        }
    }

    public static void OnCharacterPlayed(DuelEngine engine, PlayerId player, int slotIndex, DuelCard cardToPlay, IRandom rng, bool wasFirstCharThisTurn)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        pState.PlayedOnTurnByCardId[cardToPlay.Id] = state.TurnNumber;
        pState.LastPlayedCharByPlayer[player] = cardToPlay;
        pState.GetNamesPlayedThisTurn(player).Add(cardToPlay.Name);
        pState.GetPlayedOnSlotThisTurn(player, slotIndex).Add(cardToPlay);
        if (!pState.VisitedSlotsThisTurn.TryGetValue(cardToPlay.Id, out var vis))
        {
            vis = new HashSet<int>();
            pState.VisitedSlotsThisTurn[cardToPlay.Id] = vis;
        }
        vis.Add(slotIndex);

        var slot = state.Slots[slotIndex];
        var opp = player.Opponent();

        // passive_on_play_char_roll_dice_mill (#400012 BATAILLE DE VERS)
        if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_on_play_char_roll_dice_mill"))
        {
            int roll = rng.Next(1, 7);
            engine.Emit(new EffectTriggeredEvent(player, "passive_on_play_char_roll_dice_mill", slot.Card!, slotIndex));
            engine.MillCards(player, roll);
        }

        // passive_first_char_play_copy_from_deck (#400010 IMPRIMERIE)
        if (!state.IsGameOver && wasFirstCharThisTurn &&
            PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_first_char_play_copy_from_deck"))
        {
            var deck = state.GetDeck(player);
            DuelCard? match = null;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Kind == CardKind.Character &&
                    string.Equals(deck[i].Name, cardToPlay.Name, StringComparison.OrdinalIgnoreCase))
                {
                    match = deck[i];
                    break;
                }
            }
            if (match != null && state.RemoveCardFromDeck(player, match))
            {
                state.AddCharacterToSlot(slotIndex, player, match);
                pState.PlayedOnTurnByCardId[match.Id] = state.TurnNumber;
                engine.Emit(new EffectTriggeredEvent(player, "passive_first_char_play_copy_from_deck", slot.Card!, slotIndex));
                engine.Emit(new CharacterPlayedEvent(player, slotIndex, match));
            }
        }

        if (state.IsGameOver) return;

        // Board reactions to character played
        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];

            // Opponent passives reacting to `player` playing a character
            var oppChars = new List<DuelCard>(sl.GetCharacters(opp));
            for (int i = 0; i < oppChars.Count && !state.IsGameOver; i++)
            {
                var oc = oppChars[i];

                // passive_opp_char_loses_name_opp_draws_1 (#300102 VIEILLE SORCIÈRE)
                if (PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_opp_char_loses_name_opp_draws_1"))
                {
                    pState.NamelessCardIds.Add(cardToPlay.Id);
                    engine.Emit(new EffectTriggeredEvent(opp, "passive_opp_char_loses_name_opp_draws_1", oc, s));
                    engine.DrawCards(player, 1);
                }

                // passive_on_opp_play_char_cycle_1 (#200059)
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_on_opp_play_char_cycle_1"))
                {
                    var oppHand = state.GetHand(opp);
                    if (oppHand.Count > 0)
                    {
                        var putBack = oppHand[0];
                        if (state.RemoveCardFromHand(opp, putBack))
                        {
                            state.AddCardToBottomDeck(opp, putBack);
                            engine.Emit(new EffectTriggeredEvent(opp, "passive_on_opp_play_char_cycle_1", oc, s));
                            engine.DrawCards(opp, 1);
                        }
                    }
                }

                // passive_on_opp_3rd_char_debuff_2 (#500106 PROMPTEUR)
                if (!state.IsGameOver && state.CharactersPlayedThisTurn == 3 &&
                    PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_on_opp_3rd_char_debuff_2"))
                {
                    var targetChars = sl.GetCharacters(player);
                    for (int k = 0; k < targetChars.Count; k++)
                    {
                        var cid = targetChars[k].Id;
                        pState.TempForceByCardId[cid] = (pState.TempForceByCardId.TryGetValue(cid, out int cur) ? cur : 0) - 2;
                    }
                    engine.Emit(new EffectTriggeredEvent(opp, "passive_on_opp_3rd_char_debuff_2", oc, s));
                }

                // duo_andre_pierre on opp char play (once per turn)
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "duo_andre_pierre"))
                {
                    string key = $"duo_andre_opp_{oc.Id}";
                    if (pState.UsedOncePerTurn.Add(key) &&
                        engine.Effects.TryGetEffect("duo_andre_pierre_double_force", out var eff) && eff != null)
                    {
                        engine.Emit(new EffectTriggeredEvent(opp, "duo_andre_pierre_double_force", oc, s));
                        eff.Execute(new EffectContext(engine, opp, oc, s));
                    }
                }

                // passive_counter_lower_force_opp_char_by_self_discard (#300130 SOLDAT DE L'ESPACE)
                if (!state.IsGameOver && s == slotIndex && cardToPlay.Force < oc.Force &&
                    PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_counter_lower_force_opp_char_by_self_discard") &&
                    PassiveCardHelper.ContainsCard(sl.GetCharacters(player), cardToPlay))
                {
                    // Auto-counter if P2/AI or counter is clearly advantageous; headless P1 skips optional sacrifice unless solo
                    if (opp == PlayerId.Player2)
                    {
                        engine.Emit(new EffectTriggeredEvent(opp, "passive_counter_lower_force_opp_char_by_self_discard", oc, s));
                        DiscardCharacterFromPlay(engine, opp, sl, s, oc, byEffect: false);
                        DiscardCharacterFromPlay(engine, player, sl, s, cardToPlay, byEffect: true);
                    }
                }
            }

            // Own passives reacting to own char play
            var ownChars = new List<DuelCard>(sl.GetCharacters(player));
            for (int i = 0; i < ownChars.Count && !state.IsGameOver; i++)
            {
                var ch = ownChars[i];
                if (PassiveCardHelper.CardHasPassive(state, sl, player, ch, "duo_andre_pierre"))
                {
                    string key = $"duo_andre_act_{ch.Id}";
                    if (state.GetHand(player).Count > 0 && pState.UsedOncePerTurn.Add(key) &&
                        engine.Effects.TryGetEffect("duo_andre_pierre_active_double_force", out var eff) && eff != null)
                    {
                        engine.Emit(new EffectTriggeredEvent(player, "duo_andre_pierre_active_double_force", ch, s));
                        eff.Execute(new EffectContext(engine, player, ch, s));
                    }
                }
                if (!state.IsGameOver && !ReferenceEquals(ch, cardToPlay) && pState.MoveDepth == 0)
                {
                    if (PassiveCardHelper.CardHasPassive(state, sl, player, ch, "passive_on_char_played_move_self_unvisited_terrain"))
                    {
                        engine.Emit(new EffectTriggeredEvent(player, "passive_on_char_played_move_self_unvisited_terrain", ch, s));
                    }
                    if (PassiveCardHelper.CardHasPassive(state, sl, player, ch, "passive_on_any_char_played_move_self"))
                    {
                        engine.Emit(new EffectTriggeredEvent(player, "passive_on_any_char_played_move_self", ch, s));
                    }
                }
            }
        }

        if (PassiveCardHelper.RawCardHasEffect(cardToPlay, "passive_banish_if_left_after_robot_play") &&
            pState.TriggerDepth > 0)
        {
            pState.BanishOnLeaveCardIds.Add(cardToPlay.Id);
        }

        // Check post-play Traître / Réaliste / Force<=0 cleanup
        EnforceBoardStatePassives(engine);
    }

    public static void OnAfterComboCheck(DuelEngine engine, PlayerId player, int slotIndex, bool anyComboFormed)
    {
        var state = engine.State;
        if (state.IsGameOver) return;
        var pState = PassiveCardHelper.GetState(state);
        var opp = player.Opponent();
        var slot = state.Slots[slotIndex];

        if (!anyComboFormed)
        {
            // passive_on_opp_play_no_combo_mill_1 (#300068 TRICKY SORCIER)
            for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
            {
                var oppChars = state.Slots[s].GetCharacters(opp);
                for (int i = 0; i < oppChars.Count && !state.IsGameOver; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, state.Slots[s], opp, oppChars[i], "passive_on_opp_play_no_combo_mill_1"))
                    {
                        engine.Emit(new EffectTriggeredEvent(opp, "passive_on_opp_play_no_combo_mill_1", oppChars[i], s));
                        engine.MillCards(player, 1);
                    }
                }
            }
            return;
        }

        // Combo formed reactions
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_on_combo_draw_1"))
        {
            engine.Emit(new EffectTriggeredEvent(player, "passive_on_combo_draw_1", slot.Card!, slotIndex));
            engine.DrawCards(player, 1);
        }

        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];
            var ownChars = new List<DuelCard>(sl.GetCharacters(player));
            for (int i = 0; i < ownChars.Count && !state.IsGameOver; i++)
            {
                var c = ownChars[i];
                if (PassiveCardHelper.CardHasPassive(state, sl, player, c, "passive_combo_return_top_discard_once_turn"))
                {
                    string key = $"combo_ret_{c.Id}";
                    var disc = state.GetDiscard(player);
                    if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, player) && pState.UsedOncePerTurn.Add(key))
                    {
                        var top = disc[disc.Count - 1];
                        state.RemoveCardFromDiscard(player, top);
                        state.AddCardToHand(player, top);
                        engine.Emit(new EffectTriggeredEvent(player, "passive_combo_return_top_discard_once_turn", c, s));
                    }
                }
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, player, c, "passive_on_combo_move_self") &&
                    pState.MoveDepth == 0)
                {
                    // Déplacement optionnel exposé via ActivateCardPassive ou événement combo
                    engine.Emit(new EffectTriggeredEvent(player, "passive_on_combo_move_self", c, s));
                }
            }

            var oppChars = new List<DuelCard>(sl.GetCharacters(opp));
            for (int i = 0; i < oppChars.Count && !state.IsGameOver; i++)
            {
                var oc = oppChars[i];
                if (PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_discard_self_cancel_opp_combo"))
                {
                    if (opp == PlayerId.Player2 && DiscardCharacterFromPlay(engine, opp, sl, s, oc, byEffect: false))
                    {
                        engine.Emit(new EffectTriggeredEvent(opp, "passive_discard_self_cancel_opp_combo", oc, s));
                    }
                }
                if (PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_on_opp_combo_draw_1"))
                {
                    engine.Emit(new EffectTriggeredEvent(opp, "passive_on_opp_combo_draw_1", oc, s));
                    engine.DrawCards(opp, 1);
                }
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_on_opp_combo_discard_random_1"))
                {
                    engine.Emit(new EffectTriggeredEvent(opp, "passive_on_opp_combo_discard_random_1", oc, s));
                    engine.DiscardFromHand(player, 1);
                }
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, opp, oc, "passive_on_opp_combo_return_discard_bottom_1"))
                {
                    var oppDisc = state.GetDiscard(opp);
                    if (oppDisc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, opp))
                    {
                        var top = oppDisc[oppDisc.Count - 1];
                        state.RemoveCardFromDiscard(opp, top);
                        state.AddCardToBottomDeck(opp, top);
                        engine.Emit(new EffectTriggeredEvent(opp, "passive_on_opp_combo_return_discard_bottom_1", oc, s));
                    }
                }
            }
        }
    }

    public static bool CanDoubleCombo(DuelEngine engine, PlayerId player)
    {
        return !PassiveCardHelper.BoardHasCardWithPassive(engine.State, "passive_opp_cannot_double_combo", player.Opponent());
    }

    public static void OnCharacterMoved(DuelEngine engine, PlayerId player, DuelCard card, int fromSlotIndex, int toSlotIndex)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (!pState.VisitedSlotsThisTurn.TryGetValue(card.Id, out var vis))
        {
            vis = new HashSet<int>();
            pState.VisitedSlotsThisTurn[card.Id] = vis;
        }
        vis.Add(toSlotIndex);

        var toSlot = state.Slots[toSlotIndex];

        // passive_on_move_self_boost_3_turn (#400089 CAUCHEMAR)
        if (PassiveCardHelper.CardHasPassive(state, toSlot, player, card, "passive_on_move_self_boost_3_turn"))
        {
            pState.TempForceByCardId[card.Id] = (pState.TempForceByCardId.TryGetValue(card.Id, out int cur) ? cur : 0) + 3;
            engine.Emit(new EffectTriggeredEvent(player, "passive_on_move_self_boost_3_turn", card, toSlotIndex));
        }

        // passive_on_move_apply_win_effect (#400045) & passive_on_move_take_discard_2 (#400046)
        {
            var destChars = toSlot.GetCharacters(player);
            int movedIdx = -1;
            for (int i = 0; i < destChars.Count; i++)
            {
                if (ReferenceEquals(destChars[i], card)) { movedIdx = i; break; }
            }
            bool comboAtDest = movedIdx >= 0 && toSlot.IsComboActiveOnSlot(player, movedIdx, state);
            if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, toSlot, player, card, "passive_on_move_apply_win_effect", comboAtDest) &&
                toSlot.Card != null && toSlot.Card.EffectIds != null)
            {
                var ctx = new EffectContext(engine, player, toSlot.Card, toSlotIndex);
                for (int i = 0; i < toSlot.Card.EffectIds.Count && !state.IsGameOver; i++)
                {
                    string wid = toSlot.Card.EffectIds[i];
                    if (wid.StartsWith("win_", StringComparison.OrdinalIgnoreCase) &&
                        engine.Effects.TryGetEffect(wid, out var weff) && weff != null)
                    {
                        engine.Emit(new EffectTriggeredEvent(player, wid, toSlot.Card, toSlotIndex));
                        weff.Execute(ctx);
                    }
                }
            }
            if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, toSlot, player, card, "passive_on_move_take_discard_2", comboAtDest))
            {
                var disc = state.GetDiscard(player);
                int take = Math.Min(2, disc.Count);
                for (int k = 0; k < take && PassiveCardHelper.CanCardLeaveDiscard(state, player); k++)
                {
                    var top = disc[disc.Count - 1];
                    state.RemoveCardFromDiscard(player, top);
                    state.AddCardToHand(player, top);
                }
                if (take > 0) engine.Emit(new EffectTriggeredEvent(player, "passive_on_move_take_discard_2", card, toSlotIndex));
            }
        }

        // passive_on_move_discard_adjacent (#500114 CHEVALIER MALADROIT)
        if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, toSlot, player, card, "passive_on_move_discard_adjacent"))
        {
            var chars = toSlot.GetCharacters(player);
            int idx = -1;
            for (int i = 0; i < chars.Count; i++)
            {
                if (ReferenceEquals(chars[i], card)) { idx = i; break; }
            }
            DuelCard? adj = null;
            if (idx > 0) adj = chars[idx - 1];
            else if (idx >= 0 && idx + 1 < chars.Count) adj = chars[idx + 1];
            if (adj != null)
            {
                engine.Emit(new EffectTriggeredEvent(player, "passive_on_move_discard_adjacent", card, toSlotIndex));
                DiscardCharacterFromPlay(engine, player, toSlot, toSlotIndex, adj, byEffect: true);
            }
        }

        // passive_once_turn_on_move_score_dest (#500116 HOMME-VÉLO)
        if (!state.IsGameOver && pState.MoveDepth == 0)
        {
            pState.MoveDepth++;
            try
            {
                for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
                {
                    var sl = state.Slots[s];
                    var chars = new List<DuelCard>(sl.GetCharacters(player));
                    for (int i = 0; i < chars.Count && !state.IsGameOver; i++)
                    {
                        if (PassiveCardHelper.CardHasPassive(state, sl, player, chars[i], "passive_once_turn_on_move_score_dest") &&
                            toSlot.IsActive(state.TurnNumber) &&
                            CanScoreSlot(engine, player, toSlot, toSlotIndex, isAutoScore: false) &&
                            pState.UsedOncePerTurn.Add($"move_score_{chars[i].Id}"))
                        {
                            engine.Emit(new EffectTriggeredEvent(player, "passive_once_turn_on_move_score_dest", chars[i], toSlotIndex));
                            engine.ResolveScore(toSlotIndex, player);
                        }
                    }
                }
            }
            finally
            {
                pState.MoveDepth--;
            }
        }

        EnforceBoardStatePassives(engine);
    }

    public static bool CanMoveCharacter(DuelEngine engine, PlayerId player, DuelCard card, int fromSlotIndex, bool byEffect)
    {
        var slot = engine.State.Slots[fromSlotIndex];
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_forbid_move"))
        {
            return false;
        }
        if (byEffect && PassiveCardHelper.CardHasPassive(engine.State, slot, player, card, "passive_cannot_move_or_discard_by_effect"))
        {
            return false;
        }
        if (byEffect && IsProtectedByGemConnection(engine.State, slot, player, card))
        {
            return false;
        }
        return true;
    }

    private static bool IsProtectedByGemConnection(DuelState state, TerrainSlot slot, PlayerId player, DuelCard card)
    {
        var chars = slot.GetCharacters(player);
        for (int i = 0; i < chars.Count; i++)
        {
            if (ReferenceEquals(chars[i], card))
            {
                bool connLeft = i > 0 && slot.IsComboActiveOnSlot(player, i);
                bool connRight = i + 1 < chars.Count && slot.IsComboActiveOnSlot(player, i + 1);
                if (!connLeft && !connRight) return false;
                if (PassiveCardHelper.CardHasPassive(state, slot, player, card, "passive_cannot_discard_or_move_gem_connected", connLeft))
                    return true;
                if (connLeft && PassiveCardHelper.CardHasPassive(state, slot, player, chars[i - 1], "passive_cannot_discard_or_move_gem_connected"))
                    return true;
                if (connRight && PassiveCardHelper.CardHasPassive(state, slot, player, chars[i + 1], "passive_cannot_discard_or_move_gem_connected"))
                    return true;
            }
        }
        return false;
    }

    public static bool DiscardCharacterFromPlay(DuelEngine engine, PlayerId owner, TerrainSlot slot, int slotIndex, DuelCard card, bool byEffect)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);

        if (byEffect)
        {
            // Indestructible checks
            if (PassiveCardHelper.CardHasPassive(state, slot, owner, card, "passive_cannot_move_or_discard_by_effect"))
                return false;
            if (slot.GetCharacters(owner).Count == 1 &&
                PassiveCardHelper.CardHasPassive(state, slot, owner, card, "passive_no_discard_when_alone"))
                return false;
            if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_no_discard_by_effect"))
                return false;
            if (PassiveCardHelper.CardHasPassive(state, slot, owner, card, "passive_cost_3_untargetable") && card.Cost == 3)
                return false;
            if (IsProtectedByGemConnection(state, slot, owner, card))
                return false;
            if ((PassiveCardHelper.CardMatchesName(state, owner, card, "McFly", slot) ||
                 PassiveCardHelper.CardMatchesName(state, owner, card, "Carlito", slot)) &&
                PassiveCardHelper.BoardHasCardWithPassive(state, "passive_mcfly_carlito_indestructible", owner))
                return false;

            // passive_protect_other_allies_discard (#500050 GENDARME)
            var allies = slot.GetCharacters(owner);
            for (int i = 0; i < allies.Count; i++)
            {
                if (!ReferenceEquals(allies[i], card) &&
                    PassiveCardHelper.CardHasPassive(state, slot, owner, allies[i], "passive_protect_other_allies_discard"))
                {
                    return false;
                }
            }

            // passive_redirect_discard_to_opp_char (#500049 GENDARME)
            if (pState.LeavePlayDepth == 0)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, slot, owner, allies[i], "passive_redirect_discard_to_opp_char"))
                    {
                        var opp = owner.Opponent();
                        for (int s = 0; s < state.Slots.Count; s++)
                        {
                            var oppChars = state.Slots[s].GetCharacters(opp);
                            if (oppChars.Count > 0)
                            {
                                pState.LeavePlayDepth++;
                                try
                                {
                                    engine.Emit(new EffectTriggeredEvent(owner, "passive_redirect_discard_to_opp_char", allies[i], slotIndex));
                                    return DiscardCharacterFromPlay(engine, opp, state.Slots[s], s, oppChars[0], byEffect: true);
                                }
                                finally { pState.LeavePlayDepth--; }
                            }
                        }
                    }
                }
            }

            // passive_substitute_discard (#100112)
            for (int i = 0; i < allies.Count; i++)
            {
                if (!ReferenceEquals(allies[i], card) &&
                    PassiveCardHelper.CardHasPassive(state, slot, owner, allies[i], "passive_substitute_discard"))
                {
                    card = allies[i];
                    break;
                }
            }

            // passive_banish_from_discard_cancel_effect (#500075 VICTIME)
            var ownDiscard = state.GetDiscard(owner);
            for (int i = ownDiscard.Count - 1; i >= 0; i--)
            {
                if (PassiveCardHelper.RawCardHasEffect(ownDiscard[i], "passive_banish_from_discard_cancel_effect"))
                {
                    var v = ownDiscard[i];
                    state.RemoveCardFromDiscard(owner, v);
                    SendToBanish(engine, owner, new[] { v });
                    engine.Emit(new EffectTriggeredEvent(owner, "passive_banish_from_discard_cancel_effect", v, slotIndex));
                    return false;
                }
            }
        }

        if (!slot.RemoveCharacter(owner, card)) return false;

        // Clean any facedown cards under this character -> discard/banish them
        if (PassiveCardHelper.TryTakeFacedownUnderCard(state, card.Id, out var facedowns))
        {
            SendCardsToDiscardOrBanish(engine, owner, facedowns, slot);
        }

        bool mustBanish = pState.BanishOnLeaveCardIds.Remove(card.Id) ||
                          PassiveCardHelper.IsDiscardRedirectedToBanish(state, slot);

        if (mustBanish)
        {
            SendToBanish(engine, owner, new[] { card });
        }
        else
        {
            state.AddCardToDiscard(owner, card);
            engine.Emit(new CardsDiscardedEvent(owner, new[] { card }));
        }

        OnCharacterLeftPlay(engine, owner, slot, slotIndex, card, wasBanished: mustBanish, byEffect: byEffect);
        return true;
    }

    public static void OnCharacterLeftPlay(DuelEngine engine, PlayerId owner, TerrainSlot slot, int slotIndex, DuelCard card, bool wasBanished, bool byEffect)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (pState.LeavePlayDepth > 3 || state.IsGameOver) return;

        pState.LeavePlayDepth++;
        try
        {
            if (PassiveCardHelper.RawCardHasEffect(card, "passive_on_leave_play_mill_2"))
            {
                engine.Emit(new EffectTriggeredEvent(owner, "passive_on_leave_play_mill_2", card, slotIndex));
                engine.MillCards(owner.Opponent(), 2);
            }

            if (!state.IsGameOver && byEffect && !wasBanished &&
                PassiveCardHelper.RawCardHasEffect(card, "passive_on_discard_from_play_draw_4"))
            {
                engine.Emit(new EffectTriggeredEvent(owner, "passive_on_discard_from_play_draw_4", card, slotIndex));
                engine.DrawCards(owner, 4);
            }

            if (!state.IsGameOver && !wasBanished &&
                PassiveCardHelper.RawCardHasEffect(card, "passive_on_sent_from_play_to_discard_play_free_cost_ge_2"))
            {
                PlayFreeCharacterFromHand(engine, owner, minCost: 2, slotIndex: slotIndex);
            }

            if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(card, "passive_on_leave_play_discard_all_enchaines"))
            {
                for (int s = 0; s < state.Slots.Count; s++)
                {
                    var sl = state.Slots[s];
                    var list = new List<DuelCard>(sl.GetCharacters(owner));
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (PassiveCardHelper.CardMatchesName(state, owner, list[i], "Encha", sl))
                        {
                            DiscardCharacterFromPlay(engine, owner, sl, s, list[i], byEffect: true);
                        }
                    }
                }
            }

            // duo_aventuriers_manoir: once per turn on active turn when own char is discarded, draw 2
            if (!state.IsGameOver && state.ActivePlayer == owner && !wasBanished)
            {
                for (int s = 0; s < state.Slots.Count; s++)
                {
                    var chars = state.Slots[s].GetCharacters(owner);
                    for (int i = 0; i < chars.Count; i++)
                    {
                        if (PassiveCardHelper.CardHasPassive(state, state.Slots[s], owner, chars[i], "duo_aventuriers_manoir"))
                        {
                            string key = $"duo_avent_draw_{chars[i].Id}";
                            if (pState.UsedOncePerTurn.Add(key))
                            {
                                engine.Emit(new EffectTriggeredEvent(owner, "duo_aventuriers_active_draw_2", chars[i], s));
                                engine.DrawCards(owner, 2);
                                break;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            pState.LeavePlayDepth--;
        }
    }

    public static void SendCardsToDiscardOrBanish(DuelEngine engine, PlayerId owner, IReadOnlyList<DuelCard> cards, TerrainSlot? slot = null)
    {
        if (cards.Count == 0) return;
        if (PassiveCardHelper.IsDiscardRedirectedToBanish(engine.State, slot))
        {
            SendToBanish(engine, owner, cards);
        }
        else
        {
            engine.State.AddCardsToDiscard(owner, cards);
            engine.Emit(new CardsDiscardedEvent(owner, cards));
        }
    }

    public static void SendToBanish(DuelEngine engine, PlayerId owner, IReadOnlyList<DuelCard> cards)
    {
        if (cards.Count == 0) return;
        var state = engine.State;
        state.AddCardsToBanish(owner, cards);
        engine.Emit(new CardsBanishedEvent(owner, cards));

        var pState = PassiveCardHelper.GetState(state);
        if (pState.BanishDepth > 3 || state.IsGameOver) return;
        pState.BanishDepth++;
        try
        {
            // passive_terrain_on_banish_draw_1 (#500021 POOLROOMS)
            if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_on_banish_draw_1"))
            {
                for (int i = 0; i < cards.Count && !state.IsGameOver; i++)
                {
                    engine.DrawCards(owner, 1);
                }
            }

            for (int i = 0; i < cards.Count && !state.IsGameOver; i++)
            {
                var c = cards[i];
                if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_banished_draw_2"))
                {
                    engine.Emit(new EffectTriggeredEvent(owner, "passive_on_banished_draw_2", c, -1));
                    engine.DrawCards(owner, 2);
                }
                if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(c, "passive_on_banished_shuffle_discard_3_to_deck"))
                {
                    var disc = state.GetDiscard(owner);
                    int take = Math.Min(3, disc.Count);
                    if (take > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, owner))
                    {
                        for (int k = 0; k < take; k++)
                        {
                            var top = disc[disc.Count - 1];
                            state.RemoveCardFromDiscard(owner, top);
                            state.AddCardToBottomDeck(owner, top);
                        }
                        engine.Emit(new EffectTriggeredEvent(owner, "passive_on_banished_shuffle_discard_3_to_deck", c, -1));
                    }
                }
                if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(c, "passive_on_banished_return_bottom_ban_3_to_deck"))
                {
                    var banish = PassiveCardHelper.GetMutableBanish(state, owner);
                    if (banish != null && banish.Count > 0)
                    {
                        int take = Math.Min(3, banish.Count);
                        for (int k = 0; k < take; k++)
                        {
                            var bottom = banish[0];
                            banish.RemoveAt(0);
                            PassiveCardHelper.PutCardOnTopOfDeck(state, owner, bottom);
                        }
                        engine.Emit(new EffectTriggeredEvent(owner, "passive_on_banished_return_bottom_ban_3_to_deck", c, -1));
                    }
                }
            }
        }
        finally
        {
            pState.BanishDepth--;
        }
    }

    public static bool CanDrawCards(DuelEngine engine, PlayerId player)
    {
        var state = engine.State;
        if (state.GetHand(player).Count >= 3 &&
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_no_draw_if_hand_ge_3"))
        {
            return false;
        }
        return true;
    }

    public static int AdjustDrawCount(DuelEngine engine, PlayerId player, int requestedCount)
    {
        var state = engine.State;
        int count = requestedCount;
        var pState = PassiveCardHelper.GetState(state);

        // passive_terrain_draw_until_5 (#100009)
        if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_draw_until_5"))
        {
            int deficit = 5 - state.GetHand(player).Count;
            if (deficit > count) count = deficit;
        }

        // passive_opp_draw_plus_1_discard_1 (#100135)
        if (pState.DrawDepth == 0 &&
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_opp_draw_plus_1_discard_1", player.Opponent()))
        {
            count += 1;
        }

        return Math.Max(0, count);
    }

    public static void OnAfterDrawCards(DuelEngine engine, PlayerId player, IReadOnlyList<DuelCard> drawn, bool hadOppPlus1Discard1)
    {
        if (drawn.Count == 0 || engine.State.IsGameOver) return;
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);

        if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_reveal_drawn_cards") ||
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_opp_reveal_drawn", player.Opponent()))
        {
            engine.Emit(new CardsRevealedEvent(player, drawn));
        }

        if (hadOppPlus1Discard1 && pState.DrawDepth <= 1)
        {
            pState.DrawDepth++;
            try
            {
                engine.DiscardFromHand(player, 1);
            }
            finally
            {
                pState.DrawDepth--;
            }
        }

        // passive_on_effect_draw_guess_name_draw_1 (#400026 FANFICTION)
        if (pState.DrawDepth > 0 && !state.IsGameOver &&
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_on_effect_draw_guess_name_draw_1") &&
            pState.UsedOncePerTurn.Add($"fanfic_{player}_{pState.DrawDepth}"))
        {
            var hand = state.GetHand(player);
            if (hand.Count > 0 && string.Equals(drawn[0].Name, hand[0].Name, StringComparison.OrdinalIgnoreCase))
            {
                engine.DrawCards(player, 1);
            }
        }

        // passive_draw_may_take_top_discard (#300095 COMPAGNON D'AVENTURIÈRE)
        if (!state.IsGameOver && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_draw_may_take_top_discard", player))
        {
            engine.Emit(new EffectTriggeredEvent(player, "passive_draw_may_take_top_discard", drawn[0], -1));
        }

        EnforceHandPassives(engine, player);
    }

    public static int AdjustMillCount(DuelEngine engine, int count)
    {
        int minus = PassiveCardHelper.CountBoardPassives(engine.State, "passive_mill_minus_1");
        return Math.Max(0, count - minus);
    }

    public static void OnAfterMilledCards(DuelEngine engine, PlayerId player, IReadOnlyList<DuelCard> milled)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (milled.Count == 0 || pState.MillDepth > 2 || state.IsGameOver) return;
        pState.MillDepth++;
        try
        {
            for (int i = 0; i < milled.Count && !state.IsGameOver; i++)
            {
                var c = milled[i];
                if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_mill_banish_opp_mill_2"))
                {
                    state.RemoveCardFromDiscard(player, c);
                    SendToBanish(engine, player, new[] { c });
                    engine.Emit(new EffectTriggeredEvent(player, "passive_on_mill_banish_opp_mill_2", c, -1));
                    engine.MillCards(player.Opponent(), 2);
                }
                else if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_milled_banish_draw_2"))
                {
                    state.RemoveCardFromDiscard(player, c);
                    SendToBanish(engine, player, new[] { c });
                    engine.Emit(new EffectTriggeredEvent(player, "passive_on_milled_banish_draw_2", c, -1));
                    engine.DrawCards(player, 2);
                }
            }
        }
        finally
        {
            pState.MillDepth--;
        }
    }

    public static bool TryRedirectHandDiscardToMill(DuelEngine engine, PlayerId player, int count)
    {
        if (PassiveCardHelper.BoardHasCardWithPassive(engine.State, "passive_terrain_discard_hand_mills_instead"))
        {
            engine.MillCards(player, count);
            return true;
        }
        return false;
    }

    public static int AdjustHandDiscardCount(DuelEngine engine, PlayerId player, int count)
    {
        var state = engine.State;
        int adjusted = count;
        if (adjusted == 2 && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_reduce_discard_2_to_1", player))
        {
            adjusted = 1;
        }
        if (adjusted >= 2 && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_opp_discard_plus_1_if_ge_2", player.Opponent()))
        {
            adjusted += 1;
        }
        return adjusted;
    }

    public static int SelectHandDiscardIndex(DuelEngine engine, PlayerId player, IRandom rng)
    {
        var hand = engine.State.GetHand(player);
        if (hand.Count <= 1) return 0;
        if (PassiveCardHelper.BoardHasCardWithPassive(engine.State, "passive_opp_discard_random", player.Opponent()))
        {
            return rng.Next(hand.Count);
        }
        return 0;
    }

    public static void OnAfterHandDiscarded(DuelEngine engine, PlayerId player, IReadOnlyList<DuelCard> discarded)
    {
        if (discarded.Count == 0 || engine.State.IsGameOver) return;
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (pState.DiscardDepth > 2) return;
        pState.DiscardDepth++;
        try
        {
            for (int i = 0; i < discarded.Count && !state.IsGameOver; i++)
            {
                var c = discarded[i];
                if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_discard_from_hand_draw_2"))
                {
                    engine.Emit(new EffectTriggeredEvent(player, "passive_on_discard_from_hand_draw_2", c, -1));
                    engine.DrawCards(player, 2);
                }
                else if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_discard_from_hand_banish_score"))
                {
                    state.RemoveCardFromDiscard(player, c);
                    SendToBanish(engine, player, new[] { c });
                    int activeSlot = FindFirstActiveScoreableSlot(engine, player);
                    if (activeSlot >= 0)
                    {
                        engine.Emit(new EffectTriggeredEvent(player, "passive_on_discard_from_hand_banish_score", c, activeSlot));
                        engine.ResolveScore(activeSlot, player);
                    }
                }
            }

            // passive_sacrifice_to_recover_discarded_hand (#200079)
            if (!state.IsGameOver && state.ActivePlayer != player)
            {
                for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
                {
                    var sl = state.Slots[s];
                    var chars = new List<DuelCard>(sl.GetCharacters(player));
                    for (int i = 0; i < chars.Count; i++)
                    {
                        if (PassiveCardHelper.CardHasPassive(state, sl, player, chars[i], "passive_sacrifice_to_recover_discarded_hand"))
                        {
                            if (DiscardCharacterFromPlay(engine, player, sl, s, chars[i], byEffect: false))
                            {
                                for (int k = 0; k < discarded.Count; k++)
                                {
                                    if (PassiveCardHelper.ContainsCard(state.GetDiscard(player), discarded[k]))
                                    {
                                        state.RemoveCardFromDiscard(player, discarded[k]);
                                        state.AddCardToHand(player, discarded[k]);
                                    }
                                }
                                engine.Emit(new EffectTriggeredEvent(player, "passive_sacrifice_to_recover_discarded_hand", chars[i], s));
                                break;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            pState.DiscardDepth--;
        }
    }

    public static void OnSubAccepted(DuelEngine engine, PlayerId reactingPlayer)
    {
        var state = engine.State;
        if (state.IsGameOver) return;

        // passive_terrain_on_sub_take_discard_2 (#200006)
        if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_on_sub_take_discard_2"))
        {
            foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                var disc = state.GetDiscard(side);
                int take = Math.Min(2, disc.Count);
                for (int k = 0; k < take && PassiveCardHelper.CanCardLeaveDiscard(state, side); k++)
                {
                    var top = disc[disc.Count - 1];
                    state.RemoveCardFromDiscard(side, top);
                    state.AddCardToHand(side, top);
                }
            }
        }

        // passive_on_sub_played_discard_to_bottom_deck_1 (#400122 BIBI) & passive_sub_cost_minus_1 (#300037)
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var sl = state.Slots[s];
            foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                var chars = sl.GetCharacters(side);
                for (int i = 0; i < chars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, sl, side, chars[i], "passive_on_sub_played_discard_to_bottom_deck_1"))
                    {
                        var disc = state.GetDiscard(side);
                        if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, side))
                        {
                            var top = disc[disc.Count - 1];
                            state.RemoveCardFromDiscard(side, top);
                            state.AddCardToBottomDeck(side, top);
                            engine.Emit(new EffectTriggeredEvent(side, "passive_on_sub_played_discard_to_bottom_deck_1", chars[i], s));
                        }
                    }
                    if (side == reactingPlayer && PassiveCardHelper.CardHasPassive(state, sl, side, chars[i], "passive_sub_cost_minus_1"))
                    {
                        engine.Emit(new EffectTriggeredEvent(side, "passive_sub_cost_minus_1", chars[i], s));
                    }
                }
            }
        }
    }

    public static void EnforceHandPassives(DuelEngine engine, PlayerId player)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (state.IsGameOver || pState.DrawDepth > 2) return;
        pState.DrawDepth++;
        try
        {
            var hand = state.GetHand(player);
            if (hand.Count == 1 && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_if_hand_1_draw_2"))
            {
                engine.DrawCards(player, 2);
            }
            if (!state.IsGameOver && state.GetHand(player).Count < 3 &&
                PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_draw_if_hand_lt_3"))
            {
                engine.DrawCards(player, 1);
            }
            if (!state.IsGameOver)
            {
                int limit = int.MaxValue;
                if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_hand_limit_5") ||
                    PassiveCardHelper.BoardHasCardWithPassive(state, "passive_cap_hand_5"))
                {
                    limit = Math.Min(limit, 5);
                }
                if (PassiveCardHelper.BoardHasCardWithPassive(state, "passive_opp_max_hand_4", player.Opponent()))
                {
                    limit = Math.Min(limit, 4);
                }
                int excess = state.GetHand(player).Count - limit;
                if (excess > 0)
                {
                    engine.DiscardFromHand(player, excess);
                }
            }
        }
        finally
        {
            pState.DrawDepth--;
        }
    }

    public static void ExecuteStartTurnPassives(DuelEngine engine, IRandom rng)
    {
        var state = engine.State;
        var active = state.ActivePlayer;
        var opp = active.Opponent();

        // Terrain & Board StartTurn passives
        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];
            if (sl.Card != null)
            {
                if (PassiveCardHelper.RawCardHasEffect(sl.Card, "passive_start_turn_reveal_hand_3"))
                {
                    var hand = state.GetHand(active);
                    int revCount = Math.Min(3, hand.Count);
                    if (revCount > 0)
                    {
                        var rev = new List<DuelCard>(revCount);
                        for (int k = 0; k < revCount; k++) rev.Add(hand[k]);
                        engine.Emit(new CardsRevealedEvent(active, rev));
                    }
                }
                if (PassiveCardHelper.RawCardHasEffect(sl.Card, "passive_terrain_stillwater"))
                {
                    var milled = engine.MillCards(active, 1);
                    if (!state.IsGameOver && milled.Count > 0)
                    {
                        int targetForce = milled[0].Force;
                        for (int ss = 0; ss < state.Slots.Count && !state.IsGameOver; ss++)
                        {
                            var tsl = state.Slots[ss];
                            foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
                            {
                                var list = new List<DuelCard>(tsl.GetCharacters(side));
                                for (int k = 0; k < list.Count; k++)
                                {
                                    if (list[k].Force == targetForce)
                                    {
                                        DiscardCharacterFromPlay(engine, side, tsl, ss, list[k], byEffect: true);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (state.IsGameOver) return;

            var activeChars = new List<DuelCard>(sl.GetCharacters(active));
            for (int i = 0; i < activeChars.Count && !state.IsGameOver; i++)
            {
                var c = activeChars[i];
                if (PassiveCardHelper.CardHasPassive(state, sl, active, c, "passive_start_turn_take_discard_1"))
                {
                    var disc = state.GetDiscard(active);
                    if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, active))
                    {
                        var top = disc[disc.Count - 1];
                        state.RemoveCardFromDiscard(active, top);
                        state.AddCardToHand(active, top);
                        engine.Emit(new EffectTriggeredEvent(active, "passive_start_turn_take_discard_1", c, s));
                    }
                }

                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, active, c, "passive_start_turn_both_mill_1"))
                {
                    engine.Emit(new EffectTriggeredEvent(active, "passive_start_turn_both_mill_1", c, s));
                    engine.MillCards(PlayerId.Player1, 1);
                    if (!state.IsGameOver) engine.MillCards(PlayerId.Player2, 1);
                }

                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, active, c, "passive_start_turn_discard_random_cost_3"))
                {
                    var cost3Targets = new List<(TerrainSlot Slot, int SlotIdx, PlayerId Owner, DuelCard Card)>();
                    for (int ss = 0; ss < state.Slots.Count; ss++)
                    {
                        var tsl = state.Slots[ss];
                        foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
                        {
                            var lst = tsl.GetCharacters(side);
                            for (int k = 0; k < lst.Count; k++)
                            {
                                if (lst[k].Cost == 3) cost3Targets.Add((tsl, ss, side, lst[k]));
                            }
                        }
                    }
                    if (cost3Targets.Count > 0)
                    {
                        var pick = cost3Targets[rng.Next(cost3Targets.Count)];
                        engine.Emit(new EffectTriggeredEvent(active, "passive_start_turn_discard_random_cost_3", c, s));
                        DiscardCharacterFromPlay(engine, pick.Owner, pick.Slot, pick.SlotIdx, pick.Card, byEffect: true);
                    }
                }
            }

            // Opponent DUO check for Martine & Jeanine (set CannotScoreThisTurnPlayer)
            var oppChars = new List<DuelCard>(sl.GetCharacters(opp));
            for (int i = 0; i < oppChars.Count && !state.IsGameOver; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, sl, opp, oppChars[i], "duo_martine_jeanine"))
                {
                    PassiveCardHelper.GetState(state).CannotScoreThisTurnPlayer = opp;
                }
            }
        }

        EnforceHandPassives(engine, active);
        EnforceHandPassives(engine, opp);
        EnforceBoardStatePassives(engine);
    }

    public static void ExecuteEndTurnPassives(DuelEngine engine, PlayerId active)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        var opp = active.Opponent();

        // passive_terrain_play_terrain_or_discard (#100006)
        if (!state.IsGameOver && !pState.TerrainPlayedThisTurn &&
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_terrain_play_terrain_or_discard") &&
            state.GetHand(active).Count > 0)
        {
            engine.DiscardFromHand(active, 1);
        }

        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];

            // passive_terrain_must_play_second_or_discard (#200024)
            if (PassiveCardHelper.RawCardHasEffect(sl.Card, "passive_terrain_must_play_second_or_discard"))
            {
                var playedHere = pState.GetPlayedOnSlotThisTurn(active, s);
                if (playedHere.Count == 1 && PassiveCardHelper.ContainsCard(sl.GetCharacters(active), playedHere[0]))
                {
                    DiscardCharacterFromPlay(engine, active, sl, s, playedHere[0], byEffect: true);
                }
            }

            // passive_end_turn_discard_opp_char (#300007 BATTLEGROUND)
            if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(sl.Card, "passive_end_turn_discard_opp_char"))
            {
                var oppChars = sl.GetCharacters(opp);
                if (oppChars.Count > 0)
                {
                    DiscardCharacterFromPlay(engine, opp, sl, s, oppChars[0], byEffect: true);
                }
            }

            // passive_end_opp_turn_discard_if_lt_3_chars (#300086 SCP-173)
            if (!state.IsGameOver && sl.GetCharacters(active).Count > 0 && sl.GetCharacters(active).Count < 3 &&
                PassiveCardHelper.BoardHasCardWithPassive(state, "passive_end_opp_turn_discard_if_lt_3_chars", opp))
            {
                var toDisc = new List<DuelCard>(sl.GetCharacters(active));
                for (int i = 0; i < toDisc.Count; i++)
                {
                    DiscardCharacterFromPlay(engine, active, sl, s, toDisc[i], byEffect: true);
                }
            }

            // passive_end_turn_give_control_to_opp (#500121 ROBOT AGAÇANT / #300085 SCP-106)
            if (!state.IsGameOver)
            {
                var activeChars = new List<DuelCard>(sl.GetCharacters(active));
                for (int i = 0; i < activeChars.Count; i++)
                {
                    var ch = activeChars[i];
                    if (PassiveCardHelper.CardHasPassive(state, sl, active, ch, "passive_end_turn_give_control_to_opp") &&
                        sl.RemoveCharacter(active, ch))
                    {
                        sl.AddCharacter(opp, ch);
                        engine.Emit(new EffectTriggeredEvent(active, "passive_end_turn_give_control_to_opp", ch, s));
                    }
                }
            }

            // passive_terrain_banish_after_3_turns (#500017 MIRAGE)
            if (!state.IsGameOver && sl.Card != null && sl.PlacedOnTurn.HasValue &&
                PassiveCardHelper.RawCardHasEffect(sl.Card, "passive_terrain_banish_after_3_turns") &&
                state.TurnNumber - sl.PlacedOnTurn.Value >= 5)
            {
                var tCard = sl.Card;
                var placer = sl.PlacedBy ?? active;
                var c1 = new List<DuelCard>(sl.CharactersP1);
                var c2 = new List<DuelCard>(sl.CharactersP2);
                state.ClearSlot(s);
                SendCardsToDiscardOrBanish(engine, PlayerId.Player1, c1, sl);
                SendCardsToDiscardOrBanish(engine, PlayerId.Player2, c2, sl);
                SendToBanish(engine, placer, new[] { tCard });
                engine.Emit(new TerrainClearedEvent(s, tCard, c1, c2));
            }
        }

        EnforceHandPassives(engine, active);
        EnforceHandPassives(engine, opp);
        EnforceBoardStatePassives(engine);
    }

    public static void EnforceBoardStatePassives(DuelEngine engine)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        if (state.IsGameOver || pState.ForceZeroCheckDepth > 2) return;
        pState.ForceZeroCheckDepth++;
        try
        {
            bool discardZero = PassiveCardHelper.BoardHasCardWithPassive(state, "passive_discard_force_zero_chars");

            for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
            {
                var sl = state.Slots[s];
                foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
                {
                    // passive_if_only_2_chars_on_row_discard_other (#400120 TRAÎTRE)
                    var chars = new List<DuelCard>(sl.GetCharacters(side));
                    if (chars.Count == 2)
                    {
                        for (int i = 0; i < chars.Count; i++)
                        {
                            if (PassiveCardHelper.CardHasPassive(state, sl, side, chars[i], "passive_if_only_2_chars_on_row_discard_other"))
                            {
                                var other = chars[1 - i];
                                DiscardCharacterFromPlay(engine, side, sl, s, other, byEffect: true);
                                break;
                            }
                        }
                    }

                    // passive_minus_1_per_laink_discard_if_alone (#400115) / terracid (#400116)
                    chars = new List<DuelCard>(sl.GetCharacters(side));
                    for (int i = 0; i < chars.Count; i++)
                    {
                        var ch = chars[i];
                        if (PassiveCardHelper.CardHasPassive(state, sl, side, ch, "passive_minus_1_per_laink_discard_if_alone") &&
                            PassiveCardHelper.CountCharsInPlay(state, null, null, "Laink") <= 1)
                        {
                            DiscardCharacterFromPlay(engine, side, sl, s, ch, byEffect: true);
                        }
                        else if (PassiveCardHelper.CardHasPassive(state, sl, side, ch, "passive_minus_1_per_terracid_discard_if_alone") &&
                                 PassiveCardHelper.CountCharsInPlay(state, null, null, "Terracid") <= 1)
                        {
                            DiscardCharacterFromPlay(engine, side, sl, s, ch, byEffect: true);
                        }
                    }

                    // passive_discard_force_zero_chars (#300030 CIBLES)
                    if (discardZero)
                    {
                        chars = new List<DuelCard>(sl.GetCharacters(side));
                        for (int i = 0; i < chars.Count; i++)
                        {
                            if (sl.GetCharacterEffectiveForce(side, i, state, forScoring: false) <= 0)
                            {
                                DiscardCharacterFromPlay(engine, side, sl, s, chars[i], byEffect: true);
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            pState.ForceZeroCheckDepth--;
        }
    }

    public static bool CanScoreSlot(DuelEngine engine, PlayerId initiator, TerrainSlot slot, int slotIndex, bool isAutoScore)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);

        if (pState.CannotScoreThisTurnPlayer == initiator) return false;

        if (pState.LockedTerrainsUntilTurn.TryGetValue(slotIndex, out int unlockTurn) && state.TurnNumber <= unlockTurn)
        {
            return false;
        }

        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_score_only_if_alone") &&
            state.TerrainsInPlayCount > 1)
        {
            return false;
        }

        if (!isAutoScore &&
            (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_autoscore_only") ||
             PassiveCardHelper.HasSlotEffectAny(slot, "passive_only_autoscore_can_score_terrain")))
        {
            return false;
        }

        if (isAutoScore && PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_terrain_autoscore_min_5_chars") &&
            slot.GetCharacters(initiator).Count < 5)
        {
            return false;
        }

        if (PassiveCardHelper.HasSlotEffectAny(slot, "passive_forbid_score_if_played_this_turn"))
        {
            var chars = slot.GetCharacters(initiator);
            for (int i = 0; i < chars.Count; i++)
            {
                if (pState.PlayedOnTurnByCardId.TryGetValue(chars[i].Id, out int t) && t == state.TurnNumber)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool ShouldBlockDefenderSubOnScore(DuelEngine engine, TerrainSlot slot, bool isAutoScore)
    {
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_immune_to_sub")) return true;
        if (isAutoScore && PassiveCardHelper.BoardHasCardWithPassive(engine.State, "passive_opp_no_sub_on_autoscore")) return true;
        if (!isAutoScore && PassiveCardHelper.BoardHasCardWithPassive(engine.State, "passive_opp_no_sub_on_effect_score")) return true;
        return false;
    }

    private static int FindFirstActiveScoreableSlot(DuelEngine engine, PlayerId player)
    {
        for (int s = 0; s < engine.State.Slots.Count; s++)
        {
            var sl = engine.State.Slots[s];
            if (!sl.IsEmpty && sl.IsActive(engine.State.TurnNumber) && CanScoreSlot(engine, player, sl, s, isAutoScore: false))
            {
                return s;
            }
        }
        return -1;
    }

    public static void PlayFreeCharacterFromHand(DuelEngine engine, PlayerId player, int minCost = 0, int slotIndex = -1)
    {
        var state = engine.State;
        var hand = state.GetHand(player);
        DuelCard? pick = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Character && hand[i].Cost >= minCost)
            {
                pick = hand[i];
                break;
            }
        }
        if (pick == null) return;

        int targetSlot = slotIndex;
        if (targetSlot < 0 || targetSlot >= state.Slots.Count || state.Slots[targetSlot].IsEmpty)
        {
            targetSlot = -1;
            for (int s = 0; s < state.Slots.Count; s++)
            {
                if (!state.Slots[s].IsEmpty) { targetSlot = s; break; }
            }
        }
        if (targetSlot < 0) return;

        if (state.RemoveCardFromHand(player, pick))
        {
            state.AddCharacterToSlot(targetSlot, player, pick);
            PassiveCardHelper.GetState(state).PlayedOnTurnByCardId[pick.Id] = state.TurnNumber;
            engine.Emit(new CharacterPlayedEvent(player, targetSlot, pick));
        }
    }

    /// <summary>
    /// Exécute une capacité passive activable "une fois par tour" ou à sacrifice depuis le jeu ou la main.
    /// </summary>
    public static DuelActionResult ActivateCardPassive(DuelEngine engine, PlayerId player, string cardId, int targetSlotIndex = -1)
    {
        var state = engine.State;
        if (state.TurnNumber == 0 || state.IsGameOver)
            return DuelActionResult.Refused("Game is not active.");

        var pState = PassiveCardHelper.GetState(state);

        // Search in hand first (e.g. Victime #500076)
        var hand = state.GetHand(player);
        for (int i = 0; i < hand.Count; i++)
        {
            var hc = hand[i];
            if (!string.Equals(hc.Id, cardId, StringComparison.Ordinal)) continue;
            if (PassiveCardHelper.RawCardHasEffect(hc, "passive_banish_from_hand_free_play"))
            {
                state.RemoveCardFromHand(player, hc);
                SendToBanish(engine, player, new[] { hc });
                pState.BonusCharPlaysThisTurn += 1;
                engine.Emit(new EffectTriggeredEvent(player, "passive_banish_from_hand_free_play", hc, targetSlotIndex));
                return DuelActionResult.Ok();
            }
        }

        // Search in play
        TerrainSlot? foundSlot = null;
        int foundSlotIdx = -1;
        DuelCard? foundCard = null;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var sl = state.Slots[s];
            var chars = sl.GetCharacters(player);
            for (int i = 0; i < chars.Count; i++)
            {
                if (string.Equals(chars[i].Id, cardId, StringComparison.Ordinal))
                {
                    foundSlot = sl;
                    foundSlotIdx = s;
                    foundCard = chars[i];
                    break;
                }
            }
            if (foundCard != null) break;
        }

        if (foundCard == null || foundSlot == null)
            return DuelActionResult.Refused("Card not found in play or activatable in hand.");

        // Sacrifice / Once-per-turn handlers
        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_sacrifice_take_top_discard_3"))
        {
            if (DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                var disc = state.GetDiscard(player);
                int take = Math.Min(3, disc.Count);
                for (int k = 0; k < take && PassiveCardHelper.CanCardLeaveDiscard(state, player); k++)
                {
                    var top = disc[disc.Count - 1];
                    state.RemoveCardFromDiscard(player, top);
                    state.AddCardToHand(player, top);
                }
                engine.Emit(new EffectTriggeredEvent(player, "passive_sacrifice_take_top_discard_3", foundCard, foundSlotIdx));
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_sacrifice_boost_other_3_turn"))
        {
            DuelCard? ally = null;
            for (int i = 0; i < foundSlot.GetCharacters(player).Count; i++)
            {
                var c = foundSlot.GetCharacters(player)[i];
                if (!ReferenceEquals(c, foundCard)) { ally = c; break; }
            }
            if (ally != null && DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                pState.TempForceByCardId[ally.Id] = (pState.TempForceByCardId.TryGetValue(ally.Id, out int cur) ? cur : 0) + 3;
                engine.Emit(new EffectTriggeredEvent(player, "passive_sacrifice_boost_other_3_turn", foundCard, foundSlotIdx));
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_sacrifice_to_score_1") ||
            PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_sacrifice_to_score_up_to_2"))
        {
            int sIdx = targetSlotIndex >= 0 ? targetSlotIndex : foundSlotIdx;
            if (sIdx >= 0 && sIdx < state.Slots.Count && state.Slots[sIdx].IsActive(state.TurnNumber) &&
                DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                engine.ResolveScore(sIdx, player);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_banish_self_to_score_if_not_played_this_turn"))
        {
            bool playedThisTurn = pState.PlayedOnTurnByCardId.TryGetValue(foundCard.Id, out int pt) && pt == state.TurnNumber;
            if (!playedThisTurn && foundSlot.IsActive(state.TurnNumber) && foundSlot.RemoveCharacter(player, foundCard))
            {
                SendToBanish(engine, player, new[] { foundCard });
                engine.ResolveScore(foundSlotIdx, player);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_discard_self_play_free_char"))
        {
            if (DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                PlayFreeCharacterFromHand(engine, player, 0, targetSlotIndex >= 0 ? targetSlotIndex : foundSlotIdx);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_discard_reduce_both_scores_1"))
        {
            bool playedThisTurn = pState.PlayedOnTurnByCardId.TryGetValue(foundCard.Id, out int pt) && pt == state.TurnNumber;
            if (!playedThisTurn && DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                if (state.GetScore(PlayerId.Player1) > 0) state.AddScore(PlayerId.Player1, -1);
                if (state.GetScore(PlayerId.Player2) > 0) state.AddScore(PlayerId.Player2, -1);
                engine.Emit(new EffectTriggeredEvent(player, "passive_discard_reduce_both_scores_1", foundCard, foundSlotIdx));
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_own_turn_discard_self_boost_2") &&
            state.ActivePlayer == player)
        {
            DuelCard? target = null;
            for (int i = 0; i < foundSlot.GetCharacters(player).Count; i++)
            {
                var c = foundSlot.GetCharacters(player)[i];
                if (!ReferenceEquals(c, foundCard)) { target = c; break; }
            }
            if (target != null && DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                pState.TempForceByCardId[target.Id] = (pState.TempForceByCardId.TryGetValue(target.Id, out int cur) ? cur : 0) + 2;
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_opp_turn_discard_self_debuff_2") &&
            state.ActivePlayer != player)
        {
            var oppChars = foundSlot.GetCharacters(player.Opponent());
            if (oppChars.Count > 0 && DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                var t = oppChars[0];
                pState.TempForceByCardId[t.Id] = (pState.TempForceByCardId.TryGetValue(t.Id, out int cur) ? cur : 0) - 2;
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_turn_discard_2_double_force") &&
            state.GetHand(player).Count >= 2 && pState.UsedOncePerTurn.Add($"dbl_{foundCard.Id}"))
        {
            engine.DiscardFromHand(player, 2);
            pState.DoubledThisTurnCardIds.Add(foundCard.Id);
            engine.Emit(new EffectTriggeredEvent(player, "passive_once_turn_discard_2_double_force", foundCard, foundSlotIdx));
            return DuelActionResult.Ok();
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_turn_if_fewer_won_terrains_score") &&
            state.ActivePlayer == player && state.GetScore(player) < state.GetScore(player.Opponent()) &&
            foundSlot.IsActive(state.TurnNumber) && pState.UsedOncePerTurn.Add($"fewer_score_{foundCard.Id}"))
        {
            engine.ResolveScore(foundSlotIdx, player);
            return DuelActionResult.Ok();
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_per_turn_hand_to_bottom_deck") &&
            state.GetHand(player).Count > 0 && pState.UsedOncePerTurn.Add($"hand_bot_{foundCard.Id}"))
        {
            var c = state.GetHand(player)[0];
            if (state.RemoveCardFromHand(player, c)) state.AddCardToBottomDeck(player, c);
            return DuelActionResult.Ok();
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_per_turn_if_no_facedown_discard_to_bottom_deck") &&
            PassiveCardHelper.GetTotalFacedownCount(state, foundCard.Id) == 0 &&
            state.GetDiscard(player).Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, player) &&
            pState.UsedOncePerTurn.Add($"nofd_bot_{foundCard.Id}"))
        {
            var disc = state.GetDiscard(player);
            var top = disc[disc.Count - 1];
            state.RemoveCardFromDiscard(player, top);
            state.AddCardToBottomDeck(player, top);
            return DuelActionResult.Ok();
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_turn_spend_score_banish_char") &&
            state.GetScore(player) > 0 && pState.UsedOncePerTurn.Add($"spend_ban_{foundCard.Id}"))
        {
            var opp = player.Opponent();
            var oppChars = foundSlot.GetCharacters(opp);
            if (oppChars.Count > 0)
            {
                var target = oppChars[0];
                if (foundSlot.RemoveCharacter(opp, target))
                {
                    state.AddScore(player, -1);
                    SendToBanish(engine, opp, new[] { target });
                    return DuelActionResult.Ok();
                }
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_swap_with_hand_char_and_discard"))
        {
            bool playedThisTurn = pState.PlayedOnTurnByCardId.TryGetValue(foundCard.Id, out int pt) && pt == state.TurnNumber;
            DuelCard? handChar = null;
            for (int i = 0; i < state.GetHand(player).Count; i++)
            {
                if (state.GetHand(player)[i].Kind == CardKind.Character) { handChar = state.GetHand(player)[i]; break; }
            }
            if (!playedThisTurn && handChar != null && foundSlot.RemoveCharacter(player, foundCard) && state.RemoveCardFromHand(player, handChar))
            {
                foundSlot.AddCharacter(player, handChar);
                state.AddCardToHand(player, foundCard);
                pState.PlayedOnTurnByCardId[handChar.Id] = state.TurnNumber;
                if (state.GetHand(player).Count > 0) engine.DiscardFromHand(player, 1);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_end_draw_sacrifice_draw_2"))
        {
            if (DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                engine.DrawCards(player, 2);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_turn_if_alone_discard_to_bottom_deck") &&
            PassiveCardHelper.CountCharsInPlay(state, player) == 1 &&
            state.GetDiscard(player).Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, player) &&
            pState.UsedOncePerTurn.Add($"alone_bot_{foundCard.Id}"))
        {
            var disc = state.GetDiscard(player);
            var top = disc[disc.Count - 1];
            state.RemoveCardFromDiscard(player, top);
            state.AddCardToBottomDeck(player, top);
            return DuelActionResult.Ok();
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_per_turn_add_bottom_deck_to_hand") &&
            pState.UsedOncePerTurn.Add($"bot_hand_{foundCard.Id}"))
        {
            var bot = state.PopBottomDeckCard(player);
            if (bot != null)
            {
                state.AddCardToHand(player, bot);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_once_per_turn_move_clown") &&
            pState.UsedOncePerTurn.Add($"move_clown_{foundCard.Id}"))
        {
            for (int dest = 0; dest < state.Slots.Count; dest++)
            {
                if (dest != foundSlotIdx && !state.Slots[dest].IsEmpty)
                {
                    return engine.MoveCharacter(player, foundCard.Id, foundSlotIdx, dest);
                }
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "passive_banish_hand_1_search_copy_on_slot") &&
            state.GetHand(player).Count > 0 && pState.UsedOncePerTurn.Add($"barmaid_{foundCard.Id}"))
        {
            var banCard = state.GetHand(player)[0];
            if (state.RemoveCardFromHand(player, banCard))
            {
                SendToBanish(engine, player, new[] { banCard });
                var deck = state.GetDeck(player);
                for (int d = 0; d < deck.Count; d++)
                {
                    if (deck[d].Kind == CardKind.Character)
                    {
                        bool matchesSlot = false;
                        foreach (var sc in foundSlot.GetCharacters(player))
                        {
                            if (!ReferenceEquals(sc, foundCard) && string.Equals(sc.Name, deck[d].Name, StringComparison.OrdinalIgnoreCase))
                            { matchesSlot = true; break; }
                        }
                        if (matchesSlot)
                        {
                            var found = deck[d];
                            state.RemoveCardFromDeck(player, found);
                            state.AddCardToHand(player, found);
                            break;
                        }
                    }
                }
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "duo_flantier") &&
            state.ActivePlayer == player && pState.UsedOncePerTurn.Add($"duo_flantier_act_{foundCard.Id}"))
        {
            if (engine.Effects.TryGetEffect("duo_flantier_active_discard", out var eff) && eff != null)
            {
                eff.Execute(new EffectContext(engine, player, foundCard, foundSlotIdx));
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.CardHasPassive(state, foundSlot, player, foundCard, "duo_aventuriers_manoir") &&
            state.ActivePlayer != player && pState.UsedOncePerTurn.Add($"duo_avent_opp_{foundCard.Id}"))
        {
            if (engine.Effects.TryGetEffect("duo_aventuriers_opp_discard_char", out var eff) && eff != null)
            {
                eff.Execute(new EffectContext(engine, player, foundCard, foundSlotIdx));
                return DuelActionResult.Ok();
            }
        }

        // Terrain-level activatables on foundSlot
        if (PassiveCardHelper.RawCardHasEffect(foundSlot.Card, "passive_terrain_banish_2_hand_extra_play") &&
            state.GetHand(player).Count >= 2 && pState.UsedOncePerTurn.Add($"supm_{player}_{foundSlotIdx}"))
        {
            engine.BanishCardsFromHand(player, 2);
            pState.BonusCharPlaysThisTurn += 1;
            return DuelActionResult.Ok();
        }

        if (PassiveCardHelper.RawCardHasEffect(foundSlot.Card, "passive_once_per_turn_discard_own_play_free") &&
            pState.UsedOncePerTurn.Add($"cann_{player}_{foundSlotIdx}"))
        {
            if (DiscardCharacterFromPlay(engine, player, foundSlot, foundSlotIdx, foundCard, byEffect: false))
            {
                PlayFreeCharacterFromHand(engine, player, 0, foundSlotIdx);
                return DuelActionResult.Ok();
            }
        }

        if (PassiveCardHelper.RawCardHasEffect(foundSlot.Card, "passive_once_per_turn_swap_higher_force_hand") &&
            pState.UsedOncePerTurn.Add($"lab_{player}_{foundSlotIdx}"))
        {
            var handList = state.GetHand(player);
            for (int i = 0; i < handList.Count; i++)
            {
                if (handList[i].Kind == CardKind.Character && handList[i].Force > foundCard.Force)
                {
                    var repl = handList[i];
                    if (foundSlot.RemoveCharacter(player, foundCard) && state.RemoveCardFromHand(player, repl))
                    {
                        foundSlot.AddCharacter(player, repl);
                        state.AddCardToHand(player, foundCard);
                        pState.PlayedOnTurnByCardId[repl.Id] = state.TurnNumber;
                        return DuelActionResult.Ok();
                    }
                }
            }
        }

        return DuelActionResult.Refused("No activatable passive matched on card.");
    }

    public static void OnBeforeResolveScore(DuelEngine engine, int slotIndex, PlayerId attacker, PlayerId defender, out bool cancelled)
    {
        cancelled = false;
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        var slot = state.Slots[slotIndex];

        // passive_on_score_cancel_and_lock_terrain (#500051 TRICKSHOT) when losing defender in AI/P2 mode
        if (defender == PlayerId.Player2)
        {
            int p1F = slot.GetForce(PlayerId.Player1, state, forScoring: true);
            int p2F = slot.GetForce(PlayerId.Player2, state, forScoring: true);
            if (p2F < p1F)
            {
                var defChars = slot.GetCharacters(defender);
                for (int i = 0; i < defChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, slot, defender, defChars[i], "passive_on_score_cancel_and_lock_terrain") &&
                        pState.UsedOncePerTurn.Add($"trickshot_{defChars[i].Id}"))
                    {
                        pState.LockedTerrainsUntilTurn[slotIndex] = state.TurnNumber + 2;
                        engine.Emit(new EffectTriggeredEvent(defender, "passive_on_score_cancel_and_lock_terrain", defChars[i], slotIndex));
                        cancelled = true;
                        return;
                    }
                }
            }
        }

        // passive_on_score_swap_terrain_discard (#500046 GÉODEVINEUR)
        foreach (var side in new[] { attacker, defender })
        {
            var chars = slot.GetCharacters(side);
            for (int i = 0; i < chars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, slot, side, chars[i], "passive_on_score_swap_terrain_discard"))
                {
                    var disc = state.GetDiscard(side);
                    DuelCard? terrInDisc = null;
                    for (int d = disc.Count - 1; d >= 0; d--)
                    {
                        if (disc[d].Kind == CardKind.Terrain) { terrInDisc = disc[d]; break; }
                    }
                    if (terrInDisc != null && slot.Card != null && PassiveCardHelper.CanCardLeaveDiscard(state, side))
                    {
                        var oldTerrain = slot.Card;
                        var oldPlacer = slot.PlacedBy ?? side;
                        var p1Backup = new List<DuelCard>(slot.CharactersP1);
                        var p2Backup = new List<DuelCard>(slot.CharactersP2);
                        state.RemoveCardFromDiscard(side, terrInDisc);
                        state.AddCardToDiscard(oldPlacer, oldTerrain);
                        state.SetSlot(slotIndex, terrInDisc, slot.PlacedOnTurn, side);
                        var newSlot = state.Slots[slotIndex];
                        foreach (var c in p1Backup) newSlot.AddCharacter(PlayerId.Player1, c);
                        foreach (var c in p2Backup) newSlot.AddCharacter(PlayerId.Player2, c);
                        slot = newSlot;
                        engine.Emit(new EffectTriggeredEvent(side, "passive_on_score_swap_terrain_discard", chars[i], slotIndex));
                        break;
                    }
                }
            }
        }

        // passive_on_score_both_discard_1_char (#400013 VAISSEAU DE TRAITRES)
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, "passive_on_score_both_discard_1_char"))
        {
            foreach (var side in new[] { attacker, defender })
            {
                var sideChars = slot.GetCharacters(side);
                if (sideChars.Count > 0)
                {
                    DiscardCharacterFromPlay(engine, side, slot, slotIndex, sideChars[0], byEffect: true);
                }
            }
        }

        // passive_on_score_discard_opp_char (#500077 FRANKENSTEIN)
        foreach (var side in new[] { attacker, defender })
        {
            var sideChars = new List<DuelCard>(slot.GetCharacters(side));
            for (int i = 0; i < sideChars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, slot, side, sideChars[i], "passive_on_score_discard_opp_char"))
                {
                    var opp = side.Opponent();
                    var oppChars = slot.GetCharacters(opp);
                    if (oppChars.Count > 0)
                    {
                        DiscardCharacterFromPlay(engine, opp, slot, slotIndex, oppChars[0], byEffect: true);
                    }
                }
            }
        }

        // duo_flantier (Opp turn at score -> discard a char of Force <= 6)
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var sl = state.Slots[s];
            var defChars = new List<DuelCard>(sl.GetCharacters(defender));
            for (int i = 0; i < defChars.Count; i++)
            {
                if (PassiveCardHelper.CardHasPassive(state, sl, defender, defChars[i], "duo_flantier") &&
                    engine.Effects.TryGetEffect("duo_flantier_opp_discard", out var flEff) && flEff != null)
                {
                    engine.Emit(new EffectTriggeredEvent(defender, "duo_flantier_opp_discard", defChars[i], s));
                    flEff.Execute(new EffectContext(engine, defender, defChars[i], s));
                }
            }
        }

        // passive_on_score_reduce_opp_0_discard_self (#300038 FOOTBALLEUR)
        foreach (var side in new[] { attacker, defender })
        {
            var sideChars = new List<DuelCard>(slot.GetCharacters(side));
            for (int i = 0; i < sideChars.Count; i++)
            {
                var fb = sideChars[i];
                if (PassiveCardHelper.CardHasPassive(state, slot, side, fb, "passive_on_score_reduce_opp_0_discard_self"))
                {
                    var oppChars = slot.GetCharacters(side.Opponent());
                    if (oppChars.Count > 0)
                    {
                        DuelCard strongest = oppChars[0];
                        for (int k = 1; k < oppChars.Count; k++)
                        {
                            if (oppChars[k].Force > strongest.Force) strongest = oppChars[k];
                        }
                        pState.ForceZeroThisScoreCardIds.Add(strongest.Id);
                        engine.Emit(new EffectTriggeredEvent(side, "passive_on_score_reduce_opp_0_discard_self", fb, slotIndex));
                        DiscardCharacterFromPlay(engine, side, slot, slotIndex, fb, byEffect: false);
                    }
                }
            }
        }

        // passive_on_score_add_bottom_deck_force (#400063) & reveal_top_even/odd (#200112 / #200113)
        foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
        {
            var sideChars = slot.GetCharacters(side);
            for (int i = 0; i < sideChars.Count; i++)
            {
                var c = sideChars[i];
                if (PassiveCardHelper.CardHasPassive(state, slot, side, c, "passive_on_score_add_bottom_deck_force"))
                {
                    var deck = state.GetDeck(side);
                    if (deck.Count > 0)
                    {
                        var bot = deck[deck.Count - 1];
                        engine.Emit(new CardsRevealedEvent(side, new[] { bot }));
                        pState.TempForceByCardId[c.Id] = (pState.TempForceByCardId.TryGetValue(c.Id, out int cur) ? cur : 0) + bot.Force;
                    }
                }
                bool isEven = PassiveCardHelper.CardHasPassive(state, slot, side, c, "passive_on_score_reveal_top_even_add_else_sub");
                bool isOdd = PassiveCardHelper.CardHasPassive(state, slot, side, c, "passive_on_score_reveal_top_odd_add_else_sub");
                if (isEven || isOdd)
                {
                    var deck = state.GetDeck(side);
                    if (deck.Count > 0)
                    {
                        var top = deck[0];
                        engine.Emit(new CardsRevealedEvent(side, new[] { top }));
                        bool match = isEven ? (top.Force % 2 == 0) : (Math.Abs(top.Force) % 2 == 1);
                        int delta = match ? top.Force : -top.Force;
                        pState.TempForceByCardId[c.Id] = (pState.TempForceByCardId.TryGetValue(c.Id, out int cur) ? cur : 0) + delta;
                    }
                }
            }
        }
    }

    public static void CleanupSlotOnScore(
        DuelEngine engine,
        int slotIndex,
        TerrainSlot slot,
        DuelCard terrainCard,
        PlayerId terrainPlacedBy,
        PlayerId? winner,
        int forceP1,
        int forceP2)
    {
        var state = engine.State;
        var pState = PassiveCardHelper.GetState(state);
        var charsP1 = new List<DuelCard>(slot.CharactersP1);
        var charsP2 = new List<DuelCard>(slot.CharactersP2);

        bool luckyBounce = winner.HasValue &&
                           PassiveCardHelper.RawCardHasEffect(terrainCard, "passive_win_bounce_if_force_11") &&
                           ((winner.Value == PlayerId.Player1 ? forceP1 : forceP2) == 11);

        // Clear slot first before moving survivors so destination slots remain distinct
        state.ClearSlot(slotIndex);

        void ProcessSide(PlayerId side, List<DuelCard> sideChars)
        {
            var toDiscard = new List<DuelCard>();
            var toBanish = new List<DuelCard>();

            for (int i = 0; i < sideChars.Count; i++)
            {
                var c = sideChars[i];
                if (PassiveCardHelper.TryTakeFacedownUnderCard(state, c.Id, out var facedowns))
                {
                    SendCardsToDiscardOrBanish(engine, side, facedowns, slot);
                }

                if (luckyBounce)
                {
                    state.AddCardToHand(side, c);
                    continue;
                }

                bool isWin = winner.HasValue && winner.Value == side;
                bool isLose = winner.HasValue && winner.Value != side;

                if (isWin && PassiveCardHelper.RawCardHasEffect(c, "passive_on_win_score_bounce_self"))
                {
                    state.AddCardToHand(side, c);
                    continue;
                }
                if (isLose && PassiveCardHelper.RawCardHasEffect(c, "passive_on_lose_score_bounce_self"))
                {
                    state.AddCardToHand(side, c);
                    continue;
                }
                if (isLose && PassiveCardHelper.RawCardHasEffect(c, "passive_on_lose_score_banish_self"))
                {
                    toBanish.Add(c);
                    continue;
                }

                int moveDest = -1;
                if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_score_move_left"))
                {
                    for (int k = slotIndex - 1; k >= 0; k--) if (!state.Slots[k].IsEmpty) { moveDest = k; break; }
                }
                else if (PassiveCardHelper.RawCardHasEffect(c, "passive_on_score_move_right"))
                {
                    for (int k = slotIndex + 1; k < state.Slots.Count; k++) if (!state.Slots[k].IsEmpty) { moveDest = k; break; }
                }
                else if (PassiveCardHelper.RawCardHasEffect(c, "passive_move_on_score_instead_of_discard"))
                {
                    for (int k = 0; k < state.Slots.Count; k++) if (k != slotIndex && !state.Slots[k].IsEmpty) { moveDest = k; break; }
                }

                if (moveDest >= 0)
                {
                    state.Slots[moveDest].AddCharacter(side, c);
                    engine.Emit(new CharacterMovedSlotEvent(side, c, slotIndex, moveDest));
                    continue;
                }

                if (pState.BanishOnLeaveCardIds.Remove(c.Id) ||
                    PassiveCardHelper.IsDiscardRedirectedToBanish(state, slot))
                {
                    toBanish.Add(c);
                }
                else
                {
                    toDiscard.Add(c);
                }
            }

            if (toDiscard.Count > 0)
            {
                state.AddCardsToDiscard(side, toDiscard);
                engine.Emit(new CardsDiscardedEvent(side, toDiscard));
            }
            if (toBanish.Count > 0)
            {
                SendToBanish(engine, side, toBanish);
            }
        }

        ProcessSide(PlayerId.Player1, charsP1);
        ProcessSide(PlayerId.Player2, charsP2);

        if (PassiveCardHelper.RawCardHasEffect(terrainCard, "passive_discard_is_banished") ||
            PassiveCardHelper.BoardHasCardWithPassive(state, "passive_discard_is_banished"))
        {
            SendToBanish(engine, terrainPlacedBy, new[] { terrainCard });
        }
        else
        {
            state.AddCardToDiscard(terrainPlacedBy, terrainCard);
        }

        engine.Emit(new TerrainClearedEvent(slotIndex, terrainCard, charsP1, charsP2));

        // Terrain cleared / discarded passive reactions
        if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(terrainCard, "passive_on_terrain_cleared_draw_3"))
        {
            engine.Emit(new EffectTriggeredEvent(terrainPlacedBy, "passive_on_terrain_cleared_draw_3", terrainCard, slotIndex));
            engine.DrawCards(terrainPlacedBy, 3);
        }

        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];
            foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                var chars = sl.GetCharacters(side);
                for (int i = 0; i < chars.Count && !state.IsGameOver; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, sl, side, chars[i], "passive_on_terrain_discarded_take_bottom_discard"))
                    {
                        var disc = state.GetDiscard(side);
                        if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, side))
                        {
                            var bottom = disc[0];
                            state.RemoveCardFromDiscard(side, bottom);
                            state.AddCardToHand(side, bottom);
                            engine.Emit(new EffectTriggeredEvent(side, "passive_on_terrain_discarded_take_bottom_discard", chars[i], s));
                        }
                    }
                }
            }
        }
    }

    public static void OnAfterResolveScore(
        DuelEngine engine,
        int slotIndex,
        DuelCard terrainCard,
        PlayerId winPlayer,
        PlayerId losePlayer,
        int forceP1,
        int forceP2,
        IReadOnlyList<DuelCard> charsP1Before,
        IReadOnlyList<DuelCard> charsP2Before)
    {
        var state = engine.State;
        if (state.IsGameOver) return;
        var pState = PassiveCardHelper.GetState(state);
        pState.ForceZeroThisScoreCardIds.Clear();

        int winForce = winPlayer == PlayerId.Player1 ? forceP1 : forceP2;
        var winSlotChars = winPlayer == PlayerId.Player1 ? charsP1Before : charsP2Before;
        var loseSlotChars = losePlayer == PlayerId.Player1 ? charsP1Before : charsP2Before;

        // Winner reactions
        for (int i = 0; i < winSlotChars.Count && !state.IsGameOver; i++)
        {
            var wc = winSlotChars[i];
            if (PassiveCardHelper.RawCardHasEffect(wc, "passive_opp_discard_plus_1_on_win"))
            {
                engine.Emit(new EffectTriggeredEvent(winPlayer, "passive_opp_discard_plus_1_on_win", wc, slotIndex));
                engine.DiscardFromHand(losePlayer, 1);
            }
            if (!state.IsGameOver && winForce == 11 && PassiveCardHelper.RawCardHasEffect(wc, "passive_on_score_exact_11_take_discard_2"))
            {
                var disc = state.GetDiscard(winPlayer);
                int take = Math.Min(2, disc.Count);
                for (int k = 0; k < take && PassiveCardHelper.CanCardLeaveDiscard(state, winPlayer); k++)
                {
                    var top = disc[disc.Count - 1];
                    state.RemoveCardFromDiscard(winPlayer, top);
                    state.AddCardToHand(winPlayer, top);
                }
            }
            if (!state.IsGameOver && (forceP1 + forceP2) >= 18 && PassiveCardHelper.RawCardHasEffect(wc, "passive_on_score_total_18_return_discard_5"))
            {
                var disc = state.GetDiscard(winPlayer);
                int take = Math.Min(5, disc.Count);
                for (int k = 0; k < take && PassiveCardHelper.CanCardLeaveDiscard(state, winPlayer); k++)
                {
                    var top = disc[disc.Count - 1];
                    state.RemoveCardFromDiscard(winPlayer, top);
                    state.AddCardToBottomDeck(winPlayer, top);
                }
            }
        }

        // Board-wide winner & loser score reactions
        for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
        {
            var sl = state.Slots[s];
            var wBoard = new List<DuelCard>(sl.GetCharacters(winPlayer));
            for (int i = 0; i < wBoard.Count && !state.IsGameOver; i++)
            {
                var wc = wBoard[i];
                if (PassiveCardHelper.CardHasPassive(state, sl, winPlayer, wc, "passive_opp_discard_plus_1_on_win"))
                {
                    engine.Emit(new EffectTriggeredEvent(winPlayer, "passive_opp_discard_plus_1_on_win", wc, s));
                    engine.DiscardFromHand(losePlayer, 1);
                }
                if (PassiveCardHelper.CardHasPassive(state, sl, winPlayer, wc, "passive_double_force_on_poubelle_win"))
                {
                    pState.DoubledThisTurnCardIds.Add(wc.Id);
                }
                if (state.ActivePlayer == winPlayer && PassiveCardHelper.CardHasPassive(state, sl, winPlayer, wc, "passive_discard_self_if_score_on_turn"))
                {
                    DiscardCharacterFromPlay(engine, winPlayer, sl, s, wc, byEffect: true);
                }
            }

            var lBoard = new List<DuelCard>(sl.GetCharacters(losePlayer));
            for (int i = 0; i < lBoard.Count && !state.IsGameOver; i++)
            {
                var lc = lBoard[i];
                if (PassiveCardHelper.CardHasPassive(state, sl, losePlayer, lc, "passive_on_lose_score_take_discard_1"))
                {
                    var disc = state.GetDiscard(losePlayer);
                    if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, losePlayer))
                    {
                        var top = disc[disc.Count - 1];
                        state.RemoveCardFromDiscard(losePlayer, top);
                        state.AddCardToHand(losePlayer, top);
                    }
                }
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, losePlayer, lc, "passive_on_lose_score_banish_deck_3"))
                {
                    engine.BanishCardsFromDeck(losePlayer, 3);
                }
                if (!state.IsGameOver && PassiveCardHelper.CardHasPassive(state, sl, losePlayer, lc, "passive_on_lose_banish_search_rage"))
                {
                    if (sl.RemoveCharacter(losePlayer, lc))
                    {
                        SendToBanish(engine, losePlayer, new[] { lc });
                        var deck = state.GetDeck(losePlayer);
                        for (int d = 0; d < deck.Count; d++)
                        {
                            if (deck[d].Name.IndexOf("Rage", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var rage = deck[d];
                                state.RemoveCardFromDeck(losePlayer, rage);
                                state.AddCardToHand(losePlayer, rage);
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Also check loser's characters that were ON the scored slot
        for (int i = 0; i < loseSlotChars.Count && !state.IsGameOver; i++)
        {
            var lc = loseSlotChars[i];
            if (PassiveCardHelper.RawCardHasEffect(lc, "passive_on_lose_score_take_discard_1"))
            {
                var disc = state.GetDiscard(losePlayer);
                if (disc.Count > 0 && PassiveCardHelper.CanCardLeaveDiscard(state, losePlayer))
                {
                    var top = disc[disc.Count - 1];
                    state.RemoveCardFromDiscard(losePlayer, top);
                    state.AddCardToHand(losePlayer, top);
                }
            }
            if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(lc, "passive_on_lose_score_banish_deck_3"))
            {
                engine.BanishCardsFromDeck(losePlayer, 3);
            }
            if (!state.IsGameOver && PassiveCardHelper.RawCardHasEffect(lc, "passive_on_lose_opp_banish_top_deck_1"))
            {
                engine.BanishCardsFromDeck(winPlayer, 1);
            }
        }

        // Terrain / global score hooks
        if (!state.IsGameOver &&
            (PassiveCardHelper.RawCardHasEffect(terrainCard, "passive_on_score_play_free_char") ||
             PassiveCardHelper.BoardHasCardWithPassive(state, "passive_on_score_play_free_no_effect", winPlayer)))
        {
            PlayFreeCharacterFromHand(engine, winPlayer, 0);
        }

        // passive_on_score_discard_bounce_last_opp (#500126 ANYME)
        if (!state.IsGameOver && pState.LastPlayedCharByPlayer.TryGetValue(losePlayer, out var lastOpp) && lastOpp != null)
        {
            for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
            {
                var sl = state.Slots[s];
                var wChars = new List<DuelCard>(sl.GetCharacters(winPlayer));
                for (int i = 0; i < wChars.Count; i++)
                {
                    if (PassiveCardHelper.CardHasPassive(state, sl, winPlayer, wChars[i], "passive_on_score_discard_bounce_last_opp") &&
                        winPlayer == PlayerId.Player2 &&
                        DiscardCharacterFromPlay(engine, winPlayer, sl, s, wChars[i], byEffect: false))
                    {
                        for (int ss = 0; ss < state.Slots.Count; ss++)
                        {
                            if (state.Slots[ss].RemoveCharacter(losePlayer, lastOpp))
                            {
                                state.AddCardToHand(losePlayer, lastOpp);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
        if (!state.IsGameOver && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_on_score_banish_top_2"))
        {
            for (int s = 0; s < state.Slots.Count && !state.IsGameOver; s++)
            {
                foreach (var side in new[] { PlayerId.Player1, PlayerId.Player2 })
                {
                    var chars = state.Slots[s].GetCharacters(side);
                    for (int i = 0; i < chars.Count && !state.IsGameOver; i++)
                    {
                        if (PassiveCardHelper.CardHasPassive(state, state.Slots[s], side, chars[i], "passive_on_score_banish_top_2"))
                        {
                            engine.BanishCardsFromDeck(side, 2);
                        }
                    }
                }
            }
        }
    }

    public static void ExecuteScoreWinLoseEffects(
        DuelEngine engine,
        int slotIndex,
        DuelCard terrainCard,
        PlayerId winPlayer,
        PlayerId losePlayer,
        IReadOnlyList<DuelCard> charsP1Before,
        IReadOnlyList<DuelCard> charsP2Before,
        int winRepeat,
        int loseRepeat)
    {
        if (terrainCard.EffectIds == null || terrainCard.EffectIds.Count == 0) return;
        var state = engine.State;

        var winSlotChars = winPlayer == PlayerId.Player1 ? charsP1Before : charsP2Before;
        bool swapWinLose = false;
        for (int i = 0; i < winSlotChars.Count; i++)
        {
            if (PassiveCardHelper.RawCardHasEffect(winSlotChars[i], "passive_swap_win_and_lose_on_win"))
            {
                swapWinLose = true;
                break;
            }
        }
        if (!swapWinLose)
        {
            swapWinLose = PassiveCardHelper.BoardHasCardWithPassive(state, "passive_swap_win_and_lose_on_win", winPlayer);
        }

        PlayerId winTarget = swapWinLose ? losePlayer : winPlayer;
        PlayerId loseTarget = swapWinLose ? winPlayer : losePlayer;

        bool halveWin = PassiveCardHelper.BoardHasCardWithPassive(state, "passive_halve_opp_winning_effect", losePlayer);
        bool halveLose = PassiveCardHelper.BoardHasCardWithPassive(state, "passive_halve_losing_effect", losePlayer);

        if (PassiveCardHelper.RawCardHasEffect(terrainCard, "passive_terrain_double_effects"))
        {
            winRepeat *= 2;
            loseRepeat *= 2;
        }

        bool cancelWin = false;
        bool cancelLose = false;
        for (int s = 0; s < state.Slots.Count; s++)
        {
            var sl = state.Slots[s];
            var loseBoard = new List<DuelCard>(sl.GetCharacters(loseTarget));
            for (int i = 0; i < loseBoard.Count; i++)
            {
                if (!cancelWin && loseTarget == PlayerId.Player2 &&
                    PassiveCardHelper.CardHasPassive(state, sl, loseTarget, loseBoard[i], "passive_discard_cancel_win_effect") &&
                    DiscardCharacterFromPlay(engine, loseTarget, sl, s, loseBoard[i], byEffect: false))
                {
                    cancelWin = true;
                }
                if (!cancelLose && loseTarget == PlayerId.Player2 &&
                    PassiveCardHelper.CardHasPassive(state, sl, loseTarget, loseBoard[i], "passive_discard_cancel_lose_effect") &&
                    DiscardCharacterFromPlay(engine, loseTarget, sl, s, loseBoard[i], byEffect: false))
                {
                    cancelLose = true;
                }
            }
        }

        for (int i = 0; i < terrainCard.EffectIds.Count && !state.IsGameOver; i++)
        {
            string effId = terrainCard.EffectIds[i];
            if (!cancelWin && effId.StartsWith("win_", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteResolvingScoreEffect(engine, effId, winTarget, terrainCard, slotIndex, winRepeat, halveWin);
            }
            else if (!cancelLose && effId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteResolvingScoreEffect(engine, effId, loseTarget, terrainCard, slotIndex, loseRepeat, halveLose);
                if (!state.IsGameOver && PassiveCardHelper.BoardHasCardWithPassive(state, "passive_on_own_losing_effect_mill_2", loseTarget))
                {
                    engine.MillCards(loseTarget, 2);
                }
            }
        }
    }

    private static void ExecuteResolvingScoreEffect(
        DuelEngine engine,
        string effId,
        PlayerId targetPlayer,
        DuelCard terrainCard,
        int slotIndex,
        int repeat,
        bool halve)
    {
        if (halve && TryExecuteHalvedEffect(engine, effId, targetPlayer, terrainCard, slotIndex, repeat))
        {
            return;
        }

        if (engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
        {
            var ctx = new EffectContext(engine, targetPlayer, terrainCard, slotIndex);
            for (int r = 0; r < repeat && !engine.State.IsGameOver; r++)
            {
                engine.Emit(new EffectTriggeredEvent(targetPlayer, effId, terrainCard, slotIndex));
                eff.Execute(ctx);
            }
        }
    }

    private static bool TryExecuteHalvedEffect(
        DuelEngine engine,
        string effId,
        PlayerId targetPlayer,
        DuelCard terrainCard,
        int slotIndex,
        int repeat)
    {
        int lastUnderscore = effId.LastIndexOf('_');
        if (lastUnderscore <= 0 || lastUnderscore >= effId.Length - 1) return false;
        if (!int.TryParse(effId.Substring(lastUnderscore + 1), out int n) || n <= 0) return false;

        int halved = n / 2;
        string halvedId = effId.Substring(0, lastUnderscore + 1) + halved;
        if (halved == 0)
        {
            engine.Emit(new EffectTriggeredEvent(targetPlayer, effId, terrainCard, slotIndex));
            return true;
        }

        if (engine.Effects.TryGetEffect(halvedId, out var halvedEff) && halvedEff != null)
        {
            var ctx = new EffectContext(engine, targetPlayer, terrainCard, slotIndex);
            for (int r = 0; r < repeat && !engine.State.IsGameOver; r++)
            {
                engine.Emit(new EffectTriggeredEvent(targetPlayer, halvedId, terrainCard, slotIndex));
                halvedEff.Execute(ctx);
            }
            return true;
        }
        return false;
    }
}

internal static class PassiveSlotExtensions
{
    public static bool HasSlotEffectAny(this TerrainSlot slot, string target)
    {
        if (PassiveCardHelper.RawCardHasEffect(slot.Card, target)) return true;
        for (int i = 0; i < slot.CharactersP1.Count; i++)
        {
            if (PassiveCardHelper.RawCardHasEffect(slot.CharactersP1[i], target)) return true;
        }
        for (int i = 0; i < slot.CharactersP2.Count; i++)
        {
            if (PassiveCardHelper.RawCardHasEffect(slot.CharactersP2[i], target)) return true;
        }
        return false;
    }

    public static bool FirstPlayUsedSlotsContains(this PassiveState pState, int slotIndex) =>
        pState.FirstPlayFreeUsedSlots.Contains(slotIndex);
}
