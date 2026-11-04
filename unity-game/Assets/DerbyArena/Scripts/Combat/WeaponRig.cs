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
            if (Spec != null && Spec.Cannon)
            {
                // the weapon bar shows the reload: 1 right after a shot, 0 when the breech is ready
                float before = Heat;
                Heat = Mathf.Clamp01(gunCd / Mathf.Max(0.1f, Spec.CannonReload));
                Overheated = false;
                if (before > 0.35f && Heat <= 0.35f && Owner != null && Owner.Team == 0) AudioHub.Play2D(SynthAudio.Reload(), 0.5f, 1f);
            }
            else if (UseHeat)
            {
                Heat = Mathf.Max(0f, Heat - dt * (Overheated ? 0.45f : 0.6f));
                if (Overheated && Heat < 0.3f) Overheated = false;
            }
        }

        /// <summary>Main gun of the T-12: one 8.8 cm shell per reload, muzzle blast, recoil, casing (4.0).</summary>
        bool FireCannon(Vector3 aimPoint)
        {
            if (gunCd > 0f) return false;
            gunCd = Spec.CannonReload;
            Vector3 origin = Visual != null ? Visual.Muzzle() : transform.position + Vector3.up * 2f + transform.forward * 4f;
            Vector3 dir = aimPoint - origin;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            if (AimErrorDeg > 0f) dir = Quaternion.Euler(Random.Range(-AimErrorDeg, AimErrorDeg) * 0.3f, Random.Range(-AimErrorDeg, AimErrorDeg) * 0.5f, 0f) * dir;
            Projectiles.FireShell(origin, dir, Owner, Spec.GunDamage * DamageMul);
            Fx.Muzzle(origin, dir);
            Fx.Fire(origin + dir * 0.6f, 1.6f);
            Fx.Smoke(origin + dir * 1.2f, 1.4f);
            Fx.Flash(origin, new Color(1f, 0.7f, 0.35f), 4f, 16f);
            if (Visual != null) Visual.Recoil();
            if (body != null) body.AddForce(-dir * body.mass * 2.2f, ForceMode.Impulse);
            if (Owner != null && Owner.Team == 0) DerbyArena.World.PostFx.Hit(0.5f);
            Fx.Casing(Visual != null ? Visual.transform.position + Vector3.up * 2.4f : origin, -dir + Vector3.up * 1.5f + transform.right * Random.Range(-0.5f, 0.5f));
            AudioHub.PlayAt(SynthAudio.Cannon(), origin, 1f, Random.Range(0.95f, 1.05f));
            return true;
        }

        public bool TryFireGun(Vector3 aimPoint)
        {
            if (Spec == null || Owner == null || Owner.Dead) return false;
            if (Spec.Cannon) return FireCannon(aimPoint);
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
            if (Spec.Cannon)
            {
                // tank: a high-explosive shell from the main gun, wide blast, no homing
                Projectiles.FireShell(origin, dir.normalized, Owner, Spec.RocketDamage * DamageMul, 5f);
                Fx.Muzzle(origin, dir.normalized);
                Fx.Fire(origin + dir.normalized * 0.6f, 1.6f);
                Fx.Flash(origin, new Color(1f, 0.7f, 0.35f), 4f, 16f);
                if (Visual != null) Visual.Recoil();
                if (body != null) body.AddForce(-dir.normalized * body.mass * 2.2f, ForceMode.Impulse);
                if (Owner != null && Owner.Team == 0) DerbyArena.World.PostFx.Hit(0.5f);
                AudioHub.PlayAt(SynthAudio.Cannon(), origin, 1f, 0.9f);
                return true;
            }
            Projectiles.Fire(origin, dir.normalized, Owner, target, (Spec != null ? Spec.RocketDamage : 42f) * DamageMul);
            return true;
        }
    }
}
