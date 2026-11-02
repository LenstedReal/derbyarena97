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
        bool[] wheelHit = new bool[4];
        float radius;
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
        }

        void Recover()
        {
            float yaw = transform.eulerAngles.y;
            Teleport(transform.position + Vector3.up * 1.4f, yaw);
        }

        void FixedUpdate()
        {
            if (Spec == null || Body == null) return;
            float dt = Time.fixedDeltaTime;
            Vector3 up = transform.up;
            float maxLen = Rest + radius;
            float k = Body.mass * -Physics.gravity.y / 4f / RestCompression;
            float c = 2f * Mathf.Sqrt(k * Body.mass / 4f) * 0.75f;
            int grounded = 0;
            Vector3 normalSum = Vector3.zero;

            for (int i = 0; i < 4; i++)
            {
                Vector3 origin = transform.TransformPoint(anchors[i]);
                RaycastHit hit;
                if (Physics.Raycast(origin, -up, out hit, maxLen, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody != Body)
                {
                    float comp = maxLen - hit.distance;
                    float compVel = (comp - lastComp[i]) / dt;
                    float force = Mathf.Clamp(k * comp + c * compVel, 0f, k * maxLen * 2.5f);
                    Body.AddForceAtPosition(up * force, origin);
                    lastComp[i] = comp;
                    hitDist[i] = hit.distance;
                    wheelHit[i] = true;
                    normalSum += hit.normal;
                    grounded++;
                }
                else
                {
                    lastComp[i] = 0f;
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
            NitroActive = wantNitro && Nitro > 0.02f;
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

                float grip = hand ? Spec.Grip * 0.2f : Spec.Grip;
                Body.AddForce(-side * LateralSpeed * grip, ForceMode.Acceleration);
                Drifting = (hand && Mathf.Abs(ForwardSpeed) > 7f) || Mathf.Abs(LateralSpeed) > 4.5f;

                float speedAbs = Mathf.Abs(ForwardSpeed);
                float steerFactor = Mathf.Clamp01(speedAbs / 4.5f) * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(speedAbs / top));
                float dir = ForwardSpeed >= -0.4f ? 1f : -1f;
                float target = steer * Spec.TurnRate * steerFactor * dir * (hand ? 1.4f : 1f);
                Vector3 av = Body.angularVelocity;
                float yaw = Vector3.Dot(av, n);
                float newYaw = Mathf.MoveTowards(yaw, target, Spec.TurnRate * 7f * dt);
                av += n * (newYaw - yaw);

                Vector3 local = transform.InverseTransformDirection(av);
                local.x *= 0.88f;
                local.z *= 0.82f;
                Body.angularVelocity = transform.TransformDirection(local);

                Body.AddForce(-n * Mathf.Min(speedAbs, 30f) * 0.4f, ForceMode.Acceleration);
            }
            else
            {
                Drifting = false;
                Body.AddForce(Physics.gravity * 0.55f, ForceMode.Acceleration);
                Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
                Body.AddTorque(axis * 7f - Body.angularVelocity * 1.5f, ForceMode.Acceleration);
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
