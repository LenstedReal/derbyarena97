using System.Collections.Generic;
using DerbyArena.Audio;
using DerbyArena.Vehicles;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Combat
{
    /// <summary>Pooled homing rockets. Explosion damage is applied once per vehicle (fixes multi-collider multi-hit).</summary>
    public sealed class Projectiles : MonoBehaviour
    {
        sealed class Rocket
        {
            public GameObject Go;
            public TrailRenderer Trail;
            public bool Active;
            public Vector3 Velocity;
            public float Life;
            public float Damage;
            public float Radius;
            public VehicleHealth Owner;
            public Rigidbody OwnerBody;
            public Transform Target;
            public bool Shell;          // cannon round: straight, fast, smaller blast
        }

        static Projectiles inst;
        readonly List<Rocket> rockets = new List<Rocket>();
        readonly RaycastHit[] hits = new RaycastHit[8];
        readonly Collider[] overlap = new Collider[32];
        readonly HashSet<VehicleHealth> damaged = new HashSet<VehicleHealth>();

        public static void Init(Transform parent)
        {
            var go = new GameObject("Projectiles");
            go.transform.SetParent(parent, false);
            inst = go.AddComponent<Projectiles>();
            inst.Build();
        }

        void Build()
        {
            var body = Mats.Lit(new Color(0.25f, 0.26f, 0.28f), 0.4f, 0.4f, Color.black, 0f);
            var glow = Mats.Glow(new Color(1f, 0.5f, 0.15f), 3f);
            var cyl = MeshFactory.UnitCylinder();
            var trailMat = Mats.FxAdditive();
            for (int i = 0; i < 16; i++)
            {
                var go = new GameObject("Rocket");
                go.transform.SetParent(transform, false);
                var shell = MeshFactory.Make("Shell", cyl, body, go.transform, Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f));
                shell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var tail = MeshFactory.Make("Tail", cyl, glow, go.transform, new Vector3(0f, 0f, -0.48f), new Vector3(0.18f, 0.06f, 0.18f));
                tail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var trail = go.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMat;
                trail.time = 0.35f;
                trail.startWidth = 0.32f;
                trail.endWidth = 0.02f;
                trail.startColor = new Color(1f, 0.6f, 0.25f, 0.9f);
                trail.endColor = new Color(0.4f, 0.4f, 0.4f, 0f);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                go.SetActive(false);
                rockets.Add(new Rocket { Go = go, Trail = trail });
            }
        }

        public static void Fire(Vector3 pos, Vector3 dir, VehicleHealth owner, Transform target, float damage)
        {
            if (inst == null) return;
            Rocket r = null;
            for (int i = 0; i < inst.rockets.Count; i++) if (!inst.rockets[i].Active) { r = inst.rockets[i]; break; }
            if (r == null) return;
            r.Active = true;
            r.Velocity = dir.normalized * 40f;
            r.Life = 2.6f;
            r.Damage = damage;
            r.Radius = 4.5f;
            r.Owner = owner;
            r.OwnerBody = owner != null ? owner.GetComponent<Rigidbody>() : null;
            r.Target = target;
            r.Shell = false;
            r.Go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
            r.Go.transform.localScale = Vector3.one;
            r.Go.SetActive(true);
            r.Trail.Clear();
            AudioHub.PlayAt(SynthAudio.RocketLaunch(), pos, 0.8f, Random.Range(0.9f, 1.1f));
        }

        /// <summary>8.8 cm shell (4.0): no homing, 120 m/s, 1.1 s of flight, a tight blast with the full damage at the centre.</summary>
        public static void FireShell(Vector3 pos, Vector3 dir, VehicleHealth owner, float damage) { FireShell(pos, dir, owner, damage, 2.8f); }

        public static void FireShell(Vector3 pos, Vector3 dir, VehicleHealth owner, float damage, float radius)
        {
            if (inst == null) return;
            Rocket r = null;
            for (int i = 0; i < inst.rockets.Count; i++) if (!inst.rockets[i].Active) { r = inst.rockets[i]; break; }
            if (r == null) return;
            r.Active = true;
            r.Shell = true;
            r.Velocity = dir.normalized * 120f;
            r.Life = 1.1f;
            r.Damage = damage;
            r.Radius = radius;
            r.Owner = owner;
            r.OwnerBody = owner != null ? owner.GetComponent<Rigidbody>() : null;
            r.Target = null;
            r.Go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
            r.Go.transform.localScale = new Vector3(0.6f, 0.6f, 0.8f);
            r.Go.SetActive(true);
            r.Trail.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            for (int i = 0; i < rockets.Count; i++)
            {
                var r = rockets[i];
                if (!r.Active) continue;
                Vector3 pos = r.Go.transform.position;
                if (r.Target != null)
                {
                    Vector3 to = (r.Target.position + Vector3.up * 0.8f) - pos;
                    Vector3 desired = to.normalized * r.Velocity.magnitude;
                    r.Velocity = Vector3.RotateTowards(r.Velocity, desired, 1.7f * dt, 0f);
                }
                float step = r.Velocity.magnitude * dt;
                Vector3 dir = r.Velocity.normalized;
                int n = Physics.SphereCastNonAlloc(pos, 0.25f, dir, hits, step, ~0, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                Vector3 hitPoint = Vector3.zero;
                Collider bestCol = null;
                for (int h = 0; h < n; h++)
                {
                    if (hits[h].collider == null) continue;
                    if (r.OwnerBody != null && hits[h].rigidbody == r.OwnerBody) continue;
                    if (hits[h].distance < best) { best = hits[h].distance; hitPoint = hits[h].distance > 0f ? hits[h].point : pos; bestCol = hits[h].collider; }
                }
                if (best < float.MaxValue)
                {
                    if (r.Owner != null && r.Owner.Team == 0 && bestCol != null && bestCol.GetComponentInParent<VehicleHealth>() != null)
                        DerbyArena.Core.DailyMissions.Tick(DerbyArena.Core.MissionGoal.Rockets, 1);
                    Explode(r, hitPoint);
                    continue;
                }
                pos += dir * step;
                r.Go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
                r.Life -= dt;
                if (r.Life <= 0f) Explode(r, pos);
            }
        }

        void Explode(Rocket r, Vector3 point)
        {
            r.Active = false;
            r.Go.SetActive(false);
            Fx.Explosion(point, r.Shell ? 0.8f : 1f);
            AudioHub.PlayAt(SynthAudio.Explosion(), point, r.Shell ? 0.75f : 0.9f, Random.Range(r.Shell ? 1.1f : 0.95f, r.Shell ? 1.3f : 1.15f));
            Blast(point, r.Radius, r.Damage, r.Owner, true);
        }

        /// <summary>Area damage applied at most once per vehicle, with falloff and knockback.</summary>
        public static void Blast(Vector3 point, float radius, float damage, VehicleHealth owner, bool knockback)
        {
            if (inst == null) return;
            inst.damaged.Clear();
            int n = Physics.OverlapSphereNonAlloc(point, radius, inst.overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var hp = inst.overlap[i].GetComponentInParent<VehicleHealth>();
                if (hp == null || hp == owner || hp.Dead || inst.damaged.Contains(hp)) continue;
                if (owner != null && owner.Team == hp.Team && owner.Team != 0) continue;
                inst.damaged.Add(hp);
                float dist = Vector3.Distance(point, hp.transform.position);
                float falloff = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(dist / radius));
                hp.Apply(damage * falloff, owner, point);
                if (knockback)
                {
                    var body = hp.GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        body.AddExplosionForce(body.mass * 6f, point, radius * 1.5f, 0.6f, ForceMode.Impulse);
                        // 4.0.2: the knock-up is meant to lift the car; the suspension's bump cap must not take it back
                        var kicked = hp.GetComponent<DerbyArena.Vehicles.ArcadeCar>();
                        if (kicked != null) kicked.Launched(0.5f);
                    }
                }
            }
            if (owner == null || owner.Team != 0)
                DerbyArena.CameraRig.ChaseCamera.KickAt(point, 0.35f);
        }

        void OnDestroy()
        {
            if (inst == this) inst = null;
        }
    }
}
