using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.Tests
{
    /// <summary>
    /// Régression pour le problème documenté en Docs/DOCS.md §8.1 :
    /// des RarityId invalides (ex: "L-B" au lieu de "LB") dans un fichier de cartes
    /// passent inaperçus car RarityJsonConverter retombe silencieusement sur Rarity.C
    /// et enregistre une rareté dynamique avec des multiplicateurs par défaut (1.0f).
    /// Ce test s'assure qu'aucun RarityId présent dans data/cards/**/*.json
    /// ne "invente" une rareté qui n'est pas déjà déclarée dans data/rarities.json.
    /// </summary>
    [Collection("StaticStateTests")]
    public class LegacyCardsRarityValidationTests
    {
        private static string FindRepoRoot()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            while (path != null && !Directory.Exists(Path.Combine(path, "data")))
            {
                path = Directory.GetParent(path)?.FullName!;
            }
            return path ?? Directory.GetCurrentDirectory();
        }

        [Fact]
        public void RaritiesJson_ContainsDuoEntry()
        {
            // Régression directe du bug corrigé : "DUO" doit être déclaré dans rarities.json,
            // sinon toute carte Duo retombe sur Rarity.C avec des multiplicateurs par défaut.
            string root = FindRepoRoot();
            string raritiesPath = Path.Combine(root, "data", "rarities.json");
            Assert.True(File.Exists(raritiesPath), $"rarities.json introuvable à {raritiesPath}");

            JArray array = JArray.Parse(File.ReadAllText(raritiesPath));
            HashSet<string> ids = new HashSet<string>(
                array.Select(r => (string?)r["Id"]).Where(id => id != null).Select(id => id!),
                StringComparer.OrdinalIgnoreCase);

            Assert.Contains("DUO", ids);
        }

        [Fact]
        public void AllCardFiles_RarityIds_AreDeclaredInRaritiesJson()
        {
            RaritiesManager.ResetToDefaults();
            string root = FindRepoRoot();

            string raritiesPath = Path.Combine(root, "data", "rarities.json");
            if (File.Exists(raritiesPath))
            {
                RaritiesManager.LoadFromPluginPath(root);
            }

            string cardsDirectory = Path.Combine(root, "data", "cards");
            if (!Directory.Exists(cardsDirectory))
            {
                // Pas de fichiers de cartes disponibles dans cet environnement (CI headless) : rien à valider.
                return;
            }

            var unknownRarityIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string filePath in Directory.GetFiles(cardsDirectory, "*.json", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(filePath);
                JToken token = JToken.Parse(File.ReadAllText(filePath));

                IEnumerable<JToken> cardTokens = token.Type == JTokenType.Array
                    ? token.Children()
                    : Enumerable.Empty<JToken>();

                foreach (JToken cardToken in cardTokens)
                {
                    string? rarityId = (string?)cardToken["RarityId"];
                    if (string.IsNullOrEmpty(rarityId))
                    {
                        continue; // Terrains/Specials sans rareté propre : hors périmètre de ce test.
                    }

                    if (RaritiesManager.GetRarity(rarityId) == null)
                    {
                        if (!unknownRarityIds.TryGetValue(rarityId, out var files))
                        {
                            files = new List<string>();
                            unknownRarityIds[rarityId] = files;
                        }
                        if (!files.Contains(fileName))
                        {
                            files.Add(fileName);
                        }
                    }
                }
            }

            if (unknownRarityIds.Count > 0)
            {
                string details = string.Join("; ", unknownRarityIds.Select(kv =>
                    $"'{kv.Key}' utilisé dans [{string.Join(", ", kv.Value)}]"));
                Assert.Fail(
                    "RarityId non déclaré(s) dans data/rarities.json : " + details +
                    ". Corrigez la faute de frappe dans le fichier de cartes, ou ajoutez l'entrée manquante dans rarities.json.");
            }
        }

        [Theory]
        [InlineData("L-B")]
        [InlineData("L-A")]
        [InlineData("L-O")]
        public void LegacyMisspelledRarityIds_NoLongerPresentInAnyCardFile(string misspelledId)
        {
            // Régression directe : ces identifiants fautifs ("L-B"/"L-A"/"L-O") ne doivent
            // plus apparaître dans aucun fichier de cartes après la correction de legacy.json.
            string root = FindRepoRoot();
            string cardsDirectory = Path.Combine(root, "data", "cards");
            if (!Directory.Exists(cardsDirectory))
            {
                return;
            }

            foreach (string filePath in Directory.GetFiles(cardsDirectory, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(filePath);
                Assert.DoesNotContain($"\"RarityId\": \"{misspelledId}\"", content);
            }
        }
    }
}
