using System;
using DerbyArena.Audio;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    public sealed class VehicleHealth : MonoBehaviour
    {
        public float Max = 100f;
        public float Current = 100f;
        public bool Dead;
        public event Action<VehicleHealth> Died;

        float lastRam;
        ParticleSystem smoke;
        AudioSource audioSource;

        public void Configure(float max)
        {
            Max = max;
            Current = max;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 40f;
        }

        public void Apply(float amount, UnityEngine.Object source)
        {
            if (Dead || amount <= 0f)
                return;
            Current = Mathf.Max(0f, Current - amount);
            if (audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.1f);
                audioSource.PlayOneShot(SynthAudio.Hit(), 0.7f);
            }
            if (Current <= Max * 0.35f)
                EnsureSmoke();
            if (Current <= 0f)
            {
                Dead = true;
                if (smoke != null)
                    smoke.Play();
                Died?.Invoke(this);
            }
        }

        public void Repair(float amount)
        {
            if (Dead)
                return;
            Current = Mathf.Min(Max, Current + amount);
        }

        public void AddArmor(float amount)
        {
            if (Dead)
                return;
            Max += amount;
            Current += amount;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Time.time < lastRam)
                return;
            var other = collision.collider.GetComponentInParent<VehicleHealth>();
            if (other == null || other == this)
                return;
            if (GetInstanceID() > other.GetInstanceID())
                return;
            float rel = collision.relativeVelocity.magnitude;
            if (rel < 7f)
                return;
            lastRam = Time.time + 0.45f;
            float dmg = Mathf.Clamp((rel - 7f) * 3.2f, 8f, 42f);
            var mine = GetComponent<WheelDrive>();
            var theirs = other.GetComponent<WheelDrive>();
            if (mine != null && mine.Spec.Role == DriverRole.Rammer)
                dmg *= 1.45f;
            if (theirs != null && theirs.Spec.Role == DriverRole.Rammer)
                other.Apply(dmg * 0.35f, this);
            other.Apply(dmg, this);
            Apply(dmg * 0.55f, other);
            var cam = Camera.main != null ? Camera.main.GetComponent<DerbyArena.CameraRig.ChaseCamera>() : null;
            if (cam != null && (CompareTag("Player") || other.CompareTag("Player")))
                cam.Shake(Mathf.Clamp(rel * 0.012f, 0.05f, 0.28f));
        }

        void EnsureSmoke()
        {
            if (smoke != null)
                return;
            var go = new GameObject("DamageSmoke");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.1f, -0.4f);
            smoke = go.AddComponent<ParticleSystem>();
            var main = smoke.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 1.4f;
            main.startSize = 0.45f;
            main.maxParticles = 40;
            main.loop = true;
            var emission = smoke.emission;
            emission.rateOverTime = 12f;
            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            smoke.Play();
        }
    }
}
