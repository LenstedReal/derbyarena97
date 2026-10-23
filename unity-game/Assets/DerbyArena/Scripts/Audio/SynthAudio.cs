using UnityEngine;

namespace DerbyArena.Audio
{
    public static class SynthAudio
    {
        static AudioClip engine;
        static AudioClip shot;
        static AudioClip boom;
        static AudioClip hit;
        static AudioClip ping;

        public static AudioClip EngineLoop()
        {
            if (engine == null)
                engine = Tone("engine", 90f, 1.2f, true, 0.25f);
            return engine;
        }

        public static AudioClip Shot()
        {
            if (shot == null)
                shot = Noise("shot", 0.08f, 0.6f);
            return shot;
        }

        public static AudioClip Boom()
        {
            if (boom == null)
                boom = Tone("boom", 50f, 0.35f, false, 0.8f);
            return boom;
        }

        public static AudioClip Hit()
        {
            if (hit == null)
                hit = Noise("hit", 0.12f, 0.9f);
            return hit;
        }

        public static AudioClip Ping()
        {
            if (ping == null)
                ping = Tone("ping", 660f, 0.12f, false, 0.4f);
            return ping;
        }

        static AudioClip Tone(string name, float freq, float dur, bool loop, float amp)
        {
            int rate = 22050;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = loop ? 1f : 1f - (i / (float)n);
                data[i] = Mathf.Sin(t * freq * Mathf.PI * 2f) * amp * env;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float dur, float amp)
        {
            int rate = 22050;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float env = 1f - (i / (float)n);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * amp * env;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
