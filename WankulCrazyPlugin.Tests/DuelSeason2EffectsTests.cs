using System;
using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelSeason2EffectsTests
{
    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int min, int max) => min;
        public int Next(int max) => 0;
        public int Range(int min, int max) => min;
        public void Shuffle<T>(IList<T> list) { }
    }

    [Fact]
    public void BanishDeckEffect_BanishesCardsFromDeck()
    {
        var rules = new DuelRules();
        var engine = new DuelEngine(new NoShuffleRandom(), rules);
        var state = engine.State;

        var cardToBanish = new DuelCard("C1", "Target", CardKind.Character);
        state.AddDeckCards(PlayerId.Player1, new[] { cardToBanish });

        var effect = new BanishDeckEffect("test_banish_deck", 1, targetOpponent: false);
        var ctx = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "T", CardKind.Terrain), 0);

        effect.Execute(ctx);

        Assert.Empty(state.DeckP1);
        Assert.Single(state.BanishP1);
        Assert.Equal(cardToBanish, state.BanishP1[0]);
    }

    [Fact]
    public void DrawUntilOddEffect_DrawsUntilOddCard()
    {
        var rules = new DuelRules();
        var engine = new DuelEngine(new NoShuffleRandom(), rules);
        var state = engine.State;

        var evenCard = new DuelCard("C1", "Even", CardKind.Character, force: 2);
        var oddCard = new DuelCard("C2", "Odd", CardKind.Character, force: 3);
        state.AddDeckCards(PlayerId.Player1, new[] { evenCard, oddCard });

        var effect = new DrawUntilOddEffect("test_draw_odd");
        var ctx = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "T", CardKind.Terrain), 0);

        effect.Execute(ctx);

        Assert.Empty(state.DeckP1);
        Assert.Equal(2, state.HandP1.Count);
        Assert.Equal(evenCard, state.HandP1[0]);
        Assert.Equal(oddCard, state.HandP1[1]);
    }

    [Fact]
    public void TakeTopDiscardEffect_MovesCardFromDiscardToHand()
    {
        var rules = new DuelRules();
        var engine = new DuelEngine(new NoShuffleRandom(), rules);
        var state = engine.State;

        var cardToTake = new DuelCard("C1", "Target", CardKind.Character);
        state.AddCardToDiscard(PlayerId.Player1, cardToTake);

        var effect = new TakeTopDiscardEffect("test_take_discard", 1);
        var ctx = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "T", CardKind.Terrain), 0);

        effect.Execute(ctx);

        Assert.Empty(state.DiscardP1);
        Assert.Single(state.HandP1);
        Assert.Equal(cardToTake, state.HandP1[0]);
    }

    [Fact]
    public void PlayCharacterFreeEffect_EmitsChoiceEvent()
    {
        var rules = new DuelRules();
        var engine = new DuelEngine(new NoShuffleRandom(), rules);
        var state = engine.State;

        var charCard = new DuelCard("C1", "Char", CardKind.Character);
        state.AddCardToHand(PlayerId.Player1, charCard);

        bool eventFired = false;
        engine.OnEvent += e =>
        {
            if (e is PlayerChoiceRequiredEvent pce)
            {
                eventFired = true;
                Assert.Equal(PlayerId.Player1, pce.Player);
                Assert.Single(pce.Candidates);
                Assert.Equal(charCard, pce.Candidates[0]);

                // Simulate player making choice
                pce.OnCardSelected?.Invoke(charCard);
            }
        };

        var effect = new PlayCharacterFreeEffect("test_play_free");
        var ctx = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "T", CardKind.Terrain), 0);

        effect.Execute(ctx);

        Assert.True(eventFired);
        Assert.Empty(state.HandP1);
        Assert.Single(state.DiscardP1); // Mocked to go to discard in effect
    }
}
