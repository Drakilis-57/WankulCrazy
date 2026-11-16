using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
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
        typeof(DuelState).GetMethod("IncrementTurnNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(engine.State, null);
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
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
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
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
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
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
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
            if (evt is PlayerChoiceRequiredEvent choiceEvt && choiceEvt.ChoiceContext == "move_character" && choiceEvt.SourceCard?.Id == "C1")
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

    private sealed class Season4EffectEntry
    {
        public int cardIndex { get; set; }
        public int? costOverride { get; set; }
        public List<string> onPlay { get; set; } = new List<string>();
        public List<string> combo { get; set; } = new List<string>();
        public List<string> passive { get; set; } = new List<string>();
        public List<string> sub { get; set; } = new List<string>();
        public List<string> win { get; set; } = new List<string>();
        public List<string> lose { get; set; } = new List<string>();
    }

    private static string ResolveDataPath(params string[] relativeParts)
    {
        var repoRelative = Path.Combine(new[] { "..", "..", "..", ".." }.Concat(relativeParts).ToArray());
        if (File.Exists(repoRelative)) return repoRelative;

        var direct = Path.Combine(relativeParts);
        if (File.Exists(direct)) return direct;

        return Path.Combine(new[] { AppDomain.CurrentDomain.BaseDirectory }.Concat(relativeParts).ToArray());
    }

    private static List<Season4EffectEntry> LoadEffectsS4()
    {
        string path = ResolveDataPath("data", "customdecks", "effects_s4.json");
        Assert.True(File.Exists(path), $"effects_s4.json not found at {path}");
        string json = File.ReadAllText(path, Encoding.UTF8);
        var entries = JsonConvert.DeserializeObject<List<Season4EffectEntry>>(json);
        Assert.NotNull(entries);
        return entries!;
    }

    [Fact]
    public void EffectsS4Json_ContainsAll180StellarCards_WithValidStructure()
    {
        var entries = LoadEffectsS4();
        Assert.Equal(180, entries.Count);

        var expectedIndices = Enumerable.Range(400001, 180).ToList();
        Assert.Equal(expectedIndices, entries.Select(e => e.cardIndex).ToList());

        foreach (var e in entries)
        {
            Assert.NotNull(e.onPlay);
            Assert.NotNull(e.combo);
            Assert.NotNull(e.passive);
            Assert.NotNull(e.sub);
            Assert.NotNull(e.win);
            Assert.NotNull(e.lose);

            int totalEffects = e.onPlay.Count + e.combo.Count + e.passive.Count + e.sub.Count + e.win.Count + e.lose.Count;
            Assert.True(totalEffects > 0, $"Card {e.cardIndex} has no mapped effects.");
        }
    }

    [Fact]
    public void EffectsS4Json_RespectsGemsAndScrapedEdgeCases()
    {
        var byIndex = LoadEffectsS4().ToDictionary(e => e.cardIndex);

        // #400050 & #400164 À la recherche du respect: no gems -> passive only, combo must be empty
        foreach (int idx in new[] { 400050, 400164 })
        {
            Assert.Empty(byIndex[idx].combo);
            Assert.Contains("passive_no_discard_when_alone", byIndex[idx].passive);
            Assert.Contains("passive_on_mill_banish_opp_mill_2", byIndex[idx].passive);
        }

        // #400062 & #400172 Tarot (Terracid): has gem CO and combo draw_3
        foreach (int idx in new[] { 400062, 400172 })
        {
            Assert.Contains("draw_3", byIndex[idx].combo);
            Assert.Contains("passive_on_sent_from_play_to_discard_play_free_cost_ge_2", byIndex[idx].passive);
        }

        // #400004 Salon de Coiffure
        Assert.Contains("passive_forbid_no_gem_chars", byIndex[400004].passive);
        Assert.Contains("lose_mill_4", byIndex[400004].lose);

        // #400053 & #400054 Challenger Battle Royal
        Assert.Contains("passive_cannot_discard_or_move_gem_connected", byIndex[400053].combo);
        Assert.Contains("passive_cannot_discard_or_move_gem_connected", byIndex[400054].combo);
    }

    [Fact]
    public void EffectsS4Json_VerifiesStellarMechanics_StickFight_Voyante_DenueDeclat_Tatouage()
    {
        var byIndex = LoadEffectsS4().ToDictionary(e => e.cardIndex);

        // Stick Fight (#400035 Laink, #400036 Terracid)
        Assert.Contains("draw_3_if_aligned_column", byIndex[400035].onPlay);
        Assert.Contains("score_any_terrain", byIndex[400035].combo);
        Assert.Contains("draw_3_if_aligned_column", byIndex[400036].onPlay);

        // Voyante (#400107 / #400165 Laink, #400108 / #400166 Terracid)
        foreach (int idx in new[] { 400107, 400165 })
        {
            Assert.Contains("look_opp_hand", byIndex[idx].onPlay);
            Assert.Contains("guess_top_deck_name_play_free", byIndex[idx].combo);
        }
        foreach (int idx in new[] { 400108, 400166 })
        {
            Assert.Contains("look_top_deck_5_reorder", byIndex[idx].onPlay);
        }

        // Dénué d'Éclat (#400147 / #400177 Laink, #400148 / #400178 Terracid)
        foreach (int idx in new[] { 400147, 400177 })
        {
            Assert.Contains("bounce_own_char_draw_1", byIndex[idx].onPlay);
            Assert.Empty(byIndex[idx].combo);
        }
        foreach (int idx in new[] { 400148, 400178 })
        {
            Assert.Contains("bounce_own_char_draw_1", byIndex[idx].onPlay);
            Assert.Contains("banish_opp_discard_3", byIndex[idx].combo);
        }

        // Tatouage (#400101 Laink -> orange, #400102 Terracid -> violet)
        Assert.Contains("passive_terrain_grant_orange_gems", byIndex[400101].passive);
        Assert.Contains("passive_terrain_grant_violet_gems", byIndex[400102].passive);
    }

    [Fact]
    public void WinDraw2And3_AndLoseMill6_ExecuteCorrectlyInEngine()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;
        var p2 = PlayerId.Player2;

        int p1HandBefore = engine.State.GetHand(p1).Count;
        int p2DeckBefore = engine.State.GetDeck(p2).Count;

        var terrain = new DuelCard("T_TEST", "Placard", CardKind.Terrain, effectIds: new[] { "win_draw_3", "lose_mill_6" });
        var slot = new TerrainSlot(0, terrain, placedOnTurn: 0, placedBy: p1);
        var slotsField = typeof(DuelState).GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var slotsArray = (TerrainSlot[])slotsField.GetValue(engine.State)!;
        slotsArray[0] = slot;

        slot.AddCharacter(p1, new DuelCard("C1", "Winner", CardKind.Character, force: 10));
        slot.AddCharacter(p2, new DuelCard("C2", "Loser", CardKind.Character, force: 1));

        engine.ResolveScore(0);

        Assert.Equal(p1HandBefore + 3, engine.State.GetHand(p1).Count);
        Assert.Equal(p2DeckBefore - 6, engine.State.GetDeck(p2).Count);
    }
}
