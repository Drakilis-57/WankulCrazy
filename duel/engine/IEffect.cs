#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace WankulCrazy.Duel.Engine;

public sealed class EffectContext
{
    public DuelEngine Engine { get; }
    public PlayerId Player { get; }
    public DuelCard SourceCard { get; }
    public int SlotIndex { get; }

    public EffectContext(DuelEngine engine, PlayerId player, DuelCard sourceCard, int slotIndex)
    {
        Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        Player = player;
        SourceCard = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
        SlotIndex = slotIndex;
    }
}

public interface IEffect
{
    string EffectId { get; }
    DuelActionResult Execute(EffectContext context);
}

public sealed class MarkerEffect : IEffect
{
    public string EffectId { get; }
    public MarkerEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context) => DuelActionResult.Ok();
}

/// <summary>
/// Effet générique basé sur un délégué pour les primitives simples sans classe dédiée.
/// </summary>
public sealed class DelegateEffect : IEffect
{
    public string EffectId { get; }
    private readonly Func<EffectContext, DuelActionResult> _execute;

    public DelegateEffect(string effectId, Func<EffectContext, DuelActionResult> execute)
    {
        EffectId = effectId ?? throw new ArgumentNullException(nameof(effectId));
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public DelegateEffect(string effectId, Action<EffectContext> execute)
        : this(effectId, ctx => { execute(ctx); return DuelActionResult.Ok(); })
    {
    }

    public DuelActionResult Execute(EffectContext context) => _execute(context);
}

public sealed class DrawCardsEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public DrawCardsEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DrawCards(context.Player, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class MillOpponentEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public MillOpponentEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.MillCards(context.Player.Opponent(), Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class SelfMillEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public SelfMillEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.MillCards(context.Player, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class DiscardOpponentHandEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public DiscardOpponentHandEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DiscardFromHand(context.Player.Opponent(), Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class DrawBothPlayersEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public DrawBothPlayersEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DrawCards(PlayerId.Player1, Amount);
        context.Engine.DrawCards(PlayerId.Player2, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class DiscardBothPlayersEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public DiscardBothPlayersEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DiscardFromHand(PlayerId.Player1, Amount);
        context.Engine.DiscardFromHand(PlayerId.Player2, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class BoostSelfEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public BoostSelfEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    // Les boosts de Force statiques sont gérés par le moteur (ex: TerrainSlot.GetForce).
    public DuelActionResult Execute(EffectContext context) => DuelActionResult.Ok();
}

public sealed class BanishDeckEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public bool TargetOpponent { get; }
    public BanishDeckEffect(string effectId, int amount, bool targetOpponent = false)
    {
        EffectId = effectId;
        Amount = amount;
        TargetOpponent = targetOpponent;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        var target = TargetOpponent ? context.Player.Opponent() : context.Player;
        context.Engine.BanishCardsFromDeck(target, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class BanishDiscardEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public bool TargetOpponent { get; }
    public BanishDiscardEffect(string effectId, int amount, bool targetOpponent = false)
    {
        EffectId = effectId;
        Amount = amount;
        TargetOpponent = targetOpponent;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        var target = TargetOpponent ? context.Player.Opponent() : context.Player;
        context.Engine.BanishCardsFromDiscard(target, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class BanishSetAsideEffect : IEffect
{
    public string EffectId { get; }
    public bool TargetOpponent { get; }
    public BanishSetAsideEffect(string effectId, bool targetOpponent = false)
    {
        EffectId = effectId;
        TargetOpponent = targetOpponent;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        var target = TargetOpponent ? context.Player.Opponent() : context.Player;
        var setAside = context.Engine.State.GetSetAside(target);
        var toBanish = new List<DuelCard>(setAside);
        context.Engine.State.ClearSetAside(target);
        context.Engine.State.AddCardsToBanish(target, toBanish);
        return DuelActionResult.Ok();
    }
}

public sealed class TakeSetAsideCardsEffect : IEffect
{
    public string EffectId { get; }
    public TakeSetAsideCardsEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var setAside = context.Engine.State.GetSetAside(context.Player);
        var toHand = new List<DuelCard>(setAside);
        context.Engine.State.ClearSetAside(context.Player);
        foreach (var c in toHand) context.Engine.State.AddCardToHand(context.Player, c);
        return DuelActionResult.Ok();
    }
}

public sealed class ShuffleHandAndDrawEffect : IEffect
{
    public string EffectId { get; }
    public int DrawAmount { get; }
    public ShuffleHandAndDrawEffect(string effectId, int drawAmount) { EffectId = effectId; DrawAmount = drawAmount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var hand = new List<DuelCard>(context.Engine.State.GetHand(context.Player));
        foreach (var c in hand)
        {
            context.Engine.State.RemoveCardFromHand(context.Player, c);
            context.Engine.State.AddCardToBottomDeck(context.Player, c);
        }
        context.Engine.DrawCards(context.Player, DrawAmount);
        return DuelActionResult.Ok();
    }
}

public sealed class TakeTopDiscardEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public TakeTopDiscardEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var discard = context.Engine.State.GetDiscard(context.Player);
        int toTake = Math.Min(Amount, discard.Count);
        for (int i = 0; i < toTake; i++)
        {
            var card = discard[discard.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, card);
            context.Engine.State.AddCardToHand(context.Player, card);
        }
        return DuelActionResult.Ok();
    }
}

public sealed class DrawUntilCardsEffect : IEffect
{
    public string EffectId { get; }
    public int TargetAmount { get; }
    public DrawUntilCardsEffect(string effectId, int targetAmount) { EffectId = effectId; TargetAmount = targetAmount; }
    public DuelActionResult Execute(EffectContext context)
    {
        int missing = TargetAmount - context.Engine.State.GetHand(context.Player).Count;
        if (missing > 0) context.Engine.DrawCards(context.Player, missing);
        return DuelActionResult.Ok();
    }
}

public sealed class LookTopDeckEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public LookTopDeckEffect(string effectId, int amount) { EffectId = effectId; Amount = amount; }
    public DuelActionResult Execute(EffectContext context)
    {
        var deck = context.Engine.State.GetDeck(context.Player);
        int count = Math.Min(Amount, deck.Count);
        if (count > 0)
        {
            var revealed = new List<DuelCard>(count);
            for (int i = 0; i < count; i++) revealed.Add(deck[i]);
            context.Engine.Emit(new CardsRevealedEvent(context.Player, revealed));
        }
        return DuelActionResult.Ok();
    }
}

public sealed class AddBottomDeckToHandEffect : IEffect
{
    public string EffectId { get; }
    public AddBottomDeckToHandEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var card = context.Engine.State.PopBottomDeckCard(context.Player);
        if (card != null) context.Engine.State.AddCardToHand(context.Player, card);
        return DuelActionResult.Ok();
    }
}

public sealed class DrawUntilOddEffect : IEffect
{
    public string EffectId { get; }
    public DrawUntilOddEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var hand = context.Engine.State.GetHand(context.Player);
        while (hand.Count < 6)
        {
            var drawn = context.Engine.DrawCards(context.Player, 1);
            if (drawn.Count == 0 || context.Engine.State.IsGameOver) break;
            if (drawn[0].Force % 2 != 0) break;
        }
        return DuelActionResult.Ok();
    }
}

public sealed class MillSelfByMaxForceEffect : IEffect
{
    public string EffectId { get; }
    public MillSelfByMaxForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        int maxForce = 0;
        foreach (var slot in context.Engine.State.Slots)
        {
            if (slot.IsEmpty) continue;
            foreach (var c in slot.CharactersP1) if (c.Force > maxForce) maxForce = c.Force;
            foreach (var c in slot.CharactersP2) if (c.Force > maxForce) maxForce = c.Force;
        }
        if (maxForce > 0) context.Engine.MillCards(context.Player, maxForce);
        return DuelActionResult.Ok();
    }
}

public sealed class PutDiscardUnderDeckEffect : IEffect
{
    public string EffectId { get; }
    public PutDiscardUnderDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var discard = context.Engine.State.GetDiscard(context.Player);
        int toTake = Math.Min(5, discard.Count);
        for (int i = 0; i < toTake; i++)
        {
            var card = discard[discard.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, card);
            context.Engine.State.AddCardToBottomDeck(context.Player, card);
        }
        return DuelActionResult.Ok();
    }
}

public sealed class MoveCharacterEffect : IEffect
{
    public string EffectId { get; }
    public MoveCharacterEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var candidates = new List<DuelCard>();
        int occupiedSlots = 0;
        for (int i = 0; i < context.Engine.State.Slots.Count; i++)
        {
            var s = context.Engine.State.Slots[i];
            if (s.IsEmpty) continue;
            occupiedSlots++;
            candidates.AddRange(s.GetCharacters(context.Player));
        }

        if (candidates.Count > 0 && occupiedSlots >= 2)
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(
                context.Player,
                "move_character",
                "Sélectionnez un personnage à déplacer",
                candidates,
                chosenCard =>
                {
                    int fromSlot = -1;
                    for (int i = 0; i < context.Engine.State.Slots.Count; i++)
                    {
                        var s = context.Engine.State.Slots[i];
                        if (!s.IsEmpty && s.GetCharacters(context.Player).Any(c => c.Id == chosenCard.Id))
                        {
                            fromSlot = i;
                            break;
                        }
                    }
                    if (fromSlot == -1) return;

                    context.Engine.Emit(new PlayerSlotChoiceRequiredEvent(
                        context.Player,
                        "Choisissez un terrain de destination",
                        $"Choisissez un autre terrain pour déplacer {chosenCard.Name}",
                        toSlot =>
                        {
                            if (toSlot != fromSlot && toSlot >= 0 && toSlot < context.Engine.State.Slots.Count && !context.Engine.State.Slots[toSlot].IsEmpty)
                            {
                                context.Engine.MoveCharacter(context.Player, chosenCard.Id, fromSlot, toSlot);
                            }
                        }
                    ));
                }
            ));
        }
        else
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(context.Player, "move_character", context.SourceCard));
        }

        return DuelActionResult.Ok();
    }
}

