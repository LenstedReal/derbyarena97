using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DerbyArena.Combat;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;

namespace DerbyArena.Vehicles
{
    public sealed class GltfVehicleVisual : MonoBehaviour
    {
        public WheelDrive Drive;
        public WeaponRig Weapons;

        GltfImport import;
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

        IEnumerator Load(VehicleSpec spec)
        {
            string rel = Path.Combine("Models", "vehicles", spec.GlbFile);
            string path = Path.Combine(Application.streamingAssetsPath, rel);
            byte[] glbBytes = null;

            using (var req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(
                        "DERBY: GLB request failed; using fallback vehicle. " +
                        spec.GlbFile + " | " + req.error);
                    BuildFallback(spec);
                    yield break;
                }

                glbBytes = req.downloadHandler != null ? req.downloadHandler.data : null;
            }

            if (glbBytes == null || glbBytes.Length < 20)
            {
                Debug.LogWarning(
                    "DERBY: GLB is empty or truncated; using fallback vehicle: " +
                    spec.GlbFile);
                BuildFallback(spec);
                yield break;
            }

            // Validate the GLB container before passing it to glTFast.
            if (glbBytes[0] != (byte)'g' || glbBytes[1] != (byte)'l' ||
                glbBytes[2] != (byte)'T' || glbBytes[3] != (byte)'F')
            {
                Debug.LogError(
                    "DERBY: invalid GLB magic header; using fallback vehicle: " +
                    spec.GlbFile);
                BuildFallback(spec);
                yield break;
            }

            Task<bool> loadTask;
            try
            {
                import = new GltfImport();
                loadTask = import.Load(glbBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "DERBY: glTFast could not initialize " + spec.GlbFile +
                    "; using fallback vehicle. " + ex);
                BuildFallback(spec);
                yield break;
            }

            while (!loadTask.IsCompleted)
                yield return null;

            if (loadTask.IsFaulted || loadTask.IsCanceled ||
                !loadTask.IsCompletedSuccessfully || !loadTask.Result)
            {
                string detail = loadTask.Exception != null
                    ? loadTask.Exception.GetBaseException().ToString()
                    : "GLB load task failed or was cancelled.";
                Debug.LogError(
                    "DERBY: GLB import failed for " + spec.GlbFile +
                    "; using fallback vehicle. " + detail);
                DisposeImport();
                BuildFallback(spec);
                yield break;
            }

            var root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.transform.localScale = Vector3.one * spec.Scale;

            Task<bool> instantiateTask;
            try
            {
                instantiateTask = import.InstantiateMainSceneAsync(root.transform);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "DERBY: GLB scene instantiation failed for " +
                    spec.GlbFile + "; using fallback vehicle. " + ex);
                Destroy(root);
                DisposeImport();
                BuildFallback(spec);
                yield break;
            }

            while (!instantiateTask.IsCompleted)
                yield return null;

            if (instantiateTask.IsFaulted || instantiateTask.IsCanceled ||
                !instantiateTask.IsCompletedSuccessfully || !instantiateTask.Result)
            {
                string detail = instantiateTask.Exception != null
                    ? instantiateTask.Exception.GetBaseException().ToString()
                    : "GLB scene instantiation returned false or was cancelled.";
                Debug.LogError(
                    "DERBY: GLB scene could not be instantiated for " +
                    spec.GlbFile + "; using fallback vehicle. " + detail);
                Destroy(root);
                DisposeImport();
                BuildFallback(spec);
                yield break;
            }

            try
            {
                Bind(root.transform, spec);
                visualCreated = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "DERBY: GLB visual binding failed for " + spec.GlbFile +
                    "; using fallback vehicle. " + ex);
                Destroy(root);
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

            // Do not create multiple fallback bodies if more than one import
            // failure path is reached.
            if (transform.Find("FallbackVisual") != null)
                return;

            var root = new GameObject("FallbackVisual");
            root.transform.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "FallbackHull";
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localScale =
                new Vector3(1.7f, 0.5f, 3.4f) * spec.Scale;
            body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var fallbackMaterial = DerbyArena.World.SpillwayBuilder.Lit(
                spec.Accent, 0.45f, 0.4f, Color.black, 0f);
            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null && fallbackMaterial != null)
                bodyRenderer.sharedMaterial = fallbackMaterial;

            if (Drive != null)
                Drive.PlaceColliders(spec.WheelLocal, spec.WheelRadius);
        }

        void DisposeImport()
        {
            if (import == null)
                return;

            import.Dispose();
            import = null;
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            DisposeImport();
        }
    }
}
