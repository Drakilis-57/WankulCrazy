using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WankulCrazyPlugin.cards
{
    internal static class PluginLogger
    {
        public static void LogError(string message)
        {
            try
            {
                if (Plugin.Logger != null) Plugin.Logger.LogError(message);
                else Console.WriteLine("[Error] " + message);
            }
            catch
            {
                Console.WriteLine("[Error] " + message);
            }
        }
    }

    public static class SeasonsManager
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, SeasonData> seasonsById = new Dictionary<string, SeasonData>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<SeasonData> allSeasons = new List<SeasonData>();

        private static void SafeLogError(string msg)
        {
            try { Plugin.Logger?.LogError(msg); } catch { }
        }

        static SeasonsManager()
        {
            ResetToDefaults();
        }

        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                seasonsById.Clear();
                allSeasons.Clear();

                RegisterSeasonInternal(new SeasonData("S01", "Origins"));
                RegisterSeasonInternal(new SeasonData("S02", "Campus"));
                RegisterSeasonInternal(new SeasonData("S03", "Battle"));
                RegisterSeasonInternal(new SeasonData("S04", "Stellar"));
                RegisterSeasonInternal(new SeasonData("S05", "Legacy"));
                RegisterSeasonInternal(new SeasonData("HS", "Hors Serie"));
            }
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
                    lock (_lock)
                    {
                        foreach (var s in loadedSeasons)
                        {
                            if (!string.IsNullOrWhiteSpace(s.Id))
                            {
                                RegisterSeasonInternal(s);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLogger.LogError($"Failed to load seasons.json: {ex.Message}");
            }
        }

        public static void RegisterSeason(SeasonData seasonData)
        {
            if (seasonData == null || string.IsNullOrWhiteSpace(seasonData.Id)) return;
            lock (_lock)
            {
                RegisterSeasonInternal(seasonData);
            }
        }

        private static void RegisterSeasonInternal(SeasonData seasonData)
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

            try
            {
                if (Enum.TryParse<Season>(seasonData.Id, true, out var seasonEnum))
                {
                    SeasonsContainer.Seasons[seasonEnum] = seasonData.Name;
                }
            }
            catch (Exception) { }
        }

        public static SeasonData GetSeason(string seasonId)
        {
            if (string.IsNullOrWhiteSpace(seasonId)) return null;
            lock (_lock)
            {
                return seasonsById.TryGetValue(seasonId, out var seasonData) ? seasonData : null;
            }
        }

        public static string GetSeasonName(string seasonId)
        {
            var season = GetSeason(seasonId);
            return season != null ? season.Name : seasonId;
        }

        public static List<SeasonData> GetAllSeasons()
        {
            lock (_lock)
            {
                return new List<SeasonData>(allSeasons);
            }
        }
    }
}
