using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DerbyArena.UI
{
    /// <summary>One pointer (finger or mouse) in screen pixels.</summary>
    public struct Pointer
    {
        public int Id;
        public Vector2 Pos;
        public bool Began;
    }

    /// <summary>
    /// Pointer source shared by HUD and menus. Reads the new Input System (EnhancedTouch), falls back to
    /// the legacy Input class and mouse. No EventSystem is required.
    /// </summary>
    public static class PointerInput
    {
        static readonly List<Pointer> list = new List<Pointer>(8);
        static int frame = -1;
        static bool enabled;

        public static List<Pointer> Get()
        {
            if (frame == Time.frameCount) return list;
            frame = Time.frameCount;
            list.Clear();
            if (!enabled)
            {
                enabled = true;
                try { EnhancedTouchSupport.Enable(); } catch (Exception) { }
            }

            bool gotTouch = false;
            try
            {
                if (Touchscreen.current != null)
                {
                    gotTouch = true;
                    foreach (var t in Touch.activeTouches)
                    {
                        if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled) continue;
                        list.Add(new Pointer { Id = t.touchId, Pos = t.screenPosition, Began = t.phase == UnityEngine.InputSystem.TouchPhase.Began });
                    }
                }
            }
            catch (Exception) { gotTouch = false; }

            if (!gotTouch)
            {
                try
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var t = Input.GetTouch(i);
                        if (t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled) continue;
                        list.Add(new Pointer { Id = t.fingerId, Pos = t.position, Began = t.phase == UnityEngine.TouchPhase.Began });
                    }
                }
                catch (Exception) { }
            }

            if (list.Count == 0)
            {
                try
                {
                    var mouse = Mouse.current;
                    if (mouse != null && mouse.leftButton.isPressed)
                        list.Add(new Pointer { Id = -100, Pos = mouse.position.ReadValue(), Began = mouse.leftButton.wasPressedThisFrame });
                }
                catch (Exception) { }
            }
            return list;
        }
    }

    /// <summary>Code-built UI toolkit: canvas, safe area, rounded sprites, labels, panels, tap buttons.</summary>
    public static class UiKit
    {
        public static readonly Color Panel = new Color(0.055f, 0.06f, 0.075f, 0.86f);
        public static readonly Color PanelLight = new Color(0.11f, 0.12f, 0.15f, 0.92f);
        public static readonly Color Amber = new Color(1f, 0.62f, 0.13f, 1f);
        public static readonly Color Cyan = new Color(0.2f, 0.85f, 1f, 1f);
        public static readonly Color Red = new Color(0.95f, 0.22f, 0.18f, 1f);
        public static readonly Color Green = new Color(0.25f, 0.88f, 0.45f, 1f);
        public static readonly Color TextDim = new Color(0.7f, 0.73f, 0.78f, 1f);

        public static readonly Color Ink = new Color(0.08f, 0.06f, 0.03f, 1f);          // text on amber
        public static readonly Color Primary = new Color(1f, 0.6f, 0.13f, 1f);
        public static readonly Color PanelRing = new Color(1f, 1f, 1f, 0.09f);
        public static readonly Color Violet = new Color(0.72f, 0.42f, 1f, 1f);

        static Font font, fontBold;
        static Sprite rounded, roundedSmall, circle, ring, glow, square, wheel, vignette;
        static Sprite panelRing, button, buttonPrimary, pill, card, cardRing, tab, carbon, radial, hazard;
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        /// <summary>Body font (Poppins Medium, OFL) with the engine font as a fallback.</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.Load<Font>("Derby/Fonts/Poppins-Medium");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return font;
            }
        }

        /// <summary>Display font (Poppins Bold) for titles, buttons and numbers.</summary>
        public static Font FontBold
        {
            get
            {
                if (fontBold == null) fontBold = Resources.Load<Font>("Derby/Fonts/Poppins-Bold");
                if (fontBold == null) fontBold = Font;
                return fontBold;
            }
        }

        /// <summary>A 9-slice sprite from Resources/Derby/UI (null when the file is missing).</summary>
        static Sprite Load(string file, float border)
        {
            var tex = Resources.Load<Texture2D>("Derby/UI/" + file);
            if (tex == null) return null;
            var b = new Vector4(border, border, border, border);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        /// <summary>White icon sprite by name (ic_<name>.png in Resources/Derby/UI); tint with Image.color.</summary>
        public static Sprite Icon(string name)
        {
            Sprite sp;
            if (icons.TryGetValue(name, out sp)) return sp;
            sp = Load("ic_" + name, 0f);
            icons[name] = sp;
            return sp;
        }

        public static Sprite PanelRingSprite { get { if (panelRing == null) panelRing = Load("panel_ring", 30f); return panelRing; } }
        public static Sprite ButtonSprite { get { if (button == null) button = Load("button", 30f) ?? Rounded; return button; } }
        public static Sprite ButtonPrimarySprite { get { if (buttonPrimary == null) buttonPrimary = Load("button_primary", 30f) ?? Rounded; return buttonPrimary; } }
        public static Sprite Pill { get { if (pill == null) pill = Load("pill", 46f) ?? Rounded; return pill; } }
        public static Sprite Card { get { if (card == null) card = Load("card", 22f) ?? RoundedSmall; return card; } }
        public static Sprite CardRing { get { if (cardRing == null) cardRing = Load("card_ring", 22f); return cardRing; } }
        public static Sprite Tab { get { if (tab == null) tab = Load("tab", 18f) ?? RoundedSmall; return tab; } }
        public static Sprite Carbon { get { if (carbon == null) carbon = Load("carbon", 0f); return carbon; } }
        public static Sprite Radial { get { if (radial == null) radial = Load("glow", 0f) ?? Glow; return radial; } }
        public static Sprite Hazard { get { if (hazard == null) hazard = Load("hazard", 0f); return hazard; } }

        // ------------------------------------------------------------------ sprites
        static Sprite FromPixels(int w, int h, Color32[] px, Vector4 border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        static Sprite RoundRect(int size, int radius)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f));
                }
            return FromPixels(size, size, px, new Vector4(radius, radius, radius, radius));
        }

        static Sprite Disc(int size, float inner01, float softness)
        {
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    float a = Mathf.Clamp01(r - d);
                    if (inner01 > 0f) a *= Mathf.Clamp01(d - r * inner01);
                    if (softness > 0f) a = Mathf.Pow(Mathf.Clamp01(1f - d / r), softness);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            return FromPixels(size, size, px, Vector4.zero);
        }

        static Sprite SteeringWheel(int size)
        {
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 0f;
                    byte g = 255;
                    if (d < r - 1f && d > r * 0.74f) a = 1f;                      // rim
                    if (d < r * 0.24f) a = 1f;                                      // hub
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (d < r * 0.76f && d > r * 0.2f)
                    {
                        float[] spokes = { 0f, 180f, -90f };
                        for (int s = 0; s < spokes.Length; s++)
                        {
                            float diff = Mathf.Abs(Mathf.DeltaAngle(ang, spokes[s]));
                            float width = Mathf.Rad2Deg * (r * 0.075f) / Mathf.Max(1f, d);
                            if (diff < width) a = 1f;
                        }
                    }
                    if (d < r - 1f && d > r * 0.74f && Mathf.Abs(Mathf.DeltaAngle(ang, 90f)) < 7f) g = 120;   // top marker
                    a *= Mathf.Clamp01(r - d);
                    px[y * size + x] = g == 255 ? new Color32(255, 255, 255, (byte)(a * 255f)) : new Color32(255, 150, 40, (byte)(a * 255f));
                }
            return FromPixels(size, size, px, Vector4.zero);
        }

        static Sprite Vignette(int size)
        {
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - r) / r, dy = (y + 0.5f - r) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41f;
                    float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            return FromPixels(size, size, px, Vector4.zero);
        }

        public static Sprite Rounded { get { if (rounded == null) rounded = Load("panel", 30f) ?? RoundRect(64, 18); return rounded; } }
        public static Sprite RoundedSmall { get { if (roundedSmall == null) roundedSmall = RoundRect(32, 8); return roundedSmall; } }
        public static Sprite Circle { get { if (circle == null) circle = Disc(128, 0f, 0f); return circle; } }
        public static Sprite RingSprite { get { if (ring == null) ring = Disc(128, 0.86f, 0f); return ring; } }
        public static Sprite Glow { get { if (glow == null) glow = Disc(128, 0f, 1.6f); return glow; } }
        public static Sprite Wheel { get { if (wheel == null) wheel = SteeringWheel(256); return wheel; } }
        public static Sprite VignetteSprite { get { if (vignette == null) vignette = Vignette(128); return vignette; } }
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    square = FromPixels(4, 4, px, Vector4.zero);
                }
                return square;
            }
        }

        // ------------------------------------------------------------------ builders
        public static RectTransform Canvas(string name, int order, out RectTransform safe)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var s = new GameObject("Safe", typeof(RectTransform));
            s.transform.SetParent(go.transform, false);
            safe = s.GetComponent<RectTransform>();
            go.AddComponent<SafeArea>().Target = safe;
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Fixed-size element anchored at a normalised point with a pivot.</summary>
        public static void Place(RectTransform rt, float ax, float ay, float w, float h, float px, float py)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(px, py);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && (sprite.border.x > 0f || sprite.border.y > 0f)) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>Panel with a soft 1px ring (4.0 look). Returns the fill image; the ring is a child.</summary>
        public static Image Frame(string name, Transform parent, Color color)
        {
            var img = Image(name, parent, Rounded, color);
            if (PanelRingSprite != null)
            {
                var r = Image("Ring", img.transform, PanelRingSprite, PanelRing);
                Stretch(r.rectTransform, 0f, 0f, 1f, 1f);
            }
            return img;
        }

        /// <summary>Icon image of a fixed size anchored inside its parent.</summary>
        public static Image IconImage(Transform parent, string icon, float size, Color color, float ax, float ay, float px, float py)
        {
            var sp = Icon(icon);
            var img = Image("Icon_" + icon, parent, sp ?? Circle, color);
            img.preserveAspect = true;
            Place(img.rectTransform, ax, ay, size, size, px, py);
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor anchor, bool bold)
        {
            var rt = Rect(name, parent);
            Stretch(rt, 0f, 0f, 1f, 1f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? FontBold : Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            // 4.0.2: every label shrinks to fit its box (down to 60 %) and is clipped there, so a long translation
            // or the wider Poppins glyphs can never spill over a neighbouring element any more
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = size;
            t.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(size * 0.6f));
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
            sh.effectDistance = new Vector2(0f, -2f);
            return t;
        }

        public static Image Bar(Transform parent, Color fill, out Image fillImage)
        {
            var bg = Image("Bar", parent, Rounded, new Color(0f, 0f, 0f, 0.55f));
            var f = Image("Fill", bg.transform, Square, fill);
            f.type = UnityEngine.UI.Image.Type.Filled;
            f.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            f.fillAmount = 1f;
            Stretch(f.rectTransform, 0f, 0f, 1f, 1f);
            f.rectTransform.offsetMin = new Vector2(5f, 5f);
            f.rectTransform.offsetMax = new Vector2(-5f, -5f);
            fillImage = f;
            return bg;
        }

        public static UiButton Button(Transform parent, string caption, Color color, int fontSize, Action onTap)
        {
            return Button(parent, caption, color, fontSize, onTap, false, null);
        }

        /// <summary>
        /// Tap button. primary = amber gradient sprite with dark caption; icon (optional) sits left of the caption.
        /// </summary>
        public static UiButton Button(Transform parent, string caption, Color color, int fontSize, Action onTap, bool primary, string icon)
        {
            var img = Image(caption, parent, primary ? ButtonPrimarySprite : ButtonSprite, color);
            var label = Label("Label", img.transform, caption, fontSize, primary ? Ink : Color.white, TextAnchor.MiddleCenter, true);
            if (!string.IsNullOrEmpty(icon) && Icon(icon) != null)
            {
                float sz = fontSize * 1.15f;
                var ic = IconImage(img.transform, icon, sz, primary ? Ink : Color.white, 0f, 0.5f, 0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(fontSize * 0.7f, 0f);
                label.rectTransform.offsetMin = new Vector2(fontSize * 0.7f + sz + 4f, 0f);
                label.alignment = TextAnchor.MiddleLeft;
            }
            var b = img.gameObject.AddComponent<UiButton>();
            b.Background = img;
            b.Caption = label;
            b.OnTap = onTap;
            b.BaseColor = color;
            return b;
        }

        /// <summary>Small pill chip with an icon and a value (scrap, stars, level).</summary>
        public static Image Chip(Transform parent, string icon, string text, Color iconColor, float w, float h, int fontSize)
        {
            var bg = Image("Chip", parent, Pill, new Color(0f, 0f, 0f, 0.55f));
            Place(bg.rectTransform, 0f, 0.5f, w, h, 0f, 0.5f);
            if (Icon(icon) != null)
            {
                var ic = IconImage(bg.transform, icon, h * 0.62f, iconColor, 0f, 0.5f, 0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(h * 0.28f, 0f);
            }
            var t = Label("Text", bg.transform, text, fontSize, Color.white, TextAnchor.MiddleLeft, true);
            t.rectTransform.offsetMin = new Vector2(h * 0.95f, 0f);
            t.rectTransform.offsetMax = new Vector2(-h * 0.3f, 0f);
            return bg;
        }

        /// <summary>Three stars in a row (filled / outline).</summary>
        public static void Stars(Transform parent, int count, float size, float ax, float ay, float px, float py, Vector2 offset)
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = i < count;
                var img = IconImage(parent, on ? "star" : "star_outline", size, on ? Amber : new Color(1f, 1f, 1f, 0.3f), ax, ay, px, py);
                img.rectTransform.anchoredPosition = offset + new Vector2((i - 1) * size * 1.05f, 0f);
            }
        }
    }

    /// <summary>Keeps a RectTransform inside Screen.safeArea (notches / punch holes / rounded corners).</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        public RectTransform Target;
        Rect last;
        Vector2Int lastSize;

        void LateUpdate() { Apply(false); }
        void Start() { Apply(true); }

        void Apply(bool force)
        {
            if (Target == null) return;
            Rect sa = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && sa == last && size == lastSize) return;
            last = sa;
            lastSize = size;
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            Target.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h);
            Target.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h);
            Target.offsetMin = new Vector2(12f, 8f);
            Target.offsetMax = new Vector2(-12f, -8f);
        }
    }

    /// <summary>Tap button driven by PointerInput (fires on press, with press feedback).</summary>
    public sealed class UiButton : MonoBehaviour
    {
        public static bool Blocked;     // set while a modal page takes over input

        public Image Background;
        public Text Caption;
        public Action OnTap;
        public Color BaseColor;
        public bool Interactable = true;
        public bool IgnoreBlock;
        float flash;
        static float globalCooldown;

        void Update()
        {
            if (flash > 0f)
            {
                flash -= Time.unscaledDeltaTime;
                Background.color = Color.Lerp(BaseColor, Color.white, Mathf.Clamp01(flash / 0.12f) * 0.35f);
            }
            if (!Interactable || OnTap == null || (Blocked && !IgnoreBlock)) return;
            if (Time.unscaledTime < globalCooldown) return;
            var ptrs = PointerInput.Get();
            for (int i = 0; i < ptrs.Count; i++)
            {
                if (!ptrs[i].Began) continue;
                if (!Hit(Background.rectTransform, ptrs[i].Pos, 4f)) continue;
                globalCooldown = Time.unscaledTime + 0.18f;
                flash = 0.12f;
                DerbyArena.Audio.AudioHub.Play2D(DerbyArena.Audio.SynthAudio.Click(), 0.5f, 1f);
                var a = OnTap;
                a();
                return;
            }
        }

        public void SetColor(Color c)
        {
            BaseColor = c;
            if (Background != null) Background.color = c;
        }

        static readonly Vector3[] corners = new Vector3[4];

        public static bool Hit(RectTransform rt, Vector2 p, float pad)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return false;
            rt.GetWorldCorners(corners);
            return p.x >= corners[0].x - pad && p.x <= corners[2].x + pad && p.y >= corners[0].y - pad && p.y <= corners[2].y + pad;
        }
    }
}
