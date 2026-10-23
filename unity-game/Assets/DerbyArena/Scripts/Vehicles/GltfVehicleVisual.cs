using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public void Begin(VehicleSpec spec)
        {
            StartCoroutine(Load(spec));
        }

        IEnumerator Load(VehicleSpec spec)
        {
            var rel = Path.Combine("Models/vehicles", spec.GlbFile);
            var path = Path.Combine(Application.streamingAssetsPath, rel);
            var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("GLB missing, using fallback hull: " + path + " " + req.error);
                BuildFallback(spec);
                yield break;
            }
            import = new GltfImport();
            var task = import.LoadGltfBinary(req.downloadHandler.data);
            while (!task.IsCompleted)
                yield return null;
            if (task.IsFaulted || !task.Result)
            {
                Debug.LogWarning("GLB parse failed: " + spec.GlbFile);
                BuildFallback(spec);
                yield break;
            }
            var root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.transform.localScale = Vector3.one * spec.Scale;
            var inst = import.InstantiateMainSceneAsync(root.transform);
            while (!inst.IsCompleted)
                yield return null;
            if (inst.IsFaulted || !inst.Result)
            {
                BuildFallback(spec);
                yield break;
            }
            Bind(root.transform, spec);
        }

        void Bind(Transform visual, VehicleSpec spec)
        {
            var wheels = new List<Transform>();
            var points = new List<Vector3>();
            Transform turret = null;
            Transform barrel = null;
            foreach (var t in visual.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Wheel_"))
                {
                    wheels.Add(t);
                    points.Add(transform.InverseTransformPoint(t.position));
                }
                else if (t.name == "Turret")
                    turret = t;
                else if (t.name == "Barrel")
                    barrel = t;
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
            var root = new GameObject("FallbackVisual");
            root.transform.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(1.7f, 0.5f, 3.4f) * spec.Scale;
            body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = DerbyArena.World.SpillwayBuilder.Lit(spec.Accent, 0.45f, 0.4f, Color.black, 0f);
            if (Drive != null)
                Drive.PlaceColliders(spec.WheelLocal, spec.WheelRadius);
        }

        void OnDestroy()
        {
            if (import != null)
                import.Dispose();
        }
    }
}
