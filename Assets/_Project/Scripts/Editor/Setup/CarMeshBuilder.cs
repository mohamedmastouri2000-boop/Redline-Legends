using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Procedural car bodies. The lower body is lofted from rounded-rectangle cross-sections whose
    /// width, sill, shoulder and crown follow smooth curves along the length; semicircular wheel
    /// arches are carved by projecting nearby vertices onto the arch circle. A separate greenhouse
    /// loft sits on the belt line with tumblehome, a raked windscreen, rear glass and A/B/C
    /// pillars. Wheels are lathed (tyre with rounded shoulders, dished rim) with spokes. Every
    /// vehicle id gets deterministic variation so the fifteen cars differ in proportion and trim.
    /// Submeshes: 0 paint, 1 glass, 2 dark trim. Roughly 9k triangles per car.
    /// </summary>
    public static class CarMeshBuilder
    {
        public sealed class Profile
        {
            public float Length = 4.4f;
            public float Width = 1.84f;
            public float WheelRadius = 0.33f;
            public float WheelWidth = 0.24f;
            public float Wheelbase = 2.65f;
            public float Track = 1.58f;

            // Heights (m above ground).
            public float Sill = 0.2f;          // body bottom edge
            public float NoseY = 0.62f;        // shoulder at the very front
            public float BonnetY = 0.8f;       // shoulder at the windscreen base
            public float BeltY = 0.84f;        // shoulder through the cabin
            public float DeckY = 0.82f;        // shoulder at the rear glass base
            public float TailY = 0.78f;        // shoulder at the very back
            public float RoofY = 1.38f;
            public float Crown = 0.035f;       // bonnet/deck centre rises above the shoulder

            // Longitudinal positions in normalised length (-1 rear .. +1 front).
            public float CabinFront = 0.14f;   // windscreen base
            public float RoofFront = -0.08f;   // windscreen top
            public float RoofRear = -0.5f;     // rear glass top
            public float CabinRear = -0.86f;   // rear glass base
            public float BPillar = -0.3f;

            // Plan view and section shaping.
            public float NoseTaper = 0.8f;
            public float TailTaper = 0.86f;
            public float RearHaunch = 1f;      // >1 widens the rear (mid-engine stance)
            public float ShoulderRadius = 0.16f;
            public float BottomRadius = 0.07f;
            public float GlassInset = 0.1f;
            public float Tumblehome = 0.2f;
            public float RoofRadius = 0.22f;
            public float ArchGap = 0.075f;
            public float ArchFlare = 0.035f;

            public int Spoiler;                // 0 none, 1 lip, 2 wing
            public bool Splitter;
            public int Spokes = 5;
            public float RimDish = 0.05f;
            public bool SharkFin;
            public int ExhaustCount = 2;
        }

        /// <summary>Class silhouette plus deterministic per-vehicle variation.</summary>
        public static Profile ProfileFor(PlaceholderCarBuilder.Shape shape, Vehicles.VehicleClass cls, string vehicleId = null)
        {
            var p = new Profile
            {
                Length = shape.Length, Width = shape.Width, WheelRadius = shape.WheelRadius, WheelWidth = shape.WheelWidth,
                Wheelbase = shape.Wheelbase, Track = shape.Track
            };
            switch (cls)
            {
                case Vehicles.VehicleClass.Sport:
                    p.Sill = 0.17f; p.NoseY = 0.58f; p.BonnetY = 0.8f; p.BeltY = 0.84f; p.DeckY = 0.8f; p.TailY = 0.76f; p.RoofY = 1.27f;
                    p.CabinFront = 0.08f; p.RoofFront = -0.14f; p.RoofRear = -0.5f; p.CabinRear = -0.92f; p.BPillar = -0.38f;
                    p.NoseTaper = 0.78f; p.TailTaper = 0.88f; p.ShoulderRadius = 0.2f; p.Tumblehome = 0.24f; p.RoofRadius = 0.26f;
                    p.Spoiler = 2; p.Spokes = 5; p.SharkFin = false; p.ExhaustCount = 2;
                    break;
                case Vehicles.VehicleClass.Super:
                    p.Sill = 0.14f; p.NoseY = 0.5f; p.BonnetY = 0.74f; p.BeltY = 0.82f; p.DeckY = 0.84f; p.TailY = 0.8f; p.RoofY = 1.16f;
                    p.CabinFront = 0.3f; p.RoofFront = 0.02f; p.RoofRear = -0.3f; p.CabinRear = -0.7f; p.BPillar = -0.2f;
                    p.NoseTaper = 0.72f; p.TailTaper = 0.92f; p.RearHaunch = 1.04f; p.ShoulderRadius = 0.22f; p.Tumblehome = 0.26f; p.RoofRadius = 0.28f;
                    p.Crown = 0.02f; p.Spoiler = 2; p.Splitter = true; p.Spokes = 7; p.ExhaustCount = 4;
                    break;
                case Vehicles.VehicleClass.Hyper:
                    p.Sill = 0.12f; p.NoseY = 0.46f; p.BonnetY = 0.7f; p.BeltY = 0.8f; p.DeckY = 0.84f; p.TailY = 0.82f; p.RoofY = 1.1f;
                    p.CabinFront = 0.34f; p.RoofFront = 0.06f; p.RoofRear = -0.26f; p.CabinRear = -0.64f; p.BPillar = -0.14f;
                    p.NoseTaper = 0.68f; p.TailTaper = 0.95f; p.RearHaunch = 1.06f; p.ShoulderRadius = 0.24f; p.Tumblehome = 0.3f; p.RoofRadius = 0.3f;
                    p.Crown = 0.015f; p.Spoiler = 2; p.Splitter = true; p.Spokes = 6; p.ExhaustCount = 4; p.ArchFlare = 0.05f;
                    break;
                default: // Street hatch
                    p.Sill = 0.22f; p.NoseY = 0.66f; p.BonnetY = 0.84f; p.BeltY = 0.88f; p.DeckY = 0.86f; p.TailY = 0.82f; p.RoofY = 1.44f;
                    p.CabinFront = 0.2f; p.RoofFront = -0.02f; p.RoofRear = -0.72f; p.CabinRear = -0.94f; p.BPillar = -0.34f;
                    p.NoseTaper = 0.82f; p.TailTaper = 0.9f; p.ShoulderRadius = 0.14f; p.Tumblehome = 0.16f; p.RoofRadius = 0.2f;
                    p.Spoiler = 1; p.Spokes = 5; p.SharkFin = true; p.ExhaustCount = 1;
                    break;
            }
            if (!string.IsNullOrEmpty(vehicleId)) Vary(p, vehicleId);
            return p;
        }

        /// <summary>Small, deterministic proportion and trim changes keyed on the vehicle id.</summary>
        private static void Vary(Profile p, string id)
        {
            uint h = 2166136261u;
            foreach (char c in id) { h ^= c; h *= 16777619u; }
            float R(int k) { h = h * 1664525u + 1013904223u + (uint)k; return ((h >> 8) & 0xFFFF) / 65535f; }
            float S(int k, float amount) => 1f + (R(k) - 0.5f) * 2f * amount;
            p.Length *= S(1, 0.05f);
            p.Width *= S(2, 0.03f);
            p.RoofY *= S(3, 0.04f);
            float belt = S(4, 0.03f);
            p.BonnetY *= belt; p.BeltY *= belt; p.DeckY *= S(5, 0.03f);
            p.ShoulderRadius *= S(6, 0.3f);
            p.Tumblehome *= S(7, 0.25f);
            p.NoseTaper = Mathf.Clamp(p.NoseTaper * S(8, 0.06f), 0.6f, 0.9f);
            p.ArchFlare *= S(9, 0.5f);
            p.Crown *= S(10, 0.4f);
            p.CabinFront += (R(11) - 0.5f) * 0.06f;
            p.RoofRear += (R(12) - 0.5f) * 0.08f;
            p.Spokes = 5 + Mathf.FloorToInt(R(13) * 2.999f);      // 5..7
            p.RimDish = 0.03f + R(14) * 0.05f;
            if (p.Spoiler == 1 && R(15) < 0.35f) p.Spoiler = 0;
            if (p.Spoiler == 2 && R(16) < 0.25f) p.Spoiler = 1;
            p.SharkFin = p.SharkFin || R(17) < 0.3f;
        }

        // ------------------------------------------------------------------ curves
        private static float Curve(float n, params (float n, float y)[] keys)
        {
            if (n <= keys[0].n) return keys[0].y;
            for (int i = 1; i < keys.Length; i++)
            {
                if (n <= keys[i].n)
                {
                    float t = Mathf.InverseLerp(keys[i - 1].n, keys[i].n, n);
                    return Mathf.Lerp(keys[i - 1].y, keys[i].y, t * t * (3f - 2f * t));
                }
            }
            return keys[keys.Length - 1].y;
        }

        private static float ShoulderY(Profile p, float n) => Curve(n,
            (-1f, p.TailY), (p.CabinRear, p.DeckY), (p.CabinRear + 0.12f, p.BeltY), (p.CabinFront - 0.08f, p.BeltY),
            (p.CabinFront, p.BonnetY), (0.9f, p.NoseY + 0.03f), (1f, p.NoseY));

        private static float HalfWidth(Profile p, float n) => p.Width * 0.5f * Curve(n,
            (-1f, p.TailTaper), (-0.85f, p.RearHaunch * 0.985f), (-0.45f, p.RearHaunch), (0.2f, 1f), (0.7f, 0.975f), (0.92f, 0.9f), (1f, p.NoseTaper));

        private static float SillY(Profile p, float n) => p.Sill + Mathf.Pow(Mathf.Abs(n), 7f) * 0.09f;

        private static float RoofY(Profile p, float n)
        {
            float belt = ShoulderY(p, n);
            return Curve(n, (p.CabinRear, belt + 0.015f), (p.RoofRear, p.RoofY), (p.RoofFront, p.RoofY), (p.CabinFront, belt + 0.015f));
        }

        // ------------------------------------------------------------------ body
        private const int SideCount = 15;              // lower body ring points per side
        private const int GlassSideCount = 7;          // greenhouse ring points per side

        /// <summary>Builds the body mesh (submesh 0 paint, 1 glass, 2 trim) and saves it as an asset.</summary>
        public static Mesh BuildBody(Profile p, string assetPath)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var paint = new List<int>();
            var glass = new List<int>();
            var trim = new List<int>();

            // ---- lower body loft
            const int stations = 110;
            int ring = SideCount * 2;
            var ringStart = new List<int>();
            var stationN = new List<float>();
            for (int i = 0; i <= stations; i++)
            {
                float n = i / (float)stations * 2f - 1f;
                stationN.Add(n);
                ringStart.Add(verts.Count);
                var side = LowerSection(p, n);
                for (int k = 0; k < side.Length; k++) verts.Add(side[k]);
                for (int k = side.Length - 1; k >= 0; k--) verts.Add(new Vector3(-side[k].x, side[k].y, side[k].z));
                for (int k = 0; k < ring; k++) uvs.Add(new Vector2(k / (float)(ring - 1), i / (float)stations));
            }
            for (int i = 0; i < stations; i++)
            {
                int a = ringStart[i], b = ringStart[i + 1];
                float nMid = (stationN[i] + stationN[i + 1]) * 0.5f;
                for (int k = 0; k < ring; k++)
                {
                    int j = (k + 1) % ring;
                    var centre = (verts[a + k] + verts[b + k] + verts[a + j] + verts[b + j]) * 0.25f;
                    bool underside = centre.y < SillY(p, nMid) + 0.015f;
                    bool bumperSkin = Mathf.Abs(nMid) > 0.88f && centre.y < p.Sill + 0.15f;
                    var list = underside || bumperSkin ? trim : paint;
                    Quad(list, a + k, b + k, a + j, b + j);
                }
            }
            Cap(verts, uvs, paint, ringStart[0], ring, false);
            Cap(verts, uvs, paint, ringStart[stations], ring, true);

            // ---- greenhouse loft
            const int glassStations = 44;
            int gRing = GlassSideCount * 2;
            var gStart = new List<int>();
            var gN = new List<float>();
            for (int i = 0; i <= glassStations; i++)
            {
                float n = Mathf.Lerp(p.CabinRear, p.CabinFront, i / (float)glassStations);
                gN.Add(n);
                gStart.Add(verts.Count);
                var side = GlassSection(p, n);
                for (int k = 0; k < side.Length; k++) verts.Add(side[k]);
                for (int k = side.Length - 1; k >= 0; k--) verts.Add(new Vector3(-side[k].x, side[k].y, side[k].z));
                for (int k = 0; k < gRing; k++) uvs.Add(new Vector2(k / (float)(gRing - 1), i / (float)glassStations));
            }
            for (int i = 0; i < glassStations; i++)
            {
                int a = gStart[i], b = gStart[i + 1];
                float nMid = (gN[i] + gN[i + 1]) * 0.5f;
                for (int k = 0; k < gRing; k++)
                {
                    int j = (k + 1) % gRing;
                    int side = k < GlassSideCount ? k : gRing - 1 - k; // 0 belt .. 6 roof centre (mirrored)
                    bool sideSegment = side <= 2;
                    bool roofSegment = side >= 3;
                    bool pillar = Mathf.Abs(nMid - p.CabinFront) < 0.05f || Mathf.Abs(nMid - p.BPillar) < 0.022f || Mathf.Abs(nMid - p.CabinRear) < 0.05f;
                    bool windscreen = nMid > p.RoofFront + 0.02f;
                    bool rearGlass = nMid < p.RoofRear - 0.02f && nMid > p.CabinRear + 0.06f;
                    bool isGlass = (sideSegment && !pillar && side >= 1) || (roofSegment && (windscreen || rearGlass));
                    Quad(isGlass ? glass : paint, a + k, b + k, a + j, b + j);
                }
            }

            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(assetPath) };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(paint, 0);
            mesh.SetTriangles(glass, 1);
            mesh.SetTriangles(trim, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, assetPath);
        }

        private static Mesh SaveMesh(Mesh mesh, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        private static void Quad(List<int> list, int a0, int b0, int a1, int b1)
        {
            list.Add(a0); list.Add(b0); list.Add(a1);
            list.Add(a1); list.Add(b0); list.Add(b1);
        }

        private static void Cap(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int ringStart, int ring, bool front)
        {
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < ring; i++) centre += verts[ringStart + i];
            centre /= ring;
            int c = verts.Count;
            verts.Add(centre);
            uvs.Add(new Vector2(0.5f, front ? 1f : 0f));
            for (int i = 0; i < ring; i++)
            {
                int j = (i + 1) % ring;
                if (front) { tris.Add(c); tris.Add(ringStart + i); tris.Add(ringStart + j); }
                else { tris.Add(c); tris.Add(ringStart + j); tris.Add(ringStart + i); }
            }
        }

        /// <summary>Right half of the lower body section at n, bottom centre first, top centre last.</summary>
        private static Vector3[] LowerSection(Profile p, float n)
        {
            float halfL = p.Length * 0.5f;
            float z = n * halfL;
            float hw = HalfWidth(p, n);
            float y0 = SillY(p, n);
            float yS = ShoulderY(p, n);
            float yT = yS + p.Crown * (1f - Mathf.Pow(Mathf.Abs(n), 4f) * 0.5f);
            float rb = Mathf.Min(p.BottomRadius, (yS - y0) * 0.3f);
            float rt = Mathf.Min(p.ShoulderRadius, (yS - y0) * 0.45f, hw * 0.5f);
            var pts = new Vector3[SideCount];
            int i = 0;
            pts[i++] = new Vector3(0f, y0, z);
            pts[i++] = new Vector3(hw * 0.55f, y0, z);
            // bottom corner: quarter arc, centre (hw - rb, y0 + rb)
            for (int k = 0; k < 4; k++)
            {
                float a = Mathf.Lerp(270f, 360f, k / 3f) * Mathf.Deg2Rad;
                pts[i++] = new Vector3(hw - rb + rb * Mathf.Cos(a), y0 + rb + rb * Mathf.Sin(a), z);
            }
            // flank
            pts[i++] = new Vector3(hw, Mathf.Lerp(y0 + rb, yS - rt, 0.35f), z);
            pts[i++] = new Vector3(hw, Mathf.Lerp(y0 + rb, yS - rt, 0.7f), z);
            // shoulder: quarter arc, centre (hw - rt, yS - rt)
            for (int k = 0; k < 4; k++)
            {
                float a = Mathf.Lerp(0f, 90f, k / 3f) * Mathf.Deg2Rad;
                pts[i++] = new Vector3(hw - rt + rt * Mathf.Cos(a), yS - rt + rt * Mathf.Sin(a), z);
            }
            // crowned top
            pts[i++] = new Vector3(hw * 0.62f, Mathf.Lerp(yS, yT, 0.5f), z);
            pts[i++] = new Vector3(hw * 0.28f, Mathf.Lerp(yS, yT, 0.88f), z);
            pts[i++] = new Vector3(0f, yT, z);

            // Wheel arches: vertices near a hub, outboard of the wheel's inner face, are lifted onto
            // the arch circle; a small flare pushes the surrounding panel outward.
            float archR = p.WheelRadius + p.ArchGap;
            float hubY = p.WheelRadius;
            float innerFace = p.Track * 0.5f - p.WheelWidth * 0.5f - 0.04f;
            float archTop = Mathf.Min(hubY + archR, yS - rt * 0.6f);
            foreach (float hubZ in new[] { p.Wheelbase * 0.5f, -p.Wheelbase * 0.5f })
            {
                float dz = z - hubZ;
                float flareBand = archR + 0.28f;
                if (Mathf.Abs(dz) > flareBand) continue;
                float flare = 1f + p.ArchFlare * Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dz) / flareBand), 1.5f);
                for (int k = 1; k < SideCount - 1; k++)
                {
                    var v = pts[k];
                    if (v.x < innerFace) continue;
                    if (Mathf.Abs(dz) < archR)
                    {
                        float archY = Mathf.Min(hubY + Mathf.Sqrt(archR * archR - dz * dz), archTop);
                        if (v.y < archY) v.y = archY;
                    }
                    if (v.x > hw - rt - 0.001f) v.x *= flare;
                    pts[k] = v;
                }
            }
            return pts;
        }

        /// <summary>Right half of the greenhouse section at n, belt first, roof centre last.</summary>
        private static Vector3[] GlassSection(Profile p, float n)
        {
            float halfL = p.Length * 0.5f;
            float z = n * halfL;
            float hw = HalfWidth(p, n);
            float baseY = ShoulderY(p, n) + 0.005f;
            float top = RoofY(p, n);
            float h = Mathf.Max(0.01f, top - baseY);
            float gb = hw - p.GlassInset;
            float gt = Mathf.Max(0.25f, gb - p.Tumblehome);
            float rc = Mathf.Min(p.RoofRadius, h * 0.6f, gt * 0.8f);
            var pts = new Vector3[GlassSideCount];
            pts[0] = new Vector3(gb, baseY, z);
            pts[1] = new Vector3(Mathf.Lerp(gb, gt, 0.3f), baseY + h * 0.36f, z);
            pts[2] = new Vector3(Mathf.Lerp(gb, gt, 0.7f), baseY + h * 0.7f, z);
            pts[3] = new Vector3(gt, top - rc, z);
            pts[4] = new Vector3(gt - rc * 0.3f, top - rc * 0.3f, z);
            pts[5] = new Vector3(gt - rc, top, z);
            pts[6] = new Vector3(0f, top, z);
            return pts;
        }

        // ------------------------------------------------------------------ wheels
        /// <summary>Lathed wheel: submesh 0 tyre, 1 rim. Axis is local X, rim face toward +X.</summary>
        public static Mesh BuildWheelMesh(Profile p, string assetPath)
        {
            float r = p.WheelRadius, w = p.WheelWidth;
            float rimR = r * 0.64f;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tyre = new List<int>();
            var rim = new List<int>();
            // Tyre profile (radius, axial) from inner bead around the tread to outer bead.
            var tyreProfile = new[]
            {
                new Vector2(rimR, -w * 0.5f), new Vector2(r * 0.9f, -w * 0.5f), new Vector2(r * 0.985f, -w * 0.42f), new Vector2(r, -w * 0.3f),
                new Vector2(r, w * 0.3f), new Vector2(r * 0.985f, w * 0.42f), new Vector2(r * 0.9f, w * 0.5f), new Vector2(rimR, w * 0.5f),
            };
            Lathe(verts, uvs, tyre, tyreProfile, 28, false);
            // Rim profile: outer lip, barrel dished inward, hub face.
            float face = w * 0.5f - 0.01f;
            var rimProfile = new[]
            {
                new Vector2(rimR + 0.005f, face), new Vector2(rimR * 0.97f, face - 0.01f), new Vector2(rimR * 0.9f, face - p.RimDish),
                new Vector2(rimR * 0.34f, face - p.RimDish - 0.03f), new Vector2(rimR * 0.3f, face - p.RimDish - 0.03f), new Vector2(0f, face - p.RimDish - 0.03f),
            };
            Lathe(verts, uvs, rim, rimProfile, 28, true);
            // Inner barrel (dark) closing the wheel from behind.
            var barrel = new[] { new Vector2(rimR, -w * 0.5f + 0.01f), new Vector2(rimR * 0.9f, -w * 0.45f), new Vector2(0f, -w * 0.45f) };
            Lathe(verts, uvs, tyre, barrel, 20, false);
            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(assetPath) };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(tyre, 0);
            mesh.SetTriangles(rim, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, assetPath);
        }

        /// <summary>Revolves a (radius, axial) profile around the X axis.</summary>
        private static void Lathe(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector2[] profile, int segments, bool flip)
        {
            int start = verts.Count;
            for (int i = 0; i < profile.Length; i++)
            {
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    verts.Add(new Vector3(profile[i].y, Mathf.Cos(a) * profile[i].x, Mathf.Sin(a) * profile[i].x));
                    uvs.Add(new Vector2(s / (float)segments, i / (float)(profile.Length - 1)));
                }
            }
            int row = segments + 1;
            for (int i = 0; i < profile.Length - 1; i++)
            for (int s = 0; s < segments; s++)
            {
                int a = start + i * row + s, b = a + row;
                if (flip) { tris.Add(a); tris.Add(b); tris.Add(a + 1); tris.Add(a + 1); tris.Add(b); tris.Add(b + 1); }
                else { tris.Add(a); tris.Add(a + 1); tris.Add(b); tris.Add(a + 1); tris.Add(b + 1); tris.Add(b); }
            }
        }

        /// <summary>Instances the wheel mesh under a hub pivot and adds spokes, disc and caliper.</summary>
        public static void BuildWheel(Transform pivot, Profile p, Mesh wheelMesh, Material tire, Material rim, Material trim, bool leftSide)
        {
            float r = p.WheelRadius, w = p.WheelWidth;
            var go = new GameObject("WheelMesh", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(pivot, false);
            // The lathe faces +X; left wheels look outward along -X.
            go.transform.localRotation = leftSide ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            go.GetComponent<MeshFilter>().sharedMesh = wheelMesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = new[] { tire, rim };
            float outward = leftSide ? -1f : 1f;
            float face = (w * 0.5f - 0.01f - p.RimDish) * outward;
            float rimR = r * 0.64f;
            for (int i = 0; i < p.Spokes; i++)
            {
                float angle = i * 360f / p.Spokes;
                var rot = Quaternion.Euler(angle, 0f, 0f);
                var spoke = Box(pivot, "Spoke" + i, rim, Vector3.zero, new Vector3(0.028f, rimR * 0.92f, rimR * 0.2f), rot);
                spoke.transform.localPosition = new Vector3(face, 0f, 0f) + rot * new Vector3(0f, rimR * 0.48f, 0f);
            }
            Cylinder(pivot, "BrakeDisc", trim, new Vector3(face - 0.03f * outward, 0f, 0f), rimR * 0.9f, 0.025f, Quaternion.Euler(0f, 0f, 90f));
            Box(pivot, "Caliper", rim, new Vector3(face - 0.035f * outward, rimR * 0.25f, -rimR * 0.62f), new Vector3(0.05f, rimR * 0.5f, rimR * 0.3f), Quaternion.identity);
            Cylinder(pivot, "HubCap", rim, new Vector3(face + 0.004f * outward, 0f, 0f), rimR * 0.2f, 0.012f, Quaternion.Euler(0f, 0f, 90f));
        }

        // ------------------------------------------------------------------ details
        public static void AddDetails(Transform body, Profile p, Material trim, Material lightFront, Material lightRear, Material glass, Material paint)
        {
            float halfL = p.Length * 0.5f;
            float hwFront = HalfWidth(p, 0.95f), hwRear = HalfWidth(p, -0.95f), hwMid = HalfWidth(p, 0f);
            float noseY = p.NoseY, tailY = p.TailY;
            var chrome = MaterialFactory.Opaque("Car_Chrome", new Color(0.85f, 0.86f, 0.88f), 1f, 0.92f);
            var plate = MaterialFactory.Opaque("Car_Plate", new Color(0.92f, 0.92f, 0.9f), 0f, 0.4f);
            var amber = MaterialFactory.Emissive("Car_Indicator", new Color(0.9f, 0.5f, 0.1f), new Color(1.2f, 0.6f, 0.1f));
            var cabin = MaterialFactory.Opaque("Car_Interior", new Color(0.03f, 0.03f, 0.035f), 0f, 0.08f);

            // ---- front: headlight clusters (housing + lens + DRL), grille, intake, plate, splitter
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * hwFront * 0.62f;
                var rot = Quaternion.Euler(0f, s * 18f, s * -8f);
                Box(body, "HeadHousing", trim, new Vector3(x, noseY - 0.1f, halfL - 0.02f), new Vector3(0.42f, 0.13f, 0.1f), rot);
                Box(body, "HeadLens", lightFront, new Vector3(x, noseY - 0.1f, halfL + 0.02f), new Vector3(0.36f, 0.08f, 0.04f), rot);
                Box(body, "Drl", lightFront, new Vector3(x, noseY - 0.17f, halfL + 0.02f), new Vector3(0.4f, 0.016f, 0.03f), rot);
                Box(body, "Indicator", amber, new Vector3(s * hwFront * 0.85f, noseY - 0.13f, halfL - 0.06f), new Vector3(0.08f, 0.05f, 0.06f), rot);
            }
            Box(body, "Grille", trim, new Vector3(0f, noseY - 0.13f, halfL + 0.005f), new Vector3(hwFront * 0.7f, 0.12f, 0.03f), Quaternion.identity);
            for (int i = 0; i < 3; i++)
                Box(body, "GrilleBar" + i, chrome, new Vector3(0f, noseY - 0.085f - i * 0.035f, halfL + 0.02f), new Vector3(hwFront * 0.66f, 0.008f, 0.02f), Quaternion.identity);
            Box(body, "Intake", trim, new Vector3(0f, p.Sill + 0.14f, halfL - 0.04f), new Vector3(hwFront * 1.1f, 0.16f, 0.12f), Quaternion.identity);
            Box(body, "PlateFront", plate, new Vector3(0f, p.Sill + 0.27f, halfL + 0.03f), new Vector3(0.44f, 0.11f, 0.01f), Quaternion.identity);
            if (p.Splitter) Box(body, "Splitter", trim, new Vector3(0f, p.Sill - 0.02f, halfL - 0.1f), new Vector3(hwFront * 2.05f, 0.025f, 0.24f), Quaternion.identity);

            // ---- rear: light bar, lamp blocks, plate, diffuser, exhausts
            Box(body, "TailBar", lightRear, new Vector3(0f, tailY - 0.09f, -halfL - 0.01f), new Vector3(hwRear * 1.7f, 0.035f, 0.04f), Quaternion.identity);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(body, "TailLamp", lightRear, new Vector3(s * hwRear * 0.7f, tailY - 0.14f, -halfL - 0.01f), new Vector3(0.36f, 0.1f, 0.05f), Quaternion.Euler(0f, s * -14f, 0f));
                Box(body, "TailHousing", trim, new Vector3(s * hwRear * 0.7f, tailY - 0.14f, -halfL + 0.03f), new Vector3(0.4f, 0.13f, 0.06f), Quaternion.Euler(0f, s * -14f, 0f));
            }
            Box(body, "PlateRear", plate, new Vector3(0f, p.Sill + 0.34f, -halfL - 0.02f), new Vector3(0.44f, 0.11f, 0.01f), Quaternion.identity);
            Box(body, "Diffuser", trim, new Vector3(0f, p.Sill + 0.09f, -halfL + 0.05f), new Vector3(hwRear * 1.5f, 0.18f, 0.16f), Quaternion.identity);
            for (int i = -2; i <= 2; i++)
                Box(body, "Fin", trim, new Vector3(i * hwRear * 0.32f, p.Sill + 0.02f, -halfL + 0.12f), new Vector3(0.015f, 0.12f, 0.3f), Quaternion.identity);
            float exSpacing = p.ExhaustCount > 2 ? 0.12f : 0.3f;
            for (int i = 0; i < p.ExhaustCount; i++)
            {
                float x = p.ExhaustCount == 1 ? hwRear * 0.5f : (i < p.ExhaustCount / 2 ? -1f : 1f) * (hwRear * 0.45f + (i % 2) * exSpacing);
                Cylinder(body, "Exhaust" + i, chrome, new Vector3(x, p.Sill + 0.12f, -halfL - 0.03f), 0.045f, 0.1f, Quaternion.Euler(90f, 0f, 0f));
            }

            // ---- sides: mirrors, door seams, handles, skirts
            float mirrorN = p.CabinFront - 0.05f;
            float mirrorZ = mirrorN * halfL;
            float mirrorY = ShoulderY(p, mirrorN);
            float mirrorHw = HalfWidth(p, mirrorN);
            float beltY = p.BeltY;
            float doorFrontZ = (p.CabinFront - 0.1f) * halfL, doorSplitZ = p.BPillar * halfL, doorRearZ = (p.CabinRear + 0.14f) * halfL;
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (hwMid + 0.01f);
                float mirrorX = mirrorHw - p.GlassInset * 0.5f;
                Box(body, "MirrorStalk", paint, new Vector3(s * (mirrorX + 0.04f), mirrorY + 0.04f, mirrorZ), new Vector3(0.12f, 0.045f, 0.06f), Quaternion.identity);
                Box(body, "Mirror", paint, new Vector3(s * (mirrorX + 0.1f), mirrorY + 0.075f, mirrorZ), new Vector3(0.16f, 0.09f, 0.11f), Quaternion.Euler(0f, s * 6f, 0f));
                Box(body, "MirrorGlass", glass, new Vector3(s * (mirrorX + 0.1f), mirrorY + 0.075f, mirrorZ - 0.06f), new Vector3(0.13f, 0.07f, 0.01f), Quaternion.identity);
                float seamTop = beltY - 0.05f, seamBottom = p.Sill + 0.12f;
                Box(body, "SeamFront", trim, new Vector3(x, (seamTop + seamBottom) * 0.5f, doorFrontZ), new Vector3(0.012f, seamTop - seamBottom, 0.012f), Quaternion.identity);
                Box(body, "SeamSplit", trim, new Vector3(x, (seamTop + seamBottom) * 0.5f, doorSplitZ), new Vector3(0.012f, seamTop - seamBottom, 0.012f), Quaternion.identity);
                if (p.Spoiler != 2 || p.CabinRear < -0.8f)
                    Box(body, "SeamRear", trim, new Vector3(x, (seamTop + seamBottom) * 0.5f, doorRearZ), new Vector3(0.012f, seamTop - seamBottom, 0.012f), Quaternion.identity);
                Box(body, "Handle", chrome, new Vector3(x + s * 0.008f, beltY - 0.14f, doorSplitZ + 0.18f), new Vector3(0.02f, 0.03f, 0.14f), Quaternion.identity);
                float skirtLen = Mathf.Max(0.5f, p.Wheelbase - p.WheelRadius * 2f - 0.5f);
                Box(body, "Skirt", trim, new Vector3(s * (hwMid - 0.02f), p.Sill + 0.03f, 0f), new Vector3(0.06f, 0.08f, skirtLen), Quaternion.identity);
            }

            // ---- interior: dark cabin block, dash, seats, steering wheel
            float cabinRearZ = (p.CabinRear + 0.03f) * halfL, cabinFrontZ = (p.CabinFront - 0.03f) * halfL;
            float cabinHalfW = Mathf.Max(0.3f, hwMid - p.GlassInset - 0.08f);
            float cabinTop = beltY - 0.02f;
            Box(body, "Interior", cabin, new Vector3(0f, (p.Sill + 0.02f + cabinTop) * 0.5f, (cabinRearZ + cabinFrontZ) * 0.5f),
                new Vector3(cabinHalfW * 2f, cabinTop - p.Sill - 0.02f, cabinFrontZ - cabinRearZ), Quaternion.identity);
            Box(body, "Dash", cabin, new Vector3(0f, cabinTop - 0.05f, cabinFrontZ - 0.3f), new Vector3(cabinHalfW * 1.9f, 0.1f, 0.5f), Quaternion.identity);
            float seatZ = Mathf.Lerp(p.RoofRear, p.RoofFront, 0.3f) * halfL;
            float seatH = Mathf.Min(0.24f, p.RoofY - beltY - 0.14f);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(body, "Seat", cabin, new Vector3(s * cabinHalfW * 0.45f, cabinTop + seatH * 0.5f - 0.04f, seatZ), new Vector3(0.4f, seatH - 0.08f, 0.1f), Quaternion.Euler(-10f, 0f, 0f));
                Box(body, "HeadRest", cabin, new Vector3(s * cabinHalfW * 0.45f, cabinTop + seatH - 0.02f, seatZ - 0.02f), new Vector3(0.2f, 0.1f, 0.08f), Quaternion.Euler(-10f, 0f, 0f));
            }
            Box(body, "SteeringWheel", cabin, new Vector3(-cabinHalfW * 0.45f, cabinTop + 0.08f, cabinFrontZ - 0.55f), new Vector3(0.34f, 0.34f, 0.03f), Quaternion.Euler(-25f, 0f, 0f));

            // ---- wheel-well liners
            float linerX = p.Track * 0.5f - p.WheelWidth * 0.5f - 0.22f;
            foreach (float zSign in new[] { -1f, 1f })
            {
                float axleN = zSign * p.Wheelbase * 0.5f / halfL;
                float linerH = Mathf.Min(p.WheelRadius * 2f + p.ArchGap, ShoulderY(p, axleN) - 0.08f) - 0.03f;
                foreach (float xSign in new[] { -1f, 1f })
                    Box(body, "WellLiner", trim, new Vector3(xSign * linerX, linerH * 0.5f + 0.02f, zSign * p.Wheelbase * 0.5f),
                        new Vector3(0.44f, linerH, p.WheelRadius * 2f + 0.2f), Quaternion.identity);
            }

            // ---- roof and rear aero
            if (p.SharkFin)
                Box(body, "SharkFin", paint, new Vector3(0f, p.RoofY + 0.03f, (p.RoofRear + 0.08f) * halfL), new Vector3(0.06f, 0.07f, 0.24f), Quaternion.Euler(-20f, 0f, 0f));
            if (p.Spoiler == 1)
            {
                bool hatch = p.CabinRear < -0.9f;
                Box(body, "LipSpoiler", paint, new Vector3(0f, (hatch ? p.RoofY : tailY) + 0.03f, (hatch ? p.CabinRear + 0.06f : -0.96f) * halfL),
                    new Vector3(hwRear * 1.6f, 0.035f, 0.2f), Quaternion.Euler(-6f, 0f, 0f));
            }
            if (p.Spoiler == 2)
            {
                float wingY = tailY + 0.28f;
                float wingZ = -halfL + 0.2f;
                for (int s = -1; s <= 1; s += 2)
                {
                    Box(body, "WingPost", trim, new Vector3(s * hwRear * 0.55f, (wingY + tailY) * 0.5f, wingZ + 0.02f), new Vector3(0.05f, wingY - tailY, 0.14f), Quaternion.Euler(-12f, 0f, 0f));
                    Box(body, "WingPlate", paint, new Vector3(s * hwRear * 0.98f, wingY + 0.02f, wingZ), new Vector3(0.02f, 0.14f, 0.34f), Quaternion.identity);
                }
                Box(body, "Wing", paint, new Vector3(0f, wingY, wingZ), new Vector3(hwRear * 1.96f, 0.035f, 0.3f), Quaternion.Euler(-9f, 0f, 0f));
            }
        }

        private static GameObject Box(Transform parent, string name, Material material, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject Cylinder(Transform parent, string name, Material material, Vector3 center, float radius, float length, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation;
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
