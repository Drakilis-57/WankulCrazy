using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using HarmonyLib;

namespace WankulCrazyPlugin.importer
{
    internal class PatchTexturesImporter
    {
        private static readonly List<Texture2D> s_PersistentTextures = new List<Texture2D>();
        public static void ReplaceGameTextures(string textureLevel)
        {
            string texturesPath = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", textureLevel);
            if (!Directory.Exists(texturesPath))
            {
                return;
            }

            s_PersistentTextures.RemoveAll(t => t == null);
            List<string> filenames = GetFileNames(texturesPath);

            foreach (string filename in filenames)
            {
                try
                {
                    string textureName = Path.GetFileNameWithoutExtension(filename);
                    List<Texture2D> targetTextures = new List<Texture2D>();

                    if (textureName.Equals("Billboard", StringComparison.OrdinalIgnoreCase))
                    {
                        var matched = Resources.FindObjectsOfTypeAll<Texture2D>()
                            .Where(t => t != null && (t.name == "Billboard" || t.name == "Billboard_albedo" || t.name == "Billboard_second"))
                            .ToList();
                        targetTextures.AddRange(matched);
                    }
                    else if (textureName.Equals("T_3dCardModel", StringComparison.OrdinalIgnoreCase))
                    {
                        var matched = Resources.FindObjectsOfTypeAll<Texture2D>()
                            .Where(t => t != null && (t.name == "T_3dCardModel" || t.name == "CardFrontIconOnly"))
                            .ToList();
                        targetTextures.AddRange(matched);
                    }
                    else if (textureName.Equals("CardBack", StringComparison.OrdinalIgnoreCase))
                    {
                        var matched = Resources.FindObjectsOfTypeAll<Texture2D>()
                            .Where(t => t != null && (t.name == "CardBack" || t.name == "T_CardBackMesh"))
                            .ToList();
                        targetTextures.AddRange(matched);
                    }
                    else
                    {
                        Texture targetTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == textureName);
                        if (targetTexture is Texture2D targetTexture2D)
                        {
                            targetTextures.Add(targetTexture2D);
                        }
                    }

                    if (targetTextures.Count > 0)
                    {
                        string texturePath = Path.Combine(texturesPath, filename);
                        Texture2D newTexture = LoadTexture2D(texturePath);
                        if (newTexture == null)
                        {
                            continue;
                        }

                        bool keepAlive = false;
                        try
                        {
                            foreach (Texture2D targetTexture2D in targetTextures)
                            {
                                Texture2D result = ReplaceTexture(targetTexture2D, newTexture);
                                if (result == newTexture)
                                {
                                    keepAlive = true;
                                    RegisterPersistentTexture(newTexture);
                                }
                            }
                        }
                        finally
                        {
                            if (!keepAlive && newTexture != null)
                            {
                                UnityEngine.Object.Destroy(newTexture);
                            }
                        }
                    }
                    else
                    {
                        // Les textures custom pour les displays, boosters, statues, etc. ne sont pas dans les ressources du jeu de base
                        // Elles sont gérées directement par nos propres hooks (OBJImporter, GetBoxTexturePaths, etc.).
                        if (!IsKnownOrOptionalTexture(textureName))
                        {
                            Plugin.Logger.LogWarning($"[PatchTexturesImporter] Texture '{textureName}' not found");
                        }
                        else
                        {
                            Plugin.Logger.LogDebug($"[PatchTexturesImporter] Texture connue/optionnelle non présente dans le bundle vanilla : {textureName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[PatchTexturesImporter] Erreur lors du remplacement de la texture '{filename}': {ex.Message}");
                }
            }
        }

        private static bool IsKnownOrOptionalTexture(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
            {
                return false;
            }

            return textureName.StartsWith("Texture_Display_") ||
                   textureName.StartsWith("Texture_Booster_") ||
                   textureName.StartsWith("Texture_Calecon") ||
                   textureName.StartsWith("MonsterStatue_") ||
                   textureName.StartsWith("Texture_Starter_") ||
                   textureName.StartsWith("Texture_Tapis_") ||
                   textureName.StartsWith("T_CardSleeve") ||
                   textureName.StartsWith("T_BasicCardBox") ||
                   textureName.StartsWith("T_EpicCardBox") ||
                   textureName.StartsWith("T_RareCardBox") ||
                   textureName.StartsWith("T_3dCardModel") ||
                   textureName.StartsWith("T_D20Dice") ||
                   textureName.StartsWith("T_BinderBook") ||
                   textureName == "CardBack" ||
                   textureName == "Billboard" ||
                   textureName == "Billboard_albedo" ||
                   textureName == "Billboard_second" ||
                   textureName == "mcp_building_32_billboards_d";
        }

        private static void RegisterPersistentTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (!s_PersistentTextures.Contains(texture))
            {
                s_PersistentTextures.Add(texture);
            }

            try
            {
                texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }
            catch { }

            try
            {
                UnityEngine.Object.DontDestroyOnLoad(texture);
            }
            catch { }
        }

