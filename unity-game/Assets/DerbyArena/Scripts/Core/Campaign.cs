using UnityEngine;

namespace DerbyArena.Core
{
    public enum ObjectiveKind { Reactor, Demolition, Boss, Checkpoint, Survival, LastStand }
    public enum ArenaKind { Spillway, Colosseum }
    public enum EnemyKind { Drifter, Rammer, Hunter, WarRig }

    public sealed class LevelConfig
    {
        public int Number;
        public int Chapter;
        public int IndexInChapter;
        public string Title;
        public string Subtitle;
        public ObjectiveKind Kind;
        public ArenaKind Arena;
        public int Theme;
        public int Cells;
        public int Gates;
        public EnemyKind[] Enemies;
        public int StartEnemies;
        public int SurvivalConcurrent;
        public float EnemyHp = 1f;
        public float EnemySpeed = 1f;
        public float EnemyDamage = 1f;
        public float AimErrorDeg = 6f;
        public float TimeLimit = 200f;
        public float ParTime = 110f;
        public int KillTarget;
        public int Reward = 100;
        public bool Boss;
        public float BossHealth;
        public int MaxShooters = 2;
        public float EliteChance;
        public bool AcidPools;
        public bool Night;
        public string Hint;

        public string ObjectiveLine
        {
            get
            {
                switch (Kind)
                {
                    case ObjectiveKind.Reactor: return Cells + " reaktör hücresini topla ve tahliye bölgesine ulaş";
                    case ObjectiveKind.Demolition: return "Arenadaki tüm rakipleri imha et";
                    case ObjectiveKind.Checkpoint: return Gates + " kontrol noktasından sırayla geç";
                    case ObjectiveKind.Survival: return Mathf.RoundToInt(TimeLimit) + " saniye hayatta kal";
                    case ObjectiveKind.LastStand: return "Daralan bölgede son hayatta kalan sen ol";
                    default: return "Patron aracını yok et";
                }
            }
        }

        public string StarTwoLine
        {
            get
            {
                if (Kind == ObjectiveKind.Survival) return "En az " + KillTarget + " imha";
                int t = Mathf.RoundToInt(ParTime);
                return "Hızlı bitiş (" + (t / 60) + ":" + (t % 60).ToString("00") + ")";
            }
        }

        public static string KindName(ObjectiveKind k)
        {
            switch (k)
            {
                case ObjectiveKind.Reactor: return "REAKTÖR";
                case ObjectiveKind.Demolition: return "YIKIM";
                case ObjectiveKind.Checkpoint: return "KONTROL";
                case ObjectiveKind.Survival: return "HAYATTA KAL";
                case ObjectiveKind.LastStand: return "SON KALAN";
                default: return "PATRON";
            }
        }
    }

    /// <summary>
    /// 50 levels in 5 chapters. Each chapter introduces something new: checkpoint runs (ch1), survival and the
    /// shrinking-zone "last one standing" mode (ch2), acid pools (ch3), night races with headlights (ch4) and elite
    /// enemies (ch5). Every 10th level is a boss. Deterministic and data driven.
    /// </summary>
    public static class Campaign
    {
        public const int Total = 50;
        public const int PerChapter = 10;

        public static readonly string[] ChapterNames =
        {
            "ACEMİ KOLEZYUM", "HURDA ÇORAKLIK", "ZEHİRLİ DÖKÜMHANE", "GECE SİBER-RİNG", "DEMİR KIYAMET"
        };

        public static readonly string[] ChapterFeatures =
        {
            "Reaktör, yıkım ve kontrol noktası görevleri",
            "Yeni: Hayatta Kal ve daralan bölgeli Son Kalan",
            "Yeni: asit havuzları, daha akıllı rakipler",
            "Yeni: gece yarışları, farlar ve neon",
            "Yeni: elit rakipler, en zor patron"
        };

