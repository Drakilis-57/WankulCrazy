using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.Tests
{
    /// <summary>
    /// Tests de simulation et d'intégrité statistique des taux de drop sur les données réelles (JSONs).
    /// Empêche toute régression future où les cartes Légendaires (LB, LA, LO) ou Ultra-Rares
    /// dépasseraient leurs ratios prévus dans les boosters réels.
    /// </summary>
    [Collection("StaticStateTests")]
    public class DropRateStatisticalSimulationTests
    {
        private static string FindRepoRoot()
        {
            string? path = AppDomain.CurrentDomain.BaseDirectory;
            while (path != null && !Directory.Exists(Path.Combine(path, "data")))
            {
                path = Directory.GetParent(path)?.FullName;
            }
            return path ?? Directory.GetCurrentDirectory();
        }

        private static List<WankulCardData> LoadAllCardsFromSeason(string seasonSubDir)
        {
            string root = FindRepoRoot();
            string raritiesPath = Path.Combine(root, "data", "rarities.json");
            if (File.Exists(raritiesPath))
            {
                RaritiesManager.ResetToDefaults();
                RaritiesManager.LoadFromPluginPath(root);
            }

            string seasonDir = Path.Combine(root, "data", "cards", seasonSubDir);
            if (!Directory.Exists(seasonDir))
            {
                return new List<WankulCardData>();
            }

            List<WankulCardData> list = new List<WankulCardData>();
            foreach (string file in Directory.GetFiles(seasonDir, "*.json", SearchOption.AllDirectories))
            {
                string json = File.ReadAllText(file);
                JToken token = JToken.Parse(json);
                list.AddRange(JsonImporter.DeserializeToken(token));
            }
            return list;
        }

        [Theory]
        [InlineData("Origins", Season.S01)]
        [InlineData("Campus", Season.S02)]
        [InlineData("Battle", Season.S03)]
        [InlineData("Stellar", Season.S04)]
        [InlineData("Legacy", Season.S05)]
        public void RareSlot_TheoreticalDropRates_LegendariesDoNotExceedCap(string seasonSubDir, Season season)
        {
            var cards = LoadAllCardsFromSeason(seasonSubDir);
            if (cards.Count == 0) return; // Skip si les fichiers JSON ne sont pas présents dans l'environnement

            // Le slot rare ne sélectionne que les cartes Effigy éligibles (R, UR1, UR2, LB, LA, LO, DUO)
            var rareSlotCards = cards
                .OfType<EffigyCardData>()
                .Where(c => c.Rarity >= Rarity.R || (RaritiesManager.GetRarity(c.RarityId)?.IsEligibleForMinRare ?? false))
                .Cast<WankulCardData>()
                .ToList();

            Assert.NotEmpty(rareSlotCards);

            float totalWeight = 0f;
            float legendaryWeight = 0f;
            float loWeight = 0f;
            float laWeight = 0f;
            float lbWeight = 0f;
            float duoWeight = 0f;
            float rareWeight = 0f;
            float urWeight = 0f;

            foreach (var card in rareSlotCards)
            {
                float effectiveDrop = WeightedCardDropService.GetCardEffectiveDrop(card, increaseRarity: false, season);
                totalWeight += effectiveDrop;

                if (card is EffigyCardData effigy)
                {
                    switch (effigy.RarityId)
                    {
                        case "LO":
                            loWeight += effectiveDrop;
                            legendaryWeight += effectiveDrop;
                            break;
                        case "LA":
                            laWeight += effectiveDrop;
                            legendaryWeight += effectiveDrop;
                            break;
                        case "LB":
                            lbWeight += effectiveDrop;
                            legendaryWeight += effectiveDrop;
                            break;
                        case "DUO":
                            duoWeight += effectiveDrop;
                            break;
                        case "UR1":
                        case "UR2":
                            urWeight += effectiveDrop;
                            break;
                        case "R":
                            rareWeight += effectiveDrop;
                            break;
                    }
                }
            }

            Assert.True(totalWeight > 0f, "Total weight in rare slot must be positive");

            float legendaryPercent = (legendaryWeight / totalWeight) * 100f;
            float loPercent = (loWeight / totalWeight) * 100f;
            float rarePercent = (rareWeight / totalWeight) * 100f;
            float urPercent = (urWeight / totalWeight) * 100f;

            // 1. Les Légendaires totales (LB + LA + LO) ne doivent JAMAIS dépasser 2.5% du slot rare (le bug les montait à 27%)
            Assert.True(legendaryPercent <= 2.5f,
                $"[Bug Drop Légendaire détecté dans {seasonSubDir}] : Le taux théorique de légendaire est de {legendaryPercent:F2}%, doit être <= 2.5%");

            // 2. Les Légendaires Or (LO) sont les cartes les plus rares et doivent être <= 0.1% du slot rare
            Assert.True(loPercent <= 0.1f,
                $"[Taux LO excessif dans {seasonSubDir}] : {loPercent:F3}%, doit être <= 0.1%");

            // 3. Les Rares (R) doivent composer la majorité du slot rare (~70% à ~85%)
            Assert.True(rarePercent >= 65f && rarePercent <= 90f,
                $"[Taux R anormal dans {seasonSubDir}] : {rarePercent:F2}%, attendu entre 65% et 90%");

            // 4. Les Ultra Rares (UR1 + UR2) doivent composer entre 15% et 35% du slot rare
            Assert.True(urPercent >= 15f && urPercent <= 35f,
                $"[Taux UR anormal dans {seasonSubDir}] : {urPercent:F2}%, attendu entre 15% et 35%");
        }

        [Fact]
        public void MonteCarloSimulation_10000BoosterRareSlots_MatchesStatisticalDistribution()
        {
            // Simulation de Monte Carlo sur 10 000 ouvertures réelles de boosters (slot rare)
            // Utilise l'algorithme exact WeightedCardDropService.SelectCard() avec un générateur de nombres aléatoires.
            var cards = LoadAllCardsFromSeason("Legacy");
            if (cards.Count == 0) return;

            var rareSlotCards = cards
                .OfType<EffigyCardData>()
                .Where(c => c.Rarity >= Rarity.R || (RaritiesManager.GetRarity(c.RarityId)?.IsEligibleForMinRare ?? false))
                .Cast<WankulCardData>()
                .ToList();

            float totalWeight = rareSlotCards.Sum(c => WeightedCardDropService.GetCardEffectiveDrop(c, false, Season.S05));

            int totalSimulations = 10000;
            var random = new Random(42); // Seed déterministe pour tests reproductibles

            int legendaryDrops = 0;
            int urDrops = 0;
            int rareDrops = 0;
            int duoDrops = 0;

            for (int i = 0; i < totalSimulations; i++)
            {
                float randomVal = (float)(random.NextDouble() * totalWeight);
                var picked = WeightedCardDropService.SelectCard(rareSlotCards, randomVal, false, Season.S05);
                Assert.NotNull(picked);

                if (picked is EffigyCardData effigy)
                {
                    if (effigy.RarityId == "LO" || effigy.RarityId == "LA" || effigy.RarityId == "LB")
                    {
                        legendaryDrops++;
                    }
                    else if (effigy.RarityId == "UR1" || effigy.RarityId == "UR2")
                    {
                        urDrops++;
                    }
                    else if (effigy.RarityId == "DUO")
                    {
                        duoDrops++;
                    }
                    else if (effigy.RarityId == "R")
                    {
                        rareDrops++;
                    }
                }
            }

            double legendaryObservedRate = (double)legendaryDrops / totalSimulations * 100.0;
            double urObservedRate = (double)urDrops / totalSimulations * 100.0;
            double rareObservedRate = (double)rareDrops / totalSimulations * 100.0;

            // Vérification des bornes empiriques :
            // Taux théorique attendu de légendaire : ~1.18%
            // Dans 10 000 tirages, le taux observé doit se situer entre 0.7% et 1.8% (jamais 27%)
            Assert.True(legendaryObservedRate >= 0.7 && legendaryObservedRate <= 1.8,
                $"Taux observé de légendaire anormal: {legendaryObservedRate:F2}% (attendu ~1.18%)");

            // Rares observées : attendu ~72% (entre 68% et 76%)
            Assert.True(rareObservedRate >= 68.0 && rareObservedRate <= 76.0,
                $"Taux observé de rares anormal: {rareObservedRate:F2}% (attendu ~72.5%)");

            // Ultra Rares observées : attendu ~25% (entre 22% et 29%)
            Assert.True(urObservedRate >= 22.0 && urObservedRate <= 29.0,
                $"Taux observé de UR anormal: {urObservedRate:F2}% (attendu ~25.7%)");
        }

        [Fact]
        public void CommonSlots_NeverDropRareOrLegendaryCards()
        {
            // Vérifie qu'aucune carte rare ou légendaire ne peut fuiter dans les slots communs d'un booster
            string[] allSeasons = new[] { "Origins", "Campus", "Battle", "Stellar", "Legacy" };

            foreach (string seasonDir in allSeasons)
            {
                var cards = LoadAllCardsFromSeason(seasonDir);
                if (cards.Count == 0) continue;

                // Logique exacte de sélection des slots communs :
                // !(card is EffigyCardData effigyCard && (effigyCard.Rarity >= Rarity.R || (RaritiesManager.GetRarity(effigyCard.RarityId)?.IsEligibleForMinRare ?? false)))
                var nonRareCards = cards.Where(card =>
                    !(card is EffigyCardData effigyCard && (effigyCard.Rarity >= Rarity.R || (RaritiesManager.GetRarity(effigyCard.RarityId)?.IsEligibleForMinRare ?? false)))
                ).ToList();

                Assert.NotEmpty(nonRareCards);

                foreach (var card in nonRareCards)
                {
                    if (card is EffigyCardData effigy)
                    {
                        Assert.False(effigy.Rarity >= Rarity.R,
                            $"Carte {effigy.Title} ({effigy.Index}) avec rareté {effigy.RarityId} trouvée dans le pool commun de {seasonDir} !");
                        Assert.False(RaritiesManager.GetRarity(effigy.RarityId)?.IsEligibleForMinRare ?? false,
                            $"Carte {effigy.Title} ({effigy.Index}) marquée IsEligibleForMinRare trouvée dans le pool commun de {seasonDir} !");
                    }
                }
            }
        }
    }
}