        private static bool MatchesOriginal(Texture candidate, Texture original)
        {
            if (candidate == null || original == null)
            {
                return false;
            }
            if (candidate == original)
            {
                return true;
            }
            if (!string.IsNullOrEmpty(original.name) && candidate.name == original.name)
            {
                return true;
            }
            return false;
        }

        private static int ApplyDirectMaterialReplacement(Texture2D original, Texture2D replacement)
        {
            if (original == null || replacement == null)
            {
                return 0;
            }

            replacement.filterMode = FilterMode.Trilinear;
            replacement.anisoLevel = 16;
            replacement.mipMapBias = -0.5f;

            int affectedCount = 0;
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material mat in materials)
            {
                if (mat == null)
                {
                    continue;
                }

                bool referencesOriginal = false;
                if (mat.HasProperty("_MainTex"))
                {
                    try
                    {
                        Texture main = mat.mainTexture;
                        if (main != null && main != replacement && MatchesOriginal(main, original))
                        {
                            referencesOriginal = true;
                        }
                    }
                    catch { }
                }

                if (!referencesOriginal && mat.HasProperty("_BaseMap"))
                {
                    try
                    {
                        Texture tex = mat.GetTexture("_BaseMap");
                        if (tex != null && tex != replacement && MatchesOriginal(tex, original))
                        {
                            referencesOriginal = true;
                        }
                    }
                    catch { }
                }

                if (!referencesOriginal && mat.HasProperty("_BaseColorMap"))
                {
                    try
                    {
                        Texture tex = mat.GetTexture("_BaseColorMap");
                        if (tex != null && tex != replacement && MatchesOriginal(tex, original))
                        {
                            referencesOriginal = true;
                        }
                    }
                    catch { }
                }

                if (referencesOriginal)
                {
                    if (mat.HasProperty("_MainTex"))
                    {
                        try { mat.mainTexture = replacement; } catch { }
                        try { mat.SetTexture("_MainTex", replacement); } catch { }
                    }

                    if (mat.HasProperty("_BaseMap"))
                    {
                        try { mat.SetTexture("_BaseMap", replacement); } catch { }
                    }

                    if (mat.HasProperty("_BaseColorMap"))
                    {
                        try { mat.SetTexture("_BaseColorMap", replacement); } catch { }
                    }

                    affectedCount++;
                }
            }

            return affectedCount;
        }

