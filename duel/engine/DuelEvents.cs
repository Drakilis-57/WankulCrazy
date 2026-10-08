using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public enum PlayerId
{
    Player1 = 1,
    Player2 = 2
}

public static class PlayerIdExtensions
{
    public static PlayerId Opponent(this PlayerId player) =>
        player == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
}

public enum GameOverReason
{
    DeckOut,
    FiveTerrains
}

public interface IDuelEvent
{
}

public sealed class DuelStartedEvent : IDuelEvent
{
    public PlayerId StartingPlayer { get; }
    public DuelStartedEvent(PlayerId startingPlayer) => StartingPlayer = startingPlayer;
}

public sealed class TurnStartedEvent : IDuelEvent
{
    public PlayerId ActivePlayer { get; }
    public int TurnNumber { get; }
    public TurnStartedEvent(PlayerId activePlayer, int turnNumber)
    {
        ActivePlayer = activePlayer;
        TurnNumber = turnNumber;
    }
}

public sealed class CardsDrawnEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public IReadOnlyList<DuelCard> Cards { get; }
    public CardsDrawnEvent(PlayerId player, IReadOnlyList<DuelCard> cards)
    {
        Player = player;
        Cards = cards;
    }
}

public sealed class DeckExhaustedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public DeckExhaustedEvent(PlayerId player) => Player = player;
}

public sealed class TerrainPlacementRequiredEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public TerrainPlacementRequiredEvent(PlayerId player) => Player = player;
}

public sealed class TerrainPlayedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public int SlotIndex { get; }
    public DuelCard Card { get; }
    public TerrainPlayedEvent(PlayerId player, int slotIndex, DuelCard card)
    {
        Player = player;
        SlotIndex = slotIndex;
        Card = card;
    }
}

public sealed class TerrainRevealedByDigEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public int SlotIndex { get; }
    public DuelCard Card { get; }
    public IReadOnlyList<DuelCard> RevealedCardsPutUnderDeck { get; }

    public TerrainRevealedByDigEvent(
        PlayerId player,
        int slotIndex,
        DuelCard card,
        IReadOnlyList<DuelCard> revealedCardsPutUnderDeck)
    {
        Player = player;
        SlotIndex = slotIndex;
        Card = card;
        RevealedCardsPutUnderDeck = revealedCardsPutUnderDeck;
    }
}

public sealed class TerrainDigFailedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public TerrainDigFailedEvent(PlayerId player) => Player = player;
}

public sealed class TerrainsReadiedEvent : IDuelEvent
{
    public int TurnNumber { get; }
    public IReadOnlyList<int> ReadiedSlotIndices { get; }

    public TerrainsReadiedEvent(int turnNumber, IReadOnlyList<int> readiedSlotIndices)
    {
        TurnNumber = turnNumber;
        ReadiedSlotIndices = readiedSlotIndices;
    }
}

public sealed class ComboFormedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public int SlotIndex { get; }
    public DuelCard LeftCard { get; }
    public DuelCard RightCard { get; }

    public ComboFormedEvent(PlayerId player, int slotIndex, DuelCard leftCard, DuelCard rightCard)
    {
        Player = player;
        SlotIndex = slotIndex;
        LeftCard = leftCard;
        RightCard = rightCard;
    }
}

public sealed class ComboBrokenEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public int SlotIndex { get; }
    public DuelCard CardWithBrokenCombo { get; }

    public ComboBrokenEvent(PlayerId player, int slotIndex, DuelCard cardWithBrokenCombo)
    {
        Player = player;
        SlotIndex = slotIndex;
        CardWithBrokenCombo = cardWithBrokenCombo;
    }
}

public sealed class CharacterPlayedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public int SlotIndex { get; }
    public DuelCard Card { get; }

    public CharacterPlayedEvent(PlayerId player, int slotIndex, DuelCard card)
    {
        Player = player;
        SlotIndex = slotIndex;
        Card = card;
    }
}

public sealed class ScoreResolvedEvent : IDuelEvent
{
    public int SlotIndex { get; }
    public int ForceP1 { get; }
    public int ForceP2 { get; }
    public PlayerId? Winner { get; }

    public ScoreResolvedEvent(int slotIndex, int forceP1, int forceP2, PlayerId? winner)
    {
        SlotIndex = slotIndex;
        ForceP1 = forceP1;
        ForceP2 = forceP2;
        Winner = winner;
    }
}

public sealed class TerrainClearedEvent : IDuelEvent
{
    public int SlotIndex { get; }
    public DuelCard TerrainCard { get; }
    public IReadOnlyList<DuelCard> DiscardedP1 { get; }
    public IReadOnlyList<DuelCard> DiscardedP2 { get; }

