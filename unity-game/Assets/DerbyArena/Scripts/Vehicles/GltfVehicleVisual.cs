using System;
using System.Collections;
using System.Collections.Generic;
using DerbyArena.Combat;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Loads a vehicle model (GLB) from StreamingAssets with the built-in MiniGlb importer and binds
    /// wheels / turret to the physics vehicle. If anything fails a visible fallback body is built
    /// and the reason is reported on screen (DerbyStartupDiagnostics).
    /// </summary>
    public sealed class GltfVehicleVisual : MonoBehaviour
    {
        public WheelDrive Drive;
        public WeaponRig Weapons;

        bool visualCreated;

        public void Begin(VehicleSpec spec)
        {
            if (spec == null)
            {
                Debug.LogError("DERBY: vehicle visual received a null VehicleSpec.");
                return;
            }

            StartCoroutine(Load(spec));
        }

        static string UrlOf(string file)
        {
            string path = Application.streamingAssetsPath + "/Models/vehicles/" + file;
            return path.Contains("://") ? path : "file://" + path;
        }

        IEnumerator Load(VehicleSpec spec)
        {
            byte[] glbBytes = null;
            string requestError = null;

            using (var req = UnityWebRequest.Get(UrlOf(spec.GlbFile)))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null)
                    glbBytes = req.downloadHandler.data;
                else
                    requestError = req.error;
            }

            if (glbBytes == null || glbBytes.Length < 20)
            {
                Debug.LogError("DERBY: GLB could not be read: " + spec.GlbFile + " | " + requestError);
                DerbyStartupDiagnostics.Report("Araç " + spec.DisplayName, "GLB okunamadı → yedek gövde");
                BuildFallback(spec);
                yield break;
            }

            // Let one frame pass so the (heavier) import does not stack on top of other startup work.
            yield return null;

            var root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.transform.localScale = Vector3.one * spec.Scale;

            bool ok = false;
            string failure = null;
            try
            {
                MiniGlb.Build(glbBytes, root.transform);
                Bind(root.transform, spec);
                ok = true;
            }
            catch (Exception ex)
            {
                failure = ex.Message;
                Debug.LogError("DERBY: GLB import failed for " + spec.GlbFile + "; using fallback vehicle. " + ex);
            }

            if (ok)
            {
                visualCreated = true;
                DerbyStartupDiagnostics.Report("Araç " + spec.DisplayName, "GLB tamam");
            }
            else
            {
                Destroy(root);
                DerbyStartupDiagnostics.Report("Araç " + spec.DisplayName, "GLB hata (" + failure + ") → yedek gövde");
                BuildFallback(spec);
            }
        }

        void Bind(Transform visual, VehicleSpec spec)
        {
            var wheels = new List<Transform>(4);
            var points = new List<Vector3>(4);
            Transform turret = null;
            Transform barrel = null;

            foreach (var t in visual.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Wheel_", StringComparison.Ordinal))
                {
                    wheels.Add(t);
                    points.Add(transform.InverseTransformPoint(t.position));
                }
                else if (t.name == "Turret")
                {
                    turret = t;
                }
                else if (t.name == "Barrel")
                {
                    barrel = t;
                }
            }

            if (points.Count == 4 && Drive != null)
                Drive.PlaceColliders(points.ToArray(), spec.WheelRadius * spec.Scale);

            for (int i = 0; i < wheels.Count; i++)
                wheels[i].SetParent(transform, true);

            if (Drive != null)
                Drive.BindWheelVisuals(wheels.ToArray());

            if (Weapons != null)
            {
                if (turret != null && barrel != null && barrel.parent != turret)
                    barrel.SetParent(turret, true);

                Weapons.Muzzle = barrel != null ? barrel : turret;
            }
        }

        void BuildFallback(VehicleSpec spec)
        {
            if (visualCreated)
                return;

            // Do not create multiple fallback bodies if more than one failure path is reached.
            if (transform.Find("FallbackVisual") != null)
                return;

            var root = new GameObject("FallbackVisual");
            root.transform.SetParent(transform, false);

            var paint = DerbyArena.World.SpillwayBuilder.Lit(spec.Accent, 0.45f, 0.3f, Color.black, 0f);
            var dark = DerbyArena.World.SpillwayBuilder.Lit(new Color(0.06f, 0.06f, 0.07f), 0.6f, 0.2f, Color.black, 0f);

            Part(root.transform, "FallbackHull", new Vector3(0f, 0.7f, 0f),
                new Vector3(1.9f, 0.55f, 4.2f) * spec.Scale, paint);
            Part(root.transform, "FallbackCabin", new Vector3(0f, 1.2f, -0.2f),
                new Vector3(1.6f, 0.5f, 1.9f) * spec.Scale, dark);

            if (Drive != null)
                Drive.PlaceColliders(spec.WheelLocal, spec.WheelRadius);
        }

        static void Part(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }
    }
}
