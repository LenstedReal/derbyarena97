using System;
using System.Collections.Generic;
using DerbyArena.Core;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>Attaches the GLB model (shared template) to a car, binds wheels / turret and fits the hull.</summary>
    public sealed class VehicleVisual : MonoBehaviour
    {
        public Transform Turret { get; private set; }
        public Transform Barrel { get; private set; }
        public bool Loaded { get; private set; }

        ArcadeCar car;
        VehicleSpec spec;
        Transform visualRoot;
        Quaternion turretBase;
        float turretYaw;

        public void Begin(VehicleSpec vehicleSpec, ArcadeCar arcadeCar)
        {
            spec = vehicleSpec;
            car = arcadeCar;
            var vr = new GameObject("Visual");
            vr.transform.SetParent(transform, false);
            vr.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            vr.transform.localScale = Vector3.one * spec.Scale;
            visualRoot = vr.transform;
            GlbLibrary.Spawn(spec.GlbFile, visualRoot, OnModel);
        }

        void OnModel(GameObject model, string error)
        {
            if (this == null) return;
            if (model == null)
            {
                DerbyStartupDiagnostics.Report("Araç " + spec.DisplayName, "model yüklenemedi: " + error);
                BuildFallback();
                return;
            }
            try
            {
                Bind(model.transform);
                Loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: vehicle bind failed for " + spec.DisplayName + ": " + ex);
                Destroy(model);
                BuildFallback();
            }
        }

        void Bind(Transform model)
        {
            Transform fl = null, fr = null, rl = null, rr = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "Wheel_FL": fl = t; break;
                    case "Wheel_FR": fr = t; break;
                    case "Wheel_RL": rl = t; break;
                    case "Wheel_RR": rr = t; break;
                    case "Turret": Turret = t; break;
                    case "Barrel": Barrel = t; break;
                }
            }

            var wheelSet = new HashSet<Transform>();
            if (fl != null && fr != null && rl != null && rr != null)
            {
                wheelSet.Add(fl); wheelSet.Add(fr); wheelSet.Add(rl); wheelSet.Add(rr);
            }
            FitHullTo(model, wheelSet);
            if (wheelSet.Count == 4 && car != null) car.BindWheels(new[] { fl, fr, rl, rr });
            if (Turret != null)
            {
                if (Barrel != null && Barrel.parent != Turret) Barrel.SetParent(Turret, true);
                turretBase = Turret.localRotation;
            }
        }

        void FitHullTo(Transform model, HashSet<Transform> wheels)
        {
            bool any = false;
            var b = new Bounds();
            Matrix4x4 toLocal = transform.worldToLocalMatrix;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                bool isWheel = false;
                foreach (var w in wheels) if (mf.transform == w || mf.transform.IsChildOf(w)) { isWheel = true; break; }
                if (isWheel || mf.transform.name == "Turret" || mf.transform.name == "Barrel") continue;
                Bounds mb = mf.sharedMesh.bounds;
                Matrix4x4 m = toLocal * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z);
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            if (any && car != null) car.FitHull(b);
        }

        void BuildFallback()
        {
            if (visualRoot == null) return;
            var paint = Mats.Lit(spec.Accent, 0.4f, 0.3f, Color.black, 0f);
            var dark = Mats.Lit(new Color(0.07f, 0.07f, 0.08f), 0.6f, 0.2f, Color.black, 0f);
            var glass = Mats.Glass(new Color(0.1f, 0.14f, 0.16f, 0.6f));
            var cube = MeshFactory.UnitCube();
            float wr = spec.WheelRadius;
            MeshFactory.Make("Hull", cube, paint, visualRoot, new Vector3(0f, wr + 0.25f, 0f), new Vector3(spec.BodySize.x, 0.55f, spec.BodySize.z));
            MeshFactory.Make("Cabin", cube, glass, visualRoot, new Vector3(0f, wr + 0.75f, 0.3f), new Vector3(spec.BodySize.x * 0.75f, 0.45f, spec.BodySize.z * 0.4f));
            var wheelMesh = MeshFactory.Cylinder(wr, 0.36f, 18);
            var ws = new Transform[4];
            Vector3 f = spec.WheelFront, r = spec.WheelRear;
            Vector3[] pos = { new Vector3(f.x, f.y, -f.z), new Vector3(-f.x, f.y, -f.z), new Vector3(r.x, r.y, -r.z), new Vector3(-r.x, r.y, -r.z) };
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject("Wheel");
                w.transform.SetParent(visualRoot, false);
                w.transform.localPosition = pos[i];
                var mesh = MeshFactory.Make("Tyre", wheelMesh, dark, w.transform, Vector3.zero, Vector3.one);
                mesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ws[i] = w.transform;
            }
            var turret = new GameObject("Turret");
            turret.transform.SetParent(visualRoot, false);
            turret.transform.localPosition = new Vector3(0f, wr + 0.85f, 0f);
            MeshFactory.Make("TurretBody", cube, dark, turret.transform, Vector3.zero, new Vector3(0.7f, 0.3f, 0.9f));
            var barrel = MeshFactory.Make("Barrel", cube, dark, turret.transform, new Vector3(0f, 0.05f, -0.8f), new Vector3(0.14f, 0.14f, 1.1f));
            Turret = turret.transform;
            Barrel = barrel.transform;
            turretBase = Turret.localRotation;
            if (car != null) car.BindWheels(ws);
            Loaded = true;
        }

        /// <summary>Rotate the turret toward a world point (yaw only, smoothed).</summary>
        public void AimAt(Vector3 world, float dt)
        {
            if (Turret == null) return;
            Vector3 local = transform.InverseTransformPoint(world);
            float target = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            turretYaw = Mathf.MoveTowardsAngle(turretYaw, target, 420f * dt);
            Turret.localRotation = Quaternion.AngleAxis(turretYaw, Vector3.up) * turretBase;
        }

        /// <summary>World position of the gun muzzle.</summary>
        public Vector3 Muzzle()
        {
            Vector3 dir = Quaternion.AngleAxis(turretYaw, transform.up) * transform.forward;
            if (Barrel != null) return Barrel.position + dir * 0.75f * spec.Scale;
            return transform.position + transform.up * spec.MuzzleHeight * spec.Scale + transform.forward * spec.MuzzleForward * spec.Scale;
        }

        public Vector3 MuzzleDirection()
        {
            return Quaternion.AngleAxis(turretYaw, transform.up) * transform.forward;
        }
    }
}
