#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// État complet d'une partie de duel Wankul TCG.
/// Seul le DuelEngine est autorisé à modifier cet état.
/// Vue et agents extérieurs y accèdent en lecture seule.
/// Aucun membre public ou internal ne permet de modifier l'état depuis l'extérieur.
/// </summary>
public sealed class DuelState
{
    private readonly List<DuelCard> _deckP1 = new List<DuelCard>();
    private readonly List<DuelCard> _deckP2 = new List<DuelCard>();

    private readonly List<DuelCard> _handP1 = new List<DuelCard>();
    private readonly List<DuelCard> _handP2 = new List<DuelCard>();

    private readonly List<DuelCard> _discardP1 = new List<DuelCard>();
    private readonly List<DuelCard> _discardP2 = new List<DuelCard>();

    private readonly List<DuelCard> _banishP1 = new List<DuelCard>();
    private readonly List<DuelCard> _banishP2 = new List<DuelCard>();

    private readonly List<DuelCard> _setAsideP1 = new List<DuelCard>();
    private readonly List<DuelCard> _setAsideP2 = new List<DuelCard>();

    private readonly TerrainSlot[] _slots;

    public IReadOnlyList<DuelCard> DeckP1 => _deckP1;
    public IReadOnlyList<DuelCard> DeckP2 => _deckP2;

    public IReadOnlyList<DuelCard> HandP1 => _handP1;
    public IReadOnlyList<DuelCard> HandP2 => _handP2;

    public IReadOnlyList<DuelCard> DiscardP1 => _discardP1;
    public IReadOnlyList<DuelCard> DiscardP2 => _discardP2;

    public IReadOnlyList<DuelCard> BanishP1 => _banishP1;
    public IReadOnlyList<DuelCard> BanishP2 => _banishP2;

    public IReadOnlyList<DuelCard> SetAsideP1 => _setAsideP1;
    public IReadOnlyList<DuelCard> SetAsideP2 => _setAsideP2;

    public IReadOnlyList<TerrainSlot> Slots => _slots;

