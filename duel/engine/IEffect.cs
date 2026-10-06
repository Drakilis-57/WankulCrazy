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

public sealed class DrawCardsEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }

    public DrawCardsEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

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

    public MillOpponentEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var targetPlayer = context.Player.Opponent();
        context.Engine.MillCards(targetPlayer, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class DiscardCardsEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }

    public DiscardCardsEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DiscardFromHand(context.Player, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class DiscardOpponentHandEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }

    public DiscardOpponentHandEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var opponent = context.Player.Opponent();
        context.Engine.DiscardFromHand(opponent, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class DiscardOpponentCharacterEffect : IEffect
{
    public string EffectId { get; }
    public int MaxForce { get; }

    public DiscardOpponentCharacterEffect(string effectId, int maxForce = 999)
    {
        EffectId = effectId;
        MaxForce = maxForce;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        var opponent = context.Player.Opponent();
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count)
            return DuelActionResult.Ok();

        var slot = context.Engine.State.Slots[context.SlotIndex];
        var chars = slot.GetCharacters(opponent);
        if (chars == null || chars.Count == 0) return DuelActionResult.Ok();

        DuelCard? target = null;
        for (int i = 0; i < chars.Count; i++)
        {
            var c = chars[i];
            if (c.Force <= MaxForce)
            {
                if (target == null || c.Force > target.Force)
                {
                    target = c;
                }
            }
        }

        if (target != null)
        {
            slot.RemoveCharacter(opponent, target);
            context.Engine.State.AddCardToDiscard(opponent, target);
        }
        return DuelActionResult.Ok();
    }
}

public sealed class DrawBothPlayersEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }

    public DrawBothPlayersEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

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

    public DiscardBothPlayersEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DiscardFromHand(PlayerId.Player1, Amount);
        context.Engine.DiscardFromHand(PlayerId.Player2, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class TerrainStartTurnDrawEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }

    public TerrainStartTurnDrawEffect(string effectId, int amount = 1)
    {
        EffectId = effectId;
        Amount = amount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DrawCards(context.Player, Amount);
        return DuelActionResult.Ok();
    }
}


public sealed class BoostSelfEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }

    public BoostSelfEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }

    public DuelActionResult Execute(EffectContext context)
    {
        // Les boosts de Force statiques sont gérés par le moteur (ex: TerrainSlot.GetForce).
        // L'effet actif ne fait donc rien lors de l'exécution, il sert de tag sur la carte.
        return DuelActionResult.Ok();
    }
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
    public ShuffleHandAndDrawEffect(string effectId, int drawAmount)
    {
        EffectId = effectId;
        DrawAmount = drawAmount;
    }
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

public sealed class BanishHandAndDrawEffect : IEffect
{
    public string EffectId { get; }
    public BanishHandAndDrawEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var hand = new List<DuelCard>(context.Engine.State.GetHand(context.Player));
        int count = hand.Count;
        context.Engine.BanishCardsFromHand(context.Player, count);
        context.Engine.DrawCards(context.Player, count);
        return DuelActionResult.Ok();
    }
}

public sealed class TakeTopDiscardEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public TakeTopDiscardEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }
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

