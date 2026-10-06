#nullable enable
using System;
using System.Collections.Generic;

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
