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

public sealed class EffectRegistry
{
    private readonly Dictionary<string, IEffect> _effects = new Dictionary<string, IEffect>(StringComparer.OrdinalIgnoreCase);

    public EffectRegistry()
    {
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        Register(new DrawCardsEffect("draw_1", 1));
        Register(new DrawCardsEffect("draw_2", 2));
        Register(new DrawCardsEffect("draw_3", 3));

        Register(new MillOpponentEffect("mill_1", 1));
        Register(new MillOpponentEffect("mill_2", 2));
        Register(new MillOpponentEffect("mill_3", 3));
        Register(new MillOpponentEffect("mill_4", 4));
        Register(new MillOpponentEffect("mill_10", 10));

        Register(new DiscardCardsEffect("discard_1", 1));
        Register(new DiscardCardsEffect("discard_2", 2));
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
