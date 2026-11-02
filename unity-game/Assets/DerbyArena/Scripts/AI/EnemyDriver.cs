using System.Collections.Generic;
using DerbyArena.Combat;
using DerbyArena.Vehicles;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.AI
{
    /// <summary>
    /// Enemy brain: role-specific pursuit (flank, ram, keep distance, boss phases), obstacle whiskers,
    /// stuck recovery with reverse + counter-steer, line-of-sight checked burst fire with aim error,
    /// reaction delay and level-scaled difficulty.
    /// </summary>
    public sealed class EnemyDriver : MonoBehaviour
    {
        public ArcadeCar Car;
        public WeaponRig Weapons;
        public VehicleHealth Health;
        public VehicleVisual Visual;
        public VehicleHealth Player;
        public ArenaInfo Arena;
        public float AimError = 6f;
        public float Aggression = 1f;
        public bool Active;
        public int Chapter = 1;
        public float DamageScale = 1f;
        /// <summary>When &gt; 0 the driver keeps its target inside this circle (Last Stand zone).</summary>
        public float ZoneRadius;
        public Vector3 ZoneCenter;

        // ---- attack tokens: only a few enemies may shoot at the player at the same time ----
        public static int MaxShooters = 2;
        static int shooters;
        static readonly List<EnemyDriver> all = new List<EnemyDriver>();
        bool holdsToken;

        public static void ResetTokens(int max)
        {
            MaxShooters = Mathf.Max(1, max);
            shooters = 0;
            for (int i = 0; i < all.Count; i++) if (all[i] != null) all[i].holdsToken = false;
        }

        bool IsBoss { get { return Car != null && Car.Spec != null && Car.Spec.Role == DriverRole.Boss; } }

        bool AcquireToken()
        {
            if (IsBoss || holdsToken) return true;
            if (shooters >= MaxShooters) return false;
            shooters++;
            holdsToken = true;
            return true;
        }

        void ReleaseToken()
        {
            if (!holdsToken) return;
            holdsToken = false;
            shooters = Mathf.Max(0, shooters - 1);
        }

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); ReleaseToken(); }

        enum Mode { Pursue, Recover }
        Mode mode = Mode.Pursue;
        float think;
        float stuckTimer;
        float recoverTimer;
        float recoverSteer;
        float burstTimer;
        bool bursting;
        float rocketTimer;
        float shockTimer = 6f;
        float sideSign;
        float losTimer;
        bool hasLos;
        Vector3 targetPoint;
        float steerCmd, throttleCmd, brakeCmd;
        bool handCmd, nitroCmd;

        void Start()
        {
            sideSign = Random.value < 0.5f ? -1f : 1f;
            burstTimer = Random.Range(0.5f, 1.5f);
            rocketTimer = Random.Range(3f, 5f);
            think = Random.Range(0f, 0.2f);
        }

        void Update()
        {
            if (Car == null || Health == null) return;
            if (Health.Dead || !Active || Player == null)
            {
                ReleaseToken();
                bursting = false;
                Car.Throttle = 0f; Car.Brake = Health.Dead ? 0f : 1f; Car.Steer = 0f; Car.NitroHeld = false; Car.Handbrake = false;
                return;
            }

            float dt = Time.deltaTime;
            think -= dt;
            if (think <= 0f)
            {
                think = 0.1f;
                Decide();
            }

            Car.Steer = Mathf.MoveTowards(Car.Steer, steerCmd, dt * 5f);
            Car.Throttle = throttleCmd;
            Car.Brake = brakeCmd;
            Car.Handbrake = handCmd;
            Car.NitroHeld = nitroCmd;

            Fire(dt);
        }

        void Decide()
        {
            Vector3 me = transform.position;
            Vector3 pp = Player.transform.position;
            Vector3 to = pp - me; to.y = 0f;
            float dist = to.magnitude;
            Vector3 toN = dist > 0.01f ? to / dist : transform.forward;
            var role = Car.Spec.Role;

            if (Player.Dead)
            {
                targetPoint = Arena != null ? Arena.Center : Vector3.zero;
            }
            else switch (role)
            {
                case DriverRole.Flanker:
                    targetPoint = dist > 18f ? pp : pp + Vector3.Cross(Vector3.up, toN) * 10f * sideSign + toN * 4f;
                    if (Random.value < 0.02f) sideSign = -sideSign;
                    break;
                case DriverRole.Rammer:
                {
                    var pb = Player.GetComponent<Rigidbody>();
                    Vector3 lead = pb != null ? pb.linearVelocity * Mathf.Clamp(dist / 25f, 0.1f, 0.8f) : Vector3.zero;
                    targetPoint = pp + lead;
                    break;
                }
                case DriverRole.Sniper:
                    if (dist < 16f) targetPoint = me - toN * 12f + Vector3.Cross(Vector3.up, toN) * 6f * sideSign;
                    else if (dist > 34f) targetPoint = pp;
                    else targetPoint = me + Vector3.Cross(Vector3.up, toN) * 10f * sideSign;
                    break;
                case DriverRole.Boss:
                {
                    float hp = Health.Fraction;
                    if (hp > 0.5f) targetPoint = pp;
                    else targetPoint = dist > 14f ? pp : pp + Vector3.Cross(Vector3.up, toN) * 8f * sideSign;
                    break;
                }
                default:
                    targetPoint = pp;
                    break;
            }

            if (Arena != null && !Arena.Inside(targetPoint))
                targetPoint = Vector3.Lerp(targetPoint, Arena.Center, 0.5f);
            if (ZoneRadius > 0f)
            {
                Vector3 fromZone = targetPoint - ZoneCenter; fromZone.y = 0f;
                float lim = Mathf.Max(2f, ZoneRadius - 3f);
                if (fromZone.magnitude > lim) targetPoint = ZoneCenter + fromZone.normalized * lim;
            }

            // separation: don't stack on other enemies (prevents conga lines and pile-ups)
            Vector3 push = Vector3.zero;
            for (int i = 0; i < all.Count; i++)
            {
                var o = all[i];
                if (o == null || o == this || o.Health == null || o.Health.Dead) continue;
                Vector3 d = me - o.transform.position; d.y = 0f;
                float m = d.magnitude;
                if (m > 0.01f && m < 7f) push += d / m * (7f - m);
            }
            if (push.sqrMagnitude > 0.01f) targetPoint += Vector3.ClampMagnitude(push, 8f);

            float speed = Mathf.Abs(Car.ForwardSpeed);
            if (mode == Mode.Recover)
            {
                recoverTimer -= 0.1f;
                steerCmd = recoverSteer;
                throttleCmd = 0f;
                brakeCmd = 1f;
                handCmd = false;
                nitroCmd = false;
                if (recoverTimer <= 0f) { mode = Mode.Pursue; stuckTimer = 0f; }
                return;
            }

            Vector3 local = transform.InverseTransformPoint(targetPoint);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp(angle / 32f, -1f, 1f);

            // whiskers
            Vector3 nose = me + Vector3.up * 0.8f + transform.forward * (Car.Hull != null ? Car.Hull.size.z * 0.5f : 2.5f);
            float look = Mathf.Clamp(speed * 0.45f, 5f, 12f);
            float avoid = 0f;
            RaycastHit hit;
            Vector3 left = Quaternion.AngleAxis(-28f, Vector3.up) * transform.forward;
            Vector3 right = Quaternion.AngleAxis(28f, Vector3.up) * transform.forward;
            if (Blocked(nose, left, look * 0.8f, out hit)) avoid += 0.7f;
            if (Blocked(nose, right, look * 0.8f, out hit)) avoid -= 0.7f;
            if (Blocked(nose, transform.forward, look, out hit)) avoid += (Vector3.Dot(hit.normal, transform.right) >= 0f ? 1f : -1f);
            steer = Mathf.Clamp(steer + avoid, -1f, 1f);

            float throttle = 1f;
            float brake = 0f;
            bool hand = false;
            if (Mathf.Abs(angle) > 115f && dist < 14f)
            {
                throttle = 0f; brake = 1f; steer = -Mathf.Sign(angle);
            }
            else if (Mathf.Abs(angle) > 55f)
            {
                throttle = 0.65f;
                hand = speed > 13f;
            }
            if (role == DriverRole.Sniper && dist > 16f && dist < 34f) throttle *= 0.6f;
            throttleCmd = throttle * Mathf.Clamp(Aggression, 0.6f, 1.2f);
            brakeCmd = brake;
            steerCmd = steer;
            handCmd = hand;
            nitroCmd = role != DriverRole.Sniper && Mathf.Abs(angle) < 12f && dist > 22f && Car.Nitro > 0.4f;

            if (throttle > 0.5f && speed < 1.2f) stuckTimer += 0.1f; else stuckTimer = Mathf.Max(0f, stuckTimer - 0.05f);
            if (stuckTimer > 1.4f)
            {
                mode = Mode.Recover;
                recoverTimer = 1.1f;
                recoverSteer = steer >= 0f ? -1f : 1f;
            }

            losTimer -= 0.1f;
            if (losTimer <= 0f)
            {
                losTimer = 0.3f;
                hasLos = HasLineOfSight();
            }
        }

        bool Blocked(Vector3 from, Vector3 dir, float dist, out RaycastHit hit)
        {
            if (Physics.Raycast(from, dir, out hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody == Car.Body) return false;
                if (Player != null && hit.collider.GetComponentInParent<VehicleHealth>() == Player) return false;
                return Mathf.Abs(hit.normal.y) < 0.6f;
            }
            return false;
        }

        bool HasLineOfSight()
        {
            Vector3 from = Visual != null ? Visual.Muzzle() : transform.position + Vector3.up * 1.6f;
            Vector3 to = Player.transform.position + Vector3.up * 1f;
            Vector3 d = to - from;
            RaycastHit hit;
            if (Physics.Raycast(from, d.normalized, out hit, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody == Car.Body) return true;
                return hit.collider.GetComponentInParent<VehicleHealth>() != null;
            }
            return true;
        }

        void Fire(float dt)
        {
            if (Weapons == null || Player.Dead) return;
            Vector3 aim = Player.transform.position + Vector3.up * 0.9f;
            var pb = Player.GetComponent<Rigidbody>();
            float dist = Vector3.Distance(transform.position, Player.transform.position);
            if (pb != null) aim += pb.linearVelocity * Mathf.Clamp(dist / 120f, 0f, 0.25f);
            if (Visual != null) Visual.AimAt(aim, dt);

            burstTimer -= dt;
            if (burstTimer <= 0f)
            {
                if (bursting)
                {
                    bursting = false;
                    ReleaseToken();
                    burstTimer = Random.Range(1.3f, 2.4f) / Mathf.Max(0.5f, Aggression);
                }
                else if (hasLos && dist < Car.Spec.GunRange && AcquireToken())
                {
                    bursting = true;
                    burstTimer = Random.Range(0.9f, 1.5f);
                }
                else burstTimer = Random.Range(0.3f, 0.7f);
            }

            Vector3 muzzleDir = Visual != null ? Visual.MuzzleDirection() : transform.forward;
            Vector3 toAim = (aim - transform.position).normalized;
            bool aligned = Vector3.Dot(muzzleDir, toAim) > 0.9f;
            if (bursting && hasLos && aligned && dist < Car.Spec.GunRange)
            {
                Weapons.AimErrorDeg = AimError + Mathf.Max(0f, Car.PlanarSpeed() - 10f) * 0.25f;
                Weapons.TryFireGun(aim);
            }

            if (Car.Spec.Rockets && hasLos)
            {
                rocketTimer -= dt;
                bool boss = Car.Spec.Role == DriverRole.Boss;
                float minD = boss ? 8f : 12f;
                if (rocketTimer <= 0f && dist > minD && dist < 44f)
                {
                    float bossCd = Chapter <= 1 ? 5.5f : (Chapter == 2 ? 4.6f : 3.8f);
                    float cd = boss ? (Health.Fraction < 0.33f ? bossCd * 0.7f : bossCd) : Random.Range(5f, 7.5f);
                    if (Weapons.TryFireRocket(aim, Player.transform, 0.5f)) rocketTimer = cd;
                }
            }

            if (Car.Spec.Role == DriverRole.Boss && Health.Fraction < 0.4f)
            {
                shockTimer -= dt;
                if (shockTimer <= 0f && dist < 14f)
                {
                    shockTimer = Health.Fraction < 0.2f ? 6f : 8.5f;
                    StartCoroutine(Shockwave());
                }
            }
        }

        System.Collections.IEnumerator Shockwave()
        {
            var ring = MeshFactory.Make("ShockTelegraph", MeshFactory.UnitRing(), Mats.Glow(new Color(1f, 0.15f, 0.05f), 2.5f), null, transform.position + Vector3.up * 0.15f, Vector3.one);
            DerbyArena.Audio.AudioHub.PlayAt(DerbyArena.Audio.SynthAudio.Alarm(), transform.position, 0.8f, 1f);
            float windup = Chapter <= 2 ? 1.5f : 1.2f;
            const float radius = 9f;
            float t = 0f;
            while (t < windup && !Health.Dead)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(1f, radius, t / windup);
                ring.transform.position = transform.position + Vector3.up * 0.15f;
                ring.transform.localScale = new Vector3(s, 1f, s);
                yield return null;
            }
            if (!Health.Dead)
            {
                Fx.Explosion(transform.position + Vector3.up * 0.5f, 1.6f);
                DerbyArena.Audio.AudioHub.PlayAt(DerbyArena.Audio.SynthAudio.Explosion(), transform.position, 1f, 0.7f);
                Projectiles.Blast(transform.position, radius, 18f * DamageScale, Health, true);
                Fx.Ring(transform.position + Vector3.up * 0.2f, radius, new Color(1f, 0.3f, 0.1f));
            }
            Destroy(ring);
        }
    }
}
