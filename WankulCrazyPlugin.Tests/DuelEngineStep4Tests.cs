using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep4Tests
{
    private static List<DuelCard> CreateRealisticDeck(string prefix = "P")
    {
        var deck = new List<DuelCard>();

        // 10 Terrains
        for (int i = 0; i < 10; i++)
        {
            deck.Add(new DuelCard($"{prefix}_Terrain_{i}", $"Terrain {i}", CardKind.Terrain));
        }

        // 35 Personnages réguliers (forces 50 à 400)
        for (int i = 0; i < 35; i++)
        {
            int force = 50 + (i % 8) * 50;
            deck.Add(new DuelCard($"{prefix}_Char_{i}", $"Char {i}", CardKind.Character, force: force));
        }

        // 5 Scoreurs
        for (int i = 0; i < 5; i++)
        {
            deck.Add(new DuelCard($"{prefix}_Scoreur_{i}", $"Scoreur {i}", CardKind.Character, force: 100, isScoreur: true));
        }

        return deck;
    }

    [Fact]
    public void DuelAI_PlaysRequiredTerrain_WhenPending()
    {
        var rng = new SystemRandomAdapter(123);
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        engine.StartDuel(new[] { terrain }, CreateRealisticDeck("P2"), PlayerId.Player1);

        engine.State.AddCardToHand(PlayerId.Player1, terrain);
        engine.StartTurn();

        Assert.True(engine.State.TerrainRequirementPending);

        var ai = new DuelAI(PlayerId.Player1, rng);
        ai.PlayTurn(engine);

        // L'IA a dû poser le terrain, le flag pending est levé, et elle a terminé son tour
        Assert.False(engine.State.TerrainRequirementPending);
        Assert.Equal(1, engine.State.TerrainsInPlayCount);
        Assert.Equal(PlayerId.Player2, engine.State.ActivePlayer);
    }

    [Fact]
    public void DuelAI_ScoresOnlyWhenAheadOnForce()
    {
        var rng = new SystemRandomAdapter(456);
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0);
        var engine = new DuelEngine(rng, rules);

        var terrain = new DuelCard("T1", "Plaine", CardKind.Terrain);
        var oppStrongChar = new DuelCard("OppBig", "Big", CardKind.Character, force: 500);
        var aiScoreur = new DuelCard("Scoreur", "Scoreur", CardKind.Character, force: 100, isScoreur: true);

        engine.StartDuel(new[] { terrain }, new[] { terrain }, PlayerId.Player1);
        // Terrain actif sur slot 0
        engine.State.SetSlot(0, terrain, 0, PlayerId.Player1);
        // P2 a aussi une force de 500 sur le slot 1 pour qu'aucun slot ne soit prenable
        engine.State.SetSlot(1, terrain, 0, PlayerId.Player2);
        engine.State.AddCharacterToSlot(1, PlayerId.Player2, oppStrongChar);

        // P2 a 500 de force sur le slot 0
        engine.State.AddCharacterToSlot(0, PlayerId.Player2, oppStrongChar);

        // L'IA (P1) a un scoreur de force 100 (100 < 500)
        engine.State.AddCardToHand(PlayerId.Player1, aiScoreur);

        engine.StartTurn();

        var ai = new DuelAI(PlayerId.Player1, rng);
        ai.PlayTurn(engine);

        // L'IA ne doit PAS avoir joué son scoreur car elle perdrait le terrain
        Assert.Equal(0, engine.State.ScoreP1);
        Assert.False(engine.State.Slots[0].IsEmpty);
        Assert.Empty(engine.State.Slots[0].CharactersP1);
        Assert.Contains(engine.State.GetHand(PlayerId.Player1), c => c.Id == "Scoreur");
    }

    [Fact]
    public void Simulation_AI_vs_AI_200Seeds_AllCompleteWithoutExceptionsOrInfiniteLoops()
    {
        const int totalSimulations = 200;
        const int maxTurnsSafeguard = 500;

        int fiveTerrainsVictories = 0;
        int deckOutVictories = 0;

        for (int seed = 1; seed <= totalSimulations; seed++)
        {
            var rng = new SystemRandomAdapter(seed);
            var rules = new DuelRules(
                startingHandSize: 5,
                cardsDrawnPerTurn: 2,
                terrainsToWin: 5,
                maxCharactersPerTurn: 4,
                maxTerrainsOnBoard: 3
            );

            var engine = new DuelEngine(rng, rules);
            var aiP1 = new DuelAI(PlayerId.Player1, rng);
            var aiP2 = new DuelAI(PlayerId.Player2, rng);

            var deckP1 = CreateRealisticDeck("P1");
            var deckP2 = CreateRealisticDeck("P2");

            var startResult = engine.StartDuel(deckP1, deckP2);
            Assert.True(startResult.Success, $"StartDuel failed on seed {seed}: {startResult.ErrorReason}");

            while (!engine.State.IsGameOver && engine.State.TurnNumber < maxTurnsSafeguard)
            {
                engine.StartTurn();
                if (engine.State.IsGameOver) break;

                if (engine.State.ActivePlayer == PlayerId.Player1)
                {
                    aiP1.PlayTurn(engine);
                }
                else
                {
                    aiP2.PlayTurn(engine);
                }
            }

            // Vérification stricte : la partie doit être terminée
            Assert.True(engine.State.IsGameOver, $"Game did not finish within {maxTurnsSafeguard} turns on seed {seed}!");
            Assert.NotNull(engine.State.Winner);
            Assert.NotNull(engine.State.GameOverReason);

            if (engine.State.GameOverReason == GameOverReason.FiveTerrains)
            {
                fiveTerrainsVictories++;
            }
            else if (engine.State.GameOverReason == GameOverReason.DeckOut)
            {
                deckOutVictories++;
            }
        }

        // Vérifier qu'on observe les deux types de victoires sur 200 parties
        Assert.True(fiveTerrainsVictories > 0, "No FiveTerrains victories observed in 200 runs.");
        Assert.True(deckOutVictories > 0, "No DeckOut victories observed in 200 runs.");
    }
}
