using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DerbyArena.Core
{
    public sealed class DerbyStartupDiagnostics : MonoBehaviour
    {
        private static string lastError = "";
        private static bool installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // The scene may be built with missing Graphics/Quality settings. Load the
            // pipeline from Resources and activate it before GLTFast creates materials.
            try
            {
                var pipeline = Resources.Load<UniversalRenderPipelineAsset>("Derby/BlackoutURP");
                if (pipeline != null)
                {
                    GraphicsSettings.defaultRenderPipeline = pipeline;
                    QualitySettings.renderPipeline = pipeline;
                    if (GraphicsSettings.currentRenderPipeline == null)
                        Debug.LogError("DERBY: URP asset was loaded but did not become the active render pipeline.");
                }
                else
                {
                    Debug.LogError("DERBY: Resources/Derby/BlackoutURP.asset is missing. Run the Derby repair menu and rebuild.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            if (installed) return;
            installed = true;
            var go = new GameObject("DerbyStartupDiagnostics");
            DontDestroyOnLoad(go);
            go.AddComponent<DerbyStartupDiagnostics>();
        }

        private void OnEnable() { Application.logMessageReceived += HandleLog; }
        private void OnDisable() { Application.logMessageReceived -= HandleLog; }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                lastError = condition + "\n" + stackTrace;
        }

        private void OnGUI()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (string.IsNullOrEmpty(lastError)) return;
            GUI.depth = -1000;
            float width = Mathf.Min(Screen.width - 24f, 900f);
            float height = Mathf.Min(Screen.height * 0.28f, 190f);
            GUI.color = Color.white;
            GUI.Box(new Rect(12f, 12f, width, height), "DERBY ARENA - STARTUP DIAGNOSTIC");
            GUI.Label(new Rect(24f, 42f, width - 24f, height - 52f), lastError);
#endif
        }
    }
}
