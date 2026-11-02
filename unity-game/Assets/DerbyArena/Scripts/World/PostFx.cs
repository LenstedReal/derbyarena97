using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>
    /// Built-in pipeline post-processing: HDR bloom (High), ACES-style tone mapping, per-theme colour grading and
    /// vignette (Medium+). Uses the project shader Hidden/DerbyArena/Post through a Resources material; if the
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
            fx.bloom = quality >= 2;
            fx.levels = hdr ? 4 : 3;
            fx.Configure(theme, hdr);
            return fx;
        }

        void Configure(Atmosphere.Theme theme, bool hdr)
        {
            mat.SetFloat("_Threshold", hdr ? 1.0f : 0.82f);
            mat.SetFloat("_Knee", hdr ? 0.5f : 0.25f);
            mat.SetFloat("_BloomIntensity", hdr ? 0.9f : 0.6f);
            mat.SetColor("_BloomTint", Color.Lerp(Color.white, theme.Accent, 0.25f));
            mat.SetFloat("_Exposure", hdr ? 1.0f : 1.08f);
            mat.SetFloat("_Tonemap", hdr ? 1f : 0.5f);
            mat.SetFloat("_Contrast", theme.Contrast);
            mat.SetFloat("_Saturation", theme.Saturation);
            mat.SetColor("_Gain", theme.GradeGain);
            mat.SetColor("_Lift", theme.GradeLift);
            mat.SetFloat("_Vignette", 0.9f);
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
            if (mat != null) Destroy(mat);
        }
    }
}