public sealed class TakeDiscardByMinForceEffect : IEffect
{
    public string EffectId { get; }
    public TakeDiscardByMinForceEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var discard = context.Engine.State.GetDiscard(context.Player);
        if (discard.Count > 0)
        {
            var card = discard[discard.Count - 1];
            context.Engine.State.RemoveCardFromDiscard(context.Player, card);
            context.Engine.State.AddCardToHand(context.Player, card);
        }
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

public sealed class PlayCharacterFreeAndScoreEffect : IEffect
{
    public string EffectId { get; }
    public PlayCharacterFreeAndScoreEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var hand = context.Engine.State.GetHand(context.Player).Where(c => c.Kind == CardKind.Character).ToList();
        if (hand.Count > 0)
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(
                context.Player,
                "Jouez un personnage gratuitement",
                "Sélectionnez un personnage",
                hand,
                chosenCard => {
                    context.Engine.State.RemoveCardFromHand(context.Player, chosenCard);

                    int targetSlot = -1;
                    for (int i = 0; i < context.Engine.State.Slots.Count; i++)
                    {
                        var s = context.Engine.State.Slots[i];
                        if (i != context.SlotIndex && s.IsActive(context.Engine.State.TurnNumber) && s.Card != null)
                        {
                            targetSlot = i;
                            break;
                        }
                    }

                    if (targetSlot != -1)
                    {
                        context.Engine.State.AddCharacterToSlot(targetSlot, context.Player, chosenCard);
                        context.Engine.Emit(new CharacterPlayedEvent(context.Player, targetSlot, chosenCard));
                        // Score the OTHER slot
                        context.Engine.ResolveScore(targetSlot);
                    }
                    else
                    {
                        // Fallback if no other active terrain
                        context.Engine.State.AddCardToDiscard(context.Player, chosenCard);
                    }
                }
            ));
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
        if (hand.Count > 0)
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(
                context.Player,
                "Jouez un personnage gratuitement",
                "Sélectionnez un personnage",
                hand,
                chosenCard => {
                    context.Engine.State.RemoveCardFromHand(context.Player, chosenCard);
                    // Emit a specific event or put it in a "PendingFreePlay" state that the UI resolves.
                    // For the pure engine, we must put it on the board if a slot is available.
                    int emptySlot = -1;
                    for (int i = 0; i < context.Engine.State.Slots.Count; i++)
                    {
                        var s = context.Engine.State.Slots[i];
                        if (s.IsActive(context.Engine.State.TurnNumber) && s.Card != null)
                        {
                            emptySlot = i;
                            break;
                        }
                    }
                    if (emptySlot != -1)
                    {
                        context.Engine.State.AddCharacterToSlot(emptySlot, context.Player, chosenCard);
                        context.Engine.Emit(new CharacterPlayedEvent(context.Player, emptySlot, chosenCard));
                    }
                    else
                    {
                        context.Engine.State.AddCardToDiscard(context.Player, chosenCard);
                    }
                }
            ));
        }
        return DuelActionResult.Ok();
    }
}

public sealed class DiscardCharacterInPlayEffect : IEffect
{
    public string EffectId { get; }
    public bool TargetOpponent { get; }
    public DiscardCharacterInPlayEffect(string effectId, bool targetOpponent = false)
    {
        EffectId = effectId;
        TargetOpponent = targetOpponent;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        var targetPlayer = TargetOpponent ? context.Player.Opponent() : context.Player;
        if (context.SlotIndex < 0 || context.SlotIndex >= context.Engine.State.Slots.Count)
            return DuelActionResult.Ok();

        var slot = context.Engine.State.Slots[context.SlotIndex];
        var chars = slot.GetCharacters(targetPlayer);
        if (chars == null || chars.Count == 0) return DuelActionResult.Ok();

        var target = chars[0];
        slot.RemoveCharacter(targetPlayer, target);
        context.Engine.State.AddCardToDiscard(targetPlayer, target);

        return DuelActionResult.Ok();
    }
}

public sealed class MillSelfEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public MillSelfEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.MillCards(context.Player, Amount);
        return DuelActionResult.Ok();
    }
}

public sealed class BanishFromDeckOrHandEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public bool TargetOpponent { get; }
    public BanishFromDeckOrHandEffect(string effectId, int amount, bool targetOpponent = false)
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

public sealed class MillSelfMinusWinnerDrawnEffect : IEffect
{
    public string EffectId { get; }
    public int Amount { get; }
    public MillSelfMinusWinnerDrawnEffect(string effectId, int amount)
    {
        EffectId = effectId;
        Amount = amount;
    }
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.MillCards(context.Player, Amount);
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
        if (maxForce > 0)
        {
            context.Engine.MillCards(context.Player, maxForce);
        }
        return DuelActionResult.Ok();
    }
}

