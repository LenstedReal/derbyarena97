using DerbyArena.Audio;
using DerbyArena.Vehicles;
using UnityEngine;

namespace DerbyArena.Combat
{
    public sealed class WeaponRig : MonoBehaviour
    {
        public Transform Muzzle;
        public VehicleSpec Spec;
        public int Rockets;
        float gunCd;
        float rocketCd;
        VehicleHealth owner;

        public void Configure(VehicleSpec spec, VehicleHealth health)
        {
            Spec = spec;
            owner = health;
            Rockets = spec.RocketAmmo;
        }

        public void AddRockets(int amount)
        {
            Rockets += amount;
        }

        public void TickFire(bool fireGun, bool fireRocket, Vector3 aimPoint)
        {
            gunCd -= Time.deltaTime;
            rocketCd -= Time.deltaTime;
            if (owner != null && owner.Dead)
                return;
            Vector3 origin = Muzzle != null ? Muzzle.position : transform.position + Vector3.up;
            Vector3 dir = aimPoint - origin;
            if (dir.sqrMagnitude < 0.01f)
                dir = transform.forward;
            dir.y *= 0.25f;
            dir.Normalize();
            if (fireGun && gunCd <= 0f)
            {
                gunCd = Spec.GunCooldown;
                FireGun(origin, dir);
            }
            if (fireRocket && Spec.Rockets && Rockets > 0 && rocketCd <= 0f)
            {
                rocketCd = 0.85f;
                Rockets--;
                FireRocket(origin, dir);
            }
        }

        void FireGun(Vector3 origin, Vector3 dir)
        {
            AudioSource.PlayClipAtPoint(SynthAudio.Shot(), origin, 0.55f);
            if (Physics.Raycast(origin, dir, out var hit, Spec.GunRange))
            {
                var hp = hit.collider.GetComponentInParent<VehicleHealth>();
                if (hp != null && hp != owner)
                    hp.Apply(Spec.GunDamage, this);
                SpawnSpark(hit.point);
            }
            SpawnTracer(origin, origin + dir * Mathf.Min(Spec.GunRange, 28f));
        }

        void FireRocket(Vector3 origin, Vector3 dir)
        {
            var go = ProjectilePool.Spawn(origin, dir, 36f, 26f, 3.6f, owner);
            if (go == null)
                return;
            AudioSource.PlayClipAtPoint(SynthAudio.Boom(), origin, 0.4f);
        }

        static void SpawnTracer(Vector3 a, Vector3 b)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = 0.05f;
            lr.endWidth = 0.01f;
            lr.material = DerbyArena.World.SpillwayBuilder.Lit(new Color(1f, 0.75f, 0.3f), 0.2f, 0f, new Color(1f, 0.6f, 0.2f), 2.5f);
            UnityEngine.Object.Destroy(go, 0.06f);
        }

        static void SpawnSpark(Vector3 p)
        {
            var go = new GameObject("Spark");
            go.transform.position = p;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.5f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.7f, 0.35f);
            light.shadows = LightShadows.None;
            UnityEngine.Object.Destroy(go, 0.05f);
        }
    }

    public sealed class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }
        const int Size = 10;
        Rocket[] items;

        public static Rocket Spawn(Vector3 pos, Vector3 dir, float speed, float damage, float radius, VehicleHealth owner)
        {
            if (Instance == null)
            {
                var go = new GameObject("ProjectilePool");
                Instance = go.AddComponent<ProjectilePool>();
                Instance.Build();
            }
            for (int i = 0; i < Instance.items.Length; i++)
            {
                if (!Instance.items[i].Active)
                {
                    Instance.items[i].Launch(pos, dir, speed, damage, radius, owner);
                    return Instance.items[i];
                }
            }
            Instance.items[0].Launch(pos, dir, speed, damage, radius, owner);
            return Instance.items[0];
        }

        void Awake()
        {
            Instance = this;
            if (items == null)
                Build();
        }

        void Build()
        {
            items = new Rocket[Size];
            for (int i = 0; i < Size; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Rocket" + i;
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);
                Destroy(go.GetComponent<Collider>());
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.4f;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                go.GetComponent<Renderer>().sharedMaterial = DerbyArena.World.SpillwayBuilder.Lit(new Color(0.3f, 0.3f, 0.3f), 0.4f, 0.6f, new Color(1f, 0.4f, 0.1f), 1.5f);
                var rocket = go.AddComponent<Rocket>();
                rocket.gameObject.SetActive(false);
                items[i] = rocket;
            }
        }
    }

    public sealed class Rocket : MonoBehaviour
    {
        public bool Active;
        Vector3 velocity;
        float damage;
        float radius;
        float life;
        VehicleHealth owner;

        public void Launch(Vector3 pos, Vector3 dir, float speed, float dmg, float rad, VehicleHealth source)
        {
            owner = source;
            damage = dmg;
            radius = rad;
            life = 2.2f;
            velocity = dir.normalized * speed;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(dir);
            Active = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!Active)
                return;
            float dt = Time.deltaTime;
            var next = transform.position + velocity * dt;
            if (Physics.SphereCast(transform.position, 0.2f, velocity.normalized, out var hit, velocity.magnitude * dt))
            {
                Explode(hit.point);
                return;
            }
            transform.position = next;
            life -= dt;
            if (life <= 0f)
                Explode(transform.position);
        }

        void Explode(Vector3 point)
        {
            Active = false;
            gameObject.SetActive(false);
            AudioSource.PlayClipAtPoint(SynthAudio.Boom(), point, 0.8f);
            var hits = Physics.OverlapSphere(point, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                var hp = hits[i].GetComponentInParent<VehicleHealth>();
                if (hp == null || hp == owner)
                    continue;
                float dist = Vector3.Distance(point, hp.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / radius);
                hp.Apply(damage * Mathf.Max(0.35f, falloff), this);
            }
            var lightGo = new GameObject("Blast");
            lightGo.transform.position = point;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.15f);
            light.intensity = 4f;
            light.range = radius * 2f;
            light.shadows = LightShadows.None;
            Destroy(lightGo, 0.08f);
        }
    }
}
