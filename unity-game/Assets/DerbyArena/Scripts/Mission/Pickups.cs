using DerbyArena.Audio;
using DerbyArena.Combat;
using DerbyArena.Vehicles;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Mission
{
    public enum PickupKind { Health, Nitro, Ammo, Shield }

    /// <summary>Loot crate (Kotlin prop models) with spin, glow ring and respawn timer.</summary>
    public sealed class Pickup : MonoBehaviour
    {
        public PickupKind Kind;
        public float RespawnTime = 18f;
        public bool Available { get; private set; }
        Transform visual;
        float respawnAt;

        public static Color ColorOf(PickupKind k)
        {
            switch (k)
            {
                case PickupKind.Health: return new Color(0.25f, 1f, 0.4f);
                case PickupKind.Nitro: return new Color(0.25f, 0.6f, 1f);
                case PickupKind.Ammo: return new Color(1f, 0.6f, 0.15f);
                default: return new Color(0.85f, 0.85f, 1f);
            }
        }

        static string ModelOf(PickupKind k)
        {
            switch (k)
            {
                case PickupKind.Health: return "props/loot_health.glb";
                case PickupKind.Nitro: return "props/loot_nitro.glb";
                case PickupKind.Ammo: return "props/loot_ammo.glb";
                default: return "props/loot_armor.glb";
            }
        }

        public static Pickup Create(Transform parent, Vector3 pos, PickupKind kind)
        {
            var go = new GameObject("Pickup_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var p = go.AddComponent<Pickup>();
            p.Kind = kind;
            p.Build();
            return p;
        }

        void Build()
        {
            Color c = ColorOf(Kind);
            MeshFactory.Make("GlowRing", MeshFactory.UnitRing(), Mats.Glow(c, 2.2f), transform, new Vector3(0f, -0.5f, 0f), new Vector3(1.3f, 1f, 1.3f));
            var vis = new GameObject("Crate");
            vis.transform.SetParent(transform, false);
            vis.transform.localScale = Vector3.one * 1.25f;
            vis.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            vis.AddComponent<PickupSpin>();
            visual = vis.transform;
            GlbLibrary.Spawn(ModelOf(Kind), visual, (model, err) =>
            {
                if (this == null || visual == null) return;
                if (model == null)
                    MeshFactory.Make("Fallback", MeshFactory.UnitCube(), Mats.Glow(c, 1.5f), visual, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.8f);
                else
                    foreach (var r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            });
            Available = true;
        }

        void Update()
        {
            if (!Available && Time.time >= respawnAt)
            {
                Available = true;
                visual.gameObject.SetActive(true);
                Fx.Burst(transform.position, ColorOf(Kind), 8);
            }
        }

        public bool TryCollect(ArcadeCar car, VehicleHealth hp, WeaponRig weapons)
        {
            if (!Available || hp == null || hp.Dead) return false;
            switch (Kind)
            {
                case PickupKind.Health:
                    if (hp.Current >= hp.Max - 0.5f) return false;
                    hp.Heal(hp.Max * 0.4f);
                    break;
                case PickupKind.Nitro:
                    if (car.Nitro > 0.97f) return false;
                    car.Nitro = 1f;
                    break;
                case PickupKind.Ammo:
                    if (weapons == null) return false;
                    weapons.Rockets += 3;
                    break;
                default:
                    hp.AddShield(45f);
                    break;
            }
            Available = false;
            respawnAt = Time.time + RespawnTime;
            visual.gameObject.SetActive(false);
            Fx.Burst(transform.position, ColorOf(Kind), 14);
            AudioHub.Play2D(SynthAudio.Pickup(), 0.7f, 1f);
            return true;
        }
    }

    /// <summary>Reactor cell objective marker: glowing core, rings and a tall light column.</summary>
    public sealed class ReactorCell : MonoBehaviour
    {
        public bool Taken;
        Transform core;

        public static ReactorCell Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("ReactorCell");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var cell = go.AddComponent<ReactorCell>();
            var cyan = new Color(0.15f, 0.85f, 1f);
            var glow = Mats.Glow(cyan, 3f);
            var dark = Mats.Lit(new Color(0.1f, 0.11f, 0.12f), 0.4f, 0.4f, Color.black, 0f);
            var cyl = MeshFactory.UnitCylinder();
            var c = new GameObject("Core");
            c.transform.SetParent(go.transform, false);
            c.AddComponent<PickupSpin>().BobHeight = 0.25f;
            MeshFactory.Make("Crystal", cyl, glow, c.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.55f, 1.3f, 0.55f));
            MeshFactory.Make("CapTop", cyl, dark, c.transform, new Vector3(0f, 1.32f, 0f), new Vector3(0.8f, 0.16f, 0.8f));
            MeshFactory.Make("CapBottom", cyl, dark, c.transform, new Vector3(0f, -0.12f, 0f), new Vector3(0.8f, 0.16f, 0.8f));
            cell.core = c.transform;
            MeshFactory.Make("Pad", MeshFactory.UnitRing(), glow, go.transform, new Vector3(0f, -0.82f, 0f), new Vector3(2.6f, 1f, 2.6f));
            var beam = MeshFactory.Make("Beam", cyl, Mats.Glow(cyan, 1.6f), go.transform, new Vector3(0f, 9f, 0f), new Vector3(0.22f, 18f, 0.22f));
            beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return cell;
        }

        public void Collect()
        {
            if (Taken) return;
            Taken = true;
            Fx.Burst(transform.position + Vector3.up, new Color(0.2f, 0.9f, 1f), 30);
            AudioHub.Play2D(SynthAudio.Cell(), 0.8f, 1f);
            gameObject.SetActive(false);
        }
    }

    /// <summary>Extraction pad: ring + light column, pulses while the player holds position.</summary>
    public sealed class ExtractionPad : MonoBehaviour
    {
        public const float Radius = 5.5f;
        Transform ring;

        public static ExtractionPad Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Extraction");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var pad = go.AddComponent<ExtractionPad>();
            var green = new Color(0.2f, 1f, 0.45f);
            var r = MeshFactory.Make("Ring", MeshFactory.UnitRing(), Mats.Glow(green, 3f), go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(Radius, 1f, Radius));
            pad.ring = r.transform;
            MeshFactory.Make("Inner", MeshFactory.UnitRing(), Mats.Glow(green, 1.5f), go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(Radius * 0.55f, 1f, Radius * 0.55f));
            var beam = MeshFactory.Make("Beam", MeshFactory.UnitCylinder(), Mats.Glow(green, 1.4f), go.transform, new Vector3(0f, 12f, 0f), new Vector3(0.35f, 24f, 0.35f));
            beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return pad;
        }

        public void SetProgress(float p)
        {
            if (ring == null) return;
            float s = Radius * (1f + 0.06f * Mathf.Sin(Time.time * (4f + p * 10f)));
            ring.localScale = new Vector3(s, 1f, s);
        }
    }
}
