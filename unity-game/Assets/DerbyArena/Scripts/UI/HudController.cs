using System.Collections.Generic;
using DerbyArena.Audio;
using DerbyArena.Core;
using DerbyArena.Mission;
using DerbyArena.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>In-match HUD, pause menu and result screen. Built in code, safe-area aware.</summary>
    public sealed class HudController : MonoBehaviour
    {
        static HudController inst;

        MatchDirector match;
        TouchControls controls;
        RectTransform canvasRt, safe;

        Image hpFill, shieldFill, nitroFill, heatFill, bossFill, damageVignette, reticle, hitMark, wheelImg, loadingBg;
        Text hpText, timeText, objText, progText, countText, rocketText, heatText, bossText, loadingText, distText;
        GameObject bossBar, pausePage, resultPage, loadingPage;
        RectTransform radar, radarSweep;
        readonly List<Image> radarDots = new List<Image>();
        Image gasImg, brakeImg, nitroImg, driftImg, fireImg, rocketImg, leftImg, rightImg;

        readonly List<Text> feed = new List<Text>();
        readonly List<float> feedTime = new List<float>();
        float hitTimer, damageFlash;
        bool paused;
        GameObject revivePage;
        Image reviveFill;
        Text reviveText;
        bool adPending;
        static Sprite star, arrowSprite;
        PlayerDriver driver;
        CanvasGroup loadingGroup;
        Image loadingFill;
        float loadProgress;
        float loadFade = 1f;
        RectTransform objArrow;
        Image objArrowImg;
        const int EnemyBars = 8;
        readonly List<RectTransform> enemyBars = new List<RectTransform>();
        readonly List<Image> enemyBarFills = new List<Image>();

        static void SetText(Text t, string value)
        {
            if (t != null && t.text != value) t.text = value;
        }

        public static void HitMarker() { if (inst != null) inst.hitTimer = 0.12f; }

        public static void ShowResults(MatchDirector m) { if (inst != null) inst.BuildResults(m); }

        public static void ShowSecondChance(MatchDirector m) { if (inst != null) inst.BuildSecondChance(m); }

        public static HudController Create(MatchDirector m, TouchControls c)
        {
            var go = new GameObject("HudController");
            var h = go.AddComponent<HudController>();
            inst = h;
            h.match = m;
            h.controls = c;
            h.Build();
            m.Message += h.OnMessage;
            return h;
        }

        void OnDestroy() { if (inst == this) inst = null; }

        // ================================================================== build
        void Build()
        {
            canvasRt = UiKit.Canvas("HUD", 10, out safe);
            canvasRt.SetParent(transform, false);

            var vig = UiKit.Image("Vignette", canvasRt, UiKit.VignetteSprite, new Color(0f, 0f, 0f, 0.18f));
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 1f, 1f);
            vig.transform.SetAsFirstSibling();
            damageVignette = UiKit.Image("DamageVignette", canvasRt, UiKit.VignetteSprite, new Color(1f, 0.05f, 0.02f, 0f));
            UiKit.Stretch(damageVignette.rectTransform, 0f, 0f, 1f, 1f);

            // enemy health bars (pooled) and the off-screen objective arrow
            for (int i = 0; i < EnemyBars; i++)
            {
                Image fill;
                var bar = UiKit.Bar(canvasRt, UiKit.Red, out fill);
                bar.raycastTarget = false;
                fill.raycastTarget = false;
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bar.rectTransform.sizeDelta = new Vector2(96f, 16f);
                bar.gameObject.SetActive(false);
                enemyBars.Add(bar.rectTransform);
                enemyBarFills.Add(fill);
            }
            objArrowImg = UiKit.Image("ObjectiveArrow", canvasRt, ArrowSprite(), UiKit.Amber);
            objArrowImg.raycastTarget = false;
            objArrow = objArrowImg.rectTransform;
            objArrow.anchorMin = objArrow.anchorMax = new Vector2(0.5f, 0.5f);
            objArrow.pivot = new Vector2(0.5f, 0.5f);
            objArrow.sizeDelta = new Vector2(70f, 70f);
            objArrowImg.enabled = false;

            BuildTopLeft();
            BuildTopCenter();
            BuildControls();

            // pause button (top right)
            var pause = UiKit.Image("Pause", safe, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(pause.rectTransform, 1f, 1f, 110f, 90f, 1f, 1f);
            UiKit.Label("Icon", pause.transform, "II", 40, Color.white, TextAnchor.MiddleCenter, true);
            controls.PauseRect = pause.rectTransform;

            // reticle + hit marker
            reticle = UiKit.Image("Reticle", canvasRt, UiKit.RingSprite, new Color(1f, 0.3f, 0.2f, 0.9f));
            reticle.rectTransform.sizeDelta = new Vector2(110f, 110f);
            reticle.enabled = false;
            hitMark = UiKit.Image("Hit", reticle.transform, UiKit.Circle, new Color(1f, 1f, 1f, 0f));
            UiKit.Place(hitMark.rectTransform, 0.5f, 0.5f, 22f, 22f, 0.5f, 0.5f);

            // countdown + message feed
            // the countdown sits over the screen centre, the message feed above it (4.0: they no longer overlap)
            var cd = UiKit.Rect("Countdown", canvasRt);
            UiKit.Stretch(cd, 0.3f, 0.36f, 0.7f, 0.62f);
            countText = UiKit.Label("Text", cd, "", 190, Color.white, TextAnchor.MiddleCenter, true);
            for (int i = 0; i < 3; i++)
            {
                var r = UiKit.Rect("Feed" + i, safe);
                UiKit.Place(r, 0.5f, 0.775f - i * 0.052f, 1100f, 56f, 0.5f, 0.5f);
                var t = UiKit.Label("Text", r, "", 40, Color.white, TextAnchor.MiddleCenter, true);
                t.color = new Color(1f, 1f, 1f, 0f);
                feed.Add(t);
                feedTime.Add(0f);
            }

            // loading overlay: key art shown whole (never cropped) on the left, info + real progress on the right.
            // A CanvasGroup fades everything together and the page is gone before the countdown starts.
            var lb = UiKit.Image("Loading", canvasRt, UiKit.Square, new Color(0.035f, 0.03f, 0.028f, 1f));
            UiKit.Stretch(lb.rectTransform, 0f, 0f, 1f, 1f);
            loadingBg = lb;
            loadingPage = lb.gameObject;
            loadingGroup = loadingPage.AddComponent<CanvasGroup>();
            var art = Resources.Load<Texture2D>("Derby/keyart");
            if (art != null)
            {
                var box = UiKit.Rect("ArtBox", lb.transform);
                UiKit.Stretch(box, 0.02f, 0.06f, 0.49f, 0.94f);
                var ago = new GameObject("KeyArt", typeof(RectTransform));
                ago.transform.SetParent(box, false);
                var raw = ago.AddComponent<RawImage>();
                raw.texture = art;
                raw.raycastTarget = false;
                var fit = ago.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = (float)art.width / Mathf.Max(1, art.height);
            }
            var info = UiKit.Rect("Info", lb.transform);
            UiKit.Stretch(info, art != null ? 0.52f : 0.1f, 0.08f, art != null ? 0.97f : 0.9f, 0.92f);
            var kindR = UiKit.Rect("Kind", info);
            UiKit.Stretch(kindR, 0f, 0.84f, 1f, 0.94f);
            UiKit.Label("Text", kindR, LevelConfig.KindName(match.Config.Kind), 34, KindColor(match.Config.Kind), TextAnchor.MiddleCenter, true);
            var lt = UiKit.Rect("Title", info);
            UiKit.Stretch(lt, 0f, 0.66f, 1f, 0.84f);
            UiKit.Label("Text", lt, match.Config.Title, 64, UiKit.Amber, TextAnchor.MiddleCenter, true);
            var ls = UiKit.Rect("Sub", info);
            UiKit.Stretch(ls, 0f, 0.58f, 1f, 0.66f);
            UiKit.Label("Text", ls, Loc.T("BÖLÜM ", "LEVEL ") + match.Config.Number + "  ·  " + match.Config.Subtitle, 28, UiKit.TextDim, TextAnchor.MiddleCenter, false);
            var lo = UiKit.Rect("Objective", info);
            UiKit.Stretch(lo, 0f, 0.42f, 1f, 0.56f);
            UiKit.Label("Text", lo, match.Config.ObjectiveLine, 32, Color.white, TextAnchor.MiddleCenter, false);
            if (!string.IsNullOrEmpty(match.Config.Hint))
            {
                var lh = UiKit.Rect("Hint", info);
                UiKit.Stretch(lh, 0f, 0.24f, 1f, 0.4f);
                UiKit.Label("Text", lh, Loc.T("İPUCU: ", "TIP: ") + match.Config.Hint, 26, UiKit.Cyan, TextAnchor.MiddleCenter, false);
            }
            var barBg = UiKit.Bar(info, UiKit.Amber, out loadingFill);
            UiKit.Stretch(barBg.rectTransform, 0.1f, 0.1f, 0.9f, 0.14f);
            loadingFill.fillAmount = 0f;
            var ll = UiKit.Rect("Status", info);
            UiKit.Stretch(ll, 0f, 0.01f, 1f, 0.09f);
            loadingText = UiKit.Label("Text", ll, Loc.T("YÜKLENİYOR", "LOADING"), 26, UiKit.TextDim, TextAnchor.MiddleCenter, false);
        }

        void BuildTopLeft()
        {
            var radarBg = UiKit.Image("Radar", safe, UiKit.Circle, new Color(0.02f, 0.05f, 0.06f, 0.72f));
            UiKit.Place(radarBg.rectTransform, 0f, 1f, 220f, 220f, 0f, 1f);
            radar = radarBg.rectTransform;
            var rring = UiKit.Image("Ring", radar, UiKit.RingSprite, new Color(0.2f, 0.85f, 1f, 0.45f));
            UiKit.Stretch(rring.rectTransform, 0f, 0f, 1f, 1f);
            var rmid = UiKit.Image("Mid", radar, UiKit.RingSprite, new Color(0.2f, 0.85f, 1f, 0.18f));
            UiKit.Place(rmid.rectTransform, 0.5f, 0.5f, 110f, 110f, 0.5f, 0.5f);
            var me = UiKit.Image("Me", radar, UiKit.Circle, Color.white);
            UiKit.Place(me.rectTransform, 0.5f, 0.5f, 14f, 14f, 0.5f, 0.5f);
            var cone = UiKit.Image("Cone", radar, UiKit.Square, new Color(1f, 1f, 1f, 0.8f));
            UiKit.Place(cone.rectTransform, 0.5f, 0.5f, 4f, 22f, 0.5f, 0f);
            for (int i = 0; i < 24; i++)
            {
                var d = UiKit.Image("Dot", radar, UiKit.Circle, Color.red);
                UiKit.Place(d.rectTransform, 0.5f, 0.5f, 14f, 14f, 0.5f, 0.5f);
                d.enabled = false;
                radarDots.Add(d);
            }

            var bars = UiKit.Rect("Bars", safe);
            UiKit.Place(bars, 0f, 1f, 470f, 170f, 0f, 1f);
            bars.anchoredPosition = new Vector2(272f, 0f);

            var hp = UiKit.Bar(bars, UiKit.Green, out hpFill);
            UiKit.Place(hp.rectTransform, 0f, 1f, 470f, 52f, 0f, 1f);
            HudIcon(bars, "heart", UiKit.Green, -34f, -26f, 30f);
            HudIcon(bars, "flame", UiKit.Cyan, -30f, -77f, 24f);
            HudIcon(bars, "bullets", UiKit.Amber, -30f, -119f, 24f);
            var sh = UiKit.Image("Shield", hp.transform, UiKit.Square, new Color(0.7f, 0.8f, 1f, 0.75f));
            sh.type = Image.Type.Filled; sh.fillMethod = Image.FillMethod.Horizontal;
            UiKit.Stretch(sh.rectTransform, 0f, 0f, 1f, 0.22f);
            shieldFill = sh;
            hpText = UiKit.Label("Text", hp.transform, "", 30, Color.white, TextAnchor.MiddleLeft, true);
            hpText.rectTransform.offsetMin = new Vector2(16f, 0f);

            var ni = UiKit.Bar(bars, UiKit.Cyan, out nitroFill);
            UiKit.Place(ni.rectTransform, 0f, 1f, 330f, 34f, 0f, 1f);
            ni.rectTransform.anchoredPosition = new Vector2(0f, -60f);
            var nl = UiKit.Label("Text", ni.transform, Loc.T("NİTRO", "NITRO"), 22, Color.white, TextAnchor.MiddleLeft, true);
            nl.rectTransform.offsetMin = new Vector2(14f, 0f);

            var he = UiKit.Bar(bars, UiKit.Amber, out heatFill);
            UiKit.Place(he.rectTransform, 0f, 1f, 330f, 34f, 0f, 1f);
            he.rectTransform.anchoredPosition = new Vector2(0f, -102f);
            heatText = UiKit.Label("Text", he.transform, Fleet.Current().Tank ? Loc.T("TOP", "GUN") : Loc.T("SİLAH", "WEAPON"), 22, Color.white, TextAnchor.MiddleLeft, true);
            heatText.rectTransform.offsetMin = new Vector2(14f, 0f);
        }

        void BuildTopCenter()
        {
            var tp = UiKit.Image("Timer", safe, UiKit.Pill, new Color(0.03f, 0.035f, 0.045f, 0.82f));
            UiKit.Place(tp.rectTransform, 0.5f, 1f, 250f, 80f, 0.5f, 1f);
            HudIcon(tp.transform, "clock", UiKit.Amber, 22f, -40f, 34f);
            timeText = UiKit.Label("Text", tp.transform, "0:00", 50, Color.white, TextAnchor.MiddleCenter, true);
            timeText.rectTransform.offsetMin = new Vector2(44f, 0f);

            var op = UiKit.Image("Objective", safe, UiKit.Card, new Color(0.03f, 0.035f, 0.045f, 0.62f));
            UiKit.Place(op.rectTransform, 0.5f, 1f, 820f, 58f, 0.5f, 1f);
            op.rectTransform.anchoredPosition = new Vector2(0f, -88f);
            if (match != null) HudIcon(op.transform, MenuControllerKindIcon(match.Config.Kind), KindColor(match.Config.Kind), 18f, -29f, 30f);
            objText = UiKit.Label("Text", op.transform, "", 26, Color.white, TextAnchor.MiddleCenter, true);
            objText.rectTransform.offsetMin = new Vector2(52f, 0f);

            var pr = UiKit.Rect("Progress", safe);
            UiKit.Place(pr, 0.5f, 1f, 600f, 44f, 0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, -156f);
            progText = UiKit.Label("Text", pr, "", 30, UiKit.Cyan, TextAnchor.MiddleCenter, true);

            var dr = UiKit.Rect("Distance", safe);
            UiKit.Place(dr, 0.5f, 1f, 600f, 36f, 0.5f, 1f);
            dr.anchoredPosition = new Vector2(0f, -196f);
            distText = UiKit.Label("Text", dr, "", 24, UiKit.TextDim, TextAnchor.MiddleCenter, false);

            var bb = UiKit.Bar(safe, UiKit.Red, out bossFill);
            UiKit.Place(bb.rectTransform, 0.5f, 1f, 700f, 40f, 0.5f, 1f);
            bb.rectTransform.anchoredPosition = new Vector2(0f, -240f);
            bossText = UiKit.Label("Text", bb.transform, "", 24, Color.white, TextAnchor.MiddleCenter, true);
            bossBar = bb.gameObject;
            bossBar.SetActive(false);
        }

        static void HudIcon(Transform parent, string icon, Color c, float x, float y, float size)
        {
            if (UiKit.Icon(icon) == null) return;
            var im = UiKit.IconImage(parent, icon, size, c, 0f, 1f, 0.5f, 0.5f);
            im.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        static string MenuControllerKindIcon(ObjectiveKind k)
        {
            switch (k)
            {
                case ObjectiveKind.Reactor: return "reactor";
                case ObjectiveKind.Demolition: return "skull";
                case ObjectiveKind.Checkpoint: return "flag";
                case ObjectiveKind.Survival: return "heart";
                case ObjectiveKind.LastStand: return "crosshair";
                default: return "crown";
            }
        }

        /// <summary>
        /// Touch controls from the saved layout (ControlLayout / ControlWidgets). Rebuilt whenever the steering
        /// mode or the layout changes (pause menu, layout editor), so TouchControls always hit-tests the rects that
        /// are really on screen.
        /// </summary>
        void BuildControls()
        {
            if (controlsRoot != null) Destroy(controlsRoot);
            controlsRoot = UiKit.Rect("Controls", safe).gameObject;
            var root = controlsRoot.GetComponent<RectTransform>();
            UiKit.Stretch(root, 0f, 0f, 1f, 1f);
            controlsRoot.transform.SetAsFirstSibling();        // under the pause / result pages
            bool tank = match != null && match.Config != null && Fleet.Current().Tank;
            SteerMode mode = ControlLayout.Mode;
            for (int i = 0; i < ControlLayout.Count; i++)
            {
                var w = ControlWidgets.Build((ControlId)i, root, mode, tank);
                ControlWidgets.Layout(w, mode);
                widgets[i] = w;
            }
            var steer = widgets[(int)ControlId.Steer];
            wheelImg = steer.Wheel;
            leftImg = steer.Left;
            rightImg = steer.Right;
            padImg = steer.Pad;
            knobImg = steer.Knob;
            gasImg = widgets[(int)ControlId.Gas].Main;
            brakeImg = widgets[(int)ControlId.Brake].Main;
            fireImg = widgets[(int)ControlId.Fire].Main;
            rocketImg = widgets[(int)ControlId.Rocket].Main;
            rocketText = widgets[(int)ControlId.Rocket].Label;
            nitroImg = widgets[(int)ControlId.Nitro].Main;
            driftImg = widgets[(int)ControlId.Drift].Main;

            controls.WheelRect = mode == SteerMode.Wheel ? steer.Root : null;
            controls.LeftRect = steer.Left != null ? steer.Left.rectTransform : null;
            controls.RightRect = steer.Right != null ? steer.Right.rectTransform : null;
            controls.PadRect = mode == SteerMode.Swipe ? steer.Root : null;
            controls.GasRect = widgets[(int)ControlId.Gas].Root;
            controls.BrakeRect = widgets[(int)ControlId.Brake].Root;
            controls.FireRect = widgets[(int)ControlId.Fire].Root;
            controls.RocketRect = widgets[(int)ControlId.Rocket].Root;
            controls.NitroRect = widgets[(int)ControlId.Nitro].Root;
            controls.DriftRect = widgets[(int)ControlId.Drift].Root;
            padHome = steer.Root.anchoredPosition;
            ApplyControlMode();
        }

        GameObject controlsRoot;
        readonly ControlWidget[] widgets = new ControlWidget[ControlLayout.Count];
        Image padImg, knobImg;
        Vector2 padHome;

        public void ApplyControlMode()
        {
            if (gasImg != null) widgets[(int)ControlId.Gas].Root.gameObject.SetActive(!Save.Data.AutoGas);
        }

        /// <summary>Rebuilds the controls after the layout or steering mode changed (pause menu, layout editor).</summary>
        public void ApplyLayout()
        {
            if (controls == null || safe == null) return;
            BuildControls();
        }

        static void Press(Image img, bool on, float baseAlpha) { ControlWidgets.Press(img, on, baseAlpha); }

        // ================================================================== runtime
        void OnMessage(string text, Color color)
        {
            for (int i = feed.Count - 1; i > 0; i--)
            {
                feed[i].text = feed[i - 1].text;
                feed[i].color = feed[i - 1].color;
                feedTime[i] = feedTime[i - 1];
            }
            feed[0].text = text;
            feed[0].color = color;
            feedTime[0] = 2.6f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateLoading();
            if (match == null) return;
            if (match.Phase == MatchPhase.Loading) return;

            if (controls.PausePressed && (match.Phase == MatchPhase.Playing || match.Phase == MatchPhase.Countdown)) TogglePause();
            // Android back on the result screen -> main menu (same transition rules as the MENU button)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (resultPage != null && !adPending && kb != null && kb.escapeKey.wasPressedThisFrame) Transition(() => GameFlow.ToMenu("home"));

            if (revivePage != null)
            {
                if (match.Phase == MatchPhase.Downed)
                {
                    float f = Mathf.Clamp01(match.ReviveTimeLeft / DerbyArena.Ads.AdsConfig.ReviveWindowSeconds);
                    if (reviveFill != null) reviveFill.fillAmount = f;
                    if (reviveText != null) SetText(reviveText, Mathf.CeilToInt(Mathf.Min(match.ReviveTimeLeft, DerbyArena.Ads.AdsConfig.ReviveWindowSeconds)).ToString());
                }
                else
                {
                    Destroy(revivePage);
                    revivePage = null;
                    adPending = false;
                }
            }

            for (int i = 0; i < feed.Count; i++)
            {
                if (feedTime[i] <= 0f) continue;
                feedTime[i] -= dt;
                var c = feed[i].color;
                c.a = Mathf.Clamp01(feedTime[i] / 0.5f);
                feed[i].color = c;
            }

            if (match.Phase == MatchPhase.Countdown)
            {
                int n = Mathf.CeilToInt(match.Countdown);
                countText.text = n > 0 ? n.ToString() : "";
                float f = match.Countdown - Mathf.Floor(match.Countdown);
                countText.rectTransform.localScale = Vector3.one * (1f + f * 0.4f);
            }
            else if (match.Phase == MatchPhase.Playing && match.Elapsed < 0.8f)
            {
                countText.text = Loc.T("BAŞLA!", "GO!");
                countText.color = new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, 1f - match.Elapsed / 0.8f);
                countText.rectTransform.localScale = Vector3.one * (1f + match.Elapsed);
            }
            else countText.text = "";

            UpdateBars(dt);
            UpdateTop();
            UpdateRadar();
            UpdateReticle(dt);
            UpdateEnemyBars();
            UpdateControls();
        }
        void UpdateEnemyBars()
        {
            var cam = Camera.main;
            int used = 0;
            if (cam != null && match.Phase == MatchPhase.Playing && match.PlayerCar != null)
            {
                Vector3 me = match.PlayerCar.transform.position;
                foreach (var e in match.Enemies)
                {
                    if (used >= enemyBars.Count) break;
                    if (e == null || e.Health == null || e.Health.Dead || e.Health == match.Boss) continue;
                    Vector3 wp = e.transform.position;
                    float d = Vector3.Distance(wp, me);
                    if (d > 38f) continue;
                    Vector3 sp = cam.WorldToScreenPoint(wp + Vector3.up * 2.6f);
                    if (sp.z <= 0f) continue;
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out local);
                    var bar = enemyBars[used];
                    if (!bar.gameObject.activeSelf) bar.gameObject.SetActive(true);
                    bar.anchoredPosition = local;
                    float scale = Mathf.Clamp(14f / Mathf.Max(1f, d), 0.55f, 1.15f);
                    bar.localScale = new Vector3(scale, scale, 1f);
                    enemyBarFills[used].fillAmount = e.Health.Fraction;
                    bool elite = e.Car != null && e.Car.Spec != null && e.Car.Spec.Elite;
                    enemyBarFills[used].color = elite ? new Color(0.75f, 0.35f, 1f) : UiKit.Red;
                    used++;
                }
            }
            for (int i = used; i < enemyBars.Count; i++)
                if (enemyBars[i].gameObject.activeSelf) enemyBars[i].gameObject.SetActive(false);
        }

        void UpdateObjectiveArrow(bool has, Vector3 world)
        {
            var cam = Camera.main;
            if (!has || cam == null || match.Phase != MatchPhase.Playing)
            {
                if (objArrowImg.enabled) objArrowImg.enabled = false;
                return;
            }
            Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * 1.5f);
            bool behind = sp.z < 0f;
            float w = Screen.width, h = Screen.height;
            if (behind) { sp.x = w - sp.x; sp.y = h - sp.y; }
            bool onScreen = !behind && sp.x > w * 0.08f && sp.x < w * 0.92f && sp.y > h * 0.1f && sp.y < h * 0.85f;
            if (onScreen)
            {
                if (objArrowImg.enabled) objArrowImg.enabled = false;
                return;
            }
            Vector2 centre = new Vector2(w * 0.5f, h * 0.5f);
            Vector2 dir = new Vector2(sp.x, sp.y) - centre;
            if (dir.sqrMagnitude < 1f) dir = Vector2.down;
            dir.Normalize();
            float k = Mathf.Min((w * 0.4f) / Mathf.Max(0.001f, Mathf.Abs(dir.x)), (h * 0.34f) / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            Vector2 edge = centre + dir * k;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, edge, null, out local);
            objArrowImg.enabled = true;
            objArrow.anchoredPosition = local;
            objArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 7f);
            objArrow.localScale = new Vector3(pulse, pulse, 1f);
            objArrowImg.color = match.PlayerOutsideZone ? UiKit.Red : UiKit.Amber;
        }

        public static Color KindColor(ObjectiveKind k)
        {
            switch (k)
            {
                case ObjectiveKind.Reactor: return UiKit.Cyan;
                case ObjectiveKind.Demolition: return UiKit.Amber;
                case ObjectiveKind.Checkpoint: return new Color(1f, 0.85f, 0.3f);
                case ObjectiveKind.Survival: return UiKit.Green;
                case ObjectiveKind.LastStand: return new Color(1f, 0.4f, 0.3f);
                default: return UiKit.Red;
            }
        }

        static Sprite ArrowSprite()
        {
            if (arrowSprite != null) return arrowSprite;
            const int size = 64;
            var px = new Color32[size * size];
            Vector2 tip = new Vector2(32f, 60f), left = new Vector2(6f, 8f), right = new Vector2(58f, 8f), notch = new Vector2(32f, 22f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int q = 0; q < 4; q++)
                    {
                        var p = new Vector2(x + (q % 2) * 0.5f + 0.25f, y + (q / 2) * 0.5f + 0.25f);
                        if (InTri(p, tip, left, notch) || InTri(p, tip, notch, right)) hits++;
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 63));
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return arrowSprite;
        }

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        void UpdateLoading()
        {
            if (loadingPage == null || !loadingPage.activeSelf) return;
            bool done = match != null && match.Phase != MatchPhase.Loading;
            float target = done ? 1f : (match != null && match.Arena != null && match.Arena.Ready ? 0.75f : 0.4f);
            loadProgress = Mathf.MoveTowards(loadProgress, target, Time.unscaledDeltaTime * (done ? 3f : 0.45f));
            if (loadingFill != null) loadingFill.fillAmount = loadProgress;
            if (!done || loadProgress < 0.999f)
            {
                SetText(loadingText, Loc.T("YÜKLENİYOR  %" + Mathf.RoundToInt(loadProgress * 100f), "LOADING  " + Mathf.RoundToInt(loadProgress * 100f) + "%"));
                return;
            }
            SetText(loadingText, Loc.T("HAZIR", "READY"));
            loadFade -= Time.unscaledDeltaTime / 0.3f;
            if (loadingGroup != null) loadingGroup.alpha = Mathf.Clamp01(loadFade);
            if (loadFade <= 0f) loadingPage.SetActive(false);
        }

        void UpdateBars(float dt)
        {
            var hp = match.PlayerHealth;
            if (hp != null)
            {
                float f = hp.Fraction;
                hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, f, 1f - Mathf.Exp(-12f * dt));
                hpFill.color = f > 0.5f ? UiKit.Green : Color.Lerp(UiKit.Red, UiKit.Amber, f * 2f);
                shieldFill.fillAmount = hp.Shield / Mathf.Max(1f, hp.ShieldMax);
                SetText(hpText, Mathf.CeilToInt(hp.Current) + " / " + Mathf.CeilToInt(hp.Max) + (hp.Shield > 0.5f ? "   +" + Mathf.CeilToInt(hp.Shield) + Loc.T(" KALKAN", " SHIELD") : ""));
                if (Time.time - hp.LastHitTime < 0.05f) damageFlash = Mathf.Min(0.45f, damageFlash + 0.16f);
            }
            damageFlash = Mathf.Max(0f, damageFlash - dt * 2.2f);
            float low = hp != null && hp.Fraction < 0.25f && !hp.Dead ? 0.12f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            if (match.PlayerOutsideZone) low = Mathf.Max(low, 0.22f + 0.1f * Mathf.Sin(Time.unscaledTime * 9f));
            damageVignette.color = new Color(1f, 0.05f, 0.02f, Mathf.Max(damageFlash, low));

            if (match.PlayerCar != null)
            {
                nitroFill.fillAmount = match.PlayerCar.Nitro;
                nitroFill.color = match.PlayerCar.NitroActive ? Color.white : UiKit.Cyan;
            }
            var w = match.PlayerWeapons;
            if (w != null)
            {
                bool cannon = w.Spec != null && w.Spec.Cannon;
                heatFill.fillAmount = cannon ? 1f - w.Heat : w.Heat;
                heatFill.color = cannon ? (w.Heat <= 0.001f ? UiKit.Green : UiKit.Amber)
                    : (w.Overheated ? (Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? UiKit.Red : UiKit.Amber) : Color.Lerp(UiKit.Amber, UiKit.Red, w.Heat));
                SetText(heatText, cannon ? (w.Heat <= 0.001f ? Loc.T("TOP HAZIR", "GUN READY") : Loc.T("DOLDURULUYOR", "RELOADING"))
                    : (w.Overheated ? Loc.T("AŞIRI ISINDI", "OVERHEATED") : Loc.T("SİLAH", "WEAPON")));
                SetText(rocketText, (cannon ? Loc.T("HE MERMİ\n", "HE SHELL\n") : Loc.T("ROKET\n", "ROCKET\n")) + w.Rockets);
            }
        }

        void UpdateTop()
        {
            int t = Mathf.Max(0, Mathf.CeilToInt(match.TimeLeft));
            SetText(timeText, (t / 60) + ":" + (t % 60).ToString("00"));
            timeText.color = t <= 15 ? (Mathf.Repeat(Time.unscaledTime * 2f, 1f) > 0.5f ? UiKit.Red : Color.white) : Color.white;

            var cfg = match.Config;
            objText.color = match.PlayerOutsideZone ? UiKit.Red : Color.white;
            if (cfg.Kind == ObjectiveKind.Reactor)
            {
                if (match.ExtractionOpen)
                {
                    SetText(objText, Loc.T("TAHLİYE BÖLGESİNDE BEKLE", "HOLD IN THE EXTRACTION ZONE"));
                    SetText(progText, Loc.T("TAHLİYE ", "EXTRACTION ") + Mathf.FloorToInt(match.ExtractHold / MatchDirector.ExtractRequired * 100f) + "%");
                    progText.color = UiKit.Green;
                }
                else
                {
                    SetText(objText, Loc.T("REAKTÖR HÜCRELERİNİ TOPLA", "COLLECT THE REACTOR CELLS"));
                    SetText(progText, Loc.T("HÜCRE ", "CELLS ") + match.CellsTaken + " / " + cfg.Cells);
                    progText.color = UiKit.Cyan;
                }
            }
            else if (cfg.Kind == ObjectiveKind.Demolition)
            {
                SetText(objText, Loc.T("TÜM RAKİPLERİ İMHA ET", "DESTROY ALL RIVALS"));
                int alive = match.AliveEnemies();
                int pendingCount = Mathf.Max(0, match.TotalEnemies - match.Enemies.Count);
                SetText(progText, Loc.T("KALAN RAKİP ", "RIVALS LEFT ") + (alive + pendingCount));
                progText.color = UiKit.Amber;
            }
            else if (cfg.Kind == ObjectiveKind.Checkpoint)
            {
                SetText(objText, Loc.T("KAPILARDAN SIRAYLA GEÇ", "PASS THE GATES IN ORDER"));
                SetText(progText, Loc.T("KAPI ", "GATE ") + match.GatesPassed + " / " + match.Gates.Count);
                progText.color = UiKit.Amber;
            }
            else if (cfg.Kind == ObjectiveKind.Survival)
            {
                SetText(objText, Loc.T("SÜRE BİTENE KADAR HAYATTA KAL", "SURVIVE UNTIL TIME RUNS OUT"));
                SetText(progText, Loc.T("İMHA ", "KILLS ") + match.Kills + " / " + cfg.KillTarget);
                progText.color = match.Kills >= cfg.KillTarget ? UiKit.Green : UiKit.Cyan;
            }
            else if (cfg.Kind == ObjectiveKind.LastStand)
            {
                int aliveL = match.AliveEnemies();
                int pendingL = Mathf.Max(0, match.TotalEnemies - match.Enemies.Count);
                SetText(objText, match.PlayerOutsideZone ? Loc.T("BÖLGE DIŞINDASIN! İÇERİ GİR", "OUTSIDE THE ZONE! GET BACK IN") : Loc.T("SON KALAN SEN OL", "BE THE LAST ONE STANDING"));
                SetText(progText, Loc.T("KALAN RAKİP ", "RIVALS LEFT ") + (aliveL + pendingL));
                progText.color = UiKit.Amber;
            }
            else
            {
                SetText(objText, Loc.T("PATRONU YOK ET", "DESTROY THE BOSS"));
                SetText(progText, Loc.T("İMHA ", "KILLS ") + match.Kills);
                progText.color = UiKit.Amber;
            }

            Vector3 op = Vector3.zero;
            bool hasObjective = match.PlayerCar != null && match.ObjectivePoint(out op);
            UpdateObjectiveArrow(hasObjective, op);
            if (hasObjective)
                SetText(distText, Mathf.RoundToInt(Vector3.Distance(op, match.PlayerCar.transform.position)) + " m");
            else
                SetText(distText, "");

            if (match.Boss != null && !match.Boss.Dead)
            {
                if (!bossBar.activeSelf) bossBar.SetActive(true);
                bossFill.fillAmount = match.Boss.Fraction;
                SetText(bossText, string.IsNullOrEmpty(match.Config.BossName) ? match.Config.Title : match.Config.BossName);
            }
            else if (bossBar.activeSelf) bossBar.SetActive(false);
        }

        void UpdateRadar()
        {
            int used = 0;
            var car = match.PlayerCar;
            if (car != null)
            {
                Vector3 pp = car.transform.position;
                Quaternion inv = Quaternion.Euler(0f, -car.transform.eulerAngles.y, 0f);
                const float range = 60f;
                const float rad = 98f;
                foreach (var e in match.Enemies)
                {
                    if (e == null || e.Health == null || e.Health.Dead) continue;
                    Dot(ref used, inv * (e.transform.position - pp), range, rad, e.Health == match.Boss ? new Color(1f, 0.1f, 0.4f) : UiKit.Red, e.Health == match.Boss ? 20f : 14f);
                }
                foreach (var c in match.Cells)
                    if (c != null && !c.Taken) Dot(ref used, inv * (c.transform.position - pp), range, rad, UiKit.Cyan, 14f);
                if (match.ExtractionOpen && match.Extraction != null)
                    Dot(ref used, inv * (match.Extraction.transform.position - pp), range, rad, UiKit.Green, 20f);
                foreach (var p in match.Pickups)
                    if (p != null && p.Available) Dot(ref used, inv * (p.transform.position - pp), range, rad, new Color(1f, 1f, 1f, 0.45f), 8f);
            }
            for (int i = used; i < radarDots.Count; i++) radarDots[i].enabled = false;
        }

        void Dot(ref int used, Vector3 rel, float range, float rad, Color c, float size)
        {
            if (used >= radarDots.Count) return;
            var v = new Vector2(rel.x, rel.z) / range * rad;
            if (v.magnitude > rad) v = v.normalized * rad;
            var d = radarDots[used++];
            d.enabled = true;
            d.color = c;
            d.rectTransform.sizeDelta = new Vector2(size, size);
            d.rectTransform.anchoredPosition = v;
        }

        void UpdateReticle(float dt)
        {
            if (driver == null && match.PlayerCar != null) driver = match.PlayerCar.GetComponent<PlayerDriver>();
            var target = driver != null ? driver.LockTarget : null;
            var cam = Camera.main;
            if (target == null || cam == null || match.Phase != MatchPhase.Playing)
            {
                reticle.enabled = false;
                hitMark.enabled = false;
                return;
            }
            Vector3 sp = cam.WorldToScreenPoint(target.transform.position + Vector3.up * 1f);
            if (sp.z <= 0f) { reticle.enabled = false; hitMark.enabled = false; return; }
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out local);
            reticle.enabled = true;
            hitMark.enabled = true;
            reticle.rectTransform.anchoredPosition = local;
            reticle.rectTransform.anchorMin = reticle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            float s = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 8f);
            reticle.rectTransform.localScale = Vector3.one * s;
            hitTimer -= dt;
            hitMark.color = new Color(1f, 1f, 1f, hitTimer > 0f ? 1f : 0f);
        }

        void UpdateControls()
        {
            Press(gasImg, controls.Throttle > 0.5f, 0.5f);
            Press(brakeImg, controls.Brake > 0.5f, 0.5f);
            Press(nitroImg, controls.Nitro, 0.5f);
            Press(driftImg, controls.Handbrake, 0.5f);
            Press(fireImg, controls.Fire, 0.5f);
            Press(rocketImg, controls.Rocket, 0.5f);
            switch (ControlLayout.Mode)
            {
                case SteerMode.Arrows:
                    Press(leftImg, controls.Steer < -0.3f, 0.22f);
                    Press(rightImg, controls.Steer > 0.3f, 0.22f);
                    break;
                case SteerMode.Swipe:
                    UpdateSwipePad();
                    break;
                default:
                    if (wheelImg != null) wheelImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -controls.WheelAngle);
                    break;
            }
        }

        /// <summary>The swipe pad floats to the finger while steering and slides home when released.</summary>
        void UpdateSwipePad()
        {
            var steer = widgets[(int)ControlId.Steer];
            if (steer == null || steer.Root == null) return;
            var rt = steer.Root;
            float dt = Time.unscaledDeltaTime;
            if (controls.SwipeActive)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, controls.SwipeOrigin, null, out local))
                {
                    // the pad is anchored at its normalised home; convert the finger to an offset from that anchor
                    Vector2 anchorPx = new Vector2(safe.rect.xMin + safe.rect.width * rt.anchorMin.x, safe.rect.yMin + safe.rect.height * rt.anchorMin.y);
                    Vector2 want = local - anchorPx;
                    rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, want, 1f - Mathf.Exp(-22f * dt));
                }
                if (padImg != null) padImg.color = new Color(0f, 0f, 0f, 0.5f);
            }
            else
            {
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, padHome, 1f - Mathf.Exp(-10f * dt));
                if (padImg != null) padImg.color = new Color(0f, 0f, 0f, 0.35f);
            }
            if (knobImg != null)
            {
                float travel = rt.sizeDelta.x * 0.3f;
                knobImg.rectTransform.anchoredPosition = new Vector2(controls.Steer * travel, 0f);
                knobImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -controls.Steer * 60f);
            }
        }

        // ================================================================== pause
        void TogglePause()
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (paused) BuildPause();
            else if (pausePage != null) Destroy(pausePage);
        }

        void OnApplicationPause(bool p)
        {
            if (p && !paused && match != null && match.Phase == MatchPhase.Playing) TogglePause();
        }

        // ================================================================== second chance (rewarded ad)
        void BuildSecondChance(MatchDirector m)
        {
            if (revivePage != null) Destroy(revivePage);
            var bg = UiKit.Image("SecondChance", canvasRt, UiKit.Square, new Color(0f, 0f, 0f, 0.6f));
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 1f, 1f);
            revivePage = bg.gameObject;
            var panel = UiKit.Frame("Panel", bg.transform, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 820f, 520f, 0.5f, 0.5f);
            var t = panel.transform;
            var title = UiKit.Rect("Title", t);
            UiKit.Place(title, 0.5f, 1f, 780f, 100f, 0.5f, 1f);
            HudIcon(title, "heart", UiKit.Red, 150f, -50f, 48f);
            UiKit.Label("Text", title, Loc.T("İKİNCİ ŞANS?", "SECOND CHANCE?"), 56, UiKit.Amber, TextAnchor.MiddleCenter, true);
            var body = UiKit.Rect("Body", t);
            UiKit.Place(body, 0.5f, 1f, 760f, 90f, 0.5f, 1f);
            body.anchoredPosition = new Vector2(0f, -100f);
            UiKit.Label("Text", body, Loc.T("Kısa bir reklam izle: aracın onarılır, kalkan ve nitro dolar, 3 saniye hasar almazsın. Her maçta bir kez.",
                                            "Watch a short ad: your car is repaired with a shield and full nitro, and takes no damage for 3 seconds. Once per match."),
                26, Color.white, TextAnchor.MiddleCenter, false);
            Image fill;
            var bar = UiKit.Bar(t, UiKit.Red, out fill);
            UiKit.Place(bar.rectTransform, 0.5f, 1f, 700f, 30f, 0.5f, 1f);
            bar.rectTransform.anchoredPosition = new Vector2(0f, -205f);
            reviveFill = fill;
            var count = UiKit.Rect("Count", t);
            UiKit.Place(count, 0.5f, 1f, 200f, 70f, 0.5f, 1f);
            count.anchoredPosition = new Vector2(0f, -240f);
            reviveText = UiKit.Label("Text", count, "", 54, Color.white, TextAnchor.MiddleCenter, true);

            var yes = UiKit.Button(t, Loc.T("REKLAM İZLE · DEVAM ET", "WATCH AD · CONTINUE"), UiKit.Green, 30, () =>
            {
                if (adPending) return;
                adPending = true;
                m.AcceptSecondChance();
            }, true, "ad");
            yes.IgnoreBlock = true;
            UiKit.Place(yes.Background.rectTransform, 0.5f, 0f, 520f, 100f, 0.5f, 0f);
            yes.Background.rectTransform.anchoredPosition = new Vector2(-120f, 30f);
            var no = UiKit.Button(t, Loc.T("VAZGEÇ", "GIVE UP"), UiKit.PanelLight, 30, () =>
            {
                if (adPending) return;
                m.DeclineSecondChance();
            });
            no.IgnoreBlock = true;
            UiKit.Place(no.Background.rectTransform, 0.5f, 0f, 220f, 100f, 0.5f, 0f);
            no.Background.rectTransform.anchoredPosition = new Vector2(270f, 30f);
        }

        /// <summary>Runs a screen change; when an interstitial is due it plays first and the change follows when it closes.</summary>
        void Transition(System.Action go)
        {
            if (adPending) return;
            var ads = DerbyArena.Ads.AdsManager.Instance;
            if (ads != null && interstitialDue)
            {
                adPending = true;
                interstitialDue = false;
                ads.ShowInterstitial(() => { adPending = false; go(); });
                return;
            }
            go();
        }
        bool interstitialDue;

        void BuildPause()
        {
            var bg = UiKit.Image("Pause", canvasRt, UiKit.Square, new Color(0f, 0f, 0f, 0.7f));
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 1f, 1f);
            pausePage = bg.gameObject;
            var panel = UiKit.Frame("Panel", bg.transform, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 760f, 1000f, 0.5f, 0.5f);
            var title = UiKit.Rect("Title", panel.transform);
            UiKit.Place(title, 0.5f, 1f, 700f, 110f, 0.5f, 1f);
            HudIcon(title, "pause", UiKit.Amber, 120f, -55f, 48f);
            UiKit.Label("Text", title, Loc.T("DURAKLATILDI", "PAUSED"), 56, Color.white, TextAnchor.MiddleCenter, true);

            float y = -130f;
            AddPauseButton(panel.transform, Loc.T("DEVAM ET", "RESUME"), UiKit.Green, ref y, () => TogglePause());
            AddPauseButton(panel.transform, Loc.T("YENİDEN BAŞLAT", "RESTART"), UiKit.Amber, ref y, () => GameFlow.Play(match.Config.Number));
            AddPauseButton(panel.transform, Loc.T("DİREKSİYON: ", "STEERING: ") + ControlLayout.ModeName(ControlLayout.Mode), UiKit.PanelLight, ref y, () =>
            {
                Save.Data.ControlMode = (Save.Data.ControlMode + 1) % 4;
                Save.Write();
                ApplyLayout();
                Destroy(pausePage);
                BuildPause();
            });
            AddPauseButton(panel.transform, Loc.T("KONTROL DÜZENİ (SÜRÜKLE)", "CONTROL LAYOUT (DRAG)"), UiKit.PanelLight, ref y, () =>
            {
                if (pausePage != null) pausePage.SetActive(false);
                ControlLayoutEditor.Open(match != null && match.Config != null && Fleet.Current().Tank, () =>
                {
                    if (this == null) return;
                    ApplyLayout();
                    if (pausePage != null) { Destroy(pausePage); BuildPause(); }
                });
            });
            AddPauseButton(panel.transform, Loc.T("SES: ", "SOUND: ") + (Save.Data.Sound ? Loc.T("AÇIK", "ON") : Loc.T("KAPALI", "OFF")), UiKit.PanelLight, ref y, () =>
            {
                Save.Data.Sound = !Save.Data.Sound;
                Save.Write();
                AudioHub.SetSound(Save.Data.Sound);
                Destroy(pausePage);
                BuildPause();
            });
            AddPauseButton(panel.transform, Loc.T("PARLAKLIK: %" + Mathf.RoundToInt(Save.Data.Brightness * 100f), "BRIGHTNESS: " + Mathf.RoundToInt(Save.Data.Brightness * 100f) + "%"), UiKit.PanelLight, ref y, () =>
            {
                float nb = Save.Data.Brightness + 0.1f;
                Save.Data.Brightness = nb > 1.45f ? 0.8f : nb;
                Save.Write();
                DerbyArena.World.Atmosphere.ApplyBrightness();
                Destroy(pausePage);
                BuildPause();
            });
            AddPauseButton(panel.transform, Loc.T("ANA MENÜ", "MAIN MENU"), UiKit.Red, ref y, () => GameFlow.ToMenu("home"));
        }

        void AddPauseButton(Transform parent, string caption, Color color, ref float y, System.Action a)
        {
            var b = UiKit.Button(parent, caption, color, 34, a, color == UiKit.Green || color == UiKit.Amber, null);
            b.IgnoreBlock = true;
            UiKit.Place(b.Background.rectTransform, 0.5f, 1f, 560f, 96f, 0.5f, 1f);
            b.Background.rectTransform.anchoredPosition = new Vector2(0f, y);
            y -= 118f;
        }

        // ================================================================== results
        static Sprite Star()
        {
            if (star != null) return star;
            const int size = 128;
            var px = new Color32[size * size];
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 60f : 25f;
                pts[i] = new Vector2(64f + Mathf.Cos(a) * r, 64f + Mathf.Sin(a) * r);
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int s = 0; s < 4; s++)
                    {
                        var p = new Vector2(x + (s % 2) * 0.5f + 0.25f, y + (s / 2) * 0.5f + 0.25f);
                        bool inside = false;
                        for (int i = 0, j = 9; i < 10; j = i++)
                            if ((pts[i].y > p.y) != (pts[j].y > p.y) && p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                                inside = !inside;
                        if (inside) hits++;
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 63));
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            star = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return star;
        }

        void BuildResults(MatchDirector m)
        {
            if (paused) TogglePause();
            bool won = m.Phase == MatchPhase.Won;
            var bg = UiKit.Image("Result", canvasRt, UiKit.Square, new Color(0f, 0f, 0f, 0.72f));
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 1f, 1f);
            resultPage = bg.gameObject;
            var panel = UiKit.Frame("Panel", bg.transform, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 900f, 820f, 0.5f, 0.5f);
            if (UiKit.Hazard != null)
            {
                var hz = UiKit.Image("Hazard", panel.transform, UiKit.Hazard, new Color(won ? 0.25f : 0.95f, won ? 0.88f : 0.22f, won ? 0.45f : 0.18f, 0.7f));
                hz.type = Image.Type.Tiled;
                UiKit.Place(hz.rectTransform, 0.5f, 1f, 860f, 8f, 0.5f, 1f);
                hz.rectTransform.anchoredPosition = new Vector2(0f, -112f);
            }

            var title = UiKit.Rect("Title", panel.transform);
            UiKit.Place(title, 0.5f, 1f, 860f, 120f, 0.5f, 1f);
            UiKit.Label("Text", title, won ? Loc.T("GÖREV TAMAM", "MISSION COMPLETE") : Loc.T("KAYBETTİN", "YOU LOST"), 68, won ? UiKit.Green : UiKit.Red, TextAnchor.MiddleCenter, true);
            var sub = UiKit.Rect("Sub", panel.transform);
            UiKit.Place(sub, 0.5f, 1f, 860f, 50f, 0.5f, 1f);
            sub.anchoredPosition = new Vector2(0f, -118f);
            UiKit.Label("Text", sub, won ? (Loc.T("BÖLÜM ", "LEVEL ") + m.Config.Number + " · " + m.Config.Title) : m.LoseReason, 30, UiKit.TextDim, TextAnchor.MiddleCenter, false);

            for (int i = 0; i < 3; i++)
            {
                bool on = won && i < m.Stars;
                var s = UiKit.Image("Star", panel.transform, UiKit.Icon("star") ?? Star(), on ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                UiKit.Place(s.rectTransform, 0.5f, 1f, i == 1 ? 150f : 120f, i == 1 ? 150f : 120f, 0.5f, 0.5f);
                s.rectTransform.anchoredPosition = new Vector2((i - 1) * 170f, i == 1 ? -255f : -245f);
                if (on) s.gameObject.AddComponent<StarPop>().Delay = 0.25f + i * 0.3f;
            }

            string stats = Loc.T("SÜRE  ", "TIME  ") + FormatTime(m.Elapsed) + Loc.T("     İMHA  ", "     KILLS  ") + m.Kills + "\n+" + m.Reward + Loc.T(" HURDA", " SCRAP");
            if (won)
            {
                bool fast = m.Config.Kind == ObjectiveKind.Survival ? m.Kills >= m.Config.KillTarget : m.Elapsed <= m.Config.ParTime;
                bool healthy = m.PlayerHealth != null && m.PlayerHealth.Fraction >= 0.5f;
                stats += "\n\n" + m.Config.StarTwoLine + ": " + (fast ? Loc.T("EVET", "YES") : Loc.T("HAYIR", "NO"))
                       + Loc.T("     Can %50 üstü: ", "     Health above 50%: ") + (healthy ? Loc.T("EVET", "YES") : Loc.T("HAYIR", "NO"));
            }
            var st = UiKit.Rect("Stats", panel.transform);
            UiKit.Place(st, 0.5f, 1f, 820f, 200f, 0.5f, 1f);
            st.anchoredPosition = new Vector2(0f, -345f);
            UiKit.Label("Text", st, stats, 32, Color.white, TextAnchor.UpperCenter, true);

            float x = -270f;
            bool hasNext = won && m.Config.Number < Campaign.Total;
            int n = m.Config.Number;
            var ads = DerbyArena.Ads.AdsManager.Instance;
            // 4.0 ad placement: a full-screen ad only when leaving the result screen, never after a rewarded ad in the same match
            interstitialDue = ads != null && !m.RewardedShown && ads.MatchFinished(n);
            bool nextOpen = hasNext && Campaign.IsOpen(n + 1);
            if (hasNext && !nextOpen)
            {
                // star gate: the next level needs more stars, say so instead of a dead NEXT button
                var gate = UiKit.Rect("Gate", panel.transform);
                UiKit.Place(gate, 0.5f, 0f, 820f, 44f, 0.5f, 0f);
                gate.anchoredPosition = new Vector2(0f, 214f);
                UiKit.Label("Text", gate, Loc.T("Sıradaki bölüm için ★ ", "Next level needs ★ ") + Campaign.StarsRequired(n + 1) + Loc.T(" gerek (sende ", " (you have ") + Save.TotalStars() + ")", 24, UiKit.Amber, TextAnchor.MiddleCenter, true);
            }
            if (nextOpen) { ResultButton(panel.transform, Loc.T("SONRAKİ", "NEXT"), UiKit.Green, x, () => Transition(() => GameFlow.Play(n + 1)), "play"); x += 270f; }
            ResultButton(panel.transform, Loc.T("TEKRAR", "RETRY"), UiKit.Amber, nextOpen ? x : -135f, () => Transition(() => GameFlow.Play(n)), "flag");
            x += 270f;
            ResultButton(panel.transform, Loc.T("MENÜ", "MENU"), UiKit.PanelLight, nextOpen ? x : 135f, () => Transition(() => GameFlow.ToMenu(won ? "levels" : "home")), "back");

            // rewarded: double the match scrap (only when an ad is loaded, so the button never lies)
            if (won && m.Reward > 0 && ads != null && ads.RewardedReady && !m.RewardDoubled)
            {
                var dbl = UiKit.Button(panel.transform, Loc.T("REKLAM İZLE · HURDAYI 2 KATINA ÇIKAR (+" + m.Reward + ")", "WATCH AD · DOUBLE THE SCRAP (+" + m.Reward + ")"), new Color(0.2f, 0.45f, 1f, 0.95f), 24, null, false, "ad");
                dbl.IgnoreBlock = true;
                UiKit.Place(dbl.Background.rectTransform, 0.5f, 0f, 800f, 70f, 0.5f, 0f);
                dbl.Background.rectTransform.anchoredPosition = new Vector2(0f, 224f);
                dbl.OnTap = () =>
                {
                    if (adPending) return;
                    adPending = true;
                    dbl.Interactable = false;
                    ads.ShowRewarded(earned =>
                    {
                        adPending = false;
                        if (this == null || dbl == null) return;
                        if (earned)
                        {
                            m.DoubleReward();
                            dbl.Caption.text = Loc.T("HURDA 2 KATINA ÇIKTI: +", "SCRAP DOUBLED: +") + m.Reward;
                            dbl.SetColor(UiKit.Green);
                        }
                        else
                        {
                            dbl.Caption.text = Loc.T("REKLAM GÖSTERİLEMEDİ", "AD NOT AVAILABLE");
                            dbl.SetColor(UiKit.PanelLight);
                        }
                    });
                };
            }
            if (won) MaybeAskForRating(bg.transform);
            if (!won || n >= Campaign.Total)
            {
                var hint = UiKit.Rect("Tip", panel.transform);
                UiKit.Place(hint, 0.5f, 0f, 820f, 60f, 0.5f, 0f);
                hint.anchoredPosition = new Vector2(0f, 26f);
                string tip = won
                    ? Loc.T("TÜM BÖLÜMLER TAMAM! Yeni sezon bir sonraki güncellemede geliyor.", "ALL LEVELS CLEARED! A new season is coming in the next update.")
                    : (m.Assist < 3
                        ? Loc.T("Bir sonraki denemede rakipler biraz zayıflar. Garajda da yükseltme yapabilirsin.", "Rivals get a little weaker on your next try. You can also upgrade in the garage.")
                        : Loc.T("Garajda zırh ve motor yükselt, sonra tekrar dene.", "Upgrade armor and engine in the garage, then try again."));
                UiKit.Label("Text", hint, tip, 26, won ? UiKit.Amber : UiKit.Cyan, TextAnchor.MiddleCenter, false);
            }
        }

        // ================================================================== rate prompt (opens the store page)
        void MaybeAskForRating(Transform parent)
        {
            if (Save.Data.RateState != 0 || Save.Data.Wins < Save.Data.RateAskAtWin) return;
            Save.Data.RateAskAtWin = Save.Data.Wins + 8;   // ask again after eight more wins unless answered
            Save.Write();
            var box = UiKit.Image("Rate", parent, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(box.rectTransform, 0.5f, 0f, 900f, 120f, 0.5f, 0f);
            box.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            var q = UiKit.Rect("Q", box.transform);
            UiKit.Place(q, 0f, 0.5f, 420f, 110f, 0f, 0.5f);
            q.anchoredPosition = new Vector2(20f, 0f);
            UiKit.Label("Text", q, Loc.T("Oyunu beğendin mi? Google Play'de puan verirsen çok seviniriz.", "Enjoying the game? A rating on Google Play would mean a lot."), 24, Color.white, TextAnchor.MiddleLeft, false);
            var yes = UiKit.Button(box.transform, Loc.T("PUAN VER", "RATE"), UiKit.Amber, 26, () => { Save.Data.RateState = 1; Save.Write(); MenuController.OpenStorePage(); Destroy(box.gameObject); });
            yes.IgnoreBlock = true;
            UiKit.Place(yes.Background.rectTransform, 1f, 0.5f, 170f, 80f, 1f, 0.5f);
            yes.Background.rectTransform.anchoredPosition = new Vector2(-280f, 0f);
            var later = UiKit.Button(box.transform, Loc.T("SONRA", "LATER"), UiKit.PanelLight, 24, () => Destroy(box.gameObject));
            later.IgnoreBlock = true;
            UiKit.Place(later.Background.rectTransform, 1f, 0.5f, 120f, 80f, 1f, 0.5f);
            later.Background.rectTransform.anchoredPosition = new Vector2(-150f, 0f);
            var never = UiKit.Button(box.transform, Loc.T("SORMA", "NO THANKS"), UiKit.PanelLight, 22, () => { Save.Data.RateState = 2; Save.Write(); Destroy(box.gameObject); });
            never.IgnoreBlock = true;
            UiKit.Place(never.Background.rectTransform, 1f, 0.5f, 130f, 80f, 1f, 0.5f);
            never.Background.rectTransform.anchoredPosition = new Vector2(-15f, 0f);
        }

        void ResultButton(Transform parent, string caption, Color color, float x, System.Action a, string icon)
        {
            var b = UiKit.Button(parent, caption, color, 30, a, color == UiKit.Green || color == UiKit.Amber, icon);
            b.IgnoreBlock = true;
            UiKit.Place(b.Background.rectTransform, 0.5f, 0f, 250f, 100f, 0.5f, 0f);
            b.Background.rectTransform.anchoredPosition = new Vector2(x, 110f);
        }

        static string FormatTime(float s)
        {
            int t = Mathf.Max(0, Mathf.RoundToInt(s));
            return (t / 60) + ":" + (t % 60).ToString("00");
        }
    }

    /// <summary>Result-screen star: pops in with an overshoot after a delay (unscaled time).</summary>
    public sealed class StarPop : MonoBehaviour
    {
        public float Delay;
        float t;
        void Start() { transform.localScale = Vector3.zero; }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((t - Delay) / 0.35f);
            float sc = k < 1f ? 1.25f * Mathf.Sin(k * Mathf.PI * 0.5f) - 0.25f * k * k : 1f;
            transform.localScale = Vector3.one * Mathf.Max(0f, sc);
            if (k >= 1f) enabled = false;
        }
    }
}
