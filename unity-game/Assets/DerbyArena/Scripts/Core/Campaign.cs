using UnityEngine;

namespace DerbyArena.Core
{
    public enum ObjectiveKind { Reactor, Demolition, Boss, Checkpoint, Survival, LastStand }
    public enum ArenaKind { Spillway, Colosseum, Docks }
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
        public string BossName = "";

        public string ObjectiveLine
        {
            get
            {
                switch (Kind)
                {
                    case ObjectiveKind.Reactor: return Loc.T(Cells + " reaktör hücresini topla ve tahliye bölgesine ulaş", "Collect " + Cells + " reactor cells and reach the extraction zone");
                    case ObjectiveKind.Demolition: return Loc.T("Arenadaki tüm rakipleri imha et", "Destroy every rival in the arena");
                    case ObjectiveKind.Checkpoint: return Loc.T(Gates + " kontrol noktasından sırayla geç", "Pass " + Gates + " checkpoints in order");
                    case ObjectiveKind.Survival: return Loc.T(Mathf.RoundToInt(TimeLimit) + " saniye hayatta kal", "Survive for " + Mathf.RoundToInt(TimeLimit) + " seconds");
                    case ObjectiveKind.LastStand: return Loc.T("Daralan bölgede son hayatta kalan sen ol", "Be the last one standing in the shrinking zone");
                    default: return Loc.T("Patron aracını yok et", "Destroy the boss vehicle");
                }
            }
        }

        public string StarTwoLine
        {
            get
            {
                if (Kind == ObjectiveKind.Survival) return Loc.T("En az " + KillTarget + " imha", "At least " + KillTarget + " kills");
                int t = Mathf.RoundToInt(ParTime);
                return Loc.T("Hızlı bitiş (", "Fast finish (") + (t / 60) + ":" + (t % 60).ToString("00") + ")";
            }
        }

