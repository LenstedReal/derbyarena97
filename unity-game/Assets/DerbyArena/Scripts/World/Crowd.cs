using System.Collections.Generic;
using DerbyArena.Audio;
using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Robot spectators on the Colosseum tribunes (24 baked groups from the Kotlin build, vertex coloured,
    /// glowing eyes). Groups bounce in a travelling wave; kills and big hits make the stands erupt.
    /// </summary>
    public sealed class Crowd : MonoBehaviour
    {
        public static Crowd Current;

        readonly List<Transform> groups = new List<Transform>();
        readonly List<float> baseY = new List<float>();
        float excitement = 0.25f;
        float cheerCooldown;

        public static void Spawn(Transform arenaRoot, int quality)
        {
            var go = new GameObject("RobotCrowd");
            go.transform.SetParent(arenaRoot, false);
            var crowd = go.AddComponent<Crowd>();
            Current = crowd;
            var opts = new GlbOptions();
            opts.VertexColors = true;
            opts.Key = "crowd";
            GlbLibrary.Spawn("arena/crowd.glb", opts, go.transform, (model, err) =>
            {
                if (crowd == null) return;
                if (model == null)
                {
                    DerbyArena.Core.DerbyStartupDiagnostics.Report("Seyirci", "yüklenemedi: " + err);
                    return;
                }
                crowd.Bind(model.transform, quality);
            });
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Bind(Transform model, int quality)
        {
            int index = 0;
            foreach (Transform child in model)
            {
                foreach (var r in child.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                bool keep = quality > 0 || index % 2 == 0;
                child.gameObject.SetActive(keep);
                if (keep)
                {
                    groups.Add(child);
                    baseY.Add(child.localPosition.y);
                }
                index++;
            }
        }

        /// <summary>0..1, stacks with the current excitement.</summary>
        public static void Cheer(float amount)
        {
            if (Current == null) return;
            Current.excitement = Mathf.Clamp01(Current.excitement + amount);
            if (Current.cheerCooldown <= 0f && amount >= 0.5f)
            {
                Current.cheerCooldown = 1.2f;
                AudioHub.Play2D(SynthAudio.Cheer(), 0.45f, Random.Range(0.95f, 1.05f));
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            cheerCooldown -= dt;
            excitement = Mathf.MoveTowards(excitement, 0.25f, dt * 0.18f);
            float time = Time.time;
            float amp = 0.08f + excitement * 0.32f;
            float speed = 5f + excitement * 6f;
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                if (g == null) continue;
                float wave = Mathf.Sin(time * speed - i * 0.55f);
                var p = g.localPosition;
                p.y = baseY[i] + Mathf.Max(0f, wave) * amp;
                g.localPosition = p;
            }
        }
    }
}
