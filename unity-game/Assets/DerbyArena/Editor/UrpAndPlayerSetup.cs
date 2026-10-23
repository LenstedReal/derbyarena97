#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DerbyArena.EditorTools
{
    [InitializeOnLoad]
    public static class UrpAndPlayerSetup
    {
        static UrpAndPlayerSetup()
        {
            EditorApplication.delayCall += Setup;
        }

        [MenuItem("Derby/Setup URP And Android")]
        public static void Setup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            Directory.CreateDirectory("Assets/DerbyArena/Settings");
            var pipelinePath = "Assets/DerbyArena/Settings/BlackoutURP.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (existing == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/DerbyArena/Settings/BlackoutRenderer.asset");
                var pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                if (list != null)
                {
                    list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                existing = pipeline;
            }
            GraphicsSettings.defaultRenderPipeline = existing;
            QualitySettings.renderPipeline = existing;
            PlayerSettings.companyName = "LenstedReal";
            PlayerSettings.productName = "DERBY ARENA";
            PlayerSettings.bundleVersion = "1.0.5";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lenstedreal.derbyarena");
            PlayerSettings.Android.bundleVersionCode = 105;
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
            var scenes = new[] { new EditorBuildSettingsScene("Assets/DerbyArena/Scenes/BlackoutSpillway.unity", true) };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
        }
    }

    public static class BatchValidate
    {
        public static void Run()
        {
            UrpAndPlayerSetup.Setup();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/DerbyArena/Scenes/BlackoutSpillway.unity");
            var boot = Object.FindAnyObjectByType<DerbyArena.Core.DerbyBootstrap>();
            EditorApplication.Exit(boot == null || string.IsNullOrEmpty(scene.path) ? 2 : 0);
        }
    }
}
#endif
