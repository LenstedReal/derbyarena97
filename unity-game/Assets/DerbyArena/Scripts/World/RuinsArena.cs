using System.Collections.Generic;
using DerbyArena.Core;
using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// "Desert Ruins" arena (4.0), built entirely in code: a destroyed desert town. Sand ground with a dirt ring road
    /// and two cross streets, four ruined houses as cover in a pinwheel around the open centre, a broken perimeter
    /// wall, ruined blocks, a minaret stump and dead palms on the skyline, rubble piles, fallen beams, drums, tyres,
    /// a dust haze and warm lamps. Textures come from Resources/Derby/Textures/ruins_* (tileable, with normals).
    ///
    /// Layout rules: the centre circle of radius 9 stays free (last stand zone); the checkpoint ring (radius 26)
    /// and the straight gate-to-gate lines keep a car width from every cover block; spawns, pickups and cells keep
    /// 8 m from cover; every blocking piece is a box collider. Static parts are batched.
    /// </summary>
    public static class RuinsArena
    {
        const float Half = 36.5f;                 // inner face of the perimeter wall

        static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
        static Material sandMat, roadMat, brickMat, plasterMat;

        static Material Lit(Color c, float rough, float metal)
        {
            int key = (Mathf.RoundToInt(c.r * 255) << 16) ^ (Mathf.RoundToInt(c.g * 255) << 8) ^ Mathf.RoundToInt(c.b * 255) ^ (Mathf.RoundToInt(rough * 10) << 24) ^ (Mathf.RoundToInt(metal * 10) << 28);
            Material m;
            if (mats.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Lit(c, rough, metal, Color.black, 0f);
            mats[key] = m;
            return m;
        }

        static Material Textured(string name, Color tint, float smooth, Color fallback)
        {
            var albedo = Resources.Load<Texture2D>("Derby/Textures/" + name);
            var normal = Resources.Load<Texture2D>("Derby/Textures/" + name + "_n");
            if (albedo == null) return Lit(fallback, 0.9f, 0f);
            var m = Mats.TexturedNormal(albedo, normal, tint, smooth, 0f);
            if (m == null) return Lit(fallback, 0.9f, 0f);
            m.enableInstancing = true;
            return m;
        }

        public static ArenaInfo Info()
        {
            var info = new ArenaInfo();
            info.Kind = ArenaKind.Ruins;
            info.Center = Vector3.zero;
            info.Radius = 34f;
            info.Bounds = new Rect(-Half, -Half, Half * 2f, Half * 2f);
            info.PlayerSpawn = new Vector3(0f, 0.6f, -29f);
            info.PlayerYaw = 0f;
            info.EnemySpawns = new[]
            {
                new Vector3(-24f, 0.6f, 12f), new Vector3(24f, 0.6f, 12f), new Vector3(-16f, 0.6f, 28f), new Vector3(16f, 0.6f, 28f),
                new Vector3(0f, 0.6f, 24f), new Vector3(-29f, 0.6f, -10f), new Vector3(29f, 0.6f, -10f), new Vector3(-6f, 0.6f, 31f)
            };
            info.CellPoints = new[] { new Vector3(-23f, 0.9f, -20f), new Vector3(23f, 0.9f, -20f), new Vector3(-31f, 0.9f, 15f), new Vector3(31f, 0.9f, 15f), new Vector3(-8f, 0.9f, 32f) };
            info.Extraction = new Vector3(0f, 0.05f, 32f);
            info.PickupPoints = new[]
            {
                new Vector3(-24f, 0.6f, -27f), new Vector3(24f, 0.6f, -27f), new Vector3(-27f, 0.6f, 25f), new Vector3(27f, 0.6f, 25f),
                new Vector3(0f, 0.6f, 0f), new Vector3(-31f, 0.6f, -2f), new Vector3(31f, 0.6f, -2f)
            };
            info.BossSpawn = new Vector3(6f, 0.8f, 25f);
            var gates = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float a = (180f + i * 45f) * Mathf.Deg2Rad;
                gates.Add(new Vector3(Mathf.Sin(a) * 26f, 0f, Mathf.Cos(a) * 26f));
            }
            info.GatePoints = gates.ToArray();
            info.AcidPoints = new[] { new Vector3(-21f, 0f, 4f), new Vector3(21f, 0f, -4f), new Vector3(-4f, 0f, -22f) };
            info.ZoneStart = 31f;
            info.ZoneEnd = 9f;
            return info;
        }

        const float Tile = 2.5f;   // metres per texture repeat on walls and rubble

        static bool IsTextured(Material m) { return m == sandMat || m == roadMat || m == brickMat || m == plasterMat; }

        /// <summary>
        /// Visual part. Textured materials get a box mesh with world-metre UVs (so the brick texture repeats
        /// properly instead of stretching over the wall); plain colours share the unit cube.
        /// </summary>
        static GameObject Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Quaternion rot, bool shadow)
        {
            GameObject go;
            if (mesh == MeshFactory.UnitCube() && IsTextured(mat))
            {
                var tiled = MeshFactory.BoxTiled(scale, Tile);
                go = MeshFactory.Make("Part", tiled, mat, parent, Vector3.zero, Vector3.one);
                go.AddComponent<DerbyArena.Vehicles.OwnedMesh>().Mesh = tiled;
            }
            else go = MeshFactory.Make("Part", mesh, mat, parent, Vector3.zero, scale);
            go.transform.SetPositionAndRotation(pos, rot);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
            return go;
        }

        /// <summary>A wall segment with collision (solid) or visual only, from the given centre.</summary>
        static void Wall(Transform shell, Transform stat, Vector3 c, Vector3 size, float yaw, Material mat, bool solid)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            if (solid)
            {
                var b = MeshFactory.SolidBoxTiled("Wall", shell, c, size, rot, mat, Tile);
                b.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else Part(stat, MeshFactory.UnitCube(), mat, c, size, rot, true);
        }

        /// <summary>
        /// Ruined house: four walls split into segments of uneven, broken height, window and door gaps, a sagging
        /// roof slab, interior floor, rubble along the base. Footprint w x d, yaw around the centre.
        /// </summary>
        static void House(Transform shell, Transform stat, System.Random rng, Vector3 c, float w, float d, float yaw, bool solid, bool tall)
        {
            Material wall = rng.NextDouble() < 0.5 ? brickMat : plasterMat;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            float th = 0.45f;
            float baseH = tall ? 4.2f : 3.0f;
            // four walls: each in 4 segments with its own height; one segment per wall may be a gap (window or door)
            for (int side = 0; side < 4; side++)
            {
                bool along = side < 2;                       // walls along local x (front/back) or z (left/right)
                float len = along ? w : d;
                float off = (side % 2 == 0 ? 1f : -1f) * ((along ? d : w) * 0.5f - th * 0.5f);
                int segs = 4;
                float segLen = len / segs;
                int gap = rng.Next(0, segs);
                for (int s = 0; s < segs; s++)
                {
                    float t = -len * 0.5f + segLen * (s + 0.5f);
                    float h = baseH * (float)(0.45 + rng.NextDouble() * 0.6);
                    if (side == 1 && s == 1) h = baseH * 0.25f;     // the collapsed corner
                    Vector3 local = along ? new Vector3(t, 0f, off) : new Vector3(off, 0f, t);
                    if (s == gap && h > 1.6f)
                    {
                        // opening: a sill below and a lintel above (door when the sill is at the floor)
                        bool door = rng.NextDouble() < 0.4;
                        float sillH = door ? 0f : 1.0f;
                        if (sillH > 0f) Wall(shell, stat, c + rot * (local + Vector3.up * sillH * 0.5f), along ? new Vector3(segLen, sillH, th) : new Vector3(th, sillH, segLen), yaw, wall, solid);
                        float lintelY = Mathf.Min(h, 2.3f);
                        Wall(shell, stat, c + rot * (local + Vector3.up * (lintelY + (h - lintelY) * 0.5f)), along ? new Vector3(segLen, Mathf.Max(0.3f, h - lintelY), th) : new Vector3(th, Mathf.Max(0.3f, h - lintelY), segLen), yaw, wall, false);
                        continue;
                    }
                    Wall(shell, stat, c + rot * (local + Vector3.up * h * 0.5f), along ? new Vector3(segLen, h, th) : new Vector3(th, h, segLen), yaw, wall, solid);
                    // broken top: a smaller crumbled block
                    if (rng.NextDouble() < 0.5)
                        Part(stat, MeshFactory.UnitCube(), wall, c + rot * (local + new Vector3((float)(rng.NextDouble() - 0.5) * segLen * 0.4f, h + 0.18f, 0f)),
                            along ? new Vector3(segLen * 0.4f, 0.36f, th) : new Vector3(th, 0.36f, segLen * 0.4f), rot * Quaternion.Euler(0f, 0f, (float)(rng.NextDouble() - 0.5) * 12f), true);
                }
            }
            // floor slab and a sagging roof remnant over one corner
            Part(stat, MeshFactory.UnitCube(), plasterMat, c + Vector3.up * 0.05f, new Vector3(w - th, 0.1f, d - th), rot, false);
            Part(stat, MeshFactory.UnitCube(), Lit(new Color(0.36f, 0.3f, 0.24f), 0.85f, 0f), c + rot * new Vector3(w * 0.22f, baseH * 0.62f, d * 0.2f), new Vector3(w * 0.5f, 0.18f, d * 0.55f), rot * Quaternion.Euler(14f, 0f, -6f), true);
            // rubble skirt
            Rubble(stat, rng, c + rot * new Vector3(-w * 0.5f, 0f, d * 0.5f), 1.6f, 7);
            Rubble(stat, rng, c + rot * new Vector3(w * 0.5f, 0f, -d * 0.45f), 1.3f, 5);
        }

        static void Rubble(Transform stat, System.Random rng, Vector3 at, float radius, int count)
        {
            var cube = MeshFactory.UnitCube();
            Material[] pool = { brickMat, plasterMat, Lit(new Color(0.55f, 0.48f, 0.38f), 0.95f, 0f) };
            // low mound then chunks
            Part(stat, cube, Lit(new Color(0.62f, 0.52f, 0.38f), 0.95f, 0f), at + Vector3.up * 0.12f, new Vector3(radius * 1.6f, 0.25f, radius * 1.3f), Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f), false);
            for (int i = 0; i < count; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = (float)rng.NextDouble() * radius;
                float s = 0.3f + (float)rng.NextDouble() * 0.6f;
                Part(stat, cube, pool[rng.Next(pool.Length)], at + new Vector3(Mathf.Cos(a) * r, s * 0.35f, Mathf.Sin(a) * r), new Vector3(s, s * 0.7f, s * 1.2f),
                    Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f), true);
            }
        }

        static void Beam(Transform stat, System.Random rng, Vector3 foot, float len, float yaw, float tilt)
        {
            var wood = Lit(new Color(0.34f, 0.26f, 0.18f), 0.9f, 0f);
            var rot = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
            Part(stat, MeshFactory.UnitCube(), wood, foot + rot * new Vector3(0f, 0f, len * 0.5f), new Vector3(0.28f, 0.24f, len), rot, true);
        }

        static void Palm(Transform stat, System.Random rng, Vector3 foot, float h)
        {
            var trunk = Lit(new Color(0.4f, 0.32f, 0.22f), 0.9f, 0f);
            var frond = Lit(new Color(0.42f, 0.4f, 0.22f), 0.85f, 0f);
            var cyl = MeshFactory.UnitCylinder();
            float lean = (float)(rng.NextDouble() - 0.5) * 16f;
            var rot = Quaternion.Euler(lean, (float)rng.NextDouble() * 360f, 0f);
            Part(stat, cyl, trunk, foot + rot * new Vector3(0f, h * 0.5f, 0f), new Vector3(0.5f, h, 0.5f), rot, true);
            Vector3 top = foot + rot * new Vector3(0f, h, 0f);
            int fronds = 5 + rng.Next(3);
            for (int i = 0; i < fronds; i++)
            {
                float a = i * 360f / fronds + (float)rng.NextDouble() * 20f;
                var fr = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(-35f - (float)rng.NextDouble() * 30f, 0f, 0f);
                Part(stat, MeshFactory.UnitCube(), frond, top + fr * new Vector3(0f, 0f, 1.6f), new Vector3(0.5f, 0.06f, 3.2f), fr, false);
            }
        }

        /// <summary>Drifting dust haze over the arena (quality 1+): big soft sprites blown slowly by the wind.</summary>
        static void Dust(Transform root, int quality)
        {
            if (quality <= 0) return;
            var go = new GameObject("DustHaze");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0f, 2.5f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = quality >= 2 ? 48 : 28;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startColor = new Color(0.78f, 0.66f, 0.46f, 0.07f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission;
            em.rateOverTime = quality >= 2 ? 4.5f : 2.6f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(80f, 3f, 80f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var mat = Mats.FxSoft();
            if (mat != null) r.sharedMaterial = mat;
            ps.Play();
        }

        /// <summary>Builds the geometry under <paramref name="root"/>. Returns a short status for diagnostics.</summary>
        public static string Build(Transform root, Atmosphere.Theme theme, int quality)
        {
            var rng = new System.Random(4040);
            sandMat = Textured("ruins_sand", new Color(0.98f, 0.94f, 0.86f), 0.12f, new Color(0.72f, 0.62f, 0.44f));
            roadMat = Textured("ruins_road", new Color(0.9f, 0.86f, 0.8f), 0.18f, new Color(0.48f, 0.42f, 0.32f));
            brickMat = Textured("ruins_brick", new Color(0.95f, 0.9f, 0.82f), 0.2f, new Color(0.66f, 0.52f, 0.36f));
            plasterMat = Textured("ruins_plaster", new Color(0.96f, 0.93f, 0.86f), 0.22f, new Color(0.72f, 0.66f, 0.56f));

            var shell = new GameObject("CollisionShell").transform;
            shell.SetParent(root, false);
            var stat = new GameObject("Static").transform;
            stat.SetParent(root, false);

            // ground: one big sand slab (textures repeat every ~6 m through the UV scale of the unit cube), dirt ring
            // road around the centre and two cross streets, all flush
            var floor = MeshFactory.SolidBoxTiled("Floor", shell, new Vector3(0f, -0.5f, 0f), new Vector3(160f, 1f, 160f), Quaternion.identity, sandMat, 6f);
            floor.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var ringMesh = MeshFactory.RingPlanar(18.6f, 30f, 72, 6f);
            var ring = MeshFactory.Make("RingRoad", ringMesh, roadMat, stat, new Vector3(0f, 0.012f, 0f), Vector3.one);
            ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.AddComponent<DerbyArena.Vehicles.OwnedMesh>().Mesh = ringMesh;
            Part(stat, MeshFactory.UnitCube(), roadMat, new Vector3(0f, 0.008f, 0f), new Vector3(6f, 0.016f, 74f), Quaternion.identity, false);
            Part(stat, MeshFactory.UnitCube(), roadMat, new Vector3(0f, 0.008f, 0f), new Vector3(74f, 0.016f, 6f), Quaternion.identity, false);
            // the open centre: a cracked plaza slab
            Part(stat, MeshFactory.UnitCube(), plasterMat, new Vector3(0f, 0.02f, 0f), new Vector3(15f, 0.04f, 15f), Quaternion.Euler(0f, 45f, 0f), false);

            // perimeter: broken low wall in segments with gaps, tall invisible bounds behind it
            for (int side = 0; side < 4; side++)
            {
                float yaw = side * 90f;
                var rot = Quaternion.Euler(0f, yaw, 0f);
                for (int s = -6; s <= 6; s++)
                {
                    float t = s * 5.6f;
                    float h = 1.4f + (float)rng.NextDouble() * 1.6f;
                    if (rng.NextDouble() < 0.12) h = 0.5f;            // breach
                    Vector3 c = rot * new Vector3(t, h * 0.5f, Half + 0.6f);
                    Wall(shell, stat, c, new Vector3(5.3f, h, 1.2f), yaw, brickMat, true);
                    if (rng.NextDouble() < 0.5) Rubble(stat, rng, rot * new Vector3(t + 1.5f, 0f, Half - 1.2f), 1.0f, 3);
                }
                MeshFactory.ColliderBox("Bound" + side, shell, rot * new Vector3(0f, 6f, Half + 1.5f), new Vector3(90f, 12f, 1f), rot);
            }

            // cover: four ruined houses in a pinwheel around the plaza (clear of the gate ring at 26 and the centre 9)
            House(shell, stat, rng, new Vector3(-15f, 0f, -6f), 7f, 9f, 90f, true, true);
            House(shell, stat, rng, new Vector3(15f, 0f, 6f), 7f, 9f, 90f, true, false);
            House(shell, stat, rng, new Vector3(-8f, 0f, 15f), 9f, 6.5f, 0f, true, false);
            House(shell, stat, rng, new Vector3(8f, 0f, -15f), 9f, 6.5f, 0f, true, true);
            // fallen beams and debris near the houses and the streets (visual, no collision)
            Beam(stat, rng, new Vector3(-19f, 0.1f, 1f), 5f, 20f, -22f);
            Beam(stat, rng, new Vector3(19f, 0.1f, -1f), 4.5f, 200f, -28f);
            Beam(stat, rng, new Vector3(-12f, 0.1f, 19f), 4f, 110f, -18f);
            Rubble(stat, rng, new Vector3(22f, 0f, 22f), 1.6f, 6);
            Rubble(stat, rng, new Vector3(-22f, 0f, -22f), 1.6f, 6);
            Rubble(stat, rng, new Vector3(30f, 0f, 8f), 1.2f, 4);
            Rubble(stat, rng, new Vector3(-30f, 0f, -8f), 1.2f, 4);

            // skyline beyond the wall: ruined blocks, a minaret stump, dead palms, dunes
            for (int i = 0; i < 14; i++)
            {
                float a = i * 360f / 14f + (float)rng.NextDouble() * 12f;
                float r = 46f + (float)rng.NextDouble() * 14f;
                Vector3 c = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * r, 0f, Mathf.Cos(a * Mathf.Deg2Rad) * r);
                House(shell, stat, rng, c, 7f + (float)rng.NextDouble() * 5f, 7f + (float)rng.NextDouble() * 5f, a, false, rng.NextDouble() < 0.5);
            }
            // minaret stump: a tall cylinder with a broken top and a balcony ring
            var cyl = MeshFactory.UnitCylinder();
            Part(stat, cyl, plasterMat, new Vector3(-48f, 7f, 30f), new Vector3(3.2f, 14f, 3.2f), Quaternion.identity, true);
            Part(stat, MeshFactory.UnitRing(), plasterMat, new Vector3(-48f, 10.5f, 30f), new Vector3(5f, 1f, 5f), Quaternion.identity, false);
            Part(stat, cyl, brickMat, new Vector3(-48f, 15f, 30f), new Vector3(2.6f, 2.4f, 2.6f), Quaternion.Euler(8f, 0f, 5f), true);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f + (float)rng.NextDouble() * 15f;
                float r = 42f + (float)rng.NextDouble() * 10f;
                Palm(stat, rng, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * r, 0f, Mathf.Cos(a * Mathf.Deg2Rad) * r), 6f + (float)rng.NextDouble() * 4f);
            }
            var dune = Lit(new Color(0.78f, 0.66f, 0.46f), 0.95f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 20f;
                Vector3 c = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 72f, -1.5f, Mathf.Cos(a * Mathf.Deg2Rad) * 72f);
                Part(stat, MeshFactory.UnitCylinder(), dune, c, new Vector3(34f + i * 3f, 5f + (i % 3) * 2f, 20f), Quaternion.Euler(0f, a, 0f), false);
            }

            // props: drums, tyres, a lamp post with a warm bulb at each street end, smoke from a rubble fire
            var steel = Lit(new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f);
            var lamp = Mats.Glow(new Color(1f, 0.85f, 0.6f), quality >= 2 ? 4f : 3f);
            var rubber = Lit(new Color(0.05f, 0.05f, 0.055f), 0.9f, 0f);
            var rim = Lit(new Color(0.5f, 0.5f, 0.52f), 0.4f, 0.6f);
            var drumRed = Lit(new Color(0.55f, 0.12f, 0.08f), 0.6f, 0.2f);
            var drumSand = Lit(new Color(0.6f, 0.52f, 0.3f), 0.6f, 0.2f);
            var band = Lit(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.4f);
            Vector3[] tyres = { new Vector3(-33.5f, 0f, -30f), new Vector3(33.5f, 0f, 30f), new Vector3(-33.5f, 0f, 20f), new Vector3(33.5f, 0f, -20f) };
            for (int i = 0; i < tyres.Length; i++) ArenaDressing.TyreStack(stat, tyres[i], 2 + i % 2, rubber, rim);
            Vector3[] drums = { new Vector3(-32.5f, 0f, 32.5f), new Vector3(32.5f, 0f, -32.5f), new Vector3(32.5f, 0f, 32.5f), new Vector3(-32.5f, 0f, -32.5f), new Vector3(-20f, 0f, 30f), new Vector3(20f, 0f, -30f) };
            for (int i = 0; i < drums.Length; i++) ArenaDressing.Drum(stat, drums[i], i % 2 == 0 ? drumRed : drumSand, band);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                Vector3 foot = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 31f, 0f, Mathf.Cos(a * Mathf.Deg2Rad) * 31f);
                ArenaDressing.Floodlight(stat, foot, 8f, Vector3.zero, steel, lamp, quality > 0 ? ArenaDressing.ShaftMaterial(new Color(1f, 0.85f, 0.6f)) : null);
            }
            // warm lamps over the streets, cold moonlight fill at night themes
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Atmosphere.Lamp(root, new Vector3(Mathf.Sin(a) * 24f, 8f, Mathf.Cos(a) * 24f), i % 2 == 0 ? new Color(1f, 0.78f, 0.5f) : theme.Accent, 1.3f, 24f);
            }
            Dust(root, quality);

            int parts = stat.GetComponentsInChildren<MeshRenderer>(true).Length;
            try { StaticBatchingUtility.Combine(stat.gameObject); }
            catch (System.Exception ex) { Debug.LogWarning("DERBY: ruins batching skipped. " + ex.Message); }
            return Loc.T("Çöl Harabeleri hazır, ", "Desert Ruins ready, ") + parts + Loc.T(" parça", " parts");
        }
    }
}
