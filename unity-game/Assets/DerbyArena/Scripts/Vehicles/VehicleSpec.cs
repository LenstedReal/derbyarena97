using UnityEngine;

namespace DerbyArena.Vehicles
{
    public enum DriverRole { Player, Flanker, Rammer, Sniper, Boss }

    /// <summary>Tuning data for one vehicle type. Distances in metres, speeds in m/s.</summary>
    public sealed class VehicleSpec
    {
        public string DisplayName;
        public string GlbFile;
        public DriverRole Role;
        public float Mass = 1400f;
        public float TopSpeed = 27f;
        public float Accel = 13f;
        public float BrakeDecel = 22f;
        public float TurnRate = 2.1f;          // rad/s at medium speed
        public float Grip = 9f;                // lateral velocity removed per second
        public float Health = 160f;
        public float Scale = 1f;
        public float WheelRadius = 0.52f;
        public Vector3 WheelFront = new Vector3(1.1956f, 0.5246f, 1.769f);   // +x right, +z front (FR)
        public Vector3 WheelRear = new Vector3(1.1956f, 0.5246f, -1.769f);
        public Vector3 BodySize = new Vector3(2.6f, 0.95f, 5.6f);
        public float BodyCenterY = 0.95f;
        public float MuzzleHeight = 1.85f;
        public float MuzzleForward = 1.2f;
        public float GunDamage = 7f;
        public float GunInterval = 0.1f;
        public float GunRange = 42f;
        public bool Rockets = true;
        public int RocketAmmo = 4;
        public float RamMul = 1f;
        public int ScrapValue = 30;
        public Color Accent = new Color(1f, 0.55f, 0.12f);

        public static VehicleSpec Interceptor()
        {
            return new VehicleSpec
            {
                DisplayName = "Breakline Interceptor", GlbFile = "vehicles/player_muscle.glb", Role = DriverRole.Player,
                Mass = 1450f, TopSpeed = 27f, Accel = 13.5f, TurnRate = 2.2f, Grip = 9.5f, Health = 170f,
                GunDamage = 7.5f, GunInterval = 0.09f, GunRange = 44f, RocketAmmo = 4
            };
        }

        static VehicleSpec Raider(string name, string glb)
        {
            return new VehicleSpec
            {
                DisplayName = name, GlbFile = glb, WheelRadius = 0.43f,
                WheelFront = new Vector3(0.98f, 0.43f, 1.45f), WheelRear = new Vector3(0.98f, 0.43f, -1.45f),
                BodySize = new Vector3(2.15f, 0.85f, 4.7f), BodyCenterY = 0.82f, MuzzleHeight = 1.55f, MuzzleForward = 1f
            };
        }

        public static VehicleSpec Drifter()
        {
            var s = Raider("Razor Drifter", "vehicles/enemy_raider_a.glb");
            s.Role = DriverRole.Flanker; s.Mass = 1150f; s.TopSpeed = 25f; s.Accel = 12.5f; s.TurnRate = 2.4f; s.Grip = 8f;
            s.Health = 85f; s.GunDamage = 4.5f; s.GunInterval = 0.13f; s.GunRange = 32f; s.Rockets = false; s.RocketAmmo = 0;
            s.ScrapValue = 30; s.Accent = new Color(0.95f, 0.2f, 0.25f);
            return s;
        }

        public static VehicleSpec Rammer()
        {
            var s = Raider("Bastion Rammer", "vehicles/enemy_raider_b.glb");
            s.Role = DriverRole.Rammer; s.Mass = 2100f; s.TopSpeed = 24f; s.Accel = 12f; s.TurnRate = 1.8f; s.Grip = 10f;
            s.Health = 150f; s.GunDamage = 3.5f; s.GunInterval = 0.2f; s.GunRange = 22f; s.Rockets = false; s.RocketAmmo = 0;
            s.RamMul = 1.5f; s.Scale = 1.08f; s.ScrapValue = 40; s.Accent = new Color(0.55f, 0.6f, 0.65f);
            return s;
        }

        public static VehicleSpec Hunter()
        {
            var s = Raider("Siege Hunter", "vehicles/enemy_raider_c.glb");
            s.Role = DriverRole.Sniper; s.Mass = 1500f; s.TopSpeed = 22f; s.Accel = 11f; s.TurnRate = 1.9f; s.Grip = 9f;
            s.Health = 110f; s.GunDamage = 5f; s.GunInterval = 0.16f; s.GunRange = 38f; s.Rockets = true; s.RocketAmmo = 99;
            s.ScrapValue = 45; s.Accent = new Color(0.25f, 0.75f, 0.55f);
            return s;
        }

        public static VehicleSpec WarRig()
        {
            return new VehicleSpec
            {
                DisplayName = "War Rig", GlbFile = "vehicles/enemy_boss_warrig.glb", Role = DriverRole.Boss,
                Mass = 4200f, TopSpeed = 21f, Accel = 10f, TurnRate = 1.5f, Grip = 11f, Health = 620f, Scale = 1.25f,
                GunDamage = 6f, GunInterval = 0.12f, GunRange = 46f, Rockets = true, RocketAmmo = 999, RamMul = 1.6f,
                ScrapValue = 200, Accent = new Color(0.85f, 0.12f, 0.1f)
            };
        }
    }
}
