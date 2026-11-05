using System;
using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>Persistent player data (progress, currency, upgrades, settings). JSON in PlayerPrefs.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 7;             // 5 = 4.0 (20 levels per season), 6 = 4.0.1 (controls), 7 = 4.0.2 (fresh start)
        public int Scrap = 0;
        public int Unlocked = 1;
        public int LastPlayed = 1;
        public int TotalKills = 0;
        public int[] Stars = new int[Campaign.Total + 1];
        public int[] Upgrades = new int[5];   // engine, armor, weapon, nitro, rockets

        public int ControlMode = 2;           // SteerMode: 0 wheel, 1 arrow buttons, 2 swipe joystick (default since 4.0.1), 3 tilt
        public float Sensitivity = 1f;
        public bool AutoGas = true;           // 4.0.1: on by default (brake still stops / reverses)
        public bool AutoAim = true;
        public bool Sound = true;
        public bool Haptics = true;
        public int Quality = 1;               // 0 low, 1 medium, 2 high, 3 ultra
        public bool QualityChosen = false;    // false -> auto-detect once on first launch
        public int Language = 0;              // 0 Turkish, 1 English (see Loc)
        public bool LanguageChosen = false;   // false -> language picker on first launch
        public float Brightness = 1.1f;
        public int FrameRate = 60;            // 30 / 60 / 90 / 120 (high-refresh phones)
        public int[] Fails = new int[Campaign.Total + 1];   // losses per level -> automatic assist

        // 2.0
        public bool DynamicResolution = true; // lowers the render size when the phone misses the frame rate
        public int Wins = 0;                  // matches won (rate prompt timing)
        public int RateState = 0;             // 0 not asked / asked later, 1 rated, 2 never ask again
        public int RateAskAtWin = 3;          // the win count at which the rate prompt shows next
        public string RewardDay = "";         // yyyyMMdd of the last rewarded scrap ad
        public int RewardCount = 0;           // rewarded scrap ads watched on RewardDay
        public bool PrivacySeen = false;      // privacy page opened at least once

        // 2.1
        public bool ScreenEffects = true;     // post effects: blast flash, hit jolt, nitro split, film grain (HUD damage edges stay)

        // 4.0: garage shop and vehicle collection (ids from Garage.Catalog / Fleet.Defs; "vehicle:<id>" for cars)
        public string[] Owned = new string[0];
        public string[] Equipped = new string[Garage.SlotCount];
        public string Vehicle = Fleet.DefaultId;
        public int MatchesPlayed = 0;         // for ad pacing and the daily missions
        public string MissionDay = "";        // yyyyMMdd of the daily mission set in progress
        public int[] MissionProgress = new int[3];
        public bool[] MissionClaimed = new bool[3];
        public int BestStreak = 0;

        // 4.0.1: customisable controls (ControlLayout): x, y, scale per ControlId; global size / opacity; driving aid
        public float[] ControlPos = null;
        public int ControlPreset = ControlLayout.Classic;
        public float ControlScale = 1f;
        public float ControlAlpha = 1f;
        public bool SteerAssist = true;       // counter-steer and spin-out help (off = raw handling)
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
            int before = Data.Version;
            Sanitize();
            if (Data.Version != before) Write();       // a migrated save is stored at once (the fresh start runs once)
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
            if (d.Version < 5) MigrateLegacyLevels(d);
            if (d.Version < 6) MigrateControls(d);
            if (d.Version < 7) { ResetProgress(d); d.Version = 7; }
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
            // 2.1 added season 6: a player who had cleared the old last level (50) gets level 51 opened.
            while (d.Unlocked < Campaign.Total && d.Stars[d.Unlocked] > 0) d.Unlocked++;
            d.LastPlayed = Mathf.Clamp(d.LastPlayed, 1, d.Unlocked);
            d.Scrap = Mathf.Max(0, d.Scrap);
            ControlLayout.Sanitize(d);
            d.Sensitivity = Mathf.Clamp(d.Sensitivity, 0.5f, 1.8f);
            d.Quality = Mathf.Clamp(d.Quality, 0, 3);
            d.Language = Mathf.Clamp(d.Language, 0, 1);
            if (d.FrameRate != 30 && d.FrameRate != 60 && d.FrameRate != 90 && d.FrameRate != 120) d.FrameRate = 60;
            d.Wins = Mathf.Max(0, d.Wins);
            d.RateState = Mathf.Clamp(d.RateState, 0, 2);
            if (d.RateAskAtWin < 1) d.RateAskAtWin = 3;
            if (d.RewardDay == null) d.RewardDay = "";
            d.RewardCount = Mathf.Max(0, d.RewardCount);
            if (d.Owned == null) d.Owned = new string[0];
            if (d.Equipped == null || d.Equipped.Length != Garage.SlotCount)
            {
                var e = new string[Garage.SlotCount];
                if (d.Equipped != null) Array.Copy(d.Equipped, e, Mathf.Min(d.Equipped.Length, e.Length));
                d.Equipped = e;
            }
            if (string.IsNullOrEmpty(d.Vehicle) || Fleet.Find(d.Vehicle) == null) d.Vehicle = Fleet.DefaultId;
            if (d.MissionDay == null) d.MissionDay = "";
            if (d.MissionProgress == null || d.MissionProgress.Length != 3) d.MissionProgress = new int[3];
            if (d.MissionClaimed == null || d.MissionClaimed.Length != 3) d.MissionClaimed = new bool[3];
            d.MatchesPlayed = Mathf.Max(0, d.MatchesPlayed);
        }

        /// <summary>
        /// 4.0: seasons grew from 10 to 20 levels. A save written before that (Version 4 and older, arrays of 51 or
        /// 61 entries) keeps every star: old level (season c, index i) becomes new level (c - 1) * 20 + i, so the
        /// cleared levels stay cleared and the new second half of each season is simply unplayed.
        /// </summary>
        static void MigrateLegacyLevels(SaveData d)
        {
            int oldPer = Campaign.LegacyPerChapter;
            System.Func<int, int> map = delegate (int k)
            {
                if (k < 1) return k;
                int c = (k - 1) / oldPer, i = (k - 1) % oldPer + 1;
                return Mathf.Clamp(c * Campaign.PerChapter + i, 1, Campaign.Total);
            };
            d.Stars = Remap(d.Stars, map);
            d.Fails = Remap(d.Fails, map);
            d.Unlocked = map(Mathf.Max(1, d.Unlocked));
            d.LastPlayed = map(Mathf.Max(1, d.LastPlayed));
            d.Version = 5;
        }

        /// <summary>
        /// 4.0.1: the easy-control defaults are applied once to older saves (swipe steering, auto gas, driving aid,
        /// classic layout). Everything else in the save is untouched; the player can change all of it under
        /// Settings → Controls.
        /// </summary>
        static void MigrateControls(SaveData d)
        {
            d.ControlMode = (int)SteerMode.Swipe;
            d.AutoGas = true;
            d.SteerAssist = true;
            d.ControlPos = null;
            d.ControlPreset = ControlLayout.Classic;
            d.ControlScale = 1f;
            d.ControlAlpha = 1f;
            d.Version = 6;
        }

        /// <summary>
        /// 4.0.2: progress back to a new game (levels, stars, scrap, upgrades, garage items and vehicles, missions,
        /// counters). Settings stay: language, controls, sound, graphics, privacy choices and the rating answer.
        /// Runs once for every save older than version 7 (the requested fresh start). There is no reset button: the
        /// save is no longer backed up by Android (see Editor/DerbyAndroidNoBackup.cs), so deleting the game deletes it.
        /// </summary>
        public static void ResetProgress(SaveData d)
        {
            if (d == null) return;
            d.Scrap = 0;
            d.Unlocked = 1;
            d.LastPlayed = 1;
            d.TotalKills = 0;
            d.Stars = new int[Campaign.Total + 1];
            d.Fails = new int[Campaign.Total + 1];
            d.Upgrades = new int[5];
            d.Wins = 0;
            d.Owned = new string[0];
            d.Equipped = new string[Garage.SlotCount];
            d.Vehicle = Fleet.DefaultId;
            d.MatchesPlayed = 0;
            d.MissionDay = "";
            d.MissionProgress = new int[3];
            d.MissionClaimed = new bool[3];
            d.BestStreak = 0;
            if (d.RateState == 0) d.RateAskAtWin = 3;
        }

        static int[] Remap(int[] old, System.Func<int, int> map)
        {
            var n = new int[Campaign.Total + 1];
            if (old == null) return n;
            for (int k = 1; k < old.Length; k++)
            {
                int nk = map(k);
                if (nk >= 1 && nk <= Campaign.Total) n[nk] = Mathf.Max(n[nk], old[k]);
            }
            return n;
        }

        /// <summary>Rewarded scrap ads left today (the counter resets with the calendar day).</summary>
        public static int RewardedScrapLeft(int perDay)
        {
            string today = System.DateTime.Now.ToString("yyyyMMdd");
            if (Data.RewardDay != today) return perDay;
            return Mathf.Max(0, perDay - Data.RewardCount);
        }

        public static void CountRewardedScrap()
        {
            string today = System.DateTime.Now.ToString("yyyyMMdd");
            if (Data.RewardDay != today) { Data.RewardDay = today; Data.RewardCount = 0; }
            Data.RewardCount++;
            Write();
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

        public const int Count = 5;

        public static string Name(int type)
        {
            switch (type)
            {
                case Engine: return Loc.T("MOTOR", "ENGINE");
                case Armor: return Loc.T("ZIRH", "ARMOR");
                case Weapon: return Loc.T("SİLAH", "WEAPON");
                case Nitro: return Loc.T("NİTRO", "NITRO");
                default: return Loc.T("ROKET", "ROCKETS");
            }
        }

        public static string Description(int type)
        {
            switch (type)
            {
                case Engine: return Loc.T("Hız ve ivme +%5", "Speed and acceleration +5%");
                case Armor: return Loc.T("Azami can +24", "Max health +24");
                case Weapon: return Loc.T("Makineli hasarı +%12", "Machine gun damage +12%");
                case Nitro: return Loc.T("Nitro süresi ve dolum hızı", "Nitro duration and recharge");
                default: return Loc.T("Başlangıç roketi +2", "Starting rockets +2");
            }
        }

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
