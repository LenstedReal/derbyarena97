using System.Collections.Generic;
using DerbyArena.Core;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Derby "battle kit" bolted onto every car in code: roof light bar, bull bar, racing stripes and, per role or
    /// garage upgrade, armour plates, ram spikes, spoiler, side exhausts and a supercharger.
    ///
    /// 2.1: the parts follow the real shape of the model. The body mesh is measured once per spawn (height of the
    /// deck along the length, width of the sides, roof span, nose and tail), so the spoiler sits on the trunk, the
    /// blower on the hood, the stripes follow hood-roof-trunk and skip the glass, and the plates hug the doors.
    /// Materials are shared (one per colour), parts cast no shadows, everything merges into one mesh per material.
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
            if (m != null) m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        static Material Glow(Color c, float energy)
        {
            int key = 0x7000000 ^ (Mathf.RoundToInt(c.r * 255) << 16) ^ (Mathf.RoundToInt(c.g * 255) << 8) ^ Mathf.RoundToInt(c.b * 255) ^ (Mathf.RoundToInt(energy * 10) << 20);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Glow(c, energy);
            if (m != null) m.enableInstancing = true;
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

        /// <summary>Measured silhouette of the body model in car space (x right, y up, z forward).</summary>
        sealed class Shape
        {
            public const int Buckets = 28;
            public float ZMin, ZMax, YMin, YMax, XMax;
            public readonly float[] Top = new float[Buckets];     // deck height along the centre strip
            public readonly float[] Side = new float[Buckets];    // half width in the belt band
            public float RoofY, RoofZ0, RoofZ1;                   // roof height and its z span
            public float WheelZFront, WheelZRear;

            public float Dz { get { return (ZMax - ZMin) / Buckets; } }
            public float ZAt(int b) { return ZMin + (b + 0.5f) * Dz; }
            public int BucketOf(float z) { return Mathf.Clamp(Mathf.FloorToInt((z - ZMin) / Dz), 0, Buckets - 1); }
            public float TopAt(float z) { return Top[BucketOf(z)]; }
            public float SideAt(float z) { return Side[BucketOf(z)]; }
        }

        static bool UnderNamed(Transform t, Transform stop)
        {
            while (t != null && t != stop)
            {
                string n = t.name;
                if (n == "Wheel_FL" || n == "Wheel_FR" || n == "Wheel_RL" || n == "Wheel_RR" || n == "Wheel" || n == "Turret" || n == "Barrel" || n == "BattleKit" || n == "HeadlightBeam") return true;
                t = t.parent;
            }
            return false;
        }

        /// <summary>
        /// Cuts every body triangle with the centre plane (x = 0.02) for the deck height and with the belt plane
        /// for the side width. Cutting triangles instead of looking at vertices matters: a rounded roof panel has no
        /// vertex on the centre line at all, so a vertex scan would miss the roof and pick the hood scoop instead.
        /// </summary>
        static readonly Dictionary<string, Shape> shapeCache = new Dictionary<string, Shape>();

        static Shape Measure(Transform car, VehicleSpec spec)
        {
            var filters = car.GetComponentsInChildren<MeshFilter>(true);
            // every clone of a model template measures the same, so the result is cached per file, scale and
            // vertex count (the count tells a loaded model from the fallback box); saves ~0.6 MB of garbage per spawn
            int vertTotal = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var mf = filters[i];
                if (mf.sharedMesh == null || UnderNamed(mf.transform, car)) continue;
                vertTotal += mf.sharedMesh.vertexCount;
            }
            string key = (spec.GlbFile ?? "") + "|" + spec.Scale.ToString("F3") + "|" + vertTotal;
            Shape cached;
            if (shapeCache.TryGetValue(key, out cached)) return cached;
            Shape result = MeasureNow(car, spec, filters);
            if (result != null) shapeCache[key] = result;
            return result;
        }

        static Shape MeasureNow(Transform car, VehicleSpec spec, MeshFilter[] filters)
        {
            var verts = new List<Vector3>(16384);
            var tris = new List<int>(65536);
            Matrix4x4 toCar = car.worldToLocalMatrix;
            for (int i = 0; i < filters.Length; i++)
            {
                var mf = filters[i];
                if (mf.sharedMesh == null || !mf.sharedMesh.isReadable || UnderNamed(mf.transform, car)) continue;
                if (mf.GetComponent<TrailRenderer>() != null || mf.GetComponent<ParticleSystem>() != null) continue;
                Matrix4x4 m = toCar * mf.transform.localToWorldMatrix;
                var v = mf.sharedMesh.vertices;
                var t = mf.sharedMesh.triangles;
                int baseIndex = verts.Count;
                for (int k = 0; k < v.Length; k++) verts.Add(m.MultiplyPoint3x4(v[k]));
                for (int k = 0; k < t.Length; k++) tris.Add(t[k] + baseIndex);
            }
            if (verts.Count < 24 || tris.Count < 36) return null;
            var s = new Shape();
            s.ZMin = s.YMin = float.MaxValue; s.ZMax = s.YMax = s.XMax = float.MinValue;
            for (int i = 0; i < verts.Count; i++)
            {
                var p = verts[i];
                if (p.z < s.ZMin) s.ZMin = p.z;
                if (p.z > s.ZMax) s.ZMax = p.z;
                if (p.y < s.YMin) s.YMin = p.y;
                if (p.y > s.YMax) s.YMax = p.y;
                if (Mathf.Abs(p.x) > s.XMax) s.XMax = Mathf.Abs(p.x);
            }
            if (s.ZMax - s.ZMin < 1f || s.YMax - s.YMin < 0.3f) return null;
            for (int b = 0; b < Shape.Buckets; b++) { s.Top[b] = s.YMin; s.Side[b] = 0f; }
            float dz = s.Dz;
            var seg = new Vector3[2];
            System.Action<Vector3> top = delegate (Vector3 p) { int bk = s.BucketOf(p.z); if (p.y > s.Top[bk]) s.Top[bk] = p.y; };
            System.Action<Vector3> side = delegate (Vector3 p) { int bk = s.BucketOf(p.z); float ax = Mathf.Abs(p.x); if (ax > s.Side[bk]) s.Side[bk] = ax; };
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b2 = verts[tris[i + 1]], c = verts[tris[i + 2]];
                if (Cut(a, b2, c, 0, 0.02f, seg)) Sample(seg, dz, top);
            }
            // belt plane at 45% of the deck height (median of the centre strip): YMax would include an antenna,
            // exhaust stacks or a rocket pod and push the belt above the doors (2.1.1)
            var deck = new List<float>(Shape.Buckets);
            for (int b = 0; b < Shape.Buckets; b++) if (s.Top[b] > s.YMin + 0.05f) deck.Add(s.Top[b]);
            deck.Sort();
            float deckY = deck.Count > 0 ? deck[deck.Count / 2] : s.YMax;
            float beltY = s.YMin + (deckY - s.YMin) * 0.45f;
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b2 = verts[tris[i + 1]], c = verts[tris[i + 2]];
                if (Cut(a, b2, c, 1, beltY, seg)) Sample(seg, dz, side);
            }
            // fill empty side buckets from neighbours (bumpers leave gaps at the ends)
            for (int b = 1; b < Shape.Buckets; b++) if (s.Side[b] <= 0f) s.Side[b] = s.Side[b - 1];
            for (int b = Shape.Buckets - 2; b >= 0; b--) if (s.Side[b] <= 0f) s.Side[b] = s.Side[b + 1];
            // roof: the highest run of at least three buckets (a thin rack bar or antenna does not count)
            var order = new List<int>(Shape.Buckets);
            for (int b = 0; b < Shape.Buckets; b++) order.Add(b);
            order.Sort(delegate (int x, int y) { return s.Top[y].CompareTo(s.Top[x]); });
            int rb0 = order[0], rb1 = order[0];
            float roofY = s.Top[order[0]];
            for (int k = 0; k < order.Count; k++)
            {
                int seed = order[k];
                float y = s.Top[seed];
                int b0 = seed, b1 = seed;
                while (b0 > 0 && s.Top[b0 - 1] > y - 0.08f) b0--;
                while (b1 < Shape.Buckets - 1 && s.Top[b1 + 1] > y - 0.08f) b1++;
                if (b1 - b0 + 1 >= 3) { roofY = y; rb0 = b0; rb1 = b1; break; }
            }
            s.RoofY = roofY;
            s.RoofZ0 = s.ZMin + rb0 * dz;
            s.RoofZ1 = s.ZMin + (rb1 + 1) * dz;
            s.WheelZFront = spec.WheelFront.z * spec.Scale;
            s.WheelZRear = spec.WheelRear.z * spec.Scale;
            foreach (Transform t in car)
            {
                if (t.name == "Wheel_FL") s.WheelZFront = t.localPosition.z;
                else if (t.name == "Wheel_RL") s.WheelZRear = t.localPosition.z;
            }
            return s;
        }

        /// <summary>Intersection segment of a triangle with the plane axis = value (false when it does not cross).</summary>
        static bool Cut(Vector3 a, Vector3 b, Vector3 c, int axis, float value, Vector3[] seg)
        {
            float da = a[axis] - value, db = b[axis] - value, dc = c[axis] - value;
            if ((da > 0f && db > 0f && dc > 0f) || (da < 0f && db < 0f && dc < 0f)) return false;
            int n = 0;
            if (Edge(a, b, da, db, seg, ref n) && n == 2) return true;
            if (Edge(b, c, db, dc, seg, ref n) && n == 2) return true;
            if (Edge(c, a, dc, da, seg, ref n) && n == 2) return true;
            return n == 2;
        }

        static bool Edge(Vector3 p, Vector3 q, float dp, float dq, Vector3[] seg, ref int n)
        {
            if (n >= 2) return true;
            if (dp == 0f) { seg[n++] = p; return n == 2; }
            if (dq == 0f) return false;                       // q itself is added when the next edge starts there
            if ((dp < 0f) == (dq < 0f)) return false;
            float t = dp / (dp - dq);
            seg[n++] = p + (q - p) * t;
            return n == 2;
        }

        static void Sample(Vector3[] seg, float dz, System.Action<Vector3> visit)
        {
            float len = Mathf.Abs(seg[1].z - seg[0].z);
            int n = Mathf.Clamp(Mathf.FloorToInt(len / (dz * 0.5f)) + 2, 2, 64);
            for (int k = 0; k < n; k++) visit(seg[0] + (seg[1] - seg[0]) * (k / (float)(n - 1)));
        }

        public static void Build(Transform car, VehicleSpec spec, BoxCollider hull)
        {
            if (car == null || spec == null) return;
            int quality = Save.Data.Quality;
            var root = new GameObject("BattleKit").transform;
            root.SetParent(car, false);

            Vector3 c = hull != null ? hull.center : new Vector3(0f, spec.BodyCenterY * spec.Scale, 0f);
            Vector3 s = hull != null ? hull.size : spec.BodySize * spec.Scale;
            Shape shape = null;
            try { shape = Measure(car, spec); }
            catch (System.Exception ex) { Debug.LogWarning("DERBY: kit measure failed: " + ex.Message); }

            float halfW = s.x * 0.5f;
            float top = c.y + s.y * 0.5f;
            float front = c.z + s.z * 0.5f;
            float back = c.z - s.z * 0.5f;
            float low = c.y - s.y * 0.5f;
            float roofY = top, roofZ0 = c.z - 0.5f, roofZ1 = c.z + 0.5f;
            float trunkZ = back + 0.4f, trunkY = top, hoodZ = c.z + s.z * 0.26f, hoodY = top;
            float doorZ0 = c.z - s.z * 0.17f, doorZ1 = c.z + s.z * 0.25f, doorX = halfW;
            if (shape != null)
            {
                halfW = Mathf.Max(0.5f, shape.XMax);
                top = shape.RoofY; low = shape.YMin; front = shape.ZMax; back = shape.ZMin;   // RoofY: antenna/stacks/pod excluded
                roofY = shape.RoofY; roofZ0 = shape.RoofZ0; roofZ1 = shape.RoofZ1;
                trunkZ = Mathf.Max(back + 0.45f, shape.WheelZRear - 0.3f);
                trunkY = shape.TopAt(trunkZ);
                hoodZ = Mathf.Lerp(roofZ1, front, 0.42f);
                hoodY = shape.TopAt(hoodZ);
                float wr = spec.WheelRadius * spec.Scale;
                doorZ0 = shape.WheelZRear + wr + 0.3f;
                doorZ1 = shape.WheelZFront - wr - 0.3f;
                if (doorZ1 - doorZ0 < 0.6f) { doorZ0 = c.z - 0.5f; doorZ1 = c.z + 0.5f; }
                doorX = shape.SideAt((doorZ0 + doorZ1) * 0.5f);
            }
            var cube = MeshFactory.UnitCube();
            var cyl = MeshFactory.UnitCylinder();

            bool player = spec.Role == DriverRole.Player;
            bool boss = spec.Role == DriverRole.Boss;
            Color steel = new Color(0.16f, 0.16f, 0.17f);
            Color rusty = new Color(0.32f, 0.2f, 0.12f);
            var steelMat = Lit(steel, 0.45f, 0.45f);
            var rustMat = Lit(rusty, 0.8f, 0.2f);
            Color stripe = spec.Elite ? new Color(0.75f, 0.45f, 1f) : (player ? new Color(0.95f, 0.93f, 0.88f) : Color.Lerp(spec.Accent, Color.black, 0.45f));

            // roof light bar at the front edge of the roof: frame + 4 lamps; the player's lamps are brighter
            float barY = roofY + 0.1f;
            float barZ = roofZ1 - 0.08f;
            float barW = halfW * 1.24f;
            Part("RoofBar", cube, steelMat, root, new Vector3(0f, barY, barZ), new Vector3(barW, 0.07f, 0.12f), Quaternion.identity);
            Part("RoofLegL", cube, steelMat, root, new Vector3(-barW * 0.45f, barY - 0.06f, barZ), new Vector3(0.06f, 0.12f, 0.06f), Quaternion.identity);
            Part("RoofLegR", cube, steelMat, root, new Vector3(barW * 0.45f, barY - 0.06f, barZ), new Vector3(0.06f, 0.12f, 0.06f), Quaternion.identity);
            var lampMat = Glow(new Color(1f, 0.93f, 0.75f), player ? 3.2f : 2.2f);
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Lerp(-barW * 0.4f, barW * 0.4f, i / 3f);
                Part("RoofLamp", cyl, lampMat, root, new Vector3(x, barY + 0.08f, barZ + 0.04f), new Vector3(0.16f, 0.06f, 0.16f), Quaternion.Euler(90f, 0f, 0f));
            }

            // racing stripes following hood, roof and trunk (steep buckets = glass are skipped)
            var stripeMat = Lit(stripe, 0.4f, 0.1f);
            if (shape != null)
            {
                // flat runs of the deck (hood, roof, trunk) in chunks of up to four buckets: each chunk is one slab
                // laid along the chord of its run, so the stripe is continuous and hugs the curve within ~1 cm
                float dz = shape.Dz;
                int runStart = -1;
                for (int b = 1; b <= Shape.Buckets - 1; b++)
                {
                    bool flat = false;
                    if (b < Shape.Buckets - 1)
                    {
                        float z = shape.ZAt(b);
                        float rise = shape.Top[b + 1] - shape.Top[b - 1];
                        flat = z >= back + 0.35f && z <= front - 0.45f && Mathf.Abs(rise) / (2f * dz) <= 0.55f;
                    }
                    if (flat && runStart < 0) runStart = b;
                    if ((!flat || b == Shape.Buckets - 1) && runStart >= 0)
                    {
                        int runEnd = flat ? b : b - 1;
                        for (int c0 = runStart; c0 <= runEnd; c0 += 4)
                        {
                            int c1 = Mathf.Min(c0 + 3, runEnd);
                            float z0 = shape.ZMin + c0 * dz, z1 = shape.ZMin + (c1 + 1) * dz;
                            float y0 = shape.Top[c0], y1 = shape.Top[c1];
                            float yMid = 0f;
                            for (int k = c0; k <= c1; k++) yMid += shape.Top[k];
                            yMid = yMid / (c1 - c0 + 1) + 0.012f;
                            Quaternion tilt = Quaternion.Euler(-Mathf.Atan2(y1 - y0, Mathf.Max(dz, (c1 - c0) * dz)) * Mathf.Rad2Deg, 0f, 0f);
                            for (int side = -1; side <= 1; side += 2)
                                Part("Stripe", cube, stripeMat, root, new Vector3(side * halfW * 0.09f, yMid, (z0 + z1) * 0.5f), new Vector3(halfW * 0.08f, 0.02f, z1 - z0 + 0.01f), tilt);
                        }
                        runStart = -1;
                    }
                }
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                    Part("Stripe", cube, stripeMat, root, new Vector3(side * halfW * 0.09f, top + 0.012f, c.z), new Vector3(halfW * 0.08f, 0.02f, s.z * 0.92f), Quaternion.identity);
            }

            // bull bar: two rails and uprights in front of the nose
            float barFrontZ = front + 0.1f;
            float railY = low + 0.3f;
            float railW = halfW * 1.7f;
            Part("BullRailLow", cyl, steelMat, root, new Vector3(0f, railY, barFrontZ), new Vector3(0.11f, railW, 0.11f), Quaternion.Euler(0f, 0f, 90f));
            Part("BullRailHigh", cyl, steelMat, root, new Vector3(0f, railY + 0.32f, barFrontZ - 0.04f), new Vector3(0.09f, railW * 0.72f, 0.09f), Quaternion.Euler(0f, 0f, 90f));
            for (int i = -1; i <= 1; i += 2)
                Part("BullPost", cyl, steelMat, root, new Vector3(i * railW * 0.28f, railY + 0.16f, barFrontZ - 0.02f), new Vector3(0.09f, 0.36f, 0.09f), Quaternion.identity);

            int armor = player ? Upgrades.Level(Upgrades.Armor) : (spec.Role == DriverRole.Rammer || boss ? 3 : (spec.Elite ? 2 : 0));
            int engine = player ? Upgrades.Level(Upgrades.Engine) : (boss ? 4 : 0);
            int nitro = player ? Upgrades.Level(Upgrades.Nitro) : 0;

            // ram spikes (rammers, boss, player with armour 4+)
            if (spec.Role == DriverRole.Rammer || boss || armor >= 4)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = Mathf.Lerp(-railW * 0.42f, railW * 0.42f, i / 4f);
                    Part("RamSpike", cyl, rustMat, root, new Vector3(x, railY + 0.02f, barFrontZ + 0.3f), new Vector3(0.07f, 0.55f, 0.07f), Quaternion.Euler(90f, 0f, 0f));
                }
            }

            // armour plates on the doors (2+) and roll bars over the roof (3+)
            if (armor >= 2)
            {
                float plateH = Mathf.Max(0.3f, (top - low) * 0.26f);
                float plateY = low + (top - low) * 0.36f;
                float plateL = doorZ1 - doorZ0;
                for (int side = -1; side <= 1; side += 2)
                {
                    Part("DoorPlate", cube, rustMat, root, new Vector3(side * (doorX + 0.03f), plateY, (doorZ0 + doorZ1) * 0.5f), new Vector3(0.05f, plateH, plateL), Quaternion.identity);
                    for (int b = -1; b <= 1; b += 2)
                        Part("Bolt", cube, steelMat, root, new Vector3(side * (doorX + 0.06f), plateY + plateH * 0.3f, (doorZ0 + doorZ1) * 0.5f + b * plateL * 0.35f), new Vector3(0.04f, 0.06f, 0.06f), Quaternion.identity);
                }
            }
            if (armor >= 3 && quality > 0)
            {
                float rw = Mathf.Min(barW, halfW * 1.4f);
                for (int i = -1; i <= 1; i++)
                    Part("RollBar", cyl, steelMat, root, new Vector3(0f, roofY + 0.05f, Mathf.Lerp(roofZ0 + 0.2f, roofZ1 - 0.35f, (i + 1) / 2f)), new Vector3(0.05f, rw, 0.05f), Quaternion.Euler(0f, 0f, 90f));
            }

            // spoiler on the trunk (player, drifters, elites)
            if (player || spec.Role == DriverRole.Flanker || spec.Elite)
            {
                float wingY = trunkY + (player ? 0.34f : 0.26f);
                float wingW = Mathf.Min(2f * halfW * 0.86f, 2.4f);
                Part("SpoilerWing", cube, Lit(Color.Lerp(spec.Paint, Color.black, 0.55f), 0.5f, 0.2f), root, new Vector3(0f, wingY, trunkZ), new Vector3(wingW, 0.05f, 0.42f), Quaternion.Euler(-6f, 0f, 0f));
                for (int side = -1; side <= 1; side += 2)
                    Part("SpoilerPost", cube, steelMat, root, new Vector3(side * wingW * 0.35f, (wingY + trunkY) * 0.5f, trunkZ + 0.02f), new Vector3(0.05f, wingY - trunkY, 0.12f), Quaternion.identity);
            }

            // supercharger through the hood (engine 3+, boss)
            if (engine >= 3)
            {
                Part("Blower", cube, steelMat, root, new Vector3(0f, hoodY + 0.12f, hoodZ), new Vector3(0.42f, 0.24f, 0.55f), Quaternion.identity);
                Part("Scoop", cube, Lit(new Color(0.75f, 0.72f, 0.68f), 0.3f, 0.6f), root, new Vector3(0f, hoodY + 0.3f, hoodZ + 0.05f), new Vector3(0.46f, 0.12f, 0.32f), Quaternion.identity);
                Part("ScoopMouth", cube, Lit(new Color(0.02f, 0.02f, 0.02f), 0.9f, 0f), root, new Vector3(0f, hoodY + 0.3f, hoodZ + 0.22f), new Vector3(0.36f, 0.07f, 0.02f), Quaternion.identity);
            }

            // side exhausts under the doors (nitro 2+, boss)
            if (nitro >= 2 || boss)
            {
                float pipeL = Mathf.Max(0.8f, (doorZ1 - doorZ0) * 0.8f);
                float pipeZ = (doorZ0 + doorZ1) * 0.5f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Part("SidePipe", cyl, Lit(new Color(0.55f, 0.52f, 0.48f), 0.3f, 0.6f), root, new Vector3(side * (doorX + 0.07f), low + 0.2f, pipeZ), new Vector3(0.12f, pipeL, 0.12f), Quaternion.Euler(90f, 0f, 0f));
                    Part("PipeTip", cyl, Glow(new Color(1f, 0.45f, 0.15f), 1.4f), root, new Vector3(side * (doorX + 0.07f), low + 0.2f, pipeZ - pipeL * 0.5f - 0.02f), new Vector3(0.1f, 0.03f, 0.1f), Quaternion.Euler(90f, 0f, 0f));
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
