using UnityEngine;

namespace WankulCrazyPlugin.utils
{
    /// <summary>
    /// Outil de diagnostic temporaire : dump récursif de tous les Renderer sous un GameObject,
    /// avec le nom du shader de chaque material, s'il est valide (render queue non -1), et si
    /// les propriétés de texture usuelles (_BaseColorMap/_BaseMap/_MainTex) sont assignées.
    /// À retirer une fois le bug magenta identifié.
    /// </summary>
    public static class MaterialDiagnostics
    {
        public static void DumpRenderers(GameObject root, string context)
        {
            if (root == null)
            {
                Plugin.Logger?.LogWarning($"[MatDiag:{context}] root est null.");
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Plugin.Logger?.LogInfo($"[MatDiag:{context}] GameObject '{root.name}' — {renderers.Length} Renderer(s) trouvé(s).");

            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null)
                    {
                        Plugin.Logger?.LogWarning($"[MatDiag:{context}]   '{r.gameObject.name}' slot {i}: material NULL");
                        continue;
                    }

                    string shaderName = m.shader != null ? m.shader.name : "<<SHADER NULL>>";
                    bool shaderIsSupported = m.shader != null && m.shader.isSupported;
                    string hasBaseColorMap = m.HasProperty("_BaseColorMap") ? (m.GetTexture("_BaseColorMap") != null ? "OK" : "vide") : "n/a";
                    string hasBaseMap = m.HasProperty("_BaseMap") ? (m.GetTexture("_BaseMap") != null ? "OK" : "vide") : "n/a";
                    string hasMainTex = m.HasProperty("_MainTex") ? (m.GetTexture("_MainTex") != null ? "OK" : "vide") : "n/a";

                    Plugin.Logger?.LogInfo(
                        $"[MatDiag:{context}]   '{r.gameObject.name}' slot {i}: material='{m.name}' shader='{shaderName}' supported={shaderIsSupported} " +
                        $"renderQueue={m.renderQueue} _BaseColorMap={hasBaseColorMap} _BaseMap={hasBaseMap} _MainTex={hasMainTex}"
                    );

                    if (m.shader == null || !shaderIsSupported)
                    {
                        Plugin.Logger?.LogError($"[MatDiag:{context}]   ⚠️ '{r.gameObject.name}' slot {i} a un shader NULL ou non supporté par le pipeline courant — CANDIDAT MAGENTA.");
                    }
                }
            }

            Plugin.Logger?.LogInfo($"[MatDiag:{context}] Pipeline actif: {(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name : "Built-in (aucun SRP)")}");
        }
    }
}
