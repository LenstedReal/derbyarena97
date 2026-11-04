using System.Collections.Generic;
using DerbyArena.Core;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Derby "battle kit" bolted onto every car in code: roof light bar, bull bar, racing stripes and, per role or
    /// garage upgrade, armour plates, ram spikes, spoiler, side exhausts and a supercharger. Built in the car's local
    /// space from the fitted hull, so it fits every model. Materials are shared (one per colour), parts cast no
    /// shadows, and everything sits on the vehicle layer so hit flash and the wreck look apply to it too.
    /// </summary>
    public static class VehicleKit
    {
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

        static Material Lit(Color c, float rough, float metal)
        {
            int key = (Mathf.RoundToInt(c.r * 255) << 16) ^ (Mathf.RoundToInt(c.g * 255) << 8) ^ Mathf.RoundToInt(c.b * 255) ^ (Mathf.RoundToInt(rough * 10) << 24) ^ (Mathf.RoundToInt(metal * 10) << 28);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Lit(c, rough, metal, Color.black, 0f);
            cache[key] = m;
            return m;
        }

        static Material Glow(Color c, float energy)
        {
            int key = 0x7000000 ^ (Mathf.RoundToInt(c.r * 255) << 16) ^ (Mathf.RoundToInt(c.g * 255) << 8) ^ Mathf.RoundToInt(c.b * 255) ^ (Mathf.RoundToInt(energy * 10) << 20);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Glow(c, energy);
            cache[key] = m;
            return m;
        }

        static GameObject Part(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var go = MeshFactory.Make(name, mesh, mat, parent, pos, scale);
            go.transform.localRotation = rot;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
            go.layer = VehicleVisual.VehicleLayer;
            return go;
        }

        public static void Build(Transform car, VehicleSpec spec, BoxCollider hull)
        {
            if (car == null || spec == null) return;
            int quality = Save.Data.Quality;
            var root = new GameObject("BattleKit").transform;
            root.SetParent(car, false);

            Vector3 c = hull != null ? hull.center : new Vector3(0f, spec.BodyCenterY * spec.Scale, 0f);
            Vector3 s = hull != null ? hull.size : spec.BodySize * spec.Scale;
            float top = c.y + s.y * 0.5f;
            float front = c.z + s.z * 0.5f;
            float back = c.z - s.z * 0.5f;
            float halfW = s.x * 0.5f;
            float low = c.y - s.y * 0.5f;
            var cube = MeshFactory.UnitCube();
            var cyl = MeshFactory.UnitCylinder();

            bool player = spec.Role == DriverRole.Player;
            bool boss = spec.Role == DriverRole.Boss;
            Color steel = new Color(0.16f, 0.16f, 0.17f);
            Color rusty = new Color(0.32f, 0.2f, 0.12f);
            var steelMat = Lit(steel, 0.45f, 0.45f);
            var rustMat = Lit(rusty, 0.8f, 0.2f);
            Color stripe = spec.Elite ? new Color(0.75f, 0.45f, 1f) : (player ? new Color(0.95f, 0.93f, 0.88f) : Color.Lerp(spec.Accent, Color.black, 0.45f));

            // roof light bar (every car): frame + 4 lamps; the player's lamps are brighter
            float barY = top + 0.12f;
            float barZ = c.z + s.z * 0.06f;
            Part("RoofBar", cube, steelMat, root, new Vector3(0f, barY, barZ), new Vector3(s.x * 0.62f, 0.07f, 0.12f), Quaternion.identity);
            Part("RoofLegL", cube, steelMat, root, new Vector3(-s.x * 0.28f, barY - 0.07f, barZ), new Vector3(0.06f, 0.14f, 0.06f), Quaternion.identity);
            Part("RoofLegR", cube, steelMat, root, new Vector3(s.x * 0.28f, barY - 0.07f, barZ), new Vector3(0.06f, 0.14f, 0.06f), Quaternion.identity);
            var lampMat = Glow(new Color(1f, 0.93f, 0.75f), player ? 3.2f : 2.2f);
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Lerp(-s.x * 0.25f, s.x * 0.25f, i / 3f);
                Part("RoofLamp", cyl, lampMat, root, new Vector3(x, barY + 0.08f, barZ + 0.04f), new Vector3(0.16f, 0.06f, 0.16f), Quaternion.Euler(90f, 0f, 0f));
            }

            // racing stripes over hood, roof and trunk
            for (int side = -1; side <= 1; side += 2)
                Part("Stripe", cube, Lit(stripe, 0.4f, 0.1f), root, new Vector3(side * s.x * 0.09f, top + 0.012f, c.z), new Vector3(s.x * 0.08f, 0.02f, s.z * 0.92f), Quaternion.identity);

            // bull bar: two rails and uprights in front of the grille
            float barFrontZ = front + 0.12f;
            float railY = Mathf.Max(low + 0.25f, c.y - s.y * 0.15f);
            Part("BullRailLow", cyl, steelMat, root, new Vector3(0f, railY, barFrontZ), new Vector3(0.11f, s.x * 0.86f, 0.11f), Quaternion.Euler(0f, 0f, 90f));
            Part("BullRailHigh", cyl, steelMat, root, new Vector3(0f, railY + 0.32f, barFrontZ - 0.04f), new Vector3(0.09f, s.x * 0.62f, 0.09f), Quaternion.Euler(0f, 0f, 90f));
            for (int i = -1; i <= 1; i += 2)
                Part("BullPost", cyl, steelMat, root, new Vector3(i * s.x * 0.24f, railY + 0.16f, barFrontZ - 0.02f), new Vector3(0.09f, 0.36f, 0.09f), Quaternion.identity);

            int armor = player ? Upgrades.Level(Upgrades.Armor) : (spec.Role == DriverRole.Rammer || boss ? 3 : (spec.Elite ? 2 : 0));
            int engine = player ? Upgrades.Level(Upgrades.Engine) : (boss ? 4 : 0);
            int nitro = player ? Upgrades.Level(Upgrades.Nitro) : 0;

            // ram spikes (rammers, boss, player with armour 4+)
            if (spec.Role == DriverRole.Rammer || boss || armor >= 4)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = Mathf.Lerp(-s.x * 0.36f, s.x * 0.36f, i / 4f);
                    Part("RamSpike", cyl, rustMat, root, new Vector3(x, railY + 0.02f, barFrontZ + 0.3f), new Vector3(0.07f, 0.55f, 0.07f), Quaternion.Euler(90f, 0f, 0f));
                }
            }

            // armour plates on the doors (2+) and a roof cage (3+)
            if (armor >= 2)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Part("DoorPlate", cube, rustMat, root, new Vector3(side * (halfW + 0.03f), c.y + 0.05f, c.z + s.z * 0.04f), new Vector3(0.05f, s.y * 0.42f, s.z * 0.42f), Quaternion.identity);
                    for (int b = -1; b <= 1; b += 2)
                        Part("Bolt", cube, steelMat, root, new Vector3(side * (halfW + 0.06f), c.y + 0.15f, c.z + s.z * (0.04f + b * 0.17f)), new Vector3(0.04f, 0.06f, 0.06f), Quaternion.identity);
                }
            }
            if (armor >= 3 && quality > 0)
            {
                for (int i = -1; i <= 1; i++)
                    Part("RollBar", cyl, steelMat, root, new Vector3(0f, top + 0.05f, c.z - s.z * 0.12f + i * 0.4f), new Vector3(0.05f, s.x * 0.7f, 0.05f), Quaternion.Euler(0f, 0f, 90f));
            }

            // spoiler (player, drifters, elites)
            if (player || spec.Role == DriverRole.Flanker || spec.Elite)
            {
                float wingY = top + (player ? 0.32f : 0.22f);
                Part("SpoilerWing", cube, Lit(Color.Lerp(spec.Paint, Color.black, 0.55f), 0.5f, 0.2f), root, new Vector3(0f, wingY, back + 0.2f), new Vector3(s.x * 0.86f, 0.05f, 0.42f), Quaternion.Euler(-6f, 0f, 0f));
                for (int side = -1; side <= 1; side += 2)
                    Part("SpoilerPost", cube, steelMat, root, new Vector3(side * s.x * 0.3f, (wingY + top) * 0.5f, back + 0.22f), new Vector3(0.05f, wingY - top, 0.12f), Quaternion.identity);
            }

            // supercharger through the hood (engine 3+, boss)
            if (engine >= 3)
            {
                float hz = c.z + s.z * 0.26f;
                Part("Blower", cube, steelMat, root, new Vector3(0f, top + 0.12f, hz), new Vector3(0.42f, 0.24f, 0.55f), Quaternion.identity);
                Part("Scoop", cube, Lit(new Color(0.75f, 0.72f, 0.68f), 0.3f, 0.6f), root, new Vector3(0f, top + 0.3f, hz + 0.05f), new Vector3(0.46f, 0.12f, 0.32f), Quaternion.identity);
                Part("ScoopMouth", cube, Lit(new Color(0.02f, 0.02f, 0.02f), 0.9f, 0f), root, new Vector3(0f, top + 0.3f, hz + 0.22f), new Vector3(0.36f, 0.07f, 0.02f), Quaternion.identity);
            }

            // side exhausts (nitro 2+, boss)
            if (nitro >= 2 || boss)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Part("SidePipe", cyl, Lit(new Color(0.55f, 0.52f, 0.48f), 0.3f, 0.6f), root, new Vector3(side * (halfW + 0.07f), low + 0.28f, c.z), new Vector3(0.12f, s.z * 0.5f, 0.12f), Quaternion.Euler(90f, 0f, 0f));
                    Part("PipeTip", cyl, Glow(new Color(1f, 0.45f, 0.15f), 1.4f), root, new Vector3(side * (halfW + 0.07f), low + 0.28f, c.z - s.z * 0.25f - 0.02f), new Vector3(0.1f, 0.03f, 0.1f), Quaternion.Euler(90f, 0f, 0f));
                }
            }

            Merge(root);
        }

        /// <summary>
        /// 2.1: the 20-35 kit parts become one mesh per material (usually 4-6 draw calls per car instead of ~30).
        /// The merged children move with the car; the originals are removed at once so the renderer list the visual
        /// collects next (hit flash, wreck look) only sees the merged pieces.
        /// </summary>
        static void Merge(Transform root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length < 2) return;
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var order = new List<Material>();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            for (int i = 0; i < filters.Length; i++)
            {
                var f = filters[i];
                var r = f.GetComponent<MeshRenderer>();
                if (r == null || f.sharedMesh == null || r.sharedMaterial == null) continue;
                List<CombineInstance> list;
                if (!groups.TryGetValue(r.sharedMaterial, out list))
                {
                    list = new List<CombineInstance>();
                    groups[r.sharedMaterial] = list;
                    order.Add(r.sharedMaterial);
                }
                var ci = new CombineInstance();
                ci.mesh = f.sharedMesh;
                ci.subMeshIndex = 0;
                ci.transform = toRoot * f.transform.localToWorldMatrix;
                list.Add(ci);
            }
            for (int i = 0; i < order.Count; i++)
            {
                var mesh = new Mesh();
                mesh.name = "BattleKit";
                mesh.CombineMeshes(groups[order[i]].ToArray(), true, true);
                mesh.RecalculateBounds();
                var go = new GameObject("KitMerged");
                go.transform.SetParent(root, false);
                go.layer = VehicleVisual.VehicleLayer;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = order[i];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
                go.AddComponent<OwnedMesh>().Mesh = mesh;
            }
            for (int i = 0; i < filters.Length; i++)
                if (filters[i] != null) Object.DestroyImmediate(filters[i].gameObject);
        }
    }

    /// <summary>Frees a mesh created at runtime together with its GameObject.</summary>
    public sealed class OwnedMesh : MonoBehaviour
    {
        public Mesh Mesh;
        void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }
}
