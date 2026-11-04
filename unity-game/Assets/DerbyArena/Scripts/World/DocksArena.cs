using System.Collections.Generic;
using DerbyArena.Core;
using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Season 6 arena "Container Port" (2.1), built entirely in code so it needs no download: a packed-dirt yard
    /// walled in by shipping containers, four container cover blocks in a pinwheel around an open centre, two jump
    /// humps on the centre line, a gantry crane with a hanging container behind the north wall, container stacks on
    /// the skyline, floodlight masts, tyres, drums and banners.
    ///
    /// Layout rules (checked by hand, see the numbers below): the centre circle of radius 9 stays free for the last
    /// stand zone; spawns, pickups, reactor cells, the extraction pad and the checkpoint ring (radius 24) keep at
    /// least a car width from every cover block; all collision is boxes. Everything static is merged with static
    /// batching, so the ~600 small parts cost a handful of draw calls.
    /// </summary>
    public static class DocksArena
    {
        const float Half = 36.2f;                 // inner face of the container wall
        const float BoxL = 12.2f, BoxH = 2.6f, BoxW = 2.44f;   // 40 ft container

        static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();

        static Material Lit(Color c, float rough, float metal)
        {
            int key = (Mathf.RoundToInt(c.r * 255) << 16) ^ (Mathf.RoundToInt(c.g * 255) << 8) ^ Mathf.RoundToInt(c.b * 255) ^ (Mathf.RoundToInt(rough * 10) << 24) ^ (Mathf.RoundToInt(metal * 10) << 28);
            Material m;
            if (mats.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Lit(c, rough, metal, Color.black, 0f);
            mats[key] = m;
            return m;
        }

        static readonly Color[] Paints =
        {
            new Color(0.55f, 0.16f, 0.1f),    // rust red
            new Color(0.12f, 0.3f, 0.55f),    // blue
            new Color(0.15f, 0.4f, 0.25f),    // green
            new Color(0.85f, 0.42f, 0.1f),    // orange
            new Color(0.45f, 0.47f, 0.5f),    // grey
            new Color(0.82f, 0.64f, 0.12f)    // yellow
        };

        public static ArenaInfo Info()
        {
            var info = new ArenaInfo();
            info.Kind = ArenaKind.Docks;
            info.Center = Vector3.zero;
            info.Radius = 30f;
            info.Bounds = new Rect(-35.5f, -35.5f, 71f, 71f);
            info.PlayerSpawn = new Vector3(0f, 0.6f, -28f);
            info.PlayerYaw = 0f;
            info.EnemySpawns = new[]
            {
                new Vector3(-24f, 0.6f, 10f), new Vector3(24f, 0.6f, 10f), new Vector3(-18f, 0.6f, 28f), new Vector3(18f, 0.6f, 28f),
                new Vector3(0f, 0.6f, 22f), new Vector3(-28f, 0.6f, -12f), new Vector3(28f, 0.6f, -12f), new Vector3(-8f, 0.6f, 26f)
            };
            info.CellPoints = new[]
            {
                new Vector3(-22f, 0.9f, -18f), new Vector3(22f, 0.9f, -18f), new Vector3(-30f, 0.9f, 14f),
                new Vector3(30f, 0.9f, 14f), new Vector3(-10f, 0.9f, 31f)
            };
            info.Extraction = new Vector3(0f, 0.05f, 31f);
            info.PickupPoints = new[]
            {
                new Vector3(-24f, 0.6f, -26f), new Vector3(24f, 0.6f, -26f), new Vector3(-26f, 0.6f, 24f), new Vector3(26f, 0.6f, 24f),
                new Vector3(0f, 0.6f, 0f), new Vector3(-30f, 0.6f, -2f), new Vector3(30f, 0.6f, -2f)
            };
            info.BossSpawn = new Vector3(6f, 0.8f, 24f);
            var gates = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float a = (180f + i * 45f) * Mathf.Deg2Rad;
                gates.Add(new Vector3(Mathf.Sin(a) * 24f, 0f, Mathf.Cos(a) * 24f));
            }
            info.GatePoints = gates.ToArray();
            info.AcidPoints = new[] { new Vector3(-20f, 0f, 4f), new Vector3(20f, 0f, -4f), new Vector3(-4f, 0f, -21f) };
            info.ZoneStart = 30f;
            info.ZoneEnd = 9f;
            return info;
        }

        /// <summary>One container: body (solid or visual), corrugation ribs on the given faces and dark door frames.</summary>
        static void Container(Transform parent, Vector3 center, float yaw, int paint, bool solid, bool ribsOuter, bool ribsInner, float len = BoxL)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Color c = Paints[((paint % Paints.Length) + Paints.Length) % Paints.Length];
            var body = Lit(c, 0.7f, 0.25f);
            var rib = Lit(c * 0.72f, 0.75f, 0.25f);
            var frame = Lit(new Color(0.1f, 0.1f, 0.11f), 0.6f, 0.4f);
            var cube = MeshFactory.UnitCube();
            if (solid)
            {
                var b = MeshFactory.SolidBox("Container", parent, center, new Vector3(len, BoxH, BoxW), rot, body);
                b.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else
            {
                Part(parent, cube, body, center, new Vector3(len, BoxH, BoxW), rot, true);
            }
            // vertical corrugation ribs about every 1.2 m, centred (only on faces that can be seen)
            int ribs = Mathf.Max(2, Mathf.RoundToInt((len - 1.5f) / 1.19f) + 1);
            float step = (len - 1.5f) / (ribs - 1);
            for (int side = -1; side <= 1; side += 2)
            {
                if ((side < 0 && !ribsInner) || (side > 0 && !ribsOuter)) continue;
                for (int i = 0; i < ribs; i++)
                {
                    float x = -len * 0.5f + 0.75f + i * step;
                    Part(parent, cube, rib, center + rot * new Vector3(x, 0f, side * (BoxW * 0.5f + 0.03f)), new Vector3(0.14f, BoxH * 0.9f, 0.06f), rot, false);
                }
            }
            // door ends: dark frame and two locking bars
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 e = center + rot * new Vector3(end * (len * 0.5f + 0.02f), 0f, 0f);
                Part(parent, cube, frame, e, new Vector3(0.05f, BoxH * 0.96f, BoxW * 0.96f), rot, false);
                for (int k = -1; k <= 1; k += 2)
                    Part(parent, cube, rib, e + rot * new Vector3(end * 0.04f, 0f, k * 0.45f), new Vector3(0.05f, BoxH * 0.85f, 0.07f), rot, false);
            }
        }

        static GameObject Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Quaternion rot, bool shadow)
        {
            var go = MeshFactory.Make("Part", mesh, mat, parent, Vector3.zero, scale);
            go.transform.SetPositionAndRotation(pos, rot);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
            return go;
        }

        /// <summary>Builds the geometry under <paramref name="root"/>. Returns a short status for diagnostics.</summary>
        public static string Build(Transform root, Atmosphere.Theme theme, int quality)
        {
            var shell = new GameObject("CollisionShell").transform;
            shell.SetParent(root, false);
            var stat = new GameObject("Static").transform;
            stat.SetParent(root, false);

            // ground: packed dirt with a darker worn oval and a painted line ring
            var dirt = Lit(new Color(0.5f, 0.4f, 0.3f), 0.95f, 0f);
            MeshFactory.SolidBox("Floor", shell, new Vector3(0f, -0.5f, 0f), new Vector3(130f, 1f, 130f), Quaternion.identity, dirt);   // reaches the skyline stacks
            var wear = Lit(new Color(0.36f, 0.28f, 0.21f), 0.95f, 0f);
            var oval = MeshFactory.Make("WornOval", MeshFactory.UnitRing(), wear, stat, new Vector3(0f, 0.012f, 0f), new Vector3(27f, 1f, 25f));
            oval.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lineMesh = MeshFactory.Ring(0.985f, 1f, 72);
            var line = MeshFactory.Make("PaintLine", lineMesh, Lit(new Color(0.85f, 0.83f, 0.78f), 0.8f, 0f), stat, new Vector3(0f, 0.016f, 0f), new Vector3(15f, 1f, 13f));
            line.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.AddComponent<DerbyArena.Vehicles.OwnedMesh>().Mesh = lineMesh;

            // container wall: solid lower row on all four sides, a staggered visual upper row on three sides
            int paint = 0;
            float wallC = Half + BoxW * 0.5f;
            for (int i = 0; i < 6; i++)
            {
                float p = -BoxL * 2.5f + i * BoxL;
                Container(stat, new Vector3(p, BoxH * 0.5f, wallC), 0f, paint++, true, false, true);
                Container(stat, new Vector3(p, BoxH * 0.5f, -wallC), 0f, paint++, true, true, false);
                // yaw 90 turns local +z into world +x: the east wall's inner face is local -z, the west wall's local +z
                Container(stat, new Vector3(wallC, BoxH * 0.5f, p), 90f, paint++, true, false, true);
                Container(stat, new Vector3(-wallC, BoxH * 0.5f, p), 90f, paint++, true, true, false);
            }
            for (int i = 0; i < 3; i++)
            {
                float p = -BoxL * 1.5f + i * BoxL * 1.5f;
                Container(stat, new Vector3(p, BoxH * 1.5f, wallC + 0.2f), 0f, paint + i * 2, false, false, true);
                Container(stat, new Vector3(wallC + 0.2f, BoxH * 1.5f, -p), 90f, paint + i * 2 + 1, false, false, true);
                Container(stat, new Vector3(-wallC - 0.2f, BoxH * 1.5f, p), 90f, paint + i * 2 + 3, false, true, false);
            }
            // tall invisible walls so a car launched off a ramp can never leave the yard
            MeshFactory.ColliderBox("BoundN", shell, new Vector3(0f, 6f, wallC + 1.5f), new Vector3(90f, 12f, 1f), Quaternion.identity);
            MeshFactory.ColliderBox("BoundS", shell, new Vector3(0f, 6f, -wallC - 1.5f), new Vector3(90f, 12f, 1f), Quaternion.identity);
            MeshFactory.ColliderBox("BoundE", shell, new Vector3(wallC + 1.5f, 6f, 0f), new Vector3(1f, 12f, 90f), Quaternion.identity);
            MeshFactory.ColliderBox("BoundW", shell, new Vector3(-wallC - 1.5f, 6f, 0f), new Vector3(1f, 12f, 90f), Quaternion.identity);

            // cover: pinwheel around the open centre, two 40 ft (one with a second on top each) and two 20 ft boxes.
            // Positions chosen by a clearance search: >= 3.2 m to every gate pylon, the straight gate-to-gate lines,
            // the player's start lane and the final zone circle; >= 8 m to every spawn, pickup and cell.
            Container(stat, new Vector3(-14f, BoxH * 0.5f, -5f), 90f, 3, true, true, true);
            Container(stat, new Vector3(14f, BoxH * 0.5f, 5f), 90f, 1, true, true, true);
            Container(stat, new Vector3(-8f, BoxH * 0.5f, 14f), 0f, 5, true, true, true, 6.06f);
            Container(stat, new Vector3(8f, BoxH * 0.5f, -14f), 0f, 0, true, true, true, 6.06f);
            Container(stat, new Vector3(-14f, BoxH * 1.5f, -5.2f), 92f, 2, true, true, true);
            Container(stat, new Vector3(14f, BoxH * 1.5f, 5.2f), 88f, 4, true, true, true);

            // jump humps on the centre line (striped hazard plates): a 9 degree climb toward the centre and a matching
            // descent behind its crest, so there is no vertical step for cars coming the other way or landing on it
            var hazard = Lit(new Color(0.95f, 0.72f, 0.12f), 0.6f, 0.1f);
            var stripe = Lit(new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.1f);
            for (int k = -1; k <= 1; k += 2)
            {
                Quaternion face = Quaternion.Euler(0f, k < 0 ? 0f : 180f, 0f);   // local +z points to the centre
                for (int half = 0; half < 2; half++)
                {
                    // climb: centre 10 m out, rising toward the centre; descent: 5.14 m further in, falling toward it.
                    // Both tops meet at the crest (z = +-7.43, y = 0.81) and both low ends sit flush with the floor.
                    Quaternion rot = face * Quaternion.Euler(half == 0 ? -9f : 9f, 0f, 0f);
                    Vector3 c = new Vector3(0f, -0.1f, k * (half == 0 ? 10f : 4.86f));
                    MeshFactory.SolidBox(half == 0 ? "JumpRamp" : "RampBack", shell, c, new Vector3(4f, 1f, 5.2f), rot, hazard);
                    for (int s = -1; s <= 1; s++)
                        Part(stat, MeshFactory.UnitCube(), stripe, c + rot * new Vector3(s * 1.3f, 0.505f, 0f), new Vector3(0.45f, 0.02f, 5f), rot, false);
                }
            }

            BuildCrane(stat, quality);
            BuildSkyline(stat);

            // floodlight masts on the corner containers, tyres, drums and banners at the wall
            var steel = Lit(new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f);
            var lamp = Mats.Glow(new Color(1f, 0.9f, 0.7f), quality >= 2 ? 4f : 3f);
            var cornerMat = Lit(new Color(0.18f, 0.18f, 0.2f), 0.6f, 0.4f);
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? 1f : -1f, sz = i < 2 ? 1f : -1f;
                // the wall rows stop at +-36.6: a corner block fills the gap the mast stands on
                Part(stat, MeshFactory.UnitCube(), cornerMat, new Vector3(sx * wallC, BoxH * 0.5f, sz * wallC), new Vector3(BoxW + 0.4f, BoxH, BoxW + 0.4f), Quaternion.identity, true);
                ArenaDressing.Floodlight(stat, new Vector3(sx * wallC, BoxH, sz * wallC), 12f, Vector3.zero, steel, lamp);
            }
            var rubber = Lit(new Color(0.05f, 0.05f, 0.055f), 0.9f, 0f);
            var rim = Lit(new Color(0.5f, 0.5f, 0.52f), 0.4f, 0.6f);
            var drumRed = Lit(new Color(0.6f, 0.1f, 0.06f), 0.6f, 0.2f);
            var drumBlue = Lit(new Color(0.1f, 0.25f, 0.55f), 0.6f, 0.2f);
            var band = Lit(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.4f);
            Vector3[] tyres = { new Vector3(-34.6f, 0f, -30f), new Vector3(34.6f, 0f, 30f), new Vector3(-34.6f, 0f, 20f), new Vector3(34.6f, 0f, -20f) };
            for (int i = 0; i < tyres.Length; i++) ArenaDressing.TyreStack(stat, tyres[i], 3 + i % 2, rubber, rim);
            Vector3[] drums = { new Vector3(-33.5f, 0f, 33.5f), new Vector3(33.5f, 0f, -33.5f), new Vector3(33.5f, 0f, 33.5f), new Vector3(-33.5f, 0f, -33.5f) };
            for (int i = 0; i < drums.Length; i++) ArenaDressing.Drum(stat, drums[i], i % 2 == 0 ? drumRed : drumBlue, band);
            if (quality > 0)
            {
                var pole = Lit(new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.5f);
                var cloth = Lit(new Color(0.08f, 0.2f, 0.45f), 0.8f, 0f);
                var bone = Lit(new Color(0.92f, 0.9f, 0.84f), 0.7f, 0f);
                Vector3[] banners = { new Vector3(-18f, BoxH, wallC), new Vector3(18f, BoxH, wallC), new Vector3(wallC, BoxH, -12f), new Vector3(-wallC, BoxH, 12f) };
                for (int i = 0; i < banners.Length; i++) ArenaDressing.Banner(stat, banners[i], Vector3.zero, pole, cloth, bone);
            }

            // sodium lamps over the yard
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Atmosphere.Lamp(root, new Vector3(Mathf.Sin(a) * 22f, 9f, Mathf.Cos(a) * 22f), i % 2 == 0 ? theme.Accent : new Color(0.75f, 0.85f, 1f), 1.5f, 26f);
            }

            int parts = stat.GetComponentsInChildren<MeshRenderer>(true).Length;
            try { StaticBatchingUtility.Combine(stat.gameObject); }
            catch (System.Exception ex) { Debug.LogWarning("DERBY: docks batching skipped. " + ex.Message); }
            return Loc.T("Konteyner Limanı hazır, ", "Container Port ready, ") + parts + Loc.T(" parça", " parts");
        }

        /// <summary>Gantry crane behind the north wall with a container hanging from the trolley (visual only).</summary>
        static void BuildCrane(Transform parent, int quality)
        {
            var yellow = Lit(new Color(0.9f, 0.66f, 0.1f), 0.55f, 0.35f);
            var dark = Lit(new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.5f);
            var cube = MeshFactory.UnitCube();
            const float z = 42f, h = 22f;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                    Part(parent, cube, yellow, new Vector3(sx * 40f, h * 0.5f, z + sz * 3.5f), new Vector3(1.1f, h, 1.1f), Quaternion.identity, true);
                Part(parent, cube, yellow, new Vector3(sx * 40f, h * 0.35f, z), new Vector3(0.6f, 0.6f, 7f), Quaternion.identity, false);
                Part(parent, cube, dark, new Vector3(sx * 40f, 0.6f, z), new Vector3(2.2f, 1.2f, 9f), Quaternion.identity, true);
            }
            Part(parent, cube, yellow, new Vector3(0f, h + 1f, z - 1.6f), new Vector3(82f, 2f, 1.2f), Quaternion.identity, true);
            Part(parent, cube, yellow, new Vector3(0f, h + 1f, z + 1.6f), new Vector3(82f, 2f, 1.2f), Quaternion.identity, true);
            for (int i = -6; i <= 6; i++)
                Part(parent, cube, yellow, new Vector3(i * 6f, h + 1f, z), new Vector3(0.4f, 1.6f, 3.2f), Quaternion.identity, false);
            Part(parent, cube, dark, new Vector3(-8f, h - 0.4f, z), new Vector3(4f, 2.2f, 4.6f), Quaternion.identity, true);
            Part(parent, cube, dark, new Vector3(-8f, h - 5.5f, z), new Vector3(0.12f, 8f, 0.12f), Quaternion.identity, false);
            Container(parent, new Vector3(-8f, h - 10.8f, z), 8f, 3, false, true, true);
            var glow = Mats.Glow(new Color(1f, 0.35f, 0.15f), 2.6f);
            for (int sx = -1; sx <= 1; sx += 2)
                Part(parent, cube, glow, new Vector3(sx * 40f, h + 2.3f, z), new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, false);
        }

        /// <summary>Container stacks outside the walls: a busy port skyline in the fog.</summary>
        static void BuildSkyline(Transform parent)
        {
            Vector3[] spots = { new Vector3(-50f, 0f, -20f), new Vector3(50f, 0f, 14f), new Vector3(-30f, 0f, -52f), new Vector3(24f, 0f, -54f), new Vector3(-52f, 0f, 30f), new Vector3(52f, 0f, -36f) };
            for (int s = 0; s < spots.Length; s++)
            {
                float yaw = (s % 2 == 0) ? 0f : 90f;
                int levels = 2 + s % 3;
                for (int y = 0; y < levels; y++)
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, k * (BoxW + 0.1f));
                        Container(parent, spots[s] + off + Vector3.up * (BoxH * (y + 0.5f)), yaw, s * 3 + y + k, false, false, false);
                    }
            }
        }
    }
}
