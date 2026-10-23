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
            if (string.IsNullOrEmpty(lastError)) return;

            GUI.depth = -1000;

            GUI.Box(
                new Rect(12, 12, Screen.width - 24, Screen.height - 24),
                "DERBY ARENA - STARTUP ERROR");

            GUI.Label(
                new Rect(24, 48, Screen.width - 48, Screen.height - 72),
                lastError);
        }
    }
}
