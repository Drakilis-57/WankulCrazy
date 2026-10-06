using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelSeason1EffectsTests
{
    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public int Next(int maxExclusive) => 0;
        public void Shuffle<T>(IList<T> list) { }
    }

    [Fact]
    public void Moria_Terrain_GivesPlus20ForceToLainkCardsOnly()
    {
        var moria = new DuelCard("T_Moria", "Moria", CardKind.Terrain, effectIds: new[] { "win_mill_2" });
        var slot = new TerrainSlot(0, moria, placedOnTurn: 1, placedBy: PlayerId.Player1);

        var lainkCard = new DuelCard("C1", "Laink Guerrier", CardKind.Character, force: 100, effigy: "Laink");
        var terracidCard = new DuelCard("C2", "Terracid Mage", CardKind.Character, force: 100, effigy: "Terracid");

        slot.AddCharacter(PlayerId.Player1, lainkCard);
        slot.AddCharacter(PlayerId.Player1, terracidCard);

        // Moria : +20 pour Laink (120) et +0 pour Terracid (100) => Total = 220
        Assert.Equal(220, slot.GetForce(PlayerId.Player1));
    }

    [Fact]
    public void Portal_Terrain_GivesPlus20ForceToTerracidCardsOnly()
    {
        var portal = new DuelCard("T_Portal", "Portal", CardKind.Terrain, effectIds: new[] { "win_draw_1" });
        var slot = new TerrainSlot(0, portal, placedOnTurn: 1, placedBy: PlayerId.Player1);

        var lainkCard = new DuelCard("C1", "Laink Guerrier", CardKind.Character, force: 80, effigy: "Laink");
        var terracidCard = new DuelCard("C2", "Terracid Mage", CardKind.Character, force: 80, effigy: "Terracid");

        slot.AddCharacter(PlayerId.Player1, lainkCard);
        slot.AddCharacter(PlayerId.Player1, terracidCard);

        // Portal : +0 pour Laink (80) et +20 pour Terracid (100) => Total = 180
        Assert.Equal(180, slot.GetForce(PlayerId.Player1));
    }

    [Fact]
    public void Rust_And_Golf_Terrain_DrawsCardAtStartOfTurn()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 2);
        var engine = new DuelEngine(rng, rules);

        var deckP1 = new[] {
            new DuelCard("D1", "Card 1", CardKind.Character),
            new DuelCard("D2", "Card 2", CardKind.Character),
            new DuelCard("D3", "Card 3", CardKind.Character),
            new DuelCard("D4", "Card 4", CardKind.Character),
        };
        var deckP2 = new[] {
            new DuelCard("D2_1", "Card P2", CardKind.Character),
        };

        engine.StartDuel(deckP1, deckP2, PlayerId.Player1);

        // Placer le terrain Rust sur le slot 0 et le rendre actif (placedOnTurn = 0, currentTurn = 1)
        var rustTerrain = new DuelCard("T_Rust", "Rust", CardKind.Terrain, effectIds: new[] { "terrain_draw_1_turn_start" });
        engine.State.SetSlot(0, rustTerrain, placedOnTurn: 0, placedBy: PlayerId.Player1);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        // Début du tour de P1
        engine.StartTurn();

        // P1 doit avoir pioché 2 (règle standard) + 1 (effet Rust actif) = 3 cartes !
        var handP1 = engine.State.GetHand(PlayerId.Player1);
        Assert.Equal(3, handP1.Count);
        Assert.Contains(events, e => e is EffectTriggeredEvent eff && eff.EffectId == "terrain_draw_1_turn_start");
    }

    [Fact]
    public void Moria_WinEffect_Mills2CardsFromOpponentDeck()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var deckP1 = new[] { new DuelCard("P1_1", "P1 Card", CardKind.Character) };
        var deckP2 = new[] {
            new DuelCard("P2_1", "P2 Card 1", CardKind.Character),
            new DuelCard("P2_2", "P2 Card 2", CardKind.Character),
            new DuelCard("P2_3", "P2 Card 3", CardKind.Character),
        };

        engine.StartDuel(deckP1, deckP2, PlayerId.Player1);

        var moria = new DuelCard("T_Moria", "Moria", CardKind.Terrain, effectIds: new[] { "win_mill_2" });
        engine.State.SetSlot(0, moria, placedOnTurn: 0, placedBy: PlayerId.Player1);

        // P1 a 1 scoreur avec 100 de force, P2 a 0
        var scoreur = new DuelCard("S1", "Scoreur", CardKind.Character, force: 100, isScoreur: true);
        engine.State.Slots[0].AddCharacter(PlayerId.Player1, scoreur);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.ResolveScore(0);

        // Vérifier que win_mill_2 s'est déclenché
        Assert.Contains(events, e => e is EffectTriggeredEvent eff && eff.EffectId == "win_mill_2");
        // Vérifier que 2 cartes de P2 ont été meulées
        Assert.Single(engine.State.GetDeck(PlayerId.Player2));
        Assert.Equal(2, engine.State.GetDiscard(PlayerId.Player2).Count);
    }

    [Fact]
    public void Camionneur_DiscardsHighestForceCharacterFromOpponentSlot()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var deckP1 = new[] { new DuelCard("P1_1", "P1 Card", CardKind.Character) };
        var deckP2 = new[] { new DuelCard("P2_1", "P2 Card", CardKind.Character) };

        engine.StartDuel(deckP1, deckP2, PlayerId.Player1);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);

        // Mettre 2 personnages sur le slot pour l'adversaire (P2)
        var weakChar = new DuelCard("Weak", "Faible", CardKind.Character, force: 60);
        var strongChar = new DuelCard("Strong", "Fort", CardKind.Character, force: 140);
        engine.State.Slots[0].AddCharacter(PlayerId.Player2, weakChar);
        engine.State.Slots[0].AddCharacter(PlayerId.Player2, strongChar);

        // P1 joue Camionneur (discard_opp_char_any)
        var camionneur = new DuelCard("Camion", "Camionneur", CardKind.Character, force: 80, effectIds: new[] { "discard_opp_char_any" });
        engine.State.AddCardToHand(PlayerId.Player1, camionneur);

        engine.StartTurn();
        var result = engine.PlayCharacter(PlayerId.Player1, "Camion", 0);
        Assert.True(result.Success);

        // Le perso le plus fort de P2 (Strong) a dû être défaussé
        var p2Chars = engine.State.Slots[0].GetCharacters(PlayerId.Player2);
        Assert.Single(p2Chars);
        Assert.Equal("Weak", p2Chars[0].Id);
        Assert.Contains(engine.State.GetDiscard(PlayerId.Player2), c => c.Id == "Strong");
    }

    [Fact]
    public void AutoScore_TriggersWhenPlayerHas110ForceOrMoreOnActiveTerrain()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0, autoScoreForceThreshold: 110);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var strongChar = new DuelCard("Strong", "Colosse", CardKind.Character, force: 120);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        engine.State.Slots[0].AddCharacter(PlayerId.Player1, strongChar);

        engine.StartTurn();

        // P1 a 120 de force (>= 110 = 11 points) sans aucun scoreur -> peut déclencher le score automatique !
        var result = engine.TriggerAutoScore(PlayerId.Player1, 0);
        Assert.True(result.Success);

        // P1 l'emporte et marque 1 point
        Assert.Equal(1, engine.State.ScoreP1);
        Assert.True(engine.State.Slots[0].IsEmpty);
    }

    [Fact]
    public void GoldenRule_ZeroForceCharacterWinsAgainstZeroOpponentCharacters()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var zeroChar = new DuelCard("Zero", "Faible", CardKind.Character, force: 0);
        var scoreur = new DuelCard("S1", "Scoreur", CardKind.Character, force: 0, isScoreur: true);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        engine.State.Slots[0].AddCharacter(PlayerId.Player1, zeroChar);
        engine.State.AddCardToHand(PlayerId.Player1, scoreur);

        engine.StartTurn();

        // P1 joue Scoreur sur le slot 0 : P1 a 2 persos (0 force total), P2 a 0 perso
        // Règle d'or : Même à 0 en Force, le joueur qui a au moins 1 perso l'emporte si en face il n'y en a aucun.
        var result = engine.PlayCharacter(PlayerId.Player1, "S1", 0);
        Assert.True(result.Success);

        Assert.Equal(1, engine.State.ScoreP1);
    }
}