    public TerrainClearedEvent(
        int slotIndex,
        DuelCard terrainCard,
        IReadOnlyList<DuelCard> discardedP1,
        IReadOnlyList<DuelCard> discardedP2)
    {
        SlotIndex = slotIndex;
        TerrainCard = terrainCard;
        DiscardedP1 = discardedP1;
        DiscardedP2 = discardedP2;
    }
}

public sealed class TurnEndedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public int TurnNumber { get; }
    public TurnEndedEvent(PlayerId player, int turnNumber)
    {
        Player = player;
        TurnNumber = turnNumber;
    }
}

public sealed class EffectTriggeredEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public string EffectId { get; }
    public DuelCard SourceCard { get; }
    public int SlotIndex { get; }

    public EffectTriggeredEvent(PlayerId player, string effectId, DuelCard sourceCard, int slotIndex)
    {
        Player = player;
        EffectId = effectId;
        SourceCard = sourceCard;
        SlotIndex = slotIndex;
    }
}

public sealed class CardsMilledEvent : IDuelEvent
{
    public PlayerId TargetPlayer { get; }
    public IReadOnlyList<DuelCard> MilledCards { get; }

    public CardsMilledEvent(PlayerId targetPlayer, IReadOnlyList<DuelCard> milledCards)
    {
        TargetPlayer = targetPlayer;
        MilledCards = milledCards;
    }
}

public sealed class CardsDiscardedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public IReadOnlyList<DuelCard> DiscardedCards { get; }

    public CardsDiscardedEvent(PlayerId player, IReadOnlyList<DuelCard> discardedCards)
    {
        Player = player;
        DiscardedCards = discardedCards;
    }
}


public sealed class CardsBanishedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public IReadOnlyList<DuelCard> BanishedCards { get; }

    public CardsBanishedEvent(PlayerId player, IReadOnlyList<DuelCard> banishedCards)
    {
        Player = player;
        BanishedCards = banishedCards;
    }
}

public sealed class CardsSetAsideEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public IReadOnlyList<DuelCard> SetAsideCards { get; }

    public CardsSetAsideEvent(PlayerId player, IReadOnlyList<DuelCard> setAsideCards)
    {
        Player = player;
        SetAsideCards = setAsideCards;
    }
}

public sealed class DuelEndedEvent : IDuelEvent

{
    public PlayerId Winner { get; }
    public GameOverReason Reason { get; }
    public DuelEndedEvent(PlayerId winner, GameOverReason reason)
    {
        Winner = winner;
        Reason = reason;
    }
}

public sealed class CharacterMovedSlotEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public DuelCard Card { get; }
    public int FromSlotIndex { get; }
    public int ToSlotIndex { get; }

    public CharacterMovedSlotEvent(PlayerId player, DuelCard card, int fromSlotIndex, int toSlotIndex)
    {
        Player = player;
        Card = card;
        FromSlotIndex = fromSlotIndex;
        ToSlotIndex = toSlotIndex;
    }
}

public sealed class PlayerChoiceRequiredEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<DuelCard> Candidates { get; }
    public Action<DuelCard>? OnCardSelected { get; }
    public string ChoiceContext { get; }
    public DuelCard? SourceCard { get; }

    public PlayerChoiceRequiredEvent(
        PlayerId player,
        string title,
        string description,
        IReadOnlyList<DuelCard> candidates,
        Action<DuelCard> onCardSelected)
    {
        Player = player;
        Title = title ?? "Choisissez une carte";
        Description = description ?? "";
        Candidates = candidates ?? Array.Empty<DuelCard>();
        OnCardSelected = onCardSelected;
        ChoiceContext = "";
        SourceCard = null;
    }

    public PlayerChoiceRequiredEvent(PlayerId player, string choiceContext, DuelCard sourceCard)
    {
        Player = player;
        Title = choiceContext ?? "Choix requis";
        Description = "";
        Candidates = Array.Empty<DuelCard>();
        OnCardSelected = null;
        ChoiceContext = choiceContext ?? "";
        SourceCard = sourceCard;
    }
}

public sealed class CardsRevealedEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public IReadOnlyList<DuelCard> RevealedCards { get; }

    public CardsRevealedEvent(PlayerId player, IReadOnlyList<DuelCard> revealedCards)
    {
        Player = player;
        RevealedCards = revealedCards;
    }
}

public sealed class PlayerSlotChoiceRequiredEvent : IDuelEvent
{
    public PlayerId Player { get; }
    public string Title { get; }
    public string Description { get; }
    public Action<int> OnSlotSelected { get; }

    public PlayerSlotChoiceRequiredEvent(
        PlayerId player,
        string title,
        string description,
        Action<int> onSlotSelected)
    {
        Player = player;
        Title = title ?? "Choisissez un terrain";
        Description = description ?? "";
        OnSlotSelected = onSlotSelected;
    }
}
