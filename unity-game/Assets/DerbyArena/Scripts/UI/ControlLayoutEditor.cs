using System;
using DerbyArena.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    /// <summary>
    /// PUBG Mobile style control layout editor (4.0.1): every touch control is drawn exactly as in the match and can
    /// be dragged anywhere inside the safe area; the selected control is resized with − / +, the whole set has a
    /// global size and opacity, and three presets (Classic / Driver / Left hand) are one tap away. Save writes the
    /// layout to the save file, Cancel restores the snapshot taken on open. Works over the menu and over the pause
    /// screen (unscaled time, PointerInput), with every other UiButton blocked while it is up.
    /// </summary>
    public sealed class ControlLayoutEditor : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        RectTransform canvasRt, safe, area;
        readonly ControlWidget[] widgets = new ControlWidget[ControlLayout.Count];
        readonly Image[] halos = new Image[ControlLayout.Count];
        Text selectedText, sizeText, allText, alphaText, hint;
        UiButton[] presetButtons;
        int selected = -1, dragId = -1;
        Vector2 dragOffset;
        bool tank, wasBlocked;
        Action onClose;

        // snapshot for cancel
        float[] snapPos;
        int snapPreset;
        float snapScale, snapAlpha;

        public static void Open(bool tank, Action onClose)
        {
            if (IsOpen) return;
            var go = new GameObject("ControlLayoutEditor");
            var e = go.AddComponent<ControlLayoutEditor>();
            e.tank = tank;
            e.onClose = onClose;
            e.Build();
        }

        void Build()
        {
            IsOpen = true;
            wasBlocked = UiButton.Blocked;
            UiButton.Blocked = true;
            TouchControls.Suspended = true;
            var cur = ControlLayout.Current();
            snapPos = (float[])cur.Clone();
            snapPreset = Save.Data.ControlPreset;
            snapScale = Save.Data.ControlScale;
            snapAlpha = Save.Data.ControlAlpha;

            canvasRt = UiKit.Canvas("ControlEditorCanvas", 25000, out safe);
            canvasRt.SetParent(transform, false);
            var dim = UiKit.Image("Dim", canvasRt, UiKit.Square, new Color(0.02f, 0.03f, 0.04f, 0.62f));
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 1f, 1f);
            dim.transform.SetAsFirstSibling();

            // the drag area is the safe rect minus the two bars
            area = UiKit.Rect("Area", safe);
            UiKit.Stretch(area, 0f, 0f, 1f, 1f);
            area.offsetMin = new Vector2(0f, 0f);
            area.offsetMax = new Vector2(0f, 0f);
            // faint grid for alignment
            for (int i = 1; i < 8; i++)
            {
                var v = UiKit.Image("GridV", area, UiKit.Square, new Color(1f, 1f, 1f, 0.05f));
                UiKit.Stretch(v.rectTransform, i / 8f, 0f, i / 8f, 1f);
                v.rectTransform.sizeDelta = new Vector2(2f, 0f);
            }
            for (int i = 1; i < 5; i++)
            {
                var h = UiKit.Image("GridH", area, UiKit.Square, new Color(1f, 1f, 1f, 0.05f));
                UiKit.Stretch(h.rectTransform, 0f, i / 5f, 1f, i / 5f);
                h.rectTransform.sizeDelta = new Vector2(0f, 2f);
            }

            BuildWidgets();
            BuildTopBar();
            BuildBottomBar();
            Select(-1);
        }

        void BuildWidgets()
        {
            SteerMode mode = ControlLayout.Mode;
            for (int i = 0; i < ControlLayout.Count; i++)
            {
                if (widgets[i] != null && widgets[i].Root != null) Destroy(widgets[i].Root.gameObject);
                var w = ControlWidgets.Build((ControlId)i, area, mode, tank);
                ControlWidgets.Layout(w, mode);
                // selection halo (amber ring), drawn under the control
                var halo = UiKit.Image("Halo", w.Root, UiKit.RingSprite, new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, 0f));
                UiKit.Stretch(halo.rectTransform, -0.06f, -0.06f, 1.06f, 1.06f);
                halo.transform.SetAsFirstSibling();
                halos[i] = halo;
                widgets[i] = w;
                // auto gas hides the gas pedal in the HUD; in the editor it stays visible but dimmed so it can still be placed
                if (i == (int)ControlId.Gas && Save.Data.AutoGas && w.Group != null) w.Group.alpha = Save.Data.ControlAlpha * 0.45f;
            }
        }

        void BuildTopBar()
        {
            var bar = UiKit.Image("Top", safe, UiKit.Square, new Color(0.03f, 0.035f, 0.045f, 0.88f));
            UiKit.Stretch(bar.rectTransform, 0f, 1f, 1f, 1f);
            bar.rectTransform.sizeDelta = new Vector2(0f, 96f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            var t = bar.transform;
            var ic = UiKit.IconImage(t, "steering", 44f, UiKit.Amber, 0f, 0.5f, 0f, 0.5f);
            ic.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            var title = UiKit.Label("Title", t, Loc.T("KONTROL DÜZENİ", "CONTROL LAYOUT"), 40, Color.white, TextAnchor.MiddleLeft, true);
            UiKit.Place(title.rectTransform, 0f, 0.5f, 520f, 90f, 0f, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(84f, 8f);
            hint = UiKit.Label("Hint", t, Loc.T("Düğmeleri sürükle · seçili düğmeyi − / + ile boyutlandır", "Drag the buttons · resize the selected one with − / +"), 20, UiKit.TextDim, TextAnchor.MiddleLeft, false);
            UiKit.Place(hint.rectTransform, 0f, 0.5f, 900f, 40f, 0f, 0.5f);
            hint.rectTransform.anchoredPosition = new Vector2(84f, -24f);

            var save = UiKit.Button(t, Loc.T("KAYDET", "SAVE"), UiKit.Green, 28, SaveAndClose, true, "check");
            save.IgnoreBlock = true;
            UiKit.Place(save.Background.rectTransform, 1f, 0.5f, 240f, 72f, 1f, 0.5f);
            save.Background.rectTransform.anchoredPosition = new Vector2(-16f, 0f);
            var cancel = UiKit.Button(t, Loc.T("İPTAL", "CANCEL"), UiKit.PanelLight, 26, CancelAndClose, false, "back");
            cancel.IgnoreBlock = true;
            UiKit.Place(cancel.Background.rectTransform, 1f, 0.5f, 200f, 72f, 1f, 0.5f);
            cancel.Background.rectTransform.anchoredPosition = new Vector2(-272f, 0f);
            var reset = UiKit.Button(t, Loc.T("SIFIRLA", "RESET"), UiKit.PanelLight, 26, () =>
            {
                ControlLayout.ApplyPreset(ControlLayout.Classic);
                Save.Data.ControlScale = 1f;
                Save.Data.ControlAlpha = 1f;
                Refresh();
            }, false, "wrench");
            reset.IgnoreBlock = true;
            UiKit.Place(reset.Background.rectTransform, 1f, 0.5f, 200f, 72f, 1f, 0.5f);
            reset.Background.rectTransform.anchoredPosition = new Vector2(-488f, 0f);
        }

        void BuildBottomBar()
        {
            var bar = UiKit.Image("Bottom", safe, UiKit.Square, new Color(0.03f, 0.035f, 0.045f, 0.88f));
            UiKit.Stretch(bar.rectTransform, 0f, 0f, 1f, 0f);
            bar.rectTransform.sizeDelta = new Vector2(0f, 112f);
            bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            var t = bar.transform;

            // presets
            presetButtons = new UiButton[3];
            string[] names = { ControlLayout.PresetName(ControlLayout.Classic), ControlLayout.PresetName(ControlLayout.Driver), ControlLayout.PresetName(ControlLayout.LeftHand) };
            for (int i = 0; i < 3; i++)
            {
                int preset = i;
                var b = UiKit.Button(t, names[i], UiKit.PanelLight, 22, () => { ControlLayout.ApplyPreset(preset); Refresh(); });
                b.IgnoreBlock = true;
                UiKit.Place(b.Background.rectTransform, 0f, 0.5f, 170f, 66f, 0f, 0.5f);
                b.Background.rectTransform.anchoredPosition = new Vector2(16f + i * 180f, 0f);
                presetButtons[i] = b;
            }

            // selected control + its size
            selectedText = UiKit.Label("Selected", t, "", 24, UiKit.Amber, TextAnchor.MiddleLeft, true);
            UiKit.Place(selectedText.rectTransform, 0f, 0.5f, 330f, 70f, 0f, 0.5f);
            selectedText.rectTransform.anchoredPosition = new Vector2(576f, 0f);
            sizeText = Stepper(t, Loc.T("BOYUT", "SIZE"), 900f, () => StepSelected(-0.1f), () => StepSelected(0.1f));
            allText = Stepper(t, Loc.T("TÜMÜ", "ALL"), 1210f, () => { Save.Data.ControlScale = Mathf.Clamp(Save.Data.ControlScale - 0.1f, 0.7f, 1.4f); Refresh(); },
                                                             () => { Save.Data.ControlScale = Mathf.Clamp(Save.Data.ControlScale + 0.1f, 0.7f, 1.4f); Refresh(); });
            alphaText = Stepper(t, Loc.T("SAYDAMLIK", "OPACITY"), 1520f, () => { Save.Data.ControlAlpha = Mathf.Clamp(Save.Data.ControlAlpha - 0.1f, 0.4f, 1f); Refresh(); },
                                                                     () => { Save.Data.ControlAlpha = Mathf.Clamp(Save.Data.ControlAlpha + 0.1f, 0.4f, 1f); Refresh(); });
        }

        /// <summary>"LABEL  −  value  +" group anchored at x; returns the value text.</summary>
        Text Stepper(Transform parent, string label, float x, Action minus, Action plus)
        {
            var l = UiKit.Label("Label", parent, label, 20, UiKit.TextDim, TextAnchor.MiddleLeft, true);
            UiKit.Place(l.rectTransform, 0f, 0.5f, 130f, 60f, 0f, 0.5f);
            l.rectTransform.anchoredPosition = new Vector2(x, 0f);
            var m = UiKit.Button(parent, "−", UiKit.PanelLight, 34, minus);
            m.IgnoreBlock = true;
            UiKit.Place(m.Background.rectTransform, 0f, 0.5f, 64f, 64f, 0f, 0.5f);
            m.Background.rectTransform.anchoredPosition = new Vector2(x + 118f, 0f);
            var v = UiKit.Label("Value", parent, "", 24, Color.white, TextAnchor.MiddleCenter, true);
            UiKit.Place(v.rectTransform, 0f, 0.5f, 80f, 60f, 0f, 0.5f);
            v.rectTransform.anchoredPosition = new Vector2(x + 186f, 0f);
            var p = UiKit.Button(parent, "+", UiKit.PanelLight, 34, plus);
            p.IgnoreBlock = true;
            UiKit.Place(p.Background.rectTransform, 0f, 0.5f, 64f, 64f, 0f, 0.5f);
            p.Background.rectTransform.anchoredPosition = new Vector2(x + 270f, 0f);
            return v;
        }

        void StepSelected(float delta)
        {
            if (selected < 0) return;
            var id = (ControlId)selected;
            ControlLayout.SetScale(id, ControlLayout.Scale(id) + delta);
            Refresh();
        }

        void Refresh()
        {
            SteerMode mode = ControlLayout.Mode;
            for (int i = 0; i < ControlLayout.Count; i++)
            {
                ControlWidgets.Layout(widgets[i], mode);
                if (i == (int)ControlId.Gas && Save.Data.AutoGas && widgets[i].Group != null) widgets[i].Group.alpha = Save.Data.ControlAlpha * 0.45f;
            }
            Select(selected);
        }

        void Select(int index)
        {
            selected = index;
            for (int i = 0; i < halos.Length; i++)
                if (halos[i] != null) halos[i].color = new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, i == index ? 0.95f : 0f);
            if (selectedText != null)
                selectedText.text = index < 0 ? Loc.T("Bir düğmeye dokun", "Tap a button") : ControlLayout.Name((ControlId)index);
            if (sizeText != null) sizeText.text = index < 0 ? "–" : Mathf.RoundToInt(ControlLayout.Scale((ControlId)index) * 100f) + "%";
            if (allText != null) allText.text = Mathf.RoundToInt(Save.Data.ControlScale * 100f) + "%";
            if (alphaText != null) alphaText.text = Mathf.RoundToInt(Save.Data.ControlAlpha * 100f) + "%";
            if (presetButtons != null)
                for (int i = 0; i < presetButtons.Length; i++)
                    if (presetButtons[i] != null) presetButtons[i].SetColor(Save.Data.ControlPreset == i ? UiKit.Primary : UiKit.PanelLight);
        }

        static readonly Vector3[] corners = new Vector3[4];

        float openedAt;

        void Start() { openedAt = Time.unscaledTime; }

        void Update()
        {
            if (Time.unscaledTime < openedAt + 0.2f) return;        // the tap that opened the editor must not grab a control
            var ptrs = PointerInput.Get();
            bool dragging = false;
            for (int i = 0; i < ptrs.Count; i++)
            {
                var p = ptrs[i];
                if (p.Id == dragId)
                {
                    Drag(p.Pos);
                    dragging = true;
                    continue;
                }
                if (!p.Began || dragId >= 0) continue;
                if (InBars(p.Pos)) continue;
                // topmost first: later widgets draw over earlier ones
                for (int w = widgets.Length - 1; w >= 0; w--)
                {
                    var rt = widgets[w] != null ? widgets[w].Root : null;
                    if (rt == null || !UiButton.Hit(rt, p.Pos, 6f)) continue;
                    rt.GetWorldCorners(corners);
                    var centre = new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
                    dragOffset = centre - p.Pos;
                    dragId = p.Id;
                    Select(w);
                    dragging = true;
                    break;
                }
            }
            if (!dragging) dragId = -1;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) CancelAndClose();
        }

        bool InBars(Vector2 screen)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, screen, null, out local)) return false;
            float y = local.y - safe.rect.yMin;
            return y < 112f || y > safe.rect.height - 96f;
        }

        void Drag(Vector2 screen)
        {
            if (selected < 0) return;
            var w = widgets[selected];
            if (w == null || w.Root == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, screen + dragOffset, null, out local)) return;
            var r = safe.rect;
            var size = w.Root.sizeDelta;
            float hx = size.x * 0.5f / Mathf.Max(1f, r.width), hy = size.y * 0.5f / Mathf.Max(1f, r.height);
            float nx = (local.x - r.xMin) / Mathf.Max(1f, r.width);
            float ny = (local.y - r.yMin) / Mathf.Max(1f, r.height);
            // keep the control inside the area between the bars
            float barLo = 112f / Mathf.Max(1f, r.height), barHi = 96f / Mathf.Max(1f, r.height);
            nx = Mathf.Clamp(nx, hx, 1f - hx);
            ny = Mathf.Clamp(ny, barLo + hy, 1f - barHi - hy);
            ControlLayout.SetPos((ControlId)selected, new Vector2(nx, ny));
            ControlWidgets.Layout(w, ControlLayout.Mode);
            Select(selected);
        }

        void SaveAndClose()
        {
            Save.Write();
            Close();
        }

        void CancelAndClose()
        {
            var cur = ControlLayout.Current();
            Array.Copy(snapPos, cur, Mathf.Min(snapPos.Length, cur.Length));
            Save.Data.ControlPreset = snapPreset;
            Save.Data.ControlScale = snapScale;
            Save.Data.ControlAlpha = snapAlpha;
            Close();
        }

        void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UiButton.Blocked = wasBlocked;
            TouchControls.Suspended = false;
            var cb = onClose;
            onClose = null;
            Destroy(gameObject);
            if (cb != null) cb();
        }

        void OnDestroy()
        {
            if (IsOpen) { IsOpen = false; UiButton.Blocked = wasBlocked; TouchControls.Suspended = false; }
        }
    }
}
