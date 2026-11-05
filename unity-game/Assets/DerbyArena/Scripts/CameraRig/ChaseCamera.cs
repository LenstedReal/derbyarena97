using DerbyArena.Vehicles;
using UnityEngine;

namespace DerbyArena.CameraRig
{
    /// <summary>
    /// Third-person chase camera: speed-based distance and FOV, smoothed yaw, look-ahead, wall
    /// collision that ignores the target car, trauma-based shake, death orbit and menu orbit.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public static ChaseCamera Main;

        public Transform Target;
        public ArcadeCar Car;
        public bool Orbit;
        public Vector3 OrbitCenter;
        public float OrbitRadius = 11f;
        public float OrbitHeight = 3.5f;

        Camera cam;
        float yaw;
        float trauma;
        float orbitAngle;
        Vector3 vel;
        float introTimer, introMax = 1f;
        readonly RaycastHit[] hits = new RaycastHit[8];
        readonly Collider[] overlap = new Collider[8];

        // 4.0.2 follow filter: the car's position as the camera sees it (vertical bumps smoothed out), the
        // collision-limited boom length (pulls in fast, lets out slowly) and the extra lift used near walls
        Vector3 follow;
        bool followInit;
        float followVy;
        float boom = -1f;
        float lift;

        /// <summary>Menu framing (4.0.2): screen-space offset of the orbit target, so the car sits in the free area
        /// right of the menu panel instead of under it. x = fraction of the half-width (0.3 = 30 % to the right).</summary>
        public float OrbitScreenShift;

        void Awake()
        {
            Main = this;
            cam = GetComponent<Camera>();
            // Vehicle-only fill light: cars were reading as black silhouettes against bright skies.
            // Culling mask limits it to the vehicle layer so the world lighting is untouched.
            if (DerbyArena.Core.Save.Data.Quality > 0)
            {
                var go = new GameObject("VehicleFill");
                go.transform.SetParent(transform, false);
                go.transform.localRotation = Quaternion.Euler(25f, -20f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(0.78f, 0.86f, 1f);
                l.intensity = 0.4f;
                l.shadows = LightShadows.None;
                l.cullingMask = 1 << VehicleVisual.VehicleLayer;
                l.renderMode = LightRenderMode.Auto;
            }
        }

        /// <summary>Countdown fly-in: sweeps from the front of the car to the chase position.</summary>
        public void Intro(float seconds)
        {
            introMax = Mathf.Max(0.1f, seconds);
            introTimer = introMax;
        }

        float dollyTimer, dollyMax;
        /// <summary>Menu cinematic (4.0): the orbit starts high and far and eases down into its normal path.</summary>
        public void Dolly(float seconds)
        {
            dollyMax = Mathf.Max(0.1f, seconds);
            dollyTimer = dollyMax;
            Vector3 c = Target != null ? Target.position : OrbitCenter;
            transform.position = c + Quaternion.Euler(0f, orbitAngle - 40f, 0f) * new Vector3(0f, OrbitHeight * 4.5f, -OrbitRadius * 2.6f);
            transform.rotation = Quaternion.LookRotation(c + Vector3.up * 1.1f - transform.position);
        }

        void OnDestroy() { if (Main == this) Main = null; }

        public static void Kick(float amount)
        {
            if (Main != null) Main.trauma = Mathf.Clamp01(Main.trauma + amount);
        }

        public static void KickAt(Vector3 point, float amount)
        {
            if (Main == null) return;
            float d = Vector3.Distance(point, Main.transform.position);
            Kick(amount * Mathf.Clamp01(1f - d / 30f));
        }

        public void SnapBehind()
        {
            if (Target == null) return;
            yaw = Target.eulerAngles.y;
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            transform.position = Target.position - fwd * 8f + Vector3.up * 3.6f;
            transform.rotation = Quaternion.LookRotation(Target.position + Vector3.up * 1.2f - transform.position);
            follow = Target.position;
            followInit = true;
            followVy = 0f;
            boom = -1f;
            lift = 0f;
            vel = Vector3.zero;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            if (Orbit) { DoOrbit(dt); ApplyShake(dt); return; }
            if (Target == null) return;

            float speed = Car != null ? Car.PlanarSpeed() : 0f;
            float top = Car != null && Car.Spec != null ? Car.Spec.TopSpeed : 27f;
            float s01 = Mathf.Clamp01(speed / top);

            // follow point: horizontal position tracked tightly, height filtered (bumps and suspension travel no
            // longer shake the whole view)
            Vector3 tp = Target.position;
            if (!followInit) { follow = tp; followInit = true; }
            follow.x = tp.x;
            follow.z = tp.z;
            follow.y = Mathf.SmoothDamp(follow.y, tp.y, ref followVy, 0.22f, Mathf.Infinity, dt);
            if (Mathf.Abs(follow.y - tp.y) > 3f) follow.y = tp.y;      // teleports and big drops: catch up at once

            // yaw: stay behind the car; follow the direction of travel only a little and not while sliding sideways,
            // so a drift or a spin does not swing the camera around the car
            float targetYaw = Target.eulerAngles.y;
            if (Car != null && Car.Body != null && speed > 5f && Car.ForwardSpeed > 0f)
            {
                Vector3 v = Car.Body.linearVelocity;
                float velYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                float slip = Mathf.Clamp01(Mathf.Abs(Car.LateralSpeed) / Mathf.Max(1f, speed));
                targetYaw = Mathf.LerpAngle(targetYaw, velYaw, 0.2f * (1f - slip));
            }
            float yawRate = Mathf.Lerp(6.5f, 4.5f, s01);
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-yawRate * dt));
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            float dist = Mathf.Lerp(7.6f, 9.4f, s01);
            float height = Mathf.Lerp(3.6f, 3.3f, s01);
            Vector3 look = follow + Vector3.up * 1.3f + fwd * Mathf.Lerp(2.5f, 5.5f, s01);
            bool intro = introTimer > 0f;
            Vector3 desired;
            if (intro)
            {
                introTimer -= dt;
                float k = 1f - Mathf.Clamp01(introTimer / introMax);
                k = k * k * (3f - 2f * k);
                float ang = Mathf.Lerp(165f, 0f, k);
                Vector3 ofs = Quaternion.Euler(0f, yaw + ang, 0f) * new Vector3(0f, 0f, -Mathf.Lerp(11f, dist, k));
                desired = follow + ofs + Vector3.up * Mathf.Lerp(2.2f, height, k);
                look = Vector3.Lerp(follow + Vector3.up * 1.0f, look, k);
            }
            else desired = follow - fwd * dist + Vector3.up * height;

