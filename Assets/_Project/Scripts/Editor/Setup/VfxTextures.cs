using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>Generated particle textures so effects work before art assets exist.</summary>
    public static class VfxTextures
    {
        private const string SoftCirclePath = EditorPaths.Materials + "/Tex_SoftCircle.asset";
        private const string StreakPath = EditorPaths.Materials + "/Tex_Streak.asset";

        public static Texture2D GetOrCreateSoftCircle()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(SoftCirclePath);
            if (existing != null) return existing;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            AssetDatabase.CreateAsset(tex, SoftCirclePath);
            return tex;
        }

        public static Texture2D GetOrCreateStreak()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(StreakPath);
            if (existing != null) return existing;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float edge = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 4f); // soft sides, solid middle
                int n = ((x * 7 + y * 13) % 5);
                byte a = (byte)(edge * (200 + n * 8));
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            AssetDatabase.CreateAsset(tex, StreakPath);
            return tex;
        }
    }
}