        static readonly string[] BossNames =
        {
            "CRUSHER ALPHA", "SCRAPGUN PRIME", "FOUNDRY TYRANT", "MIDNIGHT BERSERKER", "OMEGA WAR RIG"
        };

        // R reactor, D demolition, C checkpoint, S survival, L last stand, B boss
        static readonly string[] Plan =
        {
            "RDRCDRCDRB",
            "RDSCLRDSCB",
            "SLRCDLRSCB",
            "LRCSDLCRSB",
            "RLSCDLRSCB"
        };

        static readonly float[] BossHealthByChapter = { 380f, 520f, 680f, 840f, 1000f };

        static ObjectiveKind KindOf(char c)
        {
            switch (c)
            {
                case 'D': return ObjectiveKind.Demolition;
                case 'C': return ObjectiveKind.Checkpoint;
                case 'S': return ObjectiveKind.Survival;
                case 'L': return ObjectiveKind.LastStand;
                case 'B': return ObjectiveKind.Boss;
                default: return ObjectiveKind.Reactor;
            }
        }

        public static LevelConfig Get(int level)
        {
            int n = Mathf.Clamp(level, 1, Total);
            var c = new LevelConfig();
            c.Number = n;
            c.Chapter = (n - 1) / PerChapter + 1;
            c.IndexInChapter = (n - 1) % PerChapter + 1;
            c.Theme = c.Chapter - 1;
            c.Kind = KindOf(Plan[c.Chapter - 1][c.IndexInChapter - 1]);
            c.Boss = c.Kind == ObjectiveKind.Boss;

            switch (c.Kind)
            {
                case ObjectiveKind.Reactor:
                case ObjectiveKind.Checkpoint:
                    c.Arena = ArenaKind.Spillway;
                    break;
                case ObjectiveKind.Survival:
                    c.Arena = c.Chapter % 2 == 0 ? ArenaKind.Spillway : ArenaKind.Colosseum;
                    break;
                default:
                    c.Arena = ArenaKind.Colosseum;
                    break;
            }

            float t = (n - 1) / (float)(Total - 1);
            c.Cells = c.Kind == ObjectiveKind.Reactor ? (n <= 12 ? 3 : (n <= 32 ? 4 : 5)) : 0;
            c.Gates = c.Kind == ObjectiveKind.Checkpoint ? (c.Chapter == 1 ? 6 : (c.Chapter == 2 ? 7 : 8)) : 0;

            int count;
            switch (c.Kind)
            {
                case ObjectiveKind.Reactor: count = Mathf.Clamp(2 + n / 9, 2, 6); break;
                case ObjectiveKind.Demolition: count = Mathf.Clamp(2 + n / 8, 2, 7); break;
                case ObjectiveKind.Checkpoint: count = Mathf.Clamp(1 + n / 12, 1, 5); break;
                case ObjectiveKind.Survival: count = 3; break;
                case ObjectiveKind.LastStand: count = Mathf.Clamp(3 + c.Chapter, 4, 8); break;
                default: count = c.Chapter - 1; break;
            }

            var rng = new System.Random(n * 7919 + 13);
            int k = 0;
            c.Enemies = new EnemyKind[count + (c.Boss ? 1 : 0)];
            if (c.Boss) c.Enemies[k++] = EnemyKind.WarRig;
            for (int i = 0; i < count; i++)
            {
                int pool = n <= 3 ? 1 : (c.Chapter <= 1 ? 2 : 3);
                int r = rng.Next(pool);
                c.Enemies[k++] = r == 0 ? EnemyKind.Drifter : (r == 1 ? EnemyKind.Rammer : EnemyKind.Hunter);
            }
            c.StartEnemies = c.Kind == ObjectiveKind.Reactor ? Mathf.Max(1, c.Enemies.Length / 2) : c.Enemies.Length;
            c.SurvivalConcurrent = c.Chapter <= 2 ? 2 : 3;

            // 1.4.0: gentler curve (1.3.0 felt like a wall from level 10 on)
            c.EnemyHp = 0.9f + 0.8f * t;
            c.EnemySpeed = 0.88f + 0.2f * t;
            c.EnemyDamage = 0.6f + 0.5f * t;
            c.AimErrorDeg = Mathf.Lerp(11f, 4.5f, t);
            c.MaxShooters = 2 + (c.Chapter >= 4 ? 1 : 0);
            c.EliteChance = c.Chapter >= 5 ? 0.4f : (c.Chapter == 4 ? 0.15f : 0f);
            c.AcidPools = c.Chapter == 3 || (c.Chapter == 5 && c.Arena == ArenaKind.Spillway);
            c.Night = c.Chapter == 4;
            c.BossHealth = BossHealthByChapter[c.Chapter - 1];

            switch (c.Kind)
            {
                case ObjectiveKind.Reactor: c.TimeLimit = 150f + c.Cells * 15f; break;
                case ObjectiveKind.Demolition: c.TimeLimit = 110f + count * 20f; break;
                case ObjectiveKind.Checkpoint: c.TimeLimit = 30f + c.Gates * 9f; break;
                case ObjectiveKind.Survival: c.TimeLimit = 50f + c.Chapter * 10f; break;
                case ObjectiveKind.LastStand: c.TimeLimit = 150f + count * 10f; break;
                default: c.TimeLimit = 300f; break;
            }
            c.ParTime = c.TimeLimit * (c.Kind == ObjectiveKind.Checkpoint ? 0.7f : 0.55f);
            c.KillTarget = 2 + c.Chapter;
            c.Reward = 70 + 18 * n + (c.Boss ? 250 : 0);

            switch (c.Kind)
            {
                case ObjectiveKind.Boss: c.Title = "PATRON: " + BossNames[c.Chapter - 1]; break;
                case ObjectiveKind.Demolition: c.Title = "YIKIM DERBİSİ"; break;
                case ObjectiveKind.Checkpoint: c.Title = "KONTROL NOKTASI"; break;
                case ObjectiveKind.Survival: c.Title = "HAYATTA KAL"; break;
                case ObjectiveKind.LastStand: c.Title = "SON KALAN"; break;
                default: c.Title = "REAKTÖR HATTI"; break;
            }
            c.Subtitle = "SEZON " + c.Chapter + " · " + ChapterNames[c.Chapter - 1];
            c.Hint = HintFor(n);
            return c;
        }

