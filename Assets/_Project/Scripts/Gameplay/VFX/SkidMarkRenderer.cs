using UnityEngine;

namespace RedlineLegends.VFX
{
    /// <summary>
    /// One mesh per scene holding a ring buffer of skid-mark quads. Cars append sections through
    /// <see cref="AddSection"/>; the mesh is rebuilt at most once per frame when dirty. No
    /// allocation after construction.
    /// </summary>
    public sealed class SkidMarkRenderer : MonoBehaviour
    {
        private struct Section
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector4 Tangent;
            public Vector3 Left;
            public Vector3 Right;
            public Color32 Color;
            public int LastIndex;
        }

        private const float MinDistance = 0.25f;
        private const float MarkWidth = 0.32f;

        private Section[] _sections;
        private int _count;
        private Mesh _mesh;
        private Vector3[] _vertices;
        private Vector3[] _normals;
        private Vector4[] _tangents;
        private Color32[] _colors;
        private Vector2[] _uvs;
        private int[] _triangles;
        private bool _dirty;
        private int _capacity;

        public void Initialize(int capacity, Material material)
        {
            _capacity = Mathf.Max(64, capacity);
            _sections = new Section[_capacity];
            _vertices = new Vector3[_capacity * 4];
            _normals = new Vector3[_capacity * 4];
            _tangents = new Vector4[_capacity * 4];
            _colors = new Color32[_capacity * 4];
            _uvs = new Vector2[_capacity * 4];
            _triangles = new int[_capacity * 6];

            _mesh = new Mesh { name = "SkidMarks" };
            _mesh.MarkDynamic();
            var filter = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = _mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }

        /// <summary>
        /// Appends a mark section at the contact point. Pass the index returned last time for a
        /// continuous strip, or -1 to start a new one. Returns the new index.
        /// </summary>
        public int AddSection(Vector3 position, Vector3 normal, float intensity01, int lastIndex)
        {
            if (_sections == null || intensity01 <= 0.01f) return -1;
            if (lastIndex != -1 && (position - _sections[lastIndex].Position).sqrMagnitude < MinDistance * MinDistance)
                return lastIndex;

            var section = new Section
            {
                Position = position + normal * 0.02f,
                Normal = normal,
                Color = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(intensity01) * 200f)),
                LastIndex = lastIndex
            };
            if (lastIndex != -1)
            {
                var prev = _sections[lastIndex];
                Vector3 dir = section.Position - prev.Position;
                Vector3 side = Vector3.Cross(dir, normal).normalized * MarkWidth * 0.5f;
                section.Left = section.Position - side;
                section.Right = section.Position + side;
                section.Tangent = new Vector4(dir.normalized.x, dir.normalized.y, dir.normalized.z, 1f);
                if (prev.LastIndex == -1)
                {
                    prev.Left = prev.Position - side;
                    prev.Right = prev.Position + side;
                    prev.Tangent = section.Tangent;
                    _sections[lastIndex] = prev;
                }
                WriteQuad(_count, prev, section);
            }
            _sections[_count] = section;
            int index = _count;
            _count = (_count + 1) % _capacity;
            return index;
        }

        private void WriteQuad(int index, in Section a, in Section b)
        {
            int v = index * 4;
            _vertices[v] = a.Left; _vertices[v + 1] = a.Right; _vertices[v + 2] = b.Left; _vertices[v + 3] = b.Right;
            _normals[v] = _normals[v + 1] = a.Normal; _normals[v + 2] = _normals[v + 3] = b.Normal;
            _tangents[v] = _tangents[v + 1] = a.Tangent; _tangents[v + 2] = _tangents[v + 3] = b.Tangent;
            _colors[v] = _colors[v + 1] = a.Color; _colors[v + 2] = _colors[v + 3] = b.Color;
            _uvs[v] = new Vector2(0f, 0f); _uvs[v + 1] = new Vector2(1f, 0f); _uvs[v + 2] = new Vector2(0f, 1f); _uvs[v + 3] = new Vector2(1f, 1f);
            int t = index * 6;
            _triangles[t] = v; _triangles[t + 1] = v + 2; _triangles[t + 2] = v + 1;
            _triangles[t + 3] = v + 2; _triangles[t + 4] = v + 3; _triangles[t + 5] = v + 1;
            _dirty = true;
        }

        private void LateUpdate()
        {
            if (!_dirty || _mesh == null) return;
            _dirty = false;
            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
            _mesh.tangents = _tangents;
            _mesh.colors32 = _colors;
            _mesh.uv = _uvs;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
        }
    }
}