public sealed class ShuffleTerrainsFromDiscardEffect : IEffect
{
    public string EffectId { get; }
    public ShuffleTerrainsFromDiscardEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var terrains = context.Engine.State.GetDiscard(context.Player).Where(c => c.Kind == CardKind.Terrain).ToList();
        if (terrains.Count > 0)
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(
                context.Player,
                "Ajoutez des terrains",
                "Sélectionnez 1 terrain à ajouter", // Simpler implementation handling 1 choice for engine compatibility
                terrains,
                chosenCard => {
                    context.Engine.State.RemoveCardFromDiscard(context.Player, chosenCard);
                    context.Engine.State.AddCardToBottomDeck(context.Player, chosenCard);
                }
            ));
        }
        return DuelActionResult.Ok();
    }
}

public sealed class SearchCardEffect : IEffect
{
    public string EffectId { get; }
    public SearchCardEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var deck = context.Engine.State.GetDeck(context.Player);
        if (deck.Count > 0)
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(
                context.Player,
                "Cherchez 1 carte",
                "Sélectionnez une carte à mettre en main",
                deck.ToList(),
                chosenCard => {
                    context.Engine.State.RemoveCardFromDeck(context.Player, chosenCard);
                    context.Engine.State.AddCardToHand(context.Player, chosenCard);
                    // context.Engine.State.ShuffleDeck(context.Player, ...); // Needs engine support for shuffling from effects // Needs proper IRandom, but we can't easily inject here without engine ref, wait engine has rng? Effects don't hold rng.
                    // Let's just assume we don't shuffle in the pure model for now or engine will handle it via events later.
                }
            ));
        }
        return DuelActionResult.Ok();
    }
}

public sealed class CopyWinningEffectEffect : IEffect
{
    public string EffectId { get; }
    public CopyWinningEffectEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        var terrains = context.Engine.State.Slots.Where(s => !s.IsEmpty && s.Card != null).Select(s => s.Card!).ToList();
        if (terrains.Count > 0)
        {
            context.Engine.Emit(new PlayerChoiceRequiredEvent(
                context.Player,
                "Copiez un effet gagnant",
                "Choisissez un terrain en jeu",
                terrains,
                chosenCard => {
                    if (chosenCard.EffectIds != null)
                    {
                        foreach (var effId in chosenCard.EffectIds)
                        {
                            if (effId.StartsWith("win_", System.StringComparison.OrdinalIgnoreCase))
                            {
                                if (context.Engine.Effects.TryGetEffect(effId, out var eff) && eff != null)
                                {
                                    eff.Execute(context);
                                }
                            }
                        }
                    }
                }
            ));
        }
        return DuelActionResult.Ok();
    }
}

