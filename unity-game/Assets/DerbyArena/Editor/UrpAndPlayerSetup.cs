#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
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
        const string DecalMaterialPath = MaterialsFolder + "/RuntimeLitDecal.mat";
        const string FxMaterialPath = MaterialsFolder + "/RuntimeFx.mat";
        const string FxAddMaterialPath = MaterialsFolder + "/RuntimeFxAdd.mat";
        const string SkyMaterialPath = MaterialsFolder + "/RuntimeSky.mat";
        const string Sky2MaterialPath = MaterialsFolder + "/RuntimeSky2.mat";
        const string VertexLitMaterialPath = MaterialsFolder + "/RuntimeVertexLit.mat";
        const string PostMaterialPath = MaterialsFolder + "/RuntimePost.mat";
        const string IconFolder = "Assets/DerbyArena/Icons";
        const string Version = "4.0.1";
        const int VersionCode = 401;
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

        /// <summary>Like EnsureMaterial but for project shaders: never fails the build (runtime falls back).</summary>
        static void TryEnsureMaterial(string path, string shaderName, Action<Material> configure)
        {
            try
            {
                if (Shader.Find(shaderName) == null)
                {
                    Debug.LogWarning("DERBY: optional shader not found, feature falls back at runtime: " + shaderName);
                    return;
                }
                EnsureMaterial(path, shaderName, configure);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: optional material " + path + " not created: " + ex.Message);
            }
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

            // 4.0: textured decals (turret markings) use Standard "Fade" blending so transparent texels vanish completely
            EnsureMaterial(DecalMaterialPath, "Standard", m =>
            {
                m.SetOverrideTag("RenderType", "Transparent");
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 2f);
                if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            });

            EnsureMaterial(FxMaterialPath, "Sprites/Default", null);
            EnsureMaterial(FxAddMaterialPath, "Legacy Shaders/Particles/Additive", null);
            EnsureMaterial(SkyMaterialPath, "Skybox/Procedural", m =>
            {
                if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", 0.03f);
                if (m.HasProperty("_AtmosphereThickness")) m.SetFloat("_AtmosphereThickness", 1.4f);
            });

            // Project shaders (Assets/DerbyArena/Shaders). Missing or broken -> runtime fallback, never a failed build.
            TryEnsureMaterial(VertexLitMaterialPath, "DerbyArena/VertexLit", null);
            TryEnsureMaterial(PostMaterialPath, "Hidden/DerbyArena/Post", null);
            TryEnsureMaterial(Sky2MaterialPath, "DerbyArena/Sky", m =>
            {
                var clouds = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Derby/Textures/clouds.png");
                if (clouds != null && m.HasProperty("_CloudTex")) m.SetTexture("_CloudTex", clouds);
            });
        }

        // ------------------------------------------------------------------ app icons
        // Icons are assigned through reflection so this file compiles on every build target (Android, iOS,
        // Editor) and across Unity API overload changes; the outcome is written to the build log.
        // Icons are imported with default settings; no re-import is attempted during the build (Unity does
        // not allow asset imports inside a build callback, which silently skipped the icons in 1.3.0).
        static Texture2D LoadIcon(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + "/" + file);
        }

        static object TargetFor(Type parameterType, string platform)
        {
            if (parameterType == typeof(NamedBuildTarget))
            {
                if (platform == "Android") return NamedBuildTarget.Android;
                if (platform == "iOS") return NamedBuildTarget.iOS;
                return NamedBuildTarget.Unknown;
            }
            if (parameterType == typeof(BuildTargetGroup))
            {
                if (platform == "Android") return BuildTargetGroup.Android;
                if (platform == "iOS") return BuildTargetGroup.iOS;
                return BuildTargetGroup.Unknown;
            }
            return null;
        }

        static bool SetDefaultIcon(Texture2D icon)
        {
            foreach (var method in typeof(PlayerSettings).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "SetIcons") continue;
                var ps = method.GetParameters();
                if (ps.Length != 3 || ps[1].ParameterType != typeof(Texture2D[]) || !ps[2].ParameterType.IsEnum) continue;
                object target = TargetFor(ps[0].ParameterType, "");
                if (target == null) continue;
                object any = Enum.Parse(ps[2].ParameterType, "Any");
                method.Invoke(null, new object[] { target, new Texture2D[] { icon }, any });
                return true;
            }
            foreach (var method in typeof(PlayerSettings).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "SetIconsForTargetGroup") continue;
                var ps = method.GetParameters();
                if (ps.Length != 2 || ps[0].ParameterType != typeof(BuildTargetGroup) || ps[1].ParameterType != typeof(Texture2D[])) continue;
                method.Invoke(null, new object[] { BuildTargetGroup.Unknown, new Texture2D[] { icon } });
                return true;
            }
            return false;
        }

        static object StaticMember(Type holder, string name)
        {
            var prop = holder.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            if (prop != null) return prop.GetValue(null, null);
            var field = holder.GetField(name, BindingFlags.Public | BindingFlags.Static);
            return field != null ? field.GetValue(null) : null;
        }

        static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = asm.GetType(fullName, false); }
                catch (Exception) { t = null; }
                if (t != null) return t;
            }
            return null;
        }

        static int SetAndroidIconKind(Type holder, string kindName, Texture2D[] layers)
        {
            object kind = StaticMember(holder, kindName);
            if (kind == null) return 0;
            MethodInfo getter = null;
            MethodInfo setter = null;
            foreach (var method in typeof(PlayerSettings).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var ps = method.GetParameters();
                if (method.Name == "GetPlatformIcons" && ps.Length == 2 && ps[1].ParameterType.IsInstanceOfType(kind) && TargetFor(ps[0].ParameterType, "Android") != null)
                    if (getter == null || ps[0].ParameterType == typeof(NamedBuildTarget)) getter = method;
                if (method.Name == "SetPlatformIcons" && ps.Length == 3 && ps[1].ParameterType.IsInstanceOfType(kind) && TargetFor(ps[0].ParameterType, "Android") != null)
                    if (setter == null || ps[0].ParameterType == typeof(NamedBuildTarget)) setter = method;
            }
            if (getter == null || setter == null) return 0;
            var icons = getter.Invoke(null, new object[] { TargetFor(getter.GetParameters()[0].ParameterType, "Android"), kind }) as Array;
            if (icons == null || icons.Length == 0) return 0;
            foreach (object icon in icons)
            {
                var setTextures = icon.GetType().GetMethod("SetTextures");
                if (setTextures != null) setTextures.Invoke(icon, new object[] { layers });
            }
            setter.Invoke(null, new object[] { TargetFor(setter.GetParameters()[0].ParameterType, "Android"), kind, icons });
            return icons.Length;
        }

        static void ApplyIcons()
        {
            try
            {
                var legacy = LoadIcon("icon_legacy.png");
                var round = LoadIcon("icon_round.png");
                var background = LoadIcon("icon_adaptive_bg.png");
                var foreground = LoadIcon("icon_adaptive_fg.png");
                if (legacy == null)
                {
                    Debug.LogWarning("DERBY ICONS: " + IconFolder + "/icon_legacy.png not found");
                    return;
                }
                bool def = SetDefaultIcon(legacy);
                int adaptive = 0, roundCount = 0, legacyCount = 0;
                Type holder = FindType("UnityEditor.Android.AndroidPlatformIconKind");
                if (holder != null)
                {
                    if (background != null && foreground != null) adaptive = SetAndroidIconKind(holder, "Adaptive", new[] { background, foreground });
                    if (round != null) roundCount = SetAndroidIconKind(holder, "Round", new[] { round });
                    legacyCount = SetAndroidIconKind(holder, "Legacy", new[] { legacy });
                }
                Debug.Log("DERBY ICONS: default=" + def + " android adaptive=" + adaptive + " round=" + roundCount + " legacy=" + legacyCount
                          + (holder == null ? " (Android module not loaded on this build target)" : ""));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY ICONS: not applied: " + ex);
            }
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
            ApplyIcons();

            PlayerSettings.companyName = "LenstedReal";
            PlayerSettings.productName = "DERBY ARENA";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lenstedreal.derbyarena");
            PlayerSettings.Android.bundleVersionCode = VersionCode;
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

            // 4.0.1: ASTC for every imported texture (UI sprites, clouds, ruins PBR set). ETC2 stays the fallback
            // Unity picks on the rare GLES3 device without ASTC. The GLB textures are decoded at runtime from JPEG/PNG
            // and are not affected by this setting (see TECHNICAL_REPORT_4.0_TR.md, memory budget).
            try { EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC; }
            catch (Exception ex) { Debug.LogWarning("DERBY: ASTC texture subtarget not applied: " + ex.Message); }

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

            // Hardware statistics to Unity: off where the licence allows it (privacy policy states the rest).
            try
            {
                var hw = typeof(PlayerSettings).GetProperty("submitAnalytics", BindingFlags.Public | BindingFlags.Static);
                if (hw != null && hw.CanWrite) hw.SetValue(null, false, null);
            }
            catch (Exception ex) { Debug.LogWarning("DERBY: hardware statistics setting not applied: " + ex.Message); }
            // Google Play requires a recent target API; Unity picks the highest installed SDK (36 in Build Automation).
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);

            // iOS: same identity; Unity Build Automation signs with your Apple credentials.
            try
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.lenstedreal.derbyarena");
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Minimal);
                PlayerSettings.iOS.buildNumber = VersionCode.ToString();
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

            Debug.Log("DERBY BUILD SETTINGS APPLIED: BUILT-IN PIPELINE + RESOURCE MATERIALS / DERBY ARENA / " + Version + " / " + VersionCode + " / ARM64 + iOS");
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
