using DerbyArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DerbyArena.UI
{
    /// <summary>
    /// Driving input: on-screen steering wheel (rotate with a finger) or arrow buttons, pedals and
    /// action buttons hit-tested against their real on-screen rectangles, plus keyboard / gamepad.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        public float Steer;
        public float Throttle;
        public float Brake;
        public bool Nitro;
        public bool Handbrake;
        public bool Fire;
        public bool Rocket;
        public bool RocketPressed;
        public bool PausePressed;
        public float WheelAngle;      // degrees, for the HUD

        public RectTransform WheelRect, LeftRect, RightRect;
        public RectTransform GasRect, BrakeRect, NitroRect, DriftRect, FireRect, RocketRect, PauseRect;

        const float MaxWheel = 135f;
        int wheelId = -1;
        float lastAngle;
        bool rocketHeldLast;

        void Update()
        {
            Throttle = 0f; Brake = 0f; Nitro = false; Handbrake = false; Fire = false; Rocket = false;
            PausePressed = false; RocketPressed = false;
            float keySteer = 0f;
            bool keySteering = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) { keySteer -= 1f; keySteering = true; }
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) { keySteer += 1f; keySteering = true; }
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) Throttle = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) Brake = 1f;
                if (kb.spaceKey.isPressed) Handbrake = true;
                if (kb.leftShiftKey.isPressed) Nitro = true;
                if (kb.jKey.isPressed || kb.fKey.isPressed) Fire = true;
                if (kb.kKey.isPressed || kb.rKey.isPressed) Rocket = true;
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) PausePressed = true;
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                float lx = pad.leftStick.ReadValue().x;
                if (Mathf.Abs(lx) > 0.12f) { keySteer = lx; keySteering = true; }
                Throttle = Mathf.Max(Throttle, pad.rightTrigger.ReadValue());
                Brake = Mathf.Max(Brake, pad.leftTrigger.ReadValue());
                if (pad.buttonSouth.isPressed) Fire = true;
                if (pad.buttonEast.isPressed) Rocket = true;
                if (pad.buttonWest.isPressed) Handbrake = true;
                if (pad.rightShoulder.isPressed) Nitro = true;
                if (pad.startButton.wasPressedThisFrame) PausePressed = true;
            }

            bool wheelMode = Save.Data.ControlMode == 0;
            bool wheelHeld = false;
            float buttonSteer = 0f;
            var ptrs = PointerInput.Get();
            for (int i = 0; i < ptrs.Count; i++)
            {
                var p = ptrs[i];
                if (UiButton.Hit(PauseRect, p.Pos, 12f)) { if (p.Began) PausePressed = true; continue; }

                bool used = false;
                if (UiButton.Hit(GasRect, p.Pos, 8f)) { Throttle = 1f; used = true; }
                if (UiButton.Hit(BrakeRect, p.Pos, 8f)) { Brake = 1f; used = true; }
                if (UiButton.Hit(NitroRect, p.Pos, 8f)) { Nitro = true; used = true; }
                if (UiButton.Hit(DriftRect, p.Pos, 8f)) { Handbrake = true; used = true; }
                if (UiButton.Hit(FireRect, p.Pos, 10f)) { Fire = true; used = true; }
                if (UiButton.Hit(RocketRect, p.Pos, 8f)) { Rocket = true; used = true; }
                if (used) continue;

                if (wheelMode)
                {
                    if (p.Id == wheelId || (wheelId < 0 && p.Began && NearWheel(p.Pos)))
                    {
                        Vector2 c = Center(WheelRect);
                        Vector2 d = p.Pos - c;
                        float ang = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;   // 0 = up, clockwise positive
                        if (wheelId != p.Id)
                        {
                            wheelId = p.Id;
                            lastAngle = ang;
                        }
                        if (d.magnitude > Radius(WheelRect) * 0.18f)
                        {
                            WheelAngle = Mathf.Clamp(WheelAngle + Mathf.DeltaAngle(lastAngle, ang), -MaxWheel, MaxWheel);
                            lastAngle = ang;
                        }
                        wheelHeld = true;
                    }
                }
                else
                {
                    if (UiButton.Hit(LeftRect, p.Pos, 10f)) buttonSteer -= 1f;
                    if (UiButton.Hit(RightRect, p.Pos, 10f)) buttonSteer += 1f;
                }
            }

            float dt = Time.unscaledDeltaTime;
            if (!wheelHeld)
            {
                wheelId = -1;
                WheelAngle = Mathf.MoveTowards(WheelAngle, 0f, dt * 540f);
            }

            float sens = Save.Data.Sensitivity;
            float touchSteer;
            if (wheelMode)
            {
                touchSteer = Mathf.Clamp(WheelAngle / (95f / sens), -1f, 1f);
            }
            else
            {
                Steer = Mathf.MoveTowards(Steer, buttonSteer, dt * 4.5f * sens);
                touchSteer = Steer;
                WheelAngle = Steer * 90f;
            }
            Steer = keySteering ? Mathf.Clamp(keySteer, -1f, 1f) : touchSteer;

            if (Save.Data.AutoGas && Brake < 0.5f) Throttle = Mathf.Max(Throttle, 1f);

            RocketPressed = Rocket && !rocketHeldLast;
            rocketHeldLast = Rocket;
        }

        static readonly Vector3[] corners = new Vector3[4];

        static Vector2 Center(RectTransform rt)
        {
            if (rt == null) return new Vector2(Screen.width * 0.13f, Screen.height * 0.25f);
            rt.GetWorldCorners(corners);
            return new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
        }

        static float Radius(RectTransform rt)
        {
            if (rt == null) return Screen.height * 0.15f;
            rt.GetWorldCorners(corners);
            return (corners[2].x - corners[0].x) * 0.5f;
        }

        bool NearWheel(Vector2 p)
        {
            if (WheelRect == null || !WheelRect.gameObject.activeInHierarchy) return false;
            if ((p - Center(WheelRect)).magnitude < Radius(WheelRect) * 1.45f) return true;
            return p.x < Screen.width * 0.4f && p.y < Screen.height * 0.75f;
        }
    }
}
