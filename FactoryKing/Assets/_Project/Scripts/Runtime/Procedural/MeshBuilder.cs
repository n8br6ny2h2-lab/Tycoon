using System.Collections.Generic;
using UnityEngine;

namespace FactoryKing.Runtime.Procedural
{
    /// <summary>
    /// Builds flat-shaded, chamfered low-poly meshes with multiple sub-meshes (one per material slot).
    /// All machine/building geometry is generated here so the project has no third-party model dependencies.
    /// Replace with authored FBX models through the same view classes when final art is available.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>(1024);
        private readonly List<Vector3> _normals = new List<Vector3>(1024);
        private readonly List<Vector2> _uvs = new List<Vector2>(1024);
        private readonly List<List<int>> _submeshes = new List<List<int>>();
        private readonly Stack<Matrix4x4> _stack = new Stack<Matrix4x4>();
        private Matrix4x4 _matrix = Matrix4x4.identity;

        public int Slot { get; set; }

        public MeshBuilder PushMatrix(Matrix4x4 m)
        {
            _stack.Push(_matrix);
            _matrix = _matrix * m;
            return this;
        }

        public MeshBuilder PushTRS(Vector3 position, Quaternion rotation, Vector3 scale) =>
            PushMatrix(Matrix4x4.TRS(position, rotation, scale));

        public MeshBuilder PopMatrix()
        {
            _matrix = _stack.Count > 0 ? _stack.Pop() : Matrix4x4.identity;
            return this;
        }

        private List<int> Indices
        {
            get
            {
                while (_submeshes.Count <= Slot) _submeshes.Add(new List<int>());
                return _submeshes[Slot];
            }
        }

        // ---------------- Primitives ----------------

