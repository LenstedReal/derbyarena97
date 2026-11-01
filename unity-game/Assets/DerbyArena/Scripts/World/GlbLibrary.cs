using System;
using System.Collections;
using System.Collections.Generic;
using DerbyArena.Core;
using DerbyArena.Vehicles;
using UnityEngine;
using UnityEngine.Networking;

namespace DerbyArena.World
{
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

        public static bool IsReady(string relativePath)
        {
            return templates.ContainsKey(relativePath) || failures.ContainsKey(relativePath);
        }

        /// <summary>Starts loading if needed. Callback receives a fresh clone (or null + error).</summary>
        public static void Spawn(string relativePath, Transform parent, Action<GameObject, string> done)
        {
            Ensure();
            instance.StartCoroutine(instance.SpawnRoutine(relativePath, parent, done));
        }

        public static void Preload(string relativePath)
        {
            Ensure();
            if (IsReady(relativePath) || loading.Contains(relativePath)) return;
            instance.StartCoroutine(instance.Load(relativePath));
        }

        IEnumerator SpawnRoutine(string relativePath, Transform parent, Action<GameObject, string> done)
        {
            if (!IsReady(relativePath))
            {
                if (!loading.Contains(relativePath)) yield return Load(relativePath);
                while (loading.Contains(relativePath)) yield return null;
            }

            GameObject template;
            if (templates.TryGetValue(relativePath, out template) && template != null)
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
                failures.TryGetValue(relativePath, out err);
                if (done != null) done(null, err ?? "unknown error");
            }
        }

        IEnumerator Load(string relativePath)
        {
            loading.Add(relativePath);
            byte[] bytes = null;
            string error = null;
            using (var req = UnityWebRequest.Get(UrlOf(relativePath)))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null)
                    bytes = req.downloadHandler.data;
                else
                    error = req.error;
            }
            yield return null;

            if (bytes == null || bytes.Length < 20)
            {
                failures[relativePath] = "okunamadı: " + error;
                Debug.LogError("DERBY: GLB could not be read: " + relativePath + " | " + error);
            }
            else
            {
                var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(relativePath));
                root.transform.SetParent(holder, false);
                try
                {
                    MiniGlb.Build(bytes, root.transform);
                    templates[relativePath] = root;
                }
                catch (Exception ex)
                {
                    Destroy(root);
                    failures[relativePath] = ex.Message;
                    Debug.LogError("DERBY: GLB import failed: " + relativePath + " | " + ex);
                }
            }
            DerbyStartupDiagnostics.Report("GLB " + relativePath, templates.ContainsKey(relativePath) ? "tamam" : "HATA");
            loading.Remove(relativePath);
        }
    }
}
