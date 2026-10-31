using System;
using DerbyArena.Mission;
using DerbyArena.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// Mobile HUD built entirely in code: health / nitro bars, timer, cell counter, objective,
    /// a real analog joystick (base + moving knob), pedal buttons with pressed feedback, safe-area
    /// layout, pause menu and a result screen. All taps are handled by TouchControls (no EventSystem
    /// needed), so the HUD works whichever input backend is active.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        public MissionDirector Mission;
        public VehicleHealth Health;
        public WheelDrive Drive;
        public TouchControls Controls;
        public DerbyArena.Combat.WeaponRig Weapons;
        public Action Restart;
        public bool Paused;

        const float KnobTravel = 84f; // canvas reference pixels (base radius 150 * StickRadiusFactor 0.56)

        Font font;
        Sprite square;
        Sprite round;
        Sprite circle;
        RectTransform safe;
        Rect lastSafe;

        Text hpText, nitroText, timeText, cellText, objText, resultTitle, resultSub;
        Image hpFill, nitroFill, knob, stickBase;
        Image gasImg, brakeImg, nitroImg, handImg, fireImg, rocketImg;
        Text rocketText;
        GameObject pausePanel, resultPanel;
        RectTransform resumeRect, againRect, resultAgainRect;
        float lastToggle = -10f;
        float resultShownAt = -1f;
        bool resultVisible;

        // ------------------------------------------------------------------ sprites
        static Sprite MakeSquare()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRound(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f;
                    float fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, radius, size - radius);
                    float cy = Mathf.Clamp(fy, radius, size - radius);
                    float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        // ------------------------------------------------------------------ element helpers
        static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Image NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced) img.type = Image.Type.Sliced;
            return img;
        }

        Text NewText(string name, Transform parent, TextAnchor anchor, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Anchor(t.rectTransform, 0f, 0f, 1f, 1f);
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.75f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        Image Panel(string name, Transform parent, float x0, float y0, float x1, float y1, Color color)
        {
            var img = NewImage(name, parent, round, color, true);
            Anchor(img.rectTransform, x0, y0, x1, y1);
            return img;
        }

        Image Bar(string name, float x0, float y0, float x1, float y1, Color fill, out Text label)
        {
            var bg = Panel(name, safe, x0, y0, x1, y1, new Color(0f, 0f, 0f, 0.55f));
            var f = NewImage("Fill", bg.transform, square, fill, false);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillAmount = 1f;
            Anchor(f.rectTransform, 0.02f, 0.16f, 0.98f, 0.84f);
            label = NewText("Label", bg.transform, TextAnchor.MiddleLeft, 28, Color.white);
            Anchor(label.rectTransform, 0.06f, 0f, 1f, 1f);
            return f;
        }

        Image Btn(string caption, float x0, float y0, float x1, float y1, Color color)
        {
            var img = Panel(caption, safe, x0, y0, x1, y1, new Color(color.r, color.g, color.b, 0.55f));
            var t = NewText("T", img.transform, TextAnchor.MiddleCenter, 32, Color.white);
            t.text = caption;
            return img;
        }

        static void Tint(Image img, bool pressed)
        {
            if (img == null) return;
            var c = img.color;
            c.a = pressed ? 0.95f : 0.55f;
            img.color = c;
        }

        // ------------------------------------------------------------------ build
        public void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            square = MakeSquare();
            round = MakeRound(64, 20);
            circle = MakeRound(128, 64);

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            safe = safeGo.GetComponent<RectTransform>();
            ApplySafeArea();

            // Top bar
            hpFill = Bar("HpBar", 0.015f, 0.905f, 0.30f, 0.975f, new Color(0.25f, 0.85f, 0.40f, 0.95f), out hpText);
            nitroFill = Bar("NitroBar", 0.015f, 0.825f, 0.24f, 0.885f, new Color(0.25f, 0.55f, 1f, 0.95f), out nitroText);

            var timePanel = Panel("TimePanel", safe, 0.42f, 0.905f, 0.58f, 0.975f, new Color(0f, 0f, 0f, 0.55f));
            timeText = NewText("Time", timePanel.transform, TextAnchor.MiddleCenter, 40, Color.white);
            var cellPanel = Panel("CellPanel", safe, 0.60f, 0.905f, 0.80f, 0.975f, new Color(0f, 0f, 0f, 0.55f));
            cellText = NewText("Cells", cellPanel.transform, TextAnchor.MiddleCenter, 30, new Color(0.45f, 0.85f, 1f));
            var pausePanelBtn = Panel("PauseBtn", safe, 0.83f, 0.905f, 0.985f, 0.975f, new Color(0.12f, 0.12f, 0.14f, 0.85f));
            var pauseLabel = NewText("T", pausePanelBtn.transform, TextAnchor.MiddleCenter, 26, Color.white);
            pauseLabel.text = "DURAKLAT";

            objText = NewText("Objective", safe, TextAnchor.MiddleCenter, 32, Color.white);
            Anchor(objText.rectTransform, 0.2f, 0.80f, 0.8f, 0.89f);

            // Analog joystick: base ring + moving knob
            stickBase = NewImage("StickBase", safe, circle, new Color(1f, 1f, 1f, 0.18f), false);
            var sb = stickBase.rectTransform;
            sb.anchorMin = new Vector2(0.125f, 0.25f);
            sb.anchorMax = new Vector2(0.125f, 0.25f);
            sb.pivot = new Vector2(0.5f, 0.5f);
            sb.sizeDelta = new Vector2(300f, 300f);
            sb.anchoredPosition = Vector2.zero;
            knob = NewImage("Knob", stickBase.transform, circle, new Color(1f, 1f, 1f, 0.45f), false);
            var kr = knob.rectTransform;
            kr.anchorMin = new Vector2(0.5f, 0.5f);
            kr.anchorMax = new Vector2(0.5f, 0.5f);
            kr.pivot = new Vector2(0.5f, 0.5f);
            kr.sizeDelta = new Vector2(130f, 130f);
            kr.anchoredPosition = Vector2.zero;
            var hint = NewText("Hint", stickBase.transform, TextAnchor.MiddleCenter, 26, new Color(1f, 1f, 1f, 0.35f));
            hint.text = "<         >";

            // Pedals and actions
            gasImg = Btn("GAZ", 0.58f, 0.05f, 0.74f, 0.28f, new Color(0.18f, 0.62f, 0.30f));
            brakeImg = Btn("FREN", 0.76f, 0.05f, 0.94f, 0.24f, new Color(0.72f, 0.20f, 0.16f));
            nitroImg = Btn("NİTRO", 0.56f, 0.30f, 0.72f, 0.46f, new Color(0.18f, 0.42f, 0.92f));
            handImg = Btn("EL FRENİ", 0.74f, 0.28f, 0.94f, 0.44f, new Color(0.30f, 0.32f, 0.38f));
            fireImg = Btn("ATEŞ", 0.72f, 0.50f, 0.96f, 0.86f, new Color(0.85f, 0.28f, 0.14f));
            rocketImg = Btn("ROKET", 0.56f, 0.50f, 0.70f, 0.64f, new Color(0.95f, 0.55f, 0.10f));
            rocketText = rocketImg.GetComponentInChildren<Text>();

            // Pause menu (full screen, above everything)
            pausePanel = NewImage("PauseMenu", canvasGo.transform, square, new Color(0f, 0f, 0f, 0.65f), false).gameObject;
            Anchor(pausePanel.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            var pt = NewText("Title", pausePanel.transform, TextAnchor.MiddleCenter, 60, Color.white);
            Anchor(pt.rectTransform, 0.2f, 0.64f, 0.8f, 0.80f);
            pt.text = "DURAKLATILDI";
            var resume = Panel("Resume", pausePanel.transform, 0.32f, 0.42f, 0.68f, 0.56f, new Color(0.18f, 0.62f, 0.30f, 0.95f));
            NewText("T", resume.transform, TextAnchor.MiddleCenter, 40, Color.white).text = "DEVAM";
            resumeRect = resume.rectTransform;
            var again = Panel("Again", pausePanel.transform, 0.32f, 0.24f, 0.68f, 0.38f, new Color(0.72f, 0.20f, 0.16f, 0.95f));
            NewText("T", again.transform, TextAnchor.MiddleCenter, 40, Color.white).text = "YENİDEN BAŞLAT";
            againRect = again.rectTransform;
            pausePanel.SetActive(false);

            // Result screen
            resultPanel = NewImage("Result", canvasGo.transform, square, new Color(0f, 0f, 0f, 0.7f), false).gameObject;
            Anchor(resultPanel.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            resultTitle = NewText("Title", resultPanel.transform, TextAnchor.MiddleCenter, 72, Color.white);
            Anchor(resultTitle.rectTransform, 0.1f, 0.58f, 0.9f, 0.78f);
            resultSub = NewText("Sub", resultPanel.transform, TextAnchor.MiddleCenter, 34, new Color(0.85f, 0.9f, 1f));
            Anchor(resultSub.rectTransform, 0.1f, 0.44f, 0.9f, 0.56f);
            var ra = Panel("Again", resultPanel.transform, 0.30f, 0.20f, 0.70f, 0.34f, new Color(0.18f, 0.42f, 0.92f, 0.95f));
            NewText("T", ra.transform, TextAnchor.MiddleCenter, 40, Color.white).text = "YENİDEN OYNA";
            resultAgainRect = ra.rectTransform;
            resultPanel.SetActive(false);

            // Register the real on-screen rectangles for input hit-testing.
            if (Controls != null)
            {
                Controls.StickRect = sb;
                Controls.GasRect = gasImg.rectTransform;
                Controls.BrakeRect = brakeImg.rectTransform;
                Controls.NitroRect = nitroImg.rectTransform;
                Controls.HandRect = handImg.rectTransform;
                Controls.FireRect = fireImg.rectTransform;
                Controls.RocketRect = rocketImg.rectTransform;
                Controls.PauseRect = pausePanelBtn.rectTransform;
            }
        }

        void ApplySafeArea()
        {
            if (safe == null) return;
            Rect sa = Screen.safeArea;
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            safe.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h);
            safe.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h);
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;
            lastSafe = sa;
        }

        // ------------------------------------------------------------------ behaviour
        public void TogglePause()
        {
            if (Mission != null && (Mission.Rules.Phase == MissionPhase.Won || Mission.Rules.Phase == MissionPhase.Lost))
                return;
            if (Time.unscaledTime - lastToggle < 0.25f)
                return;
            lastToggle = Time.unscaledTime;
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(Paused);
        }

        void DoRestart()
        {
            Time.timeScale = 1f;
            if (Restart != null) Restart();
        }

        void HandleTap(Vector2 p)
        {
            if (Paused)
            {
                if (TouchControls.Hit(resumeRect, p, 0f)) TogglePause();
                else if (TouchControls.Hit(againRect, p, 0f)) DoRestart();
                return;
            }
            if (resultVisible && Time.unscaledTime - resultShownAt > 0.8f)
                DoRestart();
        }

        void Update()
        {
            if (safe != null && Screen.safeArea != lastSafe)
                ApplySafeArea();

            if (Controls != null)
            {
                if (Controls.AnyTap) HandleTap(Controls.TapPosition);
                if (Controls.PausePressed) TogglePause();

                Tint(gasImg, Controls.Throttle > 0.5f);
                Tint(brakeImg, Controls.Brake > 0.5f);
                Tint(nitroImg, Controls.Nitro);
                Tint(handImg, Controls.Handbrake);
                Tint(fireImg, Controls.Fire);
                Tint(rocketImg, Controls.Rocket);
                if (knob != null)
                {
                    knob.rectTransform.anchoredPosition = Controls.StickVector * KnobTravel;
                    var kc = knob.color;
                    kc.a = Controls.StickVector.sqrMagnitude > 0.02f ? 0.75f : 0.45f;
                    knob.color = kc;
                }
            }

            if (Health != null && hpText != null)
            {
                float frac = Mathf.Clamp01(Health.Current / Mathf.Max(1f, Health.Max));
                hpText.text = "CAN " + Mathf.CeilToInt(Health.Current);
                hpFill.fillAmount = frac;
                hpFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.15f, 0.95f), new Color(0.25f, 0.85f, 0.40f, 0.95f), frac);
            }
            if (Weapons != null && rocketText != null)
                rocketText.text = Weapons.Spec != null && Weapons.Spec.Rockets ? "ROKET " + Weapons.Rockets : "ROKET -";
            if (Drive != null && nitroText != null)
            {
                nitroText.text = "NİTRO " + Mathf.RoundToInt(Drive.Nitro * 100f) + "%";
                nitroFill.fillAmount = Mathf.Clamp01(Drive.Nitro);
            }
            if (Mission == null || timeText == null)
                return;

            int t = Mathf.Max(0, Mathf.CeilToInt(Mission.Rules.TimeLeft));
            timeText.text = (t / 60) + ":" + (t % 60).ToString("00");
            timeText.color = t <= 30 ? new Color(1f, 0.35f, 0.3f) : Color.white;
            cellText.text = "HÜCRE " + Mission.Rules.Cells + "/" + MissionRules.CellsRequired;

            var phase = Mission.Rules.Phase;
            if (phase == MissionPhase.Hunt)
                objText.text = "Reaktör hücrelerini topla";
            else if (phase == MissionPhase.Extract)
                objText.text = "Tahliye alanında kal  " + Mathf.CeilToInt(Mission.Rules.ExtractHold) + "/" +
                               Mathf.CeilToInt(Mission.Rules.ExtractHoldRequired);
            else if (phase == MissionPhase.Won)
                objText.text = "BLACKOUT RUN TAMAM";
            else
                objText.text = "KAYBEDİLDİ";

            bool finished = phase == MissionPhase.Won || phase == MissionPhase.Lost;
            if (finished && !resultVisible)
            {
                resultVisible = true;
                resultShownAt = Time.unscaledTime;
                resultTitle.text = phase == MissionPhase.Won ? "GÖREV TAMAM" : "KAYBETTİN";
                resultSub.text = "Hücre " + Mission.Rules.Cells + "/" + MissionRules.CellsRequired +
                                 "   -   dokun ve yeniden oyna";
                if (resultPanel != null) resultPanel.SetActive(true);
            }
        }
    }
}
