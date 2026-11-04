using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>One on-screen control: its root rect plus the parts the HUD animates.</summary>
    public sealed class ControlWidget
    {
        public ControlId Id;
        public RectTransform Root;        // positioned by the layout; the hit rect for simple buttons
        public Image Main;                // the button face (null for the steer group)
        public Image Wheel, Left, Right;  // steering parts (mode dependent)
        public Image Pad, Knob;           // swipe joystick parts
        public Text Label;
        public float BaseAlpha;           // the resting alpha of Main / Left / Right
        public CanvasGroup Group;
    }

    /// <summary>
    /// Builds the touch controls the same way for the match HUD and for the layout editor, and places them from
    /// <see cref="ControlLayout"/> (normalised position inside the safe area, per-control scale, global scale and
    /// opacity). The HUD keeps the returned widgets and feeds their rects to TouchControls.
    /// </summary>
    public static class ControlWidgets
    {
        public static ControlWidget Build(ControlId id, Transform parent, SteerMode mode, bool tank)
        {
            var w = new ControlWidget { Id = id };
            w.Root = UiKit.Rect(id.ToString(), parent);
            w.Group = w.Root.gameObject.AddComponent<CanvasGroup>();
            w.Group.blocksRaycasts = false;
            switch (id)
            {
                case ControlId.Steer: BuildSteer(w, mode); break;
                case ControlId.Gas: Pedal(w, Loc.T("GAZ", "GAS"), new Color(0.15f, 0.7f, 0.32f), "play"); break;
                case ControlId.Brake: Pedal(w, Loc.T("FREN", "BRAKE"), new Color(0.75f, 0.18f, 0.14f), "brake"); break;
                case ControlId.Fire: Round(w, Loc.T("ATEŞ", "FIRE"), new Color(0.95f, 0.3f, 0.12f), 36, "crosshair"); break;
                case ControlId.Rocket: Round(w, tank ? Loc.T("HE MERMİ", "HE SHELL") : Loc.T("ROKET", "ROCKET"), new Color(1f, 0.55f, 0.1f), 24, "rocket"); break;
                case ControlId.Nitro: Round(w, Loc.T("NİTRO", "NITRO"), new Color(0.2f, 0.45f, 1f), 24, "flame"); break;
                default: Round(w, Loc.T("DRİFT", "DRIFT"), new Color(0.45f, 0.47f, 0.55f), 24, "skid"); break;
            }
            return w;
        }

        /// <summary>Positions and scales the widget from the saved layout. Call after Build and whenever the layout changes.</summary>
        public static void Layout(ControlWidget w, SteerMode mode)
        {
            if (w == null || w.Root == null) return;
            Vector2 size = ControlLayout.BaseSize(w.Id, mode) * ControlLayout.Scale(w.Id) * Save.Data.ControlScale;
            Vector2 pos = ControlLayout.Pos(w.Id);
            UiKit.Place(w.Root, pos.x, pos.y, size.x, size.y, 0.5f, 0.5f);
            if (w.Group != null) w.Group.alpha = Save.Data.ControlAlpha;
        }

        static void BuildSteer(ControlWidget w, SteerMode mode)
        {
            var root = w.Root;
            if (mode == SteerMode.Arrows)
            {
                w.BaseAlpha = 0.22f;
                w.Left = UiKit.Image("Left", root, UiKit.Rounded, new Color(1f, 1f, 1f, 0.22f));
                UiKit.Stretch(w.Left.rectTransform, 0f, 0f, 0.465f, 1f);
                UiKit.Label("L", w.Left.transform, "<", 90, Color.white, TextAnchor.MiddleCenter, true);
                w.Right = UiKit.Image("Right", root, UiKit.Rounded, new Color(1f, 1f, 1f, 0.22f));
                UiKit.Stretch(w.Right.rectTransform, 0.535f, 0f, 1f, 1f);
                UiKit.Label("R", w.Right.transform, ">", 90, Color.white, TextAnchor.MiddleCenter, true);
                return;
            }
            if (mode == SteerMode.Swipe)
            {
                w.BaseAlpha = 0.35f;
                w.Pad = UiKit.Image("Pad", root, UiKit.Circle, new Color(0f, 0f, 0f, 0.35f));
                UiKit.Stretch(w.Pad.rectTransform, 0f, 0f, 1f, 1f);
                var ring = UiKit.Image("Ring", w.Pad.transform, UiKit.RingSprite, new Color(1f, 1f, 1f, 0.35f));
                UiKit.Stretch(ring.rectTransform, 0f, 0f, 1f, 1f);
                var l = UiKit.Label("L", w.Pad.transform, "<", 44, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleLeft, true);
                l.rectTransform.offsetMin = new Vector2(22f, 0f);
                var r = UiKit.Label("R", w.Pad.transform, ">", 44, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleRight, true);
                r.rectTransform.offsetMax = new Vector2(-22f, 0f);
                w.Knob = UiKit.Image("Knob", w.Pad.transform, UiKit.Circle, new Color(0.92f, 0.94f, 0.98f, 0.85f));
                UiKit.Place(w.Knob.rectTransform, 0.5f, 0.5f, 0f, 0f, 0.5f, 0.5f);
                w.Knob.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                w.Knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                w.Knob.rectTransform.sizeDelta = new Vector2(96f, 96f);
                var ic = UiKit.IconImage(w.Knob.transform, "steering", 54f, new Color(0.1f, 0.1f, 0.12f, 0.9f), 0.5f, 0.5f, 0.5f, 0.5f);
                if (ic != null) ic.rectTransform.anchoredPosition = Vector2.zero;
                return;
            }
            // wheel (also the tilt indicator: same art, not touched)
            w.BaseAlpha = mode == SteerMode.Tilt ? 0.45f : 0.82f;
            var shadow = UiKit.Image("Back", root, UiKit.Circle, new Color(0f, 0f, 0f, 0.35f));
            UiKit.Stretch(shadow.rectTransform, 0f, 0f, 1f, 1f);
            w.Wheel = UiKit.Image("Wheel", root, UiKit.Wheel, new Color(0.92f, 0.94f, 0.98f, w.BaseAlpha));
            UiKit.Stretch(w.Wheel.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
            if (mode == SteerMode.Tilt)
            {
                var t = UiKit.Label("Tilt", root, Loc.T("EĞİM", "TILT"), 26, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, true);
                t.rectTransform.anchorMin = new Vector2(0f, 0f);
                t.rectTransform.anchorMax = new Vector2(1f, 0.28f);
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            }
        }

        static void Round(ControlWidget w, string caption, Color c, int font, string icon)
        {
            w.BaseAlpha = 0.5f;
            var img = UiKit.Image("Face", w.Root, UiKit.Circle, new Color(c.r, c.g, c.b, 0.5f));
            UiKit.Stretch(img.rectTransform, 0f, 0f, 1f, 1f);
            var ring = UiKit.Image("Ring", img.transform, UiKit.RingSprite, new Color(1f, 1f, 1f, 0.35f));
            UiKit.Stretch(ring.rectTransform, 0f, 0f, 1f, 1f);
            bool hasIcon = !string.IsNullOrEmpty(icon) && UiKit.Icon(icon) != null;
            if (hasIcon)
            {
                var ic = UiKit.IconImage(img.transform, icon, 1f, new Color(1f, 1f, 1f, 0.95f), 0.5f, 0.5f, 0.5f, 0.5f);
                // icon and caption scale with the button: anchors instead of fixed pixels
                ic.rectTransform.anchorMin = new Vector2(0.33f, 0.5f);
                ic.rectTransform.anchorMax = new Vector2(0.67f, 0.84f);
                ic.rectTransform.offsetMin = ic.rectTransform.offsetMax = Vector2.zero;
                ic.preserveAspect = true;
            }
            var lbl = UiKit.Label("Label", img.transform, caption, font, Color.white, TextAnchor.MiddleCenter, true);
            // two-line captions ("ROKET\n3") fit in the lower half under the icon
            lbl.rectTransform.anchorMin = new Vector2(0.06f, hasIcon ? 0.06f : 0f);
            lbl.rectTransform.anchorMax = new Vector2(0.94f, hasIcon ? 0.5f : 1f);
            lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
            lbl.resizeTextForBestFit = true;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;   // best fit then honours the height too
            lbl.resizeTextMinSize = 12;
            lbl.resizeTextMaxSize = font;
            w.Main = img;
            w.Label = lbl;
        }

        static void Pedal(ControlWidget w, string caption, Color c, string icon)
        {
            w.BaseAlpha = 0.6f;
            var img = UiKit.Image("Face", w.Root, UiKit.ButtonSprite, new Color(c.r, c.g, c.b, 0.6f));
            UiKit.Stretch(img.rectTransform, 0f, 0f, 1f, 1f);
            if (!string.IsNullOrEmpty(icon) && UiKit.Icon(icon) != null)
            {
                var ic = UiKit.IconImage(img.transform, icon, 1f, new Color(1f, 1f, 1f, 0.9f), 0.5f, 0.5f, 0.5f, 0.5f);
                ic.rectTransform.anchorMin = new Vector2(0.3f, 0.56f);
                ic.rectTransform.anchorMax = new Vector2(0.7f, 0.9f);
                ic.rectTransform.offsetMin = ic.rectTransform.offsetMax = Vector2.zero;
                ic.preserveAspect = true;
            }
            var lbl = UiKit.Label("Label", img.transform, caption, 30, Color.white, TextAnchor.MiddleCenter, true);
            lbl.rectTransform.anchorMin = new Vector2(0f, 0.14f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
            lbl.resizeTextForBestFit = true;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;   // best fit then honours the height too
            lbl.resizeTextMinSize = 12;
            lbl.resizeTextMaxSize = 30;
            w.Main = img;
            w.Label = lbl;
        }

        /// <summary>Press feedback shared by HUD and editor preview.</summary>
        public static void Press(Image img, bool on, float baseAlpha)
        {
            if (img == null) return;
            var c = img.color;
            c.a = on ? 0.9f : baseAlpha;
            img.color = c;
            img.rectTransform.localScale = Vector3.one * (on ? 0.94f : 1f);
        }
    }
}
