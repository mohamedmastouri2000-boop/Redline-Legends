using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>Shared URP materials. Everything reuses these so the SRP batcher keeps draw calls cheap.</summary>
    public static class MaterialFactory
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int ClearCoatMask = Shader.PropertyToID("_ClearCoatMask");
        private static readonly int ClearCoatSmoothness = Shader.PropertyToID("_ClearCoatSmoothness");
        private static readonly int Surface = Shader.PropertyToID("_Surface");
        private static readonly int Blend = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        public static Shader ComplexLit => Shader.Find("Universal Render Pipeline/Complex Lit");

        public static Material Opaque(string fileName, Color color, float metallic, float smoothness)
        {
            var mat = EditorPaths.GetOrCreateMaterial(EditorPaths.Materials + "/" + fileName + ".mat", Lit);
            mat.SetColor(BaseColor, color);
            mat.SetFloat(Metallic, metallic);
            mat.SetFloat(Smoothness, smoothness);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Car paint: metallic base under a clear coat. Material name contains "Paint" so the paint system finds it.</summary>
        public static Material CarPaint(string fileName, Color color)
        {
            var mat = EditorPaths.GetOrCreateMaterial(EditorPaths.Materials + "/" + fileName + ".mat", ComplexLit);
            mat.SetColor(BaseColor, color);
            mat.SetFloat(Metallic, 0.65f);
            mat.SetFloat(Smoothness, 0.82f);
            mat.SetFloat(ClearCoatMask, 1f);
            mat.SetFloat(ClearCoatSmoothness, 0.95f);
            mat.EnableKeyword("_CLEARCOAT");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Glass(string fileName, Color tint)
        {
            var mat = EditorPaths.GetOrCreateMaterial(EditorPaths.Materials + "/" + fileName + ".mat", Lit);
            mat.SetColor(BaseColor, tint);
            mat.SetFloat(Metallic, 0.1f);
            mat.SetFloat(Smoothness, 0.98f);
            mat.SetFloat(Surface, 1f); // transparent
            mat.SetFloat(Blend, 0f);
            mat.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat(DstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat(ZWrite, 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Emissive(string fileName, Color color, Color emission)
        {
            var mat = Opaque(fileName, color, 0.2f, 0.6f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(EmissionColor, emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
