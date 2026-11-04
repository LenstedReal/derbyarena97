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
            transform.position = Target.position - fwd * 8f + Vector3.up * 3.4f;
            transform.rotation = Quaternion.LookRotation(Target.position + Vector3.up * 1.2f - transform.position);
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

            float targetYaw = Target.eulerAngles.y;
            if (Car != null && Car.Body != null && speed > 4f)
            {
                Vector3 v = Car.Body.linearVelocity;
                float velYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                if (Car.ForwardSpeed > 0f) targetYaw = Mathf.LerpAngle(targetYaw, velYaw, 0.35f);
            }
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-4.2f * dt));
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            float dist = Mathf.Lerp(7.4f, 9.6f, s01);
            float height = Mathf.Lerp(3.3f, 3.0f, s01);
            Vector3 look = Target.position + Vector3.up * 1.3f + fwd * Mathf.Lerp(2.5f, 6f, s01);
            Vector3 desired = Target.position - fwd * dist + Vector3.up * height;
            bool intro = introTimer > 0f;
            if (intro)
            {
                introTimer -= dt;
                float k = 1f - Mathf.Clamp01(introTimer / introMax);
                k = k * k * (3f - 2f * k);
                float ang = Mathf.Lerp(165f, 0f, k);
                Vector3 ofs = Quaternion.Euler(0f, yaw + ang, 0f) * new Vector3(0f, 0f, -Mathf.Lerp(11f, dist, k));
                desired = Target.position + ofs + Vector3.up * Mathf.Lerp(2.2f, height, k);
                look = Vector3.Lerp(Target.position + Vector3.up * 1.0f, look, k);
            }

            Vector3 pivot = Target.position + Vector3.up * 1.6f;
            Vector3 dir = desired - pivot;
            float len = dir.magnitude;
            if (len > 0.1f)
            {
                dir /= len;
                int n = Physics.SphereCastNonAlloc(pivot, 0.4f, dir, hits, len, ~0, QueryTriggerInteraction.Ignore);
                float best = len;
                for (int i = 0; i < n; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (Car != null && hits[i].rigidbody == Car.Body) continue;
                    if (hits[i].rigidbody != null && !hits[i].rigidbody.isKinematic) continue;
                    if (hits[i].distance > 0f && hits[i].distance < best) best = hits[i].distance;
                }
                if (best < len) desired = pivot + dir * Mathf.Max(1.6f, best - 0.3f);
            }

            if (intro)
            {
                transform.position = desired;
                vel = Vector3.zero;
                transform.rotation = Quaternion.LookRotation(look - transform.position, Vector3.up);
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, 0.09f, Mathf.Infinity, dt);
                Quaternion rot = Quaternion.LookRotation(look - transform.position, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-10f * dt));
            }

            float fov = Mathf.Lerp(60f, 68f, s01) + (Car != null && Car.NitroActive ? 7f : 0f);
            if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-5f * dt));
            ApplyShake(dt);
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
            float follow = dollyTimer > 0f ? 8f : 3f;
            transform.position = Vector3.Lerp(transform.position, p, 1f - Mathf.Exp(-follow * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(c + Vector3.up * 1.1f - transform.position), 1f - Mathf.Exp(-4f * dt));
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
