using System;
using System.IO;
using UnityEngine;
using WankulCrazyPlugin;

namespace WankulCrazyPlugin.utils
{
    public static class TextureCacheManager
    {
        private static string GetCacheDirectory()
        {
            string path = Path.Combine(Plugin.GetPluginPath(), "cache", "textures");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private static string GetCacheFilePath(string sourcePath)
        {
            string fileName = sourcePath.Replace(Plugin.GetPluginPath(), "").Replace("\\", "_").Replace("/", "_").TrimStart('_');
            return Path.Combine(GetCacheDirectory(), fileName + ".dat");
        }

        public static Texture2D LoadTextureCached(string sourcePath, bool markNonReadable = true)
        {
            if (!File.Exists(sourcePath)) return null;

            string cachePath = GetCacheFilePath(sourcePath);
            DateTime sourceTime = File.GetLastWriteTimeUtc(sourcePath);

            // Try load from cache
            if (File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath) >= sourceTime)
            {
                try
                {
                    using (FileStream fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        int width = br.ReadInt32();
                        int height = br.ReadInt32();
                        TextureFormat format = (TextureFormat)br.ReadInt32();
                        bool hasMipMaps = br.ReadBoolean();
                        int dataLength = br.ReadInt32();

                        byte[] rawData = br.ReadBytes(dataLength);

                        Texture2D texture = new Texture2D(width, height, format, hasMipMaps);
                        texture.LoadRawTextureData(rawData);
                        texture.Apply(false, markNonReadable);
                        texture.wrapMode = TextureWrapMode.Clamp;
                        return texture;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"[TextureCache] Failed to load cache for {sourcePath}, falling back to source. Error: {ex.Message}");
                }
            }

            // Fallback to loading original image
            try
            {
                byte[] bytes = File.ReadAllBytes(sourcePath);

                // We must load it readable first to get the raw data
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, false))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.wrapMode = TextureWrapMode.Clamp;

                // Save to cache
                SaveToCache(texture, cachePath);

                // Apply readability setting
                if (markNonReadable)
                {
                    texture.Apply(false, true);
                }

                return texture;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load texture {sourcePath}: {ex.Message}");
                return null;
            }
        }

        private static void SaveToCache(Texture2D texture, string cachePath)
        {
            try
            {
                byte[] rawData = texture.GetRawTextureData();

                using (FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(texture.width);
                    bw.Write(texture.height);
                    bw.Write((int)texture.format);
                    bw.Write(texture.mipmapCount > 1);
                    bw.Write(rawData.Length);
                    bw.Write(rawData);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[TextureCache] Failed to save cache to {cachePath}: {ex.Message}");
            }
        }
    }
}
