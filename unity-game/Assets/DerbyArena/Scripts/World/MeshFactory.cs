using System.Collections.Generic;
using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>Procedural meshes. Nothing at runtime depends on engine primitives (which pull in collider classes).</summary>
    public static class MeshFactory
    {
        static Mesh cylinder, cube, ring, quad;

        public static Mesh UnitCylinder() { if (cylinder == null) cylinder = Cylinder(0.5f, 1f, 20); return cylinder; }
        public static Mesh UnitCube() { if (cube == null) cube = Box(); return cube; }
        public static Mesh UnitRing() { if (ring == null) ring = Ring(0.86f, 1f, 48); return ring; }
        public static Mesh UnitQuad() { if (quad == null) quad = Quad(); return quad; }

        /// <summary>1x1 quad on the XZ plane facing up (contact shadows, decals).</summary>
        static Mesh Quad()
        {
            var v = new List<Vector3> { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            var n = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            var uv = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            var t = new List<int> { 0, 1, 2, 0, 2, 3 };
            return Build("Quad", v, n, uv, t);
        }

        /// <summary>Solid box collider (no renderer) with an arbitrary rotation.</summary>
        public static GameObject ColliderBox(string name, Transform parent, Vector3 worldPos, Vector3 size, Quaternion rot)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(worldPos, rot);
            go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
            return go;
        }

        public static Mesh Cylinder(float radius, float height, int segments)
        {
            int seg = Mathf.Max(3, segments);
            float h = height * 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * radius + Vector3.down * h); n.Add(d); uv.Add(new Vector2((float)i / seg, 0f));
                v.Add(d * radius + Vector3.up * h); n.Add(d); uv.Add(new Vector2((float)i / seg, 1f));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                t.Add(b + 2); t.Add(b + 1); t.Add(b + 3);
            }
            Cap(v, n, uv, t, radius, h, seg, true);
            Cap(v, n, uv, t, radius, -h, seg, false);
            return Build("Cylinder", v, n, uv, t);
        }

        /// <summary>
        /// Open cone along +Z for light shafts (2.1): radius r0 at z = 0, r1 at z = length. UVs run down the centre of
        /// the soft radial sprite (u = 0.5, v from 0.5 to 1), so an additive material fades the beam out along its length.
        /// </summary>
        public static Mesh Shaft(float r0, float r1, float length, int segments)
        {
            int seg = Mathf.Max(6, segments);
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                v.Add(d * r0); n.Add(d); uv.Add(new Vector2(0.5f, 0.5f));
                v.Add(d * r1 + Vector3.forward * length); n.Add(d); uv.Add(new Vector2(0.5f, 0.995f));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b + 2); t.Add(b + 3); t.Add(b + 1);
            }
            return Build("Shaft", v, n, uv, t);
        }

        static void Cap(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, float radius, float y, int seg, bool up)
        {
            Vector3 normal = up ? Vector3.up : Vector3.down;
            int c = v.Count;
            v.Add(new Vector3(0f, y, 0f)); n.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
            int rim = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius)); n.Add(normal);
                uv.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
            }
            for (int i = 0; i < seg; i++)
            {
                if (up) { t.Add(c); t.Add(rim + i + 1); t.Add(rim + i); }
                else { t.Add(c); t.Add(rim + i); t.Add(rim + i + 1); }
            }
        }

        /// <summary>Flat annulus on the XZ plane facing up.</summary>
        public static Mesh Ring(float inner, float outer, int segments)
        {
            int seg = Mathf.Max(8, segments);
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * inner); n.Add(Vector3.up); uv.Add(new Vector2((float)i / seg, 0f));
                v.Add(d * outer); n.Add(Vector3.up); uv.Add(new Vector2((float)i / seg, 1f));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                t.Add(b); t.Add(b + 3); t.Add(b + 1);
                t.Add(b); t.Add(b + 2); t.Add(b + 3);
            }
            return Build("Ring", v, n, uv, t);
        }

        static Mesh Box()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int f = 0; f < 6; f++)
            {
                Vector3 nd = dirs[f];
                Vector3 u = Mathf.Abs(nd.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 r = Vector3.Cross(u, nd);
                int b = v.Count;
                v.Add((nd - r - u) * 0.5f); v.Add((nd - r + u) * 0.5f); v.Add((nd + r + u) * 0.5f); v.Add((nd + r - u) * 0.5f);
                for (int k = 0; k < 4; k++) n.Add(nd);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0));
                t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
            }
            return Build("Box", v, n, uv, t);
        }

        static Mesh Build(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Renderable object without collider.</summary>
        public static GameObject Make(string name, Mesh mesh, Material mat, Transform parent, Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            return go;
        }

        /// <summary>Solid box: visible mesh (optional) plus BoxCollider, positioned in world space.</summary>
        public static GameObject SolidBox(string name, Transform parent, Vector3 worldPos, Vector3 size, Quaternion rot, Material mat)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(worldPos, rot);
            go.transform.localScale = size;
            if (mat != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = UnitCube();
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
            go.AddComponent<BoxCollider>();
            go.isStatic = true;
            return go;
        }
    }
}