        public static Texture2D ReplaceTexture(Texture2D original, Texture2D replacement)
        {
            if (original == null)
            {
                return replacement;
            }
            if (replacement == null)
            {
                return original;
            }

            // Cas spécial : original est vide → on en recrée une
            if (original.width == 0 && original.height == 0)
            {
                //Plugin.Logger.LogWarning("Texture originale vide (0x0). Création d'une nouvelle texture.");

                Texture2D newTexture = new Texture2D(replacement.width, replacement.height, replacement.format, replacement.mipmapCount > 1);
                newTexture.name = replacement.name + "_Generated";

                try
                {
                    Graphics.CopyTexture(replacement, newTexture);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Erreur de copie (cas spécial) : {ex.Message}");
                }

                return newTexture;
            }

            // Cas 1 : dimensions différentes (ex: Unity 6 Mipmap / Texture Streaming 32x32 vs 1024x1024)
            if (original.width != replacement.width || original.height != replacement.height)
            {
                if (string.IsNullOrEmpty(replacement.name))
                {
                    replacement.name = original.name + "_Replaced";
                }

                int replacedCount = ApplyDirectMaterialReplacement(original, replacement);
                Plugin.Logger.LogInfo($"[PatchTexturesImporter] Remplacement par référence directe pour '{original.name}' ({original.width}x{original.height} -> {replacement.width}x{replacement.height}) sur {replacedCount} matériau(x).");
                RegisterPersistentTexture(replacement);
                return replacement;
            }

            // Cas 2 : dimensions identiques -> Graphics.CopyTexture
            Texture2D intermediate = null;
            Texture2D intermediateMip = null;
            Texture2D textureToCopy = replacement;

            if (original.mipmapCount != replacement.mipmapCount)
            {
                intermediateMip = AdjustMipMapLevels(replacement, original.mipmapCount);
                textureToCopy = intermediateMip;
            }

            if (original.format != replacement.format)
            {
                intermediate = ConvertTextureFormat(textureToCopy, original.format, original.mipmapCount);
                textureToCopy = intermediate;
            }

            bool copySuccess = false;
            try
            {
                Graphics.CopyTexture(textureToCopy, original);

                // Amélioration de la netteté pour les textures de décor/affiches vues à distance et sous des angles rasants
                original.filterMode = FilterMode.Trilinear;
                original.anisoLevel = 16;
                original.mipMapBias = -0.5f;
                copySuccess = true;
                Plugin.Logger.LogInfo($"[PatchTexturesImporter] Graphics.CopyTexture réussi pour '{original.name}' ({original.width}x{original.height}).");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[PatchTexturesImporter] Graphics.CopyTexture a échoué pour '{original.name}' : {ex.Message}. Bascule vers le remplacement par référence directe.");
            }
            finally
            {
                if (intermediateMip != null)
                {
                    UnityEngine.Object.Destroy(intermediateMip);
                }
                if (intermediate != null)
                {
                    UnityEngine.Object.Destroy(intermediate);
                }
            }

            if (copySuccess)
            {
                return original;
            }

            // Fallback si Graphics.CopyTexture a échoué : remplacement direct
            if (string.IsNullOrEmpty(replacement.name))
            {
                replacement.name = original.name + "_Replaced";
            }
            int fallbackCount = ApplyDirectMaterialReplacement(original, replacement);
            Plugin.Logger.LogInfo($"[PatchTexturesImporter] Fallback référence directe appliqué pour '{original.name}' sur {fallbackCount} matériau(x).");
            RegisterPersistentTexture(replacement);
            return replacement;
        }

