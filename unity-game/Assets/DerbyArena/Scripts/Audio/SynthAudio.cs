using UnityEngine;

namespace DerbyArena.Audio
{
    /// <summary>All sounds are synthesised at startup (no audio assets, no licensing). Cached per session.</summary>
    public static class SynthAudio
    {
        const int Rate = 22050;
        static AudioClip cheer, gate;
        static AudioClip engine, shot, hit, crash, explosion, rocket, pickup, cell, click, beep, go, overheat, win, lose, alarm, ambience;
        static System.Random rng = new System.Random(1234);

        static float Noise() { return (float)rng.NextDouble() * 2f - 1f; }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float[] Buffer(float seconds) { return new float[Mathf.Max(1, (int)(Rate * seconds))]; }

        static void Normalize(float[] d, float peak)
        {
            float m = 0.0001f;
            for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
            float s = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= s;
        }

        /// <summary>Loopable V8-ish engine: harmonic stack at 40 Hz plus filtered noise, exact period loop.</summary>
        public static AudioClip Engine()
        {
            if (engine != null) return engine;
            float baseHz = 40f;
            int period = Mathf.RoundToInt(Rate / baseHz);
            var d = new float[period * 20];
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float ph = 2f * Mathf.PI * baseHz * t;
                float s = Mathf.Sin(ph) * 0.55f + Mathf.Sin(ph * 2f) * 0.35f + Mathf.Sin(ph * 3f + 0.5f) * 0.22f + Mathf.Sin(ph * 4f) * 0.12f;
                float pulse = Mathf.Pow(Mathf.Abs(Mathf.Sin(ph * 0.5f)), 6f) * 0.4f;
                lp += (Noise() - lp) * 0.08f;
                d[i] = s * (0.7f + pulse) + lp * 0.25f;
            }
            Normalize(d, 0.5f);
            engine = Make("engine", d);
            return engine;
        }

