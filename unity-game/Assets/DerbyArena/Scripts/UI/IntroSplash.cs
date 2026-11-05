using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// Cinematic opening (4.0.2). The heavy start-up work (arena, shader warm-up, vehicle model, menu) blocks the main
    /// thread in long frames, so anything animated during it freezes half-way. The intro therefore:
    ///   1. fades its complete picture in (black → key art, glow, status, bar) and only then tells the boot to start
    ///      (<see cref="ReadyForWork"/>), so a frozen frame is always a finished, good-looking frame;
    ///   2. keeps a slow push-in, glow pulse and embers for the frames that do run, and shows the real boot step and
    ///      progress (Boot.Progress / Boot.Status);
    ///   3. leaves through black: the picture fades out, the black holds a moment while the menu camera starts its
    ///      sweep, then the black fades away — the intro and the menu are never visible on top of each other.
    /// Cold start only; once the boot is done a tap skips the hold. Every UiButton is blocked while the intro is up.
    /// </summary>
    public sealed class IntroSplash : MonoBehaviour
    {
        public static bool Shown { get; private set; }
        public static bool Active { get; private set; }
        /// <summary>The intro picture is fully on screen: the boot may start its long, blocking steps.</summary>
        public static bool ReadyForWork { get; private set; }
        /// <summary>The intro has started to leave (it starts the menu camera's dolly itself).</summary>
        public static bool Leaving { get; private set; }

        const float FadeIn = 0.5f, MinShow = 2.4f, MaxTotal = 14f, PictureOut = 0.35f, BlackHold = 0.15f, BlackOut = 0.5f;

        CanvasGroup picture;
        Image black, glowImg;
        RectTransform artRt, bar;
        Text status, tapHint;
        Image[] embers;
        float[] emberPhase;
        float startTime, leaveTime = -1f, pausedAt = -1f;
        bool blocking, dollyStarted;

        public static void Play()
        {
            if (Shown) return;
            Shown = true;
            var tex = Resources.Load<Texture2D>("Derby/keyart");
            if (tex == null) { ReadyForWork = true; return; }
            var go = new GameObject("IntroSplash");
            DontDestroyOnLoad(go);
            go.AddComponent<IntroSplash>().Build(tex);
        }

        void Build(Texture2D tex)
        {
            Active = true;
            ReadyForWork = false;
            Leaving = false;
            startTime = Time.realtimeSinceStartup;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            UiButton.Blocked = true;
            blocking = true;

            // full-screen black under everything: what the player sees between the Unity splash and the picture,
            // and again between the picture and the menu
            black = Child("Black", transform).gameObject.AddComponent<Image>();
            black.color = Color.black;
            black.raycastTarget = true;
            Stretch(black.rectTransform);

            var pic = Child("Picture", transform);
            Stretch(pic);
            picture = pic.gameObject.AddComponent<CanvasGroup>();
            picture.alpha = 0f;

            glowImg = Child("Glow", pic).gameObject.AddComponent<Image>();
            glowImg.sprite = UiKit.Radial;
            glowImg.color = new Color(1f, 0.45f, 0.1f, 0.2f);
            glowImg.raycastTarget = false;
            Place(glowImg.rectTransform, 0.5f, 0.55f, 1500f, 1300f);

            // the key art (the game's logo picture), square, 78 % of the screen height
            artRt = Child("KeyArt", pic);
            Place(artRt, 0.5f, 0.55f, 840f, 840f);
            var art = artRt.gameObject.AddComponent<RawImage>();
            art.texture = tex;
            art.raycastTarget = false;

            embers = new Image[16];
            emberPhase = new float[embers.Length];
            for (int i = 0; i < embers.Length; i++)
            {
                var e = Child("Ember", pic).gameObject.AddComponent<Image>();
                e.sprite = UiKit.Radial;
                e.color = new Color(1f, 0.6f, 0.2f, 0f);
                e.raycastTarget = false;
                float sz = Random.Range(10f, 24f);
                Place(e.rectTransform, Random.Range(0.1f, 0.9f), Random.Range(0f, 0.9f), sz, sz);
                embers[i] = e;
                emberPhase[i] = Random.Range(0f, 10f);
            }

            // progress: thin bar + the current boot step
            var track = Child("BarTrack", pic).gameObject.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.12f);
            track.raycastTarget = false;
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0f);
            trt.sizeDelta = new Vector2(560f, 6f);
            trt.anchoredPosition = new Vector2(0f, 34f);
            var fill = new GameObject("BarFill", typeof(RectTransform)).AddComponent<Image>();
            fill.transform.SetParent(trt, false);
            fill.color = new Color(1f, 0.55f, 0.15f, 1f);
            fill.raycastTarget = false;
            bar = fill.rectTransform;
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.sizeDelta = new Vector2(0f, 0f);
            var st = Child("Status", pic);
            Place(st, 0.5f, 0f, 900f, 34f);
            st.anchoredPosition = new Vector2(0f, 60f);
            status = UiKit.Label("Text", st, Loc.T("YÜKLENİYOR", "LOADING"), 20, UiKit.TextDim, TextAnchor.MiddleCenter, false);
            var th = Child("Tap", pic);
            Place(th, 0.5f, 0f, 900f, 30f);
            th.anchoredPosition = new Vector2(0f, 92f);
            tapHint = UiKit.Label("Text", th, Loc.T("DEVAM ETMEK İÇİN DOKUN", "TAP TO CONTINUE"), 18, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleCenter, true);
        }

        static RectTransform Child(string n, Transform parent)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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
            r.anchorMin = r.anchorMax = new Vector2(ax, ay);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = Vector2.zero;
        }

        void Update()
        {
            // real time, not the frame delta: after a long blocking frame the timeline is where it should be
            float t = Time.realtimeSinceStartup - startTime;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float boot = Boot.Progress;
            bool booted = boot >= 0.999f;

            if (leaveTime < 0f)
            {
                float a = Mathf.Clamp01(t / FadeIn);
                picture.alpha = a * a * (3f - 2f * a);
                if (a >= 1f) ReadyForWork = true;
            }

            if (artRt != null) artRt.localScale = Vector3.one * (1.04f - 0.04f * Mathf.Clamp01(t / 4f));
            if (glowImg != null) glowImg.color = new Color(1f, 0.45f, 0.1f, 0.2f * (0.8f + 0.2f * Mathf.Sin(t * 2.1f)));
            if (embers != null)
            {
                for (int i = 0; i < embers.Length; i++)
                {
                    var e = embers[i];
                    if (e == null) continue;
                    float ph = emberPhase[i] + t;
                    var rt = e.rectTransform;
                    var an = rt.anchorMin;
                    an.y += dt * 0.04f;
                    an.x += Mathf.Sin(ph * 1.3f) * 0.0004f;
                    if (an.y > 1.05f) an = new Vector2(Random.Range(0.1f, 0.9f), -0.05f);
                    rt.anchorMin = rt.anchorMax = an;
                    e.color = new Color(1f, 0.6f, 0.2f, 0.5f * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(ph * 2.3f))));
                }
            }
            if (bar != null)
            {
                // follows the real boot, never moves backwards; creeps a little while a step is running
                float target = booted ? 1f : Mathf.Max(boot, Mathf.Min(0.25f, t / 10f));
                float cur = bar.sizeDelta.x / 560f;
                cur = Mathf.MoveTowards(cur, Mathf.Max(cur, target), dt * (booted ? 2.5f : 0.8f));
                bar.sizeDelta = new Vector2(560f * cur, 0f);
            }
            if (status != null)
            {
                string s = booted ? Loc.T("HAZIR", "READY") : (string.IsNullOrEmpty(Boot.Status) ? Loc.T("YÜKLENİYOR", "LOADING") : Boot.Status);
                if (status.text != s) status.text = s;
            }
            if (tapHint != null) tapHint.color = new Color(1f, 1f, 1f, booted && leaveTime < 0f ? 0.4f + 0.25f * Mathf.Sin(t * 5f) : 0f);

            // leave once the boot is done and the picture had its moment (or on a tap once booted, or the safety cap)
            var p = UnityEngine.InputSystem.Pointer.current;
            bool tapped = p != null && p.press.wasPressedThisFrame && t > 0.8f;
            if (leaveTime < 0f && ((booted && (t >= MinShow || tapped)) || t >= MaxTotal))
            {
                leaveTime = t;
                ReadyForWork = true;
                Leaving = true;
            }
            if (leaveTime < 0f) return;

            float lt = t - leaveTime;
            // 1) the picture fades to black
            picture.alpha = 1f - Mathf.Clamp01(lt / PictureOut);
            // 2) black holds while the menu camera starts its sweep, 3) the black fades away
            if (!dollyStarted && lt >= PictureOut)
            {
                dollyStarted = true;
                var cam = DerbyArena.CameraRig.ChaseCamera.Main;
                if (cam != null) cam.Dolly(3f);
            }
            float bo = Mathf.Clamp01((lt - PictureOut - BlackHold) / BlackOut);
            black.color = new Color(0f, 0f, 0f, 1f - bo * bo * (3f - 2f * bo));
            if (bo >= 0.4f) Release();
            if (bo >= 1f) { Active = false; Destroy(gameObject); }
        }

        void Release()
        {
            if (!blocking) return;
            blocking = false;
            UiButton.Blocked = false;
            if (black != null) black.raycastTarget = false;
        }

        /// <summary>Time in the background (a call during a cold start) does not count: realtimeSinceStartup keeps
        /// running while the app is paused, and the intro would otherwise leave at once on resume (4.0.2).</summary>
        void OnApplicationPause(bool paused)
        {
            if (paused) pausedAt = Time.realtimeSinceStartup;
            else if (pausedAt >= 0f) { startTime += Time.realtimeSinceStartup - pausedAt; pausedAt = -1f; }
        }

        void OnDestroy() { Release(); Active = false; Leaving = false; ReadyForWork = true; }
    }
}
