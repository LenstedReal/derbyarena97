using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DerbyArena.UI
{
    public sealed class TouchControls : MonoBehaviour
    {
        public float Steer;
        public float Throttle;
        public float Brake;
        public bool Nitro;
        public bool Handbrake;
        public bool Fire;
        public bool Rocket;
        public float AimYaw;
        public bool PausePressed;

        int steerId = -1;
        Vector2 steerOrigin;
        int aimId = -1;
        Vector2 aimLast;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Update()
        {
            Throttle = 0f;
            Brake = 0f;
            Nitro = false;
            Handbrake = false;
            Fire = false;
            Rocket = false;
            PausePressed = false;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed) Steer = -1f;
                else if (kb.dKey.isPressed) Steer = 1f;
                else if (steerId < 0) Steer = 0f;
                if (kb.wKey.isPressed) Throttle = 1f;
                if (kb.sKey.isPressed) Brake = 1f;
                if (kb.leftShiftKey.isPressed) Handbrake = true;
                if (kb.spaceKey.isPressed) Nitro = true;
                if (kb.fKey.isPressed) Fire = true;
                if (kb.rKey.isPressed) Rocket = true;
                if (kb.escapeKey.wasPressedThisFrame) PausePressed = true;
            }
            if (Touchscreen.current == null)
                return;
            bool steerHeld = false;
            bool aimHeld = false;
            foreach (var t in Touch.activeTouches)
            {
                var p = t.screenPosition;
                var n = new Vector2(p.x / Screen.width, p.y / Screen.height);
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began && n.y > 0.88f && n.x > 0.75f)
                    PausePressed = true;
                if (n.x < 0.36f && n.y < 0.72f)
                {
                    if (t.phase == UnityEngine.InputSystem.TouchPhase.Began || steerId < 0)
                    {
                        steerId = t.touchId;
                        steerOrigin = p;
                    }
                    if (t.touchId == steerId)
                    {
                        steerHeld = true;
                        Steer = Mathf.Clamp((p.x - steerOrigin.x) / 90f, -1f, 1f);
                    }
                }
                else if (n.x > 0.55f)
                {
                    if (Inside(n, 0.58f, 0.74f, 0.05f, 0.28f)) Throttle = 1f;
                    if (Inside(n, 0.76f, 0.94f, 0.05f, 0.24f)) Brake = 1f;
                    if (Inside(n, 0.56f, 0.72f, 0.30f, 0.46f)) Nitro = true;
                    if (Inside(n, 0.74f, 0.94f, 0.28f, 0.44f)) Handbrake = true;
                    if (Inside(n, 0.72f, 0.96f, 0.50f, 0.86f)) Fire = true;
                    bool button = Inside(n, 0.56f, 0.96f, 0.05f, 0.46f) || Inside(n, 0.72f, 0.96f, 0.50f, 0.86f);
                    if (!button)
                    {
                        if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                        {
                            aimId = t.touchId;
                            aimLast = p;
                        }
                        if (t.touchId == aimId)
                        {
                            aimHeld = true;
                            AimYaw += (p.x - aimLast.x) * 0.15f;
                            aimLast = p;
                        }
                    }
                }
            }
            if (!steerHeld && (kb == null || (!kb.aKey.isPressed && !kb.dKey.isPressed)))
            {
                Steer = Mathf.MoveTowards(Steer, 0f, Time.deltaTime * 4f);
                steerId = -1;
            }
            if (!aimHeld)
                aimId = -1;
            AimYaw = Mathf.Clamp(AimYaw, -50f, 50f);
        }

        static bool Inside(Vector2 n, float x0, float x1, float y0, float y1)
        {
            return n.x >= x0 && n.x <= x1 && n.y >= y0 && n.y <= y1;
        }
    }
}
