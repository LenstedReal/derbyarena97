using UnityEngine;

namespace DerbyArena.Core
{
    public sealed class DerbyStartupDiagnostics : MonoBehaviour
    {
        private static string lastError = "";
        private static bool installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (installed) return;
            installed = true;

            var go = new GameObject("DerbyStartupDiagnostics");
            DontDestroyOnLoad(go);
            go.AddComponent<DerbyStartupDiagnostics>();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error &&
                type != LogType.Exception &&
                type != LogType.Assert)
                return;

            lastError = condition + "\n\n" + stackTrace;
        }

        private void OnGUI()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (string.IsNullOrEmpty(lastError))
                return;

            GUI.depth = -1000;
            float width = Mathf.Max(240f, Screen.width - 24f);
            float height = Mathf.Min(Screen.height - 24f, 360f);
            GUI.color = new Color(1f, 1f, 1f, 1f);
            GUI.Box(
                new Rect(12f, 12f, width, height),
                "DERBY ARENA - DEVELOPMENT ERROR");

            GUI.Label(
                new Rect(24f, 48f, width - 24f, height - 60f),
                lastError);
#endif
        }
    }
}
