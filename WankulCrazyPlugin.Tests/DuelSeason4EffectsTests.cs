using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelSeason4EffectsTests
{
    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public int Next(int maxExclusive) => 0;
        public void Shuffle<T>(IList<T> list) { }
    }

    private DuelEngine CreateEngine()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 3, maxHandSize: 7, terrainsToWin: 3);

        var deck1 = new List<DuelCard>();
        var deck2 = new List<DuelCard>();

        // Fill decks with empty cards
        for (int i = 0; i < 20; i++)
        {
            deck1.Add(new DuelCard($"D1_{i}", "Dummy", CardKind.Character));
            deck2.Add(new DuelCard($"D2_{i}", "Dummy", CardKind.Character));
        }

        var engine = new DuelEngine(rng, rules);
        engine.StartDuel(deck1, deck2);
        // Force state to Turn 1 since StartDuel alone doesn't increment it directly, StartTurn does.
        typeof(DuelState).GetMethod("IncrementTurnNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(engine.State, null);
        return engine;
    }

    [Fact]
    public void WinDrawUntil7_FillsHandTo7Cards()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;

        // Ensure player 1 has only 2 cards
        while (engine.State.GetHand(p1).Count > 2)
        {
            engine.State.RemoveCardFromHand(p1, engine.State.GetHand(p1)[0]);
        }
        Assert.Equal(2, engine.State.GetHand(p1).Count);

        var terrain = new DuelCard("T_CYBERPUNK", "Ville Cyberpunk", CardKind.Terrain, effectIds: new[] { "win_draw_until_7" });
        var slot = new TerrainSlot(0, terrain, placedOnTurn: 0, placedBy: p1);
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slotsArray = (TerrainSlot[])slotsField.GetValue(engine.State)!;
        slotsArray[0] = slot;

        var charP1 = new DuelCard("C1", "Winner", CardKind.Character, force: 10);
        var charP2 = new DuelCard("C2", "Loser", CardKind.Character, force: 0);

        slot.AddCharacter(PlayerId.Player1, charP1);
        slot.AddCharacter(PlayerId.Player2, charP2);

        engine.ResolveScore(0); // P1 wins because 10 > 0

        // P1 should have drawn exactly 5 cards to reach 7
        Assert.Equal(7, engine.State.GetHand(p1).Count);
    }

    [Fact]
    public void LoseMill4_Mills4CardsFromLoser()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;
        var p2 = PlayerId.Player2;

        var terrain = new DuelCard("T_LUCKY", "Lucky Block", CardKind.Terrain, effectIds: new[] { "lose_mill_4" });
        var slot = new TerrainSlot(0, terrain, placedOnTurn: 0, placedBy: p1);
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slotsArray = (TerrainSlot[])slotsField.GetValue(engine.State)!;
        slotsArray[0] = slot;

        var charP1 = new DuelCard("C1", "Winner", CardKind.Character, force: 10);
        var charP2 = new DuelCard("C2", "Loser", CardKind.Character, force: 0);

        slot.AddCharacter(p1, charP1);
        slot.AddCharacter(p2, charP2);

        int initialDeckSize = engine.State.GetDeck(p2).Count;
        int initialDiscardSize = engine.State.GetDiscard(p2).Count;

        engine.ResolveScore(0); // P1 wins, P2 loses

        Assert.Equal(initialDeckSize - 4, engine.State.GetDeck(p2).Count);
        // initialDiscardSize + 4 milled cards + 1 charP2 played on slot
        Assert.Equal(initialDiscardSize + 4 + 1, engine.State.GetDiscard(p2).Count);
    }

    [Fact]
    public void LoseDiscard3_Discards3CardsFromLoserHand()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;
        var p2 = PlayerId.Player2;

        // Give p2 at least 3 cards
        while (engine.State.GetHand(p2).Count < 5)
        {
            engine.State.AddCardToHand(p2, new DuelCard("H", "Hand", CardKind.Character));
        }
        int initialHandSize = engine.State.GetHand(p2).Count;
        int initialDiscardSize = engine.State.GetDiscard(p2).Count;

        var terrain = new DuelCard("T_COBBLE", "Cobblestone", CardKind.Terrain, effectIds: new[] { "lose_discard_3" });
        var slot = new TerrainSlot(0, terrain, placedOnTurn: 0, placedBy: p1);
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slotsArray = (TerrainSlot[])slotsField.GetValue(engine.State)!;
        slotsArray[0] = slot;

        var charP1 = new DuelCard("C1", "Winner", CardKind.Character, force: 10);
        var charP2 = new DuelCard("C2", "Loser", CardKind.Character, force: 0);

        slot.AddCharacter(p1, charP1);
        slot.AddCharacter(p2, charP2);

        engine.ResolveScore(0); // P1 wins, P2 loses

        Assert.Equal(initialHandSize - 3, engine.State.GetHand(p2).Count);
        // discard increases by 3 (from hand) + 1 (character played)
        Assert.Equal(initialDiscardSize + 3 + 1, engine.State.GetDiscard(p2).Count);
    }


    [Fact]
    public void MoveCharacterEffect_EmitsPlayerChoiceEvent()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;

        var charCard = new DuelCard("C1", "Mover", CardKind.Character, effectIds: new[] { "move_character" });

        bool eventFired = false;
        engine.OnEvent += (evt) =>
        {
            if (evt is PlayerChoiceRequiredEvent choiceEvt && choiceEvt.ChoiceContext == "move_character" && choiceEvt.SourceCard.Id == "C1")
            {
                eventFired = true;
            }
        };

        var effect = new MoveCharacterEffect("move_character");
        var ctx = new EffectContext(engine, p1, charCard, 0);
        effect.Execute(ctx);

        Assert.True(eventFired, "PlayerChoiceRequiredEvent should have been emitted.");
    }


    [Fact]
    public void AddBottomDeckToHandEffect_WorksCorrectly()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;

        // Ensure we know the bottom card. Deck is populated in CreateEngine.
        var deck = engine.State.GetDeck(p1);
        var expectedCard = deck[deck.Count - 1];

        int initialHandCount = engine.State.GetHand(p1).Count;

        var effect = new AddBottomDeckToHandEffect("add_bottom_deck_to_hand");
        var ctx = new EffectContext(engine, p1, new DuelCard("X", "X", CardKind.Character), 0);
        effect.Execute(ctx);

        // Should be added to hand
        var hand = engine.State.GetHand(p1);
        Assert.Equal(initialHandCount + 1, hand.Count);

        // Assert it's the exact expected card
        Assert.Same(expectedCard, hand.Last());
    }
}
