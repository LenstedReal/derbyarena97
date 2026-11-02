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
        GameObject bossBar, pausePage, resultPage, loadingPage, wheelGroup, arrowsGroup;
        RectTransform radar, radarSweep;
        readonly List<Image> radarDots = new List<Image>();
        Image gasImg, brakeImg, nitroImg, driftImg, fireImg, rocketImg, leftImg, rightImg;

        readonly List<Text> feed = new List<Text>();
        readonly List<float> feedTime = new List<float>();
        float hitTimer, damageFlash;
        bool paused;
        static Sprite star, arrowSprite;
        PlayerDriver driver;
        RawImage loadingArt;
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

            var vig = UiKit.Image("Vignette", canvasRt, UiKit.VignetteSprite, new Color(0f, 0f, 0f, 0.32f));
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
            var cd = UiKit.Rect("Countdown", canvasRt);
            UiKit.Stretch(cd, 0.3f, 0.45f, 0.7f, 0.75f);
            countText = UiKit.Label("Text", cd, "", 190, Color.white, TextAnchor.MiddleCenter, true);
            for (int i = 0; i < 3; i++)
            {
                var r = UiKit.Rect("Feed" + i, safe);
                UiKit.Place(r, 0.5f, 0.71f - i * 0.055f, 1100f, 56f, 0.5f, 0.5f);
                var t = UiKit.Label("Text", r, "", 40, Color.white, TextAnchor.MiddleCenter, true);
                t.color = new Color(1f, 1f, 1f, 0f);
                feed.Add(t);
                feedTime.Add(0f);
            }

            // loading overlay
            var lb = UiKit.Image("Loading", canvasRt, UiKit.Square, new Color(0.03f, 0.035f, 0.045f, 1f));
            UiKit.Stretch(lb.rectTransform, 0f, 0f, 1f, 1f);
            loadingBg = lb;
            loadingPage = lb.gameObject;
            var art = Resources.Load<Texture2D>("Derby/keyart");
            if (art != null)
            {
                var ago = new GameObject("KeyArt", typeof(RectTransform));
                ago.transform.SetParent(lb.transform, false);
                loadingArt = ago.AddComponent<RawImage>();
                loadingArt.texture = art;
                loadingArt.raycastTarget = false;
                loadingArt.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                var ar = loadingArt.rectTransform;
                ar.anchorMin = ar.anchorMax = new Vector2(0.5f, 0.5f);
                ar.pivot = new Vector2(0.5f, 0.5f);
                // square art scaled to cover the reference canvas, cropped on the long side
                ar.sizeDelta = new Vector2(2400f, 2400f);
            }
            var badge = UiKit.Rect("Kind", lb.transform);
            UiKit.Stretch(badge, 0.3f, 0.7f, 0.7f, 0.78f);
            UiKit.Label("Text", badge, LevelConfig.KindName(match.Config.Kind), 34, KindColor(match.Config.Kind), TextAnchor.MiddleCenter, true);
            var lt = UiKit.Rect("Title", lb.transform);
            UiKit.Stretch(lt, 0.1f, 0.5f, 0.9f, 0.7f);
            UiKit.Label("Text", lt, match.Config.Title, 72, UiKit.Amber, TextAnchor.MiddleCenter, true);
            var ls = UiKit.Rect("Sub", lb.transform);
            UiKit.Stretch(ls, 0.1f, 0.42f, 0.9f, 0.5f);
            UiKit.Label("Text", ls, "BÖLÜM " + match.Config.Number + "  ·  " + match.Config.Subtitle, 34, UiKit.TextDim, TextAnchor.MiddleCenter, false);
            var lo = UiKit.Rect("Objective", lb.transform);
            UiKit.Stretch(lo, 0.15f, 0.3f, 0.85f, 0.4f);
            UiKit.Label("Text", lo, match.Config.ObjectiveLine, 34, Color.white, TextAnchor.MiddleCenter, false);
            if (!string.IsNullOrEmpty(match.Config.Hint))
            {
                var lh = UiKit.Rect("Hint", lb.transform);
                UiKit.Stretch(lh, 0.15f, 0.2f, 0.85f, 0.3f);
                UiKit.Label("Text", lh, "İPUCU: " + match.Config.Hint, 28, UiKit.Cyan, TextAnchor.MiddleCenter, false);
            }
            var ll = UiKit.Rect("Status", lb.transform);
            UiKit.Stretch(ll, 0.3f, 0.08f, 0.7f, 0.15f);
            loadingText = UiKit.Label("Text", ll, "YÜKLENİYOR", 28, UiKit.TextDim, TextAnchor.MiddleCenter, false);
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
            bars.anchoredPosition = new Vector2(236f, 0f);

            var hp = UiKit.Bar(bars, UiKit.Green, out hpFill);
            UiKit.Place(hp.rectTransform, 0f, 1f, 470f, 52f, 0f, 1f);
            var sh = UiKit.Image("Shield", hp.transform, UiKit.Square, new Color(0.7f, 0.8f, 1f, 0.75f));
            sh.type = Image.Type.Filled; sh.fillMethod = Image.FillMethod.Horizontal;
            UiKit.Stretch(sh.rectTransform, 0f, 0f, 1f, 0.22f);
            shieldFill = sh;
            hpText = UiKit.Label("Text", hp.transform, "", 30, Color.white, TextAnchor.MiddleLeft, true);
            hpText.rectTransform.offsetMin = new Vector2(16f, 0f);

            var ni = UiKit.Bar(bars, UiKit.Cyan, out nitroFill);
            UiKit.Place(ni.rectTransform, 0f, 1f, 330f, 34f, 0f, 1f);
            ni.rectTransform.anchoredPosition = new Vector2(0f, -60f);
            var nl = UiKit.Label("Text", ni.transform, "NİTRO", 22, Color.white, TextAnchor.MiddleLeft, true);
            nl.rectTransform.offsetMin = new Vector2(14f, 0f);

            var he = UiKit.Bar(bars, UiKit.Amber, out heatFill);
            UiKit.Place(he.rectTransform, 0f, 1f, 330f, 34f, 0f, 1f);
            he.rectTransform.anchoredPosition = new Vector2(0f, -102f);
            heatText = UiKit.Label("Text", he.transform, "SİLAH", 22, Color.white, TextAnchor.MiddleLeft, true);
            heatText.rectTransform.offsetMin = new Vector2(14f, 0f);
        }

        void BuildTopCenter()
        {
            var tp = UiKit.Image("Timer", safe, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(tp.rectTransform, 0.5f, 1f, 220f, 84f, 0.5f, 1f);
            timeText = UiKit.Label("Text", tp.transform, "0:00", 52, Color.white, TextAnchor.MiddleCenter, true);

            var op = UiKit.Image("Objective", safe, UiKit.Rounded, new Color(0.04f, 0.05f, 0.06f, 0.6f));
            UiKit.Place(op.rectTransform, 0.5f, 1f, 820f, 60f, 0.5f, 1f);
            op.rectTransform.anchoredPosition = new Vector2(0f, -92f);
            objText = UiKit.Label("Text", op.transform, "", 28, Color.white, TextAnchor.MiddleCenter, true);

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

        Image RoundButton(string caption, float ax, float ay, float size, Color c, int font)
        {
            var img = UiKit.Image(caption, safe, UiKit.Circle, new Color(c.r, c.g, c.b, 0.5f));
            UiKit.Place(img.rectTransform, ax, ay, size, size, 0.5f, 0.5f);
            var ring = UiKit.Image("Ring", img.transform, UiKit.RingSprite, new Color(1f, 1f, 1f, 0.35f));
            UiKit.Stretch(ring.rectTransform, 0f, 0f, 1f, 1f);
            UiKit.Label("Label", img.transform, caption, font, Color.white, TextAnchor.MiddleCenter, true);
            return img;
        }

        Image Pedal(string caption, float ax, float ay, float w, float h, Color c)
        {
            var img = UiKit.Image(caption, safe, UiKit.Rounded, new Color(c.r, c.g, c.b, 0.5f));
            UiKit.Place(img.rectTransform, ax, ay, w, h, 0.5f, 0f);
            UiKit.Label("Label", img.transform, caption, 32, Color.white, TextAnchor.MiddleCenter, true);
            return img;
        }

        void BuildControls()
        {
            // steering wheel
            wheelGroup = UiKit.Rect("WheelGroup", safe).gameObject;
            var wg = wheelGroup.GetComponent<RectTransform>();
            UiKit.Place(wg, 0f, 0f, 360f, 360f, 0f, 0f);
            wg.anchoredPosition = new Vector2(30f, 20f);
            var shadow = UiKit.Image("Back", wg, UiKit.Circle, new Color(0f, 0f, 0f, 0.35f));
            UiKit.Stretch(shadow.rectTransform, 0f, 0f, 1f, 1f);
            wheelImg = UiKit.Image("Wheel", wg, UiKit.Wheel, new Color(0.92f, 0.94f, 0.98f, 0.82f));
            UiKit.Stretch(wheelImg.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
            controls.WheelRect = wg;

            // arrow buttons (alternative)
            arrowsGroup = UiKit.Rect("Arrows", safe).gameObject;
            var ag = arrowsGroup.GetComponent<RectTransform>();
            UiKit.Stretch(ag, 0f, 0f, 1f, 1f);
            leftImg = UiKit.Image("Left", ag, UiKit.Rounded, new Color(1f, 1f, 1f, 0.22f));
            UiKit.Place(leftImg.rectTransform, 0f, 0f, 200f, 200f, 0f, 0f);
            leftImg.rectTransform.anchoredPosition = new Vector2(20f, 30f);
            UiKit.Label("L", leftImg.transform, "<", 90, Color.white, TextAnchor.MiddleCenter, true);
            rightImg = UiKit.Image("Right", ag, UiKit.Rounded, new Color(1f, 1f, 1f, 0.22f));
            UiKit.Place(rightImg.rectTransform, 0f, 0f, 200f, 200f, 0f, 0f);
            rightImg.rectTransform.anchoredPosition = new Vector2(240f, 30f);
            UiKit.Label("R", rightImg.transform, ">", 90, Color.white, TextAnchor.MiddleCenter, true);
            controls.LeftRect = leftImg.rectTransform;
            controls.RightRect = rightImg.rectTransform;

            gasImg = Pedal("GAZ", 0.885f, 0.02f, 190f, 270f, new Color(0.15f, 0.7f, 0.32f));
            brakeImg = Pedal("FREN", 0.745f, 0.02f, 170f, 190f, new Color(0.75f, 0.18f, 0.14f));
            fireImg = RoundButton("ATEŞ", 0.86f, 0.58f, 230f, new Color(0.95f, 0.3f, 0.12f), 36);
            rocketImg = RoundButton("ROKET", 0.71f, 0.5f, 150f, new Color(1f, 0.55f, 0.1f), 24);
            rocketText = rocketImg.GetComponentsInChildren<Text>()[0];
            nitroImg = RoundButton("NİTRO", 0.615f, 0.27f, 140f, new Color(0.2f, 0.45f, 1f), 24);
            driftImg = RoundButton("DRİFT", 0.62f, 0.1f, 125f, new Color(0.45f, 0.47f, 0.55f), 24);

            controls.GasRect = gasImg.rectTransform;
            controls.BrakeRect = brakeImg.rectTransform;
            controls.FireRect = fireImg.rectTransform;
            controls.RocketRect = rocketImg.rectTransform;
            controls.NitroRect = nitroImg.rectTransform;
            controls.DriftRect = driftImg.rectTransform;
            ApplyControlMode();
        }

        public void ApplyControlMode()
        {
            bool wheel = Save.Data.ControlMode == 0;
            wheelGroup.SetActive(wheel);
            arrowsGroup.SetActive(!wheel);
            gasImg.gameObject.SetActive(!Save.Data.AutoGas);
        }

        static void Press(Image img, bool on, float baseAlpha)
        {
            if (img == null) return;
            var c = img.color;
            c.a = on ? 0.9f : baseAlpha;
            img.color = c;
            img.rectTransform.localScale = Vector3.one * (on ? 0.94f : 1f);
        }

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
                countText.text = "BAŞLA!";
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
            if (!done)
            {
                int dots = (int)(Time.unscaledTime * 3f) % 4;
                loadingText.text = "YÜKLENİYOR" + new string('.', dots);
                return;
            }
            var c = loadingBg.color;
            c.a -= Time.unscaledDeltaTime * 2.2f;
            loadingBg.color = c;
            foreach (var t in loadingPage.GetComponentsInChildren<Text>()) { var tc = t.color; tc.a = c.a; t.color = tc; }
            if (loadingArt != null) { var ac = loadingArt.color; ac.a = c.a; loadingArt.color = ac; }
            if (c.a <= 0f) loadingPage.SetActive(false);
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
                SetText(hpText, Mathf.CeilToInt(hp.Current) + " / " + Mathf.CeilToInt(hp.Max) + (hp.Shield > 0.5f ? "   +" + Mathf.CeilToInt(hp.Shield) + " KALKAN" : ""));
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
                heatFill.fillAmount = w.Heat;
                heatFill.color = w.Overheated ? (Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? UiKit.Red : UiKit.Amber) : Color.Lerp(UiKit.Amber, UiKit.Red, w.Heat);
                SetText(heatText, w.Overheated ? "AŞIRI ISINDI" : "SİLAH");
                SetText(rocketText, "ROKET\n" + w.Rockets);
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
                    SetText(objText, "TAHLİYE BÖLGESİNDE BEKLE");
                    SetText(progText, "TAHLİYE " + Mathf.FloorToInt(match.ExtractHold / MatchDirector.ExtractRequired * 100f) + "%");
                    progText.color = UiKit.Green;
                }
                else
                {
                    SetText(objText, "REAKTÖR HÜCRELERİNİ TOPLA");
                    SetText(progText, "HÜCRE " + match.CellsTaken + " / " + cfg.Cells);
                    progText.color = UiKit.Cyan;
                }
            }
            else if (cfg.Kind == ObjectiveKind.Demolition)
            {
                SetText(objText, "TÜM RAKİPLERİ İMHA ET");
                int alive = match.AliveEnemies();
                int pendingCount = Mathf.Max(0, match.TotalEnemies - match.Enemies.Count);
                SetText(progText, "KALAN RAKİP " + (alive + pendingCount));
                progText.color = UiKit.Amber;
            }
            else if (cfg.Kind == ObjectiveKind.Checkpoint)
            {
                SetText(objText, "KAPILARDAN SIRAYLA GEÇ");
                SetText(progText, "KAPI " + match.GatesPassed + " / " + match.Gates.Count);
                progText.color = UiKit.Amber;
            }
            else if (cfg.Kind == ObjectiveKind.Survival)
            {
                SetText(objText, "SÜRE BİTENE KADAR HAYATTA KAL");
                SetText(progText, "İMHA " + match.Kills + " / " + cfg.KillTarget);
                progText.color = match.Kills >= cfg.KillTarget ? UiKit.Green : UiKit.Cyan;
            }
            else if (cfg.Kind == ObjectiveKind.LastStand)
            {
                int aliveL = match.AliveEnemies();
                int pendingL = Mathf.Max(0, match.TotalEnemies - match.Enemies.Count);
                SetText(objText, match.PlayerOutsideZone ? "BÖLGE DIŞINDASIN! İÇERİ GİR" : "SON KALAN SEN OL");
                SetText(progText, "KALAN RAKİP " + (aliveL + pendingL));
                progText.color = UiKit.Amber;
            }
            else
            {
                SetText(objText, "PATRONU YOK ET");
                SetText(progText, "İMHA " + match.Kills);
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
                SetText(bossText, match.Config.Title.Replace("PATRON: ", ""));
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
            if (Save.Data.ControlMode == 0)
                wheelImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -controls.WheelAngle);
            else
            {
                Press(leftImg, controls.Steer < -0.3f, 0.22f);
                Press(rightImg, controls.Steer > 0.3f, 0.22f);
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

        void BuildPause()
        {
            var bg = UiKit.Image("Pause", canvasRt, UiKit.Square, new Color(0f, 0f, 0f, 0.7f));
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 1f, 1f);
            pausePage = bg.gameObject;
            var panel = UiKit.Image("Panel", bg.transform, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 760f, 760f, 0.5f, 0.5f);
            var title = UiKit.Rect("Title", panel.transform);
            UiKit.Place(title, 0.5f, 1f, 700f, 110f, 0.5f, 1f);
            UiKit.Label("Text", title, "DURAKLATILDI", 60, Color.white, TextAnchor.MiddleCenter, true);

            float y = -130f;
            AddPauseButton(panel.transform, "DEVAM ET", UiKit.Green, ref y, () => TogglePause());
            AddPauseButton(panel.transform, "YENİDEN BAŞLAT", UiKit.Amber, ref y, () => GameFlow.Play(match.Config.Number));
            AddPauseButton(panel.transform, "KONTROL: " + (Save.Data.ControlMode == 0 ? "DİREKSİYON" : "OKLAR"), UiKit.PanelLight, ref y, () =>
            {
                Save.Data.ControlMode = 1 - Save.Data.ControlMode;
                Save.Write();
                ApplyControlMode();
                Destroy(pausePage);
                BuildPause();
            });
            AddPauseButton(panel.transform, "SES: " + (Save.Data.Sound ? "AÇIK" : "KAPALI"), UiKit.PanelLight, ref y, () =>
            {
                Save.Data.Sound = !Save.Data.Sound;
                Save.Write();
                AudioHub.SetSound(Save.Data.Sound);
                Destroy(pausePage);
                BuildPause();
            });
            AddPauseButton(panel.transform, "ANA MENÜ", UiKit.Red, ref y, () => GameFlow.ToMenu("home"));
        }

        void AddPauseButton(Transform parent, string caption, Color color, ref float y, System.Action a)
        {
            var b = UiKit.Button(parent, caption, color, 36, a);
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
            var panel = UiKit.Image("Panel", bg.transform, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 900f, 820f, 0.5f, 0.5f);

            var title = UiKit.Rect("Title", panel.transform);
            UiKit.Place(title, 0.5f, 1f, 860f, 120f, 0.5f, 1f);
            UiKit.Label("Text", title, won ? "GÖREV TAMAM" : "KAYBETTİN", 72, won ? UiKit.Green : UiKit.Red, TextAnchor.MiddleCenter, true);
            var sub = UiKit.Rect("Sub", panel.transform);
            UiKit.Place(sub, 0.5f, 1f, 860f, 50f, 0.5f, 1f);
            sub.anchoredPosition = new Vector2(0f, -118f);
            UiKit.Label("Text", sub, won ? ("BÖLÜM " + m.Config.Number + " · " + m.Config.Title) : m.LoseReason, 30, UiKit.TextDim, TextAnchor.MiddleCenter, false);

            for (int i = 0; i < 3; i++)
            {
                bool on = won && i < m.Stars;
                var s = UiKit.Image("Star", panel.transform, Star(), on ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                UiKit.Place(s.rectTransform, 0.5f, 1f, i == 1 ? 150f : 120f, i == 1 ? 150f : 120f, 0.5f, 0.5f);
                s.rectTransform.anchoredPosition = new Vector2((i - 1) * 170f, i == 1 ? -255f : -245f);
            }

            string stats = "SÜRE  " + FormatTime(m.Elapsed) + "     İMHA  " + m.Kills + "\n+" + m.Reward + " HURDA";
            if (won)
            {
                bool fast = m.Config.Kind == ObjectiveKind.Survival ? m.Kills >= m.Config.KillTarget : m.Elapsed <= m.Config.ParTime;
                bool healthy = m.PlayerHealth != null && m.PlayerHealth.Fraction >= 0.5f;
                stats += "\n\n" + m.Config.StarTwoLine + ": " + (fast ? "EVET" : "HAYIR")
                       + "     Can %50 üstü: " + (healthy ? "EVET" : "HAYIR");
            }
            var st = UiKit.Rect("Stats", panel.transform);
            UiKit.Place(st, 0.5f, 1f, 820f, 200f, 0.5f, 1f);
            st.anchoredPosition = new Vector2(0f, -345f);
            UiKit.Label("Text", st, stats, 32, Color.white, TextAnchor.UpperCenter, true);

            float x = -270f;
            bool hasNext = won && m.Config.Number < Campaign.Total;
            int n = m.Config.Number;
            if (hasNext) { ResultButton(panel.transform, "SONRAKİ", UiKit.Green, x, () => GameFlow.Play(n + 1)); x += 270f; }
            ResultButton(panel.transform, "TEKRAR", UiKit.Amber, hasNext ? x : -135f, () => GameFlow.Play(n));
            x += 270f;
            ResultButton(panel.transform, "MENÜ", UiKit.PanelLight, hasNext ? x : 135f, () => GameFlow.ToMenu(won ? "levels" : "home"));
            if (!won)
            {
                var hint = UiKit.Rect("Tip", panel.transform);
                UiKit.Place(hint, 0.5f, 0f, 820f, 60f, 0.5f, 0f);
                hint.anchoredPosition = new Vector2(0f, 26f);
                UiKit.Label("Text", hint, "Garajda zırh ve motor yükselt, sonra tekrar dene.", 26, UiKit.Cyan, TextAnchor.MiddleCenter, false);
            }
        }

        void ResultButton(Transform parent, string caption, Color color, float x, System.Action a)
        {
            var b = UiKit.Button(parent, caption, color, 34, a);
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
}