public sealed class PlayCharacterFreeEffect : IEffect
{
    public string EffectId { get; }
    public PlayCharacterFreeEffect(string effectId) => EffectId = effectId;

    public DuelActionResult Execute(EffectContext context)
    {
        var hand = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character).ToList();
        if (hand.Count == 0) return DuelActionResult.Ok();

        context.Engine.Emit(new PlayerChoiceRequiredEvent(
            context.Player,
            "Jouez un personnage gratuitement",
            "Sélectionnez un personnage",
            hand,
            chosenCard =>
            {
                context.Engine.State.RemoveCardFromHand(context.Player, chosenCard);
                var occupiedSlots = new List<int>();
                for (int i = 0; i < context.Engine.State.Slots.Count; i++)
                {
                    var s = context.Engine.State.Slots[i];
                    if (!s.IsEmpty && s.Card != null) occupiedSlots.Add(i);
                }

                if (occupiedSlots.Count == 1)
                {
                    int targetSlot = occupiedSlots[0];
                    context.Engine.State.AddCharacterToSlot(targetSlot, context.Player, chosenCard);
                    context.Engine.Emit(new CharacterPlayedEvent(context.Player, targetSlot, chosenCard));
                    context.Engine.CheckDeckOutCondition(context.Player);
                }
                else if (occupiedSlots.Count > 1)
                {
                    context.Engine.Emit(new PlayerSlotChoiceRequiredEvent(
                        context.Player,
                        "Choisissez un terrain cible",
                        $"Choisissez un terrain pour poser {chosenCard.Name}",
                        slotIdx =>
                        {
                            int targetSlot = (slotIdx >= 0 && slotIdx < context.Engine.State.Slots.Count && !context.Engine.State.Slots[slotIdx].IsEmpty)
                                ? slotIdx
                                : occupiedSlots[0];
                            context.Engine.State.AddCharacterToSlot(targetSlot, context.Player, chosenCard);
                            context.Engine.Emit(new CharacterPlayedEvent(context.Player, targetSlot, chosenCard));
                            context.Engine.CheckDeckOutCondition(context.Player);
                        }
                    ));
                }
                else
                {
                    context.Engine.State.AddCardToDiscard(context.Player, chosenCard);
                    context.Engine.CheckDeckOutCondition(context.Player);
                }
            }
        ));
        return DuelActionResult.Ok();
    }
}

