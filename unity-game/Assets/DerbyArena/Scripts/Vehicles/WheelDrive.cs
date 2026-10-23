using UnityEngine;

namespace DerbyArena.Vehicles
{
    public sealed class WheelDrive : MonoBehaviour
    {
        public float Throttle;
        public float Steer;
        public float Brake;
        public bool Handbrake;
        public bool NitroHeld;
        public float Nitro = 1f;
        public bool NitroActive { get; private set; }
        public VehicleSpec Spec { get; private set; }

        Rigidbody body;
        WheelCollider[] wheels;
        Transform[] visuals;
        bool ready;

        public void Configure(VehicleSpec spec)
        {
            Spec = spec;
            body = GetComponent<Rigidbody>();
            body.mass = spec.Mass;
            body.linearDamping = 0.08f;
            body.angularDamping = 1.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, -0.35f, 0.05f);
            PlaceColliders(spec.WheelLocal, spec.WheelRadius);
        }

        public void PlaceColliders(Vector3[] localPoints, float radius)
        {
            if (wheels != null)
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    if (wheels[i] != null)
                        Destroy(wheels[i].gameObject);
                }
            }
            wheels = new WheelCollider[localPoints.Length];
            for (int i = 0; i < localPoints.Length; i++)
            {
                var go = new GameObject("WheelCol" + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = localPoints[i];
                var wc = go.AddComponent<WheelCollider>();
                wc.radius = radius;
                wc.suspensionDistance = 0.24f;
                var spring = wc.suspensionSpring;
                spring.spring = Spec.Mass * 18f;
                spring.damper = Spec.Mass * 2.2f;
                spring.targetPosition = 0.45f;
                wc.suspensionSpring = spring;
                wc.mass = 28f;
                var fwd = wc.forwardFriction;
                fwd.stiffness = Spec.Role == DriverRole.Flanker ? 1.15f : 1.55f;
                wc.forwardFriction = fwd;
                var side = wc.sidewaysFriction;
                side.stiffness = Spec.Role == DriverRole.Flanker ? 1.0f : 1.65f;
                wc.sidewaysFriction = side;
                wheels[i] = wc;
            }
            ready = true;
        }

        public void BindWheelVisuals(Transform[] wheelVisuals)
        {
            visuals = wheelVisuals;
        }

        void FixedUpdate()
        {
            if (!ready || wheels == null)
                return;
            NitroActive = NitroHeld && Nitro > 0.05f && Throttle > 0.2f;
            if (NitroActive)
                Nitro = Mathf.Max(0f, Nitro - Time.fixedDeltaTime * 0.28f);
            else
                Nitro = Mathf.Min(1f, Nitro + Time.fixedDeltaTime * 0.06f);

            float frontZ = float.MinValue;
            for (int i = 0; i < wheels.Length; i++)
                frontZ = Mathf.Max(frontZ, wheels[i].transform.localPosition.z);

            float motorMul = NitroActive ? 1.7f : 1f;
            float torque = Throttle * Spec.Motor * motorMul;
            float brakeTorque = Brake * Spec.Mass * 3.2f;
            for (int i = 0; i < wheels.Length; i++)
            {
                var wc = wheels[i];
                bool front = wc.transform.localPosition.z > frontZ - 0.4f;
                wc.steerAngle = front ? Steer * Spec.MaxSteer : 0f;
                wc.motorTorque = torque / wheels.Length;
                float hand = (!front && Handbrake) ? Spec.Mass * 4.5f : 0f;
                wc.brakeTorque = brakeTorque + hand;
                if (Handbrake && !front)
                {
                    var side = wc.sidewaysFriction;
                    side.stiffness = 0.65f;
                    wc.sidewaysFriction = side;
                }
            }
            if (NitroActive)
                body.AddForce(transform.forward * Spec.Mass * 6f, ForceMode.Force);
        }

        void Update()
        {
            if (wheels == null || visuals == null)
                return;
            int n = Mathf.Min(wheels.Length, visuals.Length);
            for (int i = 0; i < n; i++)
            {
                if (visuals[i] == null)
                    continue;
                wheels[i].GetWorldPose(out var pos, out var rot);
                visuals[i].SetPositionAndRotation(pos, rot);
            }
        }

        public float PlanarSpeed()
        {
            if (body == null)
                return 0f;
            var v = body.linearVelocity;
            v.y = 0f;
            return v.magnitude;
        }
    }
}
