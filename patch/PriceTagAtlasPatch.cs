using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Remplace l'atlas des étiquettes de rayon (PriceTagAtlas) par la version Wankul
    /// et mappe les items custom vers leurs cellules réservées (224+).
    /// </summary>
    public static class PriceTagAtlasPatch
    {
        private static Texture2D s_CustomPriceTagAtlas = null;
        private static bool s_AtlasApplied = false;
        private static readonly Dictionary<int, int> s_CustomItemCells = new Dictionary<int, int>();

        private static void InitCustomCells()
        {
            void Map(EItemType itemType, int cell)
            {
                int val = (int)itemType;
                if (val >= 0)
                {
                    s_CustomItemCells[val] = cell;
                }
            }

            Map(CustomItemTypes.DisplayStellar, 224);
            Map(CustomItemTypes.DisplayStellarTaux, 225);
            Map(CustomItemTypes.BoosterStellar, 226);
            Map(CustomItemTypes.BoosterStellarTaux, 227);
            Map(CustomItemTypes.DisplayLegacy, 228);
            Map(CustomItemTypes.DisplayLegacyTaux, 229);
            Map(CustomItemTypes.BoosterLegacy, 230);
            Map(CustomItemTypes.BoosterLegacyTaux, 231);
            Map(CustomItemTypes.CaleconStellar, 232);
            Map(CustomItemTypes.StarterApocalypse, 233);
            Map(CustomItemTypes.StarterShowtime, 234);
            Map(CustomItemTypes.TapisS41, 235);
            Map(CustomItemTypes.TapisS42, 236);
            Map(CustomItemTypes.ClasseurS4, 237);
        }

        public static bool TryGetCustomCell(int itemTypeValue, out int cell)
        {
            if (s_CustomItemCells.Count == 0)
            {
                InitCustomCells();
            }
            return s_CustomItemCells.TryGetValue(itemTypeValue, out cell);
        }

        public static bool GetItemIconIndexPrefix(int itemTypeValue, ref int __result)
        {
            if (TryGetCustomCell(itemTypeValue, out int customCell))
            {
                __result = customCell;
                return false;
            }
            return true;
        }

        private static void ConfigureAtlasTexture(Texture2D tex)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "PriceTagAtlas_Wankul";
        }

        public static void ReloadAtlas()
        {
            string p = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "PriceTagAtlas.png");
            if (!File.Exists(p))
            {
                Plugin.Logger.LogWarning($"[PriceTagPatch] ReloadAtlas: fichier introuvable: {p}");
                return;
            }

            try
            {
                Texture2D previous = s_CustomPriceTagAtlas;
                Texture2D newTex = PatchTexturesImporter.ReloadDiskTexture(p, ConfigureAtlasTexture);
                if (newTex == null)
                {
                    Plugin.Logger.LogError("[PriceTagPatch] ReloadAtlas: échec de LoadImage pour PriceTagAtlas.png");
                    return;
                }

                if (previous != null && previous != newTex)
                {
                    UnityEngine.Object.Destroy(previous);
                }
                s_CustomPriceTagAtlas = newTex;
                s_AtlasApplied = false;
                Plugin.Logger.LogInfo($"[PriceTagPatch] PriceTagAtlas_Wankul rechargé en mémoire ({newTex.width}x{newTex.height}).");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[PriceTagPatch] Erreur lors du rechargement de l'atlas: {ex.Message}");
            }
        }

        public static void EnsureCustomAtlas(Material mat)
        {
            if (mat == null) return;

            if (s_CustomPriceTagAtlas == null)
            {
                string p = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "PriceTagAtlas.png");
                if (!File.Exists(p))
                {
                    Plugin.Logger.LogWarning($"[PriceTagPatch] Atlas introuvable: {p}");
                    return;
                }
                try
                {
                    s_CustomPriceTagAtlas = PatchTexturesImporter.GetOrLoadDiskTexture(p, ConfigureAtlasTexture);
                    if (s_CustomPriceTagAtlas != null)
                    {
                        Plugin.Logger.LogInfo($"[PriceTagPatch] Texture PriceTagAtlas_Wankul chargée ({s_CustomPriceTagAtlas.width}x{s_CustomPriceTagAtlas.height}).");
                    }
                    else
                    {
                        Plugin.Logger.LogError("[PriceTagPatch] Échec de LoadImage pour PriceTagAtlas.png");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[PriceTagPatch] Exception au chargement de PriceTagAtlas: {ex.Message}");
                    return;
                }
            }

            if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != s_CustomPriceTagAtlas)
            {
                mat.SetTexture("_MainTex", s_CustomPriceTagAtlas);
                if (!s_AtlasApplied)
                {
                    s_AtlasApplied = true;
                    Plugin.Logger.LogInfo("[PriceTagPatch] Mat_PriceTagInstanced._MainTex remplacé par l'atlas Wankul avec succès !");
                }
            }
        }

        public static void SetItemImagePostfix(UI_PriceTag __instance)
        {
            if (__instance == null || __instance.m_InstancedRenderer == null) return;
            EnsureCustomAtlas(__instance.m_InstancedRenderer.sharedMaterial);
        }
    }
}
