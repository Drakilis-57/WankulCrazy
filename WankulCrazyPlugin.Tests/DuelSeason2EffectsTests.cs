using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
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

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "data", "customdecks", "effects_s2.json")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate repo root containing data/customdecks/effects_s2.json");
    }

    [Fact]
    public void EffectsS2Json_Has155ValidEntries_CoveringAllCampusCards()
    {
        var path = Path.Combine(FindRepoRoot(), "data", "customdecks", "effects_s2.json");
        var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        var arr = JArray.Parse(json);

        Assert.Equal(155, arr.Count);

        var expectedIndices = Enumerable.Range(200001, 155).ToList();
        var actualIndices = arr.Select(t => t["cardIndex"]!.Value<int>()).ToList();
        Assert.Equal(expectedIndices, actualIndices);

        string[] requiredArrays = { "onPlay", "combo", "passive", "sub", "win", "lose" };
        string[] forbiddenBoosts = { "boost_self_15", "boost_self_20", "boost_self_30", "boost_self_45", "boost_self_60", "boost_self_75", "boost_self_90" };

        foreach (var item in arr)
        {
            Assert.NotNull(item["costOverride"]);
            foreach (var key in requiredArrays)
            {
                var field = item[key] as JArray;
                Assert.NotNull(field);
                foreach (var eff in field!)
                {
                    var id = eff.Value<string>() ?? "";
                    Assert.False(string.IsNullOrWhiteSpace(id));
                    Assert.DoesNotContain(id, forbiddenBoosts);
                }
            }
        }
    }

    [Fact]
    public void EffectsS2Json_KeyCampusMechanics_AreMappedCorrectly()
    {
        var path = Path.Combine(FindRepoRoot(), "data", "customdecks", "effects_s2.json");
        var arr = JArray.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
        var byIndex = arr.ToDictionary(t => t["cardIndex"]!.Value<int>());

        // 200017 LOOTBOX: Set Aside
        Assert.Contains("set_aside_top_3", byIndex[200017]["onPlay"]!.Values<string>());
        Assert.Contains("take_setaside_all", byIndex[200017]["win"]!.Values<string>());
        Assert.Contains("banish_setaside_all", byIndex[200017]["lose"]!.Values<string>());

        // 200018 DALTONISME
        Assert.Contains("passive_terrain_daltonisme", byIndex[200018]["passive"]!.Values<string>());
        Assert.Contains("lose_mill_3", byIndex[200018]["lose"]!.Values<string>());

        // 200043 / 200044 LIVREUR Orange / Violet (Sub)
        Assert.Contains("sub_play_on_opp_orange_combo", byIndex[200043]["sub"]!.Values<string>());
        Assert.Contains("sub_play_on_opp_violet_combo", byIndex[200044]["sub"]!.Values<string>());

        // Win / Lose Campus terrains
        Assert.Contains("win_draw_until_odd", byIndex[200003]["win"]!.Values<string>());
        Assert.Contains("win_take_discard_min_force", byIndex[200005]["win"]!.Values<string>());
        Assert.Contains("lose_mill_max_force", byIndex[200005]["lose"]!.Values<string>());
        Assert.Contains("win_draw_or_put_under_deck", byIndex[200012]["win"]!.Values<string>());
        Assert.Contains("lose_discard_char_in_play", byIndex[200013]["lose"]!.Values<string>());
        Assert.Contains("win_shuffle_hand_draw_5", byIndex[200016]["win"]!.Values<string>());
        Assert.Contains("lose_banish_deck_hand_4", byIndex[200016]["lose"]!.Values<string>());
        Assert.Contains("win_put_under_deck", byIndex[200020]["win"]!.Values<string>());
        Assert.Contains("lose_mill_5_minus_drawn", byIndex[200020]["lose"]!.Values<string>());
        Assert.Contains("lose_banish_discard_5", byIndex[200022]["lose"]!.Values<string>());

        // 200047 VIKING: boost_self_3
        Assert.Contains("boost_self_3", byIndex[200047]["combo"]!.Values<string>());
    }

    [Fact]
    public void PutDiscardUnderDeckEffect_MovesUpTo5CardsToBottomOfDeck()
    {
        var rules = new DuelRules();
        var engine = new DuelEngine(new NoShuffleRandom(), rules);
        var state = engine.State;

        for (int i = 1; i <= 6; i++)
            state.AddCardToDiscard(PlayerId.Player1, new DuelCard($"D{i}", $"Card {i}", CardKind.Character));

        var effect = new PutDiscardUnderDeckEffect("win_put_under_deck");
        var ctx = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "THEATRE", CardKind.Terrain), 0);

        effect.Execute(ctx);

        Assert.Single(state.DiscardP1);
        Assert.Equal(5, state.DeckP1.Count);
    }

    [Fact]
    public void MillSelfByMaxForceEffect_MillsByHighestBaseForceInPlay()
    {
        var rules = new DuelRules();
        var engine = new DuelEngine(new NoShuffleRandom(), rules);
        var state = engine.State;

        for (int i = 1; i <= 5; i++)
            state.AddDeckCards(PlayerId.Player1, new[] { new DuelCard($"K{i}", $"K{i}", CardKind.Character) });

        state.SetSlot(0, new DuelCard("T", "SALLE AUX TRESORS", CardKind.Terrain), 0, PlayerId.Player1);
        state.Slots[0].AddCharacter(PlayerId.Player2, new DuelCard("C_Opp", "Opp", CardKind.Character, force: 3));

        var effect = new MillSelfByMaxForceEffect("lose_mill_max_force");
        var ctx = new EffectContext(engine, PlayerId.Player1, state.Slots[0].Card!, 0);

        effect.Execute(ctx);

        Assert.Equal(2, state.DeckP1.Count);
        Assert.Equal(3, state.DiscardP1.Count);
    }
}