public sealed class DrawOrPutDiscardUnderDeckEffect : IEffect
{
    public string EffectId { get; }
    public DrawOrPutDiscardUnderDeckEffect(string effectId) => EffectId = effectId;
    public DuelActionResult Execute(EffectContext context)
    {
        context.Engine.DrawCards(context.Player, 3);
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
        Register(new DrawCardsEffect("draw_1", 1));
        Register(new DrawCardsEffect("draw_2", 2));
        Register(new DrawCardsEffect("draw_3", 3));
        Register(new DrawBothPlayersEffect("draw_both_1", 1));

        // Meules
        Register(new MillOpponentEffect("mill_1", 1));
        Register(new MillOpponentEffect("mill_2", 2));
        Register(new MillOpponentEffect("mill_3", 3));
        Register(new MillOpponentEffect("mill_4", 4));
        Register(new MillOpponentEffect("mill_10", 10));

        // Défausses de main
        Register(new DiscardCardsEffect("discard_1", 1));
        Register(new DiscardCardsEffect("discard_2", 2));
        Register(new DiscardOpponentHandEffect("discard_opp_hand_1", 1));
        Register(new DiscardOpponentHandEffect("discard_opp_hand_2", 2));
        Register(new DiscardBothPlayersEffect("discard_both_hand_1", 1));

        // Défausse de personnage adverse (Removal)
        Register(new DiscardOpponentCharacterEffect("discard_opp_char_force3", maxForce: 3));
        Register(new DiscardOpponentCharacterEffect("discard_opp_char_any", maxForce: 999));

        // Terrains
        Register(new TerrainStartTurnDrawEffect("terrain_draw_1_turn_start", 1));
        Register(new DrawCardsEffect("win_draw_1", 1));
        Register(new MillOpponentEffect("win_mill_2", 2));
        Register(new DiscardCardsEffect("lose_discard_1", 1));
        Register(new MillOpponentEffect("mill_opp_3", 3));
        Register(new MillOpponentEffect("mill_opp_4", 4));
        Register(new MillOpponentEffect("mill_opp_5", 5));
        Register(new MillOpponentEffect("mill_opp_10", 10));
        Register(new BoostSelfEffect("boost_self_15", 15));
        Register(new BoostSelfEffect("boost_self_20", 20));
        Register(new BoostSelfEffect("boost_self_30", 30));
        Register(new BoostSelfEffect("boost_self_45", 45));
        Register(new BoostSelfEffect("boost_self_60", 60));
        Register(new BoostSelfEffect("boost_self_75", 75));
        Register(new BoostSelfEffect("boost_self_90", 90));

        // Campus S2 Winning Effects
        Register(new BanishDeckEffect("win_banish_deck_2", 2));
        Register(new TakeSetAsideCardsEffect("win_take_setaside_3"));
        Register(new ShuffleHandAndDrawEffect("win_shuffle_hand_draw_5", 5));
        Register(new TakeTopDiscardEffect("win_take_discard_1", 1));
        Register(new TakeTopDiscardEffect("win_take_discard_2", 2));
        Register(new TakeDiscardByMinForceEffect("win_take_discard_min_force"));
        Register(new DrawUntilOddEffect("win_draw_until_odd"));
        Register(new PlayCharacterFreeAndScoreEffect("win_play_free_score"));
        Register(new PlayCharacterFreeEffect("win_play_free"));

        // Campus S2 Losing Effects
        Register(new BanishDeckEffect("lose_banish_deck_3", 3));
        Register(new BanishDeckEffect("lose_banish_deck_4", 4));
        Register(new BanishDiscardEffect("lose_banish_discard_5", 5));
        Register(new BanishSetAsideEffect("lose_banish_setaside_3"));
        Register(new BanishHandAndDrawEffect("lose_banish_hand_draw"));
        Register(new DiscardCardsEffect("lose_discard_3", 3));
        Register(new MillSelfEffect("lose_mill_3", 3));
        Register(new MillSelfEffect("lose_mill_4", 4));
        Register(new MillSelfEffect("lose_mill_5", 5));
        Register(new DiscardCharacterInPlayEffect("lose_discard_char_in_play"));

        Register(new BanishFromDeckOrHandEffect("lose_banish_deck_hand_4", 4));
        Register(new MillSelfMinusWinnerDrawnEffect("lose_mill_5_minus_drawn", 5));
        Register(new MillSelfByMaxForceEffect("lose_mill_max_force"));
        Register(new ShuffleTerrainsFromDiscardEffect("win_shuffle_terrains"));
        Register(new SearchCardEffect("win_search_card"));
        Register(new CopyWinningEffectEffect("win_copy_effect"));
        Register(new DrawOrPutDiscardUnderDeckEffect("win_draw_or_put_under_deck"));
        Register(new PutDiscardUnderDeckEffect("win_put_under_deck"));
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