        /// <summary>Adds a triangle; winding is fixed automatically so the face points along <paramref name="outward"/>.</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            a = _matrix.MultiplyPoint3x4(a);
            b = _matrix.MultiplyPoint3x4(b);
            c = _matrix.MultiplyPoint3x4(c);
            Vector3 n = _matrix.MultiplyVector(outward).normalized;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0) { var t = b; b = c; c = t; }
            int i = _vertices.Count;
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
            _normals.Add(n); _normals.Add(n); _normals.Add(n);
            _uvs.Add(Planar(a, n)); _uvs.Add(Planar(b, n)); _uvs.Add(Planar(c, n));
            var idx = Indices;
            idx.Add(i); idx.Add(i + 1); idx.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Triangle(a, b, c, outward);
            Triangle(a, c, d, outward);
        }

        /// <summary>Quad with explicit UVs (used for scrolling conveyor belts).</summary>
        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 outward)
        {
            a = _matrix.MultiplyPoint3x4(a); b = _matrix.MultiplyPoint3x4(b);
            c = _matrix.MultiplyPoint3x4(c); d = _matrix.MultiplyPoint3x4(d);
            Vector3 n = _matrix.MultiplyVector(outward).normalized;
            bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0;
            int i = _vertices.Count;
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c); _vertices.Add(d);
            for (int k = 0; k < 4; k++) _normals.Add(n);
            _uvs.Add(ua); _uvs.Add(ub); _uvs.Add(uc); _uvs.Add(ud);
            var idx = Indices;
            if (!flip) { idx.Add(i); idx.Add(i + 1); idx.Add(i + 2); idx.Add(i); idx.Add(i + 2); idx.Add(i + 3); }
            else { idx.Add(i); idx.Add(i + 2); idx.Add(i + 1); idx.Add(i); idx.Add(i + 3); idx.Add(i + 2); }
        }

        /// <summary>Box with chamfered edges (bevel = 0 gives a sharp box).</summary>
        public void Box(Vector3 center, Vector3 size, float bevel = 0.06f)
        {
            Vector3 h = size * 0.5f;
            float b = Mathf.Clamp(bevel, 0f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.9f);
            Vector3 e = h - new Vector3(b, b, b);

            // 6 faces
            for (int axis = 0; axis < 3; axis++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    int u = (axis + 1) % 3, v = (axis + 2) % 3;
                    Vector3 n = Vector3.zero; n[axis] = s;
                    Vector3 p0 = Vector3.zero, p1 = Vector3.zero, p2 = Vector3.zero, p3 = Vector3.zero;
                    p0[axis] = p1[axis] = p2[axis] = p3[axis] = s * h[axis];
                    p0[u] = -e[u]; p0[v] = -e[v];
                    p1[u] = e[u]; p1[v] = -e[v];
                    p2[u] = e[u]; p2[v] = e[v];
                    p3[u] = -e[u]; p3[v] = e[v];
                    Quad(center + p0, center + p1, center + p2, center + p3, n);
                }
            }
            if (b <= 0f) return;

            // 12 edge chamfers
            for (int a = 0; a < 3; a++)
            {
                int c = (a + 1) % 3, w = (a + 2) % 3; // edge runs along w
                for (int sa = -1; sa <= 1; sa += 2)
                for (int sc = -1; sc <= 1; sc += 2)
                {
                    Vector3 n = Vector3.zero; n[a] = sa; n[c] = sc;
                    Vector3 p0 = Vector3.zero, p1 = Vector3.zero, p2 = Vector3.zero, p3 = Vector3.zero;
                    p0[a] = sa * h[a]; p0[c] = sc * e[c]; p0[w] = -e[w];
                    p1[a] = sa * h[a]; p1[c] = sc * e[c]; p1[w] = e[w];
                    p2[a] = sa * e[a]; p2[c] = sc * h[c]; p2[w] = e[w];
                    p3[a] = sa * e[a]; p3[c] = sc * h[c]; p3[w] = -e[w];
                    Quad(center + p0, center + p1, center + p2, center + p3, n.normalized);
                }
            }

            // 8 corner triangles
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var a = new Vector3(sx * h.x, sy * e.y, sz * e.z);
                var bb = new Vector3(sx * e.x, sy * h.y, sz * e.z);
                var c = new Vector3(sx * e.x, sy * e.y, sz * h.z);
                Triangle(center + a, center + bb, center + c, new Vector3(sx, sy, sz).normalized);
            }
        }

        /// <summary>Vertical frustum/cylinder from y=0 to y=height around <paramref name="baseCenter"/>.</summary>
        public void Frustum(Vector3 baseCenter, float bottomRadius, float topRadius, float height, int segments = 16,
            bool capTop = true, bool capBottom = true, bool smooth = true)
        {
            segments = Mathf.Max(3, segments);
            var top = baseCenter + Vector3.up * height;
            float slope = (bottomRadius - topRadius) / Mathf.Max(0.0001f, height);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                var d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                var b0 = baseCenter + d0 * bottomRadius; var b1 = baseCenter + d1 * bottomRadius;
                var t0 = top + d0 * topRadius; var t1 = top + d1 * topRadius;
                if (smooth)
                {
                    SmoothSide(b0, b1, t1, t0, (d0 + Vector3.up * slope).normalized, (d1 + Vector3.up * slope).normalized);
                }
                else
                {
                    var mid = (d0 + d1).normalized;
                    Quad(b0, b1, t1, t0, (mid + Vector3.up * slope).normalized);
                }
                if (capTop && topRadius > 0) Triangle(top, t0, t1, Vector3.up);
                if (capBottom && bottomRadius > 0) Triangle(baseCenter, b1, b0, Vector3.down);
            }
        }

        public void Cylinder(Vector3 baseCenter, float radius, float height, int segments = 16, bool caps = true) =>
            Frustum(baseCenter, radius, radius, height, segments, caps, caps);

        /// <summary>Cylinder along an arbitrary axis (e.g. rollers, wheels, pipes).</summary>
        public void CylinderAlong(Vector3 from, Vector3 to, float radius, int segments = 14, bool caps = true)
        {
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 1e-5f) return;
            PushTRS(from, Quaternion.FromToRotation(Vector3.up, dir / len), Vector3.one);
            Cylinder(Vector3.zero, radius, len, segments, caps);
            PopMatrix();
        }

        private void SmoothSide(Vector3 b0, Vector3 b1, Vector3 t1, Vector3 t0, Vector3 n0, Vector3 n1)
        {
            Vector3 w0 = _matrix.MultiplyPoint3x4(b0), w1 = _matrix.MultiplyPoint3x4(b1);
            Vector3 w2 = _matrix.MultiplyPoint3x4(t1), w3 = _matrix.MultiplyPoint3x4(t0);
            Vector3 m0 = _matrix.MultiplyVector(n0).normalized, m1 = _matrix.MultiplyVector(n1).normalized;
            Vector3 faceN = (m0 + m1).normalized;
            bool flip = Vector3.Dot(Vector3.Cross(w1 - w0, w2 - w0), faceN) < 0;
            int i = _vertices.Count;
            _vertices.Add(w0); _vertices.Add(w1); _vertices.Add(w2); _vertices.Add(w3);
            _normals.Add(m0); _normals.Add(m1); _normals.Add(m1); _normals.Add(m0);
            _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(1, 0)); _uvs.Add(new Vector2(1, 1)); _uvs.Add(new Vector2(0, 1));
            var idx = Indices;
            if (!flip) { idx.Add(i); idx.Add(i + 1); idx.Add(i + 2); idx.Add(i); idx.Add(i + 2); idx.Add(i + 3); }
            else { idx.Add(i); idx.Add(i + 2); idx.Add(i + 1); idx.Add(i); idx.Add(i + 3); idx.Add(i + 2); }
        }

        private static Vector2 Planar(Vector3 p, Vector3 n)
        {
            Vector3 an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (an.y >= an.x && an.y >= an.z) return new Vector2(p.x, p.z);
            if (an.x >= an.z) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }

        public bool IsEmpty => _vertices.Count == 0;
        public int SubMeshCount => _submeshes.Count;

        /// <summary>
        /// Creates the mesh. Empty material slots are dropped; <paramref name="usedSlots"/> lists, per sub-mesh,
        /// which slot (material id) it came from so the caller can assign matching materials.
        /// </summary>
        public Mesh ToMesh(string name, out int[] usedSlots)
        {
            var mesh = new Mesh { name = name };
            if (_vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);

            var slots = new List<int>();
            for (int s = 0; s < _submeshes.Count; s++) if (_submeshes[s].Count > 0) slots.Add(s);
            mesh.subMeshCount = Mathf.Max(1, slots.Count);
            for (int i = 0; i < slots.Count; i++) mesh.SetTriangles(_submeshes[slots[i]], i, false);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true); // free the CPU copy: geometry never changes after build
            usedSlots = slots.ToArray();
            return mesh;
        }

        public void Clear()
        {
            _vertices.Clear(); _normals.Clear(); _uvs.Clear();
            _submeshes.Clear(); _stack.Clear();
            _matrix = Matrix4x4.identity;
            Slot = 0;
        }
    }
}
