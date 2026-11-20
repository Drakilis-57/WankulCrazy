using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.duel;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTests")]
    public class DuelCardEffectsValidationTests
    {
        private sealed class NoShuffleRandom : IRandom
        {
            public int Next(int minInclusive, int maxExclusive) => minInclusive;
            public int Next(int maxExclusive) => 0;
            public void Shuffle<T>(IList<T> list) { }
        }

        private class EffectMappingDto
        {
            public int cardIndex { get; set; }
            public List<int>? cardIndexes { get; set; }
            public int? costOverride { get; set; }
            public bool isDuo { get; set; }
            public List<string>? onPlay { get; set; }
            public List<string>? combo { get; set; }
            public List<string>? passive { get; set; }
            public List<string>? sub { get; set; }
            public List<string>? win { get; set; }
            public List<string>? lose { get; set; }
        }

        private static string ResolveCustomDecksFile(string fileName)
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "customdecks", fileName),
                Path.Combine("data", "customdecks", fileName),
                Path.Combine("..", "..", "..", "..", "data", "customdecks", fileName)
            };
            foreach (var p in candidates)
            {
                if (File.Exists(p)) return p;
            }
            return candidates[2];
        }

        [Fact]
        public void DuelCardEffectsJson_ContainsValidEffects_ForAll880Cards()
        {
            string effectsPath = ResolveCustomDecksFile("duel_card_effects.json");
            Assert.True(File.Exists(effectsPath), "JSON file not found: " + effectsPath);

            string json = File.ReadAllText(effectsPath, System.Text.Encoding.UTF8);
            var mappings = JsonConvert.DeserializeObject<List<EffectMappingDto>>(json);

            Assert.NotNull(mappings);
            Assert.NotEmpty(mappings);

            var engine = new DuelEngine(new NoShuffleRandom());
            var registry = engine.Effects;
            var seenIndices = new HashSet<int>();
            int totalEffectsChecked = 0;

            foreach (var mapping in mappings!)
            {
                var indices = new List<int>();
                if (mapping.cardIndex > 0) indices.Add(mapping.cardIndex);
                if (mapping.cardIndexes != null) indices.AddRange(mapping.cardIndexes);

                Assert.NotEmpty(indices);
                foreach (int idx in indices)
                {
                    Assert.True(seenIndices.Add(idx), $"Duplicate cardIndex {idx} in duel_card_effects.json");
                }

                var buckets = new[] { mapping.onPlay, mapping.combo, mapping.passive, mapping.sub, mapping.win, mapping.lose };
                foreach (var bucket in buckets)
                {
                    if (bucket == null) continue;
                    foreach (var effectId in bucket)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(effectId), $"Empty effect ID on card(s) {string.Join(",", indices)}");
                        Assert.True(
                            registry.TryGetEffect(effectId, out _),
                            $"Effect ID '{effectId}' on card(s) {string.Join(",", indices)} is not registered in EffectRegistry.");
                        totalEffectsChecked++;
                    }
                }
            }

            Assert.Equal(880, seenIndices.Count);
            Assert.True(totalEffectsChecked > 1000, $"Expected > 1000 validated effect references, got {totalEffectsChecked}");
        }

        [Fact]
        public void Card400050_WithoutEmptyGemsEntry_DoesNotReceiveClosingGemByFallback()
        {
            var card400050 = new EffigyCardData
            {
                Index = 400050,
                Number = "S4-050",
                Title = "À LA RECHERCHE DU RESPECT",
                CardType = CardType.Effigy,
                Force = 1,
                Cost = 0,
                Combo = "(Permanent) Lorsque cette carte est seule de votre côté d'un terrain, elle ne peut pas être défaussée par effet de carte."
            };

            var duelCard = CardAdapter.ToDuelCard(card400050);

            Assert.Equal(GemMask.None, duelCard.Gems);
            Assert.Empty(duelCard.ComboEffectIds);
            Assert.Contains("passive_no_discard_when_alone", duelCard.PassiveEffectIds);
            Assert.Contains("passive_on_mill_banish_opp_mill_2", duelCard.PassiveEffectIds);
        }

        [Fact]
        public void VanillaCard100103_WithOmittedEmptyBuckets_DoesNotFallBackToLegacyRegex()
        {
            var card100103 = new EffigyCardData
            {
                Index = 100103,
                Number = "S1-103",
                Title = "STEVE",
                CardType = CardType.Effigy,
                Force = 2,
                Cost = 0,
                Combo = "+ 20 de force"
            };

            var duelCard = CardAdapter.ToDuelCard(card100103);

            Assert.Empty(duelCard.EffectIds);
            Assert.Empty(duelCard.ComboEffectIds);
            Assert.Equal(GemMask.OpeningOrange, duelCard.Gems);
            Assert.False(duelCard.HasClosingGem);
        }
    }
}
