using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Combat
{
    /// <summary>
    /// Pooled visual effects. One world-space ParticleSystem per effect type, fed with Emit();
    /// tracers and flash lights are recycled. Nothing is instantiated during a match.
    /// </summary>
    public sealed class Fx : MonoBehaviour
    {
        static Fx inst;

        ParticleSystem sparks, fire, smoke, dust, shards, glow, embers;
        Transform[] rings;
        Material[] ringMats;
        float[] ringLife, ringMax, ringRadius;
        Color[] ringColor;
        int ringNext;
        Transform follow;
        float emberTimer;
        LineRenderer[] tracers;
        float[] tracerLife;
        int tracerNext;
        Light[] flashes;
        float[] flashLife;
        float[] flashStart;
        int flashNext;
        float density = 1f;

        public static void Init(Transform parent, int quality)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            inst = go.AddComponent<Fx>();
            inst.density = quality == 0 ? 0.5f : (quality == 1 ? 0.8f : 1f);
            inst.Build(quality);
        }

        ParticleSystem MakeSystem(string name, Material mat, int max, float gravity, float drag)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.gravityModifier = gravity;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 1f;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            if (drag > 0f)
            {
                var lim = ps.limitVelocityOverLifetime;
                lim.enabled = true;
                lim.drag = drag;
                lim.multiplyDragByParticleSize = false;
                lim.multiplyDragByParticleVelocity = true;
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 0f;
            ps.Play();
            return ps;
        }

        static void SizeCurve(ParticleSystem ps, float from, float to)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
        }

        static void Fade(ParticleSystem ps, Color a, Color b)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        void Build(int quality)
        {
            var add = Mats.FxAdditive();
            var soft = Mats.FxSoft();
            sparks = MakeSystem("Sparks", add, 400, 1.2f, 0f);
            SizeCurve(sparks, 1f, 0.2f);
            fire = MakeSystem("Fire", add, 300, -0.15f, 0.8f);
            SizeCurve(fire, 0.6f, 1.6f);
            Fade(fire, new Color(1f, 0.85f, 0.5f), new Color(0.8f, 0.2f, 0.05f));
            smoke = MakeSystem("Smoke", soft, quality == 0 ? 200 : 420, -0.05f, 0.6f);
            SizeCurve(smoke, 0.5f, 2.4f);
            Fade(smoke, new Color(0.42f, 0.4f, 0.38f), new Color(0.3f, 0.29f, 0.28f));
            dust = MakeSystem("Dust", soft, 260, -0.02f, 1.2f);
            SizeCurve(dust, 0.6f, 2.2f);
            Fade(dust, new Color(0.62f, 0.58f, 0.52f), new Color(0.5f, 0.47f, 0.42f));
            shards = MakeSystem("Shards", add, 200, 2f, 0f);
            SizeCurve(shards, 1f, 0.3f);
            glow = MakeSystem("Glow", add, 260, 0f, 1.5f);
            SizeCurve(glow, 1f, 0.15f);
            Fade(glow, Color.white, new Color(1f, 0.6f, 0.3f));
            if (quality > 0)
            {
                embers = MakeSystem("Embers", add, 160, -0.03f, 0.4f);
                SizeCurve(embers, 0.4f, 1f);
                Fade(embers, new Color(1f, 0.7f, 0.35f), new Color(1f, 0.35f, 0.1f));
            }

            int ringCount = quality == 0 ? 3 : 6;
            rings = new Transform[ringCount];
            ringMats = new Material[ringCount];
            ringLife = new float[ringCount];
            ringMax = new float[ringCount];
            ringRadius = new float[ringCount];
            ringColor = new Color[ringCount];
            for (int i = 0; i < ringCount; i++)
            {
                ringMats[i] = Mats.Glow(Color.white, 2f);
                var rg = MeshFactory.Make("ShockRing", MeshFactory.UnitRing(), ringMats[i], transform, Vector3.zero, Vector3.one);
                var mr = rg.GetComponent<MeshRenderer>();
                if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rg.SetActive(false);
                rings[i] = rg.transform;
            }

            tracers = new LineRenderer[24];
            tracerLife = new float[tracers.Length];
            var lineMat = Mats.FxAdditive();
            for (int i = 0; i < tracers.Length; i++)
            {
                var go = new GameObject("Tracer");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.sharedMaterial = lineMat;
                lr.startWidth = 0.09f;
                lr.endWidth = 0.03f;
                lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;
                tracers[i] = lr;
            }

            int lights = quality == 0 ? 1 : 3;
            flashes = new Light[lights];
            flashLife = new float[lights];
            flashStart = new float[lights];
            for (int i = 0; i < lights; i++)
            {
                var go = new GameObject("Flash");
                go.transform.SetParent(transform, false);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.enabled = false;
                flashes[i] = l;
            }
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color32 color)
        {
            if (ps == null) return;
            var p = new ParticleSystem.EmitParams();
            p.position = pos;
            p.velocity = vel;
            p.startSize = size;
            p.startLifetime = life;
            p.startColor = color;
            ps.Emit(p, 1);
        }

        public static void Sparks(Vector3 pos, Vector3 normal, int count)
        {
            if (inst == null) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(count * inst.density));
            for (int i = 0; i < n; i++)
            {
                Vector3 v = (normal * 0.6f + Random.insideUnitSphere).normalized * Random.Range(4f, 11f);
                Emit(inst.sparks, pos, v, Random.Range(0.08f, 0.18f), Random.Range(0.15f, 0.4f), new Color32(255, (byte)Random.Range(150, 230), 80, 255));
            }
        }

        public static void Muzzle(Vector3 pos, Vector3 dir)
        {
            if (inst == null) return;
            Emit(inst.sparks, pos, dir * 6f, 0.9f, 0.05f, new Color32(255, 200, 110, 255));
            Emit(inst.sparks, pos + dir * 0.3f, dir * 14f, 0.12f, 0.12f, new Color32(255, 220, 140, 255));
        }

        public static void Fire(Vector3 pos, float scale)
        {
            if (inst == null) return;
            Vector3 v = Vector3.up * Random.Range(1.5f, 3f) + Random.insideUnitSphere * 0.6f;
            Emit(inst.fire, pos + Random.insideUnitSphere * 0.3f * scale, v, Random.Range(0.7f, 1.2f) * scale, Random.Range(0.35f, 0.6f), new Color32(255, 160, 60, 255));
        }

        public static void Smoke(Vector3 pos, float scale)
        {
            if (inst == null) return;
            Vector3 v = Vector3.up * Random.Range(1.2f, 2.4f) + Random.insideUnitSphere * 0.5f;
            Emit(inst.smoke, pos, v, Random.Range(0.55f, 0.9f) * scale, Random.Range(1.0f, 1.6f), new Color32(92, 88, 84, 120));
        }

        public static void Dust(Vector3 pos, Vector3 vel)
        {
            if (inst == null) return;
            Emit(inst.dust, pos, vel + Vector3.up * 0.8f + Random.insideUnitSphere * 0.4f, Random.Range(0.7f, 1.2f), Random.Range(0.6f, 1f), new Color32(170, 160, 145, 150));
        }

        public static void Burst(Vector3 pos, Color color, int count)
        {
            if (inst == null) return;
            var c = (Color32)color;
            for (int i = 0; i < count; i++)
                Emit(inst.shards, pos, Random.onUnitSphere * Random.Range(3f, 8f) + Vector3.up * 3f, Random.Range(0.15f, 0.35f), Random.Range(0.4f, 0.8f), c);
            Flash(pos, color, 4f, 9f);
        }

        public static void Explosion(Vector3 pos, float size)
        {
            if (inst == null) return;
            float d = inst.density;
            int fireN = Mathf.RoundToInt(26 * d * size);
            for (int i = 0; i < fireN; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(2f, 7f) * size + Vector3.up * 2.5f;
                Emit(inst.fire, pos + Random.insideUnitSphere * 0.6f * size, v, Random.Range(1.2f, 2.4f) * size, Random.Range(0.35f, 0.75f), new Color32(255, (byte)Random.Range(120, 200), 50, 255));
            }
            int smokeN = Mathf.RoundToInt(16 * d * size);
            for (int i = 0; i < smokeN; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(1f, 3.5f) * size + Vector3.up * 2f;
                Emit(inst.smoke, pos + Random.insideUnitSphere * size, v, Random.Range(1.2f, 2.0f) * size, Random.Range(1.2f, 2.0f), new Color32(80, 76, 72, 150));
            }
            Ring(pos + Vector3.up * 0.2f, 7f * size, new Color(1f, 0.6f, 0.25f));
            Sparks(pos, Vector3.up, Mathf.RoundToInt(30 * size));
            Flash(pos + Vector3.up, new Color(1f, 0.55f, 0.2f), 6f * size, 14f * size);
            DerbyArena.World.PostFx.Blast(pos, size);
        }

        /// <summary>Nitro exhaust flame: short additive white-hot streak behind the car.</summary>
        public static void Nitro(Vector3 pos, Vector3 backDir)
        {
            if (inst == null) return;
            Emit(inst.glow, pos, backDir * Random.Range(5f, 8f) + Random.insideUnitSphere * 0.4f, Random.Range(0.35f, 0.55f), Random.Range(0.08f, 0.16f), new Color32(120, 200, 255, 255));
            Emit(inst.glow, pos, backDir * 3f, 0.7f, 0.05f, new Color32(255, 240, 220, 255));
        }

        /// <summary>Generic glow spark (pickups, gates, hit flashes).</summary>
        public static void Glow(Vector3 pos, Vector3 vel, Color color, float size, float life)
        {
            if (inst == null) return;
            Emit(inst.glow, pos, vel, size, life, color);
        }

        /// <summary>Expanding flat shockwave ring on the ground.</summary>
        public static void Ring(Vector3 pos, float radius, Color color)
        {
            if (inst == null || inst.rings == null || inst.rings.Length == 0) return;
            int i = inst.ringNext;
            inst.ringNext = (i + 1) % inst.rings.Length;
            var t = inst.rings[i];
            t.position = pos;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one * 0.5f;
            t.gameObject.SetActive(true);
            inst.ringLife[i] = 0.45f;
            inst.ringMax[i] = 0.45f;
            inst.ringRadius[i] = radius;
            inst.ringColor[i] = color;
        }

        /// <summary>Ambient embers drift around this transform (the camera).</summary>
        public static void FollowEmbers(Transform target)
        {
            if (inst != null) inst.follow = target;
        }

        public static void Tracer(Vector3 a, Vector3 b, Color color)
        {
            if (inst == null) return;
            int i = inst.tracerNext;
            inst.tracerNext = (i + 1) % inst.tracers.Length;
            var lr = inst.tracers[i];
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0f);
            lr.enabled = true;
            inst.tracerLife[i] = 0.05f;
        }

        public static void Flash(Vector3 pos, Color color, float intensity, float range)
        {
            if (inst == null || inst.flashes.Length == 0) return;
            int i = inst.flashNext;
            inst.flashNext = (i + 1) % inst.flashes.Length;
            var l = inst.flashes[i];
            l.transform.position = pos;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.enabled = true;
            inst.flashLife[i] = 0.16f;
            inst.flashStart[i] = intensity;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < rings.Length; i++)
            {
                if (ringLife[i] <= 0f) continue;
                ringLife[i] -= dt;
                float k = 1f - Mathf.Clamp01(ringLife[i] / ringMax[i]);
                float r = Mathf.Lerp(0.5f, ringRadius[i], 1f - (1f - k) * (1f - k));
                rings[i].localScale = new Vector3(r, 1f, r);
                Color c = ringColor[i] * (1.8f * (1f - k));
                c.a = 1f - k;
                var rm = ringMats[i];
                if (rm != null)
                {
                    rm.color = c;
                    if (rm.HasProperty("_EmissionColor")) rm.SetColor("_EmissionColor", c);
                }
                if (ringLife[i] <= 0f) rings[i].gameObject.SetActive(false);
            }
            if (embers != null && follow != null)
            {
                emberTimer -= dt;
                if (emberTimer <= 0f)
                {
                    emberTimer = 0.09f / density;
                    Vector3 p = follow.position + follow.forward * Random.Range(4f, 18f) + new Vector3(Random.Range(-9f, 9f), Random.Range(-2f, 3f), Random.Range(-9f, 9f));
                    Emit(embers, p, new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 0.9f), Random.Range(-0.4f, 0.4f)), Random.Range(0.05f, 0.11f), Random.Range(2.5f, 4f), new Color32(255, 170, 90, 255));
                }
            }
            for (int i = 0; i < tracers.Length; i++)
            {
                if (tracerLife[i] <= 0f) continue;
                tracerLife[i] -= dt;
                if (tracerLife[i] <= 0f) tracers[i].enabled = false;
            }
            for (int i = 0; i < flashes.Length; i++)
            {
                if (flashLife[i] <= 0f) continue;
                flashLife[i] -= dt;
                flashes[i].intensity = flashStart[i] * Mathf.Clamp01(flashLife[i] / 0.16f);
                if (flashLife[i] <= 0f) flashes[i].enabled = false;
            }
        }

        void OnDestroy()
        {
            if (inst == this) inst = null;
        }
    }
}
