using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.duel;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    [Collection("Sequential")]
    public class DuelSeason3EffectsTests
    {
        private sealed class NoShuffleRandom : IRandom
        {
            public int Next(int minInclusive, int maxExclusive) => minInclusive;
            public int Next(int maxExclusive) => 0;
            public void Shuffle<T>(IList<T> list) { }
        }

        private sealed class BattleEffectEntry
        {
            public int cardIndex { get; set; }
            public int? costOverride { get; set; }
            public List<string> onPlay { get; set; } = new();
            public List<string> combo { get; set; } = new();
            public List<string> passive { get; set; } = new();
            public List<string> sub { get; set; } = new();
            public List<string> win { get; set; } = new();
            public List<string> lose { get; set; } = new();
        }

        private static string GetEffectsS3Path()
        {
            string baseDir = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "data", "customdecks", "effects_s3.json"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "data", "customdecks", "effects_s3.json")),
                Path.GetFullPath(Path.Combine("data", "customdecks", "effects_s3.json"))
            };
            foreach (var p in candidates)
            {
                if (File.Exists(p)) return p;
            }
            return candidates[1];
        }

        private static List<BattleEffectEntry> LoadEffectsS3()
        {
            string path = GetEffectsS3Path();
            Assert.True(File.Exists(path), $"effects_s3.json introuvable: {path}");
            string json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<BattleEffectEntry>>(json);
            Assert.NotNull(entries);
            return entries!;
        }

        [Fact]
        public void Execute_SelfMillEffect_MillsCorrectAmount()
        {
            var rng = new SystemRandomAdapter();
            var engine = new DuelEngine(rng);

            var deckP1 = new List<DuelCard>();
            for (int i = 0; i < 30; i++) deckP1.Add(new DuelCard($"C{i}", "C", CardKind.Character));
            var deckP2 = new List<DuelCard>();
            for (int i = 0; i < 30; i++) deckP2.Add(new DuelCard($"C{i}", "C", CardKind.Character));

            engine.StartDuel(deckP1, deckP2);

            int initialDeckSize = engine.State.GetDeck(PlayerId.Player1).Count;
            int initialDiscardSize = engine.State.GetDiscard(PlayerId.Player1).Count;

            var effect = new SelfMillEffect("self_mill_2", 2);
            var context = new EffectContext(engine, PlayerId.Player1, new DuelCard("T", "T", CardKind.Terrain), 0);

            effect.Execute(context);

            Assert.Equal(initialDeckSize - 2, engine.State.GetDeck(PlayerId.Player1).Count);
            Assert.Equal(initialDiscardSize + 2, engine.State.GetDiscard(PlayerId.Player1).Count);
        }

        [Fact]
        public void EffectsS3Json_HasExact180Entries_300001To300180_AndNoLegacyBoostScale()
        {
            var entries = LoadEffectsS3();
            Assert.Equal(180, entries.Count);

            for (int i = 0; i < 180; i++)
            {
                Assert.Equal(300001 + i, entries[i].cardIndex);
                Assert.NotNull(entries[i].onPlay);
                Assert.NotNull(entries[i].combo);
                Assert.NotNull(entries[i].passive);
                Assert.NotNull(entries[i].sub);
                Assert.NotNull(entries[i].win);
                Assert.NotNull(entries[i].lose);

                var allIds = entries[i].onPlay
                    .Concat(entries[i].combo)
                    .Concat(entries[i].passive)
                    .Concat(entries[i].sub)
                    .Concat(entries[i].win)
                    .Concat(entries[i].lose);

                foreach (var id in allIds)
                {
                    Assert.False(string.IsNullOrWhiteSpace(id));
                    Assert.DoesNotMatch(@"^boost_self_(1[5-9]|[2-9][0-9])$", id);
                }
            }
        }

        [Fact]
        public void EffectsS3Json_BattleTerrains_HaveExpectedPassiveEffects()
        {
            var byIndex = LoadEffectsS3().ToDictionary(e => e.cardIndex);

            Assert.Contains("passive_terrain_win_most_characters", byIndex[300003].passive); // Course de Billes
            Assert.Contains("passive_cannot_leave_discard", byIndex[300011].passive); // Backrooms
            Assert.Contains("passive_terrain_remove_laink_gems", byIndex[300015].passive); // Survivor
            Assert.Contains("passive_terrain_remove_terracid_gems", byIndex[300020].passive); // La Ferme de Thierry
            Assert.Contains("passive_terrain_double_win_if_diff_4", byIndex[300021].passive); // Station Essence
            Assert.Contains("passive_discard_force_zero_chars", byIndex[300030].passive); // Cibles
        }

        [Fact]
        public void EffectsS3Json_KeyBattleCharacters_AreMappedInCorrectBuckets()
        {
            var byIndex = LoadEffectsS3().ToDictionary(e => e.cardIndex);

            // Surfeur (300045, 300046)
            Assert.Contains("mill_opp_by_line_pos", byIndex[300045].onPlay);
            Assert.Contains("draw_by_line_pos", byIndex[300046].onPlay);

            // Vitrail (300077/300169, 300078/300170)
            Assert.Contains("passive_terrain_grant_violet_gems", byIndex[300077].passive);
            Assert.Contains("passive_terrain_grant_orange_gems", byIndex[300078].passive);
            Assert.Contains("passive_terrain_grant_violet_gems", byIndex[300169].passive);
            Assert.Contains("passive_terrain_grant_orange_gems", byIndex[300170].passive);

            // Peepo (300060)
            Assert.Contains("remove_opp_opening_gem_turn", byIndex[300060].onPlay);

            // Réalisateur (300047, 300048)
            Assert.Contains("swap_two_chars_in_play", byIndex[300047].combo);
            Assert.Contains("passive_autoscore_threshold_16", byIndex[300048].passive);

            // Français (300036)
            Assert.Contains("sub_cancel_discard_or_mill", byIndex[300036].sub);

            // Terracix (300092)
            Assert.Contains("passive_gain_violet_gems_if_dog", byIndex[300092].passive);
            Assert.Contains("passive_boost_if_pig_2", byIndex[300092].combo);

            // Masque à Gaz (300113)
            Assert.Contains("banish_hand_char_copy_stats", byIndex[300113].onPlay);

            // Cyprien (300120, 300162)
            Assert.Contains("remove_own_gems_boost_1_per_gem", byIndex[300120].onPlay);
            Assert.Contains("score_current_terrain", byIndex[300120].combo);

            // Emo (300043, 300044)
            Assert.Contains("return_top_discard_to_deck_2", byIndex[300043].combo);
            Assert.Contains("passive_opp_autoscore_plus_4", byIndex[300044].passive);

            // Bodybuilder (300049, 300050)
            Assert.Contains("passive_force_per_hand_card_minus_1", byIndex[300049].passive);
            Assert.Contains("passive_force_per_opp_hand_card_minus_1", byIndex[300050].passive);

            // Ouvrier (300031, 300032)
            Assert.Contains("passive_cost_ouvrier_minus_1", byIndex[300031].passive);
            Assert.Contains("passive_cost_ouvrier_opp_plus_1", byIndex[300032].passive);

            // Téléachat (300051, 300052)
            Assert.Contains("passive_cost_set_1", byIndex[300051].passive);
            Assert.Contains("passive_start_turn_mill_per_char", byIndex[300051].passive);
            Assert.Contains("passive_cost_opp_plus_2", byIndex[300052].passive);

            // Cartes à texte vide dans battle.json mais avec gemmes/effets (300091 Lainkix, 300175 Boss des Saviors)
            Assert.Contains("draw_2", byIndex[300091].combo);
            Assert.Contains("passive_once_turn_discard_2_double_force", byIndex[300091].passive);
            Assert.Equal(3, byIndex[300175].costOverride);
            Assert.Contains("banish_opp_char_any", byIndex[300175].combo);
        }

        [Fact]
        public void BattleMechanics_Surfeur_DrawsAndMillsByLinePosition_WhenRegistered()
        {
            var engine = new DuelEngine(new NoShuffleRandom(), new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0));
            var deckP1 = Enumerable.Range(1, 10).Select(i => new DuelCard($"D1_{i}", $"D1_{i}", CardKind.Character)).ToList();
            var deckP2 = Enumerable.Range(1, 10).Select(i => new DuelCard($"D2_{i}", $"D2_{i}", CardKind.Character)).ToList();
            engine.StartDuel(deckP1, deckP2);

            var terrain = new DuelCard("T1", "Terrain", CardKind.Terrain);
            engine.State.SetSlot(0, terrain, placedOnTurn: 0, placedBy: PlayerId.Player1);

            var filler1 = new DuelCard("F1", "Filler1", CardKind.Character, force: 1);
            var filler2 = new DuelCard("F2", "Filler2", CardKind.Character, force: 1);
            var surfeurDraw = new DuelCard("S_Draw", "SURFEUR", CardKind.Character, force: 1, effectIds: new[] { "draw_by_line_pos" });
            var surfeurMill = new DuelCard("S_Mill", "SURFEUR", CardKind.Character, force: 1, effectIds: new[] { "mill_opp_by_line_pos" });

            var slot = engine.State.Slots[0];
            slot.AddCharacter(PlayerId.Player1, filler1);
            slot.AddCharacter(PlayerId.Player1, filler2);
            slot.AddCharacter(PlayerId.Player1, surfeurDraw); // 3rd position -> draws 3

            if (engine.Effects.TryGetEffect("draw_by_line_pos", out var drawEff) && drawEff != null)
            {
                var res = drawEff.Execute(new EffectContext(engine, PlayerId.Player1, surfeurDraw, 0));
                Assert.True(res.Success);
                Assert.Equal(3, engine.State.GetHand(PlayerId.Player1).Count);
            }

            slot.AddCharacter(PlayerId.Player1, surfeurMill); // 4th position -> opponent mills 4
            if (engine.Effects.TryGetEffect("mill_opp_by_line_pos", out var millEff) && millEff != null)
            {
                var res = millEff.Execute(new EffectContext(engine, PlayerId.Player1, surfeurMill, 0));
                Assert.True(res.Success);
                Assert.Equal(4, engine.State.GetDiscard(PlayerId.Player2).Count);
            }
        }

        [Fact]
        public void BattleMechanics_Vitrail_Peepo_Realisateur_Francais_ExecuteWithoutError()
        {
            var engine = new DuelEngine(new NoShuffleRandom(), new DuelRules(startingHandSize: 0, cardsDrawnPerTurn: 0));
            var deckP1 = Enumerable.Range(1, 10).Select(i => new DuelCard($"D1_{i}", $"D1_{i}", CardKind.Character)).ToList();
            var deckP2 = Enumerable.Range(1, 10).Select(i => new DuelCard($"D2_{i}", $"D2_{i}", CardKind.Character)).ToList();
            engine.StartDuel(deckP1, deckP2);

            engine.State.SetSlot(0, new DuelCard("T1", "Terrain", CardKind.Terrain), placedOnTurn: 0, placedBy: PlayerId.Player1);
            var slot = engine.State.Slots[0];

            var vitrail = new DuelCard("V1", "VITRAIL", CardKind.Character, force: 1, effectIds: new[] { "passive_terrain_grant_violet_gems" });
            var peepo = new DuelCard("P1", "PEEPO", CardKind.Character, force: 1, effectIds: new[] { "remove_opp_opening_gem_turn" });
            var realisateur = new DuelCard("R1", "RÉALISATEUR", CardKind.Character, force: 2, effectIds: new[] { "passive_autoscore_threshold_16" }, comboEffectIds: new[] { "swap_two_chars_in_play" });
            var francais = new DuelCard("FR1", "FRANÇAIS", CardKind.Character, force: 1, effectIds: new[] { "sub_cancel_discard_or_mill" });
            var oppChar = new DuelCard("O1", "Opp", CardKind.Character, force: 2, gemMask: GemMask.OpeningOrange | GemMask.OpeningViolet);

            slot.AddCharacter(PlayerId.Player1, vitrail);
            slot.AddCharacter(PlayerId.Player1, peepo);
            slot.AddCharacter(PlayerId.Player1, realisateur);
            slot.AddCharacter(PlayerId.Player2, oppChar);

            foreach (var id in new[] { "remove_opp_opening_gem_turn", "swap_two_chars_in_play", "sub_cancel_discard_or_mill" })
            {
                if (engine.Effects.TryGetEffect(id, out var eff) && eff != null)
                {
                    var card = id == "remove_opp_opening_gem_turn" ? peepo : id == "swap_two_chars_in_play" ? realisateur : francais;
                    var res = eff.Execute(new EffectContext(engine, PlayerId.Player1, card, 0));
                    Assert.True(res.Success);
                }
            }
        }
    }
}
