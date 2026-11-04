using System;
using System.Collections;
using System.Collections.Generic;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace DerbyArena.World
{
    /// <summary>Gameplay anchors of a built arena.</summary>
    public sealed class ArenaInfo
    {
        public ArenaKind Kind;
        public Vector3 PlayerSpawn;
        public float PlayerYaw;
        public Vector3[] EnemySpawns;
        public Vector3[] CellPoints;
        public Vector3 Extraction;
        public Vector3[] PickupPoints;
        public Vector3 BossSpawn;
        public Vector3 Center;
        public float Radius;      // colosseum
        public Rect Bounds;       // spillway (x,z)
        public Vector3[] GatePoints = new Vector3[0];
        public Vector3[] AcidPoints = new Vector3[0];
        public float ZoneStart = 27f;
        public float ZoneEnd = 9f;

        public bool Inside(Vector3 p)
        {
            if (Kind == ArenaKind.Colosseum)
            {
                var d = new Vector2(p.x - Center.x, p.z - Center.z);
                return d.magnitude < Radius + 1.5f;
            }
            return Bounds.Contains(new Vector2(p.x, p.z));
        }

        public Vector3 ClosestSafePoint(Vector3 p)
        {
            Vector3 best = PlayerSpawn;
            float bestD = float.MaxValue;
            for (int i = 0; i < PickupPoints.Length; i++)
            {
                float d = (PickupPoints[i] - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = PickupPoints[i]; }
            }
            return best + Vector3.up * 1.2f;
        }
    }

    /// <summary>Builds the arena for a level: collision shell immediately, detailed meshes asynchronously.</summary>
    public sealed class ArenaLoader : MonoBehaviour
    {
        public ArenaInfo Info;
        public bool Ready;
        public string Status = "";

        Transform root;
        Atmosphere.Theme theme;
        int quality;

        public static ArenaLoader Build(ArenaKind kind, int themeIndex, Camera cam, int quality)
        {
            var go = new GameObject("Arena");
            var loader = go.AddComponent<ArenaLoader>();
            loader.root = go.transform;
            loader.quality = quality;
            loader.theme = Atmosphere.Get(themeIndex);
            Atmosphere.Apply(loader.theme, go.transform, cam, quality);
            if (kind == ArenaKind.Colosseum) loader.BuildColosseum();
            else loader.BuildSpillway();
            return loader;
        }

        // ================================================================== SPILLWAY
        static readonly string[] SpillParts =
        {
            "channel_floor", "asphalt", "wall_west", "wall_east", "wall_north", "wall_south",
            "barriers", "bridge", "pipes", "catwalk"
        };

        void BuildSpillway()
        {
            var info = new ArenaInfo();
            info.Kind = ArenaKind.Spillway;
            info.PlayerSpawn = new Vector3(0f, 0.6f, -32f);
            info.PlayerYaw = 0f;
            info.EnemySpawns = new[]
            {
                new Vector3(-24f, 0.6f, 12f), new Vector3(24f, 0.6f, 16f), new Vector3(-22f, 0.6f, 34f),
                new Vector3(22f, 0.6f, 38f), new Vector3(0f, 0.6f, 24f), new Vector3(-28f, 0.6f, -6f), new Vector3(28f, 0.6f, -10f)
            };
            info.CellPoints = new[]
            {
                new Vector3(-22f, 0.9f, 8f), new Vector3(24f, 0.9f, -6f), new Vector3(-4f, 0.9f, 30f),
                new Vector3(26f, 0.9f, 32f), new Vector3(-26f, 0.9f, -24f)
            };
            info.Extraction = new Vector3(0f, 0.05f, 42f);
            info.PickupPoints = new[]
            {
                new Vector3(-14f, 0.6f, -18f), new Vector3(6f, 0.6f, 6f), new Vector3(-12f, 0.6f, 20f),
                new Vector3(14f, 0.6f, 26f), new Vector3(0f, 0.6f, -10f), new Vector3(-30f, 0.6f, 6f), new Vector3(30f, 0.6f, 4f)
            };
            info.BossSpawn = new Vector3(0f, 0.8f, 30f);
            info.Center = new Vector3(0f, 0f, 4f);
            info.Bounds = new Rect(-35.5f, -39.5f, 71f, 87f);
            info.GatePoints = new[]
            {
                new Vector3(0f, 0f, -20f), new Vector3(-20f, 0f, -10f), new Vector3(-24f, 0f, 18f), new Vector3(-10f, 0f, 36f),
                new Vector3(12f, 0f, 38f), new Vector3(22f, 0f, 22f), new Vector3(22f, 0f, -8f), new Vector3(10f, 0f, -26f)
            };
            info.AcidPoints = new[] { new Vector3(-14f, 0f, -28f), new Vector3(16f, 0f, 10f), new Vector3(-6f, 0f, 22f) };
            info.ZoneStart = 30f;
            Info = info;

            var concrete = Mats.Lit(new Color(0.34f, 0.33f, 0.31f), 0.85f, 0f, Color.black, 0f);
            var shell = new GameObject("CollisionShell").transform;
            shell.SetParent(root, false);
            var cover = new Dictionary<string, GameObject>();
            cover["channel_floor"] = MeshFactory.SolidBox("Floor", shell, new Vector3(0f, -0.4f, 4f), new Vector3(80f, 0.8f, 96f), Quaternion.identity, concrete);
            cover["wall_west"] = MeshFactory.SolidBox("WallW", shell, new Vector3(-37f, 3.5f, 4f), new Vector3(1.4f, 7f, 96f), Quaternion.identity, concrete);
            cover["wall_east"] = MeshFactory.SolidBox("WallE", shell, new Vector3(37f, 3.5f, 4f), new Vector3(1.4f, 7f, 96f), Quaternion.identity, concrete);
            cover["wall_south"] = MeshFactory.SolidBox("WallS", shell, new Vector3(0f, 3.5f, -40.8f), new Vector3(80f, 7f, 1.6f), Quaternion.identity, concrete);
            cover["wall_north"] = MeshFactory.SolidBox("WallN", shell, new Vector3(0f, 3.5f, 48.8f), new Vector3(80f, 7f, 1.6f), Quaternion.identity, concrete);

            BuildReactorTower(new Vector3(-27f, 0f, 40f));
            BuildReactorTower(new Vector3(27f, 0f, -30f));
            for (int i = 0; i < 6; i++)
            {
                float z = -32f + i * 15f;
                LightPost(new Vector3(-34.2f, 0f, z), i % 2 == 0 ? theme.Accent : new Color(1f, 0.15f, 0.08f));
                LightPost(new Vector3(34.2f, 0f, z + 7f), i % 2 == 1 ? theme.Accent : new Color(1f, 0.15f, 0.08f));
            }
            Atmosphere.Lamp(root, new Vector3(-20f, 7f, -18f), theme.Accent, 1.6f, 20f);
            Atmosphere.Lamp(root, new Vector3(20f, 7f, 22f), theme.Accent, 1.6f, 20f);
            Atmosphere.Lamp(root, new Vector3(0f, 7f, 40f), new Color(0.2f, 1f, 0.5f), 1.4f, 18f);

            // Drain pipes had no collision (cars ghosted through them). Solid, oriented boxes from the low
            // end to the wall, plus a foot block so the sloped pipe can never act as a launch ramp.
            float[] pipeZ = { -20f, 4f, 28f };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < pipeZ.Length; i++)
                {
                    Vector3 lo = new Vector3(side * 27.25f, 0.31f, pipeZ[i]);
                    Vector3 hi = new Vector3(side * 33.2f, 4.4f, pipeZ[i]);
                    Vector3 d = hi - lo;
                    MeshFactory.ColliderBox("PipeCollider", shell, (lo + hi) * 0.5f, new Vector3(0.95f, 0.95f, d.magnitude + 0.4f), Quaternion.LookRotation(d.normalized, Vector3.up));
                }
            }

            // Neon guide strips along the walls (emissive, no collision, cheap).
            var neonA = Mats.Glow(theme.Accent, 2.2f);
            var neonB = Mats.Glow(new Color(1f, 0.2f, 0.1f), 2f);
            var stripCube = MeshFactory.UnitCube();
            for (int side = -1; side <= 1; side += 2)
            {
                var low = MeshFactory.Make("NeonLow", stripCube, side < 0 ? neonA : neonB, root, new Vector3(side * 36.2f, 0.45f, 4f), new Vector3(0.08f, 0.12f, 94f));
                low.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var high = MeshFactory.Make("NeonHigh", stripCube, side < 0 ? neonB : neonA, root, new Vector3(side * 36.2f, 6.7f, 4f), new Vector3(0.08f, 0.1f, 94f));
                high.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var north = MeshFactory.Make("NeonNorth", stripCube, neonA, root, new Vector3(0f, 0.45f, 47.9f), new Vector3(72f, 0.12f, 0.08f));
            north.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var south = MeshFactory.Make("NeonSouth", stripCube, neonA, root, new Vector3(0f, 0.45f, -39.9f), new Vector3(72f, 0.12f, 0.08f));
            south.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            try { ArenaDressing.Spillway(root); }
            catch (Exception ex) { Debug.LogWarning("DERBY: spillway dressing skipped. " + ex.Message); }
            StartCoroutine(LoadSpillway(cover));
        }

        static string SetOf(string part)
        {
            if (part == "asphalt") return "asphalt";
            if (part == "pipes" || part == "catwalk" || part == "bridge") return "steel";
            return "concrete";
        }

        IEnumerator Fetch(string relative, Action<byte[]> done)
        {
            using (var req = UnityWebRequest.Get(GlbLibrary.UrlOf(relative)))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null) done(req.downloadHandler.data);
                else
                {
                    Debug.LogError("DERBY: arena file missing: " + relative + " | " + req.error);
                    done(null);
                }
            }
        }

        static Texture2D MakeTexture(byte[] bytes, bool linear)
        {
            if (bytes == null) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
            if (!t.LoadImage(bytes, true)) { Destroy(t); return null; }
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            return t;
        }

        IEnumerator LoadSpillway(Dictionary<string, GameObject> cover)
        {
            float t0 = Time.realtimeSinceStartup;
            var mats = new Dictionary<string, Material>();
            string[] sets = { "asphalt", "concrete", "steel" };
            for (int s = 0; s < sets.Length; s++)
            {
                byte[] a = null, n = null;
                yield return Fetch("spillway/" + sets[s] + "_albedo.png", b => a = b);
                yield return Fetch("spillway/" + sets[s] + "_normal.png", b => n = b);
                var albedo = MakeTexture(a, false);
                var normal = MakeTexture(n, true);
                string set = sets[s];
                Color tint = set == "steel" ? new Color(0.9f, 0.92f, 0.96f) : new Color(0.95f, 0.93f, 0.9f);
                float smooth = set == "steel" ? 0.55f : (set == "asphalt" ? 0.42f : 0.2f);
                mats[set] = albedo != null
                    ? Mats.TexturedNormal(albedo, normal, tint, smooth, set == "steel" ? 0.35f : 0f)
                    : Mats.Lit(new Color(0.35f, 0.35f, 0.36f), 0.7f, 0.1f, Color.black, 0f);
                yield return null;
            }

            int loaded = 0;
            bool bridgeOk = false, barriersOk = false;
            for (int i = 0; i < SpillParts.Length; i++)
            {
                string part = SpillParts[i];
                byte[] data = null;
                yield return Fetch("spillway/" + part + ".obj", b => data = b);
                if (data == null) continue;
                yield return null;
                Mesh mesh = null;
                try { mesh = ObjMesh.Parse(System.Text.Encoding.UTF8.GetString(data), part); }
                catch (Exception ex) { Debug.LogError("DERBY: OBJ parse failed: " + part + " | " + ex.Message); }
                if (mesh == null || mesh.vertexCount == 0) continue;

                var go = new GameObject(part);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                Material m;
                if (mats.TryGetValue(SetOf(part), out m) && m != null) mr.sharedMaterial = m;
                bool caster = part == "pipes" || part == "bridge" || part == "barriers" || part == "catwalk";
                mr.shadowCastingMode = caster ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
                go.isStatic = true;
                if (part == "bridge" || part == "barriers")
                {
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                    if (part == "bridge") bridgeOk = true; else barriersOk = true;
                }
                GameObject shellPart;
                if (cover.TryGetValue(part, out shellPart) && shellPart != null)
                {
                    var r = shellPart.GetComponent<MeshRenderer>();
                    if (r != null) r.enabled = false;
                }
                loaded++;
                yield return null;
            }

            if (!barriersOk)
            {
                var hazard = Mats.Lit(new Color(0.85f, 0.62f, 0.08f), 0.55f, 0.1f, Color.black, 0f);
                MeshFactory.SolidBox("BarrierA", root, new Vector3(10f, 0.6f, -8f), new Vector3(1.1f, 1.2f, 6f), Quaternion.identity, hazard);
                MeshFactory.SolidBox("BarrierB", root, new Vector3(-12f, 0.6f, 14f), new Vector3(7f, 1.2f, 1.1f), Quaternion.identity, hazard);
                MeshFactory.SolidBox("BarrierC", root, new Vector3(6f, 0.6f, 22f), new Vector3(1.1f, 1.2f, 8f), Quaternion.identity, hazard);
            }
            if (!bridgeOk)
            {
                var steel = Mats.Lit(new Color(0.42f, 0.44f, 0.47f), 0.45f, 0.4f, Color.black, 0f);
                MeshFactory.SolidBox("BridgeDeck", root, new Vector3(0f, 4.6f, 0f), new Vector3(28f, 0.4f, 3.2f), Quaternion.identity, steel);
            }

            Status = loaded + "/" + SpillParts.Length + Loc.T(" parça, ", " parts, ") + (Time.realtimeSinceStartup - t0).ToString("0.0") + Loc.T(" sn", " s");
            DerbyStartupDiagnostics.Report("Arena", Status);
            Ready = true;
        }

        void BuildReactorTower(Vector3 at)
        {
            var body = Mats.Lit(new Color(0.12f, 0.13f, 0.14f), 0.45f, 0.4f, Color.black, 0f);
            var glow = Mats.Glow(new Color(0.15f, 0.85f, 1f), 2.6f);
            var t = new GameObject("ReactorTower").transform;
            t.SetParent(root, false);
            t.position = at;
            var cyl = MeshFactory.UnitCylinder();
            MeshFactory.Make("Base", cyl, body, t, new Vector3(0f, 0.6f, 0f), new Vector3(7f, 1.2f, 7f));
            MeshFactory.Make("Tower", cyl, body, t, new Vector3(0f, 5f, 0f), new Vector3(3.4f, 8f, 3.4f));
            for (int i = 0; i < 4; i++)
                MeshFactory.Make("Ring", cyl, glow, t, new Vector3(0f, 2.2f + i * 2.1f, 0f), new Vector3(3.8f, 0.18f, 3.8f));
            MeshFactory.Make("Core", cyl, glow, t, new Vector3(0f, 9.4f, 0f), new Vector3(1.4f, 0.8f, 1.4f));
            var col = new GameObject("TowerCollider");
            col.transform.SetParent(t, false);
            col.transform.localPosition = new Vector3(0f, 4f, 0f);
            var cap = col.AddComponent<BoxCollider>();
            cap.size = new Vector3(6.4f, 8f, 6.4f);
            Atmosphere.Lamp(t, at + new Vector3(0f, 6f, 0f), new Color(0.2f, 0.8f, 1f), 2f, 22f);
        }

        void LightPost(Vector3 at, Color color)
        {
            var steel = Mats.Lit(new Color(0.16f, 0.17f, 0.18f), 0.5f, 0.4f, Color.black, 0f);
            var lamp = Mats.Glow(color, 3f);
            var t = new GameObject("LightPost").transform;
            t.SetParent(root, false);
            t.position = at;
            var cube = MeshFactory.UnitCube();
            MeshFactory.Make("Pole", cube, steel, t, new Vector3(0f, 3.5f, 0f), new Vector3(0.25f, 7f, 0.25f));
            MeshFactory.Make("Head", cube, lamp, t, new Vector3(0f, 7.05f, 0f), new Vector3(0.9f, 0.18f, 0.5f));
        }

        // ================================================================== COLOSSEUM
        void BuildColosseum()
        {
            var info = new ArenaInfo();
            info.Kind = ArenaKind.Colosseum;
            info.Center = Vector3.zero;
            info.Radius = 28.5f;
            info.PlayerSpawn = new Vector3(0f, 0.6f, -21f);
            info.PlayerYaw = 0f;
            var spawns = new List<Vector3>();
            for (int i = 0; i < 7; i++)
            {
                float a = (35f + i * 48f) * Mathf.Deg2Rad;
                spawns.Add(new Vector3(Mathf.Sin(a) * 20f, 0.6f, Mathf.Cos(a) * 20f));
            }
            info.EnemySpawns = spawns.ToArray();
            info.CellPoints = new Vector3[0];
            info.Extraction = Vector3.zero;
            var pick = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                pick.Add(new Vector3(Mathf.Sin(a) * 13f, 0.6f, Mathf.Cos(a) * 13f));
            }
            pick.Add(new Vector3(0f, 0.6f, 0f));
            info.PickupPoints = pick.ToArray();
            info.BossSpawn = new Vector3(0f, 0.8f, 19f);
            var gates = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float a = (180f + i * 45f) * Mathf.Deg2Rad;
                gates.Add(new Vector3(Mathf.Sin(a) * 18f, 0f, Mathf.Cos(a) * 18f));
            }
            info.GatePoints = gates.ToArray();
            info.AcidPoints = new[] { new Vector3(0f, 0f, 12f), new Vector3(10.4f, 0f, -6f), new Vector3(-10.4f, 0f, -6f) };
            info.ZoneStart = 27f;
            info.ZoneEnd = 9f;
            Info = info;

            var sand = Mats.Lit(new Color(0.55f, 0.46f, 0.34f), 0.9f, 0f, Color.black, 0f);
            var shell = new GameObject("CollisionShell").transform;
            shell.SetParent(root, false);
            var floor = MeshFactory.SolidBox("Floor", shell, new Vector3(0f, -0.5f, 0f), new Vector3(70f, 1f, 70f), Quaternion.identity, sand);
            const int segs = 40;
            float r = 30.1f;
            float w = 2f * Mathf.PI * r / segs + 0.4f;
            for (int i = 0; i < segs; i++)
            {
                float a = i * 360f / segs;
                var pos = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 1.5f, r);
                MeshFactory.SolidBox("Ring" + i, shell, pos, new Vector3(w, 3f, 1.2f), Quaternion.Euler(0f, a, 0f), null);
            }

            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Atmosphere.Lamp(root, new Vector3(Mathf.Sin(a) * 24f, 9f, Mathf.Cos(a) * 24f), i % 2 == 0 ? theme.Accent : new Color(1f, 0.75f, 0.45f), 1.5f, 26f);
            }

            // Braziers on top of the barrier ring: animated fire, 4 of them carry a flickering light.
            var iron = Mats.Lit(new Color(0.09f, 0.08f, 0.08f), 0.5f, 0.6f, Color.black, 0f);
            var coal = Mats.Glow(new Color(1f, 0.45f, 0.12f), 3f);
            for (int i = 0; i < 8; i++)
            {
                float a = (22.5f + i * 45f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(a) * 30.1f, 3.05f, Mathf.Cos(a) * 30.1f);
                var b = new GameObject("Brazier").transform;
                b.SetParent(root, false);
                b.position = at;
                MeshFactory.Make("Bowl", MeshFactory.UnitCylinder(), iron, b, new Vector3(0f, 0.25f, 0f), new Vector3(1.3f, 0.5f, 1.3f));
                MeshFactory.Make("Coals", MeshFactory.UnitCylinder(), coal, b, new Vector3(0f, 0.52f, 0f), new Vector3(1.05f, 0.06f, 1.05f));
                var fl = b.gameObject.AddComponent<Brazier>();
                if (i % 4 == 0 && quality > 0) fl.Light = Atmosphere.Lamp(b, at + Vector3.up * 1.4f, new Color(1f, 0.55f, 0.2f), 1.3f, 12f);
            }

            StartCoroutine(LoadColosseum(floor));
        }

        static Material FindMaterial(GameObject model, string key)
        {
            if (model == null) return null;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) return m;
            return null;
        }

        /// <summary>
        /// Sparse, clearly visible obstacles (1.4.0): three low sandstone block walls as in the reference video and
        /// two small striped jump ramps. Box colliders only; positions keep clear of spawns, pickups and acid pools.
        /// </summary>
        void BuildColosseumObstacles(GameObject arena)
        {
            var stone = FindMaterial(arena, "RomanLimestone");
            if (stone == null) stone = Mats.Lit(new Color(0.8f, 0.68f, 0.5f), 0.85f, 0f, Color.black, 0f);
            var hazard = FindMaterial(arena, "HazardBarrier");
            if (hazard == null) hazard = Mats.Lit(new Color(0.95f, 0.72f, 0.12f), 0.6f, 0.1f, Color.black, 0f);
            var obstacles = new GameObject("Obstacles").transform;
            obstacles.SetParent(root, false);

            float[] wallAngles = { 60f, 180f, 300f };
            for (int w = 0; w < wallAngles.Length; w++)
            {
                float a = wallAngles[w] * Mathf.Deg2Rad;
                Vector3 c = new Vector3(Mathf.Sin(a) * 8f, 0f, Mathf.Cos(a) * 8f);
                Vector3 tangent = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
                Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);
                for (int bIndex = -1; bIndex <= 1; bIndex++)
                {
                    float h = bIndex == 0 ? 1.1f : 0.85f;
                    Vector3 p = c + tangent * (bIndex * 1.75f) + Vector3.up * (h * 0.5f);
                    var blk = MeshFactory.SolidBox("StoneBlock", obstacles, p, new Vector3(1.5f, h, 1.5f), rot * Quaternion.Euler(0f, bIndex * 7f, 0f), stone);
                    var mr = blk.GetComponent<MeshRenderer>();
                    if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }

            float[] rampAngles = { 107f, 251f };
            for (int k = 0; k < rampAngles.Length; k++)
            {
                float a = rampAngles[k] * Mathf.Deg2Rad;
                Vector3 c = new Vector3(Mathf.Sin(a) * 17f, -0.1f, Mathf.Cos(a) * 17f);
                Quaternion rot = Quaternion.Euler(0f, rampAngles[k] + 90f, 0f) * Quaternion.Euler(-9f, 0f, 0f);
                MeshFactory.SolidBox("JumpRamp", obstacles, c, new Vector3(4f, 1f, 5.2f), rot, hazard);
            }
        }

        IEnumerator LoadColosseum(GameObject floor)
        {
            float t0 = Time.realtimeSinceStartup;
            GameObject arena = null;
            string error = null;
            bool done = false;
            GlbLibrary.Spawn("arena/colosseum.glb", root, (go, err) => { arena = go; error = err; done = true; });
            while (!done) yield return null;

            if (arena != null)
            {
                foreach (var mr in arena.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string n = mr.gameObject.name;
                    bool big = n == "Ground" || n == "Tribunes" || n == "OuterWall" || n == "DistantWorld" || n == "Colonnade";
                    mr.shadowCastingMode = n == "DistantWorld" || n == "Ground"
                        ? UnityEngine.Rendering.ShadowCastingMode.Off
                        : UnityEngine.Rendering.ShadowCastingMode.On;
                    mr.receiveShadows = !big || n == "Ground" || n == "Tribunes";
                    // 1.3.0: the interior rubble / barrier / pole clutter ("Props") made the driving
                    // surface bumpy and snagged cars. The arena floor is now clean and flat.
                    if (n == "Props") mr.gameObject.SetActive(false);
                }
                int wreckIndex = 0;
                var wrecks = new List<Transform>();
                foreach (var t in arena.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("wreck", StringComparison.Ordinal) && t.name.EndsWith("_Root", StringComparison.Ordinal)) wrecks.Add(t);
                foreach (var t in wrecks)
                {
                    // keep four wrecks as cover on the diagonals near the wall, hide the rest
                    if (wreckIndex >= 4) { t.gameObject.SetActive(false); wreckIndex++; continue; }
                    float ang = 45f + wreckIndex * 90f;
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 p = t.position;
                    t.position = new Vector3(Mathf.Sin(rad) * 25.5f, p.y, Mathf.Cos(rad) * 25.5f);
                    t.RotateAround(t.position, Vector3.up, ang * 1.7f);
                    wreckIndex++;
                    var rs = t.GetComponentsInChildren<Renderer>(true);
                    if (rs.Length == 0) continue;
                    Bounds b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    var col = new GameObject(t.name + "_Collider");
                    col.transform.SetParent(root, false);
                    col.transform.position = b.center;
                    col.AddComponent<BoxCollider>().size = b.size * 0.9f;
                }
                var fr = floor.GetComponent<MeshRenderer>();
                if (fr != null) fr.enabled = false;
                Crowd.Spawn(root, quality);
                BuildColosseumObstacles(arena);
                try { ArenaDressing.Colosseum(root, theme.Accent, quality); }
                catch (Exception ex) { Debug.LogWarning("DERBY: arena dressing skipped. " + ex.Message); }
                Status = Loc.T("Kolezyum yüklendi, ", "Colosseum loaded, ") + (Time.realtimeSinceStartup - t0).ToString("0.0") + Loc.T(" sn", " s");
            }
            else
            {
                Status = Loc.T("Kolezyum modeli yüklenemedi (", "Colosseum model failed to load (") + error + Loc.T("), basit arena", "), simple arena");
                var stone = Mats.Lit(new Color(0.45f, 0.42f, 0.38f), 0.85f, 0f, Color.black, 0f);
                var cyl = MeshFactory.UnitCylinder();
                for (int i = 0; i < 24; i++)
                {
                    float a = i * 15f * Mathf.Deg2Rad;
                    MeshFactory.Make("Pillar", cyl, stone, root, new Vector3(Mathf.Sin(a) * 31f, 3f, Mathf.Cos(a) * 31f), new Vector3(1.6f, 6f, 1.6f));
                }
            }
            DerbyStartupDiagnostics.Report("Arena", Status);
            Ready = true;
        }
    }

    /// <summary>Brazier fire on the colosseum barrier: emits flames and flickers its light.</summary>
    public sealed class Brazier : MonoBehaviour
    {
        public Light Light;
        float timer;
        float baseIntensity = -1f;
        float seed;

        void Start() { seed = UnityEngine.Random.value * 10f; }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0.11f;
                DerbyArena.Combat.Fx.Fire(transform.position + Vector3.up * 0.75f, 0.7f);
            }
            if (Light != null)
            {
                if (baseIntensity < 0f) baseIntensity = Light.intensity;
                float f = Mathf.PerlinNoise(seed, Time.time * 6f);
                Light.intensity = baseIntensity * (0.75f + 0.5f * f);
            }
        }
    }
}
