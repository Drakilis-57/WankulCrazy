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
        public static void ReplaceGameTextures(string textureLevel)
        {
            string texturesPath = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", textureLevel);
            if (!Directory.Exists(texturesPath))
            {
                return;
            }
            List<string> filenames = GetFileNames(texturesPath);

            foreach (string filename in filenames)
            {
                try
                {
                    string textureName = Path.GetFileNameWithoutExtension(filename);
                    Texture targetTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == textureName);
                    if (targetTexture != null)
                    {
                        string texturePath = Path.Combine(texturesPath, filename);
                        if (targetTexture is Texture2D targetTexture2D)
                        {
                            Texture2D newTexture = LoadTexture2D(texturePath);
                            try
                            {
                                ReplaceTexture(targetTexture2D, newTexture);
                            }
                            finally
                            {
                                if (newTexture != null)
                                {
                                    UnityEngine.Object.Destroy(newTexture);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Les textures custom pour les displays, boosters, statues, etc. ne sont pas dans les ressources du jeu de base
                        // Elles sont gérées directement par nos propres hooks (OBJImporter, GetBoxTexturePaths, etc.).
                        bool isKnownCustomTexture = textureName.StartsWith("Texture_Display_") ||
                                                   textureName.StartsWith("Texture_Booster_") ||
                                                   textureName.StartsWith("MonsterStatue_") ||
                                                   textureName.StartsWith("Texture_Starter_") ||
                                                   textureName.StartsWith("Texture_Tapis_") ||
                                                   textureName.StartsWith("T_CardSleeve") ||
                                                   textureName.StartsWith("T_BasicCardBox") ||
                                                   textureName.StartsWith("T_EpicCardBox") ||
                                                   textureName.StartsWith("T_RareCardBox");

                        if (!isKnownCustomTexture)
                        {
                            Plugin.Logger.LogWarning($"Texture {textureName} not found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Erreur lors du remplacement de la texture '{filename}': {ex.Message}");
                }
            }
        }
        public static Texture2D ReplaceTexture(Texture2D original, Texture2D replacement)
        {
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

            if (original.width != replacement.width || original.height != replacement.height)
            {
                Plugin.Logger.LogError($"Dimensions incompatibles. Original: {original.width}x{original.height}, Remplacement: {replacement.width}x{replacement.height}");
                return original;
            }

            Texture2D intermediate = null;
            Texture2D intermediateMip = null;

            if (original.mipmapCount != replacement.mipmapCount)
            {
                intermediateMip = AdjustMipMapLevels(replacement, original.mipmapCount);
                replacement = intermediateMip;
            }

            if (original.format != replacement.format)
            {
                intermediate = ConvertTextureFormat(replacement, original.format, original.mipmapCount);
                replacement = intermediate;
            }

            try
            {
                Graphics.CopyTexture(replacement, original);

                // Amélioration de la netteté pour les textures de décor/affiches vues à distance et sous des angles rasants
                original.filterMode = FilterMode.Trilinear;
                original.anisoLevel = 16;
                original.mipMapBias = -0.5f;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Erreur lors de la copie : {ex.Message}");
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

            return original;
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

        private static Dictionary<EItemType, string> GetBoxTexturePaths()
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
                    { EItemType.LegendaryCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_HS.png") },
                    { EItemType.DestinyBasicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S1_TauxDrop.png") },
                    { EItemType.DestinyRareCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S2_TauxDrop.png") },
                    { EItemType.DestinyEpicCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_S3_TauxDrop.png") },
                    { EItemType.DestinyLegendaryCardBox, Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Display_HS_TauxDrop.png") },

                    // Boosters custom
                    { EnumExtensions.SafeParseEItemType("BoosterStellar"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S4.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterStellarTaux"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S4_TauxDrop.png") },
                    { EnumExtensions.SafeParseEItemType("BoosterLegacy"), Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S5.png") },
                };

                EItemType ascensionPack = EnumExtensions.SafeParseEItemType("AscensionCardPack");
                if (ascensionPack != (EItemType)0)
                {
                    boxTexturePaths[ascensionPack] = Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Texture_Booster_S5.png");
                }
            }
            return boxTexturePaths;
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
                tex.Apply();
                return tex;
            }
            return null;
        }

        private static Material GetOrCreateBoxMaterial(EItemType itemType, string texturePath, Renderer sourceRenderer)
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

            // On clone le material existant du renderer pour conserver le shader et les propriétés
            // du pipeline de rendu du jeu (HDRP/URP). Shader.Find("Standard") est incompatible.
            Material sourceMat = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
            if (sourceMat == null)
            {
                ItemMeshData fallbackMesh = InventoryBase.GetItemMeshData(EItemType.BasicCardPack) ?? InventoryBase.GetItemMeshData(EItemType.BasicCardBox);
                sourceMat = fallbackMesh?.material;
            }

            // Neutraliser le shader Standard cassé
            sourceMat = WankulCrazyPlugin.utils.ShaderUtils.EnsureNotBrokenStandard(sourceMat);
            Material newMat = WankulCrazyPlugin.utils.ShaderUtils.CreateSafeMaterial(sourceMat);

            // Assigner la texture sur la propriété principale (HDRP = _BaseColorMap, URP = _BaseMap, Built-in = _MainTex)
            // Ne PAS assigner newMat.mainTexture pour éviter l'erreur si le shader ne supporte pas _MainTex
            if (newMat.HasProperty("_BaseColorMap"))
                newMat.SetTexture("_BaseColorMap", texture);
            if (newMat.HasProperty("_BaseMap"))
                newMat.SetTexture("_BaseMap", texture);
            if (newMat.HasProperty("_MainTex"))
                newMat.SetTexture("_MainTex", texture);

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

            if (renderer.sharedMaterial != targetMaterial)
            {
                renderer.sharedMaterial = targetMaterial;
            }

            // Pour les boosters et items disposant d'un mesh secondaire (volume/dos)
            if (item.m_MeshSecondary != null)
            {
                Renderer secondaryRenderer = item.m_MeshSecondary.GetComponent<Renderer>();
                if (secondaryRenderer != null && secondaryRenderer.sharedMaterial != targetMaterial)
                {
                    secondaryRenderer.sharedMaterial = targetMaterial;
                }
            }
        }
    }
}
