using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
            void Map(string name, int cell)
            {
                var val = (int)EnumExtensions.SafeParseEItemType(name);
                if (val >= 0)
                {
                    s_CustomItemCells[val] = cell;
                }
            }

            Map("DisplayStellar", 224);
            Map("DisplayStellarTaux", 225);
            Map("BoosterStellar", 226);
            Map("BoosterStellarTaux", 227);
            Map("DisplayLegacy", 228);
            Map("DisplayLegacyTaux", 229);
            Map("BoosterLegacy", 230);
            Map("BoosterLegacyTaux", 231);
            Map("CaleconStellar", 232);
            Map("StarterApocalypse", 233);
            Map("StarterShowtime", 234);
            Map("TapisS41", 235);
            Map("TapisS42", 236);
            Map("ClasseurS4", 237);
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
                    byte[] data = File.ReadAllBytes(p);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(data))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.name = "PriceTagAtlas_Wankul";
                        tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        UnityEngine.Object.DontDestroyOnLoad(tex);
                        s_CustomPriceTagAtlas = tex;
                        Plugin.Logger.LogInfo($"[PriceTagPatch] Texture PriceTagAtlas_Wankul chargée ({tex.width}x{tex.height}).");
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
