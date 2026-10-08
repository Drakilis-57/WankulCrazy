using System;
using System.Collections.Generic;
using Xunit;
using WankulCrazy.Duel.Engine;

namespace WankulCrazyPlugin.Tests
{
    public class BottomDeckTests
    {
        private sealed class TestRandom : IRandom
        {
            public int Next(int maxValue) => 0;
            public int Next(int minValue, int maxValue) => minValue;
            public void Shuffle<T>(IList<T> list) { }
        }

        [Fact]
        public void PopBottomDeckCard_ReturnsAndRemovesLastCard()
        {
            var engine = new DuelEngine(new TestRandom());
            var state = engine.State;

            var card1 = new DuelCard("1", "Top", CardKind.Character);
            var card2 = new DuelCard("2", "Middle", CardKind.Character);
            var card3 = new DuelCard("3", "Bottom", CardKind.Character);

            // Directly add cards to P1 deck
            state.AddCardsToBottomDeck(PlayerId.Player1, new List<DuelCard> { card1, card2, card3 });

            var popped = state.PopBottomDeckCard(PlayerId.Player1);

            Assert.NotNull(popped);
            Assert.Equal("3", popped.Id);
            Assert.Equal("Bottom", popped.Name);

            var deck = state.GetDeck(PlayerId.Player1);
            Assert.Equal(2, deck.Count);
            Assert.Equal("1", deck[0].Id);
            Assert.Equal("2", deck[1].Id);
        }

        [Fact]
        public void PopTopDeckCard_ReturnsAndRemovesFirstCard()
        {
            var engine = new DuelEngine(new TestRandom());
            var state = engine.State;

            var card1 = new DuelCard("1", "Top", CardKind.Character);
            var card2 = new DuelCard("2", "Middle", CardKind.Character);
            var card3 = new DuelCard("3", "Bottom", CardKind.Character);

            state.AddCardsToBottomDeck(PlayerId.Player1, new List<DuelCard> { card1, card2, card3 });

            var popped = state.PopTopDeckCard(PlayerId.Player1);

            Assert.NotNull(popped);
            Assert.Equal("1", popped.Id);
            Assert.Equal("Top", popped.Name);

            var deck = state.GetDeck(PlayerId.Player1);
            Assert.Equal(2, deck.Count);
            Assert.Equal("2", deck[0].Id);
            Assert.Equal("3", deck[1].Id);
        }
    }
}
