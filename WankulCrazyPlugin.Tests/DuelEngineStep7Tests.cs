using System.Collections.Generic;
using System.Linq;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep7Tests
{
    private sealed class NoShuffleRandom : IRandom
    {
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public int Next(int maxExclusive) => 0;
        public void Shuffle<T>(IList<T> list) { }
    }

    [Fact]
    public void DuelAI_TriggersAutoScore_WhenReaching110ForceThresholdWithoutScoreur()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0, autoScoreForceThreshold: 110);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);

        // Deux terrains actifs pour éviter TerrainRequirementPending
        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        // P1 (IA) a déjà 60 de force sur le slot 0, et un perso régulier (non-scoreur) de 60 en main -> total 120 >= 110
        engine.State.Slots[0].AddCharacter(PlayerId.Player1, new DuelCard("C_Base", "Base", CardKind.Character, force: 60));
        engine.State.Slots[0].AddCharacter(PlayerId.Player2, new DuelCard("C_Opp", "Opp", CardKind.Character, force: 40));
        engine.State.AddCardToHand(PlayerId.Player1, new DuelCard("C_Reinforce", "Renfort", CardKind.Character, force: 60, isScoreur: false));

        engine.StartTurn();

        var ai = new DuelAI(PlayerId.Player1, rng);
        ai.PlayTurn(engine);

        // L'IA a joué Renfort sur le slot 0 (total 120 > 40 et >= 110) et a déclenché TriggerAutoScore !
        Assert.Equal(1, engine.State.ScoreP1);
        Assert.True(engine.State.Slots[0].IsEmpty);
        Assert.Equal(PlayerId.Player2, engine.State.ActivePlayer);
    }

    [Fact]
    public void DuelAI_DoesNotTriggerAutoScore_WhenBehindInForceEvenAbove110()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0, autoScoreForceThreshold: 110);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);

        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        // P1 a 120 de force mais P2 a 200 de force -> l'IA ne doit PAS autoscorer pour donner le point à P2
        engine.State.Slots[0].AddCharacter(PlayerId.Player1, new DuelCard("C_P1", "P1", CardKind.Character, force: 120));
        engine.State.Slots[0].AddCharacter(PlayerId.Player2, new DuelCard("C_P2", "P2", CardKind.Character, force: 200));

        engine.StartTurn();

        var ai = new DuelAI(PlayerId.Player1, rng);
        ai.PlayTurn(engine);

        Assert.Equal(0, engine.State.ScoreP1);
        Assert.Equal(0, engine.State.ScoreP2);
        Assert.False(engine.State.Slots[0].IsEmpty);
    }

    [Fact]
    public void DuelAI_PrefersComboSlot_WhenPlayingCharacterWithClosingGem()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);

        engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        // L'adversaire a de la force sur le slot 0, mais P1 a un personnage avec gemme d'ouverture sur le slot 1
        engine.State.Slots[0].AddCharacter(PlayerId.Player2, new DuelCard("C_Opp", "Opp", CardKind.Character, force: 50));
        engine.State.Slots[1].AddCharacter(
            PlayerId.Player1,
            new DuelCard("C_Opener", "Opener", CardKind.Character, force: 30, hasOpeningGem: true, hasClosingGem: false));

        // L'IA a en main un perso avec gemme de fermeture et bonus de combo +30
        var closer = new DuelCard(
            "C_Closer",
            "Closer",
            CardKind.Character,
            force: 40,
            comboEffectIds: new[] { "boost_self_30" },
            hasOpeningGem: false,
            hasClosingGem: true);
        engine.State.AddCardToHand(PlayerId.Player1, closer);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        engine.StartTurn();

        var ai = new DuelAI(PlayerId.Player1, rng);
        ai.PlayTurn(engine);

        // L'IA a privilégié le slot 1 pour compléter le Combo (30 + 40 + 30 = 100 de force)
        Assert.Equal(2, engine.State.Slots[1].CharactersP1.Count);
        Assert.Equal(100, engine.State.Slots[1].GetForce(PlayerId.Player1));
        Assert.Contains(events, e => e is ComboFormedEvent cf && cf.SlotIndex == 1 && cf.Player == PlayerId.Player1);
    }

    [Fact]
    public void DuelAI_AutomaticallyResolves_PlayerChoiceAndSlotChoiceEvents()
    {
        var rng = new NoShuffleRandom();
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0, maxCharactersPerTurn: 1);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);

        // Slot 0 : Terrain avec effet gagnant win_play_free_score (qui émet PlayerChoiceRequiredEvent puis PlayerSlotChoiceRequiredEvent)
        var campusTerrain = new DuelCard("T_Campus", "Campus", CardKind.Terrain, effectIds: new[] { "win_play_free_score" });
        engine.State.SetSlot(0, campusTerrain, placedOnTurn: 0, placedBy: PlayerId.Player1);
        engine.State.SetSlot(1, terrain, placedOnTurn: 0, placedBy: PlayerId.Player2);

        // P1 a déjà 60 de force sur le slot 0, joue C_Extra (maxCharactersPerTurn = 1) et garde C_Free en main
        engine.State.Slots[0].AddCharacter(PlayerId.Player1, new DuelCard("C_Init", "Init", CardKind.Character, force: 60));
        var freeCharInHand = new DuelCard("C_Free", "FreeChar", CardKind.Character, force: 80, isScoreur: false);
        engine.State.AddCardToHand(PlayerId.Player1, freeCharInHand);
        engine.State.AddCardToHand(PlayerId.Player1, new DuelCard("C_Extra", "Extra", CardKind.Character, force: 90, isScoreur: false));

        engine.StartTurn();

        var ai = new DuelAI(PlayerId.Player1, rng);
        ai.PlayTurn(engine);

        // L'IA a joué C_Extra sur le slot 0 (60 + 90 = 150 >= 110), déclenché l'autoscore du slot 0 (Point 1),
        // ce qui a déclenché win_play_free_score : l'IA a automatiquement sélectionné C_Free et le slot 1 actif, scorant aussi le slot 1 (Point 2) !
        Assert.Equal(2, engine.State.ScoreP1);
        Assert.True(engine.State.Slots[0].IsEmpty);
        Assert.True(engine.State.Slots[1].IsEmpty);
    }
}
