using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Raycast-suspension arcade car. Four spring rays hold the body, a lateral grip force removes
    /// sideways sliding, yaw is driven directly toward a target rate, and roll/pitch are damped, so the
    /// car is predictable on a phone, does not flip, and drifts only when the handbrake is held.
    /// Replaces the WheelCollider setup that jittered, flipped and kept drift grip after release.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCar : MonoBehaviour
    {
        // input (0..1 / -1..1)
        public float Throttle;
        public float Steer;
        public float Brake;
        public bool Handbrake;
        public bool NitroHeld;
        public bool Locked;

        // modifiers
        public float SpeedMul = 1f;
        public float NitroDrainMul = 1f;
        public float NitroRegenMul = 1f;

        public float Nitro = 1f;
        public bool NitroActive { get; private set; }
        public bool Grounded { get; private set; }
        public bool Drifting { get; private set; }
        public float ForwardSpeed { get; private set; }
        public float LateralSpeed { get; private set; }
        public VehicleSpec Spec { get; private set; }
        public Rigidbody Body { get; private set; }
        public BoxCollider Hull { get; private set; }

        const float Rest = 0.42f;
        const float RestCompression = 0.17f;

        Vector3[] anchors = new Vector3[4];
        float[] hitDist = new float[4];
        float[] lastComp = new float[4];
        float[] lastTrue = new float[4];
        float[] riseLp = new float[4];
        bool[] wheelHit = new bool[4];
        float launchTimer;
        float radius;
        float castRadius;
        float climbCooldown;
        Transform[] wheels;
        Quaternion[] wheelBase;
        float spin;
        float flipTimer;
        float visualSteer;

        public void Configure(VehicleSpec spec)
        {
            Spec = spec;
            Body = GetComponent<Rigidbody>();
            Body.mass = spec.Mass;
            Body.linearDamping = 0.02f;
            Body.angularDamping = 0.6f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity = 8f;

            Hull = gameObject.AddComponent<BoxCollider>();
            var pm = new PhysicsMaterial("CarHull");
            pm.dynamicFriction = 0.15f;
            pm.staticFriction = 0.15f;
            pm.bounciness = 0.12f;
            pm.frictionCombine = PhysicsMaterialCombine.Minimum;
            pm.bounceCombine = PhysicsMaterialCombine.Average;
            Hull.sharedMaterial = pm;

            float s = spec.Scale;
            Hull.size = spec.BodySize * s;
            Hull.center = new Vector3(0f, spec.BodyCenterY * s, 0f);
            Body.centerOfMass = new Vector3(0f, 0.35f * s, 0.05f);

            radius = spec.WheelRadius * s;
            castRadius = Mathf.Clamp(radius * 0.38f, 0.08f, 0.22f);
            Vector3 f = spec.WheelFront * s;
            Vector3 r = spec.WheelRear * s;
            SetAnchors(new[]
            {
                new Vector3(-f.x, f.y, f.z), new Vector3(f.x, f.y, f.z),
                new Vector3(-r.x, r.y, r.z), new Vector3(r.x, r.y, r.z)
            });
        }

        /// <summary>Wheel centres in local space: FL, FR, RL, RR.</summary>
        public void SetAnchors(Vector3[] wheelCentres)
        {
            for (int i = 0; i < 4 && i < wheelCentres.Length; i++)
            {
                anchors[i] = wheelCentres[i] + Vector3.up * (Rest - RestCompression);
                hitDist[i] = Rest + radius;
            }
        }

        public void BindWheels(Transform[] fl_fr_rl_rr)
        {
            if (fl_fr_rl_rr == null || fl_fr_rl_rr.Length != 4) return;
            wheels = fl_fr_rl_rr;
            wheelBase = new Quaternion[4];
            var centres = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                wheels[i].SetParent(transform, true);
                wheelBase[i] = wheels[i].localRotation;
                centres[i] = wheels[i].localPosition;
            }
            SetAnchors(centres);
        }

        /// <summary>Resize the hull to the visual model (local-space bounds), keeping ground clearance.</summary>
        public void FitHull(Bounds local)
        {
            if (Hull == null) return;
            float bottom = Mathf.Max(local.min.y + 0.4f, radius * 1.05f);
            float top = Mathf.Min(local.max.y, bottom + 1.3f * Spec.Scale);
            if (top - bottom < 0.4f) top = bottom + 0.6f;
            Hull.size = new Vector3(local.size.x * 0.92f, top - bottom, local.size.z * 0.94f);
            Hull.center = new Vector3(local.center.x, (top + bottom) * 0.5f, local.center.z);
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = pos;
            Body.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            flipTimer = 0f;
            for (int i = 0; i < 4; i++) { lastComp[i] = 0f; lastTrue[i] = 0f; riseLp[i] = 0f; }
        }

        /// <summary>An outside push (explosion knock-up) may lift the car off the ground for a moment without the
        /// bump cap taking it back.</summary>
        public void Launched(float seconds)
        {
            launchTimer = Mathf.Max(launchTimer, seconds);
        }

        void Recover()
        {
            float yaw = transform.eulerAngles.y;
            Teleport(transform.position + Vector3.up * 1.4f, yaw);
        }

        const float ClimbRate = 4.5f;      // m/s the spring may compress on a step (tyre climbing a curb)
        const float RampSlope = 0.36f;     // the step limit never holds back a ramp of up to ~20 degrees (tan 20)
        const float MaxCompVel = 3.5f;     // damper input cap while extending (m/s)
        const float MaxLandVel = 12f;      // damper input cap while compressing (landing) (m/s)
        const float MaxBumpRise = 3.2f;    // speed along the car's up axis a bump may give while the wheels are down (m/s)
        const float RiseLength = 1.5f;     // m: ground rise must last this far to reach the damper (ramps yes, bumps no)
        const float RiseSlope = 0.16f;     // ground rise fed to the damper is capped at a 9-degree ramp at the current speed

        /// <summary>
        /// Wheel probe: a short sphere cast (tyre-sized footprint) with a ray fallback. Hits on steep faces (walls, the
        /// side of a pipe) are ignored so a wheel brushing a wall is not lifted by it.
        /// </summary>
        bool Probe(Vector3 origin, Vector3 up, float maxLen, out float dist, out Vector3 normal)
        {
            RaycastHit hit;
            if (Physics.SphereCast(origin + up * castRadius, castRadius, -up, out hit, maxLen, ~0, QueryTriggerInteraction.Ignore)
                && hit.rigidbody != Body && Vector3.Dot(hit.normal, up) > 0.55f)
            {
                dist = Mathf.Clamp(hit.distance, 0f, maxLen);
                normal = hit.normal;
                return true;
            }
            if (Physics.Raycast(origin, -up, out hit, maxLen, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody != Body)
            {
                dist = hit.distance;
                normal = hit.normal;
                return true;
            }
            dist = maxLen;
            normal = up;
            return false;
        }

        /// <summary>
        /// Low-obstacle assist (4.0.2): when the hull runs into something whose every contact is low on the car (a curb,
        /// the foot of a pipe, debris) the car gets a small lift so it climbs over instead of stopping dead or rearing up.
        /// Walls touch the hull high as well and are left alone.
        /// </summary>
        void OnCollisionStay(Collision col)
        {
            if (Spec == null || Body == null || climbCooldown > 0f || Locked) return;
            if (col.rigidbody != null && !col.rigidbody.isKinematic) return;     // other cars are rammed, not climbed
            int n = col.contactCount;
            if (n == 0) return;
            float baseY = transform.position.y;
            float maxRel = float.MinValue;
            Vector3 avgN = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                var cp = col.GetContact(i);
                maxRel = Mathf.Max(maxRel, cp.point.y - baseY);
                avgN += cp.normal;
            }
            avgN.Normalize();
            float speed = Mathf.Abs(ForwardSpeed);
            if (maxRel > 0.62f * Spec.Scale || Vector3.Dot(avgN, Vector3.up) > 0.6f || speed < 2f) return;
            if (Vector3.Dot(avgN, transform.forward * Mathf.Sign(ForwardSpeed)) > -0.3f) return;   // not in our way
            Body.AddForce(Vector3.up * Mathf.Clamp(speed * 0.18f, 0.6f, 2.2f), ForceMode.VelocityChange);
            climbCooldown = 0.35f;
        }

        void FixedUpdate()
        {
            if (Spec == null || Body == null) return;
            if (climbCooldown > 0f) climbCooldown -= Time.fixedDeltaTime;
            if (launchTimer > 0f) launchTimer -= Time.fixedDeltaTime;
            float dt = Time.fixedDeltaTime;
            Vector3 up = transform.up;
            float maxLen = Rest + radius;
            float k = Body.mass * -Physics.gravity.y / 4f / RestCompression;
            float c = 2f * Mathf.Sqrt(k * Body.mass / 4f) * 0.75f;
            int grounded = 0;
            Vector3 normalSum = Vector3.zero;
            float along = Mathf.Abs(Vector3.Dot(Body.linearVelocity, transform.forward));
            float rampRise = along * RampSlope;
            float riseCap = along * RiseSlope;

            for (int i = 0; i < 4; i++)
            {
                Vector3 origin = transform.TransformPoint(anchors[i]);
                float dist;
                Vector3 normal;
                if (Probe(origin, up, maxLen, out dist, out normal))
                {
                    // 4.0.2 rough-ground handling (checked in a pitch-plane simulation against 4.0.1: on a rough floor
                    // the body shakes 2-3x less and practically never leaves the ground, a 30 cm curb lifts it ~2.5x
                    // less, 9-degree jump ramps launch as before up to nitro speed).
                    //  * The tyre is a small sphere: it rolls over seams, curbs and pipe feet instead of catching them.
                    //  * The ground may push the spring in only as fast as a tyre climbs a curb, plus what a ramp of up
                    //    to ~20 degrees gives at the current speed, plus the body's own approach: a step no longer fires
                    //    the spring in one physics step, while ramps and landings are followed at once.
                    //  * The damper works on the body's own motion toward the ground (approach), so seams and steps add
                    //    no spike, plus the ground rise that lasts (low-pass filtered over 1.5 m of travel, capped at a
                    //    9-degree ramp): a ramp lifts the nose in time, a bumpy floor cannot pump the car into the air.
                    float approach = -Vector3.Dot(Body.GetPointVelocity(origin), up);
                    float trueComp = maxLen - dist;
                    float rise = Mathf.Clamp((trueComp - lastTrue[i]) / dt - approach, 0f, riseCap);
                    // filtered over distance, not time: a 5 m jump ramp is crossed in 0.15 s at top speed, which a
                    // time filter missed (weaker launches, nose into the ramp); below 4 m/s it still settles in ~0.4 s
                    riseLp[i] += (rise - riseLp[i]) * Mathf.Clamp01(Mathf.Max(along, 4f) * dt / RiseLength);
                    lastTrue[i] = trueComp;
                    float comp = Mathf.Min(trueComp, lastComp[i] + (ClimbRate + rampRise + Mathf.Max(0f, approach)) * dt);
                    float compVel = Mathf.Clamp(approach + riseLp[i], -MaxCompVel, MaxLandVel);
                    float force = Mathf.Clamp(k * comp + c * compVel, 0f, k * maxLen * 2.5f);   // 2.5: catches a 11 m/s landing like 4.0.1
                    Body.AddForceAtPosition(up * force, origin);
                    lastComp[i] = comp;
                    hitDist[i] = maxLen - comp;
                    wheelHit[i] = true;
                    normalSum += normal;
                    grounded++;
                }
                else
                {
                    lastComp[i] = Mathf.MoveTowards(lastComp[i], 0f, dt * ClimbRate);
                    lastTrue[i] = 0f;
                    riseLp[i] = 0f;
                    wheelHit[i] = false;
                    hitDist[i] = Mathf.MoveTowards(hitDist[i], maxLen, dt * 3f);
                }
            }

            Grounded = grounded >= 2;
            Vector3 v = Body.linearVelocity;
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;
            ForwardSpeed = Vector3.Dot(v, fwd);
            LateralSpeed = Vector3.Dot(v, right);

            bool wantNitro = !Locked && NitroHeld && Throttle > 0.1f;
            bool nitroNow = wantNitro && Nitro > 0.02f;
            if (nitroNow && !NitroActive && Spec != null && Spec.Role == DriverRole.Player) DerbyArena.Core.DailyMissions.Tick(DerbyArena.Core.MissionGoal.Nitro, 1);
            NitroActive = nitroNow;
            if (NitroActive) Nitro = Mathf.Max(0f, Nitro - dt * 0.3f * NitroDrainMul);
            else if (!wantNitro) Nitro = Mathf.Min(1f, Nitro + dt * 0.075f * NitroRegenMul);

            float top = Spec.TopSpeed * SpeedMul * (NitroActive ? 1.35f : 1f);
            float thr = Locked ? 0f : Mathf.Clamp01(Throttle);
            float brk = Locked ? 0f : Mathf.Clamp01(Brake);
            float steer = Locked ? 0f : Mathf.Clamp(Steer, -1f, 1f);
            bool hand = !Locked && Handbrake;

            if (Grounded)
            {
                Vector3 n = normalSum.normalized;
                Vector3 drive = Vector3.ProjectOnPlane(fwd, n).normalized;
                Vector3 side = Vector3.ProjectOnPlane(right, n).normalized;
                float a = 0f;

                if (thr > 0.01f)
                {
                    if (ForwardSpeed < -0.5f) a += Spec.BrakeDecel * thr;
                    else
                    {
                        float ratio = Mathf.Clamp01(ForwardSpeed / top);
                        a += Spec.Accel * SpeedMul * (NitroActive ? 1.65f : 1f) * thr * (1f - ratio * ratio * 0.95f);
                    }
                }
                if (brk > 0.01f)
                {
                    if (ForwardSpeed > 0.8f) a -= Spec.BrakeDecel * brk;
                    else
                    {
                        float rtop = top * 0.42f;
                        a -= Spec.Accel * 0.7f * brk * (1f - Mathf.Clamp01(-ForwardSpeed / rtop));
                    }
                }
                if (thr < 0.01f && brk < 0.01f)
                    a -= Mathf.Sign(ForwardSpeed) * Mathf.Min(Mathf.Abs(ForwardSpeed) / dt, 2.2f);
                if (hand)
                    a -= Mathf.Sign(ForwardSpeed) * Mathf.Min(Mathf.Abs(ForwardSpeed) / dt, 3.5f);
                Body.AddForce(drive * a, ForceMode.Acceleration);

                // 4.0.1 driving aid (player only, Settings → Controls): extra grip while sliding, a gentle counter-steer
                // toward the direction of travel and a slightly softer yaw response, so a swipe or tilt never spins the car.
                bool assist = Spec.Role == DriverRole.Player && DerbyArena.Core.Save.Data.SteerAssist;
                float grip = hand ? Spec.Grip * 0.2f : Spec.Grip;
                if (assist && !hand) grip *= 1f + 0.35f * Mathf.Clamp01(Mathf.Abs(LateralSpeed) / 6f);
                Body.AddForce(-side * LateralSpeed * grip, ForceMode.Acceleration);
                Drifting = (hand && Mathf.Abs(ForwardSpeed) > 7f) || Mathf.Abs(LateralSpeed) > 4.5f;

                float speedAbs = Mathf.Abs(ForwardSpeed);
                // 4.0.2: the car turns already at walking pace (it used to need 4.5 m/s, which made getting off a
                // wall or out of a corner hard) and keeps more bite at speed
                float steerFactor = Mathf.Clamp01(0.3f + speedAbs / 5f) * Mathf.Lerp(1f, 0.68f, Mathf.Clamp01(speedAbs / top));
                if (speedAbs < 0.6f && thr < 0.05f && brk < 0.05f) steerFactor = 0f;
                float dir = ForwardSpeed >= -0.4f ? 1f : -1f;
                float target = steer * Spec.TurnRate * steerFactor * dir * (hand ? 1.4f : 1f);
                if (assist && !hand && speedAbs > 3f)
                {
                    float slide = Mathf.Clamp(LateralSpeed / 8f, -1f, 1f);
                    target += slide * Spec.TurnRate * 0.45f * (1f - Mathf.Abs(steer) * 0.5f) * dir;
                }
                Vector3 av = Body.angularVelocity;
                float yaw = Vector3.Dot(av, n);
                float newYaw = Mathf.MoveTowards(yaw, target, Spec.TurnRate * (assist ? 5.5f : 7f) * dt);
                av += n * (newYaw - yaw);

                Vector3 local = transform.InverseTransformDirection(av);
                local.x *= 0.88f;
                local.z *= 0.82f;
                Body.angularVelocity = transform.TransformDirection(local);

                Body.AddForce(-n * Mathf.Min(speedAbs, 30f) * 0.28f, ForceMode.Acceleration);

                // a bump may lift the car, never launch it: while the wheels are down, the speed along the car's own
                // up axis is capped. Not world up (a car driving up a ramp moves along the ramp, its own up axis tilts
                // with it, so ramps and jumps keep their full speed) and not the contact normal (on a rough floor the
                // normals tilt while the car moves level, and capping along them pushed the car into the ground).
                // Wrecks (Locked) and explosion knock-ups (Launched) are left alone.
                if (grounded >= 3 && !Locked && launchTimer <= 0f)
                {
                    Vector3 lv = Body.linearVelocity;
                    float away = Vector3.Dot(lv, up);
                    if (away > MaxBumpRise) Body.linearVelocity = lv - up * (away - MaxBumpRise);
                }
            }
            else
            {
                Drifting = false;
                Body.AddForce(Physics.gravity * 0.6f, ForceMode.Acceleration);
                // level out in the air (roll and pitch), keep the yaw the player gave it
                Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
                Vector3 av = Body.angularVelocity;
                Vector3 yawPart = Vector3.Project(av, Vector3.up);
                Body.AddTorque(axis * 9f - (av - yawPart) * 2.2f - yawPart * 0.6f, ForceMode.Acceleration);
            }

            Vector3 vel = Body.linearVelocity;
            var planar = new Vector3(vel.x, 0f, vel.z);
            float cap = top * 1.05f;
            if (planar.sqrMagnitude > cap * cap)
            {
                planar = planar.normalized * cap;
                Body.linearVelocity = new Vector3(planar.x, vel.y, planar.z);
            }

            if (Vector3.Dot(transform.up, Vector3.up) < 0.35f) flipTimer += dt;
            else flipTimer = 0f;
            if (flipTimer > 1.3f) Recover();
        }

        void Update()
        {
            if (wheels == null || Spec == null) return;
            float dt = Time.deltaTime;
            spin += ForwardSpeed / Mathf.Max(0.1f, radius) * Mathf.Rad2Deg * dt;
            spin %= 360f;
            visualSteer = Mathf.Lerp(visualSteer, (Locked ? 0f : Steer) * 28f, 1f - Mathf.Exp(-12f * dt));
            for (int i = 0; i < 4; i++)
            {
                if (wheels[i] == null) continue;
                Vector3 p = anchors[i] + Vector3.down * (hitDist[i] - radius);
                wheels[i].localPosition = p;
                float yaw = i < 2 ? visualSteer : 0f;
                wheels[i].localRotation = Quaternion.Euler(spin, yaw, 0f) * wheelBase[i];
            }
        }

        /// <summary>World-space tyre contact point of wheel i (FL, FR, RL, RR).</summary>
        public Vector3 WheelContact(int i)
        {
            return transform.TransformPoint(anchors[i] + Vector3.down * hitDist[i]);
        }

        public bool WheelGrounded(int i)
        {
            return i >= 0 && i < 4 && wheelHit[i];
        }

        public float PlanarSpeed()
        {
            if (Body == null) return 0f;
            var v = Body.linearVelocity;
            v.y = 0f;
            return v.magnitude;
        }
    }
}
