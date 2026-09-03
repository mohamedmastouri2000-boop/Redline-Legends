using System.Collections.Generic;
using RedlineLegends.Core;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Rolling heightfield around a track: flat and level with the road nearby, gentle undulation
    /// in the middle distance, hills and ridges toward the horizon. One mesh with a MeshCollider so
    /// cars that leave the road land on ground, not fall into the void.
    /// </summary>
    public static class TerrainBuilder
    {
        public sealed class Result
        {
            public GameObject Object;
            public System.Func<Vector3, float> HeightAt;
        }

        public static Result Build(Transform parent, List<TrackMeshBuilder.Sample> samples, Bounds trackBounds, Material material,
            string meshFolder, string prefix, float margin, float cell, float nearFlat, float farHills, int seed,
            Material steepMaterial = null, float blendDistance = 90f, float farDistance = 700f)
        {
            float minX = trackBounds.min.x - margin, maxX = trackBounds.max.x + margin;
            float minZ = trackBounds.min.z - margin, maxZ = trackBounds.max.z + margin;
            int nx = Mathf.CeilToInt((maxX - minX) / cell) + 1;
            int nz = Mathf.CeilToInt((maxZ - minZ) / cell) + 1;

            // Coarse spatial grid of road samples for fast nearest-road queries.
            var grid = new RoadGrid(samples, 40f);

            System.Func<Vector3, float> heightAt = p =>
            {
                grid.Nearest(p, out float dist, out float roadY);
                float shoulder = grid.HalfWidthNear(p) + 6f;
                if (dist <= shoulder) return roadY - 0.25f;
                float t = Mathf.Clamp01((dist - shoulder) / blendDistance);
                float far = Mathf.Clamp01((dist - shoulder) / farDistance);   // hills grow toward the horizon
                float noise = Fbm(p.x * 0.004f + seed, p.z * 0.004f - seed, 4) * 2f - 1f;
                float ridge = Fbm(p.x * 0.0012f - seed * 0.7f, p.z * 0.0012f + seed * 0.3f, 3) * 2f - 1f;
                float amplitude = Mathf.Lerp(nearFlat, farHills, far * far);
                // Squared ridge: soft onset, so distant hills swell instead of standing up like a wall.
                float ridgeUp = Mathf.Max(0f, ridge);
                float h = roadY - 0.25f + Smooth(t) * (noise * amplitude * 0.45f + ridgeUp * ridgeUp * amplitude);
                return h;
            };

            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                var p = new Vector3(minX + x * cell, 0f, minZ + z * cell);
                p.y = heightAt(p);
                verts[z * nx + x] = p;
                uvs[z * nx + x] = new Vector2(p.x / 20f, p.z / 20f);
            }
            // Two submeshes: gentle ground and steep faces (rock), decided per quad by slope.
            var groundTris = new List<int>((nx - 1) * (nz - 1) * 6);
            var steepTris = new List<int>();
            bool splitBySlope = steepMaterial != null;
            for (int z = 0; z < nz - 1; z++)
            for (int x = 0; x < nx - 1; x++)
            {
                int a = z * nx + x, b = a + 1, c = a + nx, d = c + 1;
                float rise = Mathf.Max(Mathf.Abs(verts[a].y - verts[b].y), Mathf.Abs(verts[a].y - verts[c].y), Mathf.Abs(verts[d].y - verts[b].y), Mathf.Abs(verts[d].y - verts[c].y));
                var list = splitBySlope && rise / cell > 0.55f ? steepTris : groundTris;
                list.Add(a); list.Add(c); list.Add(b);
                list.Add(b); list.Add(c); list.Add(d);
            }
            var mesh = new Mesh { name = prefix + "_Terrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.subMeshCount = splitBySlope ? 2 : 1;
            mesh.SetTriangles(groundTris, 0);
            if (splitBySlope) mesh.SetTriangles(steepTris, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshFolder + "/" + mesh.name + ".asset");

            var go = new GameObject("Terrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = splitBySlope ? new[] { material, steepMaterial } : new[] { material };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            go.layer = GameLayers.Track;
            return new Result { Object = go, HeightAt = heightAt };
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static float Fbm(float x, float y, int octaves)
        {
            float amp = 0.5f, freq = 1f, sum = 0f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * freq + 13.7f, y * freq + 7.3f) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }
            return sum / norm;
        }

        /// <summary>Bucketed road samples so height queries stay fast over tens of thousands of vertices.</summary>
        private sealed class RoadGrid
        {
            private readonly Dictionary<long, List<int>> _buckets = new Dictionary<long, List<int>>();
            private readonly List<TrackMeshBuilder.Sample> _samples;
            private readonly float _cell;

            public RoadGrid(List<TrackMeshBuilder.Sample> samples, float cell)
            {
                _samples = samples;
                _cell = cell;
                for (int i = 0; i < samples.Count; i++)
                {
                    long key = Key(samples[i].Position);
                    if (!_buckets.TryGetValue(key, out var list)) _buckets[key] = list = new List<int>();
                    list.Add(i);
                }
            }

            private long Key(Vector3 p) => Key(Mathf.FloorToInt(p.x / _cell), Mathf.FloorToInt(p.z / _cell));
            private static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

            public void Nearest(Vector3 p, out float distance, out float roadY)
            {
                int cx = Mathf.FloorToInt(p.x / _cell), cz = Mathf.FloorToInt(p.z / _cell);
                float best = float.MaxValue;
                roadY = 0f;
                // Expand rings until something is found (road is at most a few hundred metres away).
                for (int ring = 0; ring <= 30 && best == float.MaxValue; ring++)
                {
                    for (int dz = -ring; dz <= ring; dz++)
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Mathf.Abs(dx) != ring && Mathf.Abs(dz) != ring) continue;
                        if (!_buckets.TryGetValue(Key(cx + dx, cz + dz), out var list)) continue;
                        for (int i = 0; i < list.Count; i++)
                        {
                            var s = _samples[list[i]].Position;
                            float ddx = s.x - p.x, ddz = s.z - p.z;
                            float d = ddx * ddx + ddz * ddz;
                            if (d < best) { best = d; roadY = s.y; }
                        }
                    }
                    // Once a ring hits, check one more ring so a nearer sample in a neighbouring cell is not missed.
                    if (best < float.MaxValue && ring < 30)
                    {
                        int r2 = ring + 1;
                        for (int dz = -r2; dz <= r2; dz++)
                        for (int dx = -r2; dx <= r2; dx++)
                        {
                            if (Mathf.Abs(dx) != r2 && Mathf.Abs(dz) != r2) continue;
                            if (!_buckets.TryGetValue(Key(cx + dx, cz + dz), out var list)) continue;
                            for (int i = 0; i < list.Count; i++)
                            {
                                var s = _samples[list[i]].Position;
                                float ddx = s.x - p.x, ddz = s.z - p.z;
                                float d = ddx * ddx + ddz * ddz;
                                if (d < best) { best = d; roadY = s.y; }
                            }
                        }
                        break;
                    }
                }
                distance = best == float.MaxValue ? 9999f : Mathf.Sqrt(best);
            }

            public float HalfWidthNear(Vector3 p) => _samples.Count > 0 ? _samples[0].HalfWidth : 7f;
        }
    }
}
