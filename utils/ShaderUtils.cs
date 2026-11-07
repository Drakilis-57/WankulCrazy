using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace WankulCrazyPlugin.utils
{
    /// <summary>
    /// "Shader.Find("Standard")" renvoie null en HDRP/URP (le shader "Standard" du Built-in
    /// Render Pipeline n'existe pas dans ces pipelines) : un Material construit avec un shader
    /// null s'affiche en rose/magenta plein écran. Ce helper détecte le pipeline actif et
    /// renvoie un shader de remplacement valide, en se rabattant sur "Standard" uniquement
    /// si aucun pipeline scriptable n'est actif (Built-in RP classique).
    /// </summary>
    public static class ShaderUtils
    {
        private static Shader _cachedFallbackShader;
        private static bool _resolved;

        public static Shader GetFallbackLitShader()
        {
            if (_resolved && _cachedFallbackShader != null)
            {
                return _cachedFallbackShader;
            }

            _resolved = true;

            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset != null)
            {
                string pipelineName = pipelineAsset.GetType().Name;

                if (pipelineName.Contains("HDRenderPipeline") || pipelineName.Contains("HDRP"))
                {
                    _cachedFallbackShader = Shader.Find("HDRP/Lit");
                }
                else if (pipelineName.Contains("UniversalRenderPipeline") || pipelineName.Contains("URP"))
                {
                    _cachedFallbackShader = Shader.Find("Universal Render Pipeline/Lit");
                }
            }

            if (_cachedFallbackShader == null)
            {
                // Chercher d'abord un material 2D standard du jeu (Toony Colors Pro 2) pour récupérer son shader
                var sampleMat = Resources.FindObjectsOfTypeAll<Material>()
                    .FirstOrDefault(m => m != null && m.shader != null && 
                                   (m.name == "MAT_CardPackCommon_Lightweight" || m.name == "MAT_CardBoxInner" || m.name == "MAT_Deckbox1"));
                if (sampleMat != null && sampleMat.shader != null)
                {
                    _cachedFallbackShader = sampleMat.shader;
                    Plugin.Logger?.LogInfo($"[ShaderUtils] Fallback Lit Shader trouvé depuis '{sampleMat.name}' : {_cachedFallbackShader.name}");
                }
                else
                {
                    _cachedFallbackShader = Shader.Find("Toony Colors Pro 2/Standard PBS")
                                         ?? Shader.Find("Toony Colors Pro 2/Hybrid Shader 2")
                                         ?? Shader.Find("Sprites/Default")
                                         ?? Shader.Find("Standard");
                }
            }

            if (_cachedFallbackShader == null)
            {
                Plugin.Logger?.LogError("[ShaderUtils] Aucun shader de fallback valide trouvé (ni HDRP/Lit, ni URP/Lit, ni Standard). Le rendu magenta va persister.");
            }

            return _cachedFallbackShader;
        }

        /// <summary>
        /// Renvoie true si ce material utilise le shader "Standard" (cassé/strippé) ou un
        /// shader Texture2DArray (incompatible avec l'assignation directe d'une Texture2D en 1.07+).
        /// </summary>
        public static bool UsesBrokenStandardShader(Material m)
        {
            if (m == null || m.shader == null) return false;
            string shaderName = m.shader.name;
            return shaderName == "Standard" 
                || shaderName.IndexOf("Texture2dArray", System.StringComparison.OrdinalIgnoreCase) >= 0
                || m.HasProperty("_MainTexArray");
        }

        /// <summary>
        /// Si le material passé utilise le shader "Standard" cassé ou un shader Texture2DArray,
        /// renvoie un clone avec un shader 2D sûr à la place. Sinon renvoie le material tel quel.
        /// </summary>
        public static Material EnsureNotBrokenStandard(Material m)
        {
            if (!UsesBrokenStandardShader(m))
            {
                return m;
            }

            bool isArrayShader = m.shader != null && (m.shader.name.IndexOf("Texture2dArray", System.StringComparison.OrdinalIgnoreCase) >= 0 || m.HasProperty("_MainTexArray"));
            Texture existingTex = (!isArrayShader && m.HasProperty("_MainTex")) ? m.GetTexture("_MainTex") : null;
            Material fixedMat = CreateSafeMaterial(); // ne clone pas m : on change volontairement de shader
            fixedMat.name = m.name + "_FixedShader";
            if (existingTex is Texture2D)
            {
                if (fixedMat.HasProperty("_MainTex")) fixedMat.SetTexture("_MainTex", existingTex);
                if (fixedMat.HasProperty("_BaseColorMap")) fixedMat.SetTexture("_BaseColorMap", existingTex);
                if (fixedMat.HasProperty("_BaseMap")) fixedMat.SetTexture("_BaseMap", existingTex);
            }
            Plugin.Logger?.LogInfo($"[ShaderUtils] Material '{m.name}' utilisait le shader '{m.shader?.name}' -> remplacé par '{fixedMat.shader.name}'.");
            return fixedMat;
        }

        /// <summary>
        /// Construit un Material sûr pour une Texture2D : clone sourceMaterial s'il est compatible 2D,
        /// sinon retombe sur le shader 2D détecté par pipeline (ex: Unlit/UnlitWithShadowcaster).
        /// Ne JAMAIS faire `new Material(Shader.Find("Standard"))` directement.
        /// </summary>
        public static Material CreateSafeMaterial(Material sourceMaterial = null)
        {
            if (sourceMaterial != null && !UsesBrokenStandardShader(sourceMaterial))
            {
                return new Material(sourceMaterial);
            }

            Shader fallback = GetFallbackLitShader();
            Material mat = fallback != null ? new Material(fallback) : new Material(Shader.Find("Standard"));
            if (sourceMaterial != null)
            {
                mat.name = sourceMaterial.name + "_2D";
            }
            return mat;
        }
    }
}
