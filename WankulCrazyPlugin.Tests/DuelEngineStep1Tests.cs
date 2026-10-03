using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests;

public class DuelEngineStep1Tests
{
    private static List<DuelCard> CreateDeck(int count, string prefix = "Card")
    {
        var list = new List<DuelCard>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new DuelCard($"{prefix}_{i}", $"{prefix} {i}", CardKind.Character, force: 100));
        }
        return list;
    }

    private sealed class DeterministicRandom : IRandom
    {
        private readonly int[] _results;
        private int _index;

        public DeterministicRandom(params int[] results)
        {
            _results = results;
        }

        public int Next(int minInclusive, int maxExclusive) => minInclusive;

        public int Next(int maxExclusive)
        {
            if (_results == null || _results.Length == 0) return 0;
            int val = _results[_index % _results.Length];
            _index++;
            return val % maxExclusive;
        }

        public void Shuffle<T>(IList<T> list)
        {
            // Pas de mélange pour garder l'ordre strict des tests
        }
    }

    [Fact]
    public void DuelEngine_NoUnityEngineDependency_InEngineFolder()
    {
        // Règle 1 & 13 : duel/engine/ doit être en C# pur sans using UnityEngine
        string currentDir = Directory.GetCurrentDirectory();
        // Recherche vers la racine du repo
        string? projectRoot = currentDir;
        while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "duel", "engine")))
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName;
        }

        Assert.NotNull(projectRoot);
        string engineDir = Path.Combine(projectRoot!, "duel", "engine");
        Assert.True(Directory.Exists(engineDir), $"Engine dir not found at {engineDir}");

        var csFiles = Directory.GetFiles(engineDir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(csFiles);

        foreach (var file in csFiles)
        {
            string content = File.ReadAllText(file);
            Assert.DoesNotContain("using UnityEngine", content);
            Assert.DoesNotContain("UnityEngine.", content);
        }
    }

    [Fact]
    public void StartDuel_InitializesHandsAndState_Correctly()
    {
        var rng = new DeterministicRandom(0);
        var rules = new DuelRules(startingHandSize: 5);
        var engine = new DuelEngine(rng, rules);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        var deck1 = CreateDeck(20, "P1");
        var deck2 = CreateDeck(20, "P2");

        var result = engine.StartDuel(deck1, deck2, PlayerId.Player1);

        Assert.True(result.Success);
        Assert.Equal(PlayerId.Player1, engine.State.ActivePlayer);
        Assert.Equal(5, engine.State.HandP1.Count);
        Assert.Equal(5, engine.State.HandP2.Count);
        Assert.Equal(15, engine.State.DeckP1.Count);
        Assert.Equal(15, engine.State.DeckP2.Count);

        Assert.Contains(events, e => e is DuelStartedEvent started && started.StartingPlayer == PlayerId.Player1);
        Assert.Equal(2, events.Count(e => e is CardsDrawnEvent));
    }

    [Fact]
    public void StartTurn_DrawsTwoCards_ForActivePlayer()
    {
        var rng = new DeterministicRandom(0);
        var rules = new DuelRules(startingHandSize: 5, cardsDrawnPerTurn: 2, firstPlayerDrawsOnTurn1: true);
        var engine = new DuelEngine(rng, rules);

        engine.StartDuel(CreateDeck(20, "P1"), CreateDeck(20, "P2"), PlayerId.Player1);

        var result = engine.StartTurn();

        Assert.True(result.Success);
        Assert.Equal(1, engine.State.TurnNumber);
        Assert.Equal(7, engine.State.HandP1.Count); // 5 de départ + 2 piochées
        Assert.Equal(13, engine.State.DeckP1.Count); // 20 - 5 - 2
    }

    [Fact]
    public void DrawCards_WhenDeckHasOneCardLeft_DrawsLastCardThenTriggersDeckOut()
    {
        // Précision demandée : s'il reste 1 carte et qu'on doit en piocher 2,
        // la dernière carte est bien ajoutée en main, puis DeckOut et victoire de l'adversaire.
        var rng = new DeterministicRandom(0);
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 2);
        var engine = new DuelEngine(rng, rules);

        var events = new List<IDuelEvent>();
        engine.OnEvent += events.Add;

        // Deck P1 a 1 seule carte, deck P2 en a 10
        engine.StartDuel(CreateDeck(1, "P1"), CreateDeck(10, "P2"), PlayerId.Player1);

        // Au tour 1, P1 tente de piocher 2 cartes
        var turnResult = engine.StartTurn();

        Assert.True(turnResult.Success);
        // La dernière carte a été piochée dans la main de P1
        Assert.Single(engine.State.HandP1);
        Assert.Empty(engine.State.DeckP1);

        // Partie terminée par DeckOut avec P2 comme gagnant
        Assert.True(engine.State.IsGameOver);
        Assert.Equal(PlayerId.Player2, engine.State.Winner);
        Assert.Equal(GameOverReason.DeckOut, engine.State.GameOverReason);

        Assert.Contains(events, e => e is DeckExhaustedEvent exhausted && exhausted.Player == PlayerId.Player1);
        Assert.Contains(events, e => e is DuelEndedEvent ended && ended.Winner == PlayerId.Player2 && ended.Reason == GameOverReason.DeckOut);
    }

    [Fact]
    public void DrawCards_WhenDeckEmpty_EndsImmediatelyWithDeckOut()
    {
        var rng = new DeterministicRandom(0);
        var rules = new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 2);
        var engine = new DuelEngine(rng, rules);

        engine.StartDuel(CreateDeck(0, "P1"), CreateDeck(10, "P2"), PlayerId.Player1);

        engine.StartTurn();

        Assert.Empty(engine.State.HandP1);
        Assert.True(engine.State.IsGameOver);
        Assert.Equal(PlayerId.Player2, engine.State.Winner);
        Assert.Equal(GameOverReason.DeckOut, engine.State.GameOverReason);
    }

    [Fact]
    public void FixedSeed_YieldsReproducibleShuffleAndFirstPlayer()
    {
        var deckA1 = CreateDeck(10, "Card");
        var deckA2 = CreateDeck(10, "Card");
        var deckB1 = CreateDeck(10, "Card");
        var deckB2 = CreateDeck(10, "Card");

        var rng1 = new SystemRandomAdapter(seed: 42);
        var rng2 = new SystemRandomAdapter(seed: 42);

        var engine1 = new DuelEngine(rng1, new DuelRules(startingHandSize: 3));
        var engine2 = new DuelEngine(rng2, new DuelRules(startingHandSize: 3));

        engine1.StartDuel(deckA1, deckA2);
        engine2.StartDuel(deckB1, deckB2);

        Assert.Equal(engine1.State.ActivePlayer, engine2.State.ActivePlayer);
        Assert.Equal(
            engine1.State.HandP1.Select(c => c.Id).ToList(),
            engine2.State.HandP1.Select(c => c.Id).ToList()
        );
    }
}
