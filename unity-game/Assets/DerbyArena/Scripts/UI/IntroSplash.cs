using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// Cinematic opening (4.0): black → the DERBY ARENA key art rises out of the dark with a slow push-in, a light
    /// sweep crosses it, embers drift up, the title and subtitle slam in, then the frame holds while the menu boots
    /// behind it. The progress bar shows the real boot (Boot.Progress: arena, shader warm-up, vehicle, UI) with the
    /// current step as a caption; the fade-out starts only when the boot is done (at least ~3.2 s, at most 12 s),
    /// and hands over to the menu camera's dolly. Cold start only; a tap after the boot skips the hold. Every
    /// UiButton is blocked while the intro is up (the menu polls the pointer itself).
    /// </summary>
    public sealed class IntroSplash : MonoBehaviour
    {
        public static bool Shown { get; private set; }
        public static bool Active { get; private set; }

        const float FadeIn = 0.6f, MinHold = 2.6f, MaxTotal = 12f, FadeOut = 0.55f;

        CanvasGroup group;
        RawImage art;
        RectTransform artRt, sweep, title, subtitle, bar;
        Text status, tapHint;
        Image[] embers;
        float[] emberPhase;
        float t, outT;
        bool leaving, blocking;

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
            Active = true;
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
            UiButton.Blocked = true;
            blocking = true;

            var bg = Child("Black").gameObject.AddComponent<Image>();
            bg.color = Color.black;
            Stretch(bg.rectTransform);

            // warm glow behind the art
            var glow = Child("Glow").gameObject.AddComponent<Image>();
            glow.sprite = UiKit.Radial;
            glow.color = new Color(1f, 0.45f, 0.1f, 0f);
            glow.raycastTarget = false;
            Place(glow.rectTransform, 0.5f, 0.52f, 1500f, 1500f);
            glowImg = glow;

            // square art, 82% of the screen height, centred a little high
            artRt = Child("KeyArt");
            Place(artRt, 0.5f, 0.56f, 880f, 880f);
            art = artRt.gameObject.AddComponent<RawImage>();
            art.texture = tex;
            art.color = new Color(1f, 1f, 1f, 0f);
            art.raycastTarget = false;
            // the art is masked to a rounded card so the sweep stays inside it
            var mask = artRt.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var sw = new GameObject("Sweep", typeof(RectTransform)).AddComponent<Image>();
            sw.transform.SetParent(artRt, false);
            sw.sprite = UiKit.Radial;
            sw.color = new Color(1f, 0.95f, 0.85f, 0f);
            sw.raycastTarget = false;
            sweep = sw.rectTransform;
            sweep.sizeDelta = new Vector2(520f, 1600f);
            sweep.localRotation = Quaternion.Euler(0f, 0f, 22f);
            sweepImg = sw;

            // embers
            embers = new Image[18];
            emberPhase = new float[embers.Length];
            for (int i = 0; i < embers.Length; i++)
            {
                var e = Child("Ember").gameObject.AddComponent<Image>();
                e.sprite = UiKit.Radial;
                e.color = new Color(1f, 0.6f, 0.2f, 0f);
                e.raycastTarget = false;
                float sz = Random.Range(10f, 26f);
                Place(e.rectTransform, Random.Range(0.08f, 0.92f), Random.Range(-0.1f, 0.6f), sz, sz);
                embers[i] = e;
                emberPhase[i] = Random.Range(0f, 10f);
            }

            // title block under the art
            title = Child("Title");
            Place(title, 0.5f, 0.11f, 1200f, 92f);
            var tt = UiKit.Label("Text", title, "DERBY ARENA", 84, UiKit.Amber, TextAnchor.MiddleCenter, true);
            titleText = tt;
            subtitle = Child("Subtitle");
            Place(subtitle, 0.5f, 0.055f, 1200f, 44f);
            subText = UiKit.Label("Text", subtitle, "BLACKOUT RUN", 32, UiKit.Cyan, TextAnchor.MiddleCenter, true);

            // progress: thin bar + status caption
            var track = Child("BarTrack").gameObject.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.12f);
            track.raycastTarget = false;
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0f);
            trt.sizeDelta = new Vector2(560f, 6f);
            trt.anchoredPosition = new Vector2(0f, 24f);
            var fill = new GameObject("BarFill", typeof(RectTransform)).AddComponent<Image>();
            fill.transform.SetParent(trt, false);
            fill.color = new Color(1f, 0.55f, 0.15f, 1f);
            fill.raycastTarget = false;
            bar = fill.rectTransform;
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.sizeDelta = new Vector2(0f, 0f);
            var st = Child("Status");
            Place(st, 0.5f, 0f, 900f, 34f);
            st.anchoredPosition = new Vector2(0f, 38f);
            status = UiKit.Label("Text", st, "", 20, UiKit.TextDim, TextAnchor.MiddleCenter, false);
            var th = Child("Tap");
            Place(th, 0.5f, 0f, 900f, 30f);
            th.anchoredPosition = new Vector2(0f, 74f);
            tapHint = UiKit.Label("Text", th, Loc.T("DOKUN", "TAP"), 18, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleCenter, true);
        }

        Image glowImg, sweepImg;
        Text titleText, subText;

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

        static void Place(RectTransform r, float ax, float ay, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchorMin = r.anchorMax = new Vector2(ax, ay);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = Vector2.zero;
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
        static float Overshoot(float x) { x = Mathf.Clamp01(x); float c = 1.70158f; return 1f + (c + 1f) * Mathf.Pow(x - 1f, 3f) + c * Mathf.Pow(x - 1f, 2f); }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            t += dt;
            float boot = Boot.Progress;
            bool booted = boot >= 0.999f;

            // art: fade + slow push-in; sweep crosses once at 0.5-1.6 s
            float a = Mathf.Clamp01(t / FadeIn);
            if (art != null) art.color = new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            if (artRt != null) artRt.localScale = Vector3.one * (1.06f - 0.06f * Ease(t / 3.5f));
            if (glowImg != null) glowImg.color = new Color(1f, 0.45f, 0.1f, 0.22f * Ease(t / 1.2f) * (0.85f + 0.15f * Mathf.Sin(t * 2.1f)));
            if (sweep != null)
            {
                float sk = Mathf.Clamp01((t - 0.5f) / 1.1f);
                sweep.anchoredPosition = new Vector2(Mathf.Lerp(-900f, 900f, sk), 0f);
                sweepImg.color = new Color(1f, 0.95f, 0.85f, sk > 0f && sk < 1f ? 0.28f * Mathf.Sin(sk * Mathf.PI) : 0f);
            }
            // title slams in with an overshoot at 0.7 s, subtitle fades at 1.1 s
            float tk = Mathf.Clamp01((t - 0.7f) / 0.45f);
            if (title != null) { title.localScale = Vector3.one * (0.6f + 0.4f * Overshoot(tk)); titleText.color = new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, tk); }
            float sk2 = Mathf.Clamp01((t - 1.1f) / 0.5f);
            if (subText != null) subText.color = new Color(UiKit.Cyan.r, UiKit.Cyan.g, UiKit.Cyan.b, sk2);
            // embers drift up and flicker
            if (embers != null)
            {
                for (int i = 0; i < embers.Length; i++)
                {
                    var e = embers[i];
                    if (e == null) continue;
                    float ph = emberPhase[i] + t;
                    var rt = e.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(rt.anchorMin.x + Mathf.Sin(ph * 1.3f) * 0.0004f, rt.anchorMin.y + dt * 0.045f);
                    if (rt.anchorMin.y > 1.05f) rt.anchorMin = rt.anchorMax = new Vector2(Random.Range(0.08f, 0.92f), -0.05f);
                    float vis = Mathf.Clamp01((t - 0.4f) / 1f) * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(ph * 2.3f)));
                    e.color = new Color(1f, 0.6f, 0.2f, 0.55f * vis);
                }
            }
            // progress bar follows the real boot, never backwards, eases to the target
            if (bar != null)
            {
                float target = booted ? 1f : Mathf.Max(boot, Mathf.Min(0.3f, t / 8f));
                float cur = bar.sizeDelta.x / 560f;
                cur = Mathf.MoveTowards(cur, target, dt * (booted ? 2.5f : 0.6f));
                bar.sizeDelta = new Vector2(560f * cur, 0f);
            }
            if (status != null)
            {
                string s = booted ? Loc.T("HAZIR", "READY") : (string.IsNullOrEmpty(Boot.Status) ? Loc.T("YÜKLENİYOR", "LOADING") : Boot.Status);
                if (status.text != s) status.text = s;
            }
            if (tapHint != null) tapHint.color = new Color(1f, 1f, 1f, booted && !leaving ? 0.45f + 0.25f * Mathf.Sin(t * 5f) : 0f);

            // leave: boot done and the minimum hold passed, or the safety cap, or a tap once booted
            var p = UnityEngine.InputSystem.Pointer.current;
            bool tapped = p != null && p.press.wasPressedThisFrame && t > 0.8f;
            if (!leaving && ((booted && (t >= FadeIn + MinHold || tapped)) || t >= MaxTotal))
            {
                leaving = true;
                outT = 0f;
                var cam = DerbyArena.CameraRig.ChaseCamera.Main;
                if (cam != null) cam.Dolly(3.2f);                 // the menu camera sweeps down into its orbit as the intro fades
            }
            if (leaving)
            {
                outT += dt;
                float o = Mathf.Clamp01(outT / FadeOut);
                group.alpha = 1f - o;
                if (artRt != null) artRt.localScale = Vector3.one * (1f + 0.08f * o);
                group.blocksRaycasts = o < 0.5f;
                if (o >= 0.5f) Release();
                if (o >= 1f) { Active = false; Destroy(gameObject); }
            }
        }

        void Release()
        {
            if (!blocking) return;
            blocking = false;
            UiButton.Blocked = false;
        }

        void OnDestroy() { Release(); Active = false; }
    }
}
