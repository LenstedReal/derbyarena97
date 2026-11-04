using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace DerbyArena.Core
{
    /// <summary>
    /// Lightweight performance probe for device tests (4.0): for the first <see cref="Seconds"/> seconds of a match it
    /// samples frame time every frame and writes one summary line per 10 s window to the console / logcat
    /// (tag "DerbyPerf"): average and 95th-percentile frame time, worst frame, frames over 33 ms (hitches), the
    /// managed GC allocations in that window and the total memory the allocator holds. No UI, no file IO, no
    /// allocations after Awake (a fixed sample ring), so it is safe to leave in release builds; the final line is
    /// what to paste into KNOWN_ISSUES / the technical report after a device run.
    /// </summary>
    public sealed class PerfLog : MonoBehaviour
    {
        public const float Seconds = 60f, Window = 10f;
        const int Cap = 2048;                               // 10 s at up to ~200 fps

        readonly float[] samples = new float[Cap];
        int count, windowIndex;
        float windowStart, elapsed, worst;
        int hitches;
        long gcStart;
        readonly StringBuilder sb = new StringBuilder(256);

        void Awake()
        {
            windowStart = Time.realtimeSinceStartup;
            gcStart = GcNow();
            Debug.Log("DerbyPerf start " + SystemInfo.deviceModel + " / " + SystemInfo.graphicsDeviceName + " / " + Screen.width + "x" + Screen.height + " / quality " + QualitySettings.GetQualityLevel());
        }

        static long GcNow()
        {
            long v = Profiler.GetMonoUsedSizeLong();
            return v > 0 ? v : System.GC.GetTotalMemory(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            if (count < Cap) samples[count++] = dt;
            if (dt > worst) worst = dt;
            if (dt > 0.0333f) hitches++;
            float now = Time.realtimeSinceStartup;
            if (now - windowStart >= Window)
            {
                Flush(now - windowStart);
                windowIndex++;
                windowStart = now;
                if (elapsed >= Seconds) enabled = false;
            }
        }

        void Flush(float span)
        {
            if (count == 0) return;
            float sum = 0f;
            for (int i = 0; i < count; i++) sum += samples[i];
            float avg = sum / count;
            // p95 by partial selection: sort a copy-free way is overkill, a simple in-place sort of the window is fine here
            System.Array.Sort(samples, 0, count);
            float p95 = samples[Mathf.Clamp(Mathf.RoundToInt(count * 0.95f), 0, count - 1)];
            long gc = GcNow();
            sb.Length = 0;
            sb.Append("DerbyPerf w").Append(windowIndex)
              .Append(" frames=").Append(count)
              .Append(" avg=").Append((avg * 1000f).ToString("F1")).Append("ms (").Append((1f / Mathf.Max(avg, 0.0001f)).ToString("F0")).Append(" fps)")
              .Append(" p95=").Append((p95 * 1000f).ToString("F1")).Append("ms")
              .Append(" worst=").Append((worst * 1000f).ToString("F0")).Append("ms")
              .Append(" hitches>33ms=").Append(hitches)
              .Append(" gcDelta=").Append(((gc - gcStart) / 1024f / 1024f).ToString("F1")).Append("MB")
              .Append(" totalAlloc=").Append((Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f).ToString("F0")).Append("MB")
              .Append(" span=").Append(span.ToString("F1")).Append("s");
            Debug.Log(sb.ToString());
            count = 0;
            worst = 0f;
            hitches = 0;
            gcStart = gc;
        }

        void OnDisable()
        {
            if (count > 0) Flush(Time.realtimeSinceStartup - windowStart);
        }
    }
}
