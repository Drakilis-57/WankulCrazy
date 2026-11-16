#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public enum CardKind
{
    Terrain,
    Character
}

[Flags]
public enum GemMask
{
    None = 0,
    ClosingOrange = 1 << 0,
    ClosingViolet = 1 << 1,
    OpeningOrange = 1 << 2,
    OpeningViolet = 1 << 3
}

public sealed class DuelCard
{
    public string Id { get; }
    public string Name { get; }
    public CardKind Kind { get; }
    public int Force { get; }
    public bool IsScoreur { get; }
    public bool IsComboScoreur { get; }
    public string? Effigy { get; }
    public IReadOnlyList<string> EffectIds { get; }
    public IReadOnlyList<string> ComboEffectIds { get; }
    public int Cost { get; }
    public IReadOnlyList<string> PassiveEffectIds { get; }
    public IReadOnlyList<string> SubEffectIds { get; }
    public bool IsDuo { get; }
    public GemMask Gems { get; }
    public bool HasOpeningOrangeGem => (Gems & GemMask.OpeningOrange) != 0;
    public bool HasOpeningVioletGem => (Gems & GemMask.OpeningViolet) != 0;
    public bool HasClosingOrangeGem => (Gems & GemMask.ClosingOrange) != 0;
    public bool HasClosingVioletGem => (Gems & GemMask.ClosingViolet) != 0;
    public bool HasOpeningGem => HasOpeningOrangeGem || HasOpeningVioletGem;
    public bool HasClosingGem => HasClosingOrangeGem || HasClosingVioletGem;

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
        bool hasClosingGem = false,
        bool isComboScoreur = false,
        GemMask? gemMask = null,
        int cost = 0,
        IReadOnlyList<string>? passiveEffectIds = null,
        IReadOnlyList<string>? subEffectIds = null,
        bool isDuo = false)
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        Kind = kind;
        Force = force;
        IsScoreur = isScoreur;
        IsComboScoreur = kind == CardKind.Character && isComboScoreur;
        EffectIds = effectIds ?? Array.Empty<string>();
        Effigy = effigy;
        ComboEffectIds = comboEffectIds ?? Array.Empty<string>();
        IsDuo = isDuo;
        Cost = (kind == CardKind.Terrain || isDuo) ? 0 : Math.Max(0, cost);
        PassiveEffectIds = passiveEffectIds ?? Array.Empty<string>();
        SubEffectIds = subEffectIds ?? Array.Empty<string>();
        if (kind != CardKind.Character)
        {
            Gems = GemMask.None;
        }
        else if (gemMask.HasValue)
        {
            Gems = gemMask.Value;
        }
        else
        {
            // Compatibilité descendante pour les constructeurs booléens des tests unitaires
            var resolved = GemMask.None;
            if (hasOpeningGem) resolved |= GemMask.OpeningOrange;
            if (hasClosingGem) resolved |= GemMask.ClosingOrange;
            Gems = resolved;
        }
    }

    public override string ToString() => $"{Name} ({Kind}, Force: {Force}, Effigy: {Effigy ?? "None"}, Scoreur: {IsScoreur}, Combo: {(HasClosingGem ? "Yes" : "No")})";
}
