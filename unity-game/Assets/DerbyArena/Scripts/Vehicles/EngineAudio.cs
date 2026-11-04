using DerbyArena.Audio;
using DerbyArena.Combat;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Layered vehicle audio (4.0). Three synthesized loops are cross-faded by an rpm estimate built from speed and
    /// throttle: <c>idle</c> (low, lumpy), <c>engine</c> (the mid band with a gear ripple) and <c>load</c> (the
    /// throttle-dependent growl). Tanks run everything an octave-ish lower and add a <c>tracks</c> clatter loop that
    /// follows speed. The player's vehicle keeps all layers (priority 16, mostly 2D); rivals get the two mid/high
    /// layers only (priority 160, 3D with a linear roll-off) so the eight vehicles stay inside Unity's 32 real
    /// voices and never steal from cannon/explosion one-shots. Drift dust and the engine fade on death are kept.
    /// </summary>
    public sealed class EngineAudio : MonoBehaviour
    {
        public float Volume = 0.35f;
        ArcadeCar car;
        VehicleHealth hp;
        AudioSource idle, engine, load, tracks;
        float dustTimer, rpm;
        bool player, tank;

        void Start()
        {
            car = GetComponent<ArcadeCar>();
            hp = GetComponent<VehicleHealth>();
            player = Volume > 0.3f;
            tank = car != null && car.Spec != null && car.Spec.Tank;
            engine = Layer(SynthAudio.Engine(), 1f);
            load = Layer(SynthAudio.EngineLoad(), 0f);
            if (player) idle = Layer(SynthAudio.EngineIdle(), 1f);
            if (tank) tracks = Layer(SynthAudio.Tracks(), 0f);
        }

        AudioSource Layer(AudioClip clip, float startVolume)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = player ? 0.25f : 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = tank ? 7f : 5f;
            s.maxDistance = tank ? 60f : 45f;
            s.dopplerLevel = 0f;
            s.priority = player ? 16 : 160;
            s.volume = Volume * startVolume;
            s.pitch = tank ? 0.72f : 1f;
            if (clip != null)
            {
                s.time = Random.Range(0f, clip.length * 0.9f);
                s.Play();
            }
            return s;
        }

        static float Smooth(float a, float b, float x) { x = Mathf.Clamp01((x - a) / Mathf.Max(b - a, 0.0001f)); return x * x * (3f - 2f * x); }

        void Update()
        {
            if (car == null || engine == null) return;
            float dt = Time.deltaTime;
            if (hp != null && hp.Dead)
            {
                Fade(idle, 0f, dt); Fade(engine, 0f, dt); Fade(load, 0f, dt); Fade(tracks, 0f, dt);
                return;
            }
            float top = car.Spec != null ? car.Spec.TopSpeed : 27f;
            float speed = car.PlanarSpeed();
            float s = Mathf.Clamp01(speed / top);
            float throttle = Mathf.Abs(car.Throttle);
            // rpm estimate: speed carries most of it, throttle adds load even when the wheels are slipping
            float target = Mathf.Clamp01(s * 0.72f + throttle * 0.38f + (car.NitroActive ? 0.15f : 0f));
            rpm = Mathf.Lerp(rpm, target, 1f - Mathf.Exp(-(target > rpm ? 5f : 2.5f) * dt));
            float gear = (s * 4f) % 1f;
            float heavy = tank ? 0.72f : 1f;

            float wIdle = 1f - Smooth(0.08f, 0.42f, rpm);
            float wEngine = Smooth(0.05f, 0.35f, rpm) * (1f - 0.55f * Smooth(0.6f, 1f, rpm));
            float wLoad = Smooth(0.38f, 0.95f, rpm) * (0.35f + 0.65f * throttle);

            if (idle != null)
            {
                idle.pitch = heavy * (0.92f + rpm * 0.35f);
                idle.volume = Volume * 0.9f * wIdle;
            }
            float ep = heavy * (0.75f + s * 0.9f + gear * 0.25f + (car.NitroActive ? 0.2f : 0f) + throttle * 0.08f);
            engine.pitch = Mathf.Lerp(engine.pitch, ep, 1f - Mathf.Exp(-8f * dt));
            engine.volume = Volume * (0.55f + 0.45f * wEngine) * (player ? 1f : 0.85f);
            if (load != null)
            {
                load.pitch = heavy * (0.8f + rpm * 0.75f);
                load.volume = Volume * 0.95f * wLoad;
            }
            if (tracks != null)
            {
                float move = Mathf.Clamp01(speed / 6f);
                tracks.pitch = 0.8f + s * 0.7f;
                tracks.volume = Volume * 0.7f * move;
            }

            dustTimer -= dt;
            bool slipping = car.Grounded && (car.Drifting || (throttle > 0.5f && speed < 6f && speed > 0.5f));
            bool crawling = tank && car.Grounded && speed > 1.5f;
            if ((slipping || crawling) && dustTimer <= 0f)
            {
                dustTimer = crawling && !slipping ? 0.12f : 0.05f;
                Vector3 back = transform.position - transform.forward * 1.9f + Vector3.up * 0.2f;
                Fx.Dust(back + transform.right * 1.1f, -transform.forward * 1.5f);
                Fx.Dust(back - transform.right * 1.1f, -transform.forward * 1.5f);
            }
        }

        static void Fade(AudioSource s, float to, float dt)
        {
            if (s != null) s.volume = Mathf.MoveTowards(s.volume, to, dt);
        }
    }
}
