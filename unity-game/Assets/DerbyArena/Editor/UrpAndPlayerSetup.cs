#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace DerbyArena.EditorTools
{
    /// <summary>
    /// Build setup. The class keeps its historical name (UrpAndPlayerSetup) so existing build
    /// automation entry points keep working, but the project now uses the Built-in render pipeline:
    /// no generated SRP assets are needed for the camera to render in a player build.
    /// </summary>
    [InitializeOnLoad]
    public static class UrpAndPlayerSetup
    {
        const string RuntimeFolder = "Assets/Resources/Derby";
        const string MaterialsFolder = RuntimeFolder + "/Materials";
        const string LitMaterialPath = MaterialsFolder + "/RuntimeLit.mat";
        const string NormalMaterialPath = MaterialsFolder + "/RuntimeLitNormal.mat";
        const string EmissiveMaterialPath = MaterialsFolder + "/RuntimeLitEmissive.mat";
        const string GlassMaterialPath = MaterialsFolder + "/RuntimeLitGlass.mat";
        const string FxMaterialPath = MaterialsFolder + "/RuntimeFx.mat";
        const string FxAddMaterialPath = MaterialsFolder + "/RuntimeFxAdd.mat";
        const string SkyMaterialPath = MaterialsFolder + "/RuntimeSky.mat";
        internal const string ScenePath = "Assets/DerbyArena/Scenes/BlackoutSpillway.unity";

        static UrpAndPlayerSetup()
        {
            EditorApplication.delayCall += TrySetupFromEditor;
        }

        static void TrySetupFromEditor()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            try { ApplyBuildSettings(); }
            catch (Exception ex) { Debug.LogError("DERBY editor setup failed: " + ex); }
        }

        [MenuItem("Derby/Repair Project And Android Settings")]
        public static void Setup()
        {
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static Material EnsureMaterial(string path, string shaderName, Action<Material> configure)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new BuildFailedException("DERBY: required shader not found in Editor: " + shaderName);

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader != shader)
            {
                if (material != null)
                    AssetDatabase.DeleteAsset(path);
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

            material.enableInstancing = true;
            if (configure != null)
                configure(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Shader keywords are only compiled into a player build when a material that is part of
        // the build enables them. These template materials live in Resources and therefore make
        // sure the normal-map, emission and transparent variants exist on the device.
        static void CreateRuntimeMaterials()
        {
            EnsureMaterial(LitMaterialPath, "Standard", null);

            EnsureMaterial(NormalMaterialPath, "Standard", m =>
            {
                m.EnableKeyword("_NORMALMAP");
            });

            EnsureMaterial(EmissiveMaterialPath, "Standard", m =>
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty("_EmissionColor"))
                    m.SetColor("_EmissionColor", Color.white);
            });

            EnsureMaterial(GlassMaterialPath, "Standard", m =>
            {
                m.SetOverrideTag("RenderType", "Transparent");
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);
                if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)BlendMode.One);
                if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_ALPHABLEND_ON");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            });

            EnsureMaterial(FxMaterialPath, "Sprites/Default", null);
            EnsureMaterial(FxAddMaterialPath, "Legacy Shaders/Particles/Additive", null);
            EnsureMaterial(SkyMaterialPath, "Skybox/Procedural", m =>
            {
                if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", 0.03f);
                if (m.HasProperty("_AtmosphereThickness")) m.SetFloat("_AtmosphereThickness", 1.4f);
            });
        }

        static void EnableBothInputBackends()
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets == null || assets.Length == 0)
                    return;
                var so = new SerializedObject(assets[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue != 2)
                {
                    prop.intValue = 2; // Both: legacy Input + Input System
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: could not set active input handler: " + ex.Message);
            }
        }

        /// <summary>
        /// Fog is enabled from code at runtime, so "Automatic" fog stripping (which looks at the scene's
        /// saved RenderSettings) would remove the fog shader variants from the player. Keep them explicitly.
        /// </summary>
        static void KeepFogVariants()
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (assets == null || assets.Length == 0) return;
                var so = new SerializedObject(assets[0]);
                var strip = so.FindProperty("m_FogStripping");
                if (strip != null) strip.intValue = 1;
                string[] keep = { "m_FogKeepLinear", "m_FogKeepExp", "m_FogKeepExp2" };
                for (int i = 0; i < keep.Length; i++)
                {
                    var p = so.FindProperty(keep[i]);
                    if (p != null) p.boolValue = true;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: fog stripping settings not applied: " + ex.Message);
            }
        }

        public static void ApplyBuildSettings()
        {
            if (!File.Exists(ScenePath))
                throw new BuildFailedException("DERBY: startup scene is missing: " + ScenePath);

            Directory.CreateDirectory(RuntimeFolder);
            Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Built-in render pipeline: no SRP asset anywhere.
            GraphicsSettings.defaultRenderPipeline = null;
            string[] qualityNames = QualitySettings.names;
            int previousQuality = QualitySettings.GetQualityLevel();
            if (qualityNames != null && qualityNames.Length > 0)
            {
                for (int i = 0; i < qualityNames.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = null;
                }
                QualitySettings.SetQualityLevel(Mathf.Clamp(previousQuality, 0, qualityNames.Length - 1), false);
            }
            else
            {
                QualitySettings.renderPipeline = null;
            }

            CreateRuntimeMaterials();

            PlayerSettings.companyName = "LenstedReal";
            PlayerSettings.productName = "DERBY ARENA";
            PlayerSettings.bundleVersion = "1.2.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lenstedreal.derbyarena");
            PlayerSettings.Android.bundleVersionCode = 120;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                    new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: could not set Android graphics APIs: " + ex.Message);
            }

            EnableBothInputBackends();
            KeepFogVariants();

            // Runtime code builds every collider / particle / audio component with AddComponent, so engine
            // code stripping removed classes such as CapsuleCollider ("Can't add component because class
            // 'CapsuleCollider' doesn't exist"). Keep engine code; link.xml additionally preserves modules.
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);

            // iOS: same identity; Unity Build Automation signs with your Apple credentials.
            try
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.lenstedreal.derbyarena");
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Minimal);
                PlayerSettings.iOS.buildNumber = "120";
                PlayerSettings.iOS.targetOSVersionString = "13.0";
                PlayerSettings.iOS.requiresFullScreen = true;
                PlayerSettings.iOS.hideHomeButton = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: iOS settings not applied: " + ex.Message);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (GraphicsSettings.defaultRenderPipeline != null)
                throw new BuildFailedException("DERBY: a render pipeline asset is still assigned; expected Built-in.");
            if (AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(NormalMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(EmissiveMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(FxMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(FxAddMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath) == null)
                throw new BuildFailedException("DERBY: runtime Resources materials were not generated.");

            Debug.Log("DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS / DERBY ARENA / 1.2.0 / 120 / ARM64 + iOS");
        }
    }

    public sealed class DerbyBuildSettingsEnforcer : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report) => UrpAndPlayerSetup.ApplyBuildSettings();
    }

    public static class BatchValidate
    {
        public static void Run()
        {
            UrpAndPlayerSetup.ApplyBuildSettings();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UrpAndPlayerSetup.ScenePath);
            var boot = UnityEngine.Object.FindAnyObjectByType<DerbyArena.Core.DerbyBootstrap>();
            EditorApplication.Exit(boot == null || string.IsNullOrEmpty(scene.path) ? 2 : 0);
        }
    }
}
#endif
