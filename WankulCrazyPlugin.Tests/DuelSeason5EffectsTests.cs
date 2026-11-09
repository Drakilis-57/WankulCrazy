using System;
using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.duel;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelSeason5EffectsTests
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
        var rules = new DuelRules(startingHandSize: 2, cardsDrawnPerTurn: 1, terrainsToWin: 3);

        var deck1 = new List<DuelCard>();
        var deck2 = new List<DuelCard>();
        for (int i = 0; i < 20; i++)
        {
            deck1.Add(new DuelCard($"D1_{i}", $"Card P1 {i}", CardKind.Character, force: 10));
            deck2.Add(new DuelCard($"D2_{i}", $"Card P2 {i}", CardKind.Character, force: 10));
        }

        var engine = new DuelEngine(rng, rules);
        engine.StartDuel(deck1, deck2, PlayerId.Player1);
        engine.StartTurn();
        return engine;
    }

    [Fact]
    public void EffectRegistry_Season5_ContainsAllLegacyEffects()
    {
        var registry = new EffectRegistry();
        Assert.True(registry.TryGetEffect("lose_banish_deck_6", out _));
        Assert.True(registry.TryGetEffect("lose_draw_7", out _));
        Assert.True(registry.TryGetEffect("boost_laink_terracid_3", out _));
        Assert.True(registry.TryGetEffect("boost_laink_terracid_30", out _));
        Assert.True(registry.TryGetEffect("terrain_combo_boost_1", out _));
        Assert.True(registry.TryGetEffect("terrain_combo_boost_10", out _));
        Assert.True(registry.TryGetEffect("terrain_on_char_played_draw_1", out _));
        Assert.True(registry.TryGetEffect("terrain_on_char_played_banish_top_1", out _));
        Assert.True(registry.TryGetEffect("terrain_on_move_draw_1", out _));
    }

    [Fact]
    public void LegacyTerrain_JeuDeVitres_LoseBanishDeck6_BanishesFromLoserDeckNotOnPlay()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;
        var p2 = PlayerId.Player2;

        var jeuDeVitres = new DuelCard("500003", "JEU DE VITRES", CardKind.Terrain, effectIds: new[] { "lose_banish_deck_6" });
        engine.State.AddCardToHand(p1, jeuDeVitres);

        int p1BanishBefore = engine.State.GetBanish(p1).Count;
        int p2BanishBefore = engine.State.GetBanish(p2).Count;

        // Poser le terrain ne doit PAS déclencher lose_banish_deck_6 à la pose
        var playRes = engine.PlayTerrain(p1, "500003", 0);
        Assert.True(playRes.Success);
        Assert.Equal(p1BanishBefore, engine.State.GetBanish(p1).Count);
        Assert.Equal(p2BanishBefore, engine.State.GetBanish(p2).Count);

        // Lors du score où P1 bat P2, P2 (le perdant) bannit 6 cartes du dessus de son deck
        engine.State.Slots[0].AddCharacter(p1, new DuelCard("C1", "Winner", CardKind.Character, force: 50));
        engine.State.Slots[0].AddCharacter(p2, new DuelCard("C2", "Loser", CardKind.Character, force: 10));

        int p2DeckBefore = engine.State.GetDeck(p2).Count;
        engine.ResolveScore(0, p1);

        Assert.Equal(p2DeckBefore - 6, engine.State.GetDeck(p2).Count);
        Assert.Equal(6, engine.State.GetBanish(p2).Count);
    }

    [Fact]
    public void LegacyTerrain_MontagneTropicale_LoseDraw7_LoserDraws7Cards()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;
        var p2 = PlayerId.Player2;

        var montagne = new DuelCard("500014", "MONTAGNE TROPICALE", CardKind.Terrain, effectIds: new[] { "lose_draw_7" });
        engine.State.SetSlot(0, montagne, placedOnTurn: 0, placedBy: p1);

        engine.State.Slots[0].AddCharacter(p1, new DuelCard("C1", "Winner", CardKind.Character, force: 50));
        engine.State.Slots[0].AddCharacter(p2, new DuelCard("C2", "Loser", CardKind.Character, force: 10));

        int p2HandBefore = engine.State.GetHand(p2).Count;
        engine.ResolveScore(0, p1);

        Assert.Equal(p2HandBefore + 7, engine.State.GetHand(p2).Count);
    }

    [Fact]
    public void LegacyTerrain_Bureaux_BoostsLainkAndTerracidCharacters()
    {
        var bureaux = new DuelCard("500002", "BUREAUX", CardKind.Terrain, effectIds: new[] { "boost_laink_terracid_3", "lose_mill_3" });
        var slot = new TerrainSlot(0, bureaux, placedOnTurn: 0, placedBy: PlayerId.Player1);

        var laink = new DuelCard("C1", "Laink", CardKind.Character, force: 10, effigy: "Laink");
        var terracid = new DuelCard("C2", "Terracid", CardKind.Character, force: 10, effigy: "Terracid");
        var guest = new DuelCard("C3", "Guest", CardKind.Character, force: 10, effigy: "Guest");

        slot.AddCharacter(PlayerId.Player1, laink);
        slot.AddCharacter(PlayerId.Player1, terracid);
        slot.AddCharacter(PlayerId.Player1, guest);

        // Laink (10+3) + Terracid (10+3) + Guest (10) = 36
        Assert.Equal(36, slot.GetForce(PlayerId.Player1));
    }

    [Fact]
    public void LegacyTerrain_Cuisine_BoostsCharactersInActiveCombo()
    {
        var cuisine = new DuelCard("500008", "CUISINE", CardKind.Terrain, effectIds: new[] { "terrain_combo_boost_1", "lose_mill_4" });
        var slot = new TerrainSlot(0, cuisine, placedOnTurn: 0, placedBy: PlayerId.Player1);

        var opener = new DuelCard("C1", "Opener", CardKind.Character, force: 10, hasOpeningGem: true, hasClosingGem: false);
        var closer = new DuelCard("C2", "Closer", CardKind.Character, force: 10, hasOpeningGem: false, hasClosingGem: true);
        var solo = new DuelCard("C3", "Solo", CardKind.Character, force: 10, hasOpeningGem: false, hasClosingGem: false);

        slot.AddCharacter(PlayerId.Player1, opener);
        slot.AddCharacter(PlayerId.Player1, closer);
        slot.AddCharacter(PlayerId.Player1, solo);

        // Opener (10+1) + Closer (10+1) + Solo (10) = 32
        Assert.Equal(32, slot.GetForce(PlayerId.Player1));
    }

    [Fact]
    public void LegacyTerrain_Terradollar_Draws1CardWhenCharacterPlayedOnIt()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;

        var terradollar = new DuelCard("500006", "TERRADOLLAR", CardKind.Terrain, effectIds: new[] { "terrain_on_char_played_draw_1", "lose_mill_4" });
        engine.State.SetSlot(0, terradollar, placedOnTurn: 0, placedBy: p1);
        engine.State.SetSlot(1, new DuelCard("T2", "Autre", CardKind.Terrain), placedOnTurn: 0, placedBy: p1);

        var charCard = new DuelCard("C_Play", "Test Char", CardKind.Character, force: 20);
        engine.State.AddCardToHand(p1, charCard);

        int handBefore = engine.State.GetHand(p1).Count; // includes charCard
        var res = engine.PlayCharacter(p1, "C_Play", 0);
        Assert.True(res.Success);

        // charCard played (-1) and 1 card drawn (+1) -> hand count unchanged
        Assert.Equal(handBefore, engine.State.GetHand(p1).Count);
    }

    [Fact]
    public void LegacyTerrain_Brasserie_BanishesTopCardWhenCharacterPlayedOnIt()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;

        var brasserie = new DuelCard("500009", "BRASSERIE", CardKind.Terrain, effectIds: new[] { "terrain_on_char_played_banish_top_1", "lose_mill_3" });
        engine.State.SetSlot(0, brasserie, placedOnTurn: 0, placedBy: p1);
        engine.State.SetSlot(1, new DuelCard("T2", "Autre", CardKind.Terrain), placedOnTurn: 0, placedBy: p1);

        var charCard = new DuelCard("C_Play", "Test Char", CardKind.Character, force: 20);
        engine.State.AddCardToHand(p1, charCard);

        int banishBefore = engine.State.GetBanish(p1).Count;
        var res = engine.PlayCharacter(p1, "C_Play", 0);
        Assert.True(res.Success);

        Assert.Equal(banishBefore + 1, engine.State.GetBanish(p1).Count);
    }

    [Fact]
    public void LegacyTerrain_RoadTrip_Draws1CardWhenCharacterMoved()
    {
        var engine = CreateEngine();
        var p1 = PlayerId.Player1;

        var roadTrip = new DuelCard("500001", "ROAD TRIP", CardKind.Terrain, effectIds: new[] { "terrain_on_move_draw_1", "lose_mill_4" });
        engine.State.SetSlot(0, roadTrip, placedOnTurn: 0, placedBy: p1);
        engine.State.SetSlot(1, new DuelCard("T2", "Destination", CardKind.Terrain), placedOnTurn: 0, placedBy: p1);

        var mover = new DuelCard("C_Move", "Voyageur", CardKind.Character, force: 20);
        engine.State.Slots[0].AddCharacter(p1, mover);

        int handBefore = engine.State.GetHand(p1).Count;
        var res = engine.MoveCharacter(p1, "C_Move", 0, 1);
        Assert.True(res.Success);

        Assert.Equal(handBefore + 1, engine.State.GetHand(p1).Count);
    }
}
