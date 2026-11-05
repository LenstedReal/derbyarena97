using System;
using System.Collections.Generic;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// Main menu over a live 3D arena (4.0 look): language picker (first launch), home with the daily missions,
    /// level select (seasons of 20 star-gated levels plus the roadmap teaser), the garage shop (vehicle collection,
    /// paint, rims, neon, stripes, parts, upgrades), daily missions, settings and the legal pages. Every visible
    /// string is Turkish / English via Loc. Sprites and icons come from Resources/Derby/UI through UiKit.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        RectTransform canvasRt, safe;
        GameObject page;
        string current = "home";
        int chapter = 1;
        int selected = 1;
        string legalDoc = "privacy";
        string garageMsg = "";
        int garageTab = 0;           // 0 upgrades, 1..8 = PartSlot + 1
        int vehicleIndex = 0;
        float toastUntil;
        Text toast;

        static readonly Color DarkText = UiKit.Ink;
        static readonly Color LockedTab = new Color(0.08f, 0.08f, 0.09f, 0.9f);
        static readonly Color SidePanel = new Color(0.03f, 0.035f, 0.045f, 0.84f);

        public static MenuController Create(string startPage)
        {
            var go = new GameObject("Menu");
            var m = go.AddComponent<MenuController>();
            m.canvasRt = UiKit.Canvas("MenuCanvas", 20, out m.safe);
            m.canvasRt.SetParent(go.transform, false);
            var vig = UiKit.Image("Vignette", m.canvasRt, UiKit.VignetteSprite, new Color(0f, 0f, 0f, 0.5f));
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 1f, 1f);
            vig.transform.SetAsFirstSibling();
            m.selected = Campaign.NextToPlay();
            m.chapter = (m.selected - 1) / Campaign.PerChapter + 1;
            for (int i = 0; i < Fleet.Defs.Length; i++) if (Fleet.Defs[i].Id == Save.Data.Vehicle) m.vehicleIndex = i;
            m.Show(Save.Data.LanguageChosen ? startPage : "language");
            return m;
        }

        void Update()
        {
            if (toast != null && Time.unscaledTime > toastUntil) { Destroy(toast.transform.parent.gameObject); toast = null; }
            // Android back button (reported as Escape by the Input System): one level up, never quits silently.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (ControlLayoutEditor.IsOpen) return;
            if (current == "legal" || current == "controls") Show("settings");
            else if (current != "home" && current != "language") Show("home");
        }

        void Show(string name)
        {
            bool leavingGarage = current == "garage" && name != "garage";
            current = name;
            if (page != null) Destroy(page);
            if (leavingGarage)
            {
                Fleet.ClearPreview();
                if (Showcase.Instance != null) Showcase.Instance.Rebuild();
            }
            page = UiKit.Rect("Page_" + name, safe).gameObject;
            UiKit.Stretch(page.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            // 4.0.2: the showcase car sits in the free area right of the home panel; pages that fill the screen get a
            // dark scrim so the 3D scene never shows through text and cards
            var cam = DerbyArena.CameraRig.ChaseCamera.Main;
            if (cam != null) cam.OrbitScreenShift = name == "home" ? 0.36f : 0f;
            if (name == "levels" || name == "settings" || name == "controls" || name == "legal" || name == "missions")
            {
                // first child of the page (drawn under its content), stretched past the safe area to the screen edges
                var scrim = UiKit.Image("Scrim", page.transform, UiKit.Square, new Color(0.01f, 0.012f, 0.018f, 0.72f));
                UiKit.Stretch(scrim.rectTransform, 0f, 0f, 1f, 1f);
                scrim.rectTransform.offsetMin = new Vector2(-400f, -400f);
                scrim.rectTransform.offsetMax = new Vector2(400f, 400f);
                scrim.transform.SetAsFirstSibling();
            }
            switch (name)
            {
                case "language": BuildLanguage(); break;
                case "levels": BuildLevels(); break;
                case "garage": BuildGarage(); break;
                case "settings": BuildSettings(); break;
                case "controls": BuildControlsPage(); break;
                case "legal": BuildLegal(); break;
                case "missions": BuildMissions(); break;
                default: BuildHome(); break;
            }
        }

        Transform P { get { return page.transform; } }

        // ------------------------------------------------------------------ small helpers
        UiButton Btn(Transform parent, string caption, Color color, float ax, float ay, float w, float h, float px, float py, int font, Action a)
        {
            var b = UiKit.Button(parent, caption, color, font, a);
            UiKit.Place(b.Background.rectTransform, ax, ay, w, h, px, py);
            return b;
        }

        UiButton Btn(Transform parent, string caption, Color color, float ax, float ay, float w, float h, float px, float py, int font, Action a, bool primary, string icon)
        {
            var b = UiKit.Button(parent, caption, color, font, a, primary, icon);
            UiKit.Place(b.Background.rectTransform, ax, ay, w, h, px, py);
            return b;
        }

        Text Txt(Transform parent, string text, int size, Color c, TextAnchor anchor, bool bold, float ax, float ay, float w, float h, float px, float py)
        {
            var r = UiKit.Rect("Text", parent);
            UiKit.Place(r, ax, ay, w, h, px, py);
            return UiKit.Label("Label", r, text, size, c, anchor, bold);
        }

        static void Move(Text t, float x, float y)
        {
            t.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }

        static void At(Component c, float x, float y)
        {
            var rt = c.transform as RectTransform;
            if (rt != null) rt.anchoredPosition = new Vector2(x, y);
        }

        Image Frame(Transform parent, float ax, float ay, float w, float h, float px, float py, float x, float y)
        {
            var f = UiKit.Frame("Frame", parent, UiKit.Panel);
            UiKit.Place(f.rectTransform, ax, ay, w, h, px, py);
            f.rectTransform.anchoredPosition = new Vector2(x, y);
            return f;
        }

        void Toast(string text, Color c)
        {
            if (toast != null) Destroy(toast.transform.parent.gameObject);
            var bg = UiKit.Image("Toast", P, UiKit.Pill, new Color(0f, 0f, 0f, 0.8f));
            UiKit.Place(bg.rectTransform, 0.5f, 0f, 760f, 64f, 0.5f, 0f);
            bg.rectTransform.anchoredPosition = new Vector2(0f, 110f);
            toast = UiKit.Label("Text", bg.transform, text, 26, c, TextAnchor.MiddleCenter, true);
            toastUntil = Time.unscaledTime + 2.4f;
        }

        void Header(string title, string icon)
        {
            var back = Btn(P, Loc.T("GERİ", "BACK"), UiKit.PanelLight, 0f, 1f, 200f, 78f, 0f, 1f, 28, () => Show("home"), false, "back");
            back.IgnoreBlock = true;
            if (UiKit.Icon(icon) != null)
            {
                var ic = UiKit.IconImage(P, icon, 44f, UiKit.Amber, 0f, 1f, 0f, 1f);
                ic.rectTransform.anchoredPosition = new Vector2(232f, -17f);
            }
            Move(Txt(P, title, 50, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 900f, 78f, 0f, 1f), 290f, 0f);
            var scrap = UiKit.Chip(P, "scrap", Save.Data.Scrap.ToString(), UiKit.Amber, 250f, 64f, 30);
            UiKit.Place(scrap.rectTransform, 1f, 1f, 250f, 64f, 1f, 1f);
            scrap.rectTransform.anchoredPosition = new Vector2(0f, -7f);
            var stars = UiKit.Chip(P, "star", Save.TotalStars() + "/" + (Campaign.Total * 3), UiKit.Amber, 230f, 64f, 26);
            UiKit.Place(stars.rectTransform, 1f, 1f, 230f, 64f, 1f, 1f);
            stars.rectTransform.anchoredPosition = new Vector2(-262f, -7f);
        }

        // ================================================================== language
        void BuildLanguage()
        {
            var shade = UiKit.Image("Shade", P, UiKit.Square, new Color(0f, 0f, 0f, 0.55f));
            UiKit.Stretch(shade.rectTransform, 0f, 0f, 1f, 1f);
            var panel = UiKit.Frame("Panel", P, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 940f, 560f, 0.5f, 0.5f);
            var t = panel.transform;
            var ic = UiKit.IconImage(t, "globe", 56f, UiKit.Amber, 0.5f, 1f, 0.5f, 1f);
            ic.rectTransform.anchoredPosition = new Vector2(0f, -34f);
            Move(Txt(t, "DİL SEÇ  ·  CHOOSE LANGUAGE", 40, UiKit.Amber, TextAnchor.MiddleCenter, true, 0.5f, 1f, 900f, 60f, 0.5f, 1f), 0f, -96f);
            bool en = Loc.IsEnglish;
            var tr = Btn(t, "TÜRKÇE", en ? UiKit.PanelLight : UiKit.Primary, 0.5f, 0.5f, 400f, 150f, 0.5f, 0.5f, 44, () => PickLanguage(Loc.Turkish), !en, null);
            tr.Background.rectTransform.anchoredPosition = new Vector2(-215f, -10f);
            var eng = Btn(t, "ENGLISH", en ? UiKit.Primary : UiKit.PanelLight, 0.5f, 0.5f, 400f, 150f, 0.5f, 0.5f, 44, () => PickLanguage(Loc.English), en, null);
            eng.Background.rectTransform.anchoredPosition = new Vector2(215f, -10f);
            Move(Txt(t, "Ayarlar'dan istediğin zaman değiştirebilirsin  ·  You can change it any time in Settings", 22, UiKit.TextDim, TextAnchor.MiddleCenter, false, 0.5f, 0f, 900f, 60f, 0.5f, 0f), 0f, 40f);
        }

        void PickLanguage(int language)
        {
            Loc.Set(language);
            Show("home");
        }

        // ================================================================== home
        void BuildHome()
        {
            // 4.0.2: plain dark panel (the tiled carbon pattern shimmered on phones), a soft edge instead of a hard line
            var side = UiKit.Image("Side", P, UiKit.Square, new Color(0.015f, 0.018f, 0.026f, 0.84f));
            UiKit.Place(side.rectTransform, 0f, 0f, 720f, 0f, 0f, 0f);
            side.rectTransform.anchorMax = new Vector2(0f, 1f);
            side.rectTransform.sizeDelta = new Vector2(720f, 0f);
            var edge = UiKit.Image("Edge", P, UiKit.Square, new Color(1f, 0.6f, 0.13f, 0.35f));
            UiKit.Place(edge.rectTransform, 0f, 0f, 2f, 0f, 0f, 0f);
            edge.rectTransform.anchorMax = new Vector2(0f, 1f);
            edge.rectTransform.sizeDelta = new Vector2(2f, 0f);
            edge.rectTransform.anchoredPosition = new Vector2(720f, 0f);

            Move(Txt(P, "DERBY ARENA", 92, UiKit.Amber, TextAnchor.UpperLeft, true, 0f, 1f, 660f, 116f, 0f, 1f), 40f, -22f);
            Move(Txt(P, Loc.T("ÇARPIŞ  ·  YIK  ·  KAZAN", "SMASH  ·  WRECK  ·  WIN"), 30, UiKit.Cyan, TextAnchor.UpperLeft, true, 0f, 1f, 640f, 46f, 0f, 1f), 44f, -136f);
            if (UiKit.Hazard != null)
            {
                var hz = UiKit.Image("Hazard", P, UiKit.Hazard, new Color(1f, 0.6f, 0.13f, 0.9f));
                hz.type = Image.Type.Tiled;
                UiKit.Place(hz.rectTransform, 0f, 1f, 300f, 10f, 0f, 1f);
                hz.rectTransform.anchoredPosition = new Vector2(46f, -186f);
            }

            int play = Campaign.NextToPlay();
            var cfg = Campaign.Get(play);
            float y = -222f;
            var main = Btn(P, "", UiKit.Primary, 0f, 1f, 640f, 136f, 0f, 1f, 46, () => GameFlow.Play(play), true, null);
            main.Background.rectTransform.anchoredPosition = new Vector2(40f, y);
            var mt = main.Background.transform;
            var pi = UiKit.IconImage(mt, "play", 64f, DarkText, 0f, 0.5f, 0f, 0.5f);
            pi.rectTransform.anchoredPosition = new Vector2(28f, 0f);
            Move(Txt(mt, Loc.T("OYNA", "PLAY"), 46, DarkText, TextAnchor.MiddleLeft, true, 0f, 0.5f, 500f, 60f, 0f, 0.5f), 112f, 14f);
            Move(Txt(mt, Loc.T("BÖLÜM ", "LEVEL ") + play + "  ·  " + cfg.Title, 24, new Color(0.2f, 0.12f, 0.04f, 0.9f), TextAnchor.MiddleLeft, true, 0f, 0.5f, 520f, 36f, 0f, 0.5f), 114f, -26f);

            string under;
            Color underCol = UiKit.TextDim;
            if (Campaign.AllCleared) { under = Loc.T("TÜM BÖLÜMLER TAMAM · YENİ SEZON YAKINDA", "ALL LEVELS CLEARED · NEW SEASON COMING SOON"); underCol = UiKit.Amber; }
            else if (Save.Data.Stars[play] > 0 && play < Campaign.Total && !Campaign.IsOpen(play + 1))
            {
                under = Loc.T("Sıradaki bölüm için ", "Next level needs ") + "★ " + Campaign.StarsRequired(play + 1) + Loc.T(" gerek (sende ", " (you have ") + Save.TotalStars() + ")";
                underCol = UiKit.Amber;
            }
            else under = cfg.Subtitle + "  ·  " + LevelConfig.KindName(cfg.Kind);
            Move(Txt(P, under, 24, underCol, TextAnchor.UpperLeft, false, 0f, 1f, 640f, 40f, 0f, 1f), 44f, y - 144f);

            y -= 196f;
            string[] names = { Loc.T("BÖLÜMLER", "LEVELS"), Loc.T("GARAJ", "GARAGE"), Loc.T("AYARLAR", "SETTINGS") };
            string[] pages = { "levels", "garage", "settings" };
            string[] icons = { "flag", "wrench", "gear" };
            for (int i = 0; i < names.Length; i++)
            {
                string pg = pages[i];
                var b = Btn(P, names[i], UiKit.PanelLight, 0f, 1f, 640f, 88f, 0f, 1f, 34, () => Show(pg), false, icons[i]);
                b.Background.rectTransform.anchoredPosition = new Vector2(40f, y);
                y -= 102f;
            }

            BuildDailyPanel(40f, y - 6f, 640f, 290f);

            // top-right chips: scrap, stars, level, language
            float cx = 0f;
            var lang = Btn(P, Loc.LanguageName, UiKit.PanelLight, 1f, 1f, 220f, 62f, 1f, 1f, 24, () => Show("language"), false, "globe");
            lang.Background.rectTransform.anchoredPosition = new Vector2(cx, -8f);
            cx -= 232f;
            var lvl = UiKit.Chip(P, "flag", Save.Data.Unlocked + "/" + Campaign.Total, UiKit.Cyan, 210f, 62f, 26);
            UiKit.Place(lvl.rectTransform, 1f, 1f, 210f, 62f, 1f, 1f); lvl.rectTransform.anchoredPosition = new Vector2(cx, -8f);
            cx -= 222f;
            var st = UiKit.Chip(P, "star", Save.TotalStars() + "/" + (Campaign.Total * 3), UiKit.Amber, 230f, 62f, 26);
            UiKit.Place(st.rectTransform, 1f, 1f, 230f, 62f, 1f, 1f); st.rectTransform.anchoredPosition = new Vector2(cx, -8f);
            cx -= 242f;
            var sc = UiKit.Chip(P, "scrap", Save.Data.Scrap.ToString(), UiKit.Amber, 250f, 62f, 28);
            UiKit.Place(sc.rectTransform, 1f, 1f, 250f, 62f, 1f, 1f); sc.rectTransform.anchoredPosition = new Vector2(cx, -8f);

            // vehicle name over the showcase
            var def = Fleet.Current();
            var vchip = UiKit.Chip(P, def.Tank ? "tank" : "car", def.Name, UiKit.Cyan, 520f, 60f, 26);
            UiKit.Place(vchip.rectTransform, 0.69f, 0f, 520f, 60f, 0.5f, 0f);
            vchip.rectTransform.anchoredPosition = new Vector2(0f, 24f);

            var art = Resources.Load<Texture2D>("Derby/keyart");
            if (art != null)
            {
                var frame = UiKit.Image("KeyArtFrame", P, UiKit.Card, new Color(1f, 0.55f, 0.15f, 0.9f));
                UiKit.Place(frame.rectTransform, 1f, 0f, 250f, 250f, 1f, 0f);
                frame.rectTransform.anchoredPosition = new Vector2(-10f, 92f);
                var ago = new GameObject("KeyArt", typeof(RectTransform));
                ago.transform.SetParent(frame.transform, false);
                var raw = ago.AddComponent<RawImage>();
                raw.texture = art;
                raw.raycastTarget = false;
                UiKit.Stretch(raw.rectTransform, 0f, 0f, 1f, 1f);
                raw.rectTransform.offsetMin = new Vector2(5f, 5f);
                raw.rectTransform.offsetMax = new Vector2(-5f, -5f);
            }
            var soon = Btn(P, Loc.T("YENİ SEZON · YOL HARİTASI", "NEW SEASON · ROADMAP"), UiKit.Primary, 1f, 0f, 250f, 58f, 1f, 0f, 19,
                () => { chapter = Campaign.Seasons + 1; Show("levels"); }, true, null);
            soon.Background.rectTransform.anchoredPosition = new Vector2(-10f, 352f);

            Txt(P, "v" + DerbyStartupDiagnostics.Version, 20, new Color(1f, 1f, 1f, 0.35f), TextAnchor.LowerRight, false, 1f, 0f, 300f, 36f, 1f, 0f);
        }

        void BuildDailyPanel(float x, float y, float w, float h)
        {
            var f = Frame(P, 0f, 1f, w, h, 0f, 1f, x, y);
            var t = f.transform;
            var ic = UiKit.IconImage(t, "calendar", 34f, UiKit.Cyan, 0f, 1f, 0f, 1f);
            ic.rectTransform.anchoredPosition = new Vector2(20f, -14f);
            Move(Txt(t, Loc.T("GÜNLÜK GÖREVLER", "DAILY MISSIONS"), 28, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 400f, 44f, 0f, 1f), 64f, -10f);
            var left = DailyMissions.TimeLeft;
            Move(Txt(t, string.Format("{0:00}:{1:00}", (int)left.TotalHours, left.Minutes), 24, UiKit.TextDim, TextAnchor.MiddleRight, true, 1f, 1f, 200f, 44f, 1f, 1f), -22f, -10f);
            var ms = DailyMissions.Missions();
            float ry = -64f;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i];
                int idx = i;
                var mi = UiKit.IconImage(t, m.Icon, 30f, m.Done ? UiKit.Green : UiKit.Amber, 0f, 1f, 0f, 1f);
                mi.rectTransform.anchoredPosition = new Vector2(22f, ry - 6f);
                Move(Txt(t, m.Text, 22, m.Claimed ? UiKit.TextDim : Color.white, TextAnchor.MiddleLeft, false, 0f, 1f, 320f, 40f, 0f, 1f), 64f, ry);
                Image fill;
                var bar = UiKit.Bar(t, m.Done ? UiKit.Green : UiKit.Amber, out fill);
                UiKit.Place(bar.rectTransform, 0f, 1f, 140f, 18f, 0f, 1f);
                bar.rectTransform.anchoredPosition = new Vector2(394f, ry - 12f);
                fill.fillAmount = m.Target > 0 ? Mathf.Clamp01(m.Progress / (float)m.Target) : 0f;
                if (m.Claimed)
                {
                    var ck = UiKit.IconImage(t, "check", 28f, UiKit.Green, 0f, 1f, 0f, 1f);
                    ck.rectTransform.anchoredPosition = new Vector2(580f, ry - 6f);
                }
                else if (m.Done)
                {
                    var cb = Btn(t, Loc.T("AL", "CLAIM"), UiKit.Green, 0f, 1f, 80f, 40f, 0f, 1f, 20, () => { if (DailyMissions.Claim(idx)) Show("home"); });
                    cb.Background.rectTransform.anchoredPosition = new Vector2(544f, ry);
                }
                else Move(Txt(t, "+" + m.Reward, 20, UiKit.Amber, TextAnchor.MiddleRight, true, 0f, 1f, 74f, 40f, 0f, 1f), 546f, ry);
                ry -= 62f;
            }
            var more = Btn(t, Loc.T("TÜMÜNÜ GÖR", "SEE ALL"), new Color(1f, 1f, 1f, 0.08f), 1f, 0f, 170f, 40f, 1f, 0f, 18, () => Show("missions"));
            more.Background.rectTransform.anchoredPosition = new Vector2(-14f, 10f);
        }

        // ================================================================== daily missions page
        void BuildMissions()
        {
            Header(Loc.T("GÜNLÜK GÖREVLER", "DAILY MISSIONS"), "calendar");
            var f = Frame(P, 0f, 1f, 1300f, 560f, 0f, 1f, 0f, -100f);
            var t = f.transform;
            var left = DailyMissions.TimeLeft;
            Move(Txt(t, Loc.T("Her gün üç görev. Ödülleri gün bitmeden al; gece yarısı yenilenir.", "Three missions every day. Claim before midnight, when the set renews.") + "   " +
                Loc.T("Kalan: ", "Left: ") + string.Format("{0:00}:{1:00}", (int)left.TotalHours, left.Minutes), 24, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 1f, 1240f, 50f, 0f, 1f), 30f, -16f);
            var ms = DailyMissions.Missions();
            float ry = -90f;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i];
                int idx = i;
                var row = UiKit.Image("Row", t, UiKit.Card, UiKit.PanelLight);
                UiKit.Place(row.rectTransform, 0f, 1f, 1240f, 130f, 0f, 1f);
                row.rectTransform.anchoredPosition = new Vector2(30f, ry);
                var rt = row.transform;
                var mi = UiKit.IconImage(rt, m.Icon, 56f, m.Done ? UiKit.Green : UiKit.Amber, 0f, 0.5f, 0f, 0.5f);
                mi.rectTransform.anchoredPosition = new Vector2(28f, 0f);
                Move(Txt(rt, m.Text, 32, Color.white, TextAnchor.MiddleLeft, true, 0f, 0.5f, 640f, 50f, 0f, 0.5f), 110f, 22f);
                Move(Txt(rt, m.Progress + " / " + m.Target, 24, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 0.5f, 300f, 40f, 0f, 0.5f), 110f, -24f);
                Image fill;
                var bar = UiKit.Bar(rt, m.Done ? UiKit.Green : UiKit.Amber, out fill);
                UiKit.Place(bar.rectTransform, 0f, 0.5f, 360f, 22f, 0f, 0.5f);
                bar.rectTransform.anchoredPosition = new Vector2(300f, -26f);
                fill.fillAmount = m.Target > 0 ? Mathf.Clamp01(m.Progress / (float)m.Target) : 0f;
                var reward = UiKit.Chip(rt, "scrap", "+" + m.Reward, UiKit.Amber, 190f, 56f, 26);
                UiKit.Place(reward.rectTransform, 1f, 0.5f, 190f, 56f, 1f, 0.5f);
                reward.rectTransform.anchoredPosition = new Vector2(-250f, 0f);
                if (m.Claimed)
                {
                    var done = Btn(rt, Loc.T("ALINDI", "CLAIMED"), new Color(0.2f, 0.2f, 0.22f, 0.9f), 1f, 0.5f, 220f, 72f, 1f, 0.5f, 24, null, false, "check");
                    done.Background.rectTransform.anchoredPosition = new Vector2(-16f, 0f);
                    done.Interactable = false;
                }
                else
                {
                    var cb = Btn(rt, Loc.T("ÖDÜLÜ AL", "CLAIM"), m.Done ? UiKit.Green : new Color(0.2f, 0.2f, 0.22f, 0.9f), 1f, 0.5f, 220f, 72f, 1f, 0.5f, 24,
                        () => { if (DailyMissions.Claim(idx)) { Toast(Loc.T("+" + m.Reward + " HURDA", "+" + m.Reward + " SCRAP"), UiKit.Green); Show("missions"); } });
                    cb.Background.rectTransform.anchoredPosition = new Vector2(-16f, 0f);
                    cb.Interactable = m.Done;
                }
                ry -= 146f;
            }
        }

        // ================================================================== levels
        void BuildLevels()
        {
            Header(Loc.T("BÖLÜMLER", "LEVELS"), "flag");
            // 4.0.2: tabs, cards and the season strip live in one area that ends where the detail panel starts, and
            // are laid out in fractions of that area, so nothing runs under the panel on any screen shape
            var area = UiKit.Rect("LevelArea", P);
            UiKit.Stretch(area, 0f, 0f, 1f, 1f);
            area.offsetMin = new Vector2(0f, 0f);
            area.offsetMax = new Vector2(-(DetailWidth + 24f), 0f);

            int tabs = Campaign.Seasons + 1;   // + roadmap
            for (int c = 1; c <= tabs; c++)
            {
                int cc = c;
                bool upcoming = c > Campaign.Seasons;
                int first = (c - 1) * Campaign.PerChapter + 1;
                bool open = upcoming || Campaign.IsOpen(first);
                string caption = upcoming ? Loc.T("YAKINDA · YOL HARİTASI", "SOON · ROADMAP") : c + ". " + Campaign.ChapterName(c);
                Color col = c == chapter ? UiKit.Primary : (upcoming ? new Color(0.35f, 0.22f, 0.08f, 0.92f) : (open ? UiKit.PanelLight : LockedTab));
                var tab = UiKit.Button(area, caption, col, 19, () =>
                {
                    chapter = cc;
                    if (cc <= Campaign.Seasons)
                    {
                        int f = (cc - 1) * Campaign.PerChapter + 1;
                        selected = Mathf.Clamp(Mathf.Max(f, Mathf.Min(Save.Data.Unlocked, f + Campaign.PerChapter - 1)), 1, Campaign.Total);
                    }
                    Show("levels");
                }, c == chapter, null);
                var trt = tab.Background.rectTransform;
                trt.anchorMin = new Vector2((c - 1) / (float)tabs, 1f);
                trt.anchorMax = new Vector2(c / (float)tabs, 1f);
                trt.pivot = new Vector2(0.5f, 1f);
                trt.offsetMin = new Vector2(3f, -162f);
                trt.offsetMax = new Vector2(-3f, -96f);
                tab.Background.sprite = UiKit.Tab;
                if (!open)
                {
                    tab.Caption.color = UiKit.TextDim;
                    int need = Campaign.StarsRequired(first);
                    if (need > 0 && Save.TotalStars() < need) tab.Caption.text = caption + "\n★" + need;
                }
                tab.Interactable = open;
            }

            if (chapter > Campaign.Seasons)
            {
                BuildUpcomingSeason();
                return;
            }

            // 10 x 2 cards, each a tenth of the area wide
            int start = (chapter - 1) * Campaign.PerChapter + 1;
            float cardH = 170f, stepY = 178f;
            for (int i = 0; i < Campaign.PerChapter; i++)
            {
                int n = start + i;
                var cfg = Campaign.Get(n);
                bool open = Campaign.IsOpen(n);
                bool starGate = !open && n <= Save.Data.Unlocked;        // cleared up to here, only stars missing
                int need = Campaign.StarsRequired(n);
                Color col = !open ? new Color(0.05f, 0.05f, 0.06f, 0.92f) : (n == selected ? new Color(0.25f, 0.18f, 0.08f, 0.98f) : new Color(0.07f, 0.075f, 0.095f, 0.96f));
                int nn = n;
                var card = UiKit.Image("Card", area, UiKit.Card, col);
                var crt = card.rectTransform;
                int col10 = i % 10, row = i / 10;
                crt.anchorMin = new Vector2(col10 / 10f, 1f);
                crt.anchorMax = new Vector2((col10 + 1) / 10f, 1f);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.offsetMin = new Vector2(3f, -180f - row * stepY - cardH);
                crt.offsetMax = new Vector2(-3f, -180f - row * stepY);
                var cb = card.gameObject.AddComponent<UiButton>();
                cb.Background = card; cb.BaseColor = col;
                cb.OnTap = () => { selected = nn; Show("levels"); };
                cb.Interactable = true;
                var t = card.transform;
                if (n == selected && UiKit.CardRing != null)
                {
                    var ring = UiKit.Image("Ring", t, UiKit.CardRing, UiKit.Amber);
                    UiKit.Stretch(ring.rectTransform, 0f, 0f, 1f, 1f);
                }
                Color typeCol = HudController.KindColor(cfg.Kind);
                if (open)
                {
                    StretchTop(Txt(t, n.ToString(), 44, Color.white, TextAnchor.MiddleCenter, true, 0.5f, 1f, 100f, 56f, 0.5f, 1f), -8f, 56f);
                    var ki = UiKit.IconImage(t, KindIcon(cfg.Kind), 40f, typeCol, 0.5f, 1f, 0.5f, 1f);
                    ki.rectTransform.anchoredPosition = new Vector2(0f, -66f);
                    UiKit.Stars(t, Save.Data.Stars[n], 22f, 0.5f, 0f, 0.5f, 0f, new Vector2(0f, 14f));
                }
                else
                {
                    StretchTop(Txt(t, n.ToString(), 30, UiKit.TextDim, TextAnchor.MiddleCenter, true, 0.5f, 1f, 100f, 44f, 0.5f, 1f), -8f, 44f);
                    var lk = UiKit.IconImage(t, "lock", 40f, new Color(1f, 1f, 1f, 0.35f), 0.5f, 1f, 0.5f, 1f);
                    lk.rectTransform.anchoredPosition = new Vector2(0f, -60f);
                    if (starGate && need > 0)
                    {
                        var g = Txt(t, "★ " + need, 22, UiKit.Amber, TextAnchor.MiddleCenter, true, 0.5f, 0f, 100f, 36f, 0.5f, 0f);
                        var grt = g.rectTransform.parent as RectTransform;
                        grt.anchorMin = new Vector2(0f, 0f); grt.anchorMax = new Vector2(1f, 0f);
                        grt.offsetMin = new Vector2(4f, 10f); grt.offsetMax = new Vector2(-4f, 46f);
                    }
                    else
                    {
                        var ki = UiKit.IconImage(t, KindIcon(cfg.Kind), 26f, new Color(1f, 1f, 1f, 0.25f), 0.5f, 0f, 0.5f, 0f);
                        ki.rectTransform.anchoredPosition = new Vector2(0f, 14f);
                    }
                }
            }

            // season strip: feature line, season stars, next-season gate (three separate lines, never overlapping)
            var strip = UiKit.Frame("Strip", area, UiKit.Panel);
            var srt = strip.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(3f, -548f - 132f); srt.offsetMax = new Vector2(-3f, -548f);
            var stt = strip.transform;
            var feat = Txt(stt, Campaign.ChapterFeature(chapter), 24, UiKit.Cyan, TextAnchor.MiddleLeft, false, 0f, 1f, 100f, 44f, 0f, 1f);
            var frt = feat.rectTransform.parent as RectTransform;
            frt.anchorMin = new Vector2(0f, 1f); frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = new Vector2(24f, -56f); frt.offsetMax = new Vector2(-24f, -10f);
            int seasonStars = 0;
            for (int i = 0; i < Campaign.PerChapter; i++) seasonStars += Save.Data.Stars[start + i];
            Move(Txt(stt, Loc.T("SEZON YILDIZLARI  ", "SEASON STARS  ") + seasonStars + "/" + (Campaign.PerChapter * 3), 22, Color.white, TextAnchor.MiddleLeft, true, 0f, 0f, 330f, 36f, 0f, 0f), 24f, 16f);
            Image sfill;
            var sbar = UiKit.Bar(stt, UiKit.Amber, out sfill);
            var sbrt = sbar.rectTransform;
            sbrt.anchorMin = new Vector2(0f, 0f); sbrt.anchorMax = new Vector2(0.55f, 0f); sbrt.pivot = new Vector2(0f, 0f);
            sbrt.offsetMin = new Vector2(360f, 22f); sbrt.offsetMax = new Vector2(0f, 44f);
            sfill.fillAmount = seasonStars / (float)(Campaign.PerChapter * 3);
            if (chapter < Campaign.Seasons)
            {
                int nextFirst = chapter * Campaign.PerChapter + 1;
                int needNext = Campaign.StarsRequired(nextFirst);
                var nx = Txt(stt, Loc.T("Sonraki sezon ★ ", "Next season ★ ") + needNext + Loc.T("  ·  sende ", "  ·  you have ") + Save.TotalStars(), 22, Save.TotalStars() >= needNext ? UiKit.Green : UiKit.Amber, TextAnchor.MiddleRight, true, 1f, 0f, 100f, 36f, 1f, 0f);
                var nrt = nx.rectTransform.parent as RectTransform;
                nrt.anchorMin = new Vector2(0.56f, 0f); nrt.anchorMax = new Vector2(1f, 0f);
                nrt.offsetMin = new Vector2(12f, 14f); nrt.offsetMax = new Vector2(-24f, 50f);
            }

            BuildLevelDetail();
        }

        const float DetailWidth = 560f;

        /// <summary>Makes a Txt label span its parent's width at a fixed top offset and height.</summary>
        static void StretchTop(Text t, float top, float height)
        {
            var rt = t.rectTransform.parent as RectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(2f, top - height); rt.offsetMax = new Vector2(-2f, top);
        }

        static string KindIcon(ObjectiveKind k)
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

        void BuildLevelDetail()
        {
            var sel = Campaign.Get(selected);
            bool open = Campaign.IsOpen(selected);
            var detail = Frame(P, 1f, 1f, DetailWidth, 880f, 1f, 1f, 0f, -96f);
            var d = detail.transform;
            Move(Txt(d, Loc.T("BÖLÜM ", "LEVEL ") + selected + "  ·  " + sel.Subtitle, 22, UiKit.TextDim, TextAnchor.MiddleLeft, true, 0f, 1f, 520f, 40f, 0f, 1f), 28f, -16f);
            var ki = UiKit.IconImage(d, KindIcon(sel.Kind), 52f, HudController.KindColor(sel.Kind), 0f, 1f, 0f, 1f);
            ki.rectTransform.anchoredPosition = new Vector2(28f, -62f);
            Move(Txt(d, sel.Title, 38, UiKit.Amber, TextAnchor.MiddleLeft, true, 0f, 1f, 440f, 60f, 0f, 1f), 96f, -58f);
            Move(Txt(d, LevelConfig.KindName(sel.Kind), 22, HudController.KindColor(sel.Kind), TextAnchor.MiddleLeft, true, 0f, 1f, 440f, 30f, 0f, 1f), 98f, -108f);
            Move(Txt(d, sel.ObjectiveLine, 24, Color.white, TextAnchor.UpperLeft, false, 0f, 1f, 500f, 80f, 0f, 1f), 28f, -150f);

            string arenaName = ArenaName(sel.Arena);
            float ry = -236f;
            InfoRow(d, "globe", Loc.T("Arena", "Arena"), arenaName, ref ry);
            InfoRow(d, "skull", Loc.T("Rakip", "Rivals"), sel.Enemies.Length.ToString(), ref ry);
            InfoRow(d, "clock", Loc.T("Süre", "Time"), Mathf.RoundToInt(sel.TimeLimit) + Loc.T(" sn", " s"), ref ry);
            InfoRow(d, "scrap", Loc.T("Ödül", "Reward"), sel.Reward + Loc.T(" hurda", " scrap"), ref ry);
            InfoRow(d, "star", Loc.T("2. yıldız", "2nd star"), sel.StarTwoLine, ref ry);
            InfoRow(d, "shield", Loc.T("3. yıldız", "3rd star"), Loc.T("Canın en az yarım", "Finish with half health"), ref ry);
            Move(Txt(d, Loc.T("EN İYİ", "BEST"), 20, UiKit.TextDim, TextAnchor.MiddleLeft, true, 0f, 1f, 200f, 36f, 0f, 1f), 28f, ry - 4f);
            UiKit.Stars(d, Save.Data.Stars[selected], 34f, 0f, 1f, 0f, 1f, new Vector2(150f, ry - 2f));

            int go = selected;
            if (open)
            {
                var start2 = Btn(d, Loc.T("BAŞLAT", "START"), UiKit.Primary, 0.5f, 0f, 500f, 104f, 0.5f, 0f, 42, () => GameFlow.Play(go), true, "play");
                start2.Background.rectTransform.anchoredPosition = new Vector2(0f, 26f);
            }
            else
            {
                int need = Campaign.StarsRequired(selected);
                bool starGate = selected <= Save.Data.Unlocked && need > Save.TotalStars();
                string why = starGate
                    ? Loc.T("Bu bölüm için ★ " + need + " gerekiyor, sende " + Save.TotalStars() + ". Önceki bölümleri tekrar oynayıp daha çok yıldız kazan.",
                            "This level needs ★ " + need + ", you have " + Save.TotalStars() + ". Replay earlier levels for more stars.")
                    : Loc.T("Önce bir önceki bölümü tamamla.", "Clear the previous level first.");
                Move(Txt(d, why, 22, UiKit.Amber, TextAnchor.UpperLeft, false, 0f, 0f, 500f, 90f, 0f, 0f), 30f, 140f);
                var locked = Btn(d, starGate ? "★ " + need + Loc.T(" GEREK", " NEEDED") : Loc.T("KİLİTLİ", "LOCKED"), new Color(0.2f, 0.2f, 0.22f, 0.9f), 0.5f, 0f, 500f, 104f, 0.5f, 0f, 36, null, false, "lock");
                locked.Background.rectTransform.anchoredPosition = new Vector2(0f, 26f);
                locked.Interactable = false;
            }
        }

        void InfoRow(Transform parent, string icon, string label, string value, ref float y)
        {
            var ic = UiKit.IconImage(parent, icon, 26f, UiKit.TextDim, 0f, 1f, 0f, 1f);
            ic.rectTransform.anchoredPosition = new Vector2(30f, y - 6f);
            Move(Txt(parent, label, 22, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 1f, 160f, 38f, 0f, 1f), 68f, y);
            Move(Txt(parent, value, 22, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 330f, 38f, 0f, 1f), 220f, y);
            y -= 42f;
        }

        public static string ArenaName(ArenaKind a)
        {
            switch (a)
            {
                case ArenaKind.Colosseum: return Loc.T("Kolezyum", "Colosseum");
                case ArenaKind.Docks: return Loc.T("Konteyner Limanı", "Container Port");
                case ArenaKind.Ruins: return Loc.T("Çöl Harabeleri", "Desert Ruins");
                default: return Loc.T("Paslı Kanal", "Rust Channel");
            }
        }

        /// <summary>
        /// Roadmap info box: the campaign is built to grow with updates. Shows what is coming next, the
        /// planned additions and the online multiplayer goal (tied to player support / global rating),
        /// plus the player's star progress and a rating call to action.
        /// </summary>
        void BuildUpcomingSeason()
        {
            var panel = Frame(P, 0f, 1f, 1300f, 700f, 0f, 1f, 0f, -180f);
            var t = panel.transform;
            int season = Campaign.Seasons + 1;
            Move(Txt(t, Loc.T("YOL HARİTASI  ·  SIRADA NE VAR?", "ROADMAP  ·  WHAT'S NEXT"), 44, UiKit.Amber, TextAnchor.MiddleLeft, true, 0f, 1f, 1240f, 70f, 0f, 1f), 40f, -18f);

            float y = -100f;
            RoadmapEntry(t, ref y, UiKit.Amber,
                Loc.T("SEZON " + season + "  ·  YAKINDA", "SEASON " + season + "  ·  COMING SOON"),
                Loc.T("Yeni bir arena, yeni rakipler ve yeni görev türleriyle 20 bölüm daha. İlerlemen korunur.",
                      "Twenty more levels with a new arena, new rivals and new mission types. Your progress carries over."));
            RoadmapEntry(t, ref y, UiKit.Cyan,
                Loc.T("YENİ ARAÇLAR VE KAPLAMALAR  ·  PLANLANDI", "NEW CARS AND PAINT JOBS  ·  PLANNED"),
                Loc.T("Garaja yeni savaş araçları, özel kaplamalar ve daha derin yükseltmeler.",
                      "New combat vehicles, custom paint jobs and deeper upgrades in the garage."));
            RoadmapEntry(t, ref y, UiKit.Green,
                Loc.T("ÇEVRİM İÇİ PvP VE BATTLE ROYALE  ·  HEDEFİMİZ", "ONLINE PvP AND BATTLE ROYALE  ·  OUR GOAL"),
                Loc.T("Derby Arena sizden ilgi görür ve küresel puanı yükselirse, sıradaki büyük adım dünyanın dört bir yanındaki oyuncularla gerçek zamanlı derbiler ve daralan bölgede Battle Royale.",
                      "If Derby Arena earns your support and a strong global rating, our next big step is real-time derbies and a shrinking-zone Battle Royale against players worldwide."));

            int stars = Save.TotalStars();
            Move(Txt(t, Loc.T("YILDIZLARIN  ", "YOUR STARS  ") + stars + "/" + (Campaign.Total * 3), 26, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 400f, 44f, 0f, 1f), 40f, y - 6f);
            Image fill;
            var bar = UiKit.Bar(t, UiKit.Amber, out fill);
            UiKit.Place(bar.rectTransform, 0f, 1f, 760f, 30f, 0f, 1f);
            bar.rectTransform.anchoredPosition = new Vector2(450f, y - 14f);
            fill.fillAmount = Mathf.Clamp01(stars / (float)(Campaign.Total * 3));

            Move(Txt(t, Loc.T("Oyunu beğendiysen puan ver ve yorum yaz: her destek yeni içerikleri hızlandırır.",
                              "Enjoying the game? Rate it and leave a review: every bit of support brings new content sooner."),
                24, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 0f, 900f, 70f, 0f, 0f), 40f, 24f);
            var rate = Btn(t, Loc.T("PUAN VER", "RATE US"), UiKit.Primary, 1f, 0f, 300f, 84f, 1f, 0f, 30, OpenStorePage, true, "star");
            rate.Background.rectTransform.anchoredPosition = new Vector2(-30f, 24f);
        }

        void RoadmapEntry(Transform parent, ref float y, Color accent, string title, string body)
        {
            var dot = UiKit.Image("Dot", parent, UiKit.Circle, accent);
            UiKit.Place(dot.rectTransform, 0f, 1f, 22f, 22f, 0f, 1f);
            dot.rectTransform.anchoredPosition = new Vector2(40f, y - 12f);
            Move(Txt(parent, title, 30, accent, TextAnchor.MiddleLeft, true, 0f, 1f, 1180f, 44f, 0f, 1f), 76f, y);
            Move(Txt(parent, body, 24, Color.white, TextAnchor.UpperLeft, false, 0f, 1f, 1180f, 64f, 0f, 1f), 76f, y - 44f);
            y -= 124f;
        }

        public static void OpenStorePage()
        {
#if UNITY_ANDROID
            Application.OpenURL("market://details?id=" + Application.identifier);
#else
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
#endif
        }

        // ================================================================== garage
        void BuildGarage()
        {
            Header(Loc.T("GARAJ", "GARAGE"), "wrench");
            BuildVehiclePanel();
            BuildPartsPanel();
            if (!string.IsNullOrEmpty(garageMsg)) { Toast(garageMsg, UiKit.Green); garageMsg = ""; }
        }

        void BuildVehiclePanel()
        {
            vehicleIndex = Mathf.Clamp(vehicleIndex, 0, Fleet.Defs.Length - 1);
            var def = Fleet.Defs[vehicleIndex];
            bool owned = Fleet.Owned(def.Id);
            bool selectedNow = Save.Data.Vehicle == def.Id && owned;
            Fleet.PreviewId = owned ? null : def.Id;

            var panel = Frame(P, 0f, 1f, 600f, 560f, 0f, 1f, 0f, -96f);
            var t = panel.transform;
            Move(Txt(t, Loc.T("ARAÇ", "VEHICLE") + "  " + (vehicleIndex + 1) + "/" + Fleet.Defs.Length, 20, UiKit.TextDim, TextAnchor.MiddleLeft, true, 0f, 1f, 300f, 36f, 0f, 1f), 24f, -12f);
            var prev = Btn(t, "", UiKit.PanelLight, 1f, 1f, 64f, 56f, 1f, 1f, 20, () => { vehicleIndex = (vehicleIndex + Fleet.Defs.Length - 1) % Fleet.Defs.Length; Show("garage"); }, false, "back");
            prev.Background.rectTransform.anchoredPosition = new Vector2(-92f, -8f);
            prev.Caption.text = "";
            var next = Btn(t, "", UiKit.PanelLight, 1f, 1f, 64f, 56f, 1f, 1f, 20, () => { vehicleIndex = (vehicleIndex + 1) % Fleet.Defs.Length; Show("garage"); }, false, "back");
            next.Background.rectTransform.anchoredPosition = new Vector2(-20f, -8f);
            next.Caption.text = "";
            foreach (var im in next.Background.GetComponentsInChildren<Image>()) if (im.name.StartsWith("Icon_")) im.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            foreach (var im in prev.Background.GetComponentsInChildren<Image>()) if (im.name.StartsWith("Icon_")) im.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            foreach (var im in next.Background.GetComponentsInChildren<Image>()) if (im.name.StartsWith("Icon_")) im.rectTransform.anchoredPosition = new Vector2(44f, 0f);

            var vi = UiKit.IconImage(t, def.Tank ? "tank" : "car", 44f, UiKit.Amber, 0f, 1f, 0f, 1f);
            vi.rectTransform.anchoredPosition = new Vector2(24f, -58f);
            Move(Txt(t, def.Name, 32, UiKit.Amber, TextAnchor.MiddleLeft, true, 0f, 1f, 480f, 50f, 0f, 1f), 80f, -56f);
            Move(Txt(t, def.Blurb, 21, Color.white, TextAnchor.UpperLeft, false, 0f, 1f, 552f, 90f, 0f, 1f), 24f, -112f);

            float sy = -206f;
            StatRow(t, "gauge", Loc.T("HIZ", "SPEED"), def.Speed, ref sy);
            StatRow(t, "shield", Loc.T("ZIRH", "ARMOR"), def.Armor, ref sy);
            StatRow(t, "bullets", Loc.T("ATEŞ GÜCÜ", "FIREPOWER"), def.Firepower, ref sy);
            StatRow(t, "steering", Loc.T("YOL TUTUŞ", "HANDLING"), def.Handling, ref sy);

            // ownership / purchase
            string id = def.Id;
            if (selectedNow)
            {
                var b = Btn(t, Loc.T("SEÇİLİ ARAÇ", "SELECTED"), new Color(0.2f, 0.2f, 0.22f, 0.9f), 0.5f, 0f, 540f, 84f, 0.5f, 0f, 30, null, false, "check");
                b.Background.rectTransform.anchoredPosition = new Vector2(0f, 20f);
                b.Interactable = false;
            }
            else if (owned)
            {
                var b = Btn(t, Loc.T("BU ARACI SEÇ", "SELECT THIS VEHICLE"), UiKit.Primary, 0.5f, 0f, 540f, 84f, 0.5f, 0f, 30,
                    () => { Fleet.Select(id); if (Showcase.Instance != null) Showcase.Instance.Rebuild(); Show("garage"); }, true, "check");
                b.Background.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            }
            else
            {
                bool gates = Fleet.GatesOk(def);
                bool afford = Save.Data.Scrap >= def.Price;
                string gate = "";
                if (Save.TotalStars() < def.StarsNeeded) gate = "★ " + def.StarsNeeded + Loc.T(" GEREK", " NEEDED") + "  (" + Save.TotalStars() + ")";
                else if (Fleet.BossesBeaten() < def.BossesNeeded) gate = def.BossesNeeded + Loc.T(" PATRON YENİLMELİ", " BOSSES NEEDED") + "  (" + Fleet.BossesBeaten() + ")";
                if (!string.IsNullOrEmpty(gate))
                    Move(Txt(t, gate, 22, UiKit.Amber, TextAnchor.MiddleCenter, true, 0.5f, 0f, 540f, 36f, 0.5f, 0f), 0f, 112f);
                var b = Btn(t, Loc.T("SATIN AL  ·  ", "BUY  ·  ") + def.Price, gates ? (afford ? UiKit.Green : new Color(0.25f, 0.25f, 0.27f, 0.9f)) : new Color(0.25f, 0.25f, 0.27f, 0.9f), 0.5f, 0f, 540f, 84f, 0.5f, 0f, 30,
                    () =>
                    {
                        if (Fleet.TryBuy(id)) { garageMsg = Loc.T(def.NameTr + " GARAJDA!", def.NameEn + " IS YOURS!"); if (Showcase.Instance != null) Showcase.Instance.Rebuild(); Show("garage"); }
                        else Toast(Loc.T("Yeterli hurda yok.", "Not enough scrap."), UiKit.Red);
                    }, false, "scrap");
                b.Background.rectTransform.anchoredPosition = new Vector2(0f, 20f);
                b.Interactable = gates && afford;
                if (!gates) { b.Caption.color = UiKit.TextDim; }
            }
            if (Showcase.Instance != null) Showcase.Instance.Rebuild();
        }

        void StatRow(Transform parent, string icon, string label, int value, ref float y)
        {
            var ic = UiKit.IconImage(parent, icon, 26f, UiKit.TextDim, 0f, 1f, 0f, 1f);
            ic.rectTransform.anchoredPosition = new Vector2(26f, y - 6f);
            Move(Txt(parent, label, 20, UiKit.TextDim, TextAnchor.MiddleLeft, true, 0f, 1f, 190f, 38f, 0f, 1f), 64f, y);
            for (int i = 0; i < 5; i++)
            {
                var pip = UiKit.Image("Pip", parent, UiKit.RoundedSmall, i < value ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                UiKit.Place(pip.rectTransform, 0f, 1f, 58f, 16f, 0f, 1f);
                pip.rectTransform.anchoredPosition = new Vector2(250f + i * 64f, y - 11f);
            }
            y -= 40f;
        }

        void BuildPartsPanel()
        {
            // vertical tab strip at the right edge: upgrades + 8 cosmetic slots
            string[] tabIcons = { "wrench", "drop", "wheel", "bulb", "stripe", "roof", "spoiler", "bumper", "exhaust" };
            string[] tabNames =
            {
                Loc.T("YÜKSELT", "UPGRADE"), Garage.SlotName(PartSlot.Paint), Garage.SlotName(PartSlot.Rim), Garage.SlotName(PartSlot.Neon), Garage.SlotName(PartSlot.Stripe),
                Garage.SlotName(PartSlot.Roof), Garage.SlotName(PartSlot.Spoiler), Garage.SlotName(PartSlot.Bumper), Garage.SlotName(PartSlot.Exhaust)
            };
            var def = Fleet.Defs[Mathf.Clamp(vehicleIndex, 0, Fleet.Defs.Length - 1)];
            if (def.Tank && garageTab != 0 && garageTab != 3) garageTab = 0;   // tanks: upgrades and neon only
            for (int i = 0; i < tabIcons.Length; i++)
            {
                int ti = i;
                bool active = garageTab == i;
                bool allowed = !def.Tank || i == 0 || i == 3;
                var tb = Btn(P, "", active ? UiKit.Primary : (allowed ? UiKit.PanelLight : LockedTab), 1f, 1f, 76f, 90f, 1f, 1f, 14, () => { garageTab = ti; Show("garage"); }, active, null);
                tb.Background.rectTransform.anchoredPosition = new Vector2(0f, -96f - i * 98f);
                tb.Caption.text = "";
                var ic = UiKit.IconImage(tb.Background.transform, tabIcons[i], 38f, active ? DarkText : (allowed ? Color.white : UiKit.TextDim), 0.5f, 1f, 0.5f, 1f);
                ic.rectTransform.anchoredPosition = new Vector2(0f, -8f);
                Move(Txt(tb.Background.transform, tabNames[i], 13, active ? DarkText : (allowed ? UiKit.TextDim : new Color(1f, 1f, 1f, 0.3f)), TextAnchor.MiddleCenter, true, 0.5f, 0f, 76f, 30f, 0.5f, 0f), 0f, 4f);
                tb.Interactable = allowed;
            }

            var panel = Frame(P, 1f, 1f, 640f, 880f, 1f, 1f, -86f, -96f);
            var t = panel.transform;
            if (garageTab == 0) BuildUpgrades(t);
            else BuildSlot(t, (PartSlot)(garageTab - 1));
        }

        void BuildUpgrades(Transform t)
        {
            Move(Txt(t, Loc.T("YÜKSELTMELER", "UPGRADES"), 30, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 400f, 50f, 0f, 1f), 24f, -12f);
            float y = -72f;
            string[] icons = { "gauge", "shield", "bullets", "flame", "rocket" };
            for (int i = 0; i < Upgrades.Count; i++)
            {
                int type = i;
                int lvl = Upgrades.Level(i);
                var row = UiKit.Image("Row", t, UiKit.Card, UiKit.PanelLight);
                UiKit.Place(row.rectTransform, 0f, 1f, 600f, 118f, 0f, 1f);
                row.rectTransform.anchoredPosition = new Vector2(20f, y);
                var rt = row.transform;
                var ic = UiKit.IconImage(rt, icons[i], 40f, UiKit.Amber, 0f, 0.5f, 0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                Move(Txt(rt, Upgrades.Name(i), 28, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 300f, 44f, 0f, 1f), 74f, -8f);
                Move(Txt(rt, Upgrades.Description(i), 19, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 1f, 330f, 36f, 0f, 1f), 74f, -48f);
                for (int p = 0; p < Upgrades.MaxLevel; p++)
                {
                    var pip = UiKit.Image("Pip", rt, UiKit.RoundedSmall, p < lvl ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                    UiKit.Place(pip.rectTransform, 0f, 0f, 40f, 12f, 0f, 0f);
                    pip.rectTransform.anchoredPosition = new Vector2(76f + p * 46f, 14f);
                }
                bool max = lvl >= Upgrades.MaxLevel;
                int cost = Upgrades.Cost(i, lvl);
                bool afford = Save.Data.Scrap >= cost;
                var b = Btn(rt, max ? Loc.T("MAKS", "MAX") : cost.ToString(), max ? new Color(0.2f, 0.2f, 0.22f, 0.9f) : (afford ? UiKit.Green : new Color(0.25f, 0.25f, 0.27f, 0.9f)),
                    1f, 0.5f, 190f, 76f, 1f, 0.5f, 26, () =>
                    {
                        if (Upgrades.TryBuy(type)) { DerbyArena.Audio.AudioHub.Play2D(DerbyArena.Audio.SynthAudio.Pickup(), 0.7f, 1f); if (Showcase.Instance != null) Showcase.Instance.Rebuild(); }
                        Show("garage");
                    }, false, max ? "check" : "scrap");
                b.Background.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
                b.Interactable = !max && afford;
                y -= 128f;
            }

            // rewarded: free scrap, a few times per day. 4.0.2: the button follows the ad live (AdButtonLive) instead of
            // freezing on "ad loading…" when the ad arrived after the page was built
            var ads = DerbyArena.Ads.AdsManager.Instance;
            var free = Btn(t, Loc.T("REKLAM HAZIRLANIYOR…", "AD LOADING…"), new Color(0.25f, 0.25f, 0.27f, 0.9f), 0.5f, 0f, 600f, 70f, 0.5f, 0f, 22, null, false, "ad");
            free.Background.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            free.Interactable = false;
            var live = free.gameObject.AddComponent<AdButtonLive>();
            live.Button = free;
            live.Allowed = () => Save.RewardedScrapLeft(DerbyArena.Ads.AdsConfig.RewardedScrapPerDay) > 0;
            live.BlockedCaption = Loc.T("BUGÜNLÜK BİTTİ · YARIN TEKRAR", "DONE FOR TODAY · BACK TOMORROW");
            live.ReadyCaption = () =>
            {
                int left = Save.RewardedScrapLeft(DerbyArena.Ads.AdsConfig.RewardedScrapPerDay);
                return Loc.T("REKLAM İZLE · +" + DerbyArena.Ads.AdsConfig.RewardedScrapAmount + " HURDA (" + left + ")", "WATCH AD · +" + DerbyArena.Ads.AdsConfig.RewardedScrapAmount + " SCRAP (" + left + ")");
            };
            free.OnTap = () =>
            {
                if (ads == null || !ads.RewardedReady) return;
                garageMsg = "";
                live.Frozen = true;
                free.Interactable = false;
                ads.ShowRewarded(earned =>
                {
                    if (this == null) return;
                    if (earned)
                    {
                        Save.Data.Scrap += DerbyArena.Ads.AdsConfig.RewardedScrapAmount;
                        Save.CountRewardedScrap();
                        garageMsg = Loc.T("+" + DerbyArena.Ads.AdsConfig.RewardedScrapAmount + " HURDA EKLENDİ", "+" + DerbyArena.Ads.AdsConfig.RewardedScrapAmount + " SCRAP ADDED");
                    }
                    else garageMsg = Loc.T("Reklam tamamlanmadı, hurda eklenmedi.", "The ad was not completed, no scrap added.");
                    if (current == "garage") Show("garage");
                });
            };
        }

        void BuildSlot(Transform t, PartSlot slot)
        {
            Move(Txt(t, Garage.SlotName(slot), 30, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 400f, 50f, 0f, 1f), 24f, -12f);
            Move(Txt(t, Loc.T("Takılı: ", "Equipped: ") + Garage.EquippedSaved(slot).Name, 20, UiKit.TextDim, TextAnchor.MiddleRight, false, 1f, 1f, 360f, 50f, 1f, 1f), -24f, -12f);
            var items = Garage.Items(slot);
            float cardW = 296f, cardH = 96f;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                bool owned = Garage.Owned(it.Id);
                bool previewing = Garage.Preview != null && Garage.Preview.Id == it.Id;
                bool equipped = Garage.EquippedSaved(slot).Id == it.Id;
                bool starsOk = Garage.StarsOk(it);
                bool afford = Save.Data.Scrap >= it.Price;
                Color col = previewing ? new Color(0.1f, 0.22f, 0.3f, 0.96f) : (equipped ? new Color(0.25f, 0.18f, 0.08f, 0.96f) : UiKit.PanelLight);
                var card = UiKit.Image("Item", t, UiKit.Card, col);
                UiKit.Place(card.rectTransform, 0f, 1f, cardW, cardH, 0f, 1f);
                card.rectTransform.anchoredPosition = new Vector2(20f + (i % 2) * (cardW + 8f), -70f - (i / 2) * (cardH + 8f));
                var ct = card.transform;
                if ((equipped || previewing) && UiKit.CardRing != null)
                {
                    var ring = UiKit.Image("Ring", ct, UiKit.CardRing, previewing ? UiKit.Cyan : UiKit.Amber);
                    UiKit.Stretch(ring.rectTransform, 0f, 0f, 1f, 1f);
                }
                // swatch (colour slots) or part icon
                bool colourSlot = slot == PartSlot.Paint || slot == PartSlot.Rim || slot == PartSlot.Neon || slot == PartSlot.Stripe;
                if (colourSlot && it.Color.a > 0.5f)
                {
                    var sw = UiKit.Image("Swatch", ct, UiKit.Circle, it.Color);
                    UiKit.Place(sw.rectTransform, 0f, 0.5f, 52f, 52f, 0f, 0.5f);
                    sw.rectTransform.anchoredPosition = new Vector2(14f, 0f);
                    if (slot == PartSlot.Neon)
                    {
                        var gl = UiKit.Image("Glow", ct, UiKit.Radial, new Color(it.Color.r, it.Color.g, it.Color.b, 0.7f));
                        UiKit.Place(gl.rectTransform, 0f, 0.5f, 90f, 90f, 0f, 0.5f);
                        gl.rectTransform.anchoredPosition = new Vector2(-5f, 0f);
                        gl.transform.SetAsFirstSibling();
                    }
                }
                else
                {
                    string ico = slot == PartSlot.Roof ? "roof" : slot == PartSlot.Spoiler ? "spoiler" : slot == PartSlot.Bumper ? "bumper" : slot == PartSlot.Exhaust ? "exhaust" : slot == PartSlot.Neon ? "bulb" : "close";
                    var pi = UiKit.IconImage(ct, it.Style == 0 && (slot == PartSlot.Neon || slot == PartSlot.Stripe) ? "close" : ico, 44f, it.Style == 0 ? UiKit.TextDim : Color.white, 0f, 0.5f, 0f, 0.5f);
                    pi.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                }
                Move(Txt(ct, it.Name, 20, owned ? Color.white : (starsOk ? Color.white : UiKit.TextDim), TextAnchor.MiddleLeft, true, 0f, 1f, 200f, 40f, 0f, 1f), 78f, -10f);
                string status;
                Color sc;
                if (previewing) { status = Loc.T("ÖNİZLEME · ", "PREVIEW · ") + it.Price; sc = UiKit.Cyan; }
                else if (equipped) { status = Loc.T("TAKILI", "EQUIPPED"); sc = UiKit.Amber; }
                else if (owned) { status = Loc.T("SENİN", "OWNED"); sc = UiKit.Green; }
                else if (!starsOk) { status = "★ " + it.StarsNeeded + Loc.T(" GEREK", " NEEDED"); sc = UiKit.TextDim; }
                else { status = it.Price + Loc.T(" HURDA", " SCRAP"); sc = afford ? UiKit.Amber : UiKit.Red; }
                Move(Txt(ct, status, 18, sc, TextAnchor.MiddleLeft, true, 0f, 0f, 200f, 36f, 0f, 0f), 78f, 8f);
                if (!owned && !starsOk)
                {
                    var lk = UiKit.IconImage(ct, "lock", 22f, UiKit.TextDim, 1f, 0.5f, 1f, 0.5f);
                    lk.rectTransform.anchoredPosition = new Vector2(-12f, 0f);
                }

                var cb = card.gameObject.AddComponent<UiButton>();
                cb.Background = card; cb.BaseColor = col;
                var item = it;
                cb.OnTap = () =>
                {
                    if (Garage.Owned(item.Id))
                    {
                        Garage.Preview = null;
                        Garage.Equip(item.Id);
                        if (Showcase.Instance != null) Showcase.Instance.Rebuild();
                        Show("garage");
                    }
                    else if (Garage.Preview != null && Garage.Preview.Id == item.Id)
                    {
                        // second tap on the previewed item: buy
                        if (Garage.TryBuy(item.Id)) { Garage.Preview = null; garageMsg = item.Name + Loc.T(" SENİN!", " IS YOURS!"); DerbyArena.Audio.AudioHub.Play2D(DerbyArena.Audio.SynthAudio.Pickup(), 0.7f, 1f); }
                        else Toast(Garage.StarsOk(item) ? Loc.T("Yeterli hurda yok.", "Not enough scrap.") : Loc.T("Daha fazla yıldız gerekiyor.", "More stars needed."), UiKit.Red);
                        if (Showcase.Instance != null) Showcase.Instance.Rebuild();
                        Show("garage");
                    }
                    else
                    {
                        Garage.Preview = item;
                        if (Showcase.Instance != null) Showcase.Instance.Rebuild();
                        Show("garage");
                    }
                };
            }
            if (Garage.Preview != null && Garage.Preview.Slot == slot)
            {
                var pv = Garage.Preview;
                bool can = Garage.StarsOk(pv) && Save.Data.Scrap >= pv.Price;
                var buy = Btn(t, Loc.T("SATIN AL  ·  ", "BUY  ·  ") + pv.Price + "  ·  " + pv.Name, can ? UiKit.Green : new Color(0.25f, 0.25f, 0.27f, 0.9f), 0.5f, 0f, 600f, 76f, 0.5f, 0f, 22, () =>
                {
                    if (Garage.TryBuy(pv.Id)) { Garage.Preview = null; garageMsg = pv.Name + Loc.T(" SENİN!", " IS YOURS!"); DerbyArena.Audio.AudioHub.Play2D(DerbyArena.Audio.SynthAudio.Pickup(), 0.7f, 1f); }
                    if (Showcase.Instance != null) Showcase.Instance.Rebuild();
                    Show("garage");
                }, false, "scrap");
                buy.Background.rectTransform.anchoredPosition = new Vector2(0f, 18f);
                buy.Interactable = can;
                Move(Txt(t, Loc.T("Önizleme: araçta görüyorsun. Almak için dokun.", "Preview: shown on the car. Tap to buy."), 18, UiKit.TextDim, TextAnchor.MiddleCenter, false, 0.5f, 0f, 600f, 30f, 0.5f, 0f), 0f, 98f);
            }
        }

        // ================================================================== settings
        void BuildSettings()
        {
            Header(Loc.T("AYARLAR", "SETTINGS"), "gear");
            float y = -100f;
            string on = Loc.T("AÇIK", "ON"), off = Loc.T("KAPALI", "OFF");
            Row("globe", Loc.T("DİL", "LANGUAGE"), Loc.LanguageName, ref y, () => { Loc.Set(Loc.IsEnglish ? Loc.Turkish : Loc.English); });
            Row("steering", Loc.T("KONTROLLER", "CONTROLS"), ControlLayout.ModeName(ControlLayout.Mode) + "  ›", ref y, () => { }, "controls");
            Row("sound", Loc.T("SES", "SOUND"), Save.Data.Sound ? on : off, ref y, () => { Save.Data.Sound = !Save.Data.Sound; DerbyArena.Audio.AudioHub.SetSound(Save.Data.Sound); });
            Row("vibrate", Loc.T("TİTREŞİM", "VIBRATION"), Save.Data.Haptics ? on : off, ref y, () => { Save.Data.Haptics = !Save.Data.Haptics; });
            Row("bulb", Loc.T("PARLAKLIK", "BRIGHTNESS"), Mathf.RoundToInt(Save.Data.Brightness * 100f) + "%", ref y, () =>
            {
                float nb = Save.Data.Brightness + 0.1f;
                Save.Data.Brightness = nb > 1.45f ? 0.8f : nb;
                DerbyArena.World.Atmosphere.ApplyBrightness();
            });
            Row("clock", Loc.T("KARE HIZI", "FRAME RATE"), Save.Data.FrameRate + " FPS", ref y, () =>
            {
                int fr = Save.Data.FrameRate;
                Save.Data.FrameRate = fr < 60 ? 60 : (fr < 90 ? 90 : (fr < 120 ? 120 : 30));
                DerbyBootstrapQuality.Apply();
            });
            string[] q = { Loc.T("DÜŞÜK", "LOW"), Loc.T("ORTA", "MEDIUM"), Loc.T("YÜKSEK", "HIGH"), Loc.T("ULTRA", "ULTRA") };
            Row("gear", Loc.T("GRAFİK KALİTESİ", "GRAPHICS QUALITY"), q[Mathf.Clamp(Save.Data.Quality, 0, 3)], ref y, () => { Save.Data.Quality = (Save.Data.Quality + 1) % 4; Save.Data.QualityChosen = true; DerbyBootstrapQuality.Apply(); });
            Row("flame", Loc.T("EKRAN EFEKTLERİ", "SCREEN EFFECTS"), Save.Data.ScreenEffects ? on : off, ref y, () => { Save.Data.ScreenEffects = !Save.Data.ScreenEffects; });
            Row("info", Loc.T("DİNAMİK ÇÖZÜNÜRLÜK", "DYNAMIC RESOLUTION"), Save.Data.DynamicResolution ? on : off, ref y, () => { Save.Data.DynamicResolution = !Save.Data.DynamicResolution; DerbyBootstrapQuality.Apply(); });

            // right column: privacy, legal, rating, version
            float ry = -100f;
            SideButton("shield", Loc.T("GİZLİLİK POLİTİKASI", "PRIVACY POLICY"), ref ry, () => { legalDoc = "privacy"; Save.Data.PrivacySeen = true; Save.Write(); Show("legal"); });
            SideButton("mission", Loc.T("KULLANIM KOŞULLARI", "TERMS OF USE"), ref ry, () => { legalDoc = "terms"; Show("legal"); });
            SideButton("info", Loc.T("LİSANSLAR VE KAYNAKLAR", "LICENSES AND CREDITS"), ref ry, () => { legalDoc = "licenses"; Show("legal"); });
            var ads = DerbyArena.Ads.AdsManager.Instance;
            if (ads != null && ads.PrivacyOptionsRequired)
                SideButton("ad", Loc.T("REKLAM GİZLİLİK SEÇENEKLERİ", "AD PRIVACY OPTIONS"), ref ry, () => ads.ShowPrivacyOptions(err => { if (this != null && current == "settings") Show("settings"); }));
            SideButton("star", Loc.T("OYUNU DEĞERLENDİR", "RATE THE GAME"), ref ry, () => { Save.Data.RateState = 1; Save.Write(); OpenStorePage(); });
            var adsMgr = DerbyArena.Ads.AdsManager.Instance;
            if (DerbyArena.Ads.AdsConfig.TestMode)
            {
                var adLine = Txt(P, Loc.T("Reklam: ", "Ads: ") + (adsMgr != null ? adsMgr.Describe() : "-"), 18, UiKit.TextDim, TextAnchor.UpperRight, false, 1f, 1f, 520f, 80f, 1f, 1f);
                Move(adLine, 0f, ry - 4f);
            }
            Txt(P, Loc.T("Sürüm ", "Version ") + DerbyStartupDiagnostics.Version + " (" + DerbyStartupDiagnostics.VersionCode + ")", 20, new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerRight, false, 1f, 0f, 520f, 40f, 1f, 0f);
        }

        void SideButton(string icon, string caption, ref float y, Action a)
        {
            var b = Btn(P, caption, UiKit.PanelLight, 1f, 1f, 520f, 72f, 1f, 1f, 24, a, false, icon);
            b.Background.rectTransform.anchoredPosition = new Vector2(0f, y);
            b.Caption.color = UiKit.Cyan;
            y -= 82f;
        }

        // ================================================================== legal (offline summary + web link)
        void BuildLegal()
        {
            string title = legalDoc == "terms" ? Loc.T("KULLANIM KOŞULLARI", "TERMS OF USE")
                         : legalDoc == "licenses" ? Loc.T("LİSANSLAR", "LICENSES") : Loc.T("GİZLİLİK", "PRIVACY");
            var back = Btn(P, Loc.T("GERİ", "BACK"), UiKit.PanelLight, 0f, 1f, 200f, 78f, 0f, 1f, 28, () => Show("settings"), false, "back");
            back.IgnoreBlock = true;
            Move(Txt(P, title, 50, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 900f, 78f, 0f, 1f), 240f, 0f);
            var panel = UiKit.Frame("Doc", P, UiKit.Panel);
            UiKit.Stretch(panel.rectTransform, 0f, 0f, 1f, 1f);
            panel.rectTransform.offsetMin = new Vector2(0f, 110f);
            panel.rectTransform.offsetMax = new Vector2(0f, -96f);
            string text = legalDoc == "terms" ? Legal.TermsSummary() : legalDoc == "licenses" ? Legal.LicensesSummary() : Legal.PrivacySummary();
            var body = UiKit.Rect("Body", panel.transform);
            UiKit.Stretch(body, 0f, 0f, 1f, 1f);
            body.offsetMin = new Vector2(36f, 24f);
            body.offsetMax = new Vector2(-36f, -24f);
            var label = UiKit.Label("Text", body, text, 22, Color.white, TextAnchor.UpperLeft, false);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 24;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            string url = legalDoc == "terms" ? Legal.TermsUrl : legalDoc == "licenses" ? Legal.LicensesUrl : Legal.PrivacyUrl;
            var open = Btn(P, Loc.T("TAM METNİ WEB'DE AÇ", "OPEN FULL TEXT ON THE WEB"), UiKit.Primary, 0.5f, 0f, 560f, 86f, 0.5f, 0f, 26, () => Legal.Open(url), true, "globe");
            Txt(P, Loc.T("Son güncelleme: ", "Last updated: ") + Legal.Updated + "  ·  " + Loc.T("Geliştirici: ", "Developer: ") + Legal.Developer, 20, UiKit.TextDim, TextAnchor.LowerRight, false, 1f, 0f, 700f, 40f, 1f, 0f);
        }

        void Row(string icon, string label, string value, ref float y, Action change) { Row(icon, label, value, ref y, change, current); }

        /// <summary>Width of a full page in canvas units: the canvas is 1080 high (match height), minus the safe-area
        /// insets and the 12-unit page margins. Computed from the screen, so it is right before the first layout pass.</summary>
        static float PageWidth()
        {
            float h = Mathf.Max(1f, Screen.height);
            return Mathf.Max(800f, Screen.safeArea.width * 1080f / h - 24f);
        }

        void Row(string icon, string label, string value, ref float y, Action change, string pageAfter)
        {
            // 4.0.2: the rows leave room for the 520-wide right column on every screen shape (16:10 and 4:3 tablets
            // used to run the rows under the privacy / reset buttons); at most 1180 wide as before
            float rowW = Mathf.Clamp(PageWidth() - 560f, 700f, 1180f);
            float valueW = Mathf.Min(420f, rowW * 0.36f);
            var row = UiKit.Image("Row", P, UiKit.Card, UiKit.Panel);
            UiKit.Place(row.rectTransform, 0f, 1f, rowW, 66f, 0f, 1f);
            row.rectTransform.anchoredPosition = new Vector2(0f, y);
            var ic = UiKit.IconImage(row.transform, icon, 30f, UiKit.TextDim, 0f, 0.5f, 0f, 0.5f);
            ic.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            Move(Txt(row.transform, label, 26, Color.white, TextAnchor.MiddleLeft, true, 0f, 0.5f, rowW - valueW - 96f, 64f, 0f, 0.5f), 66f, 0f);
            var b = Btn(row.transform, value, UiKit.PanelLight, 1f, 0.5f, valueW, 54f, 1f, 0.5f, 24, () => { change(); Save.Write(); Show(pageAfter); });
            b.Background.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
            b.Caption.color = UiKit.Amber;
            y -= 72f;
        }

        // ================================================================== controls (4.0.1: PUBG Mobile style)
        void BuildControlsPage()
        {
            Header(Loc.T("KONTROLLER", "CONTROLS"), "steering");
            var back = P.GetComponentInChildren<UiButton>();
            if (back != null) back.OnTap = () => Show("settings");
            float y = -100f;
            string on = Loc.T("AÇIK", "ON"), off = Loc.T("KAPALI", "OFF");
            Row("steering", Loc.T("DİREKSİYON", "STEERING"), ControlLayout.ModeName(ControlLayout.Mode), ref y, () => { Save.Data.ControlMode = (Save.Data.ControlMode + 1) % 4; });
            Row("gauge", Loc.T("HASSASİYET", "SENSITIVITY"), Mathf.RoundToInt(Save.Data.Sensitivity * 100f) + "%", ref y, () =>
            {
                float s = Save.Data.Sensitivity + 0.1f;
                Save.Data.Sensitivity = s > 1.81f ? 0.5f : s;
            });
            Row("play", Loc.T("OTOMATİK GAZ", "AUTO GAS"), Save.Data.AutoGas ? on : off, ref y, () => { Save.Data.AutoGas = !Save.Data.AutoGas; });
            Row("wheel", Loc.T("SÜRÜŞ YARDIMI", "DRIVING AID"), Save.Data.SteerAssist ? on : off, ref y, () => { Save.Data.SteerAssist = !Save.Data.SteerAssist; });
            Row("crosshair", Loc.T("OTOMATİK NİŞAN", "AUTO AIM"), Save.Data.AutoAim ? Loc.T("GENİŞ", "WIDE") : Loc.T("DAR", "NARROW"), ref y, () => { Save.Data.AutoAim = !Save.Data.AutoAim; });
            Row("gear", Loc.T("DÜĞME BOYUTU", "BUTTON SIZE"), Mathf.RoundToInt(Save.Data.ControlScale * 100f) + "%", ref y, () =>
            {
                float s = Save.Data.ControlScale + 0.1f;
                Save.Data.ControlScale = s > 1.41f ? 0.7f : s;
            });
            Row("bulb", Loc.T("DÜĞME SAYDAMLIĞI", "BUTTON OPACITY"), Mathf.RoundToInt(Save.Data.ControlAlpha * 100f) + "%", ref y, () =>
            {
                float a = Save.Data.ControlAlpha + 0.1f;
                Save.Data.ControlAlpha = a > 1.01f ? 0.4f : a;
            });
            Row("car", Loc.T("DÜZEN", "LAYOUT"), ControlLayout.PresetName(Save.Data.ControlPreset), ref y, () =>
            {
                int next = Save.Data.ControlPreset == ControlLayout.Custom ? ControlLayout.Classic : (Save.Data.ControlPreset + 1) % 3;
                ControlLayout.ApplyPreset(next);
            });
            Row("vibrate", Loc.T("TİTREŞİM", "VIBRATION"), Save.Data.Haptics ? on : off, ref y, () => { Save.Data.Haptics = !Save.Data.Haptics; });

            // right column: the editor, the mode hint and a full reset
            var edit = Btn(P, Loc.T("DÜZENİ ÖZELLEŞTİR", "CUSTOMISE LAYOUT"), UiKit.Primary, 1f, 1f, 520f, 96f, 1f, 1f, 26, () =>
            {
                ControlLayoutEditor.Open(Fleet.Current().Tank, () => { if (this != null) Show("controls"); });
            }, true, "steering");
            edit.Background.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            var card = UiKit.Frame("Hint", P, UiKit.Panel);
            UiKit.Place(card.rectTransform, 1f, 1f, 520f, 300f, 1f, 1f);
            card.rectTransform.anchoredPosition = new Vector2(0f, -212f);
            var body = UiKit.Rect("Body", card.transform);
            UiKit.Stretch(body, 0f, 0f, 1f, 1f);
            body.offsetMin = new Vector2(22f, 18f);
            body.offsetMax = new Vector2(-22f, -18f);
            string text = ControlLayout.ModeName(ControlLayout.Mode) + "\n" + ControlLayout.ModeHint(ControlLayout.Mode) + "\n\n" +
                          Loc.T("Düğmeleri sürükleyip yerini ve boyutunu değiştirebilirsin (PUBG Mobile mantığı). Otomatik gaz açıkken sadece direksiyon, fren ve ateşle ilgilenirsin.",
                                "Drag any button to move it and change its size (PUBG Mobile style). With auto gas on you only steer, brake and shoot.");
            var lbl = UiKit.Label("Text", body, text, 21, Color.white, TextAnchor.UpperLeft, false);
            lbl.resizeTextForBestFit = true;
            lbl.resizeTextMinSize = 14;
            lbl.resizeTextMaxSize = 22;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;
            var reset = Btn(P, Loc.T("VARSAYILANA DÖN", "RESET TO DEFAULT"), UiKit.PanelLight, 1f, 1f, 520f, 72f, 1f, 1f, 24, () =>
            {
                ControlLayout.ResetAll();
                Save.Write();
                Show("controls");
                Toast(Loc.T("Kontroller sıfırlandı.", "Controls reset."), UiKit.Green);
            }, false, "wrench");
            reset.Background.rectTransform.anchoredPosition = new Vector2(0f, -526f);
        }
    }
}
