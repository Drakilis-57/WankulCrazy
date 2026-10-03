using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep3Tests
{
    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public int Next(int maxExclusive) => 0;
        public void Shuffle<T>(IList<T> list) { }
    }

    [Fact]
    public void PlayCharacter_Max4PerTurn_EnforcedAndResetOnNextTurn()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var c1 = new DuelCard("C1", "P1", CardKind.Character, force: 100);
        var c2 = new DuelCard("C2", "P2", CardKind.Character, force: 100);
        var c3 = new DuelCard("C3", "P3", CardKind.Character, force: 100);
        var c4 = new DuelCard("C4", "P4", CardKind.Character, force: 100);
        var c5 = new DuelCard("C5", "P5", CardKind.Character, force: 100);

        engine.StartDuel(new[] { terrain, c1, c2, c3, c4, c5 }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, c1);
        engine.State.AddCardToHand(PlayerId.Player1, c2);
        engine.State.AddCardToHand(PlayerId.Player1, c3);
        engine.State.AddCardToHand(PlayerId.Player1, c4);
        engine.State.AddCardToHand(PlayerId.Player1, c5);

        engine.StartTurn();

        Assert.True(engine.PlayCharacter(PlayerId.Player1, "C1", 0).Success);
        Assert.True(engine.PlayCharacter(PlayerId.Player1, "C2", 0).Success);
        Assert.True(engine.PlayCharacter(PlayerId.Player1, "C3", 0).Success);
        Assert.True(engine.PlayCharacter(PlayerId.Player1, "C4", 0).Success);
        Assert.Equal(4, engine.State.CharactersPlayedThisTurn);

        // 5ème refusé
        var r5 = engine.PlayCharacter(PlayerId.Player1, "C5", 0);
        Assert.False(r5.Success);
        Assert.Contains("Cannot play more than 4", r5.ErrorReason);

        // Tour de P2 puis nouveau tour de P1
        engine.EndTurn(PlayerId.Player1);
        engine.StartTurn();
        engine.EndTurn(PlayerId.Player2);
        engine.StartTurn();

        // Compteur réinitialisé
        Assert.Equal(0, engine.State.CharactersPlayedThisTurn);
        Assert.True(engine.PlayCharacter(PlayerId.Player1, "C5", 0).Success);
    }

    [Fact]
    public void PlayCharacter_Refused_OnEmptySlot_OrWhenTerrainPending()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terr = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var c1 = new DuelCard("C1", "Perso", CardKind.Character, force: 100);

        engine.StartDuel(new[] { terr, c1 }, new[] { terr }, PlayerId.Player1);
        engine.State.AddCardToHand(PlayerId.Player1, terr);
        engine.State.AddCardToHand(PlayerId.Player1, c1);

        engine.StartTurn();
        // 0 terrain en jeu, terrain en main -> Pending = true
        Assert.True(engine.State.TerrainRequirementPending);

        // Refus car terrain obligatoire non posé
        var rPending = engine.PlayCharacter(PlayerId.Player1, "C1", 0);
        Assert.False(rPending.Success);
        Assert.Contains("Must place a terrain", rPending.ErrorReason);

        // Pose du terrain sur slot 1
        engine.PlayTerrain(PlayerId.Player1, "T1", 1);

        // Refus sur slot 0 car slot 0 est vide
        var rEmpty = engine.PlayCharacter(PlayerId.Player1, "C1", 0);
        Assert.False(rEmpty.Success);
        Assert.Contains("empty slot", rEmpty.ErrorReason);

        // OK sur slot 1 (contient le terrain)
        var rOk = engine.PlayCharacter(PlayerId.Player1, "C1", 1);
        Assert.True(rOk.Success);
    }

    [Fact]
    public void Scoreur_OnActiveTerrain_TriggersScoring_ComparesForce_AndCleansSlot()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var p1Char = new DuelCard("P1_Char", "P1 Troupe", CardKind.Character, force: 200);
        var p1Scoreur = new DuelCard("P1_Scoreur", "P1 Scoreur", CardKind.Character, force: 50, isScoreur: true);
        var p2Char = new DuelCard("P2_Char", "P2 Troupe", CardKind.Character, force: 150);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);

        // Placer un terrain sur slot 0 posé au Tour 0 par Player1 -> Actif dès le tour 1 !
        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2); // slot 1 pour éviter requirement

        // P2 a une troupe de force 150 sur le slot 0
        engine.State.AddCharacterToSlot(0, PlayerId.Player2, p2Char);

        // Main de P1
        engine.State.AddCardToHand(PlayerId.Player1, p1Char);
        engine.State.AddCardToHand(PlayerId.Player1, p1Scoreur);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        // P1 pose P1_Char (200 force) sur le slot 0 -> P1 Force = 200, P2 Force = 150
        engine.PlayCharacter(PlayerId.Player1, "P1_Char", 0);

        // P1 pose son Scoreur (+50 force = 250 vs 150). Terrain actif -> scoring automatique !
        var rScore = engine.PlayCharacter(PlayerId.Player1, "P1_Scoreur", 0);
        Assert.True(rScore.Success);

        // Vérifier ScoreResolvedEvent
        var scoreEvt = events.OfType<ScoreResolvedEvent>().Single();
        Assert.Equal(0, scoreEvt.SlotIndex);
        Assert.Equal(250, scoreEvt.ForceP1);
        Assert.Equal(150, scoreEvt.ForceP2);
        Assert.Equal(PlayerId.Player1, scoreEvt.Winner);

        // P1 a gagné 1 point
        Assert.Equal(1, engine.State.ScoreP1);
        Assert.Equal(0, engine.State.ScoreP2);

        // Slot 0 est maintenant vide
        Assert.True(engine.State.Slots[0].IsEmpty);

        // Cartes en défausse
        var discardP1 = engine.State.GetDiscard(PlayerId.Player1);
        var discardP2 = engine.State.GetDiscard(PlayerId.Player2);

        // P1 a son terrain T1 (posé par P1), P1_Char et P1_Scoreur
        Assert.Contains(discardP1, c => c.Id == "T1");
        Assert.Contains(discardP1, c => c.Id == "P1_Char");
        Assert.Contains(discardP1, c => c.Id == "P1_Scoreur");

        // P2 a P2_Char
        Assert.Contains(discardP2, c => c.Id == "P2_Char");
    }

    [Fact]
    public void Scoreur_OnTiedForce_ResolvesWithNoWinner_AndCleansSlot()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var p1Scoreur = new DuelCard("P1_Scoreur", "P1 Scoreur", CardKind.Character, force: 100, isScoreur: true);
        var p2Char = new DuelCard("P2_Char", "P2 Troupe", CardKind.Character, force: 100);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        engine.State.AddCharacterToSlot(0, PlayerId.Player2, p2Char);
        engine.State.AddCardToHand(PlayerId.Player1, p1Scoreur);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        // Pose du scoreur : 100 force vs 100 force -> Égalité
        engine.PlayCharacter(PlayerId.Player1, "P1_Scoreur", 0);

        var scoreEvt = events.OfType<ScoreResolvedEvent>().Single();
        Assert.Null(scoreEvt.Winner);
        Assert.Equal(0, engine.State.ScoreP1);
        Assert.Equal(0, engine.State.ScoreP2);

        // Slot vidé
        Assert.True(engine.State.Slots[0].IsEmpty);

        // Terrain posé par P2 -> va dans la défausse de P2
        Assert.Contains(engine.State.GetDiscard(PlayerId.Player2), c => c.Id == "T1");
    }

    [Fact]
    public void Scoreur_OnInactiveTerrain_DoesNotTriggerScoring()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var p1Scoreur = new DuelCard("P1_Scoreur", "P1 Scoreur", CardKind.Character, force: 100, isScoreur: true);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.AddCardToHand(PlayerId.Player1, terrain);
        engine.State.AddCardToHand(PlayerId.Player1, p1Scoreur);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        // Pose du terrain ce tour (tour 1)
        engine.PlayTerrain(PlayerId.Player1, "T1", 0);
        Assert.False(engine.State.Slots[0].IsActive(engine.State.TurnNumber));

        // Pose du scoreur sur ce terrain inactif
        var rPlay = engine.PlayCharacter(PlayerId.Player1, "P1_Scoreur", 0);
        Assert.True(rPlay.Success);

        // Le scoring ne doit PAS être déclenché
        Assert.Empty(events.OfType<ScoreResolvedEvent>());
        // Le slot doit toujours contenir le terrain et le personnage
        Assert.False(engine.State.Slots[0].IsEmpty);
        Assert.Single(engine.State.Slots[0].CharactersP1);
        Assert.Equal(0, engine.State.ScoreP1);
    }

    [Fact]
    public void ReachingVictoryThreshold_EndsGame_WithFiveTerrainsReason()
    {
        var rng = new NoShuffleRandom();
        // TerrainsToWin fixé à 2 pour le test
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0, terrainsToWin: 2);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var scoreur = new DuelCard("Sc", "Scoreur", CardKind.Character, force: 100, isScoreur: true);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player1);

        // P1 a déjà 1 point
        engine.State.AddScore(PlayerId.Player1, 1);
        engine.State.AddCardToHand(PlayerId.Player1, scoreur);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        // P1 score le slot 0 (100 force vs 0) -> marque son 2ème point (seuil atteint)
        engine.PlayCharacter(PlayerId.Player1, "Sc", 0);

        Assert.Equal(2, engine.State.ScoreP1);
        Assert.True(engine.State.IsGameOver);
        Assert.Equal(PlayerId.Player1, engine.State.Winner);
        Assert.Equal(GameOverReason.FiveTerrains, engine.State.GameOverReason);

        var endEvt = events.OfType<DuelEndedEvent>().Single();
        Assert.Equal(PlayerId.Player1, endEvt.Winner);
        Assert.Equal(GameOverReason.FiveTerrains, endEvt.Reason);
    }
}
