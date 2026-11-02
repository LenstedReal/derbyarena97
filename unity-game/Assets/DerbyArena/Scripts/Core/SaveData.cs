using System;
using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>Persistent player data (progress, currency, upgrades, settings). JSON in PlayerPrefs.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 3;
        public int Scrap = 0;
        public int Unlocked = 1;
        public int LastPlayed = 1;
        public int TotalKills = 0;
        public int[] Stars = new int[Campaign.Total + 1];
        public int[] Upgrades = new int[5];   // engine, armor, weapon, nitro, rockets

        public int ControlMode = 0;           // 0 steering wheel, 1 arrow buttons
        public float Sensitivity = 1f;
        public bool AutoGas = false;
        public bool AutoAim = true;
        public bool Sound = true;
        public bool Haptics = true;
        public int Quality = 1;               // 0 low, 1 medium, 2 high
        public bool QualityChosen = false;    // false -> auto-detect once on first launch
        public float Brightness = 1.1f;       // 0.8 .. 1.4, scales key light, ambient and sky
        public int[] Fails = new int[Campaign.Total + 1];   // losses per level -> automatic assist
    }

    public static class Save
    {
        const string Key = "derby.save.v3";
        public static SaveData Data = new SaveData();
        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var d = JsonUtility.FromJson<SaveData>(json);
                    if (d != null) Data = d;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: save unreadable, starting fresh. " + ex.Message);
                Data = new SaveData();
            }
            Sanitize();
        }

        public static void Write()
        {
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: save could not be written. " + ex.Message);
            }
        }

        static void Sanitize()
        {
            var d = Data;
            if (d.Stars == null || d.Stars.Length != Campaign.Total + 1)
            {
                var s = new int[Campaign.Total + 1];
                if (d.Stars != null) Array.Copy(d.Stars, s, Mathf.Min(d.Stars.Length, s.Length));
                d.Stars = s;
            }
            if (d.Fails == null || d.Fails.Length != Campaign.Total + 1)
            {
                var f = new int[Campaign.Total + 1];
                if (d.Fails != null) Array.Copy(d.Fails, f, Mathf.Min(d.Fails.Length, f.Length));
                d.Fails = f;
            }
            if (d.Brightness < 0.6f || d.Brightness > 1.6f) d.Brightness = 1.1f;
            if (d.Upgrades == null || d.Upgrades.Length != 5)
            {
                var u = new int[5];
                if (d.Upgrades != null) Array.Copy(d.Upgrades, u, Mathf.Min(d.Upgrades.Length, u.Length));
                d.Upgrades = u;
            }
            for (int i = 0; i < d.Upgrades.Length; i++) d.Upgrades[i] = Mathf.Clamp(d.Upgrades[i], 0, Upgrades.MaxLevel);
            for (int i = 0; i < d.Stars.Length; i++) d.Stars[i] = Mathf.Clamp(d.Stars[i], 0, 3);
            d.Unlocked = Mathf.Clamp(d.Unlocked, 1, Campaign.Total);
            d.LastPlayed = Mathf.Clamp(d.LastPlayed, 1, d.Unlocked);
            d.Scrap = Mathf.Max(0, d.Scrap);
            d.ControlMode = Mathf.Clamp(d.ControlMode, 0, 1);
            d.Sensitivity = Mathf.Clamp(d.Sensitivity, 0.5f, 1.8f);
            d.Quality = Mathf.Clamp(d.Quality, 0, 2);
        }

        /// <summary>First launch: pick a quality tier from the device (memory, shader level, HDR support).</summary>
        public static void AutoQuality()
        {
            if (Data.QualityChosen) return;
            int mem = SystemInfo.systemMemorySize;
            bool hdr = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR);
            if (mem >= 7000 && SystemInfo.graphicsShaderLevel >= 45 && hdr) Data.Quality = 2;
            else if (mem >= 3500) Data.Quality = 1;
            else Data.Quality = 0;
            Data.QualityChosen = true;
            Write();
        }

        public static int TotalStars()
        {
            int s = 0;
            for (int i = 1; i < Data.Stars.Length; i++) s += Data.Stars[i];
            return s;
        }
    }

    public static class Upgrades
    {
        public const int MaxLevel = 5;
        public const int Engine = 0, Armor = 1, Weapon = 2, Nitro = 3, Rockets = 4;

        public static readonly string[] Names = { "MOTOR", "ZIRH", "SİLAH", "NİTRO", "ROKET" };
        public static readonly string[] Descriptions =
        {
            "Hız ve ivme +%5",
            "Azami can +24",
            "Makineli hasarı +%12",
            "Nitro süresi ve dolum hızı",
            "Başlangıç roketi +2"
        };

        public static int Cost(int type, int level) { return 120 + level * 140 + type * 10; }
        public static int Level(int type) { return Save.Data.Upgrades[type]; }

        public static bool TryBuy(int type)
        {
            int lvl = Save.Data.Upgrades[type];
            if (lvl >= MaxLevel) return false;
            int cost = Cost(type, lvl);
            if (Save.Data.Scrap < cost) return false;
            Save.Data.Scrap -= cost;
            Save.Data.Upgrades[type] = lvl + 1;
            Save.Write();
            return true;
        }

        public static float SpeedMul() { return 1f + 0.05f * Level(Engine); }
        public static float HealthBonus() { return 24f * Level(Armor); }
        public static float DamageMul() { return 1f + 0.12f * Level(Weapon); }
        public static float NitroDrainMul() { return 1f - 0.09f * Level(Nitro); }
        public static float NitroRegenMul() { return 1f + 0.2f * Level(Nitro); }
        public static int ExtraRockets() { return 2 * Level(Rockets); }
    }
}
