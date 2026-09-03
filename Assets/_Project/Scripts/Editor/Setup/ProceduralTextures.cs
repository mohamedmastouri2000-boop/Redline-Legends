using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Tileable placeholder textures generated at setup time: asphalt with lane markings, striped
    /// kerb + run-off, ground noise, concrete, building facades with a lit-window emission map, and
    /// UI sprites (rounded rect, gauge ring, control icons). Real textures replace these one-to-one.
    /// </summary>
    public static class ProceduralTextures
    {
        private const string Folder = EditorPaths.Materials + "/Textures";

        // ------------------------------------------------------------------ helpers
        private static Texture2D GetOrCreate(string name, int size, System.Func<int, int, Color> pixel, bool repeat, bool mipmaps = true, FilterMode filter = FilterMode.Bilinear)
        {
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            EditorPaths.EnsureFolder(Folder);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipmaps) { name = name };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = pixel(x, y);
            tex.SetPixels(pixels);
            tex.Apply(mipmaps, false);
            tex.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.filterMode = filter;
            tex.anisoLevel = 4;
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        /// <summary>Tileable value noise: sum of octaves sampled on a periodic lattice.</summary>
        private static float Noise(int x, int y, int size, int seed, int octaves = 4, float persistence = 0.5f)
        {
            float amp = 1f, freq = 4f, total = 0f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float fx = x / (float)size * freq, fy = y / (float)size * freq;
                int period = Mathf.RoundToInt(freq);
                total += Lattice(fx, fy, period, seed + o * 131) * amp;
                norm += amp;
                amp *= persistence;
                freq *= 2f;
            }
            return total / norm;
        }

        private static float Lattice(float fx, float fy, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0, y0, period, seed), b = Hash(x0 + 1, y0, period, seed);
            float c = Hash(x0, y0 + 1, period, seed), d = Hash(x0 + 1, y0 + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash(int x, int y, int period, int seed)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777216f;
        }

        // ------------------------------------------------------------------ world textures
        /// <summary>u across the road (0..1), v along it (one tile = 8 m). Edge lines and a dashed centre line.</summary>
        public static Texture2D Asphalt() => GetOrCreate("Tex_Asphalt", 512, (x, y) =>
        {
            const int size = 512;
            float n = Noise(x, y, size, 7, 5, 0.55f);
            float grain = Noise(x * 3 % size, y * 3 % size, size, 91, 2, 0.5f);
            float v = 0.38f + (n - 0.5f) * 0.18f + (grain - 0.5f) * 0.08f;
            var c = new Color(v, v, v * 1.02f, 1f);
            float u = x / (float)size;
            float along = y / (float)size; // 8 m per tile
            bool edge = Mathf.Abs(u - 0.035f) < 0.008f || Mathf.Abs(u - 0.965f) < 0.008f;
            bool centre = Mathf.Abs(u - 0.5f) < 0.006f && along < 0.4f;
            if (edge || centre)
            {
                float wear = 0.75f + n * 0.25f;
                c = Color.Lerp(c, new Color(0.9f, 0.9f, 0.86f) * wear, 0.85f);
            }
            return c;
        }, true);

        /// <summary>u across the shoulder: first quarter is red/white kerb stripes, the rest run-off (tinted by material colour).</summary>
        public static Texture2D Shoulder() => GetOrCreate("Tex_Shoulder", 256, (x, y) =>
        {
            const int size = 256;
            float u = x / (float)size;
            float n = Noise(x, y, size, 21, 4, 0.5f);
            if (u < 0.25f)
            {
                bool red = ((y / 32) % 2) == 0;
                var stripe = red ? new Color(0.85f, 0.18f, 0.14f) : new Color(0.92f, 0.92f, 0.9f);
                return stripe * (0.85f + n * 0.15f);
            }
            float g = 0.55f + (n - 0.5f) * 0.4f;
            return new Color(g, g, g, 1f);
        }, true);

        /// <summary>Emission mask for glowing kerbs: the stripe band lights up, the run-off stays dark.</summary>
        public static Texture2D ShoulderEmission() => GetOrCreate("Tex_ShoulderEmission", 256, (x, y) =>
        {
            float u = x / 256f;
            if (u >= 0.25f) return Color.black;
            bool red = ((y / 32) % 2) == 0;
            return red ? new Color(0.3f, 0.3f, 0.3f, 1f) : Color.white;
        }, true);

        public static Texture2D Ground() => GetOrCreate("Tex_Ground", 512, (x, y) =>
        {
            float n = Noise(x, y, 512, 33, 5, 0.6f);
            float patches = Noise(x, y, 512, 77, 2, 0.5f);
            float v = 0.55f + (n - 0.5f) * 0.5f + (patches - 0.5f) * 0.3f;
            return new Color(v, v, v, 1f);
        }, true);

        public static Texture2D Concrete() => GetOrCreate("Tex_Concrete", 256, (x, y) =>
        {
            float n = Noise(x, y, 256, 55, 4, 0.5f);
            float seam = (y % 64) < 2 ? 0.7f : 1f;
            float v = (0.6f + (n - 0.5f) * 0.25f) * seam;
            return new Color(v, v, v, 1f);
        }, true);

        /// <summary>Facade albedo: panels with dark window rectangles on a 4x4 grid per tile.</summary>
        public static Texture2D Facade() => GetOrCreate("Tex_Facade", 256, (x, y) =>
        {
            int cx = x % 64, cy = y % 64;
            bool window = cx > 10 && cx < 54 && cy > 14 && cy < 50;
            float n = Noise(x, y, 256, 12, 3, 0.5f);
            if (window) return new Color(0.08f, 0.1f, 0.14f, 1f) * (0.8f + n * 0.4f);
            float v = 0.55f + (n - 0.5f) * 0.15f;
            return new Color(v, v, v, 1f);
        }, true);

        /// <summary>Emission mask: a random subset of windows lit (warm), the rest black.</summary>
        public static Texture2D FacadeEmission() => GetOrCreate("Tex_FacadeEmission", 256, (x, y) =>
        {
            int cx = x % 64, cy = y % 64;
            bool window = cx > 12 && cx < 52 && cy > 16 && cy < 48;
            if (!window) return Color.black;
            float lit = Hash(x / 64, y / 64, 1024, 5);
            return lit > 0.45f ? new Color(1f, 0.85f, 0.55f, 1f) * (0.6f + lit * 0.4f) : Color.black;
        }, true);

        // ------------------------------------------------------------------ UI sprites
        public static Sprite RoundedRect()
        {
            var tex = GetOrCreate("Ui_Rounded", 64, (x, y) =>
            {
                const int size = 64; const float radius = 18f;
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius), cy = Mathf.Clamp(py, radius, size - radius);
                float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                return new Color(1f, 1f, 1f, a);
            }, false, false);
            return GetOrCreateSprite("Ui_Rounded", tex, new Vector4(24f, 24f, 24f, 24f));
        }

        /// <summary>Menu backdrop: opaque on the left fading to clear on the right, with a soft vignette.</summary>
        public static Sprite MenuFade()
        {
            var tex = GetOrCreate("Ui_MenuFade", 256, (x, y) =>
            {
                float u = (x + 0.5f) / 256f, v = (y + 0.5f) / 256f;
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.92f, u));
                float vignette = 0.45f * Mathf.Pow(Mathf.Abs(v - 0.5f) * 2f, 2.2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(fade + vignette));
            }, false, false);
            return GetOrCreateSprite("Ui_MenuFade", tex, Vector4.zero);
        }

        /// <summary>Vertical gradient, opaque at the bottom and clear at the top (HUD and header shading).</summary>
        public static Sprite ShadeUp()
        {
            var tex = GetOrCreate("Ui_ShadeUp", 64, (x, y) =>
            {
                float v = (y + 0.5f) / 64f;
                return new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, v));
            }, false, false);
            return GetOrCreateSprite("Ui_ShadeUp", tex, Vector4.zero);
        }

        public static Sprite Ring()
        {
            var tex = GetOrCreate("Ui_Ring", 256, (x, y) =>
            {
                const int size = 256;
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float outer = Mathf.Clamp01((1f - d) * size / 2f);
                float inner = Mathf.Clamp01((d - 0.8f) * size / 2f);
                return new Color(1f, 1f, 1f, Mathf.Min(outer, inner));
            }, false, false);
            return GetOrCreateSprite("Ui_Ring", tex, Vector4.zero);
        }

        public static Sprite Circle()
        {
            var tex = GetOrCreate("Ui_Circle", 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64f, 64f));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(63.5f - d));
            }, false, false);
            return GetOrCreateSprite("Ui_Circle", tex, Vector4.zero);
        }

        /// <summary>Chevron arrow pointing right; flip the RectTransform scale for left.</summary>
        public static Sprite Arrow()
        {
            var tex = GetOrCreate("Ui_Arrow", 128, (x, y) =>
            {
                float px = (x + 0.5f) / 128f, py = (y + 0.5f) / 128f;
                // chevron: two thick strokes from (0.3,0.2)->(0.7,0.5) and (0.3,0.8)->(0.7,0.5)
                float d1 = SegmentDistance(new Vector2(px, py), new Vector2(0.32f, 0.22f), new Vector2(0.7f, 0.5f));
                float d2 = SegmentDistance(new Vector2(px, py), new Vector2(0.32f, 0.78f), new Vector2(0.7f, 0.5f));
                float a = Mathf.Clamp01((0.09f - Mathf.Min(d1, d2)) * 60f);
                return new Color(1f, 1f, 1f, a);
            }, false, false);
            return GetOrCreateSprite("Ui_Arrow", tex, Vector4.zero);
        }

        /// <summary>Pedal glyph: rounded tall plate with three grip bars.</summary>
        public static Sprite Pedal()
        {
            var tex = GetOrCreate("Ui_Pedal", 128, (x, y) =>
            {
                float px = (x + 0.5f) / 128f, py = (y + 0.5f) / 128f;
                bool plate = px > 0.3f && px < 0.7f && py > 0.12f && py < 0.88f;
                bool bar = plate && ((py > 0.28f && py < 0.36f) || (py > 0.46f && py < 0.54f) || (py > 0.64f && py < 0.72f)) && px > 0.36f && px < 0.64f;
                return new Color(1f, 1f, 1f, plate ? (bar ? 0.25f : 1f) : 0f);
            }, false, false);
            return GetOrCreateSprite("Ui_Pedal", tex, Vector4.zero);
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        private static Sprite GetOrCreateSprite(string name, Texture2D tex, Vector4 border)
        {
            string path = Folder + "/" + name + "_sprite.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }
    }
}
