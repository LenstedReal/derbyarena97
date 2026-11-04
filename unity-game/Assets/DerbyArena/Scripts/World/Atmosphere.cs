using UnityEngine;
using UnityEngine.Rendering;

namespace DerbyArena.World
{
    /// <summary>Per-chapter lighting moods: sky, fog, ambient trilight, key / fill lights and colour grading.</summary>
    public static class Atmosphere
    {
        public struct Theme
        {
            public Color SkyTint, SkyGround, Fog, AmbSky, AmbEquator, AmbGround, Key, Fill, Accent;
            public float FogDensity, KeyIntensity, FillIntensity, SkyExposure, Thickness;
            public Vector3 KeyEuler;
            public Color GradeGain, GradeLift;
            public float Saturation, Contrast;
            public float BeamStrength;      // headlight beam visibility (day low, night high)
        }

        // Last applied mood, kept so the brightness setting can be changed live from the menus.
        static Theme current;
        static bool hasCurrent;
        static Light keyLight, fillLight;
        static Material skyMaterial;

        /// <summary>
        /// Moods follow the Unity AI demolition-derby reference: a bright, readable sand floor in every theme.
        /// Day (sunny sandstone), dramatic orange haze, toxic, neon night (purple haze), crimson.
        /// </summary>
        public static Theme Get(int index)
        {
            var t = new Theme();
            switch (Mathf.Clamp(index, 0, 5))
            {
                case 0: // golden day
                    t.SkyTint = new Color(0.5f, 0.62f, 0.85f); t.SkyGround = new Color(0.55f, 0.5f, 0.45f);
                    t.Fog = new Color(0.8f, 0.76f, 0.68f); t.FogDensity = 0.0032f;
                    t.AmbSky = new Color(0.72f, 0.78f, 0.9f); t.AmbEquator = new Color(0.7f, 0.62f, 0.52f); t.AmbGround = new Color(0.42f, 0.36f, 0.3f);
                    t.Key = new Color(1f, 0.93f, 0.8f); t.KeyIntensity = 1.45f; t.KeyEuler = new Vector3(48f, -35f, 0f);
                    t.Fill = new Color(0.55f, 0.65f, 0.85f); t.FillIntensity = 0.35f; t.Accent = new Color(1f, 0.6f, 0.2f);
                    t.SkyExposure = 1.25f; t.Thickness = 0.9f;
                    t.GradeGain = new Color(1.03f, 1f, 0.96f); t.GradeLift = new Color(0f, 0f, 0f); t.Saturation = 1.08f; t.Contrast = 1.05f;
                    t.BeamStrength = 0.1f;
                    break;
                case 1: // dramatic orange haze
                    t.SkyTint = new Color(0.85f, 0.48f, 0.28f); t.SkyGround = new Color(0.5f, 0.32f, 0.2f);
                    t.Fog = new Color(0.82f, 0.55f, 0.36f); t.FogDensity = 0.0055f;
                    t.AmbSky = new Color(0.78f, 0.6f, 0.5f); t.AmbEquator = new Color(0.72f, 0.5f, 0.36f); t.AmbGround = new Color(0.4f, 0.27f, 0.18f);
                    t.Key = new Color(1f, 0.78f, 0.55f); t.KeyIntensity = 1.35f; t.KeyEuler = new Vector3(30f, 60f, 0f);
                    t.Fill = new Color(0.5f, 0.5f, 0.7f); t.FillIntensity = 0.3f; t.Accent = new Color(1f, 0.42f, 0.12f);
                    t.SkyExposure = 1.3f; t.Thickness = 2f;
                    t.GradeGain = new Color(1.04f, 0.99f, 0.92f); t.GradeLift = new Color(0f, 0f, 0f); t.Saturation = 1.1f; t.Contrast = 1.06f;
                    t.BeamStrength = 0.3f;
                    break;
                case 2: // toxic foundry
                    t.SkyTint = new Color(0.55f, 0.7f, 0.5f); t.SkyGround = new Color(0.35f, 0.4f, 0.3f);
                    t.Fog = new Color(0.55f, 0.66f, 0.5f); t.FogDensity = 0.005f;
                    t.AmbSky = new Color(0.62f, 0.72f, 0.62f); t.AmbEquator = new Color(0.52f, 0.6f, 0.45f); t.AmbGround = new Color(0.3f, 0.34f, 0.26f);
                    t.Key = new Color(0.95f, 1f, 0.85f); t.KeyIntensity = 1.3f; t.KeyEuler = new Vector3(45f, 120f, 0f);
                    t.Fill = new Color(0.4f, 0.7f, 0.6f); t.FillIntensity = 0.3f; t.Accent = new Color(0.55f, 1f, 0.25f);
                    t.SkyExposure = 1.15f; t.Thickness = 1.8f;
                    t.GradeGain = new Color(0.98f, 1.03f, 0.96f); t.GradeLift = new Color(0f, 0f, 0f); t.Saturation = 1.08f; t.Contrast = 1.05f;
                    t.BeamStrength = 0.2f;
                    break;
                case 3: // neon night (purple haze, still readable)
                    t.SkyTint = new Color(0.32f, 0.36f, 0.65f); t.SkyGround = new Color(0.18f, 0.18f, 0.3f);
                    t.Fog = new Color(0.3f, 0.24f, 0.45f); t.FogDensity = 0.006f;
                    t.AmbSky = new Color(0.5f, 0.52f, 0.75f); t.AmbEquator = new Color(0.45f, 0.36f, 0.6f); t.AmbGround = new Color(0.22f, 0.2f, 0.32f);
                    t.Key = new Color(0.75f, 0.85f, 1f); t.KeyIntensity = 1.05f; t.KeyEuler = new Vector3(55f, -150f, 0f);
                    t.Fill = new Color(1f, 0.45f, 0.85f); t.FillIntensity = 0.4f; t.Accent = new Color(0.3f, 0.85f, 1f);
                    t.SkyExposure = 0.75f; t.Thickness = 0.9f;
                    t.GradeGain = new Color(0.98f, 0.99f, 1.06f); t.GradeLift = new Color(0f, 0f, 0.01f); t.Saturation = 1.12f; t.Contrast = 1.06f;
                    t.BeamStrength = 0.45f;
                    break;
                case 5: // harbour dusk: blue-teal evening sky, low warm sun, sodium-orange accents
                    t.SkyTint = new Color(0.36f, 0.47f, 0.66f); t.SkyGround = new Color(0.28f, 0.27f, 0.28f);
                    t.Fog = new Color(0.5f, 0.52f, 0.58f); t.FogDensity = 0.0042f;
                    t.AmbSky = new Color(0.58f, 0.64f, 0.78f); t.AmbEquator = new Color(0.6f, 0.54f, 0.5f); t.AmbGround = new Color(0.34f, 0.3f, 0.26f);
                    t.Key = new Color(1f, 0.8f, 0.6f); t.KeyIntensity = 1.35f; t.KeyEuler = new Vector3(26f, -60f, 0f);
                    t.Fill = new Color(0.45f, 0.6f, 0.85f); t.FillIntensity = 0.35f; t.Accent = new Color(1f, 0.62f, 0.18f);
                    t.SkyExposure = 1.1f; t.Thickness = 1.4f;
                    t.GradeGain = new Color(1.02f, 1f, 0.98f); t.GradeLift = new Color(0f, 0f, 0f); t.Saturation = 1.08f; t.Contrast = 1.05f;
                    t.BeamStrength = 0.3f;
                    break;
                default: // crimson
                    t.SkyTint = new Color(0.85f, 0.35f, 0.28f); t.SkyGround = new Color(0.45f, 0.2f, 0.16f);
                    t.Fog = new Color(0.72f, 0.38f, 0.3f); t.FogDensity = 0.0055f;
                    t.AmbSky = new Color(0.72f, 0.52f, 0.5f); t.AmbEquator = new Color(0.66f, 0.42f, 0.34f); t.AmbGround = new Color(0.36f, 0.22f, 0.18f);
                    t.Key = new Color(1f, 0.68f, 0.5f); t.KeyIntensity = 1.35f; t.KeyEuler = new Vector3(24f, 20f, 0f);
                    t.Fill = new Color(0.6f, 0.45f, 0.7f); t.FillIntensity = 0.3f; t.Accent = new Color(1f, 0.25f, 0.12f);
                    t.SkyExposure = 1.15f; t.Thickness = 2.4f;
                    t.GradeGain = new Color(1.04f, 0.97f, 0.95f); t.GradeLift = new Color(0f, 0f, 0f); t.Saturation = 1.06f; t.Contrast = 1.06f;
                    t.BeamStrength = 0.35f;
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
            RenderSettings.reflectionIntensity = 1f;

            var keyGo = new GameObject("KeyLight");
            keyGo.transform.SetParent(parent, false);
            keyGo.transform.rotation = Quaternion.Euler(t.KeyEuler);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = t.Key;
            key.shadows = quality == 0 ? LightShadows.None : (quality >= 2 ? LightShadows.Soft : LightShadows.Hard);
            key.shadowStrength = 0.7f;
            key.shadowBias = 0.05f;
            key.shadowNormalBias = 0.4f;
            RenderSettings.sun = key;

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.rotation = Quaternion.Euler(t.KeyEuler.x * 0.6f + 10f, t.KeyEuler.y + 165f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = t.Fill;
            fill.shadows = LightShadows.None;

            var sky = Mats.Sky(t.SkyTint, t.SkyGround, t.SkyExposure, t.Thickness);
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            if (cam != null)
            {
                cam.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = t.Fog;
            }

            current = t;
            hasCurrent = true;
            keyLight = key;
            fillLight = fill;
            skyMaterial = sky;
            ApplyBrightness();
            if (sky != null) DynamicGI.UpdateEnvironment();

            // Sky reflection for metallic paint and glass. Without it the built-in pipeline reflects black,
            // which is why cars read as dark silhouettes. Sky only (culling mask 0), rendered once.
            try
            {
                QualitySettings.realtimeReflectionProbes = true;
                var probeGo = new GameObject("SkyReflection");
                probeGo.transform.SetParent(parent, false);
                probeGo.transform.position = new Vector3(0f, 6f, 0f);
                var probe = probeGo.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                probe.cullingMask = 0;
                probe.resolution = 64;
                probe.size = new Vector3(400f, 200f, 400f);
                probe.hdr = false;
                probe.intensity = 1f;
                probe.RenderProbe();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("DERBY: sky reflection probe unavailable. " + ex.Message);
            }
            return key;
        }

        /// <summary>Re-applies light, ambient and sky levels with the player's brightness setting (0.8 .. 1.4).</summary>
        public static void ApplyBrightness()
        {
            if (!hasCurrent) return;
            float b = Mathf.Clamp(DerbyArena.Core.Save.Data.Brightness, 0.6f, 1.6f);
            var t = current;
            RenderSettings.ambientSkyColor = t.AmbSky * b;
            RenderSettings.ambientEquatorColor = t.AmbEquator * b;
            RenderSettings.ambientGroundColor = t.AmbGround * b;
            RenderSettings.ambientIntensity = 1f;
            if (keyLight != null) keyLight.intensity = t.KeyIntensity * b;
            if (fillLight != null) fillLight.intensity = t.FillIntensity * b;
            if (skyMaterial != null && skyMaterial.HasProperty("_Exposure")) skyMaterial.SetFloat("_Exposure", t.SkyExposure * b);
        }

        public static float CurrentBeamStrength { get { return hasCurrent ? current.BeamStrength : 0.2f; } }

        public static Light Lamp(Transform parent, Vector3 pos, Color color, float intensity, float range)
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
            return l;
        }
    }
}
