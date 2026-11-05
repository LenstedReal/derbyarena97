using DerbyArena.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DerbyArena.UI
{
    /// <summary>
    /// Driving input. Four steering modes (ControlLayout.Mode): a steering wheel you rotate, left/right buttons, a
    /// floating swipe joystick (touch anywhere on the left half and slide — the default since 4.0.1) and tilt
    /// (accelerometer roll). Pedals and action buttons are hit-tested against the real on-screen rectangles the HUD
    /// hands over, so a custom layout works without any change here. Keyboard and gamepad remain for testing.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        /// <summary>Set while the layout editor is open: no input reaches the car or the pause button.</summary>
        public static bool Suspended;

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

        public RectTransform WheelRect, LeftRect, RightRect, PadRect;
        public RectTransform GasRect, BrakeRect, NitroRect, DriftRect, FireRect, RocketRect, PauseRect;

        // swipe joystick state for the HUD
        public bool SwipeActive { get; private set; }
        public Vector2 SwipeOrigin { get; private set; }   // screen pixels
        public float SwipeSteer { get; private set; }

        const float MaxWheel = 135f;
        int wheelId = -1, swipeId = -1;
        float lastAngle;
        bool rocketHeldLast, tiltEnabled;
        float tiltSmoothed;

        void Update()
        {
            Throttle = 0f; Brake = 0f; Nitro = false; Handbrake = false; Fire = false; Rocket = false;
            PausePressed = false; RocketPressed = false;
            if (Suspended) { Steer = 0f; SwipeActive = false; swipeId = wheelId = -1; return; }
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

            SteerMode mode = ControlLayout.Mode;
            float sens = Save.Data.Sensitivity;
            bool wheelHeld = false, swipeHeld = false;
            float buttonSteer = 0f;
            var ptrs = PointerInput.Get();
            for (int i = 0; i < ptrs.Count; i++)
            {
                var p = ptrs[i];
                // a finger that already steers keeps steering, even when it crosses a button (Driver layout)
                bool ownsSteer = (mode == SteerMode.Wheel && p.Id == wheelId) || (mode == SteerMode.Swipe && p.Id == swipeId);
                if (!ownsSteer && UiButton.Hit(PauseRect, p.Pos, 12f)) { if (p.Began) PausePressed = true; continue; }

                bool used = false;
                if (ownsSteer) goto steer;
                if (UiButton.Hit(GasRect, p.Pos, 8f)) { Throttle = 1f; used = true; }
                if (UiButton.Hit(BrakeRect, p.Pos, 8f)) { Brake = 1f; used = true; }
                if (UiButton.Hit(NitroRect, p.Pos, 8f)) { Nitro = true; used = true; }
                if (UiButton.Hit(DriftRect, p.Pos, 8f)) { Handbrake = true; used = true; }
                if (UiButton.Hit(FireRect, p.Pos, 10f)) { Fire = true; used = true; }
                if (UiButton.Hit(RocketRect, p.Pos, 8f)) { Rocket = true; used = true; }
                if (used) continue;

            steer:
                switch (mode)
                {
                    case SteerMode.Wheel:
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
                        break;
                    case SteerMode.Arrows:
                        if (UiButton.Hit(LeftRect, p.Pos, 10f)) buttonSteer -= 1f;
                        if (UiButton.Hit(RightRect, p.Pos, 10f)) buttonSteer += 1f;
                        break;
                    case SteerMode.Swipe:
                        if (p.Id == swipeId || (swipeId < 0 && p.Began && InSwipeZone(p.Pos)))
                        {
                            if (swipeId != p.Id)
                            {
                                swipeId = p.Id;
                                SwipeOrigin = p.Pos;
                            }
                            // full lock at ~13% of the screen height of travel (sensitivity shortens it); small dead zone
                            float range = Mathf.Max(40f, Screen.height * 0.13f / sens);
                            float dx = p.Pos.x - SwipeOrigin.x;
                            float dead = Screen.height * 0.006f;
                            dx = Mathf.Abs(dx) < dead ? 0f : dx - Mathf.Sign(dx) * dead;
                            SwipeSteer = Mathf.Clamp(dx / range, -1f, 1f);
                            // the origin follows the finger when it slides past full lock, so steering back is immediate
                            if (Mathf.Abs(dx) > range) SwipeOrigin = new Vector2(p.Pos.x - Mathf.Sign(dx) * (range + dead), SwipeOrigin.y);
                            swipeHeld = true;
                        }
                        break;
                }
            }

            float dt = Time.unscaledDeltaTime;
            if (!wheelHeld)
            {
                wheelId = -1;
                if (mode == SteerMode.Wheel) WheelAngle = Mathf.MoveTowards(WheelAngle, 0f, dt * 540f);
            }
            if (!swipeHeld)
            {
                swipeId = -1;
                SwipeSteer = Mathf.MoveTowards(SwipeSteer, 0f, dt * 6f);
            }
            SwipeActive = swipeHeld;

            float touchSteer;
            switch (mode)
            {
                case SteerMode.Wheel:
                    touchSteer = Mathf.Clamp(WheelAngle / (95f / sens), -1f, 1f);
                    break;
                case SteerMode.Arrows:
                    Steer = Mathf.MoveTowards(Steer, buttonSteer, dt * 4.5f * sens);
                    touchSteer = Steer;
                    WheelAngle = Steer * 90f;
                    break;
                case SteerMode.Tilt:
                    touchSteer = ReadTilt(sens, dt);
                    WheelAngle = touchSteer * 90f;
                    break;
                default:
                    touchSteer = SwipeSteer;
                    WheelAngle = touchSteer * 90f;
                    break;
            }
            // 4.0.2: a mild response curve for the finger and tilt modes. Small movements give finer corrections, so
            // the car holds a straight line instead of wobbling with every twitch of the thumb; full travel is still
            // full lock (the sensitivity setting changes how far that is). The arrow buttons already ramp.
            if (mode != SteerMode.Arrows) touchSteer = Mathf.Sign(touchSteer) * Mathf.Pow(Mathf.Abs(touchSteer), 1.3f);
            Steer = keySteering ? Mathf.Clamp(keySteer, -1f, 1f) : touchSteer;

            if (Save.Data.AutoGas && Brake < 0.5f) Throttle = Mathf.Max(Throttle, 1f);

            RocketPressed = Rocket && !rocketHeldLast;
            rocketHeldLast = Rocket;
        }

        /// <summary>
        /// Accelerometer roll as steering. Rolling the phone like a wheel (about the screen normal) moves gravity
        /// along the screen's horizontal axis; pitching the phone toward you does not touch that axis, so no
        /// calibration is needed. The Input System reports sensor values compensated for the screen orientation
        /// by default (screen +X = right), so that path reads X directly; the uncompensated paths (legacy
        /// Input.acceleration, or compensation switched off) read the device Y axis — in landscape the device's
        /// long axis is horizontal — with the sign flipped for LandscapeRight. Sensitivity sets the tilt for full
        /// lock (about 20° at 1.0).
        /// </summary>
        float ReadTilt(float sens, float dt)
        {
            Vector3 a = Vector3.zero;
            bool got = false, compensated = false;
            try
            {
                var acc = Accelerometer.current;
                if (acc != null)
                {
                    if (!tiltEnabled) { InputSystem.EnableDevice(acc); tiltEnabled = true; }
                    a = acc.acceleration.ReadValue();
                    got = true;
                    compensated = InputSystem.settings == null || InputSystem.settings.compensateForScreenOrientation;
                }
            }
            catch (System.Exception) { }
            if (!got)
            {
                try { a = Input.acceleration; got = a.sqrMagnitude > 0.01f; } catch (System.Exception) { }
            }
            if (!got) return Mathf.MoveTowards(tiltSmoothed, 0f, dt * 3f);
            float sign = Screen.orientation == ScreenOrientation.LandscapeRight ? 1f : -1f;
            float roll = compensated ? a.x : sign * a.y;        // left end down → negative → steer left
            float full = 0.34f / sens;                           // sin(20°) at sensitivity 1
            float raw = Mathf.Clamp(roll / full, -1f, 1f);
            if (Mathf.Abs(raw) < 0.06f) raw = 0f;
            tiltSmoothed = Mathf.Lerp(tiltSmoothed, raw, 1f - Mathf.Exp(-10f * dt));
            return tiltSmoothed;
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
            return InSteerZone(p);
        }

        bool InSwipeZone(Vector2 p)
        {
            if (PadRect != null && PadRect.gameObject.activeInHierarchy && UiButton.Hit(PadRect, p, 20f)) return true;
            return InSteerZone(p);
        }

        /// <summary>The free steering zone: the side of the screen the steer control lives on, below the top HUD.</summary>
        bool InSteerZone(Vector2 p)
        {
            bool leftSide = ControlLayout.Pos(ControlId.Steer).x < 0.5f;
            bool side = leftSide ? p.x < Screen.width * 0.45f : p.x > Screen.width * 0.55f;
            return side && p.y < Screen.height * 0.78f;
        }
    }
}
