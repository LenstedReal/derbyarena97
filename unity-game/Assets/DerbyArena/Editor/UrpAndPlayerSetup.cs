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
        const string SettingsFolder = "Assets/DerbyArena/Settings";
        const string RendererPath = SettingsFolder + "/BlackoutRenderer.asset";
        const string PipelinePath = SettingsFolder + "/BlackoutURP.asset";
        const string ScenePath = "Assets/DerbyArena/Scenes/BlackoutSpillway.unity";

        static UrpAndPlayerSetup()
        {
            EditorApplication.delayCall += Setup;
        }

        [MenuItem("Derby/Setup URP And Android")]
        public static void Setup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
        }

        // Called again from the build callback so cloud builds do not depend
        // on InitializeOnLoad/delayCall having run in the editor session.
        public static void ApplyBuildSettings()
        {
            Directory.CreateDirectory(SettingsFolder);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "BlackoutRenderer";
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                pipeline.name = "BlackoutURP";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                if (rendererList.arraySize != 1)
                    rendererList.arraySize = 1;

                var rendererElement = rendererList.GetArrayElementAtIndex(0);
                if (rendererElement.objectReferenceValue != renderer)
                    rendererElement.objectReferenceValue = renderer;

                serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            PlayerSettings.companyName = "LenstedReal";
            PlayerSettings.productName = "DERBY ARENA";
            PlayerSettings.bundleVersion = "1.0.7";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.lenstedreal.derbyarena");
            PlayerSettings.Android.bundleVersionCode = 107;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
                throw new BuildFailedException("DERBY: URP pipeline was not assigned.");
            if (!File.Exists(ScenePath))
                throw new BuildFailedException("DERBY: startup scene is missing: " + ScenePath);

            Debug.Log("DERBY BUILD SETTINGS APPLIED: URP / DERBY ARENA / 1.0.7 / 107");
        }
    }

    public sealed class DerbyBuildSettingsEnforcer : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            UrpAndPlayerSetup.ApplyBuildSettings();
        }
    }

    public static class BatchValidate
    {
        public static void Run()
        {
            UrpAndPlayerSetup.ApplyBuildSettings();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/DerbyArena/Scenes/BlackoutSpillway.unity");
            var boot = Object.FindAnyObjectByType<DerbyArena.Core.DerbyBootstrap>();
            EditorApplication.Exit(boot == null || string.IsNullOrEmpty(scene.path) ? 2 : 0);
        }
    }
}
#endif
