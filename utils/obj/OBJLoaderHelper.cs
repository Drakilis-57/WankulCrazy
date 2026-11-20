using UnityEngine;

namespace WankulCrazyPlugin.utils.obj
{
    public static class OBJLoaderHelper
    {
        // AVANT: new Material(Shader.Find("Standard")) — "Standard" n'existe pas en HDRP/URP,
        // Shader.Find renvoie null et le material s'affiche en magenta plein écran.
        public static Material CreateNullMaterial() => ShaderUtils.CreateSafeMaterial();
    }
}
