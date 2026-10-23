using DerbyArena.Mission;
using DerbyArena.Vehicles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DerbyArena.UI
{
    public sealed class HudController : MonoBehaviour
    {
        public MissionDirector Mission;
        public VehicleHealth Health;
        public WheelDrive Drive;
        public TouchControls Controls;
        public System.Action Restart;
        public bool Paused;

        Text hp;
        Text cells;
        Text time;
        Text objective;
        Text nitro;
        GameObject pausePanel;
        Rect safe;

        public void Build()
        {
            safe = Screen.safeArea;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            hp = Label(canvasGo.transform, "Can", new Vector2(0.02f, 0.90f), new Vector2(0.28f, 0.98f), TextAnchor.MiddleLeft);
            time = Label(canvasGo.transform, "Sure", new Vector2(0.36f, 0.90f), new Vector2(0.64f, 0.98f), TextAnchor.MiddleCenter);
            cells = Label(canvasGo.transform, "Hucre", new Vector2(0.66f, 0.90f), new Vector2(0.84f, 0.98f), TextAnchor.MiddleRight);
            nitro = Label(canvasGo.transform, "Nitro", new Vector2(0.02f, 0.82f), new Vector2(0.28f, 0.90f), TextAnchor.MiddleLeft);
            objective = Label(canvasGo.transform, "Hedef", new Vector2(0.18f, 0.74f), new Vector2(0.82f, 0.84f), TextAnchor.MiddleCenter);
            Pad(canvasGo.transform, new Color(1f, 1f, 1f, 0.12f), new Vector2(0.03f, 0.08f), new Vector2(0.22f, 0.38f));
            Pad(canvasGo.transform, new Color(0.15f, 0.55f, 0.25f, 0.45f), new Vector2(0.58f, 0.05f), new Vector2(0.74f, 0.26f), "GAZ");
            Pad(canvasGo.transform, new Color(0.65f, 0.18f, 0.14f, 0.5f), new Vector2(0.76f, 0.05f), new Vector2(0.94f, 0.22f), "FREN");
            Pad(canvasGo.transform, new Color(0.15f, 0.4f, 0.85f, 0.5f), new Vector2(0.56f, 0.30f), new Vector2(0.72f, 0.46f), "NITRO");
            Pad(canvasGo.transform, new Color(0.25f, 0.27f, 0.32f, 0.55f), new Vector2(0.74f, 0.28f), new Vector2(0.94f, 0.44f), "EL FREN");
            Pad(canvasGo.transform, new Color(0.75f, 0.22f, 0.12f, 0.55f), new Vector2(0.72f, 0.50f), new Vector2(0.96f, 0.86f), "ATES");
            var pause = Button(canvasGo.transform, "DURAKLAT", new Vector2(0.84f, 0.90f), new Vector2(0.98f, 0.98f));
            pause.onClick.AddListener(TogglePause);
            pausePanel = new GameObject("Pause");
            pausePanel.transform.SetParent(canvasGo.transform, false);
            var dim = pausePanel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(pausePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var resume = Button(pausePanel.transform, "DEVAM", new Vector2(0.35f, 0.52f), new Vector2(0.65f, 0.62f));
            resume.onClick.AddListener(TogglePause);
            var again = Button(pausePanel.transform, "YENIDEN", new Vector2(0.35f, 0.38f), new Vector2(0.65f, 0.48f));
            again.onClick.AddListener(() => { if (Restart != null) Restart(); });
            pausePanel.SetActive(false);
        }

        public void TogglePause()
        {
            if (Mission != null && (Mission.Rules.Phase == MissionPhase.Won || Mission.Rules.Phase == MissionPhase.Lost))
                return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f;
            pausePanel.SetActive(Paused);
        }

        void Update()
        {
            if (Controls != null && Controls.PausePressed)
                TogglePause();
            if (Health != null)
                hp.text = "CAN " + Mathf.CeilToInt(Health.Current);
            if (Drive != null)
                nitro.text = "NITRO " + Mathf.RoundToInt(Drive.Nitro * 100f) + "%";
            if (Mission == null)
                return;
            int t = Mathf.Max(0, Mathf.CeilToInt(Mission.Rules.TimeLeft));
            time.text = (t / 60) + ":" + (t % 60).ToString("00");
            cells.text = "HUCRE " + Mission.Rules.Cells + "/3";
            if (Mission.Rules.Phase == MissionPhase.Hunt)
                objective.text = "Reaktor hucrelerini topla";
            else if (Mission.Rules.Phase == MissionPhase.Extract)
                objective.text = "Tahliye alaninda kal  " + Mathf.CeilToInt(Mission.Rules.ExtractHold) + "/6";
            else if (Mission.Rules.Phase == MissionPhase.Won)
                objective.text = "BLACKOUT RUN TAMAM";
            else
                objective.text = "KAYBEDILDI";
        }

        Text Label(Transform parent, string name, Vector2 a, Vector2 b, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(go.GetComponent<RectTransform>(), a, b);
            return text;
        }

        void Pad(Transform parent, Color color, Vector2 a, Vector2 b, string caption = null)
        {
            var go = new GameObject("Pad");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Stretch(go.GetComponent<RectTransform>(), a, b);
            if (caption != null)
            {
                var text = Label(go.transform, "T", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
                text.text = caption;
                text.fontSize = 28;
            }
        }

        Button Button(Transform parent, string caption, Vector2 a, Vector2 b)
        {
            var go = new GameObject(caption);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            var btn = go.AddComponent<Button>();
            Stretch(go.GetComponent<RectTransform>(), a, b);
            var text = Label(go.transform, "Label", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            text.text = caption;
            text.raycastTarget = false;
            return btn;
        }

        static void Stretch(RectTransform rt, Vector2 a, Vector2 b)
        {
            rt.anchorMin = a;
            rt.anchorMax = b;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