        static string HintFor(int n)
        {
            switch (n)
            {
                case 1: return "Direksiyonu çevirerek sür. Mavi ışık sütunlarındaki hücreleri topla.";
                case 2: return "Çarpmak da hasar verir: hızlı bindir, rakibin yan tarafına vur.";
                case 3: return "ATEŞ en yakın rakibe otomatik nişan alır. Silah ısınırsa biraz bekle.";
                case 4: return "Kontrol noktaları sırayla yanar. Her geçiş süreye +5 saniye ekler.";
                case 5: return "DRİFT tuşuyla keskin dön, NİTRO ile kaç ya da bindir.";
                case 6: return "ROKET ağır hasar verir. Turuncu sandıklar roket verir.";
                case 10: return "Patron yarı canın altına inince şok dalgası atar. Kırmızı halkadan uzak dur.";
                case 13: return "Hayatta Kal: süre bitene kadar ayakta kal. Rakipler dalgalar hâlinde gelir.";
                case 15: return "Son Kalan: bölge daralır, dışında kalan herkes hasar alır. Merkeze yakın kal.";
                case 21: return "Yeşil asit havuzları araçları eritir. Rakipleri içine it!";
                case 31: return "Gece yarışı: farların yolu aydınlatır, neon şeritleri takip et.";
                case 41: return "Elit rakipler (mor) daha dayanıklı ve isabetlidir.";
                default: return null;
            }
        }
    }
}
