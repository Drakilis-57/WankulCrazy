using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep6Tests
{
    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public int Next(int maxExclusive) => 0;
        public void Shuffle<T>(IList<T> list) { }
    }

    [Fact]
    public void CardEffect_DrawX_DrawsExtraCardsUponPlay()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        // Perso avec effet draw_2
        var drawer = new DuelCard("C_Draw", "Piocheur", CardKind.Character, force: 100, effectIds: new[] { "draw_2" });

        var extra1 = new DuelCard("Extra1", "Extra 1", CardKind.Character);
        var extra2 = new DuelCard("Extra2", "Extra 2", CardKind.Character);

        engine.StartDuel(new[] { extra1, extra2 }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, drawer);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        var result = engine.PlayCharacter(PlayerId.Player1, "C_Draw", 0);
        Assert.True(result.Success);

        // L'effet draw_2 s'est déclenché
        Assert.Contains(events, e => e is EffectTriggeredEvent eff && eff.EffectId == "draw_2");

        // Le joueur a pioché les 2 cartes de son deck
        var hand = engine.State.GetHand(PlayerId.Player1);
        Assert.Equal(2, hand.Count);
        Assert.Contains(hand, c => c.Id == "Extra1");
        Assert.Contains(hand, c => c.Id == "Extra2");
        Assert.Empty(engine.State.GetDeck(PlayerId.Player1));
    }

    [Fact]
    public void CardEffect_MillX_DiscardsFromOpponentDeck()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var miller = new DuelCard("C_Mill", "Meuleur", CardKind.Character, force: 100, effectIds: new[] { "mill_3" });

        var opp1 = new DuelCard("Opp1", "Opp 1", CardKind.Character);
        var opp2 = new DuelCard("Opp2", "Opp 2", CardKind.Character);
        var opp3 = new DuelCard("Opp3", "Opp 3", CardKind.Character);
        var opp4 = new DuelCard("Opp4", "Opp 4", CardKind.Character);

        // Deck P2 : 4 cartes
        engine.StartDuel(new[] { terrain }, new[] { opp1, opp2, opp3, opp4 }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, miller);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        var result = engine.PlayCharacter(PlayerId.Player1, "C_Mill", 0);
        Assert.True(result.Success);

        // 3 cartes ont été meulées du dessus du deck adverse vers sa défausse
        var discardP2 = engine.State.GetDiscard(PlayerId.Player2);
        Assert.Equal(3, discardP2.Count);
        Assert.Equal("Opp1", discardP2[0].Id);
        Assert.Equal("Opp2", discardP2[1].Id);
        Assert.Equal("Opp3", discardP2[2].Id);

        // Il reste Opp4 dans le deck adverse
        var deckP2 = engine.State.GetDeck(PlayerId.Player2);
        Assert.Single(deckP2);
        Assert.Equal("Opp4", deckP2[0].Id);

        Assert.Contains(events, e => e is CardsMilledEvent m && m.TargetPlayer == PlayerId.Player2 && m.MilledCards.Count == 3);
        Assert.False(engine.State.IsGameOver);
    }

    [Fact]
    public void CardEffect_MillLethal_EndsGameWithDeckOut()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        // Effet mill_3 alors que l'adversaire n'a que 2 cartes
        var miller = new DuelCard("C_Mill", "Meuleur", CardKind.Character, force: 100, effectIds: new[] { "mill_3" });

        var opp1 = new DuelCard("Opp1", "Opp 1", CardKind.Character);
        var opp2 = new DuelCard("Opp2", "Opp 2", CardKind.Character);

        engine.StartDuel(new[] { terrain }, new[] { opp1, opp2 }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, miller);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        var result = engine.PlayCharacter(PlayerId.Player1, "C_Mill", 0);
        Assert.True(result.Success);

        // Les 2 cartes restantes sont allées en défausse
        Assert.Equal(2, engine.State.GetDiscard(PlayerId.Player2).Count);
        Assert.Empty(engine.State.GetDeck(PlayerId.Player2));

        // Victoire immédiate de Player 1 par DeckOut adverse !
        Assert.True(engine.State.IsGameOver);
        Assert.Equal(PlayerId.Player1, engine.State.Winner);
        Assert.Equal(GameOverReason.DeckOut, engine.State.GameOverReason);

        Assert.Contains(events, e => e is DeckExhaustedEvent dex && dex.Player == PlayerId.Player2);
        Assert.Contains(events, e => e is DuelEndedEvent ended && ended.Winner == PlayerId.Player1 && ended.Reason == GameOverReason.DeckOut);
    }

    [Fact]
    public void CardEffect_DiscardCards_DiscardsFromHand()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var discardeuse = new DuelCard("C_Disc", "Défausseuse", CardKind.Character, force: 100, effectIds: new[] { "discard_1" });
        var victimCard = new DuelCard("Victim", "Victime", CardKind.Character);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, victimCard);
        engine.State.AddCardToHand(PlayerId.Player1, discardeuse);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        // On joue discardeuse -> retire discardeuse de la main, puis effet discard_1 défausse victimCard
        var result = engine.PlayCharacter(PlayerId.Player1, "C_Disc", 0);
        Assert.True(result.Success);

        Assert.Empty(engine.State.GetHand(PlayerId.Player1));
        var discardP1 = engine.State.GetDiscard(PlayerId.Player1);
        Assert.Single(discardP1);
        Assert.Equal("Victim", discardP1[0].Id);

        Assert.Contains(events, e => e is CardsDiscardedEvent dc && dc.DiscardedCards.Count == 1);
    }

    [Fact]
    public void CardEffect_UnknownEffectId_GracefullyIgnoredWithoutCrash()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var strangeCard = new DuelCard("C_Strange", "Strange", CardKind.Character, force: 100, effectIds: new[] { "unknown_custom_effect_999" });

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, strangeCard);

        engine.StartTurn();

        var result = engine.PlayCharacter(PlayerId.Player1, "C_Strange", 0);
        Assert.True(result.Success);
        Assert.Single(engine.State.Slots[0].CharactersP1);
    }
}
