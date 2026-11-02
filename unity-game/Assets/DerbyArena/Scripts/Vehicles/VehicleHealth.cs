using System;
using System.Collections.Generic;
using DerbyArena.Audio;
using DerbyArena.Combat;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Central damage API. Every source (guns, rockets, rams, shockwaves) goes through Apply, which
    /// handles shield, spawn protection, single death event, wreck state and damage effects.
    /// </summary>
    public sealed class VehicleHealth : MonoBehaviour
    {
        public int Team;                 // 0 player, 1 enemies
        public float Max = 100f;
        public float Current = 100f;
        public float Shield;
        public float ShieldMax = 60f;
        public bool Dead { get; private set; }
        public float InvulnerableUntil;
        public VehicleHealth LastAttacker { get; private set; }
        public float LastHitTime { get; private set; } = -10f;
        public Vector3 LastHitFrom { get; private set; }

        public event Action<VehicleHealth, VehicleHealth> Died;          // victim, killer
        public event Action<VehicleHealth, float> Damaged;               // victim, amount

        ArcadeCar car;
        VehicleVisual visual;
        readonly Dictionary<int, float> ramCooldown = new Dictionary<int, float>();
        float smokeTimer;
        float fireUntil;
        float hitSoundCd;

        public float Fraction { get { return Mathf.Clamp01(Current / Mathf.Max(1f, Max)); } }

        public void Configure(float max, int team)
        {
            Max = max;
            Current = max;
            Team = team;
            car = GetComponent<ArcadeCar>();
            visual = GetComponent<VehicleVisual>();
        }

        public void Apply(float amount, VehicleHealth attacker, Vector3 from)
        {
            if (Dead || amount <= 0f) return;
            if (Time.time < InvulnerableUntil) return;
            if (attacker == this) return;
            if (attacker != null && attacker.Team == Team && Team != 0 && amount < 30f) amount *= 0.25f;

            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, amount * 0.8f);
                Shield -= absorbed;
                amount -= absorbed;
            }
            Current = Mathf.Max(0f, Current - amount);
            LastAttacker = attacker;
            LastHitTime = Time.time;
            LastHitFrom = from;

            if (hitSoundCd <= 0f)
            {
                hitSoundCd = 0.08f;
                AudioHub.PlayAt(SynthAudio.Hit(), transform.position, Team == 0 ? 0.75f : 0.5f, UnityEngine.Random.Range(0.85f, 1.15f));
            }
            if (visual == null) visual = GetComponent<VehicleVisual>();
            if (visual != null && amount >= 1.5f) visual.Flash();
            if (Damaged != null) Damaged(this, amount);
            if (Current <= 0f) Die(attacker);
        }

        public void Heal(float amount)
        {
            if (Dead) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        public void AddShield(float amount)
        {
            if (Dead) return;
            Shield = Mathf.Min(ShieldMax, Shield + amount);
        }

        void Die(VehicleHealth killer)
        {
            if (Dead) return;
            Dead = true;
            fireUntil = Time.time + 7f;
            if (car != null)
            {
                car.Locked = true;
                car.NitroHeld = false;
                if (car.Body != null)
                    car.Body.AddForce(Vector3.up * car.Body.mass * 5f + UnityEngine.Random.insideUnitSphere * car.Body.mass * 2f, ForceMode.Impulse);
            }
            Fx.Explosion(transform.position + Vector3.up, 1.4f);
            AudioHub.PlayAt(SynthAudio.Explosion(), transform.position, 1f, UnityEngine.Random.Range(0.85f, 1.05f));
            Char();
            Fx.Ring(transform.position + Vector3.up * 0.3f, 9f, new Color(1f, 0.55f, 0.2f));
            DerbyArena.World.Crowd.Cheer(Team == 0 ? 0.3f : 1f);
            if (Died != null) Died(this, killer);
        }

        void Char()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].HasProperty("_Color"))
                    {
                        var col = mats[i].GetColor("_Color");
                        mats[i].SetColor("_Color", new Color(col.r * 0.18f + 0.03f, col.g * 0.16f + 0.02f, col.b * 0.15f + 0.02f, col.a));
                    }
                    if (mats[i].HasProperty("_EmissionColor")) mats[i].SetColor("_EmissionColor", Color.black);
                }
                r.materials = mats;
            }
        }

        void Update()
        {
            if (hitSoundCd > 0f) hitSoundCd -= Time.deltaTime;
            smokeTimer -= Time.deltaTime;
            if (smokeTimer > 0f) return;
            Vector3 p = transform.position + transform.up * 1.3f + transform.forward * 1.2f;
            if (Dead)
            {
                smokeTimer = 0.09f;
                if (Time.time < fireUntil) Fx.Fire(p, 1f);
                Fx.Smoke(p, Time.time < fireUntil ? 1.4f : 0.8f);
            }
            else if (Fraction < 0.4f)
            {
                smokeTimer = Mathf.Lerp(0.05f, 0.22f, Fraction / 0.4f);
                Fx.Smoke(p, 0.8f);
                if (Fraction < 0.15f) Fx.Fire(p, 0.5f);
            }
            else smokeTimer = 0.25f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Dead) return;
            var other = collision.collider.GetComponentInParent<VehicleHealth>();
            if (other == null || other == this) return;
            if (GetInstanceID() > other.GetInstanceID()) return;   // one side resolves the pair

            float cd;
            int key = other.GetInstanceID();
            if (ramCooldown.TryGetValue(key, out cd) && Time.time < cd) return;

            var myCar = car;
            var theirCar = other.GetComponent<ArcadeCar>();
            if (myCar == null || theirCar == null || myCar.Body == null || theirCar.Body == null) return;

            Vector3 dir = other.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            dir.Normalize();
            float mine = Mathf.Max(0f, Vector3.Dot(myCar.Body.linearVelocity, dir));
            float theirs = Mathf.Max(0f, Vector3.Dot(theirCar.Body.linearVelocity, -dir));
            float impact = mine + theirs;
            if (impact < 6f) return;

            ramCooldown[key] = Time.time + 0.5f;
            float baseDmg = Mathf.Clamp((impact - 6f) * 2.4f, 5f, 34f);
            bool iAmRammer = mine >= theirs;
            var rammer = iAmRammer ? this : other;
            var victim = iAmRammer ? other : this;
            var rammerCar = iAmRammer ? myCar : theirCar;
            float mul = rammerCar.Spec != null ? rammerCar.Spec.RamMul : 1f;
            if (rammer.Team == 0) mul *= 1.35f;
            if (victim.Team == 0) mul *= 0.75f;
            float selfMul = rammer.Team == 0 ? 0.2f : 0.25f;

            Vector3 contact = collision.contactCount > 0 ? collision.GetContact(0).point : (transform.position + other.transform.position) * 0.5f;
            victim.Apply(baseDmg * mul, rammer, contact);
            var victimCar = iAmRammer ? theirCar : myCar;
            if (victimCar != null && victimCar.Body != null && !victimCar.Body.isKinematic)
            {
                // extra shove + spin so a good hit visibly throws the target (demolition-derby feel)
                Vector3 push = (victim.transform.position - rammer.transform.position);
                push.y = 0f;
                if (push.sqrMagnitude > 0.001f) push.Normalize();
                float dv = Mathf.Clamp(impact * 0.22f, 1f, 6f) * (rammer.Team == 0 ? 1.25f : 0.8f);
                victimCar.Body.AddForce((push * dv + Vector3.up * dv * 0.18f) * victimCar.Body.mass, ForceMode.Impulse);
                victimCar.Body.AddTorque(Vector3.up * UnityEngine.Random.Range(-1f, 1f) * dv * victimCar.Body.mass * 0.35f, ForceMode.Impulse);
            }
            rammer.Apply(baseDmg * selfMul, victim, contact);
            if (baseDmg * mul > 18f) DerbyArena.World.Crowd.Cheer(0.6f);
            Fx.Sparks(contact, -dir, 18);
            AudioHub.PlayAt(SynthAudio.Crash(), contact, Mathf.Clamp01(impact / 25f) + 0.3f, UnityEngine.Random.Range(0.85f, 1.1f));
            if (rammer.Team == 0 || victim.Team == 0)
                DerbyArena.CameraRig.ChaseCamera.Kick(Mathf.Clamp(impact * 0.02f, 0.15f, 0.6f));
        }
    }
}
