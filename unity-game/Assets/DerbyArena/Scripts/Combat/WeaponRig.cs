using DerbyArena.Audio;
using DerbyArena.Vehicles;
using UnityEngine;

namespace DerbyArena.Combat
{
    /// <summary>
    /// Turret machine gun with heat (no ammo to manage) plus limited homing rockets.
    /// Hit detection uses a sphere cast that ignores the shooter's own colliders.
    /// </summary>
    public sealed class WeaponRig : MonoBehaviour
    {
        public VehicleSpec Spec;
        public VehicleHealth Owner;
        public VehicleVisual Visual;
        public int Rockets;
        public float Heat;
        public bool Overheated { get; private set; }
        public float DamageMul = 1f;
        public float AimErrorDeg = 1.5f;
        public bool UseHeat = true;
        public Color TracerColor = new Color(1f, 0.8f, 0.4f, 1f);
        public float RocketCooldownLeft { get { return Mathf.Max(0f, rocketCd); } }

        float gunCd;
        float rocketCd;
        Rigidbody body;
        readonly RaycastHit[] hits = new RaycastHit[12];

        public void Configure(VehicleSpec spec, VehicleHealth owner, VehicleVisual visual, int rockets)
        {
            Spec = spec;
            Owner = owner;
            Visual = visual;
            Rockets = rockets;
            body = GetComponent<Rigidbody>();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            gunCd -= dt;
            rocketCd -= dt;
            if (UseHeat)
            {
                Heat = Mathf.Max(0f, Heat - dt * (Overheated ? 0.45f : 0.6f));
                if (Overheated && Heat < 0.3f) Overheated = false;
            }
        }

        public bool TryFireGun(Vector3 aimPoint)
        {
            if (Spec == null || Owner == null || Owner.Dead) return false;
            if (gunCd > 0f || Overheated) return false;
            gunCd = Spec.GunInterval;
            if (UseHeat)
            {
                Heat += 0.042f;
                if (Heat >= 1f) { Heat = 1f; Overheated = true; AudioHub.Play2D(SynthAudio.Overheat(), 0.6f, 1f); }
            }

            Vector3 origin = Visual != null ? Visual.Muzzle() : transform.position + Vector3.up * 1.5f + transform.forward * 2f;
            Vector3 dir = aimPoint - origin;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            if (AimErrorDeg > 0f)
                dir = Quaternion.Euler(Random.Range(-AimErrorDeg, AimErrorDeg) * 0.5f, Random.Range(-AimErrorDeg, AimErrorDeg), 0f) * dir;

            Vector3 end = origin + dir * Spec.GunRange;
            int n = Physics.SphereCastNonAlloc(origin, 0.3f, dir, hits, Spec.GunRange, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            int bestI = -1;
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider == null) continue;
                if (body != null && hits[i].rigidbody == body) continue;
                if (hits[i].distance < best) { best = hits[i].distance; bestI = i; }
            }
            if (bestI >= 0)
            {
                var h = hits[bestI];
                end = h.distance > 0f ? h.point : origin + dir * 0.5f;
                var hp = h.collider.GetComponentInParent<VehicleHealth>();
                if (hp != null && hp != Owner && !(hp.Team == Owner.Team && Owner.Team != 0))
                {
                    hp.Apply(Spec.GunDamage * DamageMul, Owner, end);
                    Fx.Sparks(end, -dir, 6);
                    if (Owner.Team == 0) DerbyArena.UI.HudController.HitMarker();
                }
                else
                {
                    Fx.Sparks(end, h.normal, 3);
                }
            }

            Fx.Tracer(origin, end, TracerColor);
            Fx.Muzzle(origin, dir);
            AudioHub.PlayAt(SynthAudio.Shot(), origin, Owner.Team == 0 ? 0.45f : 0.3f, Random.Range(0.92f, 1.08f));
            return true;
        }

        public bool TryFireRocket(Vector3 aimPoint, Transform target, float cooldown)
        {
            if (Spec == null || !Spec.Rockets || Owner == null || Owner.Dead) return false;
            if (Rockets <= 0 || rocketCd > 0f) return false;
            rocketCd = cooldown;
            Rockets--;
            Vector3 origin = Visual != null ? Visual.Muzzle() + Vector3.up * 0.25f : transform.position + Vector3.up * 1.8f;
            Vector3 dir = aimPoint - origin;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            Projectiles.Fire(origin, dir.normalized, Owner, target, 42f * DamageMul);
            return true;
        }
    }
}
