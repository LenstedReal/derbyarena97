using System;
using System.Collections;
using System.Collections.Generic;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace DerbyArena.World
{
    /// <summary>
    /// Streams the real arena meshes (OBJ) and PBR textures from StreamingAssets, builds materials,
    /// and swaps out the box-based fallback shell once the meshes are in. Collision stays on the
    /// simple box shell (floor and walls); bridge and barriers get mesh colliders.
    /// </summary>
    public sealed class SpillwayAssets : MonoBehaviour
    {
        static readonly string[] Parts =
        {
            "channel_floor", "asphalt", "wall_west", "wall_east", "wall_north", "wall_south",
            "barriers", "bridge", "pipes", "catwalk"
        };

        Transform world;
        Dictionary<string, GameObject> coverage;
        GameObject dressing;

        public void Begin(Transform worldRoot, Dictionary<string, GameObject> shellCoverage, GameObject fallbackDressing)
        {
            world = worldRoot;
            coverage = shellCoverage;
            dressing = fallbackDressing;
            StartCoroutine(Run());
        }

        static string SetOf(string part)
        {
            if (part == "asphalt") return "asphalt";
            if (part == "pipes" || part == "catwalk" || part == "bridge") return "steel";
            return "concrete";
        }

        static bool CastsShadow(string part)
        {
            return part == "pipes" || part == "bridge" || part == "barriers" || part == "catwalk";
        }

        static string UrlOf(string file)
        {
            string path = Application.streamingAssetsPath + "/Models/spillway/" + file;
            return path.Contains("://") ? path : "file://" + path;
        }

        static IEnumerator Fetch(string file, Action<byte[], string> done)
        {
            using (var req = UnityWebRequest.Get(UrlOf(file)))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null)
                    done(req.downloadHandler.data, null);
                else
                    done(null, req.error);
            }
        }

        static Texture2D MakeTexture(byte[] bytes, bool linear)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
            if (!t.LoadImage(bytes, true))
            {
                Destroy(t);
                return null;
            }
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            return t;
        }

        static Material MakeMaterial(string set, Texture2D albedo, Texture2D normal)
        {
            if (albedo == null && normal == null) return null;
            Color tint = set == "steel" ? new Color(0.90f, 0.92f, 0.95f)
                : set == "asphalt" ? new Color(0.95f, 0.95f, 0.95f)
                : new Color(0.85f, 0.85f, 0.85f);
            float smooth = set == "steel" ? 0.5f : (set == "asphalt" ? 0.35f : 0.22f);
            float metal = set == "steel" ? 0.35f : 0f;
            return SpillwayBuilder.TexturedNormal(albedo, normal, tint, smooth, metal);
        }

        IEnumerator Run()
        {
            float t0 = Time.realtimeSinceStartup;
            var materials = new Dictionary<string, Material>();
            string[] sets = { "asphalt", "concrete", "steel" };

            for (int s = 0; s < sets.Length; s++)
            {
                string set = sets[s];
                byte[] albedoBytes = null;
                byte[] normalBytes = null;
                string albedoError = null;
                yield return Fetch(set + "_albedo.png", (b, e) => { albedoBytes = b; albedoError = e; });
                yield return Fetch(set + "_normal.png", (b, e) => { normalBytes = b; });
                Texture2D albedo = albedoBytes != null ? MakeTexture(albedoBytes, false) : null;
                Texture2D normal = normalBytes != null ? MakeTexture(normalBytes, true) : null;
                if (albedo == null)
                    Debug.LogWarning("DERBY: arena texture missing: " + set + "_albedo.png | " + albedoError);
                materials[set] = MakeMaterial(set, albedo, normal);
                yield return null;
            }

            int loaded = 0;
            bool barriersOk = false;
            bool bridgeOk = false;
            var failed = new List<string>();

            for (int i = 0; i < Parts.Length; i++)
            {
                string part = Parts[i];
                string text = null;
                string error = null;
                yield return Fetch(part + ".obj", (b, e) =>
                {
                    if (b != null) text = System.Text.Encoding.UTF8.GetString(b);
                    error = e;
                });
                if (text == null)
                {
                    failed.Add(part);
                    Debug.LogError("DERBY: arena mesh missing: " + part + " | " + error);
                    continue;
                }

                yield return null; // spread the parsing cost over frames

                Mesh mesh = null;
                try { mesh = ObjMesh.Parse(text, part); }
                catch (Exception ex) { Debug.LogError("DERBY: arena mesh parse failed: " + part + " | " + ex.Message); }
                if (mesh == null || mesh.vertexCount == 0)
                {
                    failed.Add(part);
                    continue;
                }

                Material mat;
                if (!materials.TryGetValue(SetOf(part), out mat) || mat == null)
                    mat = SpillwayBuilder.Lit(new Color(0.35f, 0.35f, 0.36f), 0.7f, 0.1f, Color.black, 0f);

                var go = new GameObject(part);
                go.transform.SetParent(world, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (mat != null) mr.sharedMaterial = mat;
                mr.shadowCastingMode = CastsShadow(part)
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
                go.isStatic = true;

                if (part == "bridge" || part == "barriers")
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                }
                if (part == "bridge") bridgeOk = true;
                if (part == "barriers") barriersOk = true;
                loaded++;

                GameObject shell;
                if (coverage != null && coverage.TryGetValue(part, out shell) && shell != null)
                {
                    var shellRenderer = shell.GetComponent<Renderer>();
                    if (shellRenderer != null) shellRenderer.enabled = false;
                }
                yield return null;
            }

            if (dressing != null && bridgeOk && barriersOk)
                dressing.SetActive(false);

            string summary = loaded + "/" + Parts.Length + " mesh, " +
                             (Time.realtimeSinceStartup - t0).ToString("0.0") + " sn";
            if (failed.Count > 0)
                summary += " | eksik: " + string.Join(",", failed.ToArray());
            DerbyStartupDiagnostics.Report("Arena", summary);
        }
    }
}
