using UnityEngine;

namespace DerbyArena.World
{
    public static class SpillwayBuilder
    {
        static Material litCache;

        public static Material Lit(Color albedo, float rough, float metal, Color emission, float energy)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Hidden/InternalErrorShader");
            if (shader == null)
                throw new System.InvalidOperationException(
                    "Derby Arena startup: no compatible shader found. Check URP/build settings.");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", albedo);
            mat.color = albedo;
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 1f - rough);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metal);
            if (energy > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * energy);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }

        public static void Build(Transform parent)
        {
            var concrete = Lit(new Color(0.32f, 0.31f, 0.29f), 0.82f, 0.02f, Color.black, 0f);
            var wet = Lit(new Color(0.22f, 0.23f, 0.24f), 0.35f, 0.08f, Color.black, 0f);
            var steel = Lit(new Color(0.45f, 0.46f, 0.48f), 0.4f, 0.75f, Color.black, 0f);
            var hazard = Lit(new Color(0.85f, 0.65f, 0.08f), 0.55f, 0.1f, Color.black, 0f);
            var dark = Lit(new Color(0.08f, 0.08f, 0.09f), 0.7f, 0.2f, Color.black, 0f);

            Box(parent, "Floor", new Vector3(0f, -0.4f, 4f), new Vector3(78f, 0.8f, 92f), concrete, true);
            Box(parent, "WetCenter", new Vector3(0f, 0.02f, 6f), new Vector3(16f, 0.04f, 70f), wet, false);
            Box(parent, "WallW", new Vector3(-38f, 3.2f, 4f), new Vector3(2.2f, 6.4f, 92f), concrete, true);
            Box(parent, "WallE", new Vector3(38f, 3.2f, 4f), new Vector3(2.2f, 6.4f, 92f), concrete, true);
            Box(parent, "WallS", new Vector3(0f, 3.2f, -41f), new Vector3(78f, 6.4f, 2.2f), concrete, true);
            Box(parent, "WallN", new Vector3(0f, 3.2f, 49f), new Vector3(78f, 6.4f, 2.2f), concrete, true);

            for (int i = 0; i < 8; i++)
            {
                float z = -30f + i * 9f;
                Pipe(parent, new Vector3(-32f, 3.4f, z), steel);
                Pipe(parent, new Vector3(32f, 3.4f, z), steel);
                Box(parent, "Stripe" + i, new Vector3(-36.2f, 1.2f, z), new Vector3(0.3f, 0.8f, 2.4f), hazard, false);
            }

            Box(parent, "BridgeDeck", new Vector3(0f, 4.2f, 2f), new Vector3(28f, 0.35f, 4.2f), steel, true);
            Box(parent, "BridgePierL", new Vector3(-8f, 2f, 2f), new Vector3(1.2f, 4f, 1.2f), concrete, true);
            Box(parent, "BridgePierR", new Vector3(8f, 2f, 2f), new Vector3(1.2f, 4f, 1.2f), concrete, true);

            Box(parent, "PipeHall", new Vector3(28f, 1.6f, 18f), new Vector3(10f, 3.2f, 22f), dark, true);
            Box(parent, "PipeMouth", new Vector3(22f, 1.3f, 18f), new Vector3(2f, 2.4f, 6f), steel, false);

            Box(parent, "BarrierA", new Vector3(-10f, 0.6f, -8f), new Vector3(6f, 1.2f, 1.2f), hazard, true);
            Box(parent, "BarrierB", new Vector3(12f, 0.6f, 14f), new Vector3(1.2f, 1.2f, 7f), hazard, true);
            Box(parent, "BarrierC", new Vector3(-6f, 0.6f, 22f), new Vector3(8f, 1.2f, 1.1f), concrete, true);

            Lamp(parent, new Vector3(-20f, 7f, -20f), new Color(0.7f, 0.08f, 0.06f));
            Lamp(parent, new Vector3(20f, 7f, -10f), new Color(0.7f, 0.08f, 0.06f));
            Lamp(parent, new Vector3(-18f, 7f, 24f), new Color(0.75f, 0.45f, 0.12f));
            Lamp(parent, new Vector3(16f, 7f, 30f), new Color(0.7f, 0.08f, 0.06f));

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.86f, 0.72f);
            sun.intensity = 1.05f;
            sun.shadows = SystemInfo.systemMemorySize >= 3500 ? LightShadows.Hard : LightShadows.None;
            sun.shadowStrength = 0.7f;
        }

        public static GameObject Marker(Transform parent, string name, Vector3 pos, Color color, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Lit(color, 0.4f, 0.2f, color, 1.2f);
            return go;
        }

        static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            if (!solid)
                Object.Destroy(go.GetComponent<Collider>());
        }

        static void Pipe(Transform parent, Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Pipe";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(1.4f, 4.2f, 1.4f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
        }

        static void Lamp(Transform parent, Vector3 pos, Color color)
        {
            var go = new GameObject("EmergencyLamp");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2.4f;
            light.range = 14f;
            light.shadows = LightShadows.None;
        }
    }
}
