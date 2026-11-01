using UnityEngine;

namespace DerbyArena.Core
{
    public enum ObjectiveKind { Reactor, Demolition, Boss }
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
        public EnemyKind[] Enemies;
        public int StartEnemies;
        public float EnemyHp = 1f;
        public float EnemySpeed = 1f;
        public float EnemyDamage = 1f;
        public float AimErrorDeg = 6f;
        public float TimeLimit = 200f;
        public float ParTime = 110f;
        public int Reward = 100;
        public bool Boss;
        public string Hint;

        public string ObjectiveLine
        {
            get
            {
                if (Kind == ObjectiveKind.Reactor) return Cells + " reaktör hücresini topla ve tahliye bölgesine ulaş";
                if (Kind == ObjectiveKind.Demolition) return "Arenadaki tüm rakipleri imha et";
                return "Patron aracını yok et";
            }
        }
    }

    /// <summary>50 levels: 5 chapters x 10, boss on every 10th. Deterministic and data driven.</summary>
    public static class Campaign
    {
        public const int Total = 50;
        public const int PerChapter = 10;

        public static readonly string[] ChapterNames =
        {
            "ACEMİ KOLEZYUM", "HURDA ÇORAKLIK", "ZEHİRLİ DÖKÜMHANE", "GECE SİBER-RİNG", "DEMİR KIYAMET"
        };

        static readonly string[] BossNames =
        {
            "CRUSHER ALPHA", "SCRAPGUN PRIME", "FOUNDRY TYRANT", "MIDNIGHT BERSERKER", "OMEGA WAR RIG"
        };

        public static LevelConfig Get(int level)
        {
            int n = Mathf.Clamp(level, 1, Total);
            var c = new LevelConfig();
            c.Number = n;
            c.Chapter = (n - 1) / PerChapter + 1;
            c.IndexInChapter = (n - 1) % PerChapter + 1;
            c.Theme = c.Chapter - 1;
            c.Boss = c.IndexInChapter == PerChapter;

            if (c.Boss) c.Kind = ObjectiveKind.Boss;
            else if (c.IndexInChapter % 3 == 0) c.Kind = ObjectiveKind.Demolition;
            else c.Kind = ObjectiveKind.Reactor;
            c.Arena = c.Kind == ObjectiveKind.Reactor ? ArenaKind.Spillway : ArenaKind.Colosseum;
            c.Cells = c.Kind == ObjectiveKind.Reactor ? (n <= 12 ? 3 : (n <= 32 ? 4 : 5)) : 0;

            int count;
            if (c.Kind == ObjectiveKind.Reactor) count = Mathf.Clamp(2 + n / 9, 2, 6);
            else if (c.Kind == ObjectiveKind.Demolition) count = Mathf.Clamp(2 + n / 8, 2, 7);
            else count = Mathf.Clamp(c.Chapter, 1, 5);

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

            float t = (n - 1) / (float)(Total - 1);
            c.EnemyHp = 1f + 1.1f * t;
            c.EnemySpeed = 0.9f + 0.25f * t;
            c.EnemyDamage = 0.75f + 0.8f * t;
            c.AimErrorDeg = Mathf.Lerp(10f, 3f, t);

            if (c.Kind == ObjectiveKind.Reactor) c.TimeLimit = 150f + c.Cells * 15f;
            else if (c.Kind == ObjectiveKind.Demolition) c.TimeLimit = 110f + count * 20f;
            else c.TimeLimit = 210f;
            c.ParTime = c.TimeLimit * 0.55f;
            c.Reward = 70 + 18 * n + (c.Boss ? 250 : 0);

            if (c.Boss) c.Title = "PATRON: " + BossNames[c.Chapter - 1];
            else if (c.Kind == ObjectiveKind.Demolition) c.Title = "YIKIM DERBİSİ";
            else c.Title = "REAKTÖR HATTI";
            c.Subtitle = "BÖLÜM " + c.Chapter + " · " + ChapterNames[c.Chapter - 1];
            c.Hint = HintFor(n);
            return c;
        }

        static string HintFor(int n)
        {
            switch (n)
            {
                case 1: return "Direksiyonu çevirerek sür. Mavi ışık sütunlarındaki hücreleri topla.";
                case 2: return "ATEŞ en yakın rakibe otomatik nişan alır. Silah ısınırsa biraz bekle.";
                case 3: return "Yıkım derbisi: çarpmak da hasar verir. Yüksek hızla bindir!";
                case 4: return "DRİFT tuşuyla keskin dön, NİTRO ile kaç.";
                case 5: return "ROKET ağır hasar verir. Turuncu sandıklar roket verir.";
                case 10: return "Patron yarı canın altına inince şok dalgası atar. Kırmızı halkadan uzak dur.";
                default: return null;
            }
        }
    }
}