        public static AudioClip Shot()
        {
            if (shot != null) return shot;
            var d = Buffer(0.12f);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                lp += (Noise() - lp) * 0.5f;
                float thump = Mathf.Sin(2f * Mathf.PI * 95f * i / Rate) * Mathf.Exp(-t * 18f);
                d[i] = (lp * Mathf.Exp(-t * 22f) + thump * 0.8f);
            }
            Normalize(d, 0.8f);
            shot = Make("shot", d);
            return shot;
        }

        public static AudioClip Hit()
        {
            if (hit != null) return hit;
            var d = Buffer(0.14f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                float clang = Mathf.Sin(2f * Mathf.PI * 1250f * i / Rate) * 0.5f + Mathf.Sin(2f * Mathf.PI * 1870f * i / Rate) * 0.3f;
                d[i] = (clang + Noise() * 0.6f) * Mathf.Exp(-t * 14f);
            }
            Normalize(d, 0.6f);
            hit = Make("hit", d);
            return hit;
        }

        public static AudioClip Crash()
        {
            if (crash != null) return crash;
            var d = Buffer(0.5f);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                lp += (Noise() - lp) * 0.25f;
                float metal = Mathf.Sin(2f * Mathf.PI * (420f - 200f * t) * i / Rate) * 0.4f;
                float boom = Mathf.Sin(2f * Mathf.PI * 60f * i / Rate) * Mathf.Exp(-t * 10f);
                d[i] = (lp * 0.9f + metal) * Mathf.Exp(-t * 7f) + boom;
            }
            Normalize(d, 0.9f);
            crash = Make("crash", d);
            return crash;
        }

        public static AudioClip Explosion()
        {
            if (explosion != null) return explosion;
            var d = Buffer(1.3f);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                float cutoff = Mathf.Lerp(0.35f, 0.03f, t);
                lp += (Noise() - lp) * cutoff;
                lp2 += (lp - lp2) * cutoff;
                float sub = Mathf.Sin(2f * Mathf.PI * (55f - 25f * t) * i / Rate) * Mathf.Exp(-t * 5f);
                d[i] = lp2 * 2.2f * Mathf.Exp(-t * 3.2f) + sub * 0.9f;
            }
            Normalize(d, 0.95f);
            explosion = Make("explosion", d);
            return explosion;
        }

        public static AudioClip RocketLaunch()
        {
            if (rocket != null) return rocket;
            var d = Buffer(0.45f);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                lp += (Noise() - lp) * Mathf.Lerp(0.6f, 0.15f, t);
                d[i] = lp * Mathf.Exp(-t * 4f) * (t < 0.03f ? t / 0.03f : 1f);
            }
            Normalize(d, 0.7f);
            rocket = Make("rocket", d);
            return rocket;
        }

        static AudioClip Tones(string name, float[] freqs, float noteLen, float amp)
        {
            var d = Buffer(noteLen * freqs.Length + 0.15f);
            int per = (int)(Rate * noteLen);
            for (int n = 0; n < freqs.Length; n++)
            {
                for (int i = 0; i < per + Rate * 0.15f && n * per + i < d.Length; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 9f);
                    float ph = 2f * Mathf.PI * freqs[n] * t;
                    d[n * per + i] += (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2f)) * env;
                }
            }
            Normalize(d, amp);
            return Make(name, d);
        }

        public static AudioClip Pickup() { if (pickup == null) pickup = Tones("pickup", new[] { 660f, 990f }, 0.07f, 0.55f); return pickup; }
        public static AudioClip Cell() { if (cell == null) cell = Tones("cell", new[] { 523f, 659f, 784f, 1046f }, 0.07f, 0.6f); return cell; }
        public static AudioClip Click() { if (click == null) click = Tones("click", new[] { 1200f }, 0.03f, 0.35f); return click; }
        public static AudioClip Beep() { if (beep == null) beep = Tones("beep", new[] { 600f }, 0.16f, 0.55f); return beep; }
        public static AudioClip Go() { if (go == null) go = Tones("go", new[] { 900f, 1200f }, 0.12f, 0.6f); return go; }
        public static AudioClip Overheat() { if (overheat == null) overheat = Tones("overheat", new[] { 400f, 300f }, 0.09f, 0.45f); return overheat; }
        public static AudioClip Win() { if (win == null) win = Tones("win", new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.11f, 0.6f); return win; }
        public static AudioClip Lose() { if (lose == null) lose = Tones("lose", new[] { 440f, 349f, 262f }, 0.2f, 0.55f); return lose; }
        public static AudioClip Gate() { if (gate == null) gate = Tones("gate", new[] { 784f, 1175f }, 0.08f, 0.55f); return gate; }

        /// <summary>Stadium roar: band-limited noise swell with a few chant formants.</summary>
        public static AudioClip Cheer()
        {
            if (cheer != null) return cheer;
            var d = Buffer(1.6f);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / d.Length;
                lp += (Noise() - lp) * 0.22f;
                lp2 += (lp - lp2) * 0.3f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.15f)) * (0.85f + 0.15f * Mathf.Sin(t * 40f));
                float formant = Mathf.Sin(2f * Mathf.PI * 310f * i / Rate) * 0.08f + Mathf.Sin(2f * Mathf.PI * 470f * i / Rate) * 0.05f;
                d[i] = (lp2 * 1.6f + formant * Mathf.Abs(lp)) * env;
            }
            Normalize(d, 0.7f);
            cheer = Make("cheer", d);
            return cheer;
        }

        public static AudioClip Alarm() { if (alarm == null) alarm = Tones("alarm", new[] { 880f, 660f, 880f, 660f }, 0.12f, 0.45f); return alarm; }

        /// <summary>Low industrial drone with slow beating, loopable.</summary>
        public static AudioClip Ambience()
        {
            if (ambience != null) return ambience;
            int len = Rate * 8;
            var d = new float[len];
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / Rate;
                float a = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 55.125f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.18f;
                lp += (Noise() - lp) * 0.02f;
                float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / 8f);
                d[i] = (a + lp * 0.8f) * swell;
            }
            Normalize(d, 0.35f);
            ambience = Make("ambience", d);
            return ambience;
        }
    }

    /// <summary>Pooled audio playback; honours the Sound setting.</summary>
    public sealed class AudioHub : MonoBehaviour
    {
        static AudioHub inst;
        AudioSource[] pool3d;
        AudioSource[] pool2d;
        int next3d, next2d;
        AudioSource music;

        public static void Init(Transform listenerParent, bool soundOn)
        {
            var go = new GameObject("AudioHub");
            go.transform.SetParent(listenerParent, false);
            inst = go.AddComponent<AudioHub>();
            inst.pool3d = new AudioSource[12];
            for (int i = 0; i < inst.pool3d.Length; i++)
            {
                var c = new GameObject("Sfx3D");
                c.transform.SetParent(null);
                var s = c.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 6f;
                s.maxDistance = 70f;
                s.dopplerLevel = 0f;
                inst.pool3d[i] = s;
                c.transform.SetParent(go.transform, false);
            }
            inst.pool2d = new AudioSource[4];
            for (int i = 0; i < inst.pool2d.Length; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                inst.pool2d[i] = s;
            }
            inst.music = go.AddComponent<AudioSource>();
            inst.music.playOnAwake = false;
            inst.music.loop = true;
            inst.music.spatialBlend = 0f;
            inst.music.volume = 0.5f;
            AudioListener.volume = soundOn ? 1f : 0f;
        }

        public static void SetSound(bool on) { AudioListener.volume = on ? 1f : 0f; }

        public static void PlayAt(AudioClip clip, Vector3 pos, float volume, float pitch)
        {
            if (inst == null || clip == null) return;
            var s = inst.pool3d[inst.next3d];
            inst.next3d = (inst.next3d + 1) % inst.pool3d.Length;
            s.transform.position = pos;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }

        public static void Play2D(AudioClip clip, float volume, float pitch)
        {
            if (inst == null || clip == null) return;
            var s = inst.pool2d[inst.next2d];
            inst.next2d = (inst.next2d + 1) % inst.pool2d.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume);
        }

        public static void Ambience(float volume)
        {
            if (inst == null) return;
            inst.music.clip = SynthAudio.Ambience();
            inst.music.volume = volume;
            if (!inst.music.isPlaying) inst.music.Play();
        }

        void OnDestroy() { if (inst == this) inst = null; }
    }
}
