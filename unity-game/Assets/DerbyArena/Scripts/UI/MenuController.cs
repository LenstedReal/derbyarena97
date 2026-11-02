using System;
using DerbyArena.Audio;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>Main menu over a live 3D arena: home, level select (5 chapters x 10), garage and settings.</summary>
    public sealed class MenuController : MonoBehaviour
    {
        RectTransform canvasRt, safe;
        GameObject page;
        int chapter = 1;
        int selected = 1;

        public static MenuController Create(string startPage)
        {
            var go = new GameObject("Menu");
            var m = go.AddComponent<MenuController>();
            m.canvasRt = UiKit.Canvas("MenuCanvas", 20, out m.safe);
            m.canvasRt.SetParent(go.transform, false);
            var vig = UiKit.Image("Vignette", m.canvasRt, UiKit.VignetteSprite, new Color(0f, 0f, 0f, 0.75f));
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 1f, 1f);
            vig.transform.SetAsFirstSibling();
            m.selected = Mathf.Clamp(Save.Data.LastPlayed, 1, Save.Data.Unlocked);
            m.chapter = (m.selected - 1) / Campaign.PerChapter + 1;
            m.Show(startPage);
            return m;
        }

        void Show(string name)
        {
            if (page != null) Destroy(page);
            page = UiKit.Rect("Page_" + name, safe).gameObject;
            UiKit.Stretch(page.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            switch (name)
            {
                case "levels": BuildLevels(); break;
                case "garage": BuildGarage(); break;
                case "settings": BuildSettings(); break;
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

        void Header(string title)
        {
            var back = Btn(P, "< GERİ", UiKit.PanelLight, 0f, 1f, 210f, 84f, 0f, 1f, 32, () => Show("home"));
            Txt(P, title, 56, Color.white, TextAnchor.MiddleLeft, true, 0f, 1f, 900f, 84f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(240f, 0f);
            var chip = UiKit.Image("Scrap", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(chip.rectTransform, 1f, 1f, 330f, 84f, 1f, 1f);
            UiKit.Label("Text", chip.transform, "HURDA  " + Save.Data.Scrap, 34, UiKit.Amber, TextAnchor.MiddleCenter, true);
            back.IgnoreBlock = true;
        }

        // ================================================================== home
        void BuildHome()
        {
            var shade = UiKit.Image("Shade", P, UiKit.Square, new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(shade.rectTransform, 0f, 0f, 1f, 1f);
            var side = UiKit.Image("Side", P, UiKit.Rounded, new Color(0.03f, 0.035f, 0.045f, 0.78f));
            UiKit.Stretch(side.rectTransform, 0f, 0f, 0.42f, 1f);

            Txt(P, "DERBY ARENA", 104, UiKit.Amber, TextAnchor.UpperLeft, true, 0f, 1f, 800f, 130f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(40f, -40f);
            Txt(P, "BLACKOUT RUN", 40, UiKit.Cyan, TextAnchor.UpperLeft, true, 0f, 1f, 800f, 60f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(46f, -168f);

            int next = Mathf.Clamp(Save.Data.Unlocked, 1, Campaign.Total);
            int play = Save.Data.Stars[next] > 0 ? Save.Data.LastPlayed : next;
            var cfg = Campaign.Get(play);
            float y = -270f;
            var main = Btn(P, "OYNA  ·  BÖLÜM " + play, UiKit.Amber, 0f, 1f, 640f, 130f, 0f, 1f, 46, () => GameFlow.Play(play));
            main.Background.rectTransform.anchoredPosition = new Vector2(40f, y);
            main.Caption.color = new Color(0.08f, 0.06f, 0.03f);
            Txt(P, cfg.Title + "  ·  " + cfg.Subtitle, 26, UiKit.TextDim, TextAnchor.UpperLeft, false, 0f, 1f, 640f, 40f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(44f, y - 140f);

            y -= 200f;
            string[] names = { "BÖLÜMLER", "GARAJ", "AYARLAR" };
            string[] pages = { "levels", "garage", "settings" };
            for (int i = 0; i < names.Length; i++)
            {
                string pg = pages[i];
                var b = Btn(P, names[i], UiKit.PanelLight, 0f, 1f, 640f, 100f, 0f, 1f, 38, () => Show(pg));
                b.Background.rectTransform.anchoredPosition = new Vector2(40f, y);
                y -= 118f;
            }

            var chips = UiKit.Image("Stats", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(chips.rectTransform, 1f, 1f, 640f, 90f, 1f, 1f);
            UiKit.Label("Text", chips.transform, "HURDA " + Save.Data.Scrap + "    YILDIZ " + Save.TotalStars() + "/" + (Campaign.Total * 3) + "    BÖLÜM " + Save.Data.Unlocked + "/" + Campaign.Total,
                28, Color.white, TextAnchor.MiddleCenter, true);
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
            Txt(P, "v" + DerbyStartupDiagnostics.Version, 22, new Color(1f, 1f, 1f, 0.35f), TextAnchor.LowerRight, false, 1f, 0f, 300f, 40f, 1f, 0f);
        }

        // ================================================================== levels
        void BuildLevels()
        {
            Header("BÖLÜMLER");
            for (int c = 1; c <= 5; c++)
            {
                int cc = c;
                int first = (c - 1) * Campaign.PerChapter + 1;
                bool open = Save.Data.Unlocked >= first;
                var tab = Btn(P, c + ". " + Campaign.ChapterNames[c - 1], c == chapter ? UiKit.Amber : (open ? UiKit.PanelLight : new Color(0.08f, 0.08f, 0.09f, 0.9f)),
                    0f, 1f, 360f, 70f, 0f, 1f, 22, () => { chapter = cc; selected = Mathf.Clamp(Mathf.Max(first, Mathf.Min(Save.Data.Unlocked, first + 9)), 1, Campaign.Total); Show("levels"); });
                tab.Background.rectTransform.anchoredPosition = new Vector2((c - 1) * 372f, -100f);
                if (c == chapter) tab.Caption.color = new Color(0.08f, 0.06f, 0.03f);
                tab.Interactable = open;
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
                var num = UiKit.Rect("Num", t); UiKit.Stretch(num, 0f, 0.42f, 1f, 1f);
                UiKit.Label("Label", num, locked ? "KİLİTLİ" : n.ToString(), locked ? 30 : 64, locked ? UiKit.TextDim : Color.white, TextAnchor.MiddleCenter, true);
                var kind = UiKit.Rect("Kind", t); UiKit.Stretch(kind, 0f, 0.2f, 1f, 0.42f);
                UiKit.Label("Label", kind, LevelConfig.KindName(cfg.Kind), 22, locked ? UiKit.TextDim : typeCol, TextAnchor.MiddleCenter, true);
                int stars = Save.Data.Stars[n];
                for (int s = 0; s < 3; s++)
                {
                    var dot = UiKit.Image("Star", t, UiKit.Circle, s < stars ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                    UiKit.Place(dot.rectTransform, 0.5f, 0f, 22f, 22f, 0.5f, 0f);
                    dot.rectTransform.anchoredPosition = new Vector2((s - 1) * 32f, 16f);
                }
            }

            var feat = Txt(P, Campaign.ChapterFeatures[Mathf.Clamp(chapter - 1, 0, Campaign.ChapterFeatures.Length - 1)], 24, UiKit.Cyan, TextAnchor.UpperLeft, false, 0f, 1f, 1270f, 70f, 0f, 1f);
            feat.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(4f, -812f);

            var sel = Campaign.Get(selected);
            var detail = UiKit.Image("Detail", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(detail.rectTransform, 1f, 1f, 560f, 600f, 1f, 1f);
            detail.rectTransform.anchoredPosition = new Vector2(0f, -196f);
            var d = detail.transform;
            Txt(d, "BÖLÜM " + selected, 30, UiKit.TextDim, TextAnchor.MiddleLeft, true, 0f, 1f, 500f, 50f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -20f);
            Txt(d, sel.Title, 42, UiKit.Amber, TextAnchor.MiddleLeft, true, 0f, 1f, 520f, 60f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -70f);
            string info = LevelConfig.KindName(sel.Kind) + "\n" + sel.ObjectiveLine + "\n\nArena: " + (sel.Arena == ArenaKind.Colosseum ? "Kolezyum" : "Blackout Spillway")
                        + "\nRakip: " + sel.Enemies.Length + "    Süre: " + Mathf.RoundToInt(sel.TimeLimit) + " sn"
                        + "\nÖdül: " + sel.Reward + " hurda" + "\nEn iyi: " + Save.Data.Stars[selected] + "/3 yıldız"
                        + "\n2. yıldız: " + sel.StarTwoLine;
            Txt(d, info, 26, Color.white, TextAnchor.UpperLeft, false, 0f, 1f, 500f, 320f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -150f);
            int go = selected;
            var start2 = Btn(d, "BAŞLAT", UiKit.Green, 0.5f, 0f, 480f, 110f, 0.5f, 0f, 46, () => GameFlow.Play(go));
            start2.Background.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        }

        // ================================================================== garage
        void BuildGarage()
        {
            Header("GARAJ");
            for (int i = 0; i < 5; i++)
            {
                int type = i;
                int lvl = Upgrades.Level(i);
                var row = UiKit.Image("Row", P, UiKit.Rounded, UiKit.Panel);
                UiKit.Place(row.rectTransform, 0f, 1f, 1300f, 130f, 0f, 1f);
                row.rectTransform.anchoredPosition = new Vector2(0f, -110f - i * 145f);
                var t = row.transform;
                Txt(t, Upgrades.Names[i], 40, Color.white, TextAnchor.MiddleLeft, true, 0f, 0.5f, 300f, 60f, 0f, 0f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, 0f);
                Txt(t, Upgrades.Descriptions[i], 24, UiKit.TextDim, TextAnchor.MiddleLeft, false, 0f, 0.5f, 420f, 50f, 0f, 1f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, 0f);
                for (int p = 0; p < Upgrades.MaxLevel; p++)
                {
                    var pip = UiKit.Image("Pip", t, UiKit.RoundedSmall, p < lvl ? UiKit.Amber : new Color(1f, 1f, 1f, 0.12f));
                    UiKit.Place(pip.rectTransform, 0f, 0.5f, 60f, 26f, 0f, 0.5f);
                    pip.rectTransform.anchoredPosition = new Vector2(500f + p * 72f, 0f);
                }
                bool max = lvl >= Upgrades.MaxLevel;
                int cost = Upgrades.Cost(i, lvl);
                bool afford = Save.Data.Scrap >= cost;
                var b = Btn(t, max ? "MAKS" : cost + " HURDA", max ? UiKit.PanelLight : (afford ? UiKit.Green : new Color(0.25f, 0.25f, 0.27f, 0.9f)),
                    1f, 0.5f, 330f, 96f, 1f, 0.5f, 32, () =>
                    {
                        if (Upgrades.TryBuy(type)) AudioHub.Play2D(SynthAudio.Pickup(), 0.7f, 1f);
                        Show("garage");
                    });
                b.Background.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
                b.Interactable = !max && afford;
            }
            Txt(P, "Hurda kazanmak için bölümleri tamamla ve rakipleri imha et.", 26, UiKit.Cyan, TextAnchor.MiddleLeft, false, 0f, 0f, 1300f, 50f, 0f, 0f);
        }

        // ================================================================== settings
        void BuildSettings()
        {
            Header("AYARLAR");
            float y = -110f;
            Row("KONTROL", Save.Data.ControlMode == 0 ? "DİREKSİYON SİMİDİ" : "SAĞ / SOL OKLAR", ref y, () => { Save.Data.ControlMode = 1 - Save.Data.ControlMode; });
            Row("DİREKSİYON HASSASİYETİ", Mathf.RoundToInt(Save.Data.Sensitivity * 100f) + "%", ref y, () =>
            {
                float s = Save.Data.Sensitivity + 0.1f;
                Save.Data.Sensitivity = s > 1.81f ? 0.5f : s;
            });
            Row("OTOMATİK GAZ", Save.Data.AutoGas ? "AÇIK" : "KAPALI", ref y, () => { Save.Data.AutoGas = !Save.Data.AutoGas; });
            Row("OTOMATİK NİŞAN", Save.Data.AutoAim ? "GENİŞ" : "DAR", ref y, () => { Save.Data.AutoAim = !Save.Data.AutoAim; });
            Row("SES", Save.Data.Sound ? "AÇIK" : "KAPALI", ref y, () => { Save.Data.Sound = !Save.Data.Sound; AudioHub.SetSound(Save.Data.Sound); });
            Row("TİTREŞİM", Save.Data.Haptics ? "AÇIK" : "KAPALI", ref y, () => { Save.Data.Haptics = !Save.Data.Haptics; });
            string[] q = { "DÜŞÜK", "ORTA", "YÜKSEK" };
            Row("GRAFİK KALİTESİ", q[Mathf.Clamp(Save.Data.Quality, 0, 2)], ref y, () => { Save.Data.Quality = (Save.Data.Quality + 1) % 3; Save.Data.QualityChosen = true; DerbyBootstrapQuality.Apply(); });
        }

        void Row(string label, string value, ref float y, Action change)
        {
            var row = UiKit.Image("Row", P, UiKit.Rounded, UiKit.Panel);
            UiKit.Place(row.rectTransform, 0f, 1f, 1300f, 104f, 0f, 1f);
            row.rectTransform.anchoredPosition = new Vector2(0f, y);
            Txt(row.transform, label, 34, Color.white, TextAnchor.MiddleLeft, true, 0f, 0.5f, 700f, 80f, 0f, 0.5f).rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, 0f);
            var b = Btn(row.transform, value, UiKit.PanelLight, 1f, 0.5f, 460f, 80f, 1f, 0.5f, 30, () => { change(); Save.Write(); Show("settings"); });
            b.Background.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
            b.Caption.color = UiKit.Amber;
            y -= 116f;
        }
    }
}
