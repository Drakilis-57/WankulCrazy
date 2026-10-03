using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WankulCrazyPlugin.patch;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    public class WankulDuelEffectsTests
    {
        [Fact]
        public void CardEffectsJson_ExistsAndContainsValidEntries()
        {
            // Vérifier le fichier data/customdecks/card_effects.json
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "customdecks", "card_effects.json");
            
            // Si exécuté depuis WankulCrazyPlugin.Tests/bin/... remonter au root
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "data", "customdecks", "card_effects.json");
            }
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "data", "customdecks", "card_effects.json");
            }

            Assert.True(File.Exists(jsonPath), $"Le fichier card_effects.json est introuvable à : {jsonPath}");

            string json = File.ReadAllText(jsonPath);
            var list = JsonConvert.DeserializeObject<List<WankulCardEffectConfig>>(json);

            Assert.NotNull(list);
            Assert.True(list.Count >= 15, $"Attendu au moins 15 cartes configurées, trouvé {list.Count}");

            foreach (var config in list)
            {
                Assert.True(config.cardIndex > 0, "cardIndex doit être positif");
                Assert.False(string.IsNullOrEmpty(config.cardTitle), "cardTitle ne doit pas être vide");
                Assert.NotEmpty(config.effects);

                foreach (var effect in config.effects)
                {
                    Assert.False(string.IsNullOrEmpty(effect.type), "type d'effet ne doit pas être vide");
                    Assert.True(effect.count > 0, "count d'effet doit être > 0");
                }
            }
        }
    }
}