    public int TerrainsInPlayCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty) count++;
            }
            return count;
        }
    }

    public IReadOnlyList<DuelCard> GetDeck(PlayerId player) =>
        player == PlayerId.Player1 ? DeckP1 : DeckP2;

    public IReadOnlyList<DuelCard> GetHand(PlayerId player) =>
        player == PlayerId.Player1 ? HandP1 : HandP2;

    public IReadOnlyList<DuelCard> GetDiscard(PlayerId player) =>
        player == PlayerId.Player1 ? DiscardP1 : DiscardP2;

    public IReadOnlyList<DuelCard> GetBanish(PlayerId player) =>
        player == PlayerId.Player1 ? BanishP1 : BanishP2;

    public IReadOnlyList<DuelCard> GetSetAside(PlayerId player) =>
        player == PlayerId.Player1 ? SetAsideP1 : SetAsideP2;

    public PlayerId ActivePlayer { get; private set; } = PlayerId.Player1;
    public int TurnNumber { get; private set; } = 0;

    public int CharactersPlayedThisTurn { get; private set; } = 0;

    public int ScoreP1 { get; private set; } = 0;
    public int ScoreP2 { get; private set; } = 0;

    public int GetScore(PlayerId player) =>
        player == PlayerId.Player1 ? ScoreP1 : ScoreP2;

    public bool IsGameOver { get; private set; } = false;
    public PlayerId? Winner { get; private set; } = null;
    public GameOverReason? GameOverReason { get; private set; } = null;

    public bool TerrainRequirementPending { get; private set; } = false;

    public DuelRules Rules { get; }

    public DuelState(DuelRules rules)
    {
        Rules = rules ?? new DuelRules();
        _slots = new TerrainSlot[Rules.MaxTerrainsOnBoard];
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = new TerrainSlot(i);
        }
    }

    // --- Méthodes de mutation réservées exclusivement au DuelEngine ---

    internal void AddDeckCards(PlayerId player, IEnumerable<DuelCard> cards)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        target.AddRange(cards);
    }

    internal void ShuffleDeck(PlayerId player, IRandom rng)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        rng.Shuffle(target);
    }

    internal bool RemoveCardFromDeck(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        return target.Remove(card);
    }

    internal DuelCard? PopTopDeckCard(PlayerId player)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        if (target.Count == 0) return null;
        var card = target[0];
        target.RemoveAt(0);
        return card;
    }

    internal void AddCardToHand(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _handP1 : _handP2;
        target.Add(card);
    }

    internal bool RemoveCardFromHand(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _handP1 : _handP2;
        return target.Remove(card);
    }

    internal void AddCardToBottomDeck(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        target.Add(card);
    }

    internal void AddCardsToBottomDeck(PlayerId player, IEnumerable<DuelCard> cards)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        target.AddRange(cards);
    }

    internal void AddCardToDiscard(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _discardP1 : _discardP2;
        target.Add(card);
    }

    internal void AddCardsToDiscard(PlayerId player, IEnumerable<DuelCard> cards)
    {
        var target = player == PlayerId.Player1 ? _discardP1 : _discardP2;
        target.AddRange(cards);
    }

    internal void RemoveCardFromDiscard(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _discardP1 : _discardP2;
        target.Remove(card);
    }

    internal void AddCardToBanish(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _banishP1 : _banishP2;
        target.Add(card);
    }

    internal void AddCardsToBanish(PlayerId player, IEnumerable<DuelCard> cards)
    {
        var target = player == PlayerId.Player1 ? _banishP1 : _banishP2;
        target.AddRange(cards);
    }

    internal void AddCardToSetAside(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _setAsideP1 : _setAsideP2;
        target.Add(card);
    }

    internal void AddCardsToSetAside(PlayerId player, IEnumerable<DuelCard> cards)
    {
        var target = player == PlayerId.Player1 ? _setAsideP1 : _setAsideP2;
        target.AddRange(cards);
    }

    internal void RemoveCardFromSetAside(PlayerId player, DuelCard card)
    {
        var target = player == PlayerId.Player1 ? _setAsideP1 : _setAsideP2;
        target.Remove(card);
    }

    internal void ClearSetAside(PlayerId player)
    {
        var target = player == PlayerId.Player1 ? _setAsideP1 : _setAsideP2;
        target.Clear();
    }


    internal void SetSlot(int slotIndex, DuelCard? card, int? placedOnTurn, PlayerId? placedBy = null)
    {
        _slots[slotIndex] = new TerrainSlot(slotIndex, card, placedOnTurn, placedBy);
    }

    internal void ClearSlot(int slotIndex)
    {
        _slots[slotIndex] = new TerrainSlot(slotIndex);
    }


    internal bool MoveCharacter(PlayerId player, DuelCard card, int fromSlotIndex, int toSlotIndex, int insertIndex = -1)
    {
        if (fromSlotIndex < 0 || fromSlotIndex >= _slots.Length) return false;
        if (toSlotIndex < 0 || toSlotIndex >= _slots.Length) return false;

        var fromSlot = _slots[fromSlotIndex];
        var toSlot = _slots[toSlotIndex];

        if (fromSlot.RemoveCharacter(player, card))
        {
            toSlot.AddCharacter(player, card);
            return true;
        }
        return false;
    }

    internal DuelCard? PopBottomDeckCard(PlayerId player)
    {
        var target = player == PlayerId.Player1 ? _deckP1 : _deckP2;
        if (target.Count == 0) return null;
        var card = target[target.Count - 1];
        target.RemoveAt(target.Count - 1);
        return card;
    }

    internal void AddCharacterToSlot(int slotIndex, PlayerId player, DuelCard card, int insertIndex = -1)
    {
        _slots[slotIndex].AddCharacter(player, card, insertIndex);
    }

    internal void IncrementCharactersPlayed() => CharactersPlayedThisTurn++;

    internal void ResetCharactersPlayed() => CharactersPlayedThisTurn = 0;

    internal void SetActivePlayer(PlayerId player) => ActivePlayer = player;

    internal void IncrementTurnNumber() => TurnNumber++;

    internal void SetTerrainRequirementPending(bool pending) => TerrainRequirementPending = pending;

    internal void SetGameOver(PlayerId winner, GameOverReason reason)
    {
        IsGameOver = true;
        Winner = winner;
        GameOverReason = reason;
    }

    internal void AddScore(PlayerId player, int points)
    {
        if (player == PlayerId.Player1) ScoreP1 += points;
        else ScoreP2 += points;
    }
}
