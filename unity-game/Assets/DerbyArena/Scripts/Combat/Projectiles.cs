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
            r.Go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
            r.Go.SetActive(true);
            r.Trail.Clear();
            AudioHub.PlayAt(SynthAudio.RocketLaunch(), pos, 0.8f, Random.Range(0.9f, 1.1f));
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
                for (int h = 0; h < n; h++)
                {
                    if (hits[h].collider == null) continue;
                    if (r.OwnerBody != null && hits[h].rigidbody == r.OwnerBody) continue;
                    if (hits[h].distance < best) { best = hits[h].distance; hitPoint = hits[h].distance > 0f ? hits[h].point : pos; }
                }
                if (best < float.MaxValue)
                {
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
            Fx.Explosion(point, 1f);
            AudioHub.PlayAt(SynthAudio.Explosion(), point, 0.9f, Random.Range(0.95f, 1.15f));
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
                    if (body != null) body.AddExplosionForce(body.mass * 6f, point, radius * 1.5f, 0.6f, ForceMode.Impulse);
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