        public static string KindName(ObjectiveKind k)
        {
            switch (k)
            {
                case ObjectiveKind.Reactor: return Loc.T("REAKTÖR", "REACTOR");
                case ObjectiveKind.Demolition: return Loc.T("YIKIM", "DEMOLITION");
                case ObjectiveKind.Checkpoint: return Loc.T("KONTROL", "CHECKPOINT");
                case ObjectiveKind.Survival: return Loc.T("HAYATTA KAL", "SURVIVAL");
                case ObjectiveKind.LastStand: return Loc.T("SON KALAN", "LAST STAND");
                default: return Loc.T("PATRON", "BOSS");
            }
        }
    }

    /// <summary>
    /// 50 released levels in 5 chapters (a sixth, the Container Port, is prepared but gated). Each chapter introduces
    /// something new: checkpoint runs (ch1), survival and the shrinking-zone "last one standing" mode (ch2), acid
    /// pools (ch3), night races with headlights (ch4), elite enemies (ch5) and the Container Port arena with ramps
    /// and container cover (ch6). Every 10th level is a boss. Deterministic and data driven.
    /// </summary>
    public static class Campaign
    {
        // Season 6 (Container Port, levels 51-60: DocksArena, theme 5, the sixth plan/boss/name entries below) is
        // built and statically tested but NOT released yet: the level list keeps the roadmap tab in its place.
        // To ship it raise ReleasedSeasons to 6. Saves resize automatically (SaveData.Sanitize), progress is kept.
        public const int ReleasedSeasons = 5;
        public const int PerChapter = 10;
        public const int Total = ReleasedSeasons * PerChapter;
        public const int Seasons = Total / PerChapter;

        static readonly string[] ChapterNamesTr = { "ACEMİ KOLEZYUM", "HURDA ÇORAKLIK", "ZEHİRLİ DÖKÜMHANE", "GECE SİBER-RİNG", "DEMİR KIYAMET", "KONTEYNER LİMANI" };
        static readonly string[] ChapterNamesEn = { "ROOKIE COLOSSEUM", "SCRAP BADLANDS", "TOXIC FOUNDRY", "NIGHT CYBER RING", "IRON APOCALYPSE", "CONTAINER PORT" };

        static readonly string[] ChapterFeaturesTr =
        {
            "Reaktör, yıkım ve kontrol noktası görevleri",
            "Yeni: Hayatta Kal ve daralan bölgeli Son Kalan",
            "Yeni: asit havuzları, daha akıllı rakipler",
            "Yeni: gece yarışları, farlar ve neon",
            "Yeni: elit rakipler, en zor patron",
            "Yeni arena: Konteyner Limanı. Konteyner siperleri, rampalar, dev vinç"
        };
        static readonly string[] ChapterFeaturesEn =
        {
            "Reactor, demolition and checkpoint missions",
            "New: Survival and Last Stand with a shrinking zone",
            "New: acid pools, smarter rivals",
            "New: night races, headlights and neon",
            "New: elite rivals, the toughest boss",
            "New arena: Container Port. Container cover, jump ramps, a giant crane"
        };

        /// <summary>Season (1-based) name in the current language.</summary>
        public static string ChapterName(int chapter)
        {
            int i = Mathf.Clamp(chapter - 1, 0, ChapterNamesTr.Length - 1);
            return Loc.T(ChapterNamesTr[i], ChapterNamesEn[i]);
        }

        public static string ChapterFeature(int chapter)
        {
            int i = Mathf.Clamp(chapter - 1, 0, ChapterFeaturesTr.Length - 1);
            return Loc.T(ChapterFeaturesTr[i], ChapterFeaturesEn[i]);
        }

        public static bool AllCleared { get { return Save.Data.Stars[Total] > 0; } }

        static readonly string[] BossNames =
        {
            "CRUSHER ALPHA", "SCRAPGUN PRIME", "FOUNDRY TYRANT", "MIDNIGHT BERSERKER", "OMEGA WAR RIG", "DOCKYARD KRAKEN"
        };

        // R reactor, D demolition, C checkpoint, S survival, L last stand, B boss
        static readonly string[] Plan =
        {
            "RDRCDRCDRB",
            "RDSCLRDSCB",
            "SLRCDLRSCB",
            "LRCSDLCRSB",
            "RLSCDLRSCB",
            "CDRSLCDRLB"
        };

        static readonly float[] BossHealthByChapter = { 380f, 520f, 680f, 840f, 1000f, 1150f };

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
            if (c.Chapter == 6) c.Arena = ArenaKind.Docks;   // the Container Port hosts every mode

            // Difficulty runs over the original 50 levels (1.4.0 curve, unchanged for them); season 6 continues
            // past 1 (enemy health / speed / damage keep rising, aim error is clamped by the Lerp).
            float t = (n - 1) / 49f;
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
            c.EliteChance = c.Chapter >= 6 ? 0.5f : (c.Chapter == 5 ? 0.4f : (c.Chapter == 4 ? 0.15f : 0f));
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
                case ObjectiveKind.Boss: c.Title = Loc.T("PATRON: ", "BOSS: ") + BossNames[c.Chapter - 1]; break;
                case ObjectiveKind.Demolition: c.Title = Loc.T("YIKIM DERBİSİ", "DEMOLITION DERBY"); break;
                case ObjectiveKind.Checkpoint: c.Title = Loc.T("KONTROL NOKTASI", "CHECKPOINT RUN"); break;
                case ObjectiveKind.Survival: c.Title = Loc.T("HAYATTA KAL", "SURVIVAL"); break;
                case ObjectiveKind.LastStand: c.Title = Loc.T("SON KALAN", "LAST STAND"); break;
                default: c.Title = Loc.T("REAKTÖR HATTI", "REACTOR RUN"); break;
            }
            c.BossName = c.Boss ? BossNames[c.Chapter - 1] : "";
            c.Subtitle = Loc.T("SEZON ", "SEASON ") + c.Chapter + " · " + ChapterName(c.Chapter);
            c.Hint = HintFor(n);
            return c;
        }

        static string HintFor(int n)
        {
            switch (n)
            {
                case 1: return Loc.T("Direksiyonu çevirerek sür. Mavi ışık sütunlarındaki hücreleri topla.", "Turn the wheel to steer. Collect the cells under the blue light beams.");
                case 2: return Loc.T("Çarpmak da hasar verir: hızlı bindir, rakibin yan tarafına vur.", "Ramming hurts too: hit fast and aim for the rival's side.");
                case 3: return Loc.T("ATEŞ en yakın rakibe otomatik nişan alır. Silah ısınırsa biraz bekle.", "FIRE auto-aims at the nearest rival. If the gun overheats, wait a moment.");
                case 4: return Loc.T("Kontrol noktaları sırayla yanar. Her geçiş süreye +5 saniye ekler.", "Checkpoints light up in order. Each one adds +5 seconds.");
                case 5: return Loc.T("DRİFT tuşuyla keskin dön, NİTRO ile kaç ya da bindir.", "Use DRIFT for sharp turns, NITRO to escape or ram.");
                case 6: return Loc.T("ROKET ağır hasar verir. Turuncu sandıklar roket verir.", "ROCKETS hit hard. Orange crates refill them.");
                case 10: return Loc.T("Patron güçsüzleşince şok dalgası atar. Kırmızı halkadan uzak dur.", "When the boss weakens it fires a shockwave. Keep out of the red ring.");
                case 13: return Loc.T("Hayatta Kal: süre bitene kadar ayakta kal. Rakipler dalgalar hâlinde gelir.", "Survival: stay alive until the timer ends. Rivals come in waves.");
                case 15: return Loc.T("Son Kalan: bölge daralır, dışında kalan herkes hasar alır. Merkeze yakın kal.", "Last Stand: the zone shrinks and hurts anyone outside. Stay near the center.");
                case 21: return Loc.T("Yeşil asit havuzları araçları eritir. Rakipleri içine it!", "Green acid pools melt cars. Push your rivals in!");
                case 31: return Loc.T("Gece yarışı: farların yolu aydınlatır, neon şeritleri takip et.", "Night race: your headlights light the way, follow the neon strips.");
                case 41: return Loc.T("Elit rakipler (mor) daha dayanıklı ve isabetlidir.", "Elite rivals (purple) are tougher and more accurate.");
                case 51: return Loc.T("Konteyner Limanı: ortadaki rampalardan uçarak kestirme yap, konteynerlerin arkasına saklan.", "Container Port: fly off the centre ramps for a shortcut, hide behind the containers.");
                default: return null;
            }
        }
    }
}
