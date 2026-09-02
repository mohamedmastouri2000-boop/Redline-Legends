using System.Collections.Generic;
using RedlineLegends.Core;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Themed environment props built from primitives: buildings, dunes, rocks, containers,
    /// grandstands, lamps. Every prop is checked against the whole track so nothing lands on the
    /// road. Placeholder art; real environment kits replace these by theme.
    /// </summary>
    public static class TrackDressing
    {
        public static void Dress(CircuitSpec spec, List<TrackMeshBuilder.Sample> samples, Transform root, Bounds bounds)
        {
            var rng = new System.Random(spec.Id.GetHashCode());
            var parent = new GameObject("Dressing");
            parent.transform.SetParent(root, false);
            string prefix = spec.SceneName.Replace("Track_", "");

            switch (spec.Dressing)
            {
                case "coast":
                    Sea(parent.transform, bounds, prefix);
                    Scatter(parent.transform, samples, rng, 70, 40f, 160f, 6f, 20f, 0.6f, MaterialFactory.Opaque(prefix + "_Rock", new Color(0.45f, 0.4f, 0.36f), 0f, 0.35f), true, "Rock");
                    break;
                case "city":
                    Buildings(parent.transform, samples, rng, prefix, 140, 14f, 60f, 12f, 48f, new Color(0.55f, 0.58f, 0.62f), false);
                    break;
                case "night":
                    Buildings(parent.transform, samples, rng, prefix, 160, 14f, 60f, 16f, 70f, new Color(0.12f, 0.13f, 0.18f), true);
                    Lamps(parent.transform, samples, prefix, 45f, new Color(3.5f, 2.6f, 1.6f), 4);
                    break;
                case "desert":
                    Scatter(parent.transform, samples, rng, 90, 30f, 220f, 12f, 40f, 0.35f, MaterialFactory.Opaque(prefix + "_Dune", new Color(0.82f, 0.7f, 0.46f), 0f, 0.15f), false, "Dune");
                    Scatter(parent.transform, samples, rng, 40, 20f, 120f, 3f, 9f, 0.7f, MaterialFactory.Opaque(prefix + "_Rock", new Color(0.5f, 0.42f, 0.35f), 0f, 0.3f), true, "Rock");
                    break;
                case "mountain":
                    Scatter(parent.transform, samples, rng, 120, 25f, 200f, 10f, 45f, 0.9f, MaterialFactory.Opaque(prefix + "_Rock", new Color(0.4f, 0.4f, 0.42f), 0f, 0.3f), true, "Rock");
                    Trees(parent.transform, samples, rng, prefix, 220, 12f, 120f);
                    break;
                case "industrial":
                    Containers(parent.transform, samples, rng, prefix, 120);
                    Buildings(parent.transform, samples, rng, prefix, 30, 20f, 90f, 8f, 16f, new Color(0.45f, 0.42f, 0.4f), false);
                    break;
                case "highway":
                    Lamps(parent.transform, samples, prefix, 60f, new Color(2f, 2f, 1.8f), 0);
                    Scatter(parent.transform, samples, rng, 60, 40f, 200f, 8f, 26f, 0.4f, MaterialFactory.Opaque(prefix + "_Hill", new Color(0.42f, 0.44f, 0.3f), 0f, 0.15f), false, "Hill");
                    Billboards(parent.transform, samples, rng, prefix, 12);
                    break;
                default: // circuit
                    Grandstands(parent.transform, samples, prefix, 6);
                    TyreStacks(parent.transform, samples, rng, prefix, 80);
                    break;
            }
        }

        /// <summary>True when a point is at least clearance metres from every road sample.</summary>
        private static bool IsClear(List<TrackMeshBuilder.Sample> samples, Vector3 p, float clearance)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                float dx = samples[i].Position.x - p.x, dz = samples[i].Position.z - p.z;
                float need = samples[i].HalfWidth + clearance;
                if (dx * dx + dz * dz < need * need) return false;
            }
            return true;
        }

        private static GameObject Prop(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            go.layer = GameLayers.Track;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Scatter(Transform parent, List<TrackMeshBuilder.Sample> samples, System.Random rng, int count, float minOffset,
            float maxOffset, float minSize, float maxSize, float heightRatio, Material mat, bool collider, string name)
        {
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 6)
            {
                attempts++;
                var s = samples[rng.Next(samples.Count)];
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float offset = minOffset + (float)rng.NextDouble() * (maxOffset - minOffset);
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + offset);
                float size = minSize + (float)rng.NextDouble() * (maxSize - minSize);
                if (!IsClear(samples, pos, size * 0.6f + 8f)) continue;
                pos.y = Mathf.Max(0f, s.Position.y) + size * heightRatio * 0.35f - 0.5f;
                Prop(parent, name + placed, PrimitiveType.Sphere, pos, new Vector3(size, size * heightRatio, size),
                    Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), mat, collider);
                placed++;
            }
        }

        private static void Buildings(Transform parent, List<TrackMeshBuilder.Sample> samples, System.Random rng, string prefix, int count,
            float minOffset, float maxOffset, float minHeight, float maxHeight, Color color, bool lit)
        {
            var mats = new[]
            {
                MaterialFactory.Opaque(prefix + "_Building0", color, 0.1f, 0.4f),
                MaterialFactory.Opaque(prefix + "_Building1", color * 0.8f, 0.2f, 0.6f),
                MaterialFactory.Opaque(prefix + "_Building2", color * 1.15f, 0.05f, 0.3f),
            };
            var windows = lit ? MaterialFactory.Emissive(prefix + "_Windows", new Color(0.1f, 0.1f, 0.12f), new Color(1.6f, 1.3f, 0.8f)) : null;
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 6)
            {
                attempts++;
                var s = samples[rng.Next(samples.Count)];
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float offset = minOffset + (float)rng.NextDouble() * (maxOffset - minOffset);
                float w = 14f + (float)rng.NextDouble() * 22f, d = 14f + (float)rng.NextDouble() * 22f;
                float h = minHeight + (float)rng.NextDouble() * (maxHeight - minHeight);
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + offset + w * 0.5f);
                if (!IsClear(samples, pos, Mathf.Max(w, d) * 0.75f + 4f)) continue;
                pos.y = h * 0.5f;
                var rot = Quaternion.LookRotation(s.Forward, Vector3.up);
                Prop(parent, "Building" + placed, PrimitiveType.Cube, pos, new Vector3(w, h, d), rot, mats[placed % mats.Length], true);
                if (lit && windows != null)
                    Prop(parent, "Windows" + placed, PrimitiveType.Cube, pos + rot * Vector3.right * (-side * (w * 0.5f + 0.05f)),
                        new Vector3(0.1f, h * 0.8f, d * 0.8f), rot, windows, false);
                placed++;
            }
        }

        private static void Lamps(Transform parent, List<TrackMeshBuilder.Sample> samples, string prefix, float spacing, Color emission, int realLights)
        {
            var post = MaterialFactory.Opaque(prefix + "_Post", new Color(0.3f, 0.3f, 0.32f), 0.6f, 0.5f);
            var head = MaterialFactory.Emissive(prefix + "_LampHead", Color.white, emission);
            float travelled = 0f, next = 0f;
            int made = 0;
            for (int i = 1; i < samples.Count; i++)
            {
                travelled += Vector3.Distance(samples[i - 1].Position, samples[i].Position);
                if (travelled < next) continue;
                next += spacing;
                var s = samples[i];
                float side = made % 2 == 0 ? -1f : 1f;
                Vector3 basePos = s.Position + s.Right * side * (s.HalfWidth + 5f);
                Prop(parent, "Post" + made, PrimitiveType.Cylinder, basePos + Vector3.up * 4f, new Vector3(0.3f, 4f, 0.3f), Quaternion.identity, post, false);
                Prop(parent, "Lamp" + made, PrimitiveType.Cube, basePos + Vector3.up * 8f - s.Right * side * 1.5f, new Vector3(1.4f, 0.3f, 0.6f), Quaternion.LookRotation(s.Forward), head, false);
                if (made < realLights)
                {
                    var light = new GameObject("Light" + made, typeof(Light));
                    light.transform.SetParent(parent, false);
                    light.transform.position = basePos + Vector3.up * 7.8f - s.Right * side * 1.5f;
                    light.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
                    var l = light.GetComponent<Light>();
                    l.type = LightType.Spot;
                    l.spotAngle = 120f;
                    l.range = 40f;
                    l.intensity = 600f;
                    l.color = new Color(1f, 0.85f, 0.6f);
                    l.shadows = LightShadows.None;
                }
                made++;
            }
        }

        private static void Trees(Transform parent, List<TrackMeshBuilder.Sample> samples, System.Random rng, string prefix, int count, float minOffset, float maxOffset)
        {
            var trunk = MaterialFactory.Opaque(prefix + "_Trunk", new Color(0.3f, 0.22f, 0.15f), 0f, 0.2f);
            var leaves = MaterialFactory.Opaque(prefix + "_Leaves", new Color(0.12f, 0.32f, 0.16f), 0f, 0.15f);
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 6)
            {
                attempts++;
                var s = samples[rng.Next(samples.Count)];
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float offset = minOffset + (float)rng.NextDouble() * (maxOffset - minOffset);
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + offset);
                if (!IsClear(samples, pos, 6f)) continue;
                float h = 8f + (float)rng.NextDouble() * 10f;
                pos.y = s.Position.y - 1f;
                Prop(parent, "Trunk" + placed, PrimitiveType.Cylinder, pos + Vector3.up * h * 0.25f, new Vector3(0.6f, h * 0.25f, 0.6f), Quaternion.identity, trunk, false);
                Prop(parent, "Crown" + placed, PrimitiveType.Capsule, pos + Vector3.up * h * 0.65f, new Vector3(3.5f, h * 0.4f, 3.5f), Quaternion.identity, leaves, false);
                placed++;
            }
        }

        private static void Containers(Transform parent, List<TrackMeshBuilder.Sample> samples, System.Random rng, string prefix, int count)
        {
            var mats = new[]
            {
                MaterialFactory.Opaque(prefix + "_Container0", new Color(0.55f, 0.25f, 0.18f), 0.2f, 0.45f),
                MaterialFactory.Opaque(prefix + "_Container1", new Color(0.2f, 0.35f, 0.55f), 0.2f, 0.45f),
                MaterialFactory.Opaque(prefix + "_Container2", new Color(0.3f, 0.5f, 0.3f), 0.2f, 0.45f),
            };
            int placed = 0, attempts = 0;
            while (placed < count && attempts < count * 6)
            {
                attempts++;
                var s = samples[rng.Next(samples.Count)];
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float offset = 8f + (float)rng.NextDouble() * 40f;
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + offset);
                if (!IsClear(samples, pos, 9f)) continue;
                int stack = 1 + rng.Next(3);
                var rot = Quaternion.LookRotation(s.Forward) * Quaternion.Euler(0f, (float)rng.NextDouble() * 12f - 6f, 0f);
                for (int k = 0; k < stack; k++)
                    Prop(parent, "Container" + placed + "_" + k, PrimitiveType.Cube, pos + Vector3.up * (1.3f + k * 2.6f), new Vector3(2.4f, 2.6f, 12f), rot, mats[rng.Next(mats.Length)], k == 0);
                placed++;
            }
        }

        private static void Billboards(Transform parent, List<TrackMeshBuilder.Sample> samples, System.Random rng, string prefix, int count)
        {
            var board = MaterialFactory.Emissive(prefix + "_Billboard", new Color(0.9f, 0.9f, 0.9f), new Color(0.6f, 0.6f, 0.7f));
            var post = MaterialFactory.Opaque(prefix + "_BillPost", new Color(0.3f, 0.3f, 0.32f), 0.5f, 0.5f);
            for (int i = 0; i < count; i++)
            {
                var s = samples[(i * samples.Count) / count];
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + 16f);
                if (!IsClear(samples, pos, 10f)) continue;
                Prop(parent, "BillPost" + i, PrimitiveType.Cylinder, pos + Vector3.up * 4f, new Vector3(0.5f, 4f, 0.5f), Quaternion.identity, post, false);
                Prop(parent, "Billboard" + i, PrimitiveType.Cube, pos + Vector3.up * 11f, new Vector3(16f, 6f, 0.4f), Quaternion.LookRotation(s.Right * -side), board, false);
            }
        }

        private static void Grandstands(Transform parent, List<TrackMeshBuilder.Sample> samples, string prefix, int count)
        {
            var stand = MaterialFactory.Opaque(prefix + "_Stand", new Color(0.55f, 0.55f, 0.6f), 0.1f, 0.4f);
            var seats = MaterialFactory.Opaque(prefix + "_Seats", new Color(0.8f, 0.2f, 0.2f), 0f, 0.3f);
            for (int i = 0; i < count; i++)
            {
                int index = (i * samples.Count) / count;
                var s = samples[index];
                float side = i % 2 == 0 ? 1f : -1f;
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + 22f);
                if (!IsClear(samples, pos, 16f)) continue;
                var rot = Quaternion.LookRotation(s.Forward);
                Prop(parent, "Stand" + i, PrimitiveType.Cube, pos + Vector3.up * 4f, new Vector3(14f, 8f, 60f), rot * Quaternion.Euler(0f, 0f, side * 25f), stand, true);
                Prop(parent, "Seats" + i, PrimitiveType.Cube, pos + Vector3.up * 8.5f - s.Right * side * 3f, new Vector3(8f, 0.6f, 58f), rot * Quaternion.Euler(0f, 0f, side * 25f), seats, false);
            }
        }

        private static void TyreStacks(Transform parent, List<TrackMeshBuilder.Sample> samples, System.Random rng, string prefix, int count)
        {
            var tyre = MaterialFactory.Opaque(prefix + "_TyreStack", new Color(0.08f, 0.08f, 0.08f), 0f, 0.35f);
            for (int i = 0; i < count; i++)
            {
                var s = samples[(i * samples.Count) / count];
                float side = rng.Next(2) == 0 ? -1f : 1f;
                Vector3 pos = s.Position + s.Right * side * (s.HalfWidth + 5.5f);
                Prop(parent, "Tyres" + i, PrimitiveType.Cylinder, pos + Vector3.up * 0.6f, new Vector3(1.2f, 0.6f, 1.2f), Quaternion.identity, tyre, false);
            }
        }

        private static void Sea(Transform parent, Bounds bounds, string prefix)
        {
            var sea = MaterialFactory.Opaque(prefix + "_Sea", new Color(0.05f, 0.25f, 0.4f), 0.1f, 0.95f);
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Sea";
            water.transform.SetParent(parent, false);
            water.transform.position = new Vector3(bounds.center.x, -6f, bounds.center.z);
            water.transform.localScale = new Vector3(400f, 1f, 400f);
            water.GetComponent<MeshRenderer>().sharedMaterial = sea;
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.isStatic = true;
        }
    }
}
