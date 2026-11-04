using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// Opening screen (2.1): the DERBY ARENA key art (same art as the app icon) fades in over black with a thin
    /// loading line, holds while the menu arena builds behind it, then fades out. Cold start only, tap to skip,
    /// unscaled time, its own top-most canvas; if the art is missing it simply does nothing.
    /// </summary>
    public sealed class IntroSplash : MonoBehaviour
    {
        public static bool Shown { get; private set; }

        const float FadeIn = 0.45f, Hold = 1.4f, FadeOut = 0.45f;

        CanvasGroup group;
        RawImage art;
        RectTransform bar;
        float t;

        public static void Play()
        {
            if (Shown) return;
            Shown = true;
            var tex = Resources.Load<Texture2D>("Derby/keyart");
            if (tex == null) return;
            var go = new GameObject("IntroSplash");
            DontDestroyOnLoad(go);
            go.AddComponent<IntroSplash>().Build(tex);
        }

        void Build(Texture2D tex)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;

            var bg = Child("Black").gameObject.AddComponent<Image>();
            bg.color = Color.black;
            Stretch(bg.rectTransform);

            // square art, 86% of the screen height, centred
            var artRt = Child("KeyArt");
            artRt.anchorMin = artRt.anchorMax = artRt.pivot = new Vector2(0.5f, 0.5f);
            artRt.sizeDelta = new Vector2(930f, 930f);
            art = artRt.gameObject.AddComponent<RawImage>();
            art.texture = tex;
            art.color = new Color(1f, 1f, 1f, 0f);

            var track = Child("BarTrack").gameObject.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.12f);
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0f);
            trt.sizeDelta = new Vector2(520f, 6f);
            trt.anchoredPosition = new Vector2(0f, 46f);

            var fill = new GameObject("BarFill", typeof(RectTransform)).AddComponent<Image>();
            fill.transform.SetParent(trt, false);
            fill.color = new Color(1f, 0.55f, 0.15f, 1f);
            bar = fill.rectTransform;
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.sizeDelta = new Vector2(0f, 0f);
        }

        RectTransform Child(string n)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            t += dt;
            // tap or click skips straight to the fade-out
            var p = UnityEngine.InputSystem.Pointer.current;
            if (t > 0.3f && t < FadeIn + Hold && p != null && p.press.wasPressedThisFrame) t = FadeIn + Hold;

            float a = Mathf.Clamp01(t / FadeIn);
            if (art != null) art.color = new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            if (art != null) art.rectTransform.localScale = Vector3.one * (1.04f - 0.04f * Mathf.Clamp01(t / (FadeIn + Hold)));
            if (bar != null) bar.sizeDelta = new Vector2(520f * Mathf.Clamp01(t / (FadeIn + Hold)), 0f);

            if (t > FadeIn + Hold)
            {
                float o = Mathf.Clamp01((t - FadeIn - Hold) / FadeOut);
                group.alpha = 1f - o;
                group.blocksRaycasts = o < 0.5f;
                if (o >= 1f) Destroy(gameObject);
            }
        }
    }
}
