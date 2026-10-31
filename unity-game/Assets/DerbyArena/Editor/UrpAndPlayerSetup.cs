#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DerbyArena.EditorTools
{
    [InitializeOnLoad]
    public static class UrpAndPlayerSetup
    {
        const string RuntimeFolder = "Assets/Resources/Derby";
        const string MaterialsFolder = RuntimeFolder + "/Materials";
        const string RendererPath = RuntimeFolder + "/BlackoutRenderer.asset";
        const string PipelinePath = RuntimeFolder + "/BlackoutURP.asset";
        const string LitMaterialPath = MaterialsFolder + "/RuntimeLit.mat";
        const string EmissiveMaterialPath = MaterialsFolder + "/RuntimeLitEmissive.mat";
        const string UnlitMaterialPath = MaterialsFolder + "/RuntimeUnlit.mat";
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
            catch (System.Exception ex) { Debug.LogError("DERBY editor setup failed: " + ex); }
        }

        [MenuItem("Derby/Repair Project And Android Settings")]
        public static void Setup()
        {
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static UniversalRendererData EnsureRenderer()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "BlackoutRenderer";
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            return renderer;
        }

        static UniversalRenderPipelineAsset EnsurePipeline(UniversalRendererData renderer)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline != null)
            {
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                if (list == null || list.arraySize == 0 ||
                    list.GetArrayElementAtIndex(0).objectReferenceValue == null)
                {
                    AssetDatabase.DeleteAsset(PipelinePath);
                    pipeline = null;
                }
            }

            if (pipeline == null)
            {
                // URP factory initializes required pipeline resources and defaults.
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                if (pipeline == null)
                    throw new BuildFailedException("DERBY: URP factory returned null.");
                pipeline.name = "BlackoutURP";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            var serialized = new SerializedObject(pipeline);
            var rendererList = serialized.FindProperty("m_RendererDataList");
            if (rendererList == null)
                throw new BuildFailedException("DERBY: URP asset has no renderer data list.");
            if (rendererList.arraySize != 1)
                rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Conservative mobile defaults: no HDR/MSAA surprises and no render graph features.
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1f;
            return pipeline;
        }

        static Material EnsureMaterial(string path, string shaderName, bool emissive)
        {
            var shader = Shader.Find(shaderName);
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
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", Color.white);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        public static void ApplyBuildSettings()
        {
            if (!File.Exists(ScenePath))
                throw new BuildFailedException("DERBY: startup scene is missing: " + ScenePath);

            Directory.CreateDirectory(RuntimeFolder);
            Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var renderer = EnsureRenderer();
            var pipeline = EnsurePipeline(renderer);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            string[] qualityNames = QualitySettings.names;
            int previousQuality = QualitySettings.GetQualityLevel();
            if (qualityNames != null && qualityNames.Length > 0)
            {
                for (int i = 0; i < qualityNames.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
                QualitySettings.SetQualityLevel(Mathf.Clamp(previousQuality, 0, qualityNames.Length - 1), false);
            }
            else
            {
                QualitySettings.renderPipeline = pipeline;
            }

            // Shader.Find alone is not a build dependency. Keeping actual materials in
            // Resources forces the required URP shaders into the Android player.
            EnsureMaterial(LitMaterialPath, "Universal Render Pipeline/Lit", false);
            EnsureMaterial(EmissiveMaterialPath, "Universal Render Pipeline/Lit", true);
            EnsureMaterial(UnlitMaterialPath, "Universal Render Pipeline/Unlit", false);

            PlayerSettings.companyName = "LenstedReal";
            PlayerSettings.productName = "DERBY ARENA";
            PlayerSettings.bundleVersion = "1.0.9";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lenstedreal.derbyarena");
            PlayerSettings.Android.bundleVersionCode = 109;
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

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
                throw new BuildFailedException("DERBY: default URP pipeline was not assigned.");
            if (AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(EmissiveMaterialPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath) == null)
                throw new BuildFailedException("DERBY: runtime Resources materials were not generated.");

            Debug.Log("DERBY BUILD SETTINGS APPLIED: URP + RESOURCE SHADERS / DERBY ARENA / 1.0.9 / 109 / ARM64");
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
            var boot = Object.FindAnyObjectByType<DerbyArena.Core.DerbyBootstrap>();
            EditorApplication.Exit(boot == null || string.IsNullOrEmpty(scene.path) ? 2 : 0);
        }
    }
}
#endif