public sealed class EffectRegistry
{
    private readonly Dictionary<string, IEffect> _effects = new Dictionary<string, IEffect>(StringComparer.OrdinalIgnoreCase);

    public EffectRegistry()
    {
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        // Pioches
        foreach (var (id, amount) in new[]
        {
            ("draw_1", 1), ("draw_2", 2), ("draw_3", 3),
            ("win_draw_1", 1), ("win_draw_2", 2), ("win_draw_3", 3),
            ("lose_draw_7", 7), ("terrain_draw_1_turn_start", 1),
            ("win_draw_or_put_under_deck", 3)
        })
        {
            Register(new DrawCardsEffect(id, amount));
        }
        Register(new DrawBothPlayersEffect("draw_both_1", 1));
        Register(new DrawUntilCardsEffect("win_draw_until_7", 7));

        // Meules adverses
        foreach (var (id, amount) in new[]
        {
            ("mill_1", 1), ("mill_2", 2), ("mill_3", 3), ("mill_4", 4), ("mill_5", 5), ("mill_10", 10),
            ("win_mill_2", 2), ("mill_opp_3", 3), ("mill_opp_4", 4), ("mill_opp_5", 5), ("mill_opp_10", 10)
        })
        {
            Register(new MillOpponentEffect(id, amount));
        }

        // Auto-meules
        foreach (var (id, amount) in new[]
        {
            ("self_mill_1", 1), ("self_mill_2", 2), ("self_mill_3", 3), ("self_mill_4", 4),
            ("lose_self_mill_1", 1), ("lose_self_mill_2", 2), ("lose_self_mill_3", 3), ("lose_self_mill_4", 4), ("lose_self_mill_5", 5),
            ("lose_mill_1", 1), ("lose_mill_2", 2), ("lose_mill_3", 3), ("lose_mill_4", 4), ("lose_mill_5", 5), ("lose_mill_6", 6),
            ("lose_mill_5_minus_drawn", 5)
        })
        {
            Register(new SelfMillEffect(id, amount));
        }
        Register(new MillSelfByMaxForceEffect("lose_mill_max_force"));

        // Bannissements deck
        foreach (var (id, amount) in new[]
        {
            ("banish_deck_3", 3), ("banish_deck_4", 4), ("banish_deck_6", 6),
            ("win_banish_deck_2", 2), ("lose_banish_deck_3", 3), ("lose_banish_deck_4", 4),
            ("lose_banish_deck_6", 6), ("lose_banish_deck_hand_4", 4)
        })
        {
            Register(new BanishDeckEffect(id, amount));
        }

        // Défausses de main
        foreach (var (id, amount) in new[]
        {
            ("discard_1", 1), ("discard_2", 2), ("lose_discard_1", 1), ("lose_discard_2", 2), ("lose_discard_3", 3)
        })
        {
            int n = amount;
            Register(new DelegateEffect(id, ctx => ctx.Engine.DiscardFromHand(ctx.Player, n)));
        }
        Register(new DiscardOpponentHandEffect("discard_opp_hand_1", 1));
        Register(new DiscardOpponentHandEffect("discard_opp_hand_2", 2));
        Register(new DiscardBothPlayersEffect("discard_both_hand_1", 1));

        // Défausse de personnage sur plateau
        Register(new DelegateEffect("discard_opp_char_force3", ctx => ExecuteDiscardOpponentCharacter(ctx, 3)));
        Register(new DelegateEffect("discard_opp_char_any", ctx => ExecuteDiscardOpponentCharacter(ctx, 999)));
        Register(new DelegateEffect("lose_discard_char_in_play", ExecuteDiscardOwnCharacterInPlay));

        // Manipulation de zones & choix
        Register(new MoveCharacterEffect("move_character"));
        Register(new AddBottomDeckToHandEffect("add_bottom_deck_to_hand"));
        Register(new LookTopDeckEffect("look_top_deck_5", 5));
        Register(new TakeSetAsideCardsEffect("win_take_setaside_3"));
        Register(new ShuffleHandAndDrawEffect("win_shuffle_hand_draw_5", 5));
        Register(new TakeTopDiscardEffect("win_take_discard_1", 1));
        Register(new TakeTopDiscardEffect("win_take_discard_2", 2));
        Register(new TakeTopDiscardEffect("win_take_discard_min_force", 1));
        Register(new DrawUntilOddEffect("win_draw_until_odd"));
        Register(new PlayCharacterFreeEffect("win_play_free"));
        Register(new DelegateEffect("win_play_free_score", ExecutePlayCharacterFreeAndScore));
        Register(new BanishDiscardEffect("lose_banish_discard_5", 5));
        Register(new BanishSetAsideEffect("lose_banish_setaside_3"));
        Register(new DelegateEffect("lose_banish_hand_draw", ctx =>
        {
            int count = ctx.Engine.State.GetHand(ctx.Player).Count;
            ctx.Engine.BanishCardsFromHand(ctx.Player, count);
            ctx.Engine.DrawCards(ctx.Player, count);
        }));
        Register(new DelegateEffect("win_shuffle_terrains", ExecuteShuffleTerrainsFromDiscard));
        Register(new DelegateEffect("win_search_card", ExecuteSearchCard));
        Register(new DelegateEffect("win_copy_effect", ExecuteCopyWinningEffect));
        Register(new PutDiscardUnderDeckEffect("win_put_under_deck"));

        // Boosts legacy (échelle Tetramon)
        foreach (int boost in new[] { 15, 20, 30, 45, 60, 75, 90 })
        {
            Register(new BoostSelfEffect($"boost_self_{boost}", boost));
        }

        // Marqueurs passifs & terrain (évalués dynamiquement par TerrainSlot / DuelEngine / PassiveHandlers)
        string[] markerIds =
        {
            "boost_laink_20", "boost_terracid_20", "boost_laink_terracid_3", "boost_laink_terracid_30",
            "terrain_combo_boost_1", "terrain_combo_boost_10",
            "terrain_on_char_played_draw_1", "terrain_on_char_played_banish_top_1",
            "terrain_on_move_draw_1", "terrain_on_terrain_played_draw_1",
            "passive_autoscore_threshold_9", "passive_autoscore_threshold_13",
            "passive_boost_1_per_wcdo", "passive_boost_4_if_only_guest",
            "passive_boost_all_1_if_multi_score_turn", "passive_boost_laink_1",
            "passive_boost_self_5_if_alone", "passive_boost_terracid_1",
            "passive_combo_return_top_discard_once_turn", "passive_copy_right_force",
            "passive_cost_all_minus_1", "passive_cost_all_plus_1", "passive_cost_opp_plus_1",
            "passive_cost_self_minus_1", "passive_cost_self_minus_2_if_fewer_chars",
            "passive_discard_self_if_score_on_turn", "passive_double_force_if_carlito",
            "passive_double_force_if_mcfly", "passive_double_force_if_single_opp_char",
            "passive_double_force_laink", "passive_double_force_own_on_slot",
            "passive_double_force_per_copy", "passive_double_force_terracid",
            "passive_end_draw_sacrifice_draw_2", "passive_forbid_opp_char_force_ge_2_on_slot",
            "passive_force_per_5_discard_1", "passive_force_per_completed_combo_1",
            "passive_force_per_hand_card_1", "passive_force_per_opp_completed_combo_1",
            "passive_force_play_on_this_slot", "passive_halve_force_opp_on_slot",
            "passive_halve_losing_effect", "passive_halve_opp_winning_effect",
            "passive_move_on_score_instead_of_discard", "passive_on_leave_play_mill_2",
            "passive_on_lose_opp_banish_top_deck_1", "passive_on_lose_score_take_discard_1",
            "passive_on_opp_combo_discard_random_1", "passive_on_opp_combo_return_discard_bottom_1",
            "passive_on_opp_play_char_cycle_1", "passive_on_score_move_left", "passive_on_score_move_right",
            "passive_on_score_play_free_no_effect", "passive_on_score_reveal_top_even_add_else_sub",
            "passive_on_score_reveal_top_odd_add_else_sub", "passive_once_turn_if_alone_discard_to_bottom_deck",
            "passive_opp_chars_minus_1", "passive_opp_discard_plus_1_if_ge_2", "passive_opp_discard_random",
            "passive_opp_draw_plus_1_discard_1", "passive_reduce_discard_2_to_1",
            "passive_sacrifice_boost_other_3_turn", "passive_sacrifice_take_top_discard_3",
            "passive_sacrifice_to_recover_discarded_hand", "passive_sacrifice_to_score_1",
            "passive_sacrifice_to_score_up_to_2", "passive_start_turn_infecte",
            "passive_start_turn_take_discard_1", "passive_substitute_discard",
            "passive_swap_with_hand_char_and_discard", "passive_terrain_autoscore_min_5_chars",
            "passive_terrain_autoscore_only", "passive_terrain_boost_all_1",
            "passive_terrain_cost_plus_2", "passive_terrain_daltonisme", "passive_terrain_disable_sub",
            "passive_terrain_discard_hand_mills_instead", "passive_terrain_draw_if_hand_lt_3",
            "passive_terrain_draw_until_5", "passive_terrain_forbid_duplicate_names",
            "passive_terrain_forbid_other_terrains", "passive_terrain_hand_limit_5",
            "passive_terrain_most_hand_boost_1", "passive_terrain_must_play_second_or_discard",
            "passive_terrain_no_draw_if_hand_ge_3", "passive_terrain_on_sub_take_discard_2",
            "passive_terrain_play_terrain_or_discard", "passive_terrain_score_only_if_alone",
            "passive_terrain_stillwater", "passive_triple_force_non_laink_terracid",
            "passive_unique_in_play", "passive_unique_name_on_slot", "passive_unique_trophy"
        };
        foreach (string id in markerIds)
        {
            Register(new MarkerEffect(id));
        }
    }

