using DerbyArena.Audio;
using DerbyArena.Combat;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>Looping engine sound pitched by speed and load, plus drift dust and tyre feedback.</summary>
    public sealed class EngineAudio : MonoBehaviour
    {
        public float Volume = 0.35f;
        ArcadeCar car;
        VehicleHealth hp;
        AudioSource src;
        float dustTimer;

        void Start()
        {
            car = GetComponent<ArcadeCar>();
            hp = GetComponent<VehicleHealth>();
            src = gameObject.AddComponent<AudioSource>();
            src.clip = SynthAudio.Engine();
            src.loop = true;
            src.spatialBlend = Volume > 0.3f ? 0.25f : 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 5f;
            src.maxDistance = 45f;
            src.dopplerLevel = 0f;
            src.volume = Volume;
            src.time = Random.Range(0f, src.clip.length * 0.9f);
            src.Play();
        }

        void Update()
        {
            if (car == null || src == null) return;
            if (hp != null && hp.Dead)
            {
                src.volume = Mathf.MoveTowards(src.volume, 0f, Time.deltaTime);
                return;
            }
            float top = car.Spec != null ? car.Spec.TopSpeed : 27f;
            float s = Mathf.Clamp01(car.PlanarSpeed() / top);
            float gear = (s * 4f) % 1f;
            float target = 0.75f + s * 0.9f + gear * 0.25f + (car.NitroActive ? 0.2f : 0f) + car.Throttle * 0.08f;
            src.pitch = Mathf.Lerp(src.pitch, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            src.volume = Volume * (0.7f + 0.3f * car.Throttle);

            dustTimer -= Time.deltaTime;
            if (car.Grounded && (car.Drifting || (car.Throttle > 0.5f && car.PlanarSpeed() < 6f && car.PlanarSpeed() > 0.5f)) && dustTimer <= 0f)
            {
                dustTimer = 0.05f;
                Vector3 back = transform.position - transform.forward * 1.9f + Vector3.up * 0.2f;
                Fx.Dust(back + transform.right * 1.1f, -transform.forward * 1.5f);
                Fx.Dust(back - transform.right * 1.1f, -transform.forward * 1.5f);
            }
        }
    }
}
