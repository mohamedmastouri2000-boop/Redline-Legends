using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Turns a closed control polygon into a smooth centreline (Catmull-Rom) and builds road,
    /// kerb and barrier geometry in chunks (separate meshes = cheap culling and colliders on
    /// mobile). Generic: any future track supplies its own control points.
    /// </summary>
    public static class TrackMeshBuilder
    {
        public struct Sample
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Vector3 Right;
            public float HalfWidth;
        }

        /// <summary>Samples a Catmull-Rom spline through the control points every ~step metres.</summary>
        public static List<Sample> SampleSpline(Vector3[] control, float[] halfWidths, bool loop, float step)
        {
            var samples = new List<Sample>(1024);
            int n = control.Length;
            int segments = loop ? n : n - 1;
            for (int s = 0; s < segments; s++)
            {
                Vector3 p0 = control[Wrap(s - 1, n, loop)], p1 = control[s], p2 = control[Wrap(s + 1, n, loop)], p3 = control[Wrap(s + 2, n, loop)];
                float w1 = halfWidths[s], w2 = halfWidths[Wrap(s + 1, n, loop)];
                float segLen = Vector3.Distance(p1, p2);
                int count = Mathf.Max(2, Mathf.CeilToInt(segLen / step));
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / count;
                    Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
                    Vector3 next = CatmullRom(p0, p1, p2, p3, Mathf.Min(1f, t + 0.01f));
                    Vector3 fwd = next - pos;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                    fwd.Normalize();
                    samples.Add(new Sample
                    {
                        Position = pos,
                        Forward = fwd,
                        Right = Vector3.Cross(Vector3.up, fwd),
                        HalfWidth = Mathf.Lerp(w1, w2, t)
                    });
                }
            }
            if (!loop)
            {
                var last = control[n - 1];
                var prev = samples[samples.Count - 1];
                samples.Add(new Sample { Position = last, Forward = prev.Forward, Right = prev.Right, HalfWidth = halfWidths[n - 1] });
            }
            return samples;
        }

        private static int Wrap(int i, int n, bool loop) => loop ? ((i % n) + n) % n : Mathf.Clamp(i, 0, n - 1);

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>
        /// Road surface strips in chunks of chunkSamples samples, with a MeshCollider each. The
        /// shoulder strip reaches the barrier line so an elevated road has no gap to fall through.
        /// </summary>
        public static List<GameObject> BuildRoad(Transform parent, List<Sample> samples, bool loop, int chunkSamples,
            Material roadMaterial, Material kerbMaterial, string meshFolder, string meshPrefix, int layer, float shoulderWidth = 3.6f)
        {
            var chunks = new List<GameObject>();
            int total = samples.Count;
            int chunkIndex = 0;
            for (int start = 0; start < total; start += chunkSamples)
            {
                int end = Mathf.Min(start + chunkSamples, total);
                // Overlap one sample into the next chunk so there is no seam; loop closes to sample 0.
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var normals = new List<Vector3>();
                var roadTris = new List<int>();
                var kerbTris = new List<int>();
                float u = 0f;
                int count = end - start + 1;
                for (int k = 0; k < count; k++)
                {
                    int idx = start + k;
                    if (idx >= total) { if (!loop) break; idx = 0; }
                    var s = samples[idx];
                    if (k > 0)
                    {
                        int prevIdx = start + k - 1;
                        if (prevIdx >= total) prevIdx = 0;
                        u += Vector3.Distance(samples[prevIdx].Position, s.Position) / 8f;
                    }
                    float kerb = shoulderWidth;
                    // 4 verts per ring: shoulder L, road L, road R, shoulder R
                    Vector3 lKerb = s.Position - s.Right * (s.HalfWidth + kerb) + Vector3.up * 0.04f;
                    Vector3 l = s.Position - s.Right * s.HalfWidth;
                    Vector3 r = s.Position + s.Right * s.HalfWidth;
                    Vector3 rKerb = s.Position + s.Right * (s.HalfWidth + kerb) + Vector3.up * 0.04f;
                    verts.Add(lKerb); verts.Add(l); verts.Add(r); verts.Add(rKerb);
                    uvs.Add(new Vector2(0f, u)); uvs.Add(new Vector2(0f, u)); uvs.Add(new Vector2(1f, u)); uvs.Add(new Vector2(1f, u));
                    for (int v = 0; v < 4; v++) normals.Add(Vector3.up);
                    if (k > 0)
                    {
                        int a = (k - 1) * 4, b = k * 4;
                        Quad(kerbTris, a, a + 1, b + 1, b);       // left kerb
                        Quad(roadTris, a + 1, a + 2, b + 2, b + 1); // road
                        Quad(kerbTris, a + 2, a + 3, b + 3, b + 2); // right kerb
                    }
                }
                var mesh = new Mesh { name = meshPrefix + "_Road" + chunkIndex };
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetNormals(normals);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(roadTris, 0);
                mesh.SetTriangles(kerbTris, 1);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                AssetDatabase.CreateAsset(mesh, meshFolder + "/" + mesh.name + ".asset");

                var go = new GameObject("Road" + chunkIndex, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { roadMaterial, kerbMaterial };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.GetComponent<MeshCollider>().sharedMesh = mesh;
                go.isStatic = true;
                go.layer = layer;
                chunks.Add(go);
                chunkIndex++;
            }
            return chunks;
        }

        /// <summary>Two triangles for the strip quad (a,b) -> (c,d): a,c are the "from" edge, b,d the "to" edge.</summary>
        private static void Face(List<int> tris, int a, int b, int c, int d)
        {
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(d);
        }

        private static void Quad(List<int> tris, int a, int b, int c, int d)
        {
            // a-b-c-d counter-clockwise seen from above: (a, d, b) (b, d, c) wound for Unity's clockwise front faces
            tris.Add(a); tris.Add(d); tris.Add(b);
            tris.Add(b); tris.Add(d); tris.Add(c);
        }

        /// <summary>
        /// Continuous barrier ribbons along both edges (chunked). A single smooth strip gives cars a
        /// clean wall to slide along; segmented boxes leave wedges that trap them in corners.
        /// </summary>
        public static void BuildBarriers(Transform parent, List<Sample> samples, bool loop, float offsetFromEdge, float height,
            Material material, int layer, string meshFolder, string meshPrefix, int chunkSamples = 40)
        {
            var root = new GameObject("Barriers");
            root.transform.SetParent(parent, false);
            int total = samples.Count;
            for (int side = -1; side <= 1; side += 2)
            {
                int chunkIndex = 0;
                for (int start = 0; start < total; start += chunkSamples)
                {
                    int end = Mathf.Min(start + chunkSamples, total);
                    var verts = new List<Vector3>();
                    var uvs = new List<Vector2>();
                    var tris = new List<int>();
                    int count = end - start + 1;
                    float u = 0f;
                    for (int k = 0; k < count; k++)
                    {
                        int idx = start + k;
                        if (idx >= total) { if (!loop) break; idx = 0; }
                        var s = samples[idx];
                        if (k > 0)
                        {
                            int prevIdx = start + k - 1;
                            if (prevIdx >= total) prevIdx = 0;
                            u += Vector3.Distance(samples[prevIdx].Position, s.Position) / 4f;
                        }
                        // Extruded wall: inner face, top and outer face, so it is solid from both
                        // sides (a single-sided ribbon lets a fast car pass through from behind).
                        const float thickness = 0.5f;
                        Vector3 inner = s.Position + s.Right * side * (s.HalfWidth + offsetFromEdge);
                        Vector3 outer = inner + s.Right * side * thickness;
                        Vector3 innerTop = inner + Vector3.up * height - s.Right * side * 0.1f;
                        Vector3 outerTop = outer + Vector3.up * height;
                        verts.Add(inner - Vector3.up * 0.2f);   // 0 inner bottom
                        verts.Add(innerTop);                    // 1 inner top
                        verts.Add(outerTop);                    // 2 outer top
                        verts.Add(outer - Vector3.up * 0.2f);   // 3 outer bottom
                        uvs.Add(new Vector2(u, 0f)); uvs.Add(new Vector2(u, 1f)); uvs.Add(new Vector2(u, 1f)); uvs.Add(new Vector2(u, 0f));
                        if (k > 0)
                        {
                            int a = (k - 1) * 4, b = k * 4;
                            // Winding depends on the side so faces point away from the wall's core.
                            if (side < 0)
                            {
                                Face(tris, a, b, a + 1, b + 1);         // inner face (toward track)
                                Face(tris, a + 1, b + 1, a + 2, b + 2); // top
                                Face(tris, a + 2, b + 2, a + 3, b + 3); // outer face
                            }
                            else
                            {
                                Face(tris, b, a, b + 1, a + 1);
                                Face(tris, b + 1, a + 1, b + 2, a + 2);
                                Face(tris, b + 2, a + 2, b + 3, a + 3);
                            }
                        }
                    }
                    if (verts.Count < 8) continue;
                    var mesh = new Mesh { name = meshPrefix + "_Barrier" + (side < 0 ? "L" : "R") + chunkIndex };
                    mesh.SetVertices(verts);
                    mesh.SetUVs(0, uvs);
                    mesh.SetTriangles(tris, 0);
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, meshFolder + "/" + mesh.name + ".asset");

                    var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                    go.transform.SetParent(root.transform, false);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    go.GetComponent<MeshRenderer>().sharedMaterial = material;
                    go.GetComponent<MeshCollider>().sharedMesh = mesh;
                    go.isStatic = true;
                    go.layer = layer;
                    chunkIndex++;
                }
            }
        }
    }
}
