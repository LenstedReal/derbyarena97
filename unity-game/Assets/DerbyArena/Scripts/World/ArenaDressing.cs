using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Derby-stadium dressing in code: red/white striped barrier caps, floodlight masts, tyre stacks, oil drums and
    /// skull banners. Visual first; only the tyre stacks and drums have (small, box) colliders and they stay out of
    /// spawns, pickups, gates and the driving line. Shared materials, static batching (2.1), no extra real-time lights.
    /// </summary>
    public static class ArenaDressing
    {
        static Material Lit(Color c, float rough, float metal) { return Mats.Lit(c, rough, metal, Color.black, 0f); }

        static GameObject Visual(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, bool shadow)
        {
            var go = MeshFactory.Make(name, mesh, mat, parent, pos, scale);
            go.transform.rotation = rot;
            go.transform.position = pos;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
            return go;
        }

        /// <summary>Stack of tyres (visual cylinders, one box collider).</summary>
        public static void TyreStack(Transform parent, Vector3 at, int count, Material rubber, Material rim)
        {
            var t = new GameObject("TyreStack").transform;
            t.SetParent(parent, false);
            t.position = at;
            var cyl = MeshFactory.UnitCylinder();
            for (int i = 0; i < count; i++)
            {
                Vector3 p = at + new Vector3(Mathf.Sin(i * 2.1f) * 0.06f, 0.18f + i * 0.34f, Mathf.Cos(i * 1.7f) * 0.06f);
                Visual("Tyre", cyl, rubber, t, p, new Vector3(1.15f, 0.32f, 1.15f), Quaternion.Euler(0f, i * 23f, 0f), i == 0);
                Visual("Rim", cyl, rim, t, p + Vector3.up * 0.01f, new Vector3(0.55f, 0.33f, 0.55f), Quaternion.identity, false);
            }
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, count * 0.17f, 0f);
            col.size = new Vector3(1.05f, count * 0.34f, 1.05f);
            t.gameObject.isStatic = true;
        }

        public static void Drum(Transform parent, Vector3 at, Material paint, Material band)
        {
            var t = new GameObject("OilDrum").transform;
            t.SetParent(parent, false);
            t.position = at;
            var cyl = MeshFactory.UnitCylinder();
            Visual("Drum", cyl, paint, t, at + Vector3.up * 0.45f, new Vector3(0.62f, 0.9f, 0.62f), Quaternion.identity, true);
            Visual("Band", cyl, band, t, at + Vector3.up * 0.3f, new Vector3(0.64f, 0.05f, 0.64f), Quaternion.identity, false);
            Visual("Band", cyl, band, t, at + Vector3.up * 0.62f, new Vector3(0.64f, 0.05f, 0.64f), Quaternion.identity, false);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.6f, 0.9f, 0.6f);
        }

        /// <summary>Floodlight mast: lattice pole, cross head with glowing lamp panels (emissive only, no light component).</summary>
        public static void Floodlight(Transform parent, Vector3 foot, float height, Vector3 faceTo, Material steel, Material lamp)
        {
            var t = new GameObject("Floodlight").transform;
            t.SetParent(parent, false);
            t.position = foot;
            var cube = MeshFactory.UnitCube();
            Vector3 dir = faceTo - foot; dir.y = 0f;
            Quaternion face = dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir.normalized, Vector3.up) : Quaternion.identity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 o = face * new Vector3(i % 2 == 0 ? -0.35f : 0.35f, 0f, i < 2 ? -0.35f : 0.35f);
                Visual("Leg", cube, steel, t, foot + o + Vector3.up * height * 0.5f, new Vector3(0.12f, height, 0.12f), face, false);
            }
            for (int k = 1; k < 6; k++)
                Visual("Brace", cube, steel, t, foot + Vector3.up * (height * k / 6f), new Vector3(0.82f, 0.08f, 0.82f), face, false);
            Vector3 head = foot + Vector3.up * (height + 0.6f);
            Visual("Head", cube, steel, t, head, new Vector3(3.2f, 1.6f, 0.3f), face, false);
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 3; c++)
                    Visual("Lamp", cube, lamp, t, head + face * new Vector3(-1f + c, -0.35f + r * 0.7f, 0.18f), new Vector3(0.82f, 0.56f, 0.06f), face * Quaternion.Euler(-12f, 0f, 0f), false);
        }

        public static void Banner(Transform parent, Vector3 foot, Vector3 faceTo, Material pole, Material cloth, Material skull)
        {
            var t = new GameObject("Banner").transform;
            t.SetParent(parent, false);
            var cube = MeshFactory.UnitCube();
            Vector3 dir = faceTo - foot; dir.y = 0f;
            Quaternion face = dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir.normalized, Vector3.up) : Quaternion.identity;
            Visual("Pole", cube, pole, t, foot + Vector3.up * 2.4f, new Vector3(0.1f, 4.8f, 0.1f), face, false);
            Visual("Cloth", cube, cloth, t, foot + Vector3.up * 3.5f + face * new Vector3(0.6f, 0f, 0f), new Vector3(1.1f, 2f, 0.04f), face, false);
            // simple skull mark: head disc + jaw block (no texture needed)
            var cyl = MeshFactory.UnitCylinder();
            Vector3 mark = foot + Vector3.up * 3.75f + face * new Vector3(0.6f, 0f, 0.035f);
            Visual("Skull", cyl, skull, t, mark, new Vector3(0.5f, 0.02f, 0.5f), face * Quaternion.Euler(90f, 0f, 0f), false);
            Visual("Jaw", cube, skull, t, mark + Vector3.down * 0.3f, new Vector3(0.3f, 0.18f, 0.02f), face, false);
        }

        /// <summary>
        /// 2.1: everything in the dressing is static, but objects made at run time are not batched by Unity on their
        /// own. Combining the container turns ~250 small renderers into a handful of draw calls.
        /// </summary>
        static Transform Container(Transform root)
        {
            var t = new GameObject("Dressing").transform;
            t.SetParent(root, false);
            return t;
        }

        static void Batch(Transform container)
        {
            try { StaticBatchingUtility.Combine(container.gameObject); }
            catch (System.Exception ex) { Debug.LogWarning("DERBY: dressing batching skipped. " + ex.Message); }
        }

        /// <summary>Colosseum: striped barrier cap on the ring wall, four floodlight masts, tyre stacks and drums at the wall.</summary>
        public static void Colosseum(Transform arenaRoot, Color accent, int quality)
        {
            var root = Container(arenaRoot);
            var red = Lit(new Color(0.78f, 0.08f, 0.06f), 0.55f, 0.05f);
            var white = Lit(new Color(0.92f, 0.9f, 0.86f), 0.55f, 0.05f);
            var cube = MeshFactory.UnitCube();
            const int segs = 40;
            const float r = 30.1f;
            float w = 2f * Mathf.PI * r / segs + 0.05f;
            var cap = new GameObject("BarrierCap").transform;
            cap.SetParent(root, false);
            for (int i = 0; i < segs; i++)
            {
                float a = i * 360f / segs;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 3.12f, r - 0.5f);
                Visual("Cap", cube, i % 2 == 0 ? red : white, cap, p, new Vector3(w, 0.24f, 0.5f), Quaternion.Euler(0f, a, 0f), false);
            }

            var steel = Lit(new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f);
            var lamp = Mats.Glow(new Color(1f, 0.97f, 0.88f), quality >= 2 ? 4f : 3f);
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f) * Mathf.Deg2Rad;
                Floodlight(root, new Vector3(Mathf.Sin(a) * 30.1f, 3.2f, Mathf.Cos(a) * 30.1f), 13f, Vector3.zero, steel, lamp);
            }

            var rubber = Lit(new Color(0.05f, 0.05f, 0.055f), 0.9f, 0f);
            var rim = Lit(new Color(0.5f, 0.5f, 0.52f), 0.4f, 0.6f);
            var drumRed = Lit(new Color(0.6f, 0.1f, 0.06f), 0.6f, 0.2f);
            var drumYellow = Lit(new Color(0.85f, 0.62f, 0.08f), 0.6f, 0.2f);
            var band = Lit(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.4f);
            float[] tyreAngles = { 20f, 110f, 200f, 290f };
            for (int i = 0; i < tyreAngles.Length; i++)
            {
                float a = tyreAngles[i] * Mathf.Deg2Rad;
                TyreStack(root, new Vector3(Mathf.Sin(a) * 28.3f, 0f, Mathf.Cos(a) * 28.3f), 3 + i % 2, rubber, rim);
                float b = (tyreAngles[i] + 6f) * Mathf.Deg2Rad;
                Drum(root, new Vector3(Mathf.Sin(b) * 28.6f, 0f, Mathf.Cos(b) * 28.6f), i % 2 == 0 ? drumRed : drumYellow, band);
            }

            if (quality > 0)
            {
                var pole = Lit(new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.5f);
                var cloth = Lit(new Color(0.55f, 0.04f, 0.04f), 0.8f, 0f);
                var bone = Lit(new Color(0.92f, 0.9f, 0.84f), 0.7f, 0f);
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + i * 90f + 12f) * Mathf.Deg2Rad;
                    Banner(root, new Vector3(Mathf.Sin(a) * 30.3f, 3.2f, Mathf.Cos(a) * 30.3f), Vector3.zero, pole, cloth, bone);
                }
            }
            Batch(root);
        }

        /// <summary>Spillway: tyre stacks and drums along the side walls, clear of pipes, spawns and the gate line.</summary>
        public static void Spillway(Transform arenaRoot)
        {
            var root = Container(arenaRoot);
            var rubber = Lit(new Color(0.05f, 0.05f, 0.055f), 0.9f, 0f);
            var rim = Lit(new Color(0.5f, 0.5f, 0.52f), 0.4f, 0.6f);
            var drumRed = Lit(new Color(0.6f, 0.1f, 0.06f), 0.6f, 0.2f);
            var band = Lit(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.4f);
            // positions checked against the light posts (x = +-34.2), drain pipes (z = -20 / 4 / 28) and reactor towers
            Vector3[] tyres = { new Vector3(-34.6f, 0f, -36f), new Vector3(34.6f, 0f, -14f), new Vector3(-34.6f, 0f, 8f), new Vector3(34.6f, 0f, 41f) };
            Vector3[] drums = { new Vector3(34.8f, 0f, -33f), new Vector3(-34.8f, 0f, -10f), new Vector3(34.8f, 0f, 12f), new Vector3(-34.8f, 0f, 36f) };
            for (int i = 0; i < tyres.Length; i++) TyreStack(root, tyres[i], 3, rubber, rim);
            for (int i = 0; i < drums.Length; i++) Drum(root, drums[i], drumRed, band);
            Batch(root);
        }
    }
}
