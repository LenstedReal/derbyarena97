using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Minimal Wavefront OBJ parser (v / vt / vn / f, triangles and polygons).
    /// OBJ data is right-handed. The X axis is mirrored so the geometry stays correct in Unity's
    /// left-handed space; mirroring also keeps the triangle winding valid.
    /// </summary>
    public static class ObjMesh
    {
        static readonly char[] Sep = { ' ', '\t' };
        static readonly char[] Slash = { '/' };

        static float F(string s)
        {
            return float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static int Idx(string s, int count)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            int i = int.Parse(s, CultureInfo.InvariantCulture);
            if (i < 0) return count + i;
            return i - 1;
        }

        public static Mesh Parse(string text, string name)
        {
            var pos = new List<Vector3>(4096);
            var uv = new List<Vector2>(4096);
            var nor = new List<Vector3>(4096);
            var vertices = new List<Vector3>(8192);
            var uvs = new List<Vector2>(8192);
            var normals = new List<Vector3>(8192);
            var tris = new List<int>(16384);
            var map = new Dictionary<long, int>(8192);
            var face = new List<int>(8);
            bool hasNormals = true;

            int len = text.Length;
            int start = 0;
            while (start < len)
            {
                int end = text.IndexOf('\n', start);
                if (end < 0) end = len;
                string line = text.Substring(start, end - start).Trim();
                start = end + 1;
                if (line.Length < 2 || line[0] == '#') continue;

                string[] p = line.Split(Sep, System.StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0) continue;

                if (p[0] == "v" && p.Length >= 4)
                    pos.Add(new Vector3(-F(p[1]), F(p[2]), F(p[3])));
                else if (p[0] == "vt" && p.Length >= 3)
                    uv.Add(new Vector2(F(p[1]), F(p[2])));
                else if (p[0] == "vn" && p.Length >= 4)
                    nor.Add(new Vector3(-F(p[1]), F(p[2]), F(p[3])));
                else if (p[0] == "f" && p.Length >= 4)
                {
                    face.Clear();
                    for (int k = 1; k < p.Length; k++)
                    {
                        string[] c = p[k].Split(Slash);
                        int vi = Idx(c[0], pos.Count);
                        int ti = c.Length > 1 ? Idx(c[1], uv.Count) : -1;
                        int ni = c.Length > 2 ? Idx(c[2], nor.Count) : -1;
                        if (vi < 0 || vi >= pos.Count) continue;
                        if (ni < 0 || ni >= nor.Count) { ni = -1; hasNormals = false; }
                        if (ti >= uv.Count) ti = -1;

                        long key = ((long)vi) | ((long)(ti + 1) << 22) | ((long)(ni + 1) << 43);
                        int index;
                        if (!map.TryGetValue(key, out index))
                        {
                            index = vertices.Count;
                            vertices.Add(pos[vi]);
                            uvs.Add(ti >= 0 ? uv[ti] : Vector2.zero);
                            normals.Add(ni >= 0 ? nor[ni] : Vector3.up);
                            map[key] = index;
                        }
                        face.Add(index);
                    }
                    for (int k = 1; k + 1 < face.Count; k++)
                    {
                        tris.Add(face[0]);
                        tris.Add(face[k]);
                        tris.Add(face[k + 1]);
                    }
                }
            }

            var mesh = new Mesh();
            mesh.name = name;
            if (vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            if (hasNormals)
                mesh.SetNormals(normals);
            else
                mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