    private static void ExecuteDiscardOpponentCharacter(EffectContext context, int maxForce)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return;
        var opponent = context.Player.Opponent();
        var slot = context.Engine.State.Slots[context.SlotIndex];
        var chars = slot.GetCharacters(opponent);
        if (chars == null || chars.Count == 0) return;

        DuelCard? target = null;
        for (int i = 0; i < chars.Count; i++)
        {
            var c = chars[i];
            if (c.Force <= maxForce && (target == null || c.Force > target.Force))
            {
                target = c;
            }
        }
        if (target != null)
        {
            slot.RemoveCharacter(opponent, target);
            context.Engine.State.AddCardToDiscard(opponent, target);
        }
    }

    private static void ExecuteDiscardOwnCharacterInPlay(EffectContext context)
    {
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count) return;
        var slot = context.Engine.State.Slots[context.SlotIndex];
        var chars = slot.GetCharacters(context.Player);
        if (chars == null || chars.Count == 0) return;

        var target = chars[0];
        slot.RemoveCharacter(context.Player, target);
        context.Engine.State.AddCardToDiscard(context.Player, target);
    }

    private static void ExecutePlayCharacterFreeAndScore(EffectContext context)
    {
        var hand = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character).ToList();
        if (hand.Count == 0) return;

        context.Engine.Emit(new PlayerChoiceRequiredEvent(
            context.Player,
            "Jouez un personnage gratuitement",
            "Sélectionnez un personnage (suivi du choix du terrain)",
            hand,
            chosenCard => context.Engine.Emit(new PlayerSlotChoiceRequiredEvent(
                context.Player,
                "Choisissez un terrain cible",
                $"Sélectionnez un terrain actif pour poser {chosenCard.Name} et le scorer.",
                slotIdx =>
                {
                    if (slotIdx < 0 || slotIdx >= context.Engine.State.Slots.Count) return;
                    if (context.Engine.State.Slots[slotIdx].IsEmpty) return;
                    context.Engine.State.RemoveCardFromHand(context.Player, chosenCard);
                    context.Engine.State.AddCharacterToSlot(slotIdx, context.Player, chosenCard);
                    context.Engine.Emit(new CharacterPlayedEvent(context.Player, slotIdx, chosenCard));
                    context.Engine.ResolveScore(slotIdx, context.Player);
                    context.Engine.CheckDeckOutCondition(context.Player);
                }
            ))
        ));
    }

    private static void ExecuteShuffleTerrainsFromDiscard(EffectContext context)
    {
        var terrains = context.Engine.State.GetDiscard(context.Player).Where(c => c.Kind == CardKind.Terrain).ToList();
        if (terrains.Count == 0) return;

        context.Engine.Emit(new PlayerChoiceRequiredEvent(
            context.Player,
            "Ajoutez des terrains",
            "Sélectionnez 1 terrain à ajouter",
            terrains,
            chosenCard =>
            {
                context.Engine.State.RemoveCardFromDiscard(context.Player, chosenCard);
                context.Engine.State.AddCardToBottomDeck(context.Player, chosenCard);
            }
        ));
    }

    private static void ExecuteSearchCard(EffectContext context)
    {
        var deck = context.Engine.State.GetDeck(context.Player);
        if (deck.Count == 0) return;

        context.Engine.Emit(new PlayerChoiceRequiredEvent(
            context.Player,
            "Cherchez 1 carte",
            "Sélectionnez une carte à mettre en main",
            deck.ToList(),
            chosenCard =>
            {
                context.Engine.State.RemoveCardFromDeck(context.Player, chosenCard);
                context.Engine.State.AddCardToHand(context.Player, chosenCard);
            }
        ));
    }

    private static void ExecuteCopyWinningEffect(EffectContext context)
    {
        var terrains = context.Engine.State.Slots.Where(s => !s.IsEmpty && s.Card != null).Select(s => s.Card!).ToList();
        if (terrains.Count == 0) return;

        context.Engine.Emit(new PlayerChoiceRequiredEvent(
            context.Player,
            "Copiez un effet gagnant",
            "Choisissez un terrain en jeu",
            terrains,
            chosenCard =>
            {
                if (chosenCard.EffectIds == null) return;
                foreach (var effId in chosenCard.EffectIds)
                {
                    if (effId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) &&
                        context.Engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
                    {
                        eff.Execute(context);
                    }
                }
            }
        ));
    }

    public void Register(IEffect effect)
    {
        if (effect == null) return;
        _effects[effect.EffectId] = effect;
    }

    public bool TryGetEffect(string effectId, out IEffect? effect)
    {
        return _effects.TryGetValue(effectId, out effect);
    }
}
