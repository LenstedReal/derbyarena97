using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Built-in pipeline post-processing: bloom (Medium+), ACES filmic tone mapping (HDR: High/Ultra), per-theme colour
    /// grading, vignette, film grain (High+), hit/blast feedback and a nitro chromatic split. Uses the project shader Hidden/DerbyArena/Post through a Resources material; if the
    /// shader is missing or unsupported on the device the component disables itself and the frame is untouched.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PostFx : MonoBehaviour
    {
        const int PassPrefilter = 0, PassDown = 1, PassUp = 2, PassComposite = 3, PassGrade = 4;
        const int MaxLevels = 5;

        Material mat;
        bool bloom;
        int levels = 4;
        readonly RenderTexture[] chain = new RenderTexture[MaxLevels];

        // 2.1 feedback: decaying hit / blast values and the smoothed nitro amount, pushed to the shader each frame.
        static PostFx current;
        float jolt, flash, speed;
        static readonly int IdFlash = Shader.PropertyToID("_Flash");
        static readonly int IdSpeed = Shader.PropertyToID("_Speed");

        /// <summary>The player took a hit (0..1): a short chromatic jolt (the HUD draws the red damage edges).</summary>
        public static void Hit(float strength)
        {
            if (current == null || !DerbyArena.Core.Save.Data.ScreenEffects) return;
            current.jolt = Mathf.Clamp01(Mathf.Max(current.jolt, strength));
        }

        /// <summary>An explosion near the camera: short white flash, weaker with distance.</summary>
        public static void Blast(Vector3 pos, float size)
        {
            if (current == null || !DerbyArena.Core.Save.Data.ScreenEffects) return;
            float dist = Vector3.Distance(current.transform.position, pos);
            float k = Mathf.Clamp01(1f - dist / (18f * Mathf.Max(0.5f, size))) * 0.32f * Mathf.Clamp(size, 0.5f, 1.5f);
            if (k > current.flash) current.flash = k;
        }

        void Awake() { current = this; }
        void OnEnable() { current = this; }
        void OnDisable() { if (current == this) current = null; }

        void Update()
        {
            if (mat == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            jolt = Mathf.MoveTowards(jolt, 0f, dt * 4f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 2.6f);
            float target = 0f;
            var md = DerbyArena.Mission.MatchDirector.Current;
            if (md != null && md.PlayerCar != null && md.PlayerCar.NitroActive && Time.timeScale > 0f && DerbyArena.Core.Save.Data.ScreenEffects) target = 1f;
            speed = Mathf.MoveTowards(speed, target, dt * (target > speed ? 4f : 2.5f));
            mat.SetFloat(IdFlash, flash);
            mat.SetFloat(IdSpeed, Mathf.Max(speed, jolt * 0.6f));
        }

        public static PostFx Attach(Camera cam, int quality, Atmosphere.Theme theme)
        {
            if (cam == null || quality <= 0) return null;
            var template = Resources.Load<Material>("Derby/Materials/RuntimePost");
            if (template == null || template.shader == null || !template.shader.isSupported || template.passCount < 5)
            {
                DerbyArena.Core.DerbyStartupDiagnostics.Report("Post", "kapalı (shader yok/desteklenmiyor)");
                return null;
            }
            var fx = cam.gameObject.GetComponent<PostFx>();
            if (fx == null) fx = cam.gameObject.AddComponent<PostFx>();
            fx.mat = new Material(template);
            bool hdr = quality >= 2 && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR);
            cam.allowHDR = hdr;
            fx.bloom = quality >= 1;   // 2.0: soft bloom also on Medium (3 half-res levels, no HDR)
            fx.levels = hdr ? (quality >= 3 ? 5 : 4) : 3;
            fx.Configure(theme, hdr, quality);
            current = fx;
            return fx;
        }

        void Configure(Atmosphere.Theme theme, bool hdr, int quality)
        {
            mat.SetFloat("_Aces", hdr ? (quality >= 3 ? 0.6f : 0.5f) : 0f);   // half-strength filmic curve: keeps dark ground readable
            mat.SetFloat("_Grain", quality >= 2 && DerbyArena.Core.Save.Data.ScreenEffects ? (quality >= 3 ? 0.035f : 0.025f) : 0f);
            mat.SetFloat("_Flash", 0f);
            mat.SetFloat("_Speed", 0f);
            mat.SetFloat("_Threshold", hdr ? 1.1f : 0.9f);
            mat.SetFloat("_Knee", hdr ? 0.5f : 0.3f);
            mat.SetFloat("_BloomIntensity", hdr ? 0.55f : 0.35f);
            mat.SetColor("_BloomTint", Color.Lerp(Color.white, theme.Accent, 0.2f));
            mat.SetFloat("_Exposure", 1f);
            mat.SetFloat("_Tonemap", hdr ? 1f : 0f);                    // soft shoulder as in 2.0 (ACES blends on top)
            mat.SetFloat("_Contrast", theme.Contrast);
            mat.SetFloat("_Saturation", theme.Saturation);
            mat.SetColor("_Gain", theme.GradeGain);
            mat.SetColor("_Lift", theme.GradeLift);
            mat.SetFloat("_Vignette", 0.42f);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null)
            {
                Graphics.Blit(src, dst);
                return;
            }
            if (!bloom)
            {
                Graphics.Blit(src, dst, mat, PassGrade);
                return;
            }

            int w = Mathf.Max(1, src.width / 2);
            int h = Mathf.Max(1, src.height / 2);
            RenderTextureFormat fmt = src.format;
            int used = 0;
            chain[0] = RenderTexture.GetTemporary(w, h, 0, fmt);
            Graphics.Blit(src, chain[0], mat, PassPrefilter);
            used = 1;
            for (int i = 1; i < levels; i++)
            {
                w = Mathf.Max(1, w / 2);
                h = Mathf.Max(1, h / 2);
                if (w < 8 || h < 8) break;
                chain[i] = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(chain[i - 1], chain[i], mat, PassDown);
                used = i + 1;
            }
            for (int i = used - 1; i > 0; i--)
            {
                Graphics.Blit(chain[i], chain[i - 1], mat, PassUp);
                RenderTexture.ReleaseTemporary(chain[i]);
                chain[i] = null;
            }
            mat.SetTexture("_BloomTex", chain[0]);
            Graphics.Blit(src, dst, mat, PassComposite);
            RenderTexture.ReleaseTemporary(chain[0]);
            chain[0] = null;
        }

        void OnDestroy()
        {
            if (current == this) current = null;
            if (mat != null) Destroy(mat);
        }
    }
}