        private static Texture2D ConvertTextureFormat(Texture2D texture, TextureFormat format, int mipCount)
        {
            if (format == TextureFormat.DXT1)
            {
                Texture2D noAlphaTex = ConvertTextureToNoAlpha(texture);
                Texture2D convertedTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, mipCount > 1);
                try
                {
                    convertedTexture.SetPixels(noAlphaTex.GetPixels());
                    convertedTexture.Apply(true);
                    convertedTexture.Compress(false);
                }
                finally
                {
                    UnityEngine.Object.Destroy(noAlphaTex);
                }
                return convertedTexture;
            }
            else if (format == TextureFormat.DXT5)
            {
                Texture2D convertedTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipCount > 1);
                convertedTexture.SetPixels(texture.GetPixels());
                convertedTexture.Apply(true);
                convertedTexture.Compress(false);
                return convertedTexture;
            }
            else
            {
                // For formats where SetPixels might fail (e.g. compressed or platform-specific formats),
                // use RenderTexture / Blit or safe fallback
                Texture2D convertedTexture = new Texture2D(texture.width, texture.height, format, mipCount > 1);
                try
                {
                    convertedTexture.SetPixels(texture.GetPixels());
                    convertedTexture.Apply(true);
                }
                catch
                {
                    // Fallback using RenderTexture
                    RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(texture, rt);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    convertedTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    convertedTexture.Apply(true);
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                }
                return convertedTexture;
            }
        }

        private static Texture2D ConvertTextureToNoAlpha(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;

            Texture2D textureNoAlpha = new Texture2D(width, height, TextureFormat.RGB24, texture.mipmapCount > 1);
            Color[] pixels = texture.GetPixels();
            Color[] pixelsNoAlpha = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixelsNoAlpha[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b);
            }

            textureNoAlpha.SetPixels(pixelsNoAlpha);
            textureNoAlpha.Apply(true);

            return textureNoAlpha;
        }

        private static Texture2D AdjustMipMapLevels(Texture2D texture, int mipCount)
        {
            Texture2D adjustedTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipCount > 1);
            adjustedTexture.SetPixels(texture.GetPixels());
            adjustedTexture.Apply(true);
            return adjustedTexture;
        }

        public static Texture2D LoadTexture2D(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            texture.LoadImage(bytes);
            return texture;
        }

        public static List<string> GetFileNames(string directoryPath)
        {
            List<string> fileNames = new List<string>();
            string[] files = Directory.GetFiles(directoryPath);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                fileNames.Add(fileName);
            }

            return fileNames;
        }

        private static readonly Dictionary<EItemType, Material> cachedBoxMaterials = new Dictionary<EItemType, Material>();
        private static Dictionary<EItemType, string> boxTexturePaths = null;

        public static Dictionary<EItemType, string> GetBoxTexturePaths()
        {
            if (boxTexturePaths == null)
            {
                boxTexturePaths = new Dictionary<EItemType, string>
                {
                    { EItemType.BasicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S1.png") },
                    { EItemType.RareCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S2.png") },
                    { EItemType.EpicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S3.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayStellar"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S4.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayStellarTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S4_TauxDrop.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayLegacy"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S5.png") },
                    { EnumExtensions.SafeParseEItemType("DisplayLegacyTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S5_TauxDrop.png") },
                    { EItemType.LegendaryCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_HS.png") },
                    { EItemType.DestinyBasicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S1_TauxDrop.png") },
                    { EItemType.DestinyRareCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S2_TauxDrop.png") },
                    { EItemType.DestinyEpicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S3_TauxDrop.png") },
                    { EItemType.DestinyLegendaryCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_HS_TauxDrop.png") },

                    // Boosters vanilla (en 1.07 ils utilisent Texture2DArray et ne passent plus par T_CardPack*)
                    { EItemType.BasicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackCommon.png") },
                    { EItemType.RareCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackRare.png") },
                    { EItemType.EpicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackEpic.png") },
                    { EItemType.LegendaryCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackLegnd.png") },
                    { EItemType.DestinyBasicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyCommon.png") },
                    { EItemType.DestinyRareCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyRare.png") },
                    { EItemType.DestinyEpicCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyEpic.png") },
                    { EItemType.DestinyLegendaryCardPack, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardPackDestinyLegend.png") },

                    // Accessoires, mangas, tapis, classeurs, deckboxes, sleeves & figurines vanilla
                    { EItemType.Deodorant, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Cleanser_BaseColor.png") },
                    { EItemType.DeckBox1, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_DeckBox.png") },
                    { EItemType.DeckBox2, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_DeckBox2.png") },
                    { EItemType.DeckBox3, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_DeckBox3.png") },
                    { EItemType.DeckBox4, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_DeckBox4.png") },
                    { EItemType.BinderBook, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BinderBook.png") },
                    { EItemType.BinderBookPremium, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BinderBookAllStar.png") },
                    { EItemType.Toy_PiggyA, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PiggyA.png") },
                    { EItemType.Toy_PiggyB, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PiggyB.png") },
                    { EItemType.Toy_PiggyC, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PiggyC.png") },
                    { EItemType.Toy_PiggyD, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PiggyD.png") },
                    { EItemType.Toy_GolemA, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_GolemA.png") },
                    { EItemType.Toy_GolemB, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_GolemB.png") },
                    { EItemType.Toy_GolemC, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_GolemC.png") },
                    { EItemType.Toy_GolemD, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_GolemD.png") },
                    { EItemType.Toy_StarfishA, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_StarfishA.png") },
                    { EItemType.Toy_StarfishB, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_StarfishB.png") },
                    { EItemType.Toy_StarfishC, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_StarfishC.png") },
                    { EItemType.Toy_StarfishD, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_StarfishD.png") },
                    { EItemType.Toy_BatA, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BatA.png") },
                    { EItemType.Toy_BatB, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BatB.png") },
                    { EItemType.Toy_BatC, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BatC.png") },
                    { EItemType.Toy_BatD, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BatD.png") },
                    { EItemType.Toy_Beetle, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Beetle.png") },
                    { EItemType.Toy_FoxB, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_FoxB.png") },
                    { EItemType.Toy_ToonZ, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_ToonZPlushie.png") },
                    { EItemType.BulkBox_TetramonBase, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BulkBoxTetramonBase.png") },
                    { EItemType.BulkBox_TetramonDestiny, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BulkBoxDestiny.png") },
                    { EItemType.PreconDeck_Fire, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckFire.png") },
                    { EItemType.PreconDeck_Earth, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckEarth.png") },
                    { EItemType.PreconDeck_Water, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckWater.png") },
                    { EItemType.PreconDeck_Wind, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckWind.png") },
                    { EItemType.PreconDeck_FireDestiny, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckFireDestiny.png") },
                    { EItemType.PreconDeck_EarthDestiny, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckEarthDestiny.png") },
                    { EItemType.PreconDeck_WaterDestiny, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckWaterDestiny.png") },
                    { EItemType.PreconDeck_WindDestiny, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PreconDeckWindDestiny.png") },
                    { EItemType.CardSleeve_Clear, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardSleeveClear.png") },
                    { EItemType.CardSleeve_Tetramon, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardSleeveTetramon.png") },
                    { EItemType.CardSleeve_Fire, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardSleeveFire.png") },
                    { EItemType.CardSleeve_Earth, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardSleeveEarth.png") },
                    { EItemType.CardSleeve_Water, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardSleeveWater.png") },
                    { EItemType.CardSleeve_Wind, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_CardSleeveWind.png") },
                    { EItemType.Playmat1, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat1.png") },
                    { EItemType.Playmat2, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat2.png") },
                    { EItemType.Playmat2b, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat2b.png") },
                    { EItemType.Playmat3, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat3.png") },
                    { EItemType.Playmat4, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat4.png") },
                    { EItemType.Playmat5, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat5.png") },
                    { EItemType.Playmat6, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat6.png") },
                    { EItemType.Playmat7, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat7.png") },
                    { EItemType.Playmat8, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat8.png") },
                    { EItemType.Playmat9, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat9.png") },
                    { EItemType.Playmat10, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat10.png") },
                    { EItemType.Playmat11, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat11.png") },
                    { EItemType.Playmat12, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat12.png") },
                    { EItemType.Playmat13, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat13.png") },
                    { EItemType.Playmat14, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat14.png") },
                    { EItemType.PlayMat15, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat15.png") },
                    { EItemType.PlayMat16, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat16.png") },
                    { EItemType.PlayMat17, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat17.png") },
                    { EItemType.PlayMat18, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_PlayMat18.png") },
                    { EItemType.Manga1, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_1.png") },
                    { EItemType.Manga2, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_2.png") },
                    { EItemType.Manga3, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_3.png") },
                    { EItemType.Manga4, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_4.png") },
                    { EItemType.Manga5, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_5.png") },
                    { EItemType.Manga6, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_6.png") },
                    { EItemType.Manga7, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_7.png") },
                    { EItemType.Manga8, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_8.png") },
                    { EItemType.Manga9, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_9.png") },
                    { EItemType.Manga10, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_10.png") },
                    { EItemType.Manga11, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_11.png") },
                    { EItemType.Manga12, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_Manga_12.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterGoldBattle"), Path.Combine(Plugin.GetPluginPath(), "data", "customitems", "textures", "Texture_Booster_Gold.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterGoldStellar"), Path.Combine(Plugin.GetPluginPath(), "data", "customitems", "textures", "Texture_Booster_Gold.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterGoldLegacy"), Path.Combine(Plugin.GetPluginPath(), "data", "customitems", "textures", "Texture_Booster_Gold.png") },
                    { EnumExtensions.SafeParseEItemType("StarterShowtime"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Starter_Showtime.png") },
                    { EnumExtensions.SafeParseEItemType("StarterApocalypse"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Starter_Apocalypse.png") },

                    // Boosters custom
                    { EnumExtensions.SafeParseEItemType("BoosterStellar"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S4.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterStellarTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S4_TauxDrop.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterLegacy"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S5.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterLegacyTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S5_TauxDrop.png") },

                    // Accessoires & goodies custom
                    { EnumExtensions.SafeParseEItemType("TapisS41"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Tapis_S4_1.png") },
                    { EnumExtensions.SafeParseEItemType("TapisS42"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Tapis_S4_2.png") },
                    { EnumExtensions.SafeParseEItemType("ClasseurS4"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "T_BinderBook.png") },
                    { EnumExtensions.SafeParseEItemType("CaleconStellar"), File.Exists(Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Calecon.png"))
                        ? Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Calecon.png")
                        : Path.Combine(Plugin.GetPluginPath(), "data", "customitems", "textures", "Texture_Calecon.png") },
                };
            }
            return boxTexturePaths;
        }



        public static void ApplyCustomItemMeshMaterials()
        {
            if (CSingleton<InventoryBase>.Instance?.m_StockItemData_SO?.m_ItemMeshDataList == null)
            {
                return;
            }

            var meshDataList = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ItemMeshDataList;
            var texturePaths = GetBoxTexturePaths();
            int updatedCount = 0;

            foreach (var kvp in texturePaths)
            {
                int index = (int)kvp.Key;
                if (index < 0 || index >= meshDataList.Count)
                {
                    continue;
                }

                ItemMeshData meshData = meshDataList[index];
                if (meshData == null)
                {
                    continue;
                }

                Material mat = GetOrCreateBoxMaterial(kvp.Key, kvp.Value, null);
                if (mat == null)
                {
                    continue;
                }

                meshData.material = mat;

                EItemType boosterType = GetBoosterTypeForDisplay(kvp.Key);
                if (boosterType != EItemType.None && texturePaths.TryGetValue(boosterType, out string boosterPath))
                {
                    Material boosterMat = GetOrCreateBoxMaterial(boosterType, boosterPath, null);
                    if (boosterMat != null)
                    {
                        meshData.materialSecondary = boosterMat;
                    }
                }
                else if (IsBoosterPackType(kvp.Key))
                {
                    meshData.materialSecondary = mat;
                }

                meshData.arrayMaterialType = ItemMeshData.ItemShareTexture2DArrayMaterial.None;
                updatedCount++;
            }

            if (CustomItemsImporter.ItemMeshDataList != null)
            {
                foreach (var customMesh in CustomItemsImporter.ItemMeshDataList)
                {
                    if (customMesh == null) continue;
                    EItemType cType = EnumExtensions.SafeParseEItemType(customMesh.name);
                    if (cType == EItemType.None) continue;
                    if (texturePaths.TryGetValue(cType, out string cPath))
                    {
                        Material mat = GetOrCreateBoxMaterial(cType, cPath, null);
                        if (mat != null) customMesh.material = mat;
                        EItemType bType = GetBoosterTypeForDisplay(cType);
                        if (bType != EItemType.None && texturePaths.TryGetValue(bType, out string bPath))
                        {
                            Material bMat = GetOrCreateBoxMaterial(bType, bPath, null);
                            if (bMat != null) customMesh.materialSecondary = bMat;
                        }
                        else if (IsBoosterPackType(cType))
                        {
                            customMesh.materialSecondary = mat;
                        }
                        customMesh.arrayMaterialType = ItemMeshData.ItemShareTexture2DArrayMaterial.None;
                        updatedCount++;
                    }
                }
            }

            Plugin.Logger.LogInfo($"[PatchTexturesImporter] {updatedCount} ItemMeshData convertis en matériaux 2D Wankul (arrayMaterialType = None).");
        }

        static void ItemArrayMaterialPostfix(Item __instance)
        {
            if (__instance == null) return;
            EItemType itemType = __instance.GetItemType();
            var texturePaths = GetBoxTexturePaths();
            if (texturePaths.TryGetValue(itemType, out string texturePath))
            {
                if (__instance.m_Mesh != null) __instance.m_Mesh.SetPropertyBlock(null);
                if (__instance.m_MeshSecondary != null) __instance.m_MeshSecondary.SetPropertyBlock(null);
                ApplyTextureToItem(__instance, itemType, texturePath);
            }
        }

        static void ItemPostfix(Item __instance, Mesh mesh, Material material, EItemType itemType, Mesh meshSecondary, Material materialSecondary)
        {
            var texturePaths = GetBoxTexturePaths();

            if (texturePaths.TryGetValue(itemType, out string texturePath))
            {
                ApplyTextureToItem(__instance, itemType, texturePath);
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(fileData))
            {
                tex.Apply(); tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return tex;
            }
            return null;
        }

        private static Material s_Safe2DBaseMaterial = null;

        public static Material GetSafe2DBaseMaterial()
        {
            if (s_Safe2DBaseMaterial != null) return s_Safe2DBaseMaterial;

            var allMats = Resources.FindObjectsOfTypeAll<Material>();
            s_Safe2DBaseMaterial = allMats.FirstOrDefault(m => m != null && m.name == "MAT_CardPackCommon_Lightweight")
                                ?? allMats.FirstOrDefault(m => m != null && m.name == "MAT_CardBoxInner")
                                ?? allMats.FirstOrDefault(m => m != null && m.name.StartsWith("MAT_CardPack") && !m.HasProperty("_MainTexArray"))
                                ?? allMats.FirstOrDefault(m => m != null && m.name.StartsWith("MAT_Deckbox") && !m.HasProperty("_MainTexArray"))
                                ?? allMats.FirstOrDefault(m => m != null && m.HasProperty("_MainTex") && !m.HasProperty("_MainTexArray"));

            if (s_Safe2DBaseMaterial != null)
            {
                Plugin.Logger.LogInfo($"[PatchTexturesImporter] Modèle de matériau 2D sécurisé trouvé : '{s_Safe2DBaseMaterial.name}' (shader: {s_Safe2DBaseMaterial.shader?.name}).");
            }
            return s_Safe2DBaseMaterial;
        }

        public static Material GetOrCreateBoxMaterial(EItemType itemType, string texturePath, Renderer sourceRenderer)
        {
            if (cachedBoxMaterials.TryGetValue(itemType, out Material cachedMat) && cachedMat != null)
            {
                return cachedMat;
            }

            Texture2D texture = LoadTexture(texturePath);
            if (texture == null)
            {
                Plugin.Logger.LogError($"Échec du chargement de la texture pour {itemType} : {texturePath}");
                return null;
            }

            // Si le renderer source ou meshData utilise un shader Texture2DArray (cas général en 1.07+),
            // on ne PEUT PAS le cloner pour une texture 2D. On prend le modèle 2D natif sécurisé.
            Material sourceMat = null;
            if (sourceRenderer != null && sourceRenderer.sharedMaterial != null && !WankulCrazyPlugin.utils.ShaderUtils.UsesBrokenStandardShader(sourceRenderer.sharedMaterial))
            {
                sourceMat = sourceRenderer.sharedMaterial;
            }

            if (sourceMat == null)
            {
                sourceMat = GetSafe2DBaseMaterial();
            }

            sourceMat = WankulCrazyPlugin.utils.ShaderUtils.EnsureNotBrokenStandard(sourceMat);
            Material newMat = WankulCrazyPlugin.utils.ShaderUtils.CreateSafeMaterial(sourceMat);

            if (newMat.HasProperty("_MainTex"))
                newMat.SetTexture("_MainTex", texture);
            if (newMat.HasProperty("_BaseMap"))
                newMat.SetTexture("_BaseMap", texture);
            if (newMat.HasProperty("_BaseColorMap"))
                newMat.SetTexture("_BaseColorMap", texture);



            newMat.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            cachedBoxMaterials[itemType] = newMat;
            return newMat;
        }

        private static void ApplyTextureToItem(Item item, EItemType itemType, string texturePath)
        {
            if (item.m_Mesh == null)
            {
                return;
            }

            Renderer renderer = item.m_Mesh.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }





            // On passe le renderer pour que GetOrCreateBoxMaterial puisse cloner son material
            Material targetMaterial = GetOrCreateBoxMaterial(itemType, texturePath, renderer);
            if (targetMaterial == null)
            {
                return;
            }

            EItemType boosterType = GetBoosterTypeForDisplay(itemType);
            var allBoxPaths = GetBoxTexturePaths();

            if (boosterType != EItemType.None && allBoxPaths.TryGetValue(boosterType, out string boosterPath))
            {
                Material boosterMat = GetOrCreateBoxMaterial(boosterType, boosterPath, renderer);
                Material[] newMats = new Material[2] { targetMaterial, boosterMat };
                renderer.sharedMaterials = newMats;
                renderer.materials = newMats;
                renderer.SetPropertyBlock(null);

                if (item.m_MeshSecondary != null)
                {
                    Renderer secRenderer = item.m_MeshSecondary.GetComponent<Renderer>();
                    if (secRenderer != null)
                    {
                        secRenderer.sharedMaterial = boosterMat;
                        secRenderer.material = boosterMat;
                        secRenderer.SetPropertyBlock(null);
                    }
                }
            }
            else
            {
                renderer.sharedMaterial = targetMaterial;
                renderer.material = targetMaterial;
                renderer.SetPropertyBlock(null);
            }

            // Pour les boosters et les displays disposant d'un mesh secondaire
            if (item.m_MeshSecondary != null)
            {
                Renderer secondaryRenderer = item.m_MeshSecondary.GetComponent<Renderer>();
                if (secondaryRenderer != null)
                {
                    if (boosterType != EItemType.None && allBoxPaths.TryGetValue(boosterType, out string boosterPath2))
                    {
                        Material boosterMat = GetOrCreateBoxMaterial(boosterType, boosterPath2, secondaryRenderer);
                        if (boosterMat != null && secondaryRenderer.sharedMaterial != boosterMat)
                        {
                            secondaryRenderer.sharedMaterial = boosterMat;
                            secondaryRenderer.SetPropertyBlock(null);
                        }
                    }
                    else if (IsBoosterPackType(itemType))
                    {
                        if (secondaryRenderer.sharedMaterial != targetMaterial)
                        {
                            secondaryRenderer.sharedMaterial = targetMaterial;
                            secondaryRenderer.SetPropertyBlock(null);
                        }
                    }
                }
            }
        }

        public static EItemType GetBoosterTypeForDisplay(EItemType displayType)
        {
            EItemType displayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
            EItemType displayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
            EItemType displayLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
            EItemType displayLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");

            if (displayType == EItemType.BasicCardBox) return EItemType.BasicCardPack;
            if (displayType == EItemType.RareCardBox) return EItemType.RareCardPack;
            if (displayType == EItemType.EpicCardBox) return EItemType.EpicCardPack;
            if (displayType == EItemType.LegendaryCardBox) return EItemType.LegendaryCardPack;
            if (displayType == EItemType.DestinyBasicCardBox) return EItemType.DestinyBasicCardPack;
            if (displayType == EItemType.DestinyRareCardBox) return EItemType.DestinyRareCardPack;
            if (displayType == EItemType.DestinyEpicCardBox) return EItemType.DestinyEpicCardPack;
            if (displayType == EItemType.DestinyLegendaryCardBox) return EItemType.DestinyLegendaryCardPack;
            if (displayType == displayStellar) return EnumExtensions.SafeParseEItemType("BoosterStellar");
            if (displayType == displayStellarTaux) return EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            if (displayType == displayLegacy) return EnumExtensions.SafeParseEItemType("BoosterLegacy");
            if (displayType == displayLegacyTaux) return EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
            return EItemType.None;
        }

        public static bool IsBoosterPackType(EItemType itemType)
        {
            return itemType == EItemType.BasicCardPack ||
                   itemType == EItemType.RareCardPack ||
                   itemType == EItemType.EpicCardPack ||
                   itemType == EItemType.LegendaryCardPack ||
                   itemType == EItemType.DestinyBasicCardPack ||
                   itemType == EItemType.DestinyRareCardPack ||
                   itemType == EItemType.DestinyEpicCardPack ||
                   itemType == EItemType.DestinyLegendaryCardPack ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterStellar") ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterStellarTaux") ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterLegacy") ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterLegacyTaux") ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterGoldBattle") ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterGoldStellar") ||
                   itemType == EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");
        }
    }
}
