using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    public static class SeasonsManager
    {
        private static readonly Dictionary<string, SeasonData> seasonsById = new Dictionary<string, SeasonData>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<SeasonData> allSeasons = new List<SeasonData>();

        static SeasonsManager()
        {
            ResetToDefaults();
        }

        public static void ResetToDefaults()
        {
            seasonsById.Clear();
            allSeasons.Clear();

            RegisterSeason(new SeasonData("S01", "Origins"));
            RegisterSeason(new SeasonData("S02", "Campus"));
            RegisterSeason(new SeasonData("S03", "Battle"));
            RegisterSeason(new SeasonData("S04", "Stellar"));
            RegisterSeason(new SeasonData("HS", "Hors Serie"));
        }

        public static void LoadFromPluginPath(string pluginPath)
        {
            ResetToDefaults();

            string seasonsPath = Path.Combine(pluginPath, "data/seasons.json");
            if (!File.Exists(seasonsPath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(seasonsPath);
                List<SeasonData> loadedSeasons = JsonConvert.DeserializeObject<List<SeasonData>>(json);
                if (loadedSeasons != null && loadedSeasons.Count > 0)
                {
                    foreach (var s in loadedSeasons)
                    {
                        if (!string.IsNullOrWhiteSpace(s.Id))
                        {
                            RegisterSeason(s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load seasons.json: {ex.Message}");
            }
        }

        public static void RegisterSeason(SeasonData seasonData)
        {
            if (seasonData == null || string.IsNullOrWhiteSpace(seasonData.Id)) return;

            if (seasonsById.TryGetValue(seasonData.Id, out var existingSeason))
            {
                if (string.Equals(seasonData.Name, seasonData.Id, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(existingSeason.Name))
                {
                    seasonData.Name = existingSeason.Name;
                }
            }

            seasonsById[seasonData.Id] = seasonData;
            int existingIndex = allSeasons.FindIndex(s => string.Equals(s.Id, seasonData.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                allSeasons[existingIndex] = seasonData;
            }
            else
            {
                allSeasons.Add(seasonData);
            }

            if (Enum.TryParse<Season>(seasonData.Id, true, out var seasonEnum))
            {
                SeasonsContainer.Seasons[seasonEnum] = seasonData.Name;
            }
        }

        public static SeasonData GetSeason(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            seasonsById.TryGetValue(id, out var data);
            return data;
        }

        public static string GetSeasonName(string id)
        {
            var season = GetSeason(id);
            return season != null ? season.Name : id;
        }

        public static string GetSeasonName(Season season)
        {
            return GetSeasonName(season.ToString());
        }

        public static List<SeasonData> GetAllSeasons()
        {
            return new List<SeasonData>(allSeasons);
        }
    }
}