            // collision: cast from above the car to the wanted spot. The boom pulls in at once, lets out slowly,
            // and when it gets short the camera rises over the car instead of sliding into its roof.
            Vector3 pivot = follow + Vector3.up * 1.8f;
            Vector3 dir = desired - pivot;
            float len = dir.magnitude;
            float allowed = len;
            if (len > 0.1f)
            {
                dir /= len;
                int n = Physics.SphereCastNonAlloc(pivot, 0.45f, dir, hits, len, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i];
                    if (h.collider == null) continue;
                    if (Car != null && h.rigidbody == Car.Body) continue;
                    if (h.rigidbody != null && !h.rigidbody.isKinematic) continue;     // other cars never push the camera
                    if (h.distance > 0f && h.distance < allowed) allowed = h.distance;
                }
            }
            if (boom < 0f) boom = allowed;
            boom = allowed < boom ? allowed : Mathf.MoveTowards(boom, allowed, dt * 6f);
            float shortBy = Mathf.Clamp01((len - boom) / Mathf.Max(1f, len));
            lift = Mathf.MoveTowards(lift, shortBy * 3f, dt * 8f);
            // never past the hit (a minimum length here used to put the lens behind thin walls or pump it in and out of
            // thick ones); a short boom is made up for by the lift over the car
            float useLen = Mathf.Clamp(boom - 0.25f, 0.6f, Mathf.Max(0.6f, len));
            Vector3 want = len > 0.1f && useLen < len ? pivot + dir * useLen : desired;
            want += Vector3.up * lift;
            if (lift > 0.01f)
            {
                // the lift itself must not take the lens into a bridge deck, a pipe or a container roof
                Vector3 d2 = want - pivot;
                float l2 = d2.magnitude;
                if (l2 > 0.1f)
                {
                    d2 /= l2;
                    float ok = l2;
                    int n2 = Physics.SphereCastNonAlloc(pivot, 0.35f, d2, hits, l2, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < n2; i++)
                    {
                        var h = hits[i];
                        if (h.collider == null) continue;
                        if (Car != null && h.rigidbody == Car.Body) continue;
                        if (h.rigidbody != null && !h.rigidbody.isKinematic) continue;
                        if (h.distance > 0f && h.distance < ok) ok = h.distance;
                    }
                    if (ok < l2) want = pivot + d2 * Mathf.Max(0.4f, ok - 0.2f);
                }
            }
            desired = want;

            if (intro)
            {
                transform.position = desired;
                vel = Vector3.zero;
            }
            else transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, 0.08f, Mathf.Infinity, dt);

            // never end a frame inside geometry: step back toward the pivot until the lens is clear
            KeepOutOfWalls(pivot);

            Quaternion rot = Quaternion.LookRotation(look - transform.position, Vector3.up);
            transform.rotation = intro ? rot : Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-12f * dt));

            float fov = Mathf.Lerp(60f, 67f, s01) + (Car != null && Car.NitroActive ? 6f : 0f);
            if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-5f * dt));
            bool shaking = trauma > 0f;
            ApplyShake(dt);
            if (shaking) KeepOutOfWalls(pivot);         // a hard shake must not push the lens into a wall either
        }

        void KeepOutOfWalls(Vector3 pivot)
        {
            Vector3 p = transform.position;
            for (int step = 0; step < 6; step++)
            {
                int n = Physics.OverlapSphereNonAlloc(p, 0.3f, overlap, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < n; i++)
                {
                    var c = overlap[i];
                    if (c == null) continue;
                    if (Car != null && c.attachedRigidbody == Car.Body) continue;
                    if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) continue;
                    blocked = true;
                    break;
                }
                if (!blocked) break;
                p = Vector3.Lerp(p, pivot + Vector3.up * 0.6f, 0.35f);
            }
            if (p != transform.position)
            {
                transform.position = p;
                vel = Vector3.zero;
            }
        }

        void DoOrbit(float dt)
        {
            float radius = OrbitRadius, height = OrbitHeight, fov = 50f;
            if (dollyTimer > 0f)
            {
                dollyTimer -= dt;
                float k = 1f - Mathf.Clamp01(dollyTimer / dollyMax);
                float e = 1f - Mathf.Pow(1f - k, 3f);                    // ease-out: fast start, gentle landing
                radius = Mathf.Lerp(OrbitRadius * 2.6f, OrbitRadius, e);
                height = Mathf.Lerp(OrbitHeight * 4.5f, OrbitHeight, e);
                fov = Mathf.Lerp(42f, 50f, e);
                orbitAngle += dt * Mathf.Lerp(26f, 9f, e);
            }
            else orbitAngle += dt * 9f;
            Vector3 c = Target != null ? Target.position : OrbitCenter;
            Vector3 p = c + Quaternion.Euler(0f, orbitAngle, 0f) * new Vector3(0f, height, -radius);
            float followRate = dollyTimer > 0f ? 8f : 3f;
            transform.position = Vector3.Lerp(transform.position, p, 1f - Mathf.Exp(-followRate * dt));
            // aim left of the car by the requested share of the visible half-width: the car appears right of centre
            Vector3 aim = c + Vector3.up * 1.1f;
            if (OrbitScreenShift != 0f && cam != null)
            {
                float d = Vector3.Distance(transform.position, aim);
                float halfW = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
                aim -= transform.right * (halfW * OrbitScreenShift);
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim - transform.position), 1f - Mathf.Exp(-4f * dt));
            if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-3f * dt));
        }

        void ApplyShake(float dt)
        {
            if (trauma <= 0f) return;
            float s = trauma * trauma;
            float t = Time.unscaledTime * 28f;
            transform.position += transform.right * (Mathf.PerlinNoise(t, 0.1f) - 0.5f) * s * 0.8f
                                + transform.up * (Mathf.PerlinNoise(0.3f, t) - 0.5f) * s * 0.6f;
            transform.rotation *= Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, t) - 0.5f) * s * 6f);
            trauma = Mathf.Max(0f, trauma - dt * 1.6f);
        }
    }
}
