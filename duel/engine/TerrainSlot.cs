#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Un terrain est actif (et donc scorable) s'il a été placé avant le tour en cours (PlacedOnTurn &lt; currentTurn).
    /// </summary>
    public bool IsActive(int currentTurn)
    {
        return Card != null && PlacedOnTurn.HasValue && PlacedOnTurn.Value < currentTurn;
    }

    public IReadOnlyList<DuelCard> GetCharacters(PlayerId player) =>
        player == PlayerId.Player1 ? CharactersP1 : CharactersP2;

    private static bool HasEffect(IReadOnlyList<string>? effects, string target)
    {
        if (effects == null) return false;
        int count = effects.Count;
        for (int i = 0; i < count; i++)
        {
            if (string.Equals(effects[i], target, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryParseBoostSelf(string eff, out int boostVal)
    {
        const string Prefix = "boost_self_";
        boostVal = 0;
        if (eff == null || eff.Length <= Prefix.Length || !eff.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        int result = 0;
        for (int i = Prefix.Length; i < eff.Length; i++)
        {
            char ch = eff[i];
            if (ch < '0' || ch > '9')
            {
                return false;
            }
            result = (result * 10) + (ch - '0');
        }
        boostVal = result;
        return true;
    }

    public int GetForce(PlayerId player)
    {
        var chars = GetCharacters(player);
        int charCount = chars.Count;
        if (charCount == 0)
        {
            return 0;
        }

        int total = 0;
        var terrainEffects = Card?.EffectIds;
        bool isMoria = HasEffect(terrainEffects, "boost_laink_20");
        bool isPortal = HasEffect(terrainEffects, "boost_terracid_20");
        bool isBureaux3 = HasEffect(terrainEffects, "boost_laink_terracid_3");
        bool isBureaux30 = HasEffect(terrainEffects, "boost_laink_terracid_30");
        bool isCuisine1 = HasEffect(terrainEffects, "terrain_combo_boost_1");
        bool isCuisine10 = HasEffect(terrainEffects, "terrain_combo_boost_10");

        for (int i = 0; i < charCount; i++)
        {
            var c = chars[i];
            int charForce = c.Force;
            bool isLaink = string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase);
            bool isTerracid = string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase);

            if (isMoria && isLaink)
            {
                charForce += 20;
            }
            else if (isPortal && isTerracid)
            {
                charForce += 20;
            }

            if (isLaink || isTerracid)
            {
                if (isBureaux3) charForce += 3;
                if (isBureaux30) charForce += 30;
            }

            if (c.EffectIds != null)
            {
                int effCount = c.EffectIds.Count;
                for (int j = 0; j < effCount; j++)
                {
                    if (TryParseBoostSelf(c.EffectIds[j], out int boostVal))
                    {
                        charForce += boostVal;
                    }
                }
            }

            bool comboActiveRight = ComboEvaluator.IsComboActive(chars, i);
            bool comboActiveLeft = (i + 1 < charCount) && ComboEvaluator.IsComboActive(chars, i + 1);

            if (comboActiveRight && c.ComboEffectIds != null)
            {
                int comboCount = c.ComboEffectIds.Count;
                for (int j = 0; j < comboCount; j++)
                {
                    if (TryParseBoostSelf(c.ComboEffectIds[j], out int boostVal))
                    {
                        charForce += boostVal;
                    }
                }
            }

            if (comboActiveRight || comboActiveLeft)
            {
                if (isCuisine1) charForce += 1;
                if (isCuisine10) charForce += 10;
            }

            total += charForce;
        }
        return total;
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
