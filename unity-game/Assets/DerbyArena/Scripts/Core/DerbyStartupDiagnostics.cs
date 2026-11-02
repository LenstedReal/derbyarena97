using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace DerbyArena.Core
{
    /// <summary>
    /// Error overlay that also works in release APKs. Silent unless an error/exception is logged; then it
    /// shows the pipeline, device API, loader status and the last errors for 10 seconds (tap to hide).
    /// </summary>
    public sealed class DerbyStartupDiagnostics : MonoBehaviour
    {
        public const string Version = "1.4.0";

        static readonly Dictionary<string, string> status = new Dictionary<string, string>();
        static readonly List<string> errors = new List<string>();
        static bool installed;
        static float showUntil;

        public static void Report(string key, string value) { status[key] = value; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (installed) return;
            installed = true;
            var go = new GameObject("DerbyStartupDiagnostics");
            DontDestroyOnLoad(go);
            go.AddComponent<DerbyStartupDiagnostics>();
        }

        void OnEnable() { Application.logMessageReceived += HandleLog; }
        void OnDisable() { Application.logMessageReceived -= HandleLog; }

        void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string line = condition.Length > 170 ? condition.Substring(0, 170) : condition;
            if (errors.Count > 0 && errors[errors.Count - 1] == line) return;
            errors.Add(line);
            if (errors.Count > 4) errors.RemoveAt(0);
            showUntil = Time.realtimeSinceStartup + 10f;
        }

        void OnGUI()
        {
            if (Time.realtimeSinceStartup > showUntil) return;
            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Mathf.Min(Screen.width / scale - 20f, 760f);
            var sb = new StringBuilder(512);
            sb.Append("DERBY ARENA v").Append(Version).Append(" | ");
            sb.Append(GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.name);
            sb.Append(" | ").Append(SystemInfo.graphicsDeviceType);
            foreach (var kv in status) sb.Append('\n').Append(kv.Key).Append(": ").Append(kv.Value);
            for (int i = 0; i < errors.Count; i++) sb.Append("\nHATA: ").Append(errors[i]);
            var style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 12;
            style.wordWrap = true;
            style.normal.textColor = Color.white;
            float height = style.CalcHeight(new GUIContent(sb.ToString()), width) + 8f;
            GUI.depth = -1000;
            GUI.Box(new Rect(10f, Screen.height / scale - height - 10f, width, height), sb.ToString(), style);
        }
    }
}
