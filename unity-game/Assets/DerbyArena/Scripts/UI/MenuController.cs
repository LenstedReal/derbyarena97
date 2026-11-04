using System;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// Main menu over a live 3D arena: language picker (first launch), home, level select (seasons of 10 levels
    /// plus the upcoming-season teaser), garage and settings. Every visible string is Turkish / English via Loc.
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

        static readonly Color DarkText = new Color(0.08f, 0.06f, 0.03f);
        static readonly Color LockedTab = new Color(0.08f, 0.08f, 0.09f, 0.9f);

        public static MenuController Create(string startPage)
        {
            var go = new GameObject("Menu");
            var m = go.AddComponent<MenuController>();
            m.canvasRt = UiKit.Canvas("MenuCanvas", 20, out m.safe);
            m.canvasRt.SetParent(go.transform, false);
            var vig = UiKit.Image("Vignette", m.canvasRt, UiKit.VignetteSprite, new Color(0f, 0f, 0f, 0.5f));
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 1f, 1f);
            vig.transform.SetAsFirstSibling();
            m.selected = Mathf.Clamp(Save.Data.LastPlayed, 1, Save.Data.Unlocked);
            m.chapter = (m.selected - 1) / Campaign.PerChapter + 1;
            m.Show(Save.Data.LanguageChosen ? startPage : "language");
            return m;
        }

        void Update()
        {
            // Android back button (reported as Escape by the Input System): one level up, never quits silently.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (current == "legal") Show("settings");
            else if (current != "home" && current != "language") Show("home");
        }

        void Show(string name)
        {
            current = name;
            if (page != null) Destroy(page);
            page = UiKit.Rect("Page_" + name, safe).gameObject;
            UiKit.Stretch(page.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            switch (name)
            {
                case "language": BuildLanguage(); break;
                case "levels": BuildLevels(); break;
                case "garage": BuildGarage(); break;
                case "settings": BuildSettings(); break;
                case "legal": BuildLegal(); break;
                default: BuildHome(); break;
            }
        }

        Transform P { get { return page.transform; } }

        UiButton Btn(Transform parent, string caption, Color color, float ax, float ay, float w, float h, float px, float py, int font, Action a)
        {
            var b = UiKit.Button(parent, caption, color, font, a);
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

        void Header(string title)
        {
            var back = Btn(P, Loc.T("< GERİ", "< BACK"), UiKit.PanelLight, 0f, 1f, 210f, 84f, 0f, 1f, 32, () => Show("home"));
            Move(Txt(P, title, 56, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 900f, 84f, 0f, 1f), 240f, 0f);
            var chip = UiKit.Image("Scrap", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(chip.rectTransform, 1f, 1f, 330f, 84f, 1f, 1f);
            UiKit.Label("Text", chip.transform, Loc.T("HURDA  ", "SCRAP  ") + Save.Data.Scrap, 34, UiKit.Amber, TextAnchor.MiddleCenter, true);
            back.IgnoreBlock = true;
        }

        // ================================================================== language
        void BuildLanguage()
        {
            var shade = UiKit.Image("Shade", P, UiKit.Square, new Color(0f, 0f, 0f, 0.55f));
            UiKit.Stretch(shade.rectTransform, 0f, 0f, 1f, 1f);
            var panel = UiKit.Image("Panel", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0.5f, 0.5f, 940f, 560f, 0.5f, 0.5f);
            var t = panel.transform;
            Move(Txt(t, "DİL SEÇ  ·  CHOOSE LANGUAGE", 44, UiKit.Amber, TextAnchor.MiddleCenter, true, 0.5f, 1f, 900f, 110f, 0.5f, 1f), 0f, -30f);
            bool en = Loc.IsEnglish;
            var tr = Btn(t, "TÜRKÇE", en ? UiKit.PanelLight : UiKit.Amber, 0.5f, 0.5f, 400f, 150f, 0.5f, 0.5f, 46, () => PickLanguage(Loc.Turkish));
            tr.Background.rectTransform.anchoredPosition = new Vector2(-215f, -20f);
            var eng = Btn(t, "ENGLISH", en ? UiKit.Amber : UiKit.PanelLight, 0.5f, 0.5f, 400f, 150f, 0.5f, 0.5f, 46, () => PickLanguage(Loc.English));
            eng.Background.rectTransform.anchoredPosition = new Vector2(215f, -20f);
            if (en) eng.Caption.color = DarkText;
            else tr.Caption.color = DarkText;
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
            var shade = UiKit.Image("Shade", P, UiKit.Square, new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(shade.rectTransform, 0f, 0f, 1f, 1f);
            var side = UiKit.Image("Side", P, UiKit.Rounded, new Color(0.03f, 0.035f, 0.045f, 0.78f));
            UiKit.Stretch(side.rectTransform, 0f, 0f, 0.42f, 1f);

            Move(Txt(P, "DERBY ARENA", 104, UiKit.Amber, TextAnchor.UpperLeft, true, 0f, 1f, 800f, 130f, 0f, 1f), 40f, -40f);
            Move(Txt(P, "BLACKOUT RUN", 40, UiKit.Cyan, TextAnchor.UpperLeft, true, 0f, 1f, 800f, 60f, 0f, 1f), 46f, -168f);

            int next = Mathf.Clamp(Save.Data.Unlocked, 1, Campaign.Total);
            int play = Save.Data.Stars[next] > 0 ? Save.Data.LastPlayed : next;
            var cfg = Campaign.Get(play);
            float y = -270f;
            var main = Btn(P, Loc.T("OYNA  ·  BÖLÜM ", "PLAY  ·  LEVEL ") + play, UiKit.Amber, 0f, 1f, 640f, 130f, 0f, 1f, 46, () => GameFlow.Play(play));
            main.Background.rectTransform.anchoredPosition = new Vector2(40f, y);
            main.Caption.color = DarkText;
            string under = Campaign.AllCleared
                ? Loc.T("TÜM BÖLÜMLER TAMAM · YENİ SEZON YAKINDA", "ALL LEVELS CLEARED · NEW SEASON COMING SOON")
                : cfg.Title + "  ·  " + cfg.Subtitle;
            Move(Txt(P, under, 26, Campaign.AllCleared ? UiKit.Amber : UiKit.TextDim, TextAnchor.UpperLeft, false, 0f, 1f, 640f, 40f, 0f, 1f), 44f, y - 140f);

            y -= 200f;
            string[] names = { Loc.T("BÖLÜMLER", "LEVELS"), Loc.T("GARAJ", "GARAGE"), Loc.T("AYARLAR", "SETTINGS") };
            string[] pages = { "levels", "garage", "settings" };
            for (int i = 0; i < names.Length; i++)
            {
                string pg = pages[i];
                var b = Btn(P, names[i], UiKit.PanelLight, 0f, 1f, 640f, 100f, 0f, 1f, 38, () => Show(pg));
                b.Background.rectTransform.anchoredPosition = new Vector2(40f, y);
                y -= 118f;
            }

            var chips = UiKit.Image("Stats", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(chips.rectTransform, 1f, 1f, 680f, 90f, 1f, 1f);
            UiKit.Label("Text", chips.transform,
                Loc.T("HURDA ", "SCRAP ") + Save.Data.Scrap + Loc.T("    YILDIZ ", "    STARS ") + Save.TotalStars() + "/" + (Campaign.Total * 3)
                + Loc.T("    BÖLÜM ", "    LEVEL ") + Save.Data.Unlocked + "/" + Campaign.Total,
                28, Color.white, TextAnchor.MiddleCenter, true);
            var lang = Btn(P, Loc.T("DİL: ", "LANGUAGE: ") + Loc.LanguageName, UiKit.PanelLight, 1f, 1f, 420f, 76f, 1f, 1f, 28, () => Show("language"));
            lang.Background.rectTransform.anchoredPosition = new Vector2(0f, -104f);

            var art = Resources.Load<Texture2D>("Derby/keyart");
            if (art != null)
            {
                var frame = UiKit.Image("KeyArtFrame", P, UiKit.Rounded, new Color(1f, 0.55f, 0.15f, 0.9f));
                UiKit.Place(frame.rectTransform, 1f, 0f, 330f, 330f, 1f, 0f);
                frame.rectTransform.anchoredPosition = new Vector2(-10f, 70f);
                var ago = new GameObject("KeyArt", typeof(RectTransform));
                ago.transform.SetParent(frame.transform, false);
                var raw = ago.AddComponent<RawImage>();
                raw.texture = art;
                raw.raycastTarget = false;
                UiKit.Stretch(raw.rectTransform, 0f, 0f, 1f, 1f);
                raw.rectTransform.offsetMin = new Vector2(6f, 6f);
                raw.rectTransform.offsetMax = new Vector2(-6f, -6f);
            }

            // the game keeps growing: upcoming season badge (opens the teaser in the level list)
            var soon = Btn(P, Loc.T("YENİ SEZON · YOL HARİTASI", "NEW SEASON · ROADMAP"), new Color(1f, 0.55f, 0.15f, 0.95f), 1f, 0f, 330f, 64f, 1f, 0f, 22,
                () => { chapter = Campaign.Seasons + 1; Show("levels"); });
            soon.Background.rectTransform.anchoredPosition = new Vector2(-10f, 412f);
            soon.Caption.color = DarkText;

            Txt(P, "v" + DerbyStartupDiagnostics.Version, 22, new Color(1f, 1f, 1f, 0.35f), TextAnchor.LowerRight, false, 1f, 0f, 300f, 40f, 1f, 0f);
        }

        // ================================================================== levels
        void BuildLevels()
        {
            Header(Loc.T("BÖLÜMLER", "LEVELS"));
            int tabs = Campaign.Seasons + 1;   // + upcoming season
            for (int c = 1; c <= tabs; c++)
            {
                int cc = c;
                bool upcoming = c > Campaign.Seasons;
                int first = (c - 1) * Campaign.PerChapter + 1;
                bool open = upcoming || Save.Data.Unlocked >= first;
                string caption = upcoming ? Loc.T("YAKINDA · YOL HARİTASI", "COMING SOON · ROADMAP") : c + ". " + Campaign.ChapterName(c);
                Color col = c == chapter ? UiKit.Amber : (upcoming ? new Color(0.35f, 0.22f, 0.08f, 0.92f) : (open ? UiKit.PanelLight : LockedTab));
                var tab = Btn(P, caption, col, 0f, 1f, 300f, 70f, 0f, 1f, 20, () =>
                {
                    chapter = cc;
                    if (cc <= Campaign.Seasons)
                    {
                        int f = (cc - 1) * Campaign.PerChapter + 1;
                        selected = Mathf.Clamp(Mathf.Max(f, Mathf.Min(Save.Data.Unlocked, f + Campaign.PerChapter - 1)), 1, Campaign.Total);
                    }
                    Show("levels");
                });
                tab.Background.rectTransform.anchoredPosition = new Vector2((c - 1) * 310f, -100f);
                if (c == chapter) tab.Caption.color = DarkText;
                tab.Interactable = open;
            }

            if (chapter > Campaign.Seasons)
            {
                BuildUpcomingSeason();
                return;
            }

            int start = (chapter - 1) * Campaign.PerChapter + 1;
            for (int i = 0; i < Campaign.PerChapter; i++)
            {
                int n = start + i;
                var cfg = Campaign.Get(n);
                bool locked = n > Save.Data.Unlocked;
                Color col = locked ? new Color(0.06f, 0.06f, 0.07f, 0.85f) : (n == selected ? new Color(0.22f, 0.17f, 0.08f, 0.95f) : UiKit.Panel);
                int nn = n;
                var card = Btn(P, "", col, 0f, 1f, 240f, 190f, 0f, 1f, 20, () => { selected = nn; Show("levels"); });
                card.Background.rectTransform.anchoredPosition = new Vector2((i % 5) * 256f, -196f - (i / 5) * 206f);
                card.Interactable = !locked;
                var t = card.Background.transform;
                Color typeCol = HudController.KindColor(cfg.Kind);
                var num = UiKit.Rect("Num", t);
                UiKit.Stretch(num, 0f, 0.42f, 1f, 1f);
                UiKit.Label("Label", num, locked ? Loc.T("KİLİTLİ", "LOCKED") : n.ToString(), locked ? 30 : 64, locked ? UiKit.TextDim : Color.white, TextAnchor.MiddleCenter, true);
                var kind = UiKit.Rect("Kind", t);
                UiKit.Stretch(kind, 0f, 0.2f, 1f, 0.42f);
                UiKit.Label("Label", kind, LevelConfig.KindName(cfg.Kind), 22, locked ? UiKit.TextDim : typeCol, TextAnchor.MiddleCenter, true);
                int stars = Save.Data.Stars[n];
                for (int s = 0; s < 3; s++)
                {
                    var dot = UiKit.Image("Star", t, UiKit.Circle, s < stars ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                    UiKit.Place(dot.rectTransform, 0.5f, 0f, 22f, 22f, 0.5f, 0f);
                    dot.rectTransform.anchoredPosition = new Vector2((s - 1) * 32f, 16f);
                }
            }

            Move(Txt(P, Campaign.ChapterFeature(chapter), 24, UiKit.Cyan, TextAnchor.UpperLeft, false, 0f, 1f, 1270f, 70f, 0f, 1f), 4f, -812f);

            var sel = Campaign.Get(selected);
            var detail = UiKit.Image("Detail", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(detail.rectTransform, 1f, 1f, 560f, 600f, 1f, 1f);
            detail.rectTransform.anchoredPosition = new Vector2(0f, -196f);
            var d = detail.transform;
            Move(Txt(d, Loc.T("BÖLÜM ", "LEVEL ") + selected, 30, UiKit.TextDim, TextAnchor.MiddleLeft, true, 0f, 1f, 500f, 50f, 0f, 1f), 30f, -20f);
            Move(Txt(d, sel.Title, 42, UiKit.Amber, TextAnchor.MiddleLeft, true, 0f, 1f, 520f, 60f, 0f, 1f), 30f, -70f);
            string arenaName = sel.Arena == ArenaKind.Colosseum ? Loc.T("Kolezyum", "Colosseum") : "Blackout Spillway";
            string info = LevelConfig.KindName(sel.Kind) + "\n" + sel.ObjectiveLine
                        + "\n\nArena: " + arenaName
                        + Loc.T("\nRakip: ", "\nRivals: ") + sel.Enemies.Length
                        + Loc.T("    Süre: ", "    Time: ") + Mathf.RoundToInt(sel.TimeLimit) + Loc.T(" sn", " s")
                        + Loc.T("\nÖdül: ", "\nReward: ") + sel.Reward + Loc.T(" hurda", " scrap")
                        + Loc.T("\nEn iyi: ", "\nBest: ") + Save.Data.Stars[selected] + Loc.T("/3 yıldız", "/3 stars")
                        + Loc.T("\n2. yıldız: ", "\n2nd star: ") + sel.StarTwoLine;
            Move(Txt(d, info, 26, Color.white, TextAnchor.UpperLeft, false, 0f, 1f, 500f, 320f, 0f, 1f), 30f, -150f);
            int go = selected;
            var start2 = Btn(d, Loc.T("BAŞLAT", "START"), UiKit.Green, 0.5f, 0f, 480f, 110f, 0.5f, 0f, 46, () => GameFlow.Play(go));
            start2.Background.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        }

        /// <summary>
        /// Roadmap info box: the campaign is built to grow with updates. Shows what is coming next, the
        /// planned additions and the online multiplayer goal (tied to player support / global rating),
        /// plus the player's star progress and a rating call to action.
        /// </summary>
        void BuildUpcomingSeason()
        {
            var panel = UiKit.Image("Roadmap", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(panel.rectTransform, 0f, 1f, 1300f, 640f, 0f, 1f);
            panel.rectTransform.anchoredPosition = new Vector2(0f, -190f);
            var t = panel.transform;
            int season = Campaign.Seasons + 1;
            Move(Txt(t, Loc.T("YOL HARİTASI  ·  SIRADA NE VAR?", "ROADMAP  ·  WHAT'S NEXT"), 46, UiKit.Amber, TextAnchor.MiddleLeft, true, 0f, 1f, 1240f, 70f, 0f, 1f), 40f, -18f);

            float y = -100f;
            RoadmapEntry(t, ref y, UiKit.Amber,
                Loc.T("SEZON " + season + "  ·  YAKINDA", "SEASON " + season + "  ·  COMING SOON"),
                Loc.T("Yeni bir arena, yeni rakipler ve yeni görev türleriyle 10 bölüm daha. İlerlemen korunur.",
                      "Ten more levels with a new arena, new rivals and new mission types. Your progress carries over."));
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
            var rate = Btn(t, Loc.T("PUAN VER", "RATE US"), UiKit.Amber, 1f, 0f, 300f, 84f, 1f, 0f, 32, OpenStorePage);
            rate.Background.rectTransform.anchoredPosition = new Vector2(-30f, 24f);
            rate.Caption.color = DarkText;
        }

        void RoadmapEntry(Transform parent, ref float y, Color accent, string title, string body)
        {
            var dot = UiKit.Image("Dot", parent, UiKit.Circle, accent);
            UiKit.Place(dot.rectTransform, 0f, 1f, 22f, 22f, 0f, 1f);
            dot.rectTransform.anchoredPosition = new Vector2(40f, y - 12f);
            Move(Txt(parent, title, 30, accent, TextAnchor.MiddleLeft, true, 0f, 1f, 1180f, 44f, 0f, 1f), 76f, y);
            Move(Txt(parent, body, 24, Color.white, TextAnchor.UpperLeft, false, 0f, 1f, 1180f, 64f, 0f, 1f), 76f, y - 44f);
            y -= 118f;
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
            Header(Loc.T("GARAJ", "GARAGE"));
            for (int i = 0; i < Upgrades.Count; i++)
            {
                int type = i;
                int lvl = Upgrades.Level(i);
                var row = UiKit.Image("Row", P, UiKit.Rounded, UiKit.Panel);
                UiKit.Place(row.rectTransform, 0f, 1f, 1300f, 130f, 0f, 1f);
                row.rectTransform.anchoredPosition = new Vector2(0f, -110f - i * 145f);
                var t = row.transform;
                Move(Txt(t, Upgrades.Name(i), 40, Color.white, TextAnchor.MiddleLeft, true, 0f, 0.5f, 300f, 60f, 0f, 0f), 30f, 0f);
                Move(Txt(t, Upgrades.Description(i), 24, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 0.5f, 420f, 50f, 0f, 1f), 30f, 0f);
                for (int p = 0; p < Upgrades.MaxLevel; p++)
                {
                    var pip = UiKit.Image("Pip", t, UiKit.RoundedSmall, p < lvl ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                    UiKit.Place(pip.rectTransform, 0f, 0.5f, 60f, 26f, 0f, 0.5f);
                    pip.rectTransform.anchoredPosition = new Vector2(500f + p * 72f, 0f);
                }
                bool max = lvl >= Upgrades.MaxLevel;
                int cost = Upgrades.Cost(i, lvl);
                bool afford = Save.Data.Scrap >= cost;
                var b = Btn(t, max ? Loc.T("MAKS", "MAX") : cost + Loc.T(" HURDA", " SCRAP"), max ? UiKit.PanelLight : (afford ? UiKit.Green : new Color(0.25f, 0.25f, 0.27f, 0.9f)),
                    1f, 0.5f, 330f, 96f, 1f, 0.5f, 32, () =>
                    {
                        if (Upgrades.TryBuy(type)) DerbyArena.Audio.AudioHub.Play2D(DerbyArena.Audio.SynthAudio.Pickup(), 0.7f, 1f);
                        Show("garage");
                    });
                b.Background.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
                b.Interactable = !max && afford;
            }
            Txt(P, Loc.T("Hurda kazanmak için bölümleri tamamla ve rakipleri imha et.", "Finish levels and destroy rivals to earn scrap."), 26, UiKit.Cyan, TextAnchor.MiddleLeft, false, 0f, 0f, 1300f, 50f, 0f, 0f);

            // rewarded: free scrap, a few times per day (only offered when an ad is actually loaded)
            var ads = DerbyArena.Ads.AdsManager.Instance;
            int left = Save.RewardedScrapLeft(DerbyArena.Ads.AdsConfig.RewardedScrapPerDay);
            bool ready = ads != null && ads.RewardedReady;
            string caption = left <= 0
                ? Loc.T("BUGÜNLÜK BİTTİ · YARIN TEKRAR", "DONE FOR TODAY · COME BACK TOMORROW")
                : (ready ? Loc.T("REKLAM İZLE · +" + DerbyArena.Ads.AdsConfig.RewardedScrapAmount + " HURDA (" + left + ")", "WATCH AD · +" + DerbyArena.Ads.AdsConfig.RewardedScrapAmount + " SCRAP (" + left + ")")
                         : Loc.T("REKLAM HAZIRLANIYOR…", "AD LOADING…"));
            var free = Btn(P, caption, left > 0 && ready ? new Color(0.2f, 0.45f, 1f, 0.95f) : new Color(0.25f, 0.25f, 0.27f, 0.9f), 1f, 0f, 560f, 70f, 1f, 0f, 24, () =>
            {
                if (ads == null) return;
                garageMsg = "";
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
            });
            free.Interactable = left > 0 && ready;
            if (!string.IsNullOrEmpty(garageMsg))
                Move(Txt(P, garageMsg, 24, UiKit.Green, TextAnchor.MiddleRight, true, 1f, 0f, 700f, 40f, 1f, 0f), -580f, 14f);
        }

        // ================================================================== settings
        void BuildSettings()
        {
            Header(Loc.T("AYARLAR", "SETTINGS"));
            float y = -110f;
            string on = Loc.T("AÇIK", "ON"), off = Loc.T("KAPALI", "OFF");
            Row(Loc.T("DİL", "LANGUAGE"), Loc.LanguageName, ref y, () => { Loc.Set(Loc.IsEnglish ? Loc.Turkish : Loc.English); });
            Row(Loc.T("KONTROL", "CONTROLS"), Save.Data.ControlMode == 0 ? Loc.T("DİREKSİYON SİMİDİ", "STEERING WHEEL") : Loc.T("SAĞ / SOL OKLAR", "LEFT / RIGHT ARROWS"), ref y, () => { Save.Data.ControlMode = 1 - Save.Data.ControlMode; });
            Row(Loc.T("DİREKSİYON HASSASİYETİ", "STEERING SENSITIVITY"), Mathf.RoundToInt(Save.Data.Sensitivity * 100f) + "%", ref y, () =>
            {
                float s = Save.Data.Sensitivity + 0.1f;
                Save.Data.Sensitivity = s > 1.81f ? 0.5f : s;
            });
            Row(Loc.T("OTOMATİK GAZ", "AUTO GAS"), Save.Data.AutoGas ? on : off, ref y, () => { Save.Data.AutoGas = !Save.Data.AutoGas; });
            Row(Loc.T("OTOMATİK NİŞAN", "AUTO AIM"), Save.Data.AutoAim ? Loc.T("GENİŞ", "WIDE") : Loc.T("DAR", "NARROW"), ref y, () => { Save.Data.AutoAim = !Save.Data.AutoAim; });
            Row(Loc.T("SES", "SOUND"), Save.Data.Sound ? on : off, ref y, () => { Save.Data.Sound = !Save.Data.Sound; DerbyArena.Audio.AudioHub.SetSound(Save.Data.Sound); });
            Row(Loc.T("TİTREŞİM", "VIBRATION"), Save.Data.Haptics ? on : off, ref y, () => { Save.Data.Haptics = !Save.Data.Haptics; });
            Row(Loc.T("PARLAKLIK", "BRIGHTNESS"), Mathf.RoundToInt(Save.Data.Brightness * 100f) + "%", ref y, () =>
            {
                float nb = Save.Data.Brightness + 0.1f;
                Save.Data.Brightness = nb > 1.45f ? 0.8f : nb;
                DerbyArena.World.Atmosphere.ApplyBrightness();
            });
            Row(Loc.T("KARE HIZI", "FRAME RATE"), Save.Data.FrameRate + " FPS", ref y, () =>
            {
                int fr = Save.Data.FrameRate;
                Save.Data.FrameRate = fr < 60 ? 60 : (fr < 90 ? 90 : (fr < 120 ? 120 : 30));
                DerbyBootstrapQuality.Apply();
            });
            string[] q = { Loc.T("DÜŞÜK", "LOW"), Loc.T("ORTA", "MEDIUM"), Loc.T("YÜKSEK", "HIGH") };
            Row(Loc.T("GRAFİK KALİTESİ", "GRAPHICS QUALITY"), q[Mathf.Clamp(Save.Data.Quality, 0, 2)], ref y, () => { Save.Data.Quality = (Save.Data.Quality + 1) % 3; Save.Data.QualityChosen = true; DerbyBootstrapQuality.Apply(); });
            Row(Loc.T("DİNAMİK ÇÖZÜNÜRLÜK", "DYNAMIC RESOLUTION"), Save.Data.DynamicResolution ? on : off, ref y, () => { Save.Data.DynamicResolution = !Save.Data.DynamicResolution; DerbyBootstrapQuality.Apply(); });

            // right column: privacy, legal, rating, version
            float ry = -110f;
            SideButton(Loc.T("GİZLİLİK POLİTİKASI", "PRIVACY POLICY"), ref ry, () => { legalDoc = "privacy"; Save.Data.PrivacySeen = true; Save.Write(); Show("legal"); });
            SideButton(Loc.T("KULLANIM KOŞULLARI", "TERMS OF USE"), ref ry, () => { legalDoc = "terms"; Show("legal"); });
            SideButton(Loc.T("LİSANSLAR VE KAYNAKLAR", "LICENSES AND CREDITS"), ref ry, () => { legalDoc = "licenses"; Show("legal"); });
            var ads = DerbyArena.Ads.AdsManager.Instance;
            if (ads != null && ads.PrivacyOptionsRequired)
                SideButton(Loc.T("REKLAM GİZLİLİK SEÇENEKLERİ", "AD PRIVACY OPTIONS"), ref ry, () => ads.ShowPrivacyOptions(err => { if (this != null && current == "settings") Show("settings"); }));
            SideButton(Loc.T("OYUNU DEĞERLENDİR", "RATE THE GAME"), ref ry, () => { Save.Data.RateState = 1; Save.Write(); OpenStorePage(); });
            Txt(P, Loc.T("Sürüm ", "Version ") + DerbyStartupDiagnostics.Version + " (" + DerbyStartupDiagnostics.VersionCode + ")", 22, new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerRight, false, 1f, 0f, 520f, 40f, 1f, 0f);
        }

        void SideButton(string caption, ref float y, Action a)
        {
            var b = Btn(P, caption, UiKit.PanelLight, 1f, 1f, 520f, 76f, 1f, 1f, 26, a);
            b.Background.rectTransform.anchoredPosition = new Vector2(0f, y);
            b.Caption.color = UiKit.Cyan;
            y -= 86f;
        }

        // ================================================================== legal (offline summary + web link)
        void BuildLegal()
        {
            string title = legalDoc == "terms" ? Loc.T("KULLANIM KOŞULLARI", "TERMS OF USE")
                         : legalDoc == "licenses" ? Loc.T("LİSANSLAR", "LICENSES") : Loc.T("GİZLİLİK", "PRIVACY");
            var back = Btn(P, Loc.T("< GERİ", "< BACK"), UiKit.PanelLight, 0f, 1f, 210f, 84f, 0f, 1f, 32, () => Show("settings"));
            back.IgnoreBlock = true;
            Move(Txt(P, title, 56, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 900f, 84f, 0f, 1f), 240f, 0f);
            var panel = UiKit.Image("Doc", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Stretch(panel.rectTransform, 0f, 0f, 1f, 1f);
            panel.rectTransform.offsetMin = new Vector2(0f, 110f);
            panel.rectTransform.offsetMax = new Vector2(0f, -100f);
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
            Btn(P, Loc.T("TAM METNİ WEB'DE AÇ", "OPEN FULL TEXT ON THE WEB"), UiKit.Amber, 0.5f, 0f, 560f, 90f, 0.5f, 0f, 28, () => Legal.Open(url)).Caption.color = DarkText;
            Txt(P, Loc.T("Son güncelleme: ", "Last updated: ") + Legal.Updated + "  ·  " + Legal.Contact, 20, UiKit.TextDim, TextAnchor.LowerRight, false, 1f, 0f, 700f, 40f, 1f, 0f);
        }

        void Row(string label, string value, ref float y, Action change)
        {
            var row = UiKit.Image("Row", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(row.rectTransform, 0f, 1f, 1180f, 72f, 0f, 1f);
            row.rectTransform.anchoredPosition = new Vector2(0f, y);
            Move(Txt(row.transform, label, 28, Color.white, TextAnchor.MiddleLeft, true, 0f, 0.5f, 640f, 70f, 0f, 0.5f), 26f, 0f);
            var b = Btn(row.transform, value, UiKit.PanelLight, 1f, 0.5f, 420f, 58f, 1f, 0.5f, 26, () => { change(); Save.Write(); Show("settings"); });
            b.Background.rectTransform.anchoredPosition = new Vector2(-12f, 0f);
            b.Caption.color = UiKit.Amber;
            y -= 78f;
        }
    }
}
