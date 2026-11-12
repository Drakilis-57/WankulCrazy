using System;
using System.Collections.Generic;
using System.IO;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.importer
{
    /// <summary>
    /// Indexeur et résolveur de fichiers haute performance en mémoire.
    /// Élimine les ~10 000 syscalls File.Exists lors du chargement des textures et masques.
    /// Insensible à la casse (OrdinalIgnoreCase) et normalise les séparateurs '/' et '\'.
    /// </summary>
    public static class WankulFileIndex
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, string> _fileIndex;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void SafeLogWarning(string msg)
        {
            try { Plugin.Logger?.LogWarning(msg); } catch { }
        }

        /// <summary>
        /// Initialise l'index des fichiers de data/ en mémoire une seule fois de manière thread-safe.
        /// </summary>
        public static void EnsureInitialized(string pluginPath)
        {
            if (_fileIndex != null) return;
            lock (_lock)
            {
                if (_fileIndex != null) return;

                var index = new Dictionary<string, string>(2048, StringComparer.OrdinalIgnoreCase);
                try
                {
                    string dataDir = Path.Combine(pluginPath, "data");
                    if (Directory.Exists(dataDir))
                    {
                        string rootedDataDir = Path.GetFullPath(dataDir).Replace('/', '\\').TrimEnd('\\');
                        foreach (string file in Directory.EnumerateFiles(rootedDataDir, "*.*", SearchOption.AllDirectories))
                        {
                            string normFull = NormalizePathKey(file);
                            index[normFull] = file;

                            // Indexation également en chemin relatif pour supporter les lookups relatifs
                            if (file.Length > rootedDataDir.Length && file.StartsWith(rootedDataDir, StringComparison.OrdinalIgnoreCase))
                            {
                                string rel = file.Substring(rootedDataDir.Length).TrimStart('\\', '/');
                                string relKey = NormalizePathKey(rel);
                                index[relKey] = file;

                                string withData = NormalizePathKey("data\\" + rel);
                                index[withData] = file;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeLogWarning($"[WankulFileIndex] Indexation partielle: {ex.Message}");
                }
                _fileIndex = index;
            }
        }

        /// <summary>
        /// Invalide le cache d'index en mémoire (utile en tests ou lors d'ajouts dynamiques).
        /// </summary>
        public static void Invalidate()
        {
            lock (_lock)
            {
                _fileIndex = null;
            }
        }

        public static string NormalizePathKey(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            try
            {
                if (Path.IsPathRooted(path))
                {
                    return Path.GetFullPath(path).Replace('/', '\\').TrimEnd('\\');
                }
                return path.Replace('/', '\\').TrimEnd('\\');
            }
            catch
            {
                return path.Replace('/', '\\').TrimEnd('\\');
            }
        }

        /// <summary>
        /// Résout le chemin physique d'un fichier en mémoire (O(1)) sans aucun syscall File.Exists.
        /// Gère la casse, les séparateurs et les extensions alternatives (.png, .jpg, .jpeg).
        /// </summary>
        public static string ResolveExistingPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (_fileIndex != null)
            {
                string norm = NormalizePathKey(path);
                if (_fileIndex.TryGetValue(norm, out string exact))
                    return exact;

                string ext = Path.GetExtension(path);
                if (!string.IsNullOrEmpty(ext))
                {
                    if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        string altJpg = Path.ChangeExtension(path, ".jpg");
                        if (_fileIndex.TryGetValue(NormalizePathKey(altJpg), out string jpg))
                            return jpg;

                        string altJpeg = Path.ChangeExtension(path, ".jpeg");
                        if (_fileIndex.TryGetValue(NormalizePathKey(altJpeg), out string jpeg))
                            return jpeg;
                    }
                    else if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        string altPng = Path.ChangeExtension(path, ".png");
                        if (_fileIndex.TryGetValue(NormalizePathKey(altPng), out string png))
                            return png;
                    }
                }

                return null;
            }

            // Fallback classique sans index initialisé
            if (File.Exists(path)) return path;

            string fallbackExt = Path.GetExtension(path);
            if (string.IsNullOrEmpty(fallbackExt)) return null;

            if (fallbackExt.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                string altJpg = Path.ChangeExtension(path, ".jpg");
                if (File.Exists(altJpg)) return altJpg;

                string altJpeg = Path.ChangeExtension(path, ".jpeg");
                if (File.Exists(altJpeg)) return altJpeg;
            }
            else if (fallbackExt.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || fallbackExt.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                string altPng = Path.ChangeExtension(path, ".png");
                if (File.Exists(altPng)) return altPng;
            }

            return null;
        }

        /// <summary>
        /// Résout le chemin du masque foil pour une carte selon les règles de nommage et saisons Wankul.
        /// </summary>
        public static string ResolveMaskPath(WankulCardData card, string pluginPath)
        {
            if (card == null || string.IsNullOrEmpty(card.TexturePath)) return null;

            // 1. Chemin direct relatif (data/masks/<TexturePath>)
            string directPath = Path.Combine(pluginPath, "data/masks", card.TexturePath);
            string resolvedDirect = ResolveExistingPath(directPath);
            if (resolvedDirect != null) return resolvedDirect;

            // 2. Résolution dynamique selon la saison dans data/masks/textures/{prefix}_{card.Number}.png
            if (!string.IsNullOrEmpty(card.Number) &&
                int.TryParse(card.Number, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int num) &&
                num > 0)
            {
                string season = !string.IsNullOrEmpty(card.SeasonId) ? card.SeasonId : "";
                string texturePath = card.TexturePath.Replace('\\', '/');

                int? prefix = null;
                if (season == "S01" || texturePath.Contains("Origins/"))
                {
                    prefix = 150 + (num - 151); // 151 -> 150_151.png, 180 -> 179_180.png
                }
                else if (season == "S02" || texturePath.Contains("Campus/"))
                {
                    prefix = 305 + (num - 126); // 126 -> 305_126.png, 155 -> 334_155.png
                }
                else if (season == "S03" || texturePath.Contains("Battle/"))
                {
                    prefix = 516 + (num - 151); // 151 -> 516_151.png, 180 -> 545_180.png
                }
                else if (season == "S04" || texturePath.Contains("Stellar/"))
                {
                    prefix = 696 + (num - 151); // 151 -> 696_151.png, 180 -> 725_180.png
                }

                if (prefix.HasValue && prefix.Value >= 0)
                {
                    string seasonMaskPath = Path.Combine(pluginPath, "data/masks/textures", $"{prefix.Value}_{card.Number}.png");
                    string resolvedSeason = ResolveExistingPath(seasonMaskPath);
                    if (resolvedSeason != null) return resolvedSeason;
                }

                // Fallback générique : {num-1}_{num}.png
                string fallbackIdPath = Path.Combine(pluginPath, "data/masks/textures", $"{num - 1}_{card.Number}.png");
                string resolvedFallback = ResolveExistingPath(fallbackIdPath);
                if (resolvedFallback != null) return resolvedFallback;
            }

            return null;
        }

        /// <summary>
        /// Détermine si une carte est éligible au rendu foil (masque de brillance).
        /// </summary>
        public static bool IsCardFoil(WankulCardData card)
        {
            if (card is EffigyCardData effigyCard)
            {
                var rarityData = RaritiesManager.GetRarity(effigyCard.RarityId);
                if ((rarityData != null && rarityData.IsEligibleForFoil) || effigyCard.Rarity >= Rarity.UR1)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
