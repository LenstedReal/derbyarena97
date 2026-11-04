using UnityEngine;

namespace DerbyArena.Core
{
    public enum ObjectiveKind { Reactor, Demolition, Boss, Checkpoint, Survival, LastStand }
    public enum ArenaKind { Spillway, Colosseum, Docks, Ruins }
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
        public int StarsRequired;

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
    /// 100 released levels in 5 seasons of 20 (a sixth season, the Container Port, is prepared but gated). Each season
    /// introduces something new: checkpoint runs (s1), survival and the shrinking-zone "last one standing" mode (s2),
    /// acid pools (s3), night races with headlights (s4), elite enemies (s5) and the Container Port arena with ramps
    /// and container cover (s6). Level 10 of every season is a big demolition, level 20 the boss. Levels 6, 11, 16 and
    /// every season after the first are star gated (StarsRequired): the player needs a share of the stars available
    /// so far. Deterministic and data driven.
    /// </summary>
    public static class Campaign
    {
        // Season 6 (Container Port, levels 101-120: DocksArena, theme 5, the sixth plan/boss/name entries below) is
        // built and statically tested but NOT released yet: the level list keeps the roadmap tab in its place.
        // To ship it raise ReleasedSeasons to 6. Saves resize automatically (SaveData.Sanitize), progress is kept.
        public const int ReleasedSeasons = 5;
        public const int PerChapter = 20;
        public const int Total = ReleasedSeasons * PerChapter;
        public const int Seasons = Total / PerChapter;
        /// <summary>Levels per season before 4.0 (saves written by 1.x-2.1 are remapped, see SaveData).</summary>
        public const int LegacyPerChapter = 10;

        static readonly string[] ChapterNamesTr = { "ACEMİ KOLEZYUM", "HURDA ÇORAKLIK", "ZEHİRLİ DÖKÜMHANE", "GECE SİBER-RİNG", "DEMİR KIYAMET", "KONTEYNER LİMANI" };
        static readonly string[] ChapterNamesEn = { "ROOKIE COLOSSEUM", "SCRAP BADLANDS", "TOXIC FOUNDRY", "NIGHT CYBER RING", "IRON APOCALYPSE", "CONTAINER PORT" };

