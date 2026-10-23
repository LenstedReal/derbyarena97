using DerbyArena.Combat;
using DerbyArena.Mission;
using DerbyArena.Vehicles;
using UnityEngine;

namespace DerbyArena.AI
{
    public sealed class EnemyDriver : MonoBehaviour
    {
        public WheelDrive Drive;
        public WeaponRig Weapons;
        public VehicleHealth Health;
        public Transform Player;
        public MissionDirector Mission;

        float think;
        float shock;
        int phase = 1;
        Vector3 wander;

        void Update()
        {
            if (Drive == null || Health == null || Health.Dead || Player == null)
            {
                if (Drive != null)
                {
                    Drive.Throttle = 0f;
                    Drive.Brake = 1f;
                }
                return;
            }
            think -= Time.deltaTime;
            if (think <= 0f)
            {
                think = 0.12f;
                Decide();
            }
            AimAndFire();
        }

        void Decide()
        {
            Vector3 to = Player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            Vector3 desired = Player.position;
            bool rocket = false;
            bool gun = false;
            switch (Drive.Spec.Role)
            {
                case DriverRole.Flanker:
                    desired = Player.position + Vector3.Cross(Vector3.up, to.normalized) * 9f;
                    gun = dist < 30f && dist > 8f;
                    break;
                case DriverRole.Rammer:
                    var body = Player.GetComponent<Rigidbody>();
                    Vector3 lead = body != null ? body.linearVelocity * 0.35f : Vector3.zero;
                    desired = Player.position + lead;
                    gun = dist < 16f;
                    break;
                case DriverRole.Sniper:
                    if (dist < 20f)
                        desired = transform.position - to.normalized * 8f;
                    else if (dist > 38f)
                        desired = Player.position;
                    else
                        desired = transform.position;
                    rocket = dist < 42f && dist > 14f;
                    gun = dist < 34f;
                    break;
                case DriverRole.Boss:
                    UpdateBoss(dist, to, ref desired, ref gun, ref rocket);
                    break;
                default:
                    desired = Player.position;
                    gun = dist < 28f;
                    break;
            }
            if (Physics.Raycast(transform.position + Vector3.up, transform.forward, 6f))
                desired += transform.right * 6f;
            SteerToward(desired, dist);
            if (Weapons != null)
                Weapons.TickFire(gun, rocket, Player.position + Vector3.up * 0.8f);
        }

        void UpdateBoss(float dist, Vector3 to, ref Vector3 desired, ref bool gun, ref bool rocket)
        {
            float hp = Health.Current / Mathf.Max(1f, Health.Max);
            phase = hp > 0.66f ? 1 : hp > 0.33f ? 2 : 3;
            if (phase == 1)
            {
                desired = Player.position;
                gun = dist < 40f;
                rocket = dist > 16f && dist < 46f;
            }
            else if (phase == 2)
            {
                desired = Player.position + Vector3.Cross(Vector3.up, to.normalized) * 6f;
                gun = true;
                rocket = dist < 40f;
                shock -= 0.12f;
                if (shock <= 0f)
                {
                    shock = 7.5f;
                    StartCoroutine(Shockwave());
                }
            }
            else
            {
                Vector3 gate = Mission != null ? Mission.ExtractPoint : Player.position;
                desired = Vector3.Lerp(Player.position, gate, 0.55f);
                gun = dist < 36f;
                rocket = true;
            }
        }

        System.Collections.IEnumerator Shockwave()
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShockTelegraph";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.position = transform.position + Vector3.up * 0.2f;
            ring.transform.localScale = new Vector3(2f, 0.05f, 2f);
            ring.GetComponent<Renderer>().sharedMaterial = DerbyArena.World.SpillwayBuilder.Lit(new Color(1f, 0.2f, 0.1f), 0.4f, 0f, new Color(1f, 0.2f, 0.05f), 1.5f);
            float t = 0f;
            while (t < 1.15f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(2f, 14f, t / 1.15f);
                ring.transform.localScale = new Vector3(s, 0.05f, s);
                ring.transform.position = transform.position + Vector3.up * 0.2f;
                yield return null;
            }
            var hits = Physics.OverlapSphere(transform.position, 7.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                var hp = hits[i].GetComponentInParent<VehicleHealth>();
                if (hp != null && hp != Health)
                    hp.Apply(22f, this);
            }
            Destroy(ring);
        }

        void SteerToward(Vector3 worldPoint, float dist)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            float steer = Mathf.Clamp(local.x / 6f, -1f, 1f);
            float throttle = local.z > 1f ? 1f : 0.25f;
            if (Drive.Spec.Role == DriverRole.Flanker && dist < 7f)
                throttle = 0.7f;
            Drive.Steer = steer;
            Drive.Throttle = throttle;
            Drive.Brake = local.z < -2f ? 0.6f : 0f;
            Drive.NitroHeld = dist > 22f && Drive.Spec.Role != DriverRole.Sniper;
            Drive.Handbrake = false;
        }

        void AimAndFire()
        {
        }
    }
}
