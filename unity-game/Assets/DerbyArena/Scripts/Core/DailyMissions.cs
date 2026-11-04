using System;
using UnityEngine;

namespace DerbyArena.Core
{
    public enum MissionGoal { Kills, Wins, Nitro, Rockets, Rams, Stars }

    public sealed class DailyMission
    {
        public int Index;
        public MissionGoal Goal;
        public int Target;
        public int Reward;

        public string Text
        {
            get
            {
                switch (Goal)
                {
                    case MissionGoal.Kills: return Loc.T(Target + " rakip imha et", "Destroy " + Target + " rivals");
                    case MissionGoal.Wins: return Loc.T(Target + " bölüm kazan", "Win " + Target + " levels");
                    case MissionGoal.Nitro: return Loc.T(Target + " kez nitro kullan", "Use nitro " + Target + " times");
                    case MissionGoal.Rockets: return Loc.T(Target + " roket isabet ettir", "Land " + Target + " rockets");
                    case MissionGoal.Rams: return Loc.T(Target + " kez rakibe bindir", "Ram rivals " + Target + " times");
                    default: return Loc.T(Target + " yıldız kazan", "Earn " + Target + " stars");
                }
            }
        }

        public string Icon
        {
            get
            {
                switch (Goal)
                {
                    case MissionGoal.Kills: return "skull";
                    case MissionGoal.Wins: return "trophy";
                    case MissionGoal.Nitro: return "flame";
                    case MissionGoal.Rockets: return "rocket";
                    case MissionGoal.Rams: return "bumper";
                    default: return "star";
                }
            }
        }

        public int Progress { get { return Mathf.Min(Target, Save.Data.MissionProgress[Index]); } }
        public bool Done { get { return Save.Data.MissionProgress[Index] >= Target; } }
        public bool Claimed { get { return Save.Data.MissionClaimed[Index]; } }
    }

    /// <summary>
    /// Offline daily missions (4.0, like the runner's): three goals seeded by the calendar day, progress counted
    /// during matches, scrap claimed in the menu. Resets at local midnight; progress of an old day is dropped.
    /// </summary>
    public static class DailyMissions
    {
        static DailyMission[] cache;
        static string cacheDay = "";

        public static string Today { get { return DateTime.Now.ToString("yyyyMMdd"); } }

        public static DailyMission[] Missions()
        {
            string day = Today;
            if (cache != null && cacheDay == day) return cache;
            Sync();
            var rng = new System.Random(int.Parse(day) * 31 + 7);
            int tier = Mathf.Clamp(Save.Data.Unlocked / 20, 0, 4);        // harder, better paid as the player progresses
            var m = new DailyMission[3];
            m[0] = new DailyMission { Index = 0, Goal = MissionGoal.Kills, Target = 8 + tier * 3 + rng.Next(0, 5), Reward = 300 + tier * 120 };
            m[1] = new DailyMission { Index = 1, Goal = MissionGoal.Wins, Target = 2 + rng.Next(0, 2) + (tier >= 2 ? 1 : 0), Reward = 400 + tier * 150 };
            int pick = rng.Next(0, 4);
            switch (pick)
            {
                case 0: m[2] = new DailyMission { Index = 2, Goal = MissionGoal.Nitro, Target = 10 + tier * 4 + rng.Next(0, 6), Reward = 250 + tier * 100 }; break;
                case 1: m[2] = new DailyMission { Index = 2, Goal = MissionGoal.Rockets, Target = 4 + tier * 2 + rng.Next(0, 3), Reward = 350 + tier * 120 }; break;
                case 2: m[2] = new DailyMission { Index = 2, Goal = MissionGoal.Rams, Target = 6 + tier * 3 + rng.Next(0, 4), Reward = 300 + tier * 110 }; break;
                default: m[2] = new DailyMission { Index = 2, Goal = MissionGoal.Stars, Target = 4 + tier + rng.Next(0, 3), Reward = 450 + tier * 150 }; break;
            }
            cache = m;
            cacheDay = day;
            return m;
        }

        /// <summary>Drops the previous day's progress when the calendar day changed.</summary>
        static void Sync()
        {
            var d = Save.Data;
            string day = Today;
            if (d.MissionDay == day) return;
            d.MissionDay = day;
            for (int i = 0; i < 3; i++) { d.MissionProgress[i] = 0; d.MissionClaimed[i] = false; }
        }

        /// <summary>Counts progress; the save is written by the match end / claim, not here.</summary>
        public static void Tick(MissionGoal goal, int amount)
        {
            if (amount <= 0) return;
            var ms = Missions();
            for (int i = 0; i < ms.Length; i++)
                if (ms[i].Goal == goal && !ms[i].Claimed)
                    Save.Data.MissionProgress[i] = Mathf.Min(ms[i].Target, Save.Data.MissionProgress[i] + amount);
        }

        public static bool Claim(int index)
        {
            var ms = Missions();
            if (index < 0 || index >= ms.Length) return false;
            var m = ms[index];
            if (!m.Done || m.Claimed) return false;
            Save.Data.MissionClaimed[index] = true;
            Save.Data.Scrap += m.Reward;
            Save.Write();
            return true;
        }

        public static int ClaimableCount()
        {
            int n = 0;
            foreach (var m in Missions()) if (m.Done && !m.Claimed) n++;
            return n;
        }

        public static TimeSpan TimeLeft
        {
            get
            {
                var now = DateTime.Now;
                return now.Date.AddDays(1) - now;
            }
        }
    }
}
