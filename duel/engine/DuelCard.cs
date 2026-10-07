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
    public string? Effigy { get; }
    public IReadOnlyList<string> EffectIds { get; }
    public IReadOnlyList<string> ComboEffectIds { get; }
    public bool HasOpeningGem { get; }
    public bool HasClosingGem { get; }

    public DuelCard(
        string id,
        string name,
        CardKind kind,
        int force = 0,
        bool isScoreur = false,
        IReadOnlyList<string>? effectIds = null,
        string? effigy = null,
        IReadOnlyList<string>? comboEffectIds = null,
        bool hasOpeningGem = true,
        bool hasClosingGem = false)
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        Kind = kind;
        Force = force;
        IsScoreur = isScoreur;
        EffectIds = effectIds ?? Array.Empty<string>();
        Effigy = effigy;
        ComboEffectIds = comboEffectIds ?? Array.Empty<string>();
        HasOpeningGem = kind == CardKind.Character ? hasOpeningGem : false;
        HasClosingGem = kind == CardKind.Character ? hasClosingGem : false;
    }

    public override string ToString() => $"{Name} ({Kind}, Force: {Force}, Effigy: {Effigy ?? "None"}, Scoreur: {IsScoreur}, Combo: {(HasClosingGem ? "Yes" : "No")})";
}
