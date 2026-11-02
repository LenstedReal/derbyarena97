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
        public float TurnRate = 2.1f;
        public float Grip = 9f;
        public float Health = 160f;
        public float Scale = 1f;
        public float WheelRadius = 0.52f;
        public Vector3 WheelFront = new Vector3(1.1956f, 0.5246f, 1.769f);
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
        public float RocketDamage = 45f;
        public float RamMul = 1f;
        public int ScrapValue = 30;
        public bool Elite;
        public Color Accent = new Color(1f, 0.55f, 0.12f);
        public Color Paint = new Color(0.78f, 0.36f, 0.12f);

        /// <summary>GLB import options: per-type paint so every vehicle reads clearly against the arena.</summary>
        public DerbyArena.World.GlbOptions ModelOptions()
        {
            var o = new DerbyArena.World.GlbOptions();
            o.Recolor = true;
            o.Paint = Paint;
            o.EmissionBoost = Role == DriverRole.Player ? 2.2f : 1.6f;
            o.Key = "paint_" + ColorUtility.ToHtmlStringRGB(Paint);
            return o;
        }

        public static VehicleSpec Interceptor()
        {
            return new VehicleSpec
            {
                DisplayName = "Breakline Interceptor", GlbFile = "vehicles/player_muscle.glb", Role = DriverRole.Player,
                Mass = 1450f, TopSpeed = 27f, Accel = 13.5f, TurnRate = 2.2f, Grip = 9.5f, Health = 170f,
                GunDamage = 7.5f, GunInterval = 0.09f, GunRange = 44f, RocketAmmo = 4, RocketDamage = 48f,
                Paint = new Color(0.8f, 0.38f, 0.12f), Accent = new Color(1f, 0.55f, 0.12f)
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

        // Enemy weapons are tuned for fairness: lower per-shot damage and slower cadence than the player,
        // combined with burst/pause rhythm and a limited number of simultaneous shooters (MatchDirector).
        public static VehicleSpec Drifter()
        {
            var s = Raider("Razor Drifter", "vehicles/enemy_raider_a.glb");
            s.Role = DriverRole.Flanker; s.Mass = 1150f; s.TopSpeed = 25f; s.Accel = 12.5f; s.TurnRate = 2.4f; s.Grip = 8f;
            s.Health = 85f; s.GunDamage = 2.6f; s.GunInterval = 0.2f; s.GunRange = 32f; s.Rockets = false; s.RocketAmmo = 0;
            s.ScrapValue = 30; s.Accent = new Color(0.95f, 0.2f, 0.25f); s.Paint = new Color(0.7f, 0.12f, 0.14f);
            return s;
        }

        public static VehicleSpec Rammer()
        {
            var s = Raider("Bastion Rammer", "vehicles/enemy_raider_b.glb");
            s.Role = DriverRole.Rammer; s.Mass = 2100f; s.TopSpeed = 24f; s.Accel = 12f; s.TurnRate = 1.8f; s.Grip = 10f;
            s.Health = 150f; s.GunDamage = 2.2f; s.GunInterval = 0.28f; s.GunRange = 22f; s.Rockets = false; s.RocketAmmo = 0;
            s.RamMul = 1.3f; s.Scale = 1.08f; s.ScrapValue = 40; s.Accent = new Color(0.55f, 0.6f, 0.65f); s.Paint = new Color(0.38f, 0.44f, 0.52f);
            return s;
        }

        public static VehicleSpec Hunter()
        {
            var s = Raider("Siege Hunter", "vehicles/enemy_raider_c.glb");
            s.Role = DriverRole.Sniper; s.Mass = 1500f; s.TopSpeed = 22f; s.Accel = 11f; s.TurnRate = 1.9f; s.Grip = 9f;
            s.Health = 110f; s.GunDamage = 3f; s.GunInterval = 0.24f; s.GunRange = 38f; s.Rockets = true; s.RocketAmmo = 99;
            s.RocketDamage = 18f; s.ScrapValue = 45; s.Accent = new Color(0.25f, 0.75f, 0.55f); s.Paint = new Color(0.32f, 0.46f, 0.24f);
            return s;
        }

        public static VehicleSpec WarRig()
        {
            return new VehicleSpec
            {
                DisplayName = "War Rig", GlbFile = "vehicles/enemy_boss_warrig.glb", Role = DriverRole.Boss,
                Mass = 4200f, TopSpeed = 20f, Accel = 9.5f, TurnRate = 1.45f, Grip = 11f, Health = 600f, Scale = 1.25f,
                GunDamage = 2.6f, GunInterval = 0.16f, GunRange = 40f, Rockets = true, RocketAmmo = 999, RocketDamage = 16f, RamMul = 1.3f,
                ScrapValue = 200, Accent = new Color(0.85f, 0.12f, 0.1f), Paint = new Color(0.34f, 0.06f, 0.05f)
            };
        }

        /// <summary>Chapter 4-5 elite variant: tougher, more accurate, violet-black paint.</summary>
        public void MakeElite()
        {
            Elite = true;
            Health *= 1.35f;
            GunDamage *= 1.15f;
            TopSpeed *= 1.05f;
            ScrapValue = Mathf.RoundToInt(ScrapValue * 1.6f);
            DisplayName = DerbyArena.Core.Loc.T("Elit ", "Elite ") + DisplayName;
            Paint = new Color(0.3f, 0.1f, 0.42f);
            Accent = new Color(0.7f, 0.3f, 1f);
        }
    }
}
