using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WankulCrazyPlugin.utils
{
    public class TextureUtils
    {
        public static Texture2D MakeTextureReadable(Texture2D texture)
        {
            RenderTexture temporaryRenderTex = RenderTexture.GetTemporary(
                texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            Graphics.Blit(texture, temporaryRenderTex);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = temporaryRenderTex;

            Texture2D readableTexture = new Texture2D(texture.width, texture.height);
            readableTexture.ReadPixels(new Rect(0, 0, temporaryRenderTex.width, temporaryRenderTex.height), 0, 0);
            readableTexture.Apply(); // NE PAS utiliser makeNoLongerReadable=true ici :
            // cette copie doit rester lisible par CPU pour les GetPixels() de PatchTexturesImporter.

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporaryRenderTex);

            return readableTexture;
        }

        /// <summary>
        /// Indique si un chemin correspond à une image JPEG sans canal alpha (.jpg / .jpeg).
        /// </summary>
        public static bool IsJpegPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Charge une texture depuis le disque avec optimisation VRAM (compression DXT1/DXT5 si dimensions multiples de 4
        /// et libération de la mémoire CPU via markNonReadable).
        /// </summary>
        public static Texture2D LoadTexture(
            string path,
            bool markNonReadable = true,
            bool compress = true,
            bool mipChain = false,
            Action<Texture2D> configure = null,
            bool hasAlpha = true)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return null;
            }

            byte[] bytes = System.IO.File.ReadAllBytes(path);
            return LoadTextureFromBytes(bytes, markNonReadable, compress, mipChain, configure, hasAlpha);
        }

        /// <summary>
        /// Charge une texture depuis un buffer en mémoire avec optimisation VRAM :
        /// LoadImage -> Compress(false) conditionnel (multiples de 4 uniquement) -> Apply(mipChain, makeNoLongerReadable).
        /// Quand hasAlpha == false, instancie en RGB24 pour que Compress(false) compresse en DXT1 (0,5 B/px) au lieu de DXT5 (1 B/px).
        /// </summary>
        public static Texture2D LoadTextureFromBytes(
            byte[] bytes,
            bool markNonReadable = true,
            bool compress = true,
            bool mipChain = false,
            Action<Texture2D> configure = null,
            bool hasAlpha = true)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            TextureFormat initialFormat = hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            Texture2D texture = new Texture2D(2, 2, initialFormat, mipChain);

            if (!compress && !mipChain)
            {
                if (!texture.LoadImage(bytes, markNonReadable))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }
                configure?.Invoke(texture);
                return texture;
            }

            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            configure?.Invoke(texture);

            bool compressed = false;
            if (compress && texture.width % 4 == 0 && texture.height % 4 == 0)
            {
                texture.Compress(false);
                compressed = true;
            }

            texture.Apply(mipChain && !compressed, makeNoLongerReadable: markNonReadable);
            return texture;
        }
    }
}
