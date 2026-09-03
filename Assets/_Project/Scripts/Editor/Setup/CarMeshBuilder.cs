using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Lofts a stylised car body from per-class silhouette curves: a closed hull with sills, wheel
    /// arches, shoulder line, greenhouse and roof, split into paint and glass sub-meshes with smooth
    /// normals. Roughly 4k triangles per body, mobile friendly. Real models replace the prefab;
    /// this exists so the game looks like cars, not boxes, before art arrives.
    /// </summary>
    public static class CarMeshBuilder
    {
        public sealed class Profile
        {
            public float Length = 4.3f;
            public float Width = 1.8f;
            public float SillHeight = 0.28f;      // body bottom above ground
            public float ShoulderHeight = 0.8f;   // top of the doors
            public float RoofHeight = 1.36f;
            public float CabinStart = -0.62f;     // normalized z where the windscreen base begins (-1 rear .. +1 front)
            public float CabinEnd = 0.12f;        // where the windscreen top (roof front) is
            public float RoofFront = 0.05f;       // roof extends to here before dropping into the windscreen
            public float RearGlassStart = -0.95f; // rear window base
            public float RoofRear = -0.55f;
            public float NoseDrop = 0.22f;        // how far the bonnet falls toward the nose
            public float TailDrop = 0.1f;
            public float GlassInset = 0.09f;      // greenhouse narrower than the body
            public float WheelRadius = 0.33f;
            public float WheelWidth = 0.24f;
            public float Wheelbase = 2.6f;
            public float Track = 1.56f;
            public bool Spoiler;
            public bool Splitter;
        }

        public static Profile ProfileFor(PlaceholderCarBuilder.Shape shape, Vehicles.VehicleClass cls)
        {
            var p = new Profile
            {
                Length = shape.Length, Width = shape.Width, WheelRadius = shape.WheelRadius, WheelWidth = shape.WheelWidth,
                Wheelbase = shape.Wheelbase, Track = shape.Track
            };
            // Shoulder lines sit just above the wheel tops so the arches read as arches, not as
            // tyres poking through a tray.
            switch (cls)
            {
                case Vehicles.VehicleClass.Sport:
                    p.SillHeight = 0.24f; p.ShoulderHeight = 0.8f; p.RoofHeight = 1.26f; p.CabinStart = -0.7f; p.CabinEnd = 0.05f;
                    p.RoofFront = -0.05f; p.RoofRear = -0.6f; p.RearGlassStart = -0.98f; p.NoseDrop = 0.3f; p.TailDrop = 0.1f; p.Spoiler = true;
                    break;
                case Vehicles.VehicleClass.Super:
                    p.SillHeight = 0.2f; p.ShoulderHeight = 0.78f; p.RoofHeight = 1.16f; p.CabinStart = -0.75f; p.CabinEnd = -0.1f;
                    p.RoofFront = -0.18f; p.RoofRear = -0.62f; p.RearGlassStart = -0.95f; p.NoseDrop = 0.36f; p.TailDrop = 0.06f; p.Spoiler = true; p.Splitter = true;
                    break;
                case Vehicles.VehicleClass.Hyper:
                    p.SillHeight = 0.18f; p.ShoulderHeight = 0.78f; p.RoofHeight = 1.1f; p.CabinStart = -0.78f; p.CabinEnd = -0.15f;
                    p.RoofFront = -0.22f; p.RoofRear = -0.65f; p.RearGlassStart = -0.96f; p.NoseDrop = 0.4f; p.TailDrop = 0.04f; p.Spoiler = true; p.Splitter = true;
                    break;
                default: // Street hatch
                    p.SillHeight = 0.3f; p.ShoulderHeight = 0.84f; p.RoofHeight = 1.44f; p.CabinStart = -0.55f; p.CabinEnd = 0.15f;
                    p.RoofFront = 0.05f; p.RoofRear = -0.72f; p.RearGlassStart = -0.98f; p.NoseDrop = 0.22f; p.TailDrop = 0.06f;
                    break;
            }
            return p;
        }

        private struct Station
        {
            public float Z;
            public float HalfWidth;
            public float Sill;
            public float Shoulder;
            public float Roof;
            public float GlassHalfWidth;
            public float Arch;        // 0..1 how much of a wheel arch cuts into this ring
            public float ArchTop;     // height the arch lifts the lower body to
            public bool Glass;        // greenhouse rings between shoulder and roof are glass here
            public bool RoofGlass;    // the roof segment slopes here (windscreen / rear glass) so it is glass too
        }

        /// <summary>Builds the body mesh (submesh 0 paint, submesh 1 glass) and saves it as an asset.</summary>
        public static Mesh BuildBody(Profile p, string assetPath)
        {
            var stations = new List<Station>();
            const int count = 60;
            float halfL = p.Length * 0.5f;
            float archTop = Mathf.Min(p.WheelRadius * 2f + 0.07f, p.ShoulderHeight - 0.06f);
            float archHalfWidth = (p.WheelRadius + 0.14f) / halfL;   // in normalized z
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;          // 0 rear .. 1 front
                float n = t * 2f - 1f;               // -1 .. 1
                float z = n * halfL;
                // Plan view: rounded rectangle, narrower at the very ends, flared over the wheels.
                float endTaper = 1f - Mathf.Pow(Mathf.Abs(n), 6f) * 0.2f;
                float axle = p.Wheelbase * 0.5f / halfL;
                float archFront = Mathf.Exp(-Mathf.Pow((n - axle) / archHalfWidth, 2f) * 1.6f);
                float archRear = Mathf.Exp(-Mathf.Pow((n + axle) / archHalfWidth, 2f) * 1.6f);
                float archK = Mathf.Max(archFront, archRear);
                float arch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.85f, archK));
                float flare = 1f + 0.07f * archK;
                float hw = Mathf.Max(p.Track * 0.5f + p.WheelWidth * 0.5f + 0.03f, p.Width * 0.5f) * endTaper * flare;
                // Bonnet/boot fall away toward the ends.
                float front = Mathf.Clamp01((n - p.CabinEnd) / Mathf.Max(0.01f, 1f - p.CabinEnd));
                float rear = Mathf.Clamp01((p.RearGlassStart - n) / Mathf.Max(0.01f, p.RearGlassStart + 1f));
                float shoulder = p.ShoulderHeight - Smooth(front) * p.NoseDrop - Smooth(rear) * p.TailDrop;
                float sill = p.SillHeight + Mathf.Pow(Mathf.Abs(n), 8f) * 0.08f;

                // Roof profile: rises from the cabin start to the roof, flat, drops into the rear glass.
                float roof;
                bool glass;
                if (n >= p.CabinEnd) { roof = shoulder; glass = false; }
                else if (n >= p.RoofFront) { float k = Smooth(Mathf.InverseLerp(p.CabinEnd, p.RoofFront, n)); roof = Mathf.Lerp(shoulder, p.RoofHeight, k); glass = true; }
                else if (n >= p.RoofRear) { roof = p.RoofHeight; glass = true; }
                else if (n >= p.RearGlassStart) { float k = Smooth(Mathf.InverseLerp(p.RoofRear, p.RearGlassStart, n)); roof = Mathf.Lerp(p.RoofHeight, shoulder + 0.02f, k); glass = true; }
                else { roof = shoulder; glass = false; }
                // Windscreen base is where glass begins (CabinStart); before that the bonnet is paint.
                if (n > p.CabinStart && n < p.CabinEnd) glass = true;

                stations.Add(new Station
                {
                    Z = z, HalfWidth = hw, Sill = sill, Shoulder = shoulder, Roof = roof,
                    GlassHalfWidth = Mathf.Max(0.2f, hw - p.GlassInset - (roof - shoulder) * 0.35f),
                    Arch = arch, ArchTop = Mathf.Min(archTop, shoulder - 0.05f),
                    Glass = glass && roof > shoulder + 0.03f,
                    RoofGlass = glass && roof > shoulder + 0.03f && roof < p.RoofHeight - 0.015f
                });
            }

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var paintTris = new List<int>();
            var glassTris = new List<int>();
            var ringIndex = new List<int>(); // start vertex of each station ring
            const int ringPoints = 20; // symmetric: 10 per side (floor centre and roof centre shared)
            foreach (var s in stations)
            {
                ringIndex.Add(verts.Count);
                var side = RingSide(s);
                // Right side bottom->top, then left side top->bottom (closed loop).
                for (int i = 0; i < side.Length; i++) verts.Add(new Vector3(side[i].x, side[i].y, s.Z));
                for (int i = side.Length - 1; i >= 0; i--) verts.Add(new Vector3(-side[i].x, side[i].y, s.Z));
                for (int i = 0; i < ringPoints; i++) uvs.Add(new Vector2(i / (float)(ringPoints - 1), (s.Z / p.Length) + 0.5f));
            }

            for (int si = 0; si < stations.Count - 1; si++)
            {
                int a = ringIndex[si], b = ringIndex[si + 1];
                bool glassRing = stations[si].Glass && stations[si + 1].Glass;
                bool roofGlass = stations[si].RoofGlass && stations[si + 1].RoofGlass;
                for (int i = 0; i < ringPoints; i++)
                {
                    int j = (i + 1) % ringPoints;
                    bool isGlassSegment = (glassRing && IsGreenhouseSegment(i)) || (roofGlass && IsRoofSegment(i));
                    var list = isGlassSegment ? glassTris : paintTris;
                    list.Add(a + i); list.Add(b + i); list.Add(a + j);
                    list.Add(a + j); list.Add(b + i); list.Add(b + j);
                }
            }
            // End caps (fan around a centre vertex).
            AddCap(verts, uvs, paintTris, ringIndex[0], ringPoints, stations[0].Z, false);
            AddCap(verts, uvs, paintTris, ringIndex[stations.Count - 1], ringPoints, stations[stations.Count - 1].Z, true);

            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(assetPath) };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(paintTris, 0);
            mesh.SetTriangles(glassTris, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        private static bool IsGreenhouseSegment(int i)
        {
            // Ring order: 0 floor-centre, 1 floor-edge, 2 sill-low, 3 sill-high, 4 door, 5 shoulder, 6 belt,
            // 7 glass-base, 8 glass-top, 9 roof-centre, then 10..19 mirrored (10 roof-centre ... 19 floor-centre).
            // Side windows span belt->glass-top on both sides; the roof panel itself is paint unless it slopes.
            return i == 6 || i == 7 || i == 11 || i == 12;
        }

        private static bool IsRoofSegment(int i) => i == 8 || i == 10;

        private static Vector2[] RingSide(Station s)
        {
            float hw = s.HalfWidth;
            float belt = s.Shoulder + (s.Roof - s.Shoulder) * 0.06f;
            float glassTopHw = Mathf.Lerp(s.GlassHalfWidth, 0.25f, 0.55f);
            // Wheel arches: the lower outboard points lift toward ArchTop where a wheel sits, so the
            // body wraps over the tyre instead of passing through it.
            float Lift(float y) => Mathf.Lerp(y, Mathf.Max(y, s.ArchTop), s.Arch);
            float door = Mathf.Lerp(s.Sill, s.Shoulder, 0.6f);
            return new[]
            {
                new Vector2(0f, s.Sill),                                   // 0 floor centre
                new Vector2(hw * 0.78f, s.Sill),                           // 1 floor edge (stays low: the arch wall hides behind the tyre)
                new Vector2(hw * 0.95f, Lift(s.Sill + 0.06f)),             // 2 sill low
                new Vector2(hw * 0.995f, Lift(s.Sill + 0.2f)),             // 3 sill high
                new Vector2(hw, Lift(door)),                               // 4 door
                new Vector2(hw * 0.99f, s.Shoulder - 0.04f),               // 5 shoulder
                new Vector2(hw * 0.955f, belt),                            // 6 belt
                new Vector2(s.GlassHalfWidth, belt + 0.02f),               // 7 glass base
                new Vector2(glassTopHw, s.Roof - 0.03f),                   // 8 glass top
                new Vector2(0f, s.Roof),                                   // 9 roof centre
            };
        }

        private static void AddCap(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int ringStart, int ringPoints, float z, bool front)
        {
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < ringPoints; i++) centre += verts[ringStart + i];
            centre /= ringPoints;
            int c = verts.Count;
            verts.Add(centre);
            uvs.Add(new Vector2(0.5f, front ? 1f : 0f));
            for (int i = 0; i < ringPoints; i++)
            {
                int j = (i + 1) % ringPoints;
                if (front) { tris.Add(c); tris.Add(ringStart + i); tris.Add(ringStart + j); }
                else { tris.Add(c); tris.Add(ringStart + j); tris.Add(ringStart + i); }
            }
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        // ------------------------------------------------------------------ details
        public static void AddDetails(Transform body, Profile p, Material trim, Material lightFront, Material lightRear, Material glass, Material paint)
        {
            float halfL = p.Length * 0.5f;
            float noseY = p.ShoulderHeight - p.NoseDrop;
            float tailY = p.ShoulderHeight - p.TailDrop;
            // Headlights: slim angled strips on the nose.
            Box(body, "HeadlightL", lightFront, new Vector3(-p.Width * 0.3f, noseY - 0.1f, halfL - 0.03f), new Vector3(0.34f, 0.08f, 0.06f), Quaternion.Euler(0f, -12f, 0f));
            Box(body, "HeadlightR", lightFront, new Vector3(p.Width * 0.3f, noseY - 0.1f, halfL - 0.03f), new Vector3(0.34f, 0.08f, 0.06f), Quaternion.Euler(0f, 12f, 0f));
            // Grille and splitter
            Box(body, "Grille", trim, new Vector3(0f, p.SillHeight + 0.2f, halfL - 0.01f), new Vector3(p.Width * 0.5f, 0.16f, 0.05f), Quaternion.identity);
            if (p.Splitter) Box(body, "Splitter", trim, new Vector3(0f, p.SillHeight - 0.04f, halfL - 0.1f), new Vector3(p.Width * 1.02f, 0.04f, 0.35f), Quaternion.identity);
            // Tail light bar
            Box(body, "Taillights", lightRear, new Vector3(0f, tailY - 0.08f, -halfL + 0.02f), new Vector3(p.Width * 0.78f, 0.07f, 0.05f), Quaternion.identity);
            Box(body, "Diffuser", trim, new Vector3(0f, p.SillHeight + 0.08f, -halfL + 0.05f), new Vector3(p.Width * 0.7f, 0.16f, 0.12f), Quaternion.identity);
            // Exhaust tips
            Cylinder(body, "ExhaustL", trim, new Vector3(-p.Width * 0.22f, p.SillHeight + 0.1f, -halfL - 0.04f), 0.05f, 0.08f, Quaternion.Euler(90f, 0f, 0f));
            Cylinder(body, "ExhaustR", trim, new Vector3(p.Width * 0.22f, p.SillHeight + 0.1f, -halfL - 0.04f), 0.05f, 0.08f, Quaternion.Euler(90f, 0f, 0f));
            // Mirrors at the windscreen base
            float mirrorZ = p.CabinStart * halfL + 0.15f;
            float mirrorY = p.ShoulderHeight + 0.1f;
            Box(body, "MirrorL", paint, new Vector3(-p.Width * 0.5f - 0.1f, mirrorY, mirrorZ), new Vector3(0.2f, 0.09f, 0.14f), Quaternion.identity);
            Box(body, "MirrorR", paint, new Vector3(p.Width * 0.5f + 0.1f, mirrorY, mirrorZ), new Vector3(0.2f, 0.09f, 0.14f), Quaternion.identity);
            // Side skirts between the arches
            float skirtLength = Mathf.Max(0.6f, p.Wheelbase - p.WheelRadius * 2f - 0.5f);
            Box(body, "SkirtL", trim, new Vector3(-p.Width * 0.49f, p.SillHeight + 0.02f, 0f), new Vector3(0.05f, 0.08f, skirtLength), Quaternion.identity);
            Box(body, "SkirtR", trim, new Vector3(p.Width * 0.49f, p.SillHeight + 0.02f, 0f), new Vector3(0.05f, 0.08f, skirtLength), Quaternion.identity);
            // Rear spoiler
            if (p.Spoiler)
            {
                float wingY = tailY + (p.RoofHeight - tailY) * 0.35f + 0.08f;
                float wingZ = -halfL + 0.25f;
                Box(body, "WingPostL", trim, new Vector3(-p.Width * 0.3f, (wingY + tailY) * 0.5f, wingZ), new Vector3(0.05f, wingY - tailY, 0.12f), Quaternion.identity);
                Box(body, "WingPostR", trim, new Vector3(p.Width * 0.3f, (wingY + tailY) * 0.5f, wingZ), new Vector3(0.05f, wingY - tailY, 0.12f), Quaternion.identity);
                Box(body, "Wing", paint, new Vector3(0f, wingY, wingZ), new Vector3(p.Width * 0.9f, 0.04f, 0.3f), Quaternion.Euler(-8f, 0f, 0f));
            }
        }

        public static void BuildWheel(Transform pivot, Profile p, Material tire, Material rim, Material trim)
        {
            float r = p.WheelRadius, w = p.WheelWidth;
            Cylinder(pivot, "Tyre", tire, Vector3.zero, r, w, Quaternion.Euler(0f, 0f, 90f));
            var disc = Cylinder(pivot, "RimLip", rim, Vector3.zero, r * 0.62f, w * 1.04f, Quaternion.Euler(0f, 0f, 90f));
            Cylinder(pivot, "Hub", rim, Vector3.zero, r * 0.16f, w * 1.1f, Quaternion.Euler(0f, 0f, 90f));
            // Brake disc and caliper visible through the spokes (rim face is offset outward).
            Cylinder(pivot, "BrakeDisc", trim, Vector3.zero, r * 0.55f, w * 0.3f, Quaternion.Euler(0f, 0f, 90f));
            // Spokes: five thin bars on each face.
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                var spoke = Box(pivot, "Spoke" + i, rim, Vector3.zero, new Vector3(w * 1.06f, r * 1.1f, r * 0.14f), Quaternion.Euler(angle, 0f, 0f));
                spoke.transform.localPosition = Vector3.zero;
            }
            // Cut the solid rim disc down to a ring so spokes read: shrink it into a lip.
            disc.transform.localScale = new Vector3(r * 1.24f, w * 0.52f, r * 1.24f);
            Cylinder(pivot, "RimInner", trim, Vector3.zero, r * 0.6f, w * 0.9f, Quaternion.Euler(0f, 0f, 90f));
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
