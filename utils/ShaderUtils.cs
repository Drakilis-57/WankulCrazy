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
                // Built-in RP (ou pipeline non reconnu) : NE PAS utiliser "Standard".
                // Shader.Find("Standard") réussit et isSupported==true même si Unity a
                // strippé toutes les variantes du shader à la compilation (le jeu de base
                // ne l'utilise nulle part) -> rendu magenta silencieux au runtime, invisible
                // depuis le code (pas d'exception, pas de log Unity).
                // On préfère un shader dont on sait qu'il est réellement compilé dans ce
                // build car utilisé ailleurs par le jeu (confirmé via MaterialDiagnostics).
                _cachedFallbackShader = Shader.Find("Unlit/UnlitWithShadowcaster")
                                        ?? Shader.Find("Sprites/Default") // shader quasi toujours embarqué (UI/2D)
                                        ?? Shader.Find("Standard");        // dernier recours si vraiment rien d'autre
            }

            if (_cachedFallbackShader == null)
            {
                Plugin.Logger?.LogError("[ShaderUtils] Aucun shader de fallback valide trouvé (ni HDRP/Lit, ni URP/Lit, ni Standard). Le rendu magenta va persister.");
            }

            return _cachedFallbackShader;
        }

        /// <summary>
        /// Renvoie true si ce material utilise le shader "Standard" — confirmé, dans ce
        /// build, comme silencieusement cassé (variantes strippées, rendu magenta) car le
        /// jeu de base ne l'utilise nulle part ailleurs. À appeler avant de lire/assigner
        /// des textures sur un material dont on n'est pas sûr de l'origine.
        /// </summary>
        public static bool UsesBrokenStandardShader(Material m)
        {
            return m != null && m.shader != null && m.shader.name == "Standard";
        }

        /// <summary>
        /// Si le material passé utilise le shader "Standard" cassé, renvoie un clone avec un
        /// shader sûr à la place (texture principale préservée si possible). Sinon renvoie
        /// le material tel quel (pas de clone inutile).
        /// </summary>
        public static Material EnsureNotBrokenStandard(Material m)
        {
            if (!UsesBrokenStandardShader(m))
            {
                return m;
            }

            Texture existingTex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Material fixedMat = CreateSafeMaterial(); // ne clone pas m : on change volontairement de shader
            fixedMat.name = m.name + "_FixedShader";
            if (existingTex != null)
            {
                if (fixedMat.HasProperty("_MainTex")) fixedMat.SetTexture("_MainTex", existingTex);
                if (fixedMat.HasProperty("_BaseColorMap")) fixedMat.SetTexture("_BaseColorMap", existingTex);
                if (fixedMat.HasProperty("_BaseMap")) fixedMat.SetTexture("_BaseMap", existingTex);
            }
            Plugin.Logger?.LogWarning($"[ShaderUtils] Material '{m.name}' utilisait le shader 'Standard' (cassé/strippé dans ce build) -> remplacé par '{fixedMat.shader.name}'.");
            return fixedMat;
        }

        /// <summary>
        /// Construit un Material sûr : clone sourceMaterial si disponible (préserve shader +
        /// propriétés du pipeline en cours), sinon retombe sur le shader détecté par pipeline.
        /// Ne JAMAIS faire `new Material(Shader.Find("Standard"))` directement : c'est la cause
        /// du plein écran magenta en HDRP/URP.
        /// </summary>
        public static Material CreateSafeMaterial(Material sourceMaterial = null)
        {
            if (sourceMaterial != null)
            {
                return new Material(sourceMaterial);
            }

            Shader fallback = GetFallbackLitShader();
            return fallback != null ? new Material(fallback) : new Material(Shader.Find("Standard"));
        }
    }
}
