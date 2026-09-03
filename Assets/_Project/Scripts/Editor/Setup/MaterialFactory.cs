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

        /// <summary>Opaque Lit with an albedo texture (tint multiplies), tiled per metre.</summary>
        public static Material Textured(string fileName, Texture2D texture, Color tint, float metallic, float smoothness, Vector2 tiling)
        {
            var mat = Opaque(fileName, tint, metallic, smoothness);
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Textured Lit with an emission map (lit windows at night).</summary>
        public static Material TexturedEmissive(string fileName, Texture2D albedo, Texture2D emission, Color tint, Color emissionColor, Vector2 tiling)
        {
            var mat = Textured(fileName, albedo, tint, 0.05f, 0.35f, tiling);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor(EmissionColor, emissionColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
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
            // Two-sided: the lofted body is an open shell around the wheel wells, so back faces
            // must render or the car looks see-through from low angles.
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
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

        /// <summary>URP particle material: alpha-blended or additive, optional vertex colour (skid marks fade by alpha).</summary>
        public static Material Particle(string fileName, Texture2D texture, Color tint, bool additive, bool vertexColor = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var mat = EditorPaths.GetOrCreateMaterial(EditorPaths.Materials + "/" + fileName + ".mat", shader);
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor(BaseColor, tint);
            mat.SetFloat(Surface, 1f);
            mat.SetFloat(Blend, additive ? 2f : 0f);
            mat.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat(DstBlend, additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat(ZWrite, 0f);
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + (vertexColor ? -10 : 0);
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
