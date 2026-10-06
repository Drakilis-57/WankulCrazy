using System;
using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.duel;
using WankulCrazyPlugin.cards;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    [Collection("Sequential")]
    public class DuelSeason3EffectsTests
    {
        [Fact]
        public void Execute_SelfMillEffect_MillsCorrectAmount()
        {
            var rng = new SystemRandomAdapter();
            var engine = new DuelEngine(rng);

            var deckP1 = new List<DuelCard>();
            for(int i=0; i<30; i++) deckP1.Add(new DuelCard($"C{i}", "C", CardKind.Character));
            var deckP2 = new List<DuelCard>();
            for(int i=0; i<30; i++) deckP2.Add(new DuelCard($"C{i}", "C", CardKind.Character));

            engine.StartDuel(deckP1, deckP2);

            int initialDeckSize = engine.State.GetDeck(PlayerId.Player1).Count;
            int initialDiscardSize = engine.State.GetDiscard(PlayerId.Player1).Count;

            var effect = new SelfMillEffect("self_mill_2", 2);
            var context = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "T", CardKind.Terrain), 0);

            effect.Execute(context);

            Assert.Equal(initialDeckSize - 2, engine.State.GetDeck(PlayerId.Player1).Count);
            Assert.Equal(initialDiscardSize + 2, engine.State.GetDiscard(PlayerId.Player1).Count);
        }
    }
}
