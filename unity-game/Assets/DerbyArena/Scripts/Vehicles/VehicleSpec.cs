using UnityEngine;

namespace DerbyArena.Vehicles
{
    public enum DriverRole
    {
        Balanced,
        Flanker,
        Rammer,
        Sniper,
        Boss
    }

    public sealed class VehicleSpec
    {
        public string DisplayName;
        public string GlbFile;
        public DriverRole Role;
        public float Mass = 1500f;
        public float Motor = 2200f;
        public float MaxSteer = 28f;
        public float Health = 140f;
        public float WheelRadius = 0.42f;
        public float Scale = 1f;
        public Vector3[] WheelLocal;
        public bool Rockets;
        public int RocketAmmo = 0;
        public float GunDamage = 9f;
        public float GunCooldown = 0.14f;
        public float GunRange = 42f;
        public Color Accent = new Color(0.85f, 0.35f, 0.12f);

        public static VehicleSpec Interceptor()
        {
            return new VehicleSpec
            {
                DisplayName = "Breakline Interceptor",
                GlbFile = "player_muscle.glb",
                Role = DriverRole.Balanced,
                Mass = 1480f,
                Motor = 2600f,
                MaxSteer = 30f,
                Health = 160f,
                WheelRadius = 0.46f,
                Scale = 1f,
                GunDamage = 8f,
                GunCooldown = 0.11f,
                GunRange = 40f,
                RocketAmmo = 4,
                Rockets = true,
                Accent = new Color(0.92f, 0.45f, 0.12f),
                WheelLocal = WideWheels()
            };
        }

        public static VehicleSpec Drifter()
        {
            return new VehicleSpec
            {
                DisplayName = "Razor Drifter",
                GlbFile = "enemy_raider_a.glb",
                Role = DriverRole.Flanker,
                Mass = 1180f,
                Motor = 2400f,
                MaxSteer = 36f,
                Health = 90f,
                WheelRadius = 0.38f,
                Scale = 1f,
                GunDamage = 6f,
                GunCooldown = 0.09f,
                GunRange = 32f,
                Accent = new Color(0.95f, 0.2f, 0.25f),
                WheelLocal = NarrowWheels()
            };
        }

        public static VehicleSpec Rammer()
        {
            return new VehicleSpec
            {
                DisplayName = "Bastion Rammer",
                GlbFile = "enemy_raider_b.glb",
                Role = DriverRole.Rammer,
                Mass = 2400f,
                Motor = 3200f,
                MaxSteer = 22f,
                Health = 260f,
                WheelRadius = 0.4f,
                Scale = 1.08f,
                GunDamage = 5f,
                GunCooldown = 0.22f,
                GunRange = 24f,
                Accent = new Color(0.45f, 0.5f, 0.55f),
                WheelLocal = NarrowWheels()
            };
        }

        public static VehicleSpec Hunter()
        {
            return new VehicleSpec
            {
                DisplayName = "Siege Hunter",
                GlbFile = "enemy_raider_c.glb",
                Role = DriverRole.Sniper,
                Mass = 1600f,
                Motor = 2000f,
                MaxSteer = 24f,
                Health = 120f,
                WheelRadius = 0.38f,
                Scale = 1f,
                GunDamage = 7f,
                GunCooldown = 0.16f,
                GunRange = 36f,
                Rockets = true,
                RocketAmmo = 10,
                Accent = new Color(0.25f, 0.7f, 0.55f),
                WheelLocal = NarrowWheels()
            };
        }

        public static VehicleSpec WarRig()
        {
            return new VehicleSpec
            {
                DisplayName = "War Rig",
                GlbFile = "enemy_boss_warrig.glb",
                Role = DriverRole.Boss,
                Mass = 4200f,
                Motor = 5200f,
                MaxSteer = 18f,
                Health = 520f,
                WheelRadius = 0.5f,
                Scale = 1.22f,
                GunDamage = 11f,
                GunCooldown = 0.12f,
                GunRange = 48f,
                Rockets = true,
                RocketAmmo = 18,
                Accent = new Color(0.75f, 0.12f, 0.1f),
                WheelLocal = WideWheels()
            };
        }

        static Vector3[] WideWheels()
        {
            return new[]
            {
                new Vector3(-1.20f, 0.52f, 1.77f),
                new Vector3(1.20f, 0.52f, 1.77f),
                new Vector3(-1.20f, 0.52f, -1.77f),
                new Vector3(1.20f, 0.52f, -1.77f)
            };
        }

        static Vector3[] NarrowWheels()
        {
            return new[]
            {
                new Vector3(-0.98f, 0.43f, 1.45f),
                new Vector3(0.98f, 0.43f, 1.45f),
                new Vector3(-0.98f, 0.43f, -1.45f),
                new Vector3(0.98f, 0.43f, -1.45f)
            };
        }
    }
}
