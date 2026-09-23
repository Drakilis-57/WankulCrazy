using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    public static class RaritiesManager
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, RarityData> raritiesById = new Dictionary<string, RarityData>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<RarityData> allRarities = new List<RarityData>();

        static RaritiesManager()
        {
            ResetToDefaults();
        }

        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                raritiesById.Clear();
                allRarities.Clear();

                RegisterRarityInternal(new RarityData("C", "Common", 1.0f, 1.0f));
                RegisterRarityInternal(new RarityData("UC", "Uncommon", 2.0f, 1.0f));
                RegisterRarityInternal(new RarityData("R", "Rare", 4.0f, 1.0f));
                RegisterRarityInternal(new RarityData("UR1", "Ultra Rare 1", 7.0f, 1.0f));
                RegisterRarityInternal(new RarityData("UR2", "Ultra Rare 2", 10.0f, 1.0f));
                RegisterRarityInternal(new RarityData("LB", "Legendary Bronze", 15.0f, 1.0f));
                RegisterRarityInternal(new RarityData("LA", "Legendary Argent", 20.0f, 1.0f));
                RegisterRarityInternal(new RarityData("LO", "Legendary Or", 25.0f, 1.0f));
                RegisterRarityInternal(new RarityData("PGW23", "PGW 23", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("NOEL23", "Noel 23", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("PGW24", "PGW 24", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("SPCIV", "Starter Pack Civilisations", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("SPLEG", "Starter Pack Legendes", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("ED", "Edition speciale", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("SPPOP", "Starter Pack Pop Culture", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("GP", "Gemmes Pack", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("SPTV", "Starter Pack TV", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("SPJV", "Starter Pack Jeux Video", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("EG", "Edition Gold", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("SPCAR", "Starter Pack Carrières", 50.0f, 1.0f));
                RegisterRarityInternal(new RarityData("TOR", "The One Ring", 50.0f, 1.0f));
            }
        }

        public static void LoadFromPluginPath(string pluginPath)
        {
            ResetToDefaults();

            string raritiesPath = Path.Combine(pluginPath, "data/rarities.json");
            if (!File.Exists(raritiesPath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(raritiesPath);
                List<RarityData> loadedRarities = JsonConvert.DeserializeObject<List<RarityData>>(json);
                if (loadedRarities != null && loadedRarities.Count > 0)
                {
                    lock (_lock)
                    {
                        foreach (var r in loadedRarities)
                        {
                            if (!string.IsNullOrWhiteSpace(r.Id))
                            {
                                RegisterRarityInternal(r);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load rarities.json: {ex.Message}");
            }
        }

        public static void RegisterRarity(RarityData rarityData)
        {
            if (rarityData == null || string.IsNullOrWhiteSpace(rarityData.Id)) return;
            lock (_lock)
            {
                RegisterRarityInternal(rarityData);
            }
        }

        private static void RegisterRarityInternal(RarityData rarityData)
        {
            if (rarityData == null || string.IsNullOrWhiteSpace(rarityData.Id)) return;

            if (raritiesById.TryGetValue(rarityData.Id, out var existingRarity))
            {
                if (string.Equals(rarityData.Name, rarityData.Id, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(existingRarity.Name))
                {
                    rarityData.Name = existingRarity.Name;
                }
                if (rarityData.ExperienceMultiplier == 1.0f && existingRarity.ExperienceMultiplier != 1.0f)
                {
                    rarityData.ExperienceMultiplier = existingRarity.ExperienceMultiplier;
                }
                if (rarityData.PriceMultiplier == 1.0f && existingRarity.PriceMultiplier != 1.0f)
                {
                    rarityData.PriceMultiplier = existingRarity.PriceMultiplier;
                }
            }

            raritiesById[rarityData.Id] = rarityData;
            int existingIndex = allRarities.FindIndex(r => string.Equals(r.Id, rarityData.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                allRarities[existingIndex] = rarityData;
            }
            else
            {
                allRarities.Add(rarityData);
            }
        }

        public static RarityData GetRarity(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_lock)
            {
                raritiesById.TryGetValue(id, out var data);
                return data;
            }
        }

        public static RarityData GetRarity(Rarity rarityEnum)
        {
            return GetRarity(rarityEnum.ToString());
        }

        public static float GetExperienceMultiplier(string rarityId)
        {
            var r = GetRarity(rarityId);
            return r != null ? r.ExperienceMultiplier : 1.0f;
        }

        public static float GetExperienceMultiplier(Rarity rarityEnum)
        {
            return GetExperienceMultiplier(rarityEnum.ToString());
        }

        public static float GetPriceMultiplier(string rarityId)
        {
            var r = GetRarity(rarityId);
            return r != null ? r.PriceMultiplier : 1.0f;
        }

        public static List<RarityData> GetAllRarities()
        {
            lock (_lock)
            {
                return new List<RarityData>(allRarities);
            }
        }

        public static int CalculateExperience(WankulCardData wankulCardData, int shopLevel = 1)
        {
            float experienceFloat = 1.0f;

            if (wankulCardData is TerrainCardData)
            {
                experienceFloat = 1.5f;
            }
            else if (wankulCardData is EffigyCardData effigyCardData)
            {
                experienceFloat = GetExperienceMultiplier(effigyCardData.RarityId);
            }
            else if (wankulCardData is SpecialCardData)
            {
                experienceFloat = 100.0f;
            }

            float shopXpFactor = 1f + shopLevel + (shopLevel * shopLevel * 0.001f);
            if (shopXpFactor < 1) shopXpFactor = 1;

            return (int)Math.Ceiling(experienceFloat * shopXpFactor);
        }
    }
}
