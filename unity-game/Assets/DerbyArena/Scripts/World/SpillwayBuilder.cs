using System.Collections.Generic;
using UnityEngine;

namespace DerbyArena.World
{
    public static class SpillwayBuilder
    {
        // ------------------------------------------------------------------ materials
        // Materials are cloned from template materials stored in Resources/Derby/Materials.
        // The Editor setup generates them; keeping them in Resources guarantees the shader variants
        // (normal map, emission, transparency) are compiled into the Android player.

        static Material Make(string template)
        {
            var t = Resources.Load<Material>("Derby/Materials/" + template);
            if (t != null) return new Material(t);

            Shader s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Legacy Shaders/Diffuse");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null)
            {
                Debug.LogError("DERBY: material template '" + template + "' is missing and no fallback shader exists.");
                return null;
            }
            Debug.LogWarning("DERBY: material template '" + template + "' missing; using fallback shader " + s.name);
            return new Material(s);
        }

        static void SetColor(Material m, string prop, Color c)
        {
            if (m != null && m.HasProperty(prop)) m.SetColor(prop, c);
        }

        static void SetFloat(Material m, string prop, float v)
        {
            if (m != null && m.HasProperty(prop)) m.SetFloat(prop, v);
        }

        static void SetTex(Material m, string prop, Texture t)
        {
            if (m != null && t != null && m.HasProperty(prop)) m.SetTexture(prop, t);
        }

        public static Material Lit(Color albedo, float rough, float metal, Color emission, float energy)
        {
            var mat = Make(energy > 0f ? "RuntimeLitEmissive" : "RuntimeLit");
            if (mat == null) return null;
            SetColor(mat, "_Color", albedo);
            SetFloat(mat, "_Glossiness", 1f - rough);
            SetFloat(mat, "_Metallic", Mathf.Min(metal, 0.5f));
            if (energy > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                SetColor(mat, "_EmissionColor", emission * energy);
            }
            return mat;
        }

        public static Material Textured(Texture2D albedo, Color tint, float smooth, float metal)
        {
            var mat = Make("RuntimeLit");
            if (mat == null) return null;
            SetColor(mat, "_Color", tint);
            SetTex(mat, "_MainTex", albedo);
            SetFloat(mat, "_Glossiness", smooth);
            SetFloat(mat, "_Metallic", Mathf.Min(metal, 0.5f));
            return mat;
        }

        public static Material TexturedNormal(Texture2D albedo, Texture2D normal, Color tint, float smooth, float metal)
        {
            if (normal == null) return Textured(albedo, tint, smooth, metal);
            var mat = Make("RuntimeLitNormal");
            if (mat == null) return null;
            SetColor(mat, "_Color", tint);
            SetTex(mat, "_MainTex", albedo);
            SetTex(mat, "_BumpMap", normal);
            SetFloat(mat, "_BumpScale", 1f);
            SetFloat(mat, "_Glossiness", smooth);
            SetFloat(mat, "_Metallic", Mathf.Min(metal, 0.5f));
            return mat;
        }

        public static Material Glass(Color color)
        {
            var mat = Make("RuntimeLitGlass");
            if (mat == null) return null;
            SetColor(mat, "_Color", color);
            SetFloat(mat, "_Glossiness", 0.85f);
            SetFloat(mat, "_Metallic", 0f);
            return mat;
        }

        /// <summary>Flat colour material for particles / effects (Sprites/Default works in every pipeline).</summary>
        public static Material Fx(Color color)
        {
            var mat = Make("RuntimeFx");
            if (mat == null) return null;
            SetColor(mat, "_Color", color);
            return mat;
        }

