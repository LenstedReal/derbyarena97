using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>
    /// Dynamic resolution / quality during a match. Measures the real frame rate in 3-second windows; when two
    /// windows in a row stay under 80% of the chosen target it steps down once: first shadows off, then the render
    /// size to 85%, then 72%. It never steps back up inside a session, so the picture does not "breathe".
    /// Disabled with Settings -> DYNAMIC RESOLUTION.
    /// </summary>
    public sealed class AdaptivePerformance : MonoBehaviour
    {
        static int step;          // persists across matches of this session
        /// <summary>Current step (0 none, 1 shadows off, 2-3 lower render scale). Re-applied by DerbyBootstrapQuality.</summary>
        public static int Step { get { return step; } }
        float window, frames;
        int slowWindows;
        float warmup = 6f;        // loading, shader warm-up and countdown are not representative

        void Update()
        {
            if (!Save.Data.DynamicResolution || step >= 3) return;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale <= 0f) return;
            if (warmup > 0f) { warmup -= dt; return; }
            window += dt;
            frames++;
            if (window < 3f) return;
            float fps = frames / window;
            window = 0f; frames = 0f;
            float target = Mathf.Max(30, Save.Data.FrameRate);
            if (fps < target * 0.8f) slowWindows++; else slowWindows = 0;
            if (slowWindows < 2) return;
            slowWindows = 0;
            step++;
            if (step == 1) QualitySettings.shadows = ShadowQuality.Disable;
            else
            {
                DerbyBootstrapQuality.DynamicScale = step == 2 ? 0.85f : 0.72f;
                DerbyBootstrapQuality.Apply();
                QualitySettings.shadows = ShadowQuality.Disable;
            }
            DerbyStartupDiagnostics.Report("Performans", "kademe " + step + " (" + Mathf.RoundToInt(fps) + " FPS)");
        }
    }
}