        static readonly string[] ChapterFeaturesTr =
        {
            "Reaktör, yıkım ve kontrol noktası görevleri",
            "Yeni arena: Çöl Harabeleri. Yeni: Hayatta Kal ve daralan bölgeli Son Kalan",
            "Yeni: asit havuzları, daha akıllı rakipler",
            "Yeni: gece yarışları, farlar ve neon",
            "Yeni: elit rakipler, harabe derbileri, en zor patron",
            "Yeni arena: Konteyner Limanı. Konteyner siperleri, rampalar, dev vinç"
        };
        static readonly string[] ChapterFeaturesEn =
        {
            "Reactor, demolition and checkpoint missions",
            "New arena: Desert Ruins. New: Survival and Last Stand with a shrinking zone",
            "New: acid pools, smarter rivals",
            "New: night races, headlights and neon",
            "New: elite rivals, ruins derbies, the toughest boss",
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

        // R reactor, D demolition, C checkpoint, S survival, L last stand, B boss (20 per season; index 10 is a big
        // demolition, index 20 the boss)
        static readonly string[] Plan =
        {
            "RDRCDRCDRD" + "RCDRCRDCRB",
            "RDSCLRDSCD" + "RSDCLRSDCB",
            "SLRCDLRSCD" + "RLSCDRLSCB",
            "LRCSDLCRSD" + "CLRSDCLRSB",
            "RLSCDLRSCD" + "LRSCDLRSCB",
            "CDRSLCDRLD" + "CDRSLCDRLB"
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
            // 4.0: the Desert Ruins host the Scrap Badlands derbies (season 2) and every other Iron Apocalypse
            // derby (season 5, odd levels); reactor and checkpoint runs stay in the spillway, bosses in the colosseum
            if (c.Arena == ArenaKind.Colosseum && !c.Boss && (c.Chapter == 2 || (c.Chapter == 5 && c.IndexInChapter % 2 == 1))) c.Arena = ArenaKind.Ruins;

            // Difficulty runs over the 100 released levels; season 6 continues past 1 (enemy health / speed / damage
            // keep rising, aim error is clamped by the Lerp).
            float t = (n - 1) / (float)(Total - 1);
            bool big = c.IndexInChapter == PerChapter / 2;          // level 10 of each season: the big derby
            c.Cells = c.Kind == ObjectiveKind.Reactor ? (n <= 24 ? 3 : (n <= 64 ? 4 : 5)) : 0;
            c.Gates = c.Kind == ObjectiveKind.Checkpoint ? (c.Chapter == 1 ? 6 : (c.Chapter == 2 ? 7 : 8)) : 0;

            int count;
            switch (c.Kind)
            {
                case ObjectiveKind.Reactor: count = Mathf.Clamp(2 + n / 18, 2, 6); break;
                case ObjectiveKind.Demolition: count = Mathf.Clamp(2 + n / 14, 2, 7) + (big ? 1 : 0); break;
                case ObjectiveKind.Checkpoint: count = Mathf.Clamp(1 + n / 24, 1, 5); break;
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
            if (big) c.EliteChance = Mathf.Max(c.EliteChance, c.Chapter >= 3 ? 0.35f : 0.15f);
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
            c.Reward = 70 + 18 * n + (c.Boss ? 250 : 0) + (big ? 120 : 0);

            switch (c.Kind)
            {
                case ObjectiveKind.Boss: c.Title = Loc.T("PATRON: ", "BOSS: ") + BossNames[c.Chapter - 1]; break;
                case ObjectiveKind.Demolition: c.Title = big ? Loc.T("BÜYÜK DERBİ", "GRAND DERBY") : Loc.T("YIKIM DERBİSİ", "DEMOLITION DERBY"); break;
                case ObjectiveKind.Checkpoint: c.Title = Loc.T("KONTROL NOKTASI", "CHECKPOINT RUN"); break;
                case ObjectiveKind.Survival: c.Title = Loc.T("HAYATTA KAL", "SURVIVAL"); break;
                case ObjectiveKind.LastStand: c.Title = Loc.T("SON KALAN", "LAST STAND"); break;
                default: c.Title = Loc.T("REAKTÖR HATTI", "REACTOR RUN"); break;
            }
            c.BossName = c.Boss ? BossNames[c.Chapter - 1] : "";
            c.Subtitle = Loc.T("SEZON ", "SEASON ") + c.Chapter + " · " + ChapterName(c.Chapter);
            c.Hint = HintFor(n, c);
            c.StarsRequired = StarsRequired(n);
            return c;
        }

        /// <summary>
        /// Stars the player must own before level n opens (besides having cleared level n - 1): the first level of
        /// every season after the first asks for half of the stars available before it, levels 6, 11 and 16 of a
        /// season for 45%. Everything else only needs the previous level cleared.
        /// </summary>
        public static int StarsRequired(int n)
        {
            if (n <= 1) return 0;
            int idx = (n - 1) % PerChapter + 1;
            int before = (n - 1) * 3;
            if (idx == 1) return Mathf.RoundToInt(before * 0.5f);
            if (idx == 6 || idx == 11 || idx == 16) return Mathf.RoundToInt(before * 0.45f);
            return 0;
        }

        /// <summary>Level n can be played: the previous level is cleared and the star gate (if any) is met.</summary>
        public static bool IsOpen(int n)
        {
            if (n < 1 || n > Total) return false;
            return n <= Save.Data.Unlocked && Save.TotalStars() >= StarsRequired(n);
        }

        /// <summary>Stars still missing for level n (0 when the gate is met).</summary>
        public static int StarsMissing(int n)
        {
            return Mathf.Max(0, StarsRequired(n) - Save.TotalStars());
        }

        /// <summary>The level the PLAY button should offer: the first open level without a star, else the last open one.</summary>
        public static int NextToPlay()
        {
            int last = 1;
            for (int n = 1; n <= Total; n++)
            {
                if (!IsOpen(n)) break;
                last = n;
                if (Save.Data.Stars[n] == 0) return n;
            }
            return last;
        }

        /// <summary>First level (1-based) whose plan code is the given one, 0 if none.</summary>
        static int FirstOf(char code)
        {
            for (int ch = 0; ch < Plan.Length; ch++)
            {
                int i = Plan[ch].IndexOf(code);
                if (i >= 0) return ch * PerChapter + i + 1;
            }
            return 0;
        }

        static string HintFor(int n, LevelConfig c)
        {
            switch (n)
            {
                case 1: return Loc.T("Direksiyonu çevirerek sür. Mavi ışık sütunlarındaki hücreleri topla.", "Turn the wheel to steer. Collect the cells under the blue light beams.");
                case 2: return Loc.T("Çarpmak da hasar verir: hızlı bindir, rakibin yan tarafına vur.", "Ramming hurts too: hit fast and aim for the rival's side.");
                case 3: return Loc.T("ATEŞ en yakın rakibe otomatik nişan alır. Silah ısınırsa biraz bekle.", "FIRE auto-aims at the nearest rival. If the gun overheats, wait a moment.");
                case 4: return Loc.T("Kontrol noktaları sırayla yanar. Her geçiş süreye +5 saniye ekler.", "Checkpoints light up in order. Each one adds +5 seconds.");
                case 5: return Loc.T("DRİFT tuşuyla keskin dön, NİTRO ile kaç ya da bindir.", "Use DRIFT for sharp turns, NITRO to escape or ram.");
                case 6: return Loc.T("ROKET ağır hasar verir. Turuncu sandıklar roket verir.", "ROCKETS hit hard. Orange crates refill them.");
            }
            if (n == FirstOf('B')) return Loc.T("Patron güçsüzleşince şok dalgası atar. Kırmızı halkadan uzak dur.", "When the boss weakens it fires a shockwave. Keep out of the red ring.");
            if (n == FirstOf('S')) return Loc.T("Hayatta Kal: süre bitene kadar ayakta kal. Rakipler dalgalar hâlinde gelir.", "Survival: stay alive until the timer ends. Rivals come in waves.");
            if (n == FirstOf('L')) return Loc.T("Son Kalan: bölge daralır, dışında kalan herkes hasar alır. Merkeze yakın kal.", "Last Stand: the zone shrinks and hurts anyone outside. Stay near the center.");
            if (c.IndexInChapter == PerChapter / 2) return Loc.T("Büyük Derbi: daha kalabalık, daha sert. Siperleri kullan, rakipleri birbirine düşür.", "Grand Derby: more rivals, harder hits. Use cover and let rivals ram each other.");
            if (c.IndexInChapter == 1)
            {
                switch (c.Chapter)
                {
                    case 2: return Loc.T("Çöl Harabeleri: yıkık evler siperdir, sokaklarda hız kazan, rakipleri duvara sıkıştır.", "Desert Ruins: ruined houses are cover, build speed in the streets, pin rivals against the walls.");
                    case 3: return Loc.T("Yeşil asit havuzları araçları eritir. Rakipleri içine it!", "Green acid pools melt cars. Push your rivals in!");
                    case 4: return Loc.T("Gece yarışı: farların yolu aydınlatır, neon şeritleri takip et.", "Night race: your headlights light the way, follow the neon strips.");
                    case 5: return Loc.T("Elit rakipler (mor) daha dayanıklı ve isabetlidir.", "Elite rivals (purple) are tougher and more accurate.");
                    case 6: return Loc.T("Konteyner Limanı: ortadaki rampalardan uçarak kestirme yap, konteynerlerin arkasına saklan.", "Container Port: fly off the centre ramps for a shortcut, hide behind the containers.");
                }
            }
            return null;
        }
    }
}
