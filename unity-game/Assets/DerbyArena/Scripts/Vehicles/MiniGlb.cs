using System;
using System.Collections.Generic;
using System.Text;
using DerbyArena.World;
using UnityEngine;
using static DerbyArena.Vehicles.MiniJson;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Small, dependency-free GLB (glTF 2.0 binary) importer for the vehicle models.
    /// Supports node hierarchies (translation / rotation / scale), indexed triangle meshes with
    /// POSITION / NORMAL / TEXCOORD_0, base colour factors and textures, emissive factors and
    /// alpha-blended glass. It builds ordinary Unity meshes and materials from the Resources
    /// templates, so it works with the Built-in render pipeline and needs no extra packages.
    /// glTF is right-handed: X is mirrored (positions, normals, node transforms) like glTFast does.
    /// </summary>
    public static class MiniGlb
    {
        sealed class Ctx
        {
            public byte[] Glb;
            public int BinStart;
            public Dictionary<string, object> Root;
            public List<object> Accessors;
            public List<object> Views;
            public List<object> Textures;
            public List<object> Images;
            public readonly Dictionary<int, Texture2D> TextureCache = new Dictionary<int, Texture2D>();
        }

        sealed class MeshData
        {
            public Mesh Mesh;
            public Material[] Materials;
        }

        public static void Build(byte[] glb, Transform parent)
        {
            if (glb == null || glb.Length < 20)
                throw new FormatException("GLB is empty or truncated.");
            if (BitConverter.ToUInt32(glb, 0) != 0x46546C67u)
                throw new FormatException("Invalid GLB magic header.");

            string json = null;
            int binStart = -1;
            int off = 12;
            while (off + 8 <= glb.Length)
            {
                int clen = (int)BitConverter.ToUInt32(glb, off);
                uint ctype = BitConverter.ToUInt32(glb, off + 4);
                int dstart = off + 8;
                if (ctype == 0x4E4F534Au) json = Encoding.UTF8.GetString(glb, dstart, clen);
                else if (ctype == 0x004E4942u && binStart < 0) binStart = dstart;
                off = dstart + clen;
            }
            if (json == null) throw new FormatException("GLB has no JSON chunk.");
            if (binStart < 0) throw new FormatException("GLB has no BIN chunk.");

            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null) throw new FormatException("GLB JSON root is not an object.");

            var ctx = new Ctx();
            ctx.Glb = glb;
            ctx.BinStart = binStart;
            ctx.Root = root;
            ctx.Accessors = Arr(root, "accessors");
            ctx.Views = Arr(root, "bufferViews");
            ctx.Textures = Arr(root, "textures");
            ctx.Images = Arr(root, "images");
            if (ctx.Accessors == null || ctx.Views == null)
                throw new FormatException("GLB has no accessors or buffer views.");

            var materials = new List<Material>();
            var matList = Arr(root, "materials");
            if (matList != null)
            {
                for (int i = 0; i < matList.Count; i++)
                    materials.Add(MakeMaterial(Obj(matList[i]), ctx));
            }

            var nodes = Arr(root, "nodes");
            var meshList = Arr(root, "meshes");
            if (nodes == null || meshList == null)
                throw new FormatException("GLB has no nodes or meshes.");
            var meshCache = new MeshData[meshList.Count];

            List<object> roots = null;
            var scenes = Arr(root, "scenes");
            int sceneIndex = (int)Num(root, "scene", 0);
            if (scenes != null && sceneIndex >= 0 && sceneIndex < scenes.Count)
                roots = Arr(Obj(scenes[sceneIndex]), "nodes");

            if (roots == null || roots.Count == 0)
            {
                MakeNode(0, parent, ctx, nodes, meshCache, materials);
                return;
            }
            for (int i = 0; i < roots.Count; i++)
                MakeNode((int)(double)roots[i], parent, ctx, nodes, meshCache, materials);
        }

        static void MakeNode(int idx, Transform parent, Ctx ctx, List<object> nodes, MeshData[] meshCache, List<Material> materials)
        {
            if (idx < 0 || idx >= nodes.Count) return;
            var n = Obj(nodes[idx]);
            string name = Str(n, "name");
            var go = new GameObject(string.IsNullOrEmpty(name) ? "node" + idx : name);
            go.transform.SetParent(parent, false);

            var t = Floats(n, "translation");
            if (t != null && t.Length >= 3)
                go.transform.localPosition = new Vector3(-t[0], t[1], t[2]);
            var r = Floats(n, "rotation");
            if (r != null && r.Length >= 4)
                go.transform.localRotation = new Quaternion(r[0], -r[1], -r[2], r[3]);
            var s = Floats(n, "scale");
            if (s != null && s.Length >= 3)
                go.transform.localScale = new Vector3(s[0], s[1], s[2]);

            int mi = (int)Num(n, "mesh", -1);
            if (mi >= 0 && mi < meshCache.Length)
            {
                var data = GetMesh(mi, ctx, meshCache, materials);
                if (data != null && data.Mesh != null)
                {
                    go.AddComponent<MeshFilter>().sharedMesh = data.Mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = data.Materials;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    mr.receiveShadows = true;
                }
            }

            var kids = Arr(n, "children");
            if (kids != null)
            {
                for (int i = 0; i < kids.Count; i++)
                    MakeNode((int)(double)kids[i], go.transform, ctx, nodes, meshCache, materials);
            }
        }

        static MeshData GetMesh(int mi, Ctx ctx, MeshData[] cache, List<Material> materials)
        {
            if (cache[mi] != null) return cache[mi];

            var m = Obj(Arr(ctx.Root, "meshes")[mi]);
            var prims = Arr(m, "primitives");
            if (prims == null) return null;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var subTris = new List<int[]>();
            var mats = new List<Material>();
            bool allNormals = true;

            for (int pi = 0; pi < prims.Count; pi++)
            {
                var p = Obj(prims[pi]);
                if ((int)Num(p, "mode", 4) != 4) continue;
                var attr = Obj(p, "attributes");
                int posAcc = (int)Num(attr, "POSITION", -1);
                if (posAcc < 0) continue;

                Vector3[] pos = ReadVec3(ctx, posAcc);
                int nAcc = (int)Num(attr, "NORMAL", -1);
                int uAcc = (int)Num(attr, "TEXCOORD_0", -1);
                Vector3[] nor = nAcc >= 0 ? ReadVec3(ctx, nAcc) : null;
                Vector2[] uv = uAcc >= 0 ? ReadVec2(ctx, uAcc) : null;
                int[] tri = ReadIndices(ctx, (int)Num(p, "indices", -1), pos.Length);

                int baseVertex = verts.Count;
                for (int k = 0; k < pos.Length; k++)
                {
                    verts.Add(pos[k]);
                    norms.Add(nor != null && k < nor.Length ? nor[k] : Vector3.up);
                    uvs.Add(uv != null && k < uv.Length ? uv[k] : Vector2.zero);
                }
                if (nor == null) allNormals = false;
                for (int k = 0; k < tri.Length; k++) tri[k] += baseVertex;
                subTris.Add(tri);

                int matIndex = (int)Num(p, "material", -1);
                Material mat = matIndex >= 0 && matIndex < materials.Count ? materials[matIndex] : null;
                if (mat == null)
                    mat = SpillwayBuilder.Lit(new Color(0.5f, 0.5f, 0.5f), 0.6f, 0.2f, Color.black, 0f);
                mats.Add(mat);
            }
            if (subTris.Count == 0) return null;

            var mesh = new Mesh();
            mesh.name = Str(m, "name") ?? ("mesh" + mi);
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subTris.Count;
            for (int i = 0; i < subTris.Count; i++)
                mesh.SetTriangles(subTris[i], i);
            if (!allNormals) mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var data = new MeshData();
            data.Mesh = mesh;
            data.Materials = mats.ToArray();
            cache[mi] = data;
            return data;
        }

        // ------------------------------------------------------------------ binary accessors
        static int DataStart(Ctx ctx, Dictionary<string, object> acc)
        {
            int bv = (int)Num(acc, "bufferView", -1);
            if (bv < 0 || bv >= ctx.Views.Count) throw new FormatException("Accessor without buffer view.");
            var view = Obj(ctx.Views[bv]);
            return ctx.BinStart + (int)Num(view, "byteOffset", 0) + (int)Num(acc, "byteOffset", 0);
        }

        static int ViewStride(Ctx ctx, Dictionary<string, object> acc)
        {
            int bv = (int)Num(acc, "bufferView", -1);
            if (bv < 0 || bv >= ctx.Views.Count) return 0;
            return (int)Num(Obj(ctx.Views[bv]), "byteStride", 0);
        }

        static Vector3[] ReadVec3(Ctx ctx, int accIndex)
        {
            var acc = Obj(ctx.Accessors[accIndex]);
            if ((int)Num(acc, "componentType", 0) != 5126)
                throw new FormatException("VEC3 accessor must use float components.");
            int count = (int)Num(acc, "count", 0);
            int start = DataStart(ctx, acc);
            int stride = ViewStride(ctx, acc);
            if (stride <= 0) stride = 12;
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int p = start + i * stride;
                float x = BitConverter.ToSingle(ctx.Glb, p);
                float y = BitConverter.ToSingle(ctx.Glb, p + 4);
                float z = BitConverter.ToSingle(ctx.Glb, p + 8);
                result[i] = new Vector3(-x, y, z);
            }
            return result;
        }

        static Vector2[] ReadVec2(Ctx ctx, int accIndex)
        {
            var acc = Obj(ctx.Accessors[accIndex]);
            if ((int)Num(acc, "componentType", 0) != 5126)
                throw new FormatException("VEC2 accessor must use float components.");
            int count = (int)Num(acc, "count", 0);
            int start = DataStart(ctx, acc);
            int stride = ViewStride(ctx, acc);
            if (stride <= 0) stride = 8;
            var result = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                int p = start + i * stride;
                // glTF UV origin is top-left; Unity's is bottom-left.
                result[i] = new Vector2(BitConverter.ToSingle(ctx.Glb, p), 1f - BitConverter.ToSingle(ctx.Glb, p + 4));
            }
            return result;
        }

        static int[] ReadIndices(Ctx ctx, int accIndex, int vertexCount)
        {
            if (accIndex < 0)
            {
                var seq = new int[vertexCount];
                for (int i = 0; i < seq.Length; i++) seq[i] = i;
                return seq;
            }
            var acc = Obj(ctx.Accessors[accIndex]);
            int count = (int)Num(acc, "count", 0);
            int ct = (int)Num(acc, "componentType", 0);
            int size = ct == 5125 ? 4 : (ct == 5123 ? 2 : 1);
            int start = DataStart(ctx, acc);
            int stride = ViewStride(ctx, acc);
            if (stride <= 0) stride = size;
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                int p = start + i * stride;
                if (ct == 5125) result[i] = (int)BitConverter.ToUInt32(ctx.Glb, p);
                else if (ct == 5123) result[i] = BitConverter.ToUInt16(ctx.Glb, p);
                else result[i] = ctx.Glb[p];
            }
            return result;
        }

        // ------------------------------------------------------------------ materials / textures
        static Texture2D GetTexture(Ctx ctx, int textureIndex)
        {
            if (textureIndex < 0 || ctx.Textures == null || textureIndex >= ctx.Textures.Count) return null;
            Texture2D cached;
            if (ctx.TextureCache.TryGetValue(textureIndex, out cached)) return cached;

            Texture2D result = null;
            var tex = Obj(ctx.Textures[textureIndex]);
            int src = (int)Num(tex, "source", -1);
            if (src >= 0 && ctx.Images != null && src < ctx.Images.Count)
            {
                var img = Obj(ctx.Images[src]);
                int bv = (int)Num(img, "bufferView", -1);
                if (bv >= 0 && bv < ctx.Views.Count)
                {
                    var view = Obj(ctx.Views[bv]);
                    int start = ctx.BinStart + (int)Num(view, "byteOffset", 0);
                    int len = (int)Num(view, "byteLength", 0);
                    var bytes = new byte[len];
                    Buffer.BlockCopy(ctx.Glb, start, bytes, 0, len);
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                    if (t.LoadImage(bytes, true))
                    {
                        t.wrapMode = TextureWrapMode.Repeat;
                        t.filterMode = FilterMode.Trilinear;
                        t.anisoLevel = 4;
                        result = t;
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(t);
                    }
                }
            }
            ctx.TextureCache[textureIndex] = result;
            return result;
        }

        static Material MakeMaterial(Dictionary<string, object> m, Ctx ctx)
        {
            var pbr = Obj(m, "pbrMetallicRoughness");
            Color color = Color.white;
            float metallic = 1f;
            float roughness = 1f;
            Texture2D tex = null;
            if (pbr != null)
            {
                var f = Floats(pbr, "baseColorFactor");
                if (f != null && f.Length >= 4) color = new Color(f[0], f[1], f[2], f[3]);
                metallic = (float)Num(pbr, "metallicFactor", 1.0);
                roughness = (float)Num(pbr, "roughnessFactor", 1.0);
                var bt = Obj(pbr, "baseColorTexture");
                if (bt != null) tex = GetTexture(ctx, (int)Num(bt, "index", -1));
            }

            string alpha = Str(m, "alphaMode");
            float[] em = Floats(m, "emissiveFactor");
            float strength = 1f;
            var es = Obj(Obj(m, "extensions"), "KHR_materials_emissive_strength");
            if (es != null) strength = (float)Num(es, "emissiveStrength", 1.0);

            // The built-in pipeline has no environment reflections here, so fully metallic surfaces
            // would render almost black. Metalness is toned down on purpose.
            float smooth = (tex != null && roughness > 0.95f) ? 0.5f : 1f - roughness;
            float metal = tex != null ? 0.2f : Mathf.Clamp(metallic * 0.55f, 0f, 0.5f);

            Material mat;
            if (alpha == "BLEND")
                mat = SpillwayBuilder.Glass(color);
            else if (em != null && em.Length >= 3 && em[0] + em[1] + em[2] > 0.01f)
                mat = SpillwayBuilder.Lit(color, 1f - smooth, metal, new Color(em[0], em[1], em[2]), Mathf.Clamp(strength, 1f, 2.5f));
            else if (tex != null)
                mat = SpillwayBuilder.Textured(tex, color, smooth, metal);
            else
                mat = SpillwayBuilder.Lit(color, 1f - smooth, metal, Color.black, 0f);

            if (mat != null)
            {
                string name = Str(m, "name");
                mat.name = string.IsNullOrEmpty(name) ? "glb_material" : name;
            }
            return mat;
        }
    }
}
