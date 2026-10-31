using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace DerbyArena.Core
{
    /// <summary>
    /// Always-on startup diagnostics (also in release APKs). It shows the active render pipeline,
    /// camera state, what loaded, and the last errors, so a black screen can never fail silently again.
    /// The panel hides itself after a few seconds unless a new error arrives.
    /// </summary>
    public sealed class DerbyStartupDiagnostics : MonoBehaviour
    {
        public const string Version = "1.1.0";

        static readonly Dictionary<string, string> status = new Dictionary<string, string>();
        static readonly List<string> errors = new List<string>();
        static bool installed;
        static float showUntil;

        public static void Report(string key, string value)
        {
            status[key] = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (installed) return;
            installed = true;
            var go = new GameObject("DerbyStartupDiagnostics");
            DontDestroyOnLoad(go);
            go.AddComponent<DerbyStartupDiagnostics>();
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            showUntil = Time.realtimeSinceStartup + 25f;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            string line = condition;
            if (line.Length > 160) line = line.Substring(0, 160);
            errors.Add(line);
            if (errors.Count > 4) errors.RemoveAt(0);
            showUntil = Time.realtimeSinceStartup + 25f;
        }

        void OnGUI()
        {
            if (Time.realtimeSinceStartup > showUntil)
                return;

            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Mathf.Min(Screen.width / scale - 20f, 760f);

            var sb = new StringBuilder(512);
            sb.Append("DERBY ARENA v").Append(Version).Append("  |  ");
            sb.Append(GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.name);
            sb.Append("  |  kamera: ").Append(Camera.allCamera.Length);
            sb.Append("  |  ").Append(SystemInfo.graphicsDeviceType);
            foreach (var kv in status)
                sb.Append('\n').Append(kv.Key).Append(": ").Append(kv.Value);
            for (int i = 0; i < errors.Count; i++)
                sb.Append("\nHATA: ").Append(errors[i]);

            var style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 13;
            style.wordWrap = true;
            style.normal.textColor = Color.white;
            float height = style.CalcHeight(new GUIContent(sb.ToString()), width) + 8f;
            GUI.depth = -1000;
            GUI.Box(new Rect(10f, 10f, width, height), sb.ToString(), style);
        }
    }
}
