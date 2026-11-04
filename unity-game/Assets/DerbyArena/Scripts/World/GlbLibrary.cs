using System;
using System.Collections;
using System.Collections.Generic;
using DerbyArena.Core;
using DerbyArena.Vehicles;
using UnityEngine;
using UnityEngine.Networking;

namespace DerbyArena.World
{
    /// <summary>Import options. Different options for the same file produce a separate shared template.</summary>
    public sealed class GlbOptions
    {
        public string Key = "";
        public bool VertexColors;
        public bool Recolor;
        public Color Paint = Color.white;
        public float EmissionBoost = 1f;
    }

    /// <summary>
    /// Loads GLB files from StreamingAssets once, builds a template hierarchy under a persistent
    /// inactive holder and hands out clones. Meshes, materials and textures are shared, so five
    /// enemies of the same model cost one import, and replaying a level costs no import at all.
    /// </summary>
    public sealed class GlbLibrary : MonoBehaviour
    {
        static GlbLibrary instance;
        static Transform holder;
        static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, string> failures = new Dictionary<string, string>();
        static readonly Dictionary<string, byte[]> bytesCache = new Dictionary<string, byte[]>();
        static readonly HashSet<string> loading = new HashSet<string>();

        static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("GlbLibrary");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GlbLibrary>();
            var h = new GameObject("GlbTemplates");
            DontDestroyOnLoad(h);
            h.SetActive(false);
            holder = h.transform;
        }

        public static string UrlOf(string relativePath)
        {
            string path = Application.streamingAssetsPath + "/Models/" + relativePath;
            return path.Contains("://") ? path : "file://" + path;
        }

        static string KeyOf(string relativePath, GlbOptions options)
        {
            return options == null || string.IsNullOrEmpty(options.Key) ? relativePath : relativePath + "#" + options.Key;
        }

        public static bool IsReady(string relativePath, GlbOptions options)
        {
            string key = KeyOf(relativePath, options);
            return templates.ContainsKey(key) || failures.ContainsKey(key);
        }

        public static void Spawn(string relativePath, Transform parent, Action<GameObject, string> done)
        {
            Spawn(relativePath, null, parent, done);
        }

        /// <summary>Starts loading if needed. Callback receives a fresh clone (or null + error).</summary>
        public static void Spawn(string relativePath, GlbOptions options, Transform parent, Action<GameObject, string> done)
        {
            Ensure();
            instance.StartCoroutine(instance.SpawnRoutine(relativePath, options, parent, done));
        }

        public static void Preload(string relativePath, GlbOptions options)
        {
            Ensure();
            string key = KeyOf(relativePath, options);
            if (templates.ContainsKey(key) || failures.ContainsKey(key) || loading.Contains(key)) return;
            instance.StartCoroutine(instance.Load(relativePath, options));
        }

        IEnumerator SpawnRoutine(string relativePath, GlbOptions options, Transform parent, Action<GameObject, string> done)
        {
            string key = KeyOf(relativePath, options);
            if (!templates.ContainsKey(key) && !failures.ContainsKey(key))
            {
                if (!loading.Contains(key)) yield return Load(relativePath, options);
                while (loading.Contains(key)) yield return null;
            }

            GameObject template;
            if (templates.TryGetValue(key, out template) && template != null)
            {
                if (parent == null)
                {
                    if (done != null) done(null, "parent destroyed");
                    yield break;
                }
                var clone = Instantiate(template);
                clone.name = template.name;
                clone.transform.SetParent(parent, false);
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;
                if (done != null) done(clone, null);
            }
            else
            {
                string err;
                failures.TryGetValue(key, out err);
                if (done != null) done(null, err ?? "unknown error");
            }
        }

        IEnumerator Load(string relativePath, GlbOptions options)
        {
            string key = KeyOf(relativePath, options);
            loading.Add(key);
            byte[] bytes;
            string error = null;
            if (!bytesCache.TryGetValue(relativePath, out bytes))
            {
                using (var req = UnityWebRequest.Get(UrlOf(relativePath)))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null)
                        bytes = req.downloadHandler.data;
                    else
                        error = req.error;
                }
                // keep raw bytes only while other variants of the same file may still be requested
                if (bytes != null && options != null) bytesCache[relativePath] = bytes;
            }
            yield return null;

            if (bytes == null || bytes.Length < 20)
            {
                failures[key] = Loc.T("okunamadı: ", "unreadable: ") + error;
                Debug.LogError("DERBY: GLB could not be read: " + relativePath + " | " + error);
            }
            else
            {
                var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(relativePath));
                root.transform.SetParent(holder, false);
                // staged import (4.0): textures and meshes are built one per frame so big models never freeze a frame
                MiniGlb.Import import = null;
                try { import = MiniGlb.Prepare(bytes, options); }
                catch (Exception ex) { failures[key] = ex.Message; Debug.LogError("DERBY: GLB parse failed: " + key + " | " + ex); }
                if (import != null)
                {
                    var texSteps = MiniGlb.PreloadTextures(import).GetEnumerator();
                    while (true)
                    {
                        bool more;
                        try { more = texSteps.MoveNext(); }
                        catch (Exception ex) { failures[key] = ex.Message; Debug.LogError("DERBY: GLB texture failed: " + key + " | " + ex); break; }
                        if (!more) break;
                        yield return null;
                    }
                }
                if (import != null && !failures.ContainsKey(key))
                {
                    var meshSteps = MiniGlb.PreloadMeshes(import).GetEnumerator();
                    while (true)
                    {
                        bool more;
                        try { more = meshSteps.MoveNext(); }
                        catch (Exception ex) { failures[key] = ex.Message; Debug.LogError("DERBY: GLB mesh failed: " + key + " | " + ex); break; }
                        if (!more) break;
                        yield return null;
                    }
                }
                if (import != null && !failures.ContainsKey(key))
                {
                    try
                    {
                        MiniGlb.Finish(import, root.transform);
                        templates[key] = root;
                    }
                    catch (Exception ex)
                    {
                        failures[key] = ex.Message;
                        Debug.LogError("DERBY: GLB import failed: " + key + " | " + ex);
                    }
                }
                if (!templates.ContainsKey(key)) Destroy(root);
            }
            DerbyStartupDiagnostics.Report("GLB " + key, templates.ContainsKey(key) ? "tamam" : "HATA");
            loading.Remove(key);
        }
    }
}