        // ------------------------------------------------------------------ world
        public static void Build(Transform parent)
        {
            var concrete = Lit(new Color(0.32f, 0.31f, 0.29f), 0.82f, 0.02f, Color.black, 0f);
            var wet = Lit(new Color(0.22f, 0.23f, 0.24f), 0.35f, 0.08f, Color.black, 0f);
            var steel = Lit(new Color(0.45f, 0.46f, 0.48f), 0.4f, 0.4f, Color.black, 0f);
            var hazard = Lit(new Color(0.85f, 0.65f, 0.08f), 0.55f, 0.1f, Color.black, 0f);
            var dark = Lit(new Color(0.08f, 0.08f, 0.09f), 0.7f, 0.2f, Color.black, 0f);

            // Collision shell: always present (the OBJ meshes are visuals only).
            // Its renderers are hidden once the real arena meshes have loaded.
            var core = new GameObject("Collision").transform;
            core.SetParent(parent, false);
            var coverage = new Dictionary<string, GameObject>();
            coverage["channel_floor"] = Box(core, "Floor", new Vector3(0f, -0.35f, 4f), new Vector3(78f, 0.8f, 92f), concrete, true);
            coverage["wall_west"] = Box(core, "WallW", new Vector3(-37f, 3.2f, 4f), new Vector3(1.2f, 6.4f, 92f), concrete, true);
            coverage["wall_east"] = Box(core, "WallE", new Vector3(37f, 3.2f, 4f), new Vector3(1.2f, 6.4f, 92f), concrete, true);
            coverage["wall_south"] = Box(core, "WallS", new Vector3(0f, 3.2f, -40.8f), new Vector3(78f, 6.4f, 1.6f), concrete, true);
            coverage["wall_north"] = Box(core, "WallN", new Vector3(0f, 3.2f, 48.8f), new Vector3(78f, 6.4f, 1.6f), concrete, true);

            // Fallback dressing: simple boxes, only used when the OBJ arena cannot be loaded.
            var dressing = new GameObject("FallbackDressing").transform;
            dressing.SetParent(parent, false);
            Box(dressing, "WetCenter", new Vector3(0f, 0.02f, 6f), new Vector3(16f, 0.04f, 70f), wet, false);
            for (int i = 0; i < 8; i++)
            {
                float z = -30f + i * 9f;
                Pipe(dressing, new Vector3(-32f, 3.4f, z), steel);
                Pipe(dressing, new Vector3(32f, 3.4f, z), steel);
                Box(dressing, "Stripe" + i, new Vector3(-36.2f, 1.2f, z), new Vector3(0.3f, 0.8f, 2.4f), hazard, false);
            }
            Box(dressing, "BridgeDeck", new Vector3(0f, 4.2f, 2f), new Vector3(28f, 0.35f, 4.2f), steel, true);
            Box(dressing, "BridgePierL", new Vector3(-8f, 2f, 2f), new Vector3(1.2f, 4f, 1.2f), concrete, true);
            Box(dressing, "BridgePierR", new Vector3(8f, 2f, 2f), new Vector3(1.2f, 4f, 1.2f), concrete, true);
            Box(dressing, "PipeHall", new Vector3(28f, 1.6f, 18f), new Vector3(10f, 3.2f, 22f), dark, true);
            Box(dressing, "BarrierA", new Vector3(-10f, 0.6f, -8f), new Vector3(6f, 1.2f, 1.2f), hazard, true);
            Box(dressing, "BarrierB", new Vector3(12f, 0.6f, 14f), new Vector3(1.2f, 1.2f, 7f), hazard, true);
            Box(dressing, "BarrierC", new Vector3(-6f, 0.6f, 22f), new Vector3(8f, 1.2f, 1.1f), concrete, true);

            BuildReactor(parent, new Vector3(-27f, 0f, 38f));

            Lamp(parent, new Vector3(-20f, 7f, -20f), new Color(0.9f, 0.12f, 0.08f), 1.8f, 18f);
            Lamp(parent, new Vector3(20f, 7f, -10f), new Color(0.9f, 0.12f, 0.08f), 1.8f, 18f);
            Lamp(parent, new Vector3(-18f, 7f, 24f), new Color(0.95f, 0.55f, 0.15f), 1.8f, 18f);
            Lamp(parent, new Vector3(16f, 7f, 30f), new Color(0.9f, 0.12f, 0.08f), 1.8f, 18f);
            Lamp(parent, new Vector3(0f, 6f, 0f), new Color(0.35f, 0.55f, 0.9f), 1.2f, 22f);

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.80f, 0.86f, 1f);
            sun.intensity = 0.95f;
            sun.shadows = SystemInfo.systemMemorySize >= 3500 ? LightShadows.Hard : LightShadows.None;
            sun.shadowStrength = 0.75f;
            sun.shadowBias = 0.06f;
            sun.shadowNormalBias = 0.4f;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.5f, 0.45f);
            fill.intensity = 0.3f;
            fill.shadows = LightShadows.None;

            var assets = parent.gameObject.AddComponent<SpillwayAssets>();
            assets.Begin(parent, coverage, dressing.gameObject);
        }

        public static GameObject Marker(Transform parent, string name, Vector3 pos, Color color, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            var material = Lit(color, 0.4f, 0.2f, color, 1.4f);
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return go;
        }

        /// <summary>Thin glowing light column so pickups can be spotted from across the arena.</summary>
        public static void Beacon(Transform target, Color color, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Beacon";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(target, false);
            Vector3 ls = target.lossyScale;
            float sx = Mathf.Max(0.01f, ls.x);
            float sy = Mathf.Max(0.01f, ls.y);
            float sz = Mathf.Max(0.01f, ls.z);
            go.transform.localScale = new Vector3(0.16f / sx, height * 0.5f / sy, 0.16f / sz);
            go.transform.localPosition = new Vector3(0f, height * 0.5f / sy, 0f);
            var r = go.GetComponent<Renderer>();
            var m = Lit(color, 0.5f, 0f, color, 2.2f);
            if (r != null && m != null)
            {
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;
            go.isStatic = true;
            if (!solid)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            return go;
        }

        static GameObject Cyl(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;
            if (!solid)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            return go;
        }

        static void Pipe(Transform parent, Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Pipe";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(1.4f, 4.2f, 1.4f);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;
            go.isStatic = true;
        }

        static void BuildReactor(Transform parent, Vector3 center)
        {
            var body = Lit(new Color(0.10f, 0.11f, 0.12f), 0.5f, 0.4f, Color.black, 0f);
            var glow = Lit(new Color(0.1f, 0.6f, 0.8f), 0.3f, 0f, new Color(0.15f, 0.85f, 1f), 2.4f);

            var root = new GameObject("Reactor").transform;
            root.SetParent(parent, false);
            root.position = center;
            Cyl(root, "Base", new Vector3(0f, 0.6f, 0f), new Vector3(7f, 0.6f, 7f), body, true);
            Cyl(root, "Tower", new Vector3(0f, 4.2f, 0f), new Vector3(3.2f, 3.0f, 3.2f), body, true);
            Cyl(root, "RingLow", new Vector3(0f, 2.4f, 0f), new Vector3(3.7f, 0.12f, 3.7f), glow, false);
            Cyl(root, "RingMid", new Vector3(0f, 4.2f, 0f), new Vector3(3.7f, 0.12f, 3.7f), glow, false);
            Cyl(root, "RingTop", new Vector3(0f, 6.0f, 0f), new Vector3(3.7f, 0.12f, 3.7f), glow, false);

            var lightGo = new GameObject("ReactorGlow");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 0.8f, 1f);
            light.intensity = 2.0f;
            light.range = 24f;
            light.shadows = LightShadows.None;
        }

        static void Lamp(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("EmergencyLamp");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }
    }
}
