using System.Reflection;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep2Tests
{
    private static List<DuelCard> CreateCustomDeck(int characterCount, int terrainCount, string prefix = "C")
    {
        var list = new List<DuelCard>();
        for (int i = 0; i < characterCount; i++)
        {
            list.Add(new DuelCard($"{prefix}_Char_{i}", $"Character {i}", CardKind.Character, force: 100));
        }
        for (int i = 0; i < terrainCount; i++)
        {
            list.Add(new DuelCard($"{prefix}_Terr_{i}", $"Terrain {i}", CardKind.Terrain));
        }
        return list;
    }

    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public int Next(int maxExclusive) => 0;
        public void Shuffle<T>(IList<T> list) { }
    }

    [Fact]
    public void DuelState_Encapsulation_NoPublicOrInternalSettersOrMutableCollections()
    {
        var type = typeof(DuelState);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Vérifier les setters : aucun setter public ou internal
            var setMethod = prop.GetSetMethod(nonPublic: true);
            if (setMethod != null)
            {
                Assert.True(setMethod.IsPrivate, $"Property {prop.Name} has non-private setter ({setMethod.Attributes}).");
            }

            // Vérifier les types exposés : pas de List<T> ou T[] direct mutable
            var propType = prop.PropertyType;
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Assert.Fail($"Property {prop.Name} exposes mutable List<T>.");
            }
            if (propType.IsArray)
            {
                Assert.Fail($"Property {prop.Name} exposes mutable Array.");
            }
        }
    }

    [Fact]
    public void PlayTerrain_Max3Slots_Rejects4thAndRejectsOccupiedSlot()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terr1 = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var terr2 = new DuelCard("T2", "Montagne", CardKind.Terrain);
        var terr3 = new DuelCard("T3", "Foret", CardKind.Terrain);
        var terr4 = new DuelCard("T4", "Desert", CardKind.Terrain);

        engine.StartDuel(new[] { terr1, terr2, terr3, terr4 }, CreateCustomDeck(5, 5), PlayerId.Player1);
        engine.State.AddCardToHand(PlayerId.Player1, terr1);
        engine.State.AddCardToHand(PlayerId.Player1, terr2);
        engine.State.AddCardToHand(PlayerId.Player1, terr3);
        engine.State.AddCardToHand(PlayerId.Player1, terr4);

        engine.StartTurn();

        // Pose slot 0
        var r1 = engine.PlayTerrain(PlayerId.Player1, "T1", 0);
        Assert.True(r1.Success);
        Assert.Equal(1, engine.State.TerrainsInPlayCount);

        // Pose sur slot 0 déjà occupé -> Refusé
        var rOccupied = engine.PlayTerrain(PlayerId.Player1, "T2", 0);
        Assert.False(rOccupied.Success);
        Assert.Contains("occupied", rOccupied.ErrorReason, StringComparison.OrdinalIgnoreCase);

        // Pose slot 1 et slot 2
        var r2 = engine.PlayTerrain(PlayerId.Player1, "T2", 1);
        var r3 = engine.PlayTerrain(PlayerId.Player1, "T3", 2);
        Assert.True(r2.Success);
        Assert.True(r3.Success);
        Assert.Equal(3, engine.State.TerrainsInPlayCount);

        // 4ème terrain quand max 3 -> Refusé
        var r4 = engine.PlayTerrain(PlayerId.Player1, "T4", 0);
        Assert.False(r4.Success);
    }

    [Fact]
    public void Terrain_Freshness_NotActiveOnPlacedTurn_ActiveOnNextTurn()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terr = new DuelCard("T1", "Plaine", CardKind.Terrain);
        // Deck P2 ne contient que des personnages pour éviter un dépilage automatique au tour 2
        engine.StartDuel(new[] { terr }, CreateCustomDeck(10, 0, "P2"), PlayerId.Player1);
        engine.State.AddCardToHand(PlayerId.Player1, terr);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        // Tour 1 de P1
        engine.StartTurn();
        Assert.Equal(1, engine.State.TurnNumber);

        var playResult = engine.PlayTerrain(PlayerId.Player1, "T1", 0);
        Assert.True(playResult.Success);

        var slot = engine.State.Slots[0];
        Assert.Equal(1, slot.PlacedOnTurn);

        // Au tour 1, PlacedOnTurn (1) < TurnNumber (1) est FAUX -> Non actif
        Assert.False(slot.IsActive(engine.State.TurnNumber));

        // Fin du tour 1 de P1
        engine.EndTurn(PlayerId.Player1);

        // Tour 2 (tour de P2)
        engine.StartTurn();
        Assert.Equal(2, engine.State.TurnNumber);

        // Au tour 2, PlacedOnTurn (1) < TurnNumber (2) est VRAI -> Actif et redressé
        Assert.True(slot.IsActive(engine.State.TurnNumber));

        // Vérifier TerrainsReadiedEvent avec slot 0
        var readiedEvent = events.OfType<TerrainsReadiedEvent>().FirstOrDefault(e => e.TurnNumber == 2);
        Assert.NotNull(readiedEvent);
        Assert.Contains(0, readiedEvent!.ReadiedSlotIndices);

        // Tour 3 (tour de P1 à nouveau) : PlacedOnTurn == 1 != 3 - 1, donc ne doit PAS réapparaître dans TerrainsReadiedEvent du tour 3
        engine.EndTurn(PlayerId.Player2);
        engine.StartTurn();
        Assert.Equal(3, engine.State.TurnNumber);

        var readiedTurn3 = events.OfType<TerrainsReadiedEvent>().FirstOrDefault(e => e.TurnNumber == 3);
        Assert.Null(readiedTurn3); // Aucun nouveau terrain redressé
    }

    [Fact]
    public void TerrainRequirementPending_BlocksEndTurn_WhenTerrainInHand()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terr = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.StartDuel(new[] { terr }, CreateCustomDeck(5, 5), PlayerId.Player1);
        engine.State.AddCardToHand(PlayerId.Player1, terr);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        // 0 terrain en jeu et un terrain en main -> Pending = true et event émis
        Assert.True(engine.State.TerrainRequirementPending);
        Assert.Contains(events, e => e is TerrainPlacementRequiredEvent req && req.Player == PlayerId.Player1);

        // Tentative de fin de tour bloquée
        var endResult = engine.EndTurn(PlayerId.Player1);
        Assert.False(endResult.Success);
        Assert.Contains("Must place a terrain", endResult.ErrorReason);

        // Pose du terrain
        var playResult = engine.PlayTerrain(PlayerId.Player1, "T1", 0);
        Assert.True(playResult.Success);
        Assert.False(engine.State.TerrainRequirementPending);

        // Fin de tour désormais autorisée
        var endResultAfter = engine.EndTurn(PlayerId.Player1);
        Assert.True(endResultAfter.Success);
    }

    [Fact]
    public void AutomaticDig_WhenNoTerrainInHand_PutsRevealedCardsUnderDeckInOrder()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var c1 = new DuelCard("C1", "Perso 1", CardKind.Character);
        var c2 = new DuelCard("C2", "Perso 2", CardKind.Character);
        var t1 = new DuelCard("T1", "Terrain 1", CardKind.Terrain);
        var c3 = new DuelCard("C3", "Perso 3", CardKind.Character);

        // Deck P1 dans l'ordre du dessus vers le dessous : C1, C2, T1, C3
        engine.StartDuel(new[] { c1, c2, t1, c3 }, CreateCustomDeck(5, 5), PlayerId.Player1);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        // Au début du tour : main vide (0 terrain), 0 terrain en jeu -> déclenche le dépilage
        engine.StartTurn();

        Assert.False(engine.State.TerrainRequirementPending);

        // Le terrain T1 est placé sur le slot 0
        var slot0 = engine.State.Slots[0];
        Assert.False(slot0.IsEmpty);
        Assert.Equal("T1", slot0.Card!.Id);
        Assert.Equal(1, slot0.PlacedOnTurn);
        // Non scorable le tour même !
        Assert.False(slot0.IsActive(engine.State.TurnNumber));

        // Événement émis
        var digEvt = events.OfType<TerrainRevealedByDigEvent>().Single();
        Assert.Equal(0, digEvt.SlotIndex);
        Assert.Equal("T1", digEvt.Card.Id);
        Assert.Equal(2, digEvt.RevealedCardsPutUnderDeck.Count);
        Assert.Equal("C1", digEvt.RevealedCardsPutUnderDeck[0].Id);
        Assert.Equal("C2", digEvt.RevealedCardsPutUnderDeck[1].Id);

        // Deck restant : C3 était resté dans le deck, suivi de C1 et C2 remis dessous dans l'ordre !
        var p1Deck = engine.State.GetDeck(PlayerId.Player1);
        Assert.Equal(3, p1Deck.Count);
        Assert.Equal("C3", p1Deck[0].Id);
        Assert.Equal("C1", p1Deck[1].Id);
        Assert.Equal("C2", p1Deck[2].Id);
    }

    [Fact]
    public void AutomaticDig_WhenDeckHasNoTerrain_EmitsTerrainDigFailed_AndPreservesDeckOrderWithoutMill()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var c1 = new DuelCard("C1", "Perso 1", CardKind.Character);
        var c2 = new DuelCard("C2", "Perso 2", CardKind.Character);
        var c3 = new DuelCard("C3", "Perso 3", CardKind.Character);

        engine.StartDuel(new[] { c1, c2, c3 }, CreateCustomDeck(5, 5), PlayerId.Player1);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        // StartTurn déclenche le dépilage mais 0 terrain dans tout le deck
        engine.StartTurn();

        Assert.False(engine.State.IsGameOver); // Pas de meule !
        Assert.False(engine.State.TerrainRequirementPending);

        Assert.Contains(events, e => e is TerrainDigFailedEvent failed && failed.Player == PlayerId.Player1);

        // Le deck est restauré exactement dans l'ordre d'origine
        var p1Deck = engine.State.GetDeck(PlayerId.Player1);
        Assert.Equal(3, p1Deck.Count);
        Assert.Equal("C1", p1Deck[0].Id);
        Assert.Equal("C2", p1Deck[1].Id);
        Assert.Equal("C3", p1Deck[2].Id);
    }

    [Fact]
    public void AutomaticDig_WithOneTerrainAlreadyInPlay_PlacesInFirstFreeSlot()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var existingTerrain = new DuelCard("T0", "Terrain 0", CardKind.Terrain);
        var t1 = new DuelCard("T1", "Terrain 1", CardKind.Terrain);

        engine.StartDuel(new[] { t1 }, CreateCustomDeck(5, 5), PlayerId.Player1);

        // On place déjà un terrain sur le slot 0
        engine.State.SetSlot(0, existingTerrain, 0);
        Assert.Equal(1, engine.State.TerrainsInPlayCount);

        // Au tour 1 : 1 terrain en jeu (seuil <= 1), 0 terrain en main -> dépile et place sur premier slot libre (slot 1)
        engine.StartTurn();

        Assert.Equal(2, engine.State.TerrainsInPlayCount);
        Assert.Equal("T0", engine.State.Slots[0].Card!.Id);
        Assert.Equal("T1", engine.State.Slots[1].Card!.Id);
    }

    [Fact]
    public void TwoTerrainsInPlay_WithTerrainInHand_DoesNotTriggerPending()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var t0 = new DuelCard("T0", "Terrain 0", CardKind.Terrain);
        var t1 = new DuelCard("T1", "Terrain 1", CardKind.Terrain);
        var tHand = new DuelCard("TH", "Terrain Hand", CardKind.Terrain);

        engine.StartDuel(CreateCustomDeck(5, 5), CreateCustomDeck(5, 5), PlayerId.Player1);
        engine.State.SetSlot(0, t0, 0);
        engine.State.SetSlot(1, t1, 0);
        engine.State.AddCardToHand(PlayerId.Player1, tHand);

        // 2 terrains en jeu (> seuil 1)
        engine.StartTurn();

        Assert.False(engine.State.TerrainRequirementPending);
        // EndTurn immédiatement permis
        var endResult = engine.EndTurn(PlayerId.Player1);
        Assert.True(endResult.Success);
    }

    [Fact]
    public void PlayTerrain_NonActivePlayerOrBeforeGame_IsRefused()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terr = new DuelCard("T1", "Plaine", CardKind.Terrain);

        // Avant StartTurn (TurnNumber == 0)
        engine.StartDuel(new[] { terr }, CreateCustomDeck(5, 5), PlayerId.Player1);
        engine.State.AddCardToHand(PlayerId.Player1, terr);

        var rBefore = engine.PlayTerrain(PlayerId.Player1, "T1", 0);
        Assert.False(rBefore.Success);
        Assert.Contains("not started", rBefore.ErrorReason);

        engine.StartTurn();

        // Par joueur non actif (Player2)
        var rP2 = engine.PlayTerrain(PlayerId.Player2, "T1", 0);
        Assert.False(rP2.Success);
        Assert.Contains("Not this player's turn", rP2.ErrorReason);

        // Carte inconnue dans la main
        var rUnknown = engine.PlayTerrain(PlayerId.Player1, "UNKNOWN", 0);
        Assert.False(rUnknown.Success);
        Assert.Contains("not found", rUnknown.ErrorReason);
    }
}
