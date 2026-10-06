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

    public int GetForce(PlayerId player)
    {
        var chars = GetCharacters(player);
        int total = 0;
        bool isMoria = Card != null && (Card.Name.IndexOf("MORIA", StringComparison.OrdinalIgnoreCase) >= 0 || (Card.EffectIds != null && Card.EffectIds.Contains("boost_laink_20")));
        bool isPortal = Card != null && (Card.Name.IndexOf("PORTAL", StringComparison.OrdinalIgnoreCase) >= 0 || (Card.EffectIds != null && Card.EffectIds.Contains("boost_terracid_20")));

        for (int i = 0; i < chars.Count; i++)
        {
            var c = chars[i];
            int charForce = c.Force;
            if (isMoria && string.Equals(c.Effigy, "Laink", StringComparison.OrdinalIgnoreCase))
            {
                charForce += 20;
            }
            else if (isPortal && string.Equals(c.Effigy, "Terracid", StringComparison.OrdinalIgnoreCase))
            {
                charForce += 20;
            }
            total += charForce;
        }
        return total;
    }

    internal void AddCharacter(PlayerId player, DuelCard card)
    {
        if (player == PlayerId.Player1) _charactersP1.Add(card);
        else _charactersP2.Add(card);
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
