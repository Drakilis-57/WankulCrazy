using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Xunit;
using WankulCrazy.Duel.Engine;

namespace WankulCrazyPlugin.Tests
{
    public class DuelCardEffectsValidationTests
    {
        private class EffectMapping
        {
            public int cardIndex { get; set; }
            public List<string> effects { get; set; } = new List<string>();
        }

        [Fact]
        public void DuelCardEffectsJson_ContainsValidEffects()
        {
            // Resolve the path to the JSON file
            string effectsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "customdecks", "duel_card_effects.json");
            if (!File.Exists(effectsPath)) {
                effectsPath = Path.Combine("data", "customdecks", "duel_card_effects.json");
            }
            if (!File.Exists(effectsPath)) {
                effectsPath = Path.Combine("..", "..", "..", "..", "data", "customdecks", "duel_card_effects.json");
            }

            Assert.True(File.Exists(effectsPath), "JSON file not found: " + effectsPath);

            string json = File.ReadAllText(effectsPath, System.Text.Encoding.UTF8);
            var mappings = JsonConvert.DeserializeObject<List<EffectMapping>>(json);

            Assert.NotNull(mappings);
            Assert.NotEmpty(mappings);

            var registry = new EffectRegistry();

            foreach (var mapping in mappings)
            {
                foreach (var effectId in mapping.effects)
                {
                    Assert.True(registry.TryGetEffect(effectId, out _), $"Effect ID '{effectId}' on card {mapping.cardIndex} is not registered in EffectRegistry.");
                }
            }
        }
    }
}
