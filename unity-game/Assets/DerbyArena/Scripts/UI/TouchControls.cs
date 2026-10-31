using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DerbyArena.UI
{
    /// <summary>
    /// Touch / mouse / keyboard input. Pointers are read from the new Input System first and fall back
    /// to the legacy Input class, so touch works whichever input backend the build ends up with.
    /// Button hit-testing uses the real on-screen rectangles registered by the HUD, so what you see is
    /// exactly what you touch (also with notches / safe areas).
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
        public float AimYaw;
        public bool PausePressed;

        // HUD feedback
        public Vector2 StickVector;
        public bool AnyTap;
        public Vector2 TapPosition;

        // Registered by the HUD (screen-space rectangles).
        public RectTransform StickRect;
        public RectTransform GasRect;
        public RectTransform BrakeRect;
        public RectTransform NitroRect;
        public RectTransform HandRect;
        public RectTransform FireRect;
        public RectTransform RocketRect;
        public RectTransform PauseRect;
        public float StickRadiusFactor = 0.56f;

        struct Ptr
        {
            public int Id;
            public Vector2 Pos;
            public bool Began;
        }

        readonly List<Ptr> ptrs = new List<Ptr>(8);
        static readonly Vector3[] corners = new Vector3[4];
        int steerId = -1;
        int aimId = -1;
        Vector2 aimLast;

        void OnEnable()
        {
            try { EnhancedTouchSupport.Enable(); }
            catch (Exception ex) { Debug.LogWarning("DERBY: EnhancedTouch unavailable: " + ex.Message); }
        }

        public static bool Hit(RectTransform rt, Vector2 p, float pad)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return false;
            rt.GetWorldCorners(corners);
            return p.x >= corners[0].x - pad && p.x <= corners[2].x + pad &&
                   p.y >= corners[0].y - pad && p.y <= corners[2].y + pad;
        }

        void Gather()
        {
            ptrs.Clear();
            bool gotTouch = false;
            try
            {
                if (Touchscreen.current != null)
                {
                    gotTouch = true;
                    foreach (var t in Touch.activeTouches)
                    {
                        var q = new Ptr();
                        q.Id = t.touchId;
                        q.Pos = t.screenPosition;
                        q.Began = t.phase == UnityEngine.InputSystem.TouchPhase.Began;
                        ptrs.Add(q);
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
                        var q = new Ptr();
                        q.Id = t.fingerId;
                        q.Pos = t.position;
                        q.Began = t.phase == UnityEngine.TouchPhase.Began;
                        ptrs.Add(q);
                    }
                }
                catch (Exception) { }
            }

            if (ptrs.Count == 0)
            {
                // Mouse (editor / desktop testing).
                try
                {
                    var mouse = Mouse.current;
                    if (mouse != null && mouse.leftButton.isPressed)
                    {
                        var q = new Ptr();
                        q.Id = -100;
                        q.Pos = mouse.position.ReadValue();
                        q.Began = mouse.leftButton.wasPressedThisFrame;
                        ptrs.Add(q);
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        if (Input.GetMouseButton(0))
                        {
                            var q = new Ptr();
                            q.Id = -100;
                            q.Pos = Input.mousePosition;
                            q.Began = Input.GetMouseButtonDown(0);
                            ptrs.Add(q);
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        void GetStick(out Vector2 center, out float radius)
        {
            if (StickRect != null && StickRect.gameObject.activeInHierarchy)
            {
                StickRect.GetWorldCorners(corners);
                center = new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
                radius = Mathf.Max(20f, (corners[2].x - corners[0].x) * 0.5f * StickRadiusFactor);
            }
            else
            {
                center = new Vector2(Screen.width * 0.125f, Screen.height * 0.23f);
                radius = Screen.height * 0.12f;
            }
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
            AnyTap = false;

            bool keyboardSteer = false;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed) { Steer = -1f; keyboardSteer = true; }
                else if (kb.dKey.isPressed) { Steer = 1f; keyboardSteer = true; }
                if (kb.wKey.isPressed) Throttle = 1f;
                if (kb.sKey.isPressed) Brake = 1f;
                if (kb.leftShiftKey.isPressed) Handbrake = true;
                if (kb.spaceKey.isPressed) Nitro = true;
                if (kb.fKey.isPressed) Fire = true;
                if (kb.rKey.isPressed) Rocket = true;
                if (kb.escapeKey.wasPressedThisFrame) PausePressed = true;
            }

            Gather();

            bool steerHeld = false;
            bool aimHeld = false;
            Vector2 stickCenter;
            float stickRadius;
            GetStick(out stickCenter, out stickRadius);

            for (int i = 0; i < ptrs.Count; i++)
            {
                var q = ptrs[i];
                if (q.Began)
                {
                    AnyTap = true;
                    TapPosition = q.Pos;
                }

                if (Hit(PauseRect, q.Pos, 10f))
                {
                    if (q.Began) PausePressed = true;
                    continue;
                }

                bool onButton = false;
                if (Hit(GasRect, q.Pos, 6f)) { Throttle = 1f; onButton = true; }
                if (Hit(BrakeRect, q.Pos, 6f)) { Brake = 1f; onButton = true; }
                if (Hit(NitroRect, q.Pos, 6f)) { Nitro = true; onButton = true; }
                if (Hit(HandRect, q.Pos, 6f)) { Handbrake = true; onButton = true; }
                if (Hit(FireRect, q.Pos, 6f)) { Fire = true; onButton = true; }
                if (Hit(RocketRect, q.Pos, 6f)) { Rocket = true; onButton = true; }
                if (onButton) continue;

                bool inStickZone = q.Pos.x < Screen.width * 0.42f && q.Pos.y < Screen.height * 0.80f;
                if (inStickZone && (steerId < 0 || q.Id == steerId))
                {
                    steerId = q.Id;
                    steerHeld = true;
                    Vector2 d = (q.Pos - stickCenter) / stickRadius;
                    if (d.magnitude > 1f) d.Normalize();
                    StickVector = d;
                    Steer = Mathf.Abs(d.x) < 0.08f ? 0f : d.x;
                }
                else if (q.Pos.x > Screen.width * 0.5f)
                {
                    if (q.Began)
                    {
                        aimId = q.Id;
                        aimLast = q.Pos;
                    }
                    if (q.Id == aimId)
                    {
                        aimHeld = true;
                        AimYaw += (q.Pos.x - aimLast.x) * 0.15f;
                        aimLast = q.Pos;
                    }
                }
            }

            if (!steerHeld)
            {
                steerId = -1;
                StickVector = Vector2.Lerp(StickVector, Vector2.zero, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
                if (!keyboardSteer)
                    Steer = Mathf.MoveTowards(Steer, 0f, Time.unscaledDeltaTime * 6f);
            }
            if (!aimHeld)
                aimId = -1;
            AimYaw = Mathf.Clamp(AimYaw, -50f, 50f);
        }
    }
}
