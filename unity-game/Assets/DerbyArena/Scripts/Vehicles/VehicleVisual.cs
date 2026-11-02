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

        /// <summary>Vehicle renderers live on this layer so the camera rim light only touches cars.</summary>
        public const int VehicleLayer = 9;

        ArcadeCar car;
        VehicleSpec spec;
        Transform visualRoot;
        Quaternion turretBase;
        float turretYaw;
        float roll, pitch;
        Vector3 lastVel;
        Renderer[] renderers = new Renderer[0];
        MaterialPropertyBlock block;
        float flashTimer;
        bool flashOn;
        TrailRenderer[] skids;
        Transform shadow;
        float nitroTimer;
        bool isPlayer;
        int quality;

        static Material skidMat;
        static Mesh beamMesh;
        static Material beamMat;
        VehicleHealth health;
        GameObject[] beams;

        public void Begin(VehicleSpec vehicleSpec, ArcadeCar arcadeCar)
        {
            spec = vehicleSpec;
            car = arcadeCar;
            var vr = new GameObject("Visual");
            vr.transform.SetParent(transform, false);
            vr.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            vr.transform.localScale = Vector3.one * spec.Scale;
            visualRoot = vr.transform;
            isPlayer = spec.Role == DriverRole.Player;
            quality = Save.Data.Quality;
            GlbLibrary.Spawn(spec.GlbFile, spec.ModelOptions(), visualRoot, OnModel);
            BuildShadow();
            if (quality > 0 || isPlayer) BuildSkids();
            health = GetComponent<VehicleHealth>();
            if (quality > 0) BuildBeams();
            if (isPlayer && (quality == 2 || (quality == 1 && Atmosphere.CurrentBeamStrength >= 0.4f))) BuildHeadlight();
        }

        /// <summary>Headlight beams as in the reference video: two soft additive cones, stronger at night.</summary>
        void BuildBeams()
        {
            if (beamMat == null) beamMat = Mats.FxAdditive();
            if (beamMat == null) return;
            float strength = Atmosphere.CurrentBeamStrength * (isPlayer ? 1f : 0.8f);
            if (beamMat.HasProperty("_TintColor")) beamMat.SetColor("_TintColor", new Color(0.5f, 0.47f, 0.4f, Mathf.Clamp01(strength)));
            var mesh = BeamMesh();
            beams = new GameObject[2];
            float sc = spec.Scale;
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var go = MeshFactory.Make("HeadlightBeam", mesh, beamMat, transform,
                    new Vector3(side * spec.BodySize.x * 0.3f * sc, 0.72f * sc, spec.BodySize.z * 0.47f * sc), Vector3.one * sc);
                go.transform.localRotation = Quaternion.Euler(4f, side * 3f, 0f);
                var r = go.GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                beams[i] = go;
            }
        }

        static Mesh BeamMesh()
        {
            if (beamMesh != null) return beamMesh;
            const int seg = 12;
            const float length = 9f, r0 = 0.12f, r1 = 2.1f;
            var v = new Vector3[(seg + 1) * 2];
            var c = new Color[v.Length];
            var uv = new Vector2[v.Length];
            var tri = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                v[i * 2] = new Vector3(cs * r0, sn * r0 * 0.6f, 0f);
                v[i * 2 + 1] = new Vector3(cs * r1, sn * r1 * 0.45f - 0.35f, length);
                c[i * 2] = new Color(1f, 0.92f, 0.75f, 1f);
                c[i * 2 + 1] = new Color(1f, 0.92f, 0.75f, 0f);
                uv[i * 2] = new Vector2(0.5f, 0.5f);
                uv[i * 2 + 1] = new Vector2(0.5f, 0.5f);
            }
            for (int i = 0; i < seg; i++)
            {
                int k = i * 6;
                int a0 = i * 2, b0 = i * 2 + 1, a1 = i * 2 + 2, b1 = i * 2 + 3;
                tri[k] = a0; tri[k + 1] = b0; tri[k + 2] = a1;
                tri[k + 3] = a1; tri[k + 4] = b0; tri[k + 5] = b1;
            }
            beamMesh = new Mesh();
            beamMesh.name = "HeadlightBeam";
            beamMesh.vertices = v;
            beamMesh.colors = c;
            beamMesh.uv = uv;
            beamMesh.triangles = tri;
            beamMesh.RecalculateBounds();
            return beamMesh;
        }

        void BuildShadow()
        {
            var mat = Mats.BlobShadow();
            if (mat == null) return;
            var go = MeshFactory.Make("BlobShadow", MeshFactory.UnitQuad(), mat, null, Vector3.zero, Vector3.one);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.transform.SetParent(transform.parent, false);
            shadow = go.transform;
            shadow.localScale = new Vector3(spec.BodySize.x * 1.35f * spec.Scale, 1f, spec.BodySize.z * 1.25f * spec.Scale);
        }

        void BuildSkids()
        {
            if (skidMat == null)
            {
                skidMat = Mats.FxSoft();
                if (skidMat != null) skidMat.color = new Color(0.03f, 0.03f, 0.03f, 0.55f);
            }
            if (skidMat == null) return;
            skids = new TrailRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Skid");
                go.transform.SetParent(transform.parent, false);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var tr = go.AddComponent<TrailRenderer>();
                tr.sharedMaterial = skidMat;
                tr.time = isPlayer ? 3.5f : 2f;
                tr.minVertexDistance = 0.35f;
                tr.startWidth = 0.32f * spec.Scale;
                tr.endWidth = 0.28f * spec.Scale;
                tr.alignment = LineAlignment.TransformZ;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                tr.emitting = false;
                tr.autodestruct = false;
                skids[i] = tr;
            }
        }

        void BuildHeadlight()
        {
            var go = new GameObject("Headlight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.0f * spec.Scale, spec.BodySize.z * 0.5f);
            go.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 26f;
            l.spotAngle = 62f;
            l.intensity = 1.6f;
            l.color = new Color(1f, 0.92f, 0.78f);
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
        }

        /// <summary>White hit flash on all vehicle renderers (MaterialPropertyBlock, no material copies).</summary>
        public void Flash()
        {
            if (renderers.Length == 0) return;
            if (block == null) block = new MaterialPropertyBlock();
            block.Clear();
            block.SetColor("_Color", new Color(1.9f, 1.6f, 1.4f, 1f));
            block.SetColor("_EmissionColor", new Color(1.2f, 0.75f, 0.5f));
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].SetPropertyBlock(block);
            flashTimer = 0.07f;
            flashOn = true;
        }

        void ClearFlash()
        {
            flashOn = false;
            if (block == null) block = new MaterialPropertyBlock();
            block.Clear();
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].SetPropertyBlock(block);
        }

        void CollectRenderers()
        {
            if (visualRoot == null) return;
            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            var list = new List<Renderer>(renderers);
            if (car != null)
                foreach (var r in car.GetComponentsInChildren<Renderer>(true))
                    if (!list.Contains(r) && r.GetComponent<TrailRenderer>() == null && r.GetComponent<ParticleSystem>() == null) list.Add(r);
            renderers = list.ToArray();
            list.RemoveAll(x => x == null || x.gameObject.name == "HeadlightBeam");
            renderers = list.ToArray();
            foreach (var r in renderers) r.gameObject.layer = VehicleLayer;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || car == null) return;
            if (beams != null && health != null && health.Dead)
            {
                for (int i = 0; i < beams.Length; i++) if (beams[i] != null) beams[i].SetActive(false);
                beams = null;
            }
            if (flashOn)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) ClearFlash();
            }

            Vector3 vel = car.Body != null ? car.Body.linearVelocity : Vector3.zero;
            Vector3 accel = (vel - lastVel) / dt;
            lastVel = vel;
            Vector3 la = transform.InverseTransformDirection(accel);
            float targetRoll = Mathf.Clamp(la.x * 0.35f, -5f, 5f);
            float targetPitch = Mathf.Clamp(-la.z * 0.22f, -3f, 3f);
            if (!car.Grounded) { targetRoll = 0f; targetPitch = 0f; }
            float k = 1f - Mathf.Exp(-6f * dt);
            roll = Mathf.Lerp(roll, targetRoll, k);
            pitch = Mathf.Lerp(pitch, targetPitch, k);
            if (visualRoot != null) visualRoot.localRotation = Quaternion.Euler(pitch, 180f, roll);

            if (shadow != null)
            {
                Vector3 p = transform.position;
                RaycastHit hit;
                bool ground = Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (ground && hit.rigidbody == car.Body)
                    ground = Physics.Raycast(hit.point + Vector3.down * 0.05f, Vector3.down, out hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                shadow.gameObject.SetActive(ground);
                if (ground)
                {
                    shadow.position = hit.point + hit.normal * 0.04f;
                    shadow.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                }
            }

            if (skids != null)
            {
                bool slide = car.Grounded && (car.Drifting || car.Handbrake || Mathf.Abs(car.LateralSpeed) > 4.5f) && vel.sqrMagnitude > 16f;
                for (int i = 0; i < 2; i++)
                {
                    var tr = skids[i];
                    if (tr == null) continue;
                    bool grounded = car.WheelGrounded(2 + i);
                    tr.transform.position = car.WheelContact(2 + i) + Vector3.up * 0.04f;
                    tr.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    tr.emitting = slide && grounded;
                }
            }

            if (car.NitroActive)
            {
                nitroTimer -= dt;
                if (nitroTimer <= 0f)
                {
                    nitroTimer = 0.03f;
                    Vector3 back = -transform.forward;
                    Vector3 basePos = transform.position + transform.up * (0.55f * spec.Scale) + back * (spec.BodySize.z * 0.5f * spec.Scale);
                    Vector3 side = transform.right * (spec.BodySize.x * 0.22f * spec.Scale);
                    DerbyArena.Combat.Fx.Nitro(basePos + side, back);
                    DerbyArena.Combat.Fx.Nitro(basePos - side, back);
                }
            }
        }

        void OnDestroy()
        {
            if (shadow != null) Destroy(shadow.gameObject);
            if (skids != null)
                foreach (var tr in skids) if (tr != null) Destroy(tr.gameObject, tr.time);
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
                CollectRenderers();
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
            CollectRenderers();
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
