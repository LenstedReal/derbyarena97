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

        static Font font;
        static Sprite rounded, roundedSmall, circle, ring, glow, square, wheel, vignette;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return font;
            }
        }

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

        public static Sprite Rounded { get { if (rounded == null) rounded = RoundRect(64, 18); return rounded; } }
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
            if (sprite == Rounded || sprite == RoundedSmall) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor anchor, bool bold)
        {
            var rt = Rect(name, parent);
            Stretch(rt, 0f, 0f, 1f, 1f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sh.effectDistance = new Vector2(2f, -2f);
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
            var img = Image(caption, parent, Rounded, color);
            var label = Label("Label", img.transform, caption, fontSize, Color.white, TextAnchor.MiddleCenter, true);
            var b = img.gameObject.AddComponent<UiButton>();
            b.Background = img;
            b.Caption = label;
            b.OnTap = onTap;
            b.BaseColor = color;
            return b;
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
