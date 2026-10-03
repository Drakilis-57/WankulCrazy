#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public enum CardKind
{
    Terrain,
    Character
}

public sealed class DuelCard
{
    public string Id { get; }
    public string Name { get; }
    public CardKind Kind { get; }
    public int Force { get; }
    public bool IsScoreur { get; }
    public IReadOnlyList<string> EffectIds { get; }

    public DuelCard(
        string id,
        string name,
        CardKind kind,
        int force = 0,
        bool isScoreur = false,
        IReadOnlyList<string>? effectIds = null)
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        Kind = kind;
        Force = force;
        IsScoreur = isScoreur;
        EffectIds = effectIds ?? Array.Empty<string>();
    }

    public override string ToString() => $"{Name} ({Kind}, Force: {Force}, Scoreur: {IsScoreur})";
}
