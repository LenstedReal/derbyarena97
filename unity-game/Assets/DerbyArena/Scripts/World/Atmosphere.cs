using UnityEngine;
using UnityEngine.Rendering;

namespace DerbyArena.World
{
    /// <summary>Per-chapter lighting moods: sky, fog, ambient trilight, key / fill lights, accent lamps.</summary>
    public static class Atmosphere
    {
        public struct Theme
        {
            public Color SkyTint, SkyGround, Fog, AmbSky, AmbEquator, AmbGround, Key, Fill, Accent;
            public float FogDensity, KeyIntensity, SkyExposure, Thickness;
            public Vector3 KeyEuler;
        }

        public static Theme Get(int index)
        {
            var t = new Theme();
            switch (Mathf.Clamp(index, 0, 4))
            {
                case 0: // warm dusk
                    t.SkyTint = new Color(0.62f, 0.5f, 0.45f); t.SkyGround = new Color(0.22f, 0.18f, 0.16f);
                    t.Fog = new Color(0.36f, 0.3f, 0.28f); t.FogDensity = 0.0075f;
                    t.AmbSky = new Color(0.46f, 0.44f, 0.48f); t.AmbEquator = new Color(0.36f, 0.3f, 0.26f); t.AmbGround = new Color(0.14f, 0.12f, 0.1f);
                    t.Key = new Color(1f, 0.82f, 0.62f); t.KeyIntensity = 1.35f; t.KeyEuler = new Vector3(32f, -38f, 0f);
                    t.Fill = new Color(0.45f, 0.55f, 0.75f); t.Accent = new Color(1f, 0.55f, 0.15f);
                    t.SkyExposure = 1.15f; t.Thickness = 1.6f;
                    break;
                case 1: // rust haze
                    t.SkyTint = new Color(0.7f, 0.45f, 0.3f); t.SkyGround = new Color(0.25f, 0.16f, 0.1f);
                    t.Fog = new Color(0.42f, 0.28f, 0.18f); t.FogDensity = 0.01f;
                    t.AmbSky = new Color(0.5f, 0.4f, 0.34f); t.AmbEquator = new Color(0.4f, 0.27f, 0.18f); t.AmbGround = new Color(0.15f, 0.1f, 0.07f);
                    t.Key = new Color(1f, 0.72f, 0.45f); t.KeyIntensity = 1.3f; t.KeyEuler = new Vector3(26f, 60f, 0f);
                    t.Fill = new Color(0.4f, 0.45f, 0.6f); t.Accent = new Color(1f, 0.35f, 0.1f);
                    t.SkyExposure = 1.1f; t.Thickness = 2.2f;
                    break;
                case 2: // toxic foundry
                    t.SkyTint = new Color(0.4f, 0.55f, 0.35f); t.SkyGround = new Color(0.12f, 0.16f, 0.1f);
                    t.Fog = new Color(0.2f, 0.28f, 0.18f); t.FogDensity = 0.012f;
                    t.AmbSky = new Color(0.36f, 0.46f, 0.36f); t.AmbEquator = new Color(0.24f, 0.32f, 0.2f); t.AmbGround = new Color(0.1f, 0.12f, 0.08f);
                    t.Key = new Color(0.85f, 1f, 0.7f); t.KeyIntensity = 1.15f; t.KeyEuler = new Vector3(45f, 120f, 0f);
                    t.Fill = new Color(0.3f, 0.6f, 0.5f); t.Accent = new Color(0.55f, 1f, 0.2f);
                    t.SkyExposure = 0.9f; t.Thickness = 2.6f;
                    break;
                case 3: // neon night
                    t.SkyTint = new Color(0.25f, 0.3f, 0.55f); t.SkyGround = new Color(0.05f, 0.06f, 0.1f);
                    t.Fog = new Color(0.08f, 0.1f, 0.2f); t.FogDensity = 0.011f;
                    t.AmbSky = new Color(0.3f, 0.32f, 0.5f); t.AmbEquator = new Color(0.22f, 0.16f, 0.32f); t.AmbGround = new Color(0.06f, 0.06f, 0.1f);
                    t.Key = new Color(0.6f, 0.75f, 1f); t.KeyIntensity = 0.95f; t.KeyEuler = new Vector3(55f, -150f, 0f);
                    t.Fill = new Color(1f, 0.3f, 0.75f); t.Accent = new Color(0.25f, 0.85f, 1f);
                    t.SkyExposure = 0.45f; t.Thickness = 0.8f;
                    break;
                default: // crimson apocalypse
                    t.SkyTint = new Color(0.65f, 0.25f, 0.2f); t.SkyGround = new Color(0.18f, 0.06f, 0.05f);
                    t.Fog = new Color(0.3f, 0.1f, 0.08f); t.FogDensity = 0.012f;
                    t.AmbSky = new Color(0.45f, 0.3f, 0.3f); t.AmbEquator = new Color(0.36f, 0.16f, 0.12f); t.AmbGround = new Color(0.12f, 0.05f, 0.04f);
                    t.Key = new Color(1f, 0.55f, 0.4f); t.KeyIntensity = 1.2f; t.KeyEuler = new Vector3(18f, 20f, 0f);
                    t.Fill = new Color(0.5f, 0.35f, 0.6f); t.Accent = new Color(1f, 0.2f, 0.1f);
                    t.SkyExposure = 0.85f; t.Thickness = 3f;
                    break;
            }
            return t;
        }

        public static Light Apply(Theme t, Transform parent, Camera cam, int quality)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = t.Fog;
            RenderSettings.fogDensity = t.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = t.AmbSky;
            RenderSettings.ambientEquatorColor = t.AmbEquator;
            RenderSettings.ambientGroundColor = t.AmbGround;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.6f;

            var keyGo = new GameObject("KeyLight");
            keyGo.transform.SetParent(parent, false);
            keyGo.transform.rotation = Quaternion.Euler(t.KeyEuler);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = t.Key;
            key.intensity = t.KeyIntensity;
            key.shadows = quality == 0 ? LightShadows.None : (quality == 2 ? LightShadows.Soft : LightShadows.Hard);
            key.shadowStrength = 0.8f;
            key.shadowBias = 0.05f;
            key.shadowNormalBias = 0.4f;
            RenderSettings.sun = key;

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.rotation = Quaternion.Euler(t.KeyEuler.x * 0.6f + 10f, t.KeyEuler.y + 165f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = t.Fill;
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;

            var sky = Mats.Sky(t.SkyTint, t.SkyGround, t.SkyExposure, t.Thickness);
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            if (sky != null) DynamicGI.UpdateEnvironment();
            if (cam != null)
            {
                cam.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = t.Fog;
            }
            return key;
        }

        public static void Lamp(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("Lamp");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
        }
    }
}
