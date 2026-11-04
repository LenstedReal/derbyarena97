using System;
using System.Collections;
using System.Collections.Generic;
using DerbyArena.AI;
using DerbyArena.Audio;
using DerbyArena.CameraRig;
using DerbyArena.Combat;
using DerbyArena.Core;
using DerbyArena.UI;
using DerbyArena.Vehicles;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Mission
{
    public enum MatchPhase { Loading, Countdown, Playing, Won, Lost, Downed }

    /// <summary>
    /// Owns one match: spawns, objectives (reactor run / demolition / boss), timer, pickups,
    /// out-of-bounds recovery, scoring, stars, rewards and save. One instance per scene load.
    /// </summary>
    public sealed class MatchDirector : MonoBehaviour
    {
        public static MatchDirector Current;

        public LevelConfig Config;
        public ArenaLoader Arena;
        public MatchPhase Phase = MatchPhase.Loading;
        public float TimeLeft;
        public float Elapsed;
        public float Countdown;
        public int Kills;
        public int CellsTaken;
        public float ExtractHold;
        public const float ExtractRequired = 3.5f;
        public bool ExtractionOpen;
        public int Stars;
        public int Reward;
        public string LoseReason = "";
        public bool NewUnlock;

        public ArcadeCar PlayerCar;
        public VehicleHealth PlayerHealth;
        public WeaponRig PlayerWeapons;
        public VehicleVisual PlayerVisual;
        public VehicleHealth Boss;
        public readonly List<EnemyDriver> Enemies = new List<EnemyDriver>();
        public readonly List<ReactorCell> Cells = new List<ReactorCell>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public ExtractionPad Extraction;
        public readonly List<CheckpointGate> Gates = new List<CheckpointGate>();
        public readonly List<AcidPool> Acid = new List<AcidPool>();
        public int GatesPassed;
        public ZoneRing Zone;
        public bool PlayerOutsideZone;
        public const float ZoneDelay = 15f;
        public const float ZoneShrinkTime = 100f;

        public event Action<string, Color> Message;

        TouchControls controls;
        Queue<EnemyKind> pending = new Queue<EnemyKind>();
        Transform root;
        float endTimer;
        bool resultsSent;
        int beepSecond = -1;
        string lastMessage = "";
        float lastMessageTime = -10f;
        float hazardTick;
        int survivalSpawnIndex;
        System.Random eliteRng;
        /// <summary>0..3: grows with every loss on this level, eases damage/health/time (reset on win).</summary>
        public int Assist;
        float damageScale = 1f;
        float healthScale = 1f;
        /// <summary>The rewarded second chance was used in this match (one per match).</summary>
        public bool ReviveUsed;
        /// <summary>Seconds left of the second-chance offer (unscaled; the match is frozen meanwhile).</summary>
        public float ReviveTimeLeft;
        /// <summary>The rewarded "double scrap" was taken on the result screen.</summary>
        public bool RewardDoubled;

        public static MatchDirector Begin(LevelConfig config, ArenaLoader arena, TouchControls touch, Transform sceneRoot)
        {
            var go = new GameObject("MatchDirector");
            go.transform.SetParent(sceneRoot, false);
            var m = go.AddComponent<MatchDirector>();
            Current = m;
            m.Config = config;
            m.Arena = arena;
            m.controls = touch;
            m.root = sceneRoot;
            m.TimeLeft = config.TimeLimit;
            m.StartCoroutine(m.Setup());
            return m;
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Say(string text, Color color)
        {
            if (text == lastMessage && Time.time - lastMessageTime < 1.5f) return;
            lastMessage = text;
            lastMessageTime = Time.time;
            if (Message != null) Message(text, color);
        }

        IEnumerator Setup()
        {
            // preload vehicle models while the arena streams in
            var preSpec = VehicleSpec.Interceptor();
            GlbLibrary.Preload(preSpec.GlbFile, preSpec.ModelOptions());
            foreach (var k in Config.Enemies)
            {
                var ks = SpecOf(k);
                GlbLibrary.Preload(ks.GlbFile, ks.ModelOptions());
            }
            if (Config.EliteChance > 0f)
            {
                foreach (var k in Config.Enemies)
                {
                    if (k == EnemyKind.WarRig) continue;
                    var es = SpecOf(k);
                    es.MakeElite();
                    GlbLibrary.Preload(es.GlbFile, es.ModelOptions());
                }
            }
            eliteRng = new System.Random(Config.Number * 977 + 13);
            EnemyDriver.ResetTokens(Config.MaxShooters);

            Assist = Mathf.Clamp(Save.Data.Fails[Config.Number], 0, 3);
            damageScale = Config.EnemyDamage * (1f - 0.15f * Assist) * (Config.Kind == ObjectiveKind.Survival ? 0.75f : 1f);
            healthScale = 1f - 0.1f * Assist;
            TimeLeft += 15f * Assist * (Config.Kind == ObjectiveKind.Survival ? 0f : 1f);
            if (Config.Kind == ObjectiveKind.Survival) TimeLeft *= 1f - 0.1f * Assist;

            float wait = 0f;
            while (!Arena.Ready && wait < 15f) { wait += Time.unscaledDeltaTime; yield return null; }
            var info = Arena.Info;

            // player
            var spec = VehicleSpec.Interceptor();
            spec.Health += Upgrades.HealthBonus();
            var player = SpawnVehicle(spec, info.PlayerSpawn, info.PlayerYaw, 0);
            PlayerCar = player.GetComponent<ArcadeCar>();
            PlayerHealth = player.GetComponent<VehicleHealth>();
            PlayerWeapons = player.GetComponent<WeaponRig>();
            PlayerVisual = player.GetComponent<VehicleVisual>();
            PlayerCar.SpeedMul = Upgrades.SpeedMul();
            PlayerCar.NitroDrainMul = Upgrades.NitroDrainMul();
            PlayerCar.NitroRegenMul = Upgrades.NitroRegenMul();
            PlayerWeapons.DamageMul = Upgrades.DamageMul();
            PlayerWeapons.Rockets = spec.RocketAmmo + Upgrades.ExtraRockets();
            PlayerWeapons.AimErrorDeg = 1.2f;
            PlayerWeapons.TracerColor = new Color(1f, 0.85f, 0.45f, 1f);
            player.AddComponent<EngineAudio>().Volume = 0.42f;
            player.AddComponent<PlayerDriver>().Init(this, controls);
            PlayerHealth.Died += OnPlayerDied;
            PlayerHealth.DeferDeath = OfferSecondChance;
            PlayerCar.Locked = true;

            var cam = ChaseCamera.Main;
            if (cam != null)
            {
                cam.Orbit = false;
                cam.Target = player.transform;
                cam.Car = PlayerCar;
                cam.SnapBehind();
            }

            // objectives
            if (Config.Kind == ObjectiveKind.Reactor)
            {
                var pts = Shuffle(info.CellPoints, Config.Number);
                for (int i = 0; i < Config.Cells && i < pts.Length; i++) Cells.Add(ReactorCell.Create(root, pts[i]));
                Extraction = ExtractionPad.Create(root, info.Extraction);
            }
            else if (Config.Kind == ObjectiveKind.Checkpoint && info.GatePoints.Length > 1)
            {
                int n = Mathf.Max(1, Config.Gates);
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = info.GatePoints[i % info.GatePoints.Length];
                    Vector3 next = info.GatePoints[(i + 1) % info.GatePoints.Length];
                    Vector3 dir = next - at; dir.y = 0f;
                    float gateYaw = dir.sqrMagnitude > 0.01f ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg : 0f;
                    Gates.Add(CheckpointGate.Create(root, at, gateYaw, i));
                }
                Gates[0].SetState(1);
            }
            else if (Config.Kind == ObjectiveKind.LastStand)
            {
                Zone = ZoneRing.Create(root, info.Center, info.ZoneStart);
            }
            if (Config.AcidPools)
                foreach (var ap in info.AcidPoints) Acid.Add(AcidPool.Create(root, ap, 3.2f));

            // pickups
            PickupKind[] kinds = { PickupKind.Health, PickupKind.Nitro, PickupKind.Ammo, PickupKind.Shield, PickupKind.Health, PickupKind.Ammo, PickupKind.Nitro };
            for (int i = 0; i < info.PickupPoints.Length; i++)
                Pickups.Add(Pickup.Create(root, info.PickupPoints[i], kinds[i % kinds.Length]));

            // enemies
            if (Config.Kind == ObjectiveKind.Survival)
            {
                int c = Mathf.Max(1, Config.SurvivalConcurrent);
                for (int i = 0; i < c; i++) SpawnEnemy(NextSurvivalKind(), i);
            }
            else
            {
                foreach (var k in Config.Enemies) pending.Enqueue(k);
                int initial = Config.StartEnemies;
                for (int i = 0; i < initial && pending.Count > 0; i++) SpawnEnemy(pending.Dequeue(), i);
            }

            // wait until vehicle models are in (or a short timeout) so nothing pops in during the countdown
            wait = 0f;
            while (wait < 6f && (PlayerVisual == null || !PlayerVisual.Loaded)) { wait += Time.unscaledDeltaTime; yield return null; }

            // compile the shaders now (behind the loading screen) instead of stuttering on first use
            try { Shader.WarmupAllShaders(); } catch (Exception ex) { Debug.LogWarning("DERBY: shader warmup skipped. " + ex.Message); }

            Phase = MatchPhase.Countdown;
            Countdown = 3.2f;
            if (cam != null) cam.Intro(2.6f);
            AudioHub.Ambience(0.35f);
            Say(Config.Title, UiKit.Amber);
            if (Assist > 0) Say(Loc.T("KOLAYLAŞTIRILDI · " + (Assist + 1) + ". DENEME", "EASIER NOW · ATTEMPT " + (Assist + 1)), UiKit.Cyan);
        }

        EnemyKind NextSurvivalKind()
        {
            var list = Config.Enemies;
            if (list == null || list.Length == 0) return EnemyKind.Drifter;
            var k = list[survivalSpawnIndex % list.Length];
            survivalSpawnIndex++;
            return k == EnemyKind.WarRig ? EnemyKind.Hunter : k;
        }

        static VehicleSpec SpecOf(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.Rammer: return VehicleSpec.Rammer();
                case EnemyKind.Hunter: return VehicleSpec.Hunter();
                case EnemyKind.WarRig: return VehicleSpec.WarRig();
                default: return VehicleSpec.Drifter();
            }
        }

        static Vector3[] Shuffle(Vector3[] src, int seed)
        {
            var a = (Vector3[])src.Clone();
            var rng = new System.Random(seed * 31 + 7);
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = a[i]; a[i] = a[j]; a[j] = t;
            }
            return a;
        }

        GameObject SpawnVehicle(VehicleSpec spec, Vector3 pos, float yaw, int team)
        {
            var go = new GameObject(spec.DisplayName);
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            if (team == 0) go.tag = "Player";
            go.AddComponent<Rigidbody>();
            var car = go.AddComponent<ArcadeCar>();
            car.Configure(spec);
            var hp = go.AddComponent<VehicleHealth>();
            hp.Configure(spec.Health, team);
            var visual = go.AddComponent<VehicleVisual>();
            visual.Begin(spec, car);
            var guns = go.AddComponent<WeaponRig>();
            guns.Configure(spec, hp, visual, spec.RocketAmmo);
            guns.UseHeat = team == 0;
            return go;
        }

        Vector3 PickSpawn(out float yaw)
        {
            var info = Arena.Info;
            Vector3 best = info.EnemySpawns[0];
            float bestScore = float.MinValue;
            Vector3 pp = PlayerCar != null ? PlayerCar.transform.position : info.PlayerSpawn;
            for (int i = 0; i < info.EnemySpawns.Length; i++)
            {
                Vector3 s = info.EnemySpawns[i];
                float dPlayer = Vector3.Distance(s, pp);
                if (dPlayer < 14f) continue;
                float crowd = 0f;
                foreach (var e in Enemies) if (e != null && !e.Health.Dead) crowd += Mathf.Max(0f, 8f - Vector3.Distance(e.transform.position, s));
                float score = Mathf.Min(dPlayer, 40f) - crowd * 4f + UnityEngine.Random.value * 3f;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            Vector3 to = pp - best;
            yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            return best;
        }

        void SpawnEnemy(EnemyKind kind, int index)
        {
            var spec = SpecOf(kind);
            if (kind != EnemyKind.WarRig && Config.EliteChance > 0f && eliteRng != null && eliteRng.NextDouble() < Config.EliteChance)
                spec.MakeElite();
            spec.Health *= Config.EnemyHp;
            if (kind == EnemyKind.WarRig && Config.BossHealth > 0f) spec.Health = Config.BossHealth;
            spec.Health *= healthScale;
            spec.TopSpeed *= Config.EnemySpeed;
            spec.Accel *= Mathf.Lerp(1f, Config.EnemySpeed, 0.6f);
            float yaw;
            Vector3 pos;
            if (kind == EnemyKind.WarRig)
            {
                pos = Arena.Info.BossSpawn;
                Vector3 to = Arena.Info.PlayerSpawn - pos;
                yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            }
            else pos = PickSpawn(out yaw);

            var go = SpawnVehicle(spec, pos, yaw, 1);
            var car = go.GetComponent<ArcadeCar>();
            var hp = go.GetComponent<VehicleHealth>();
            var guns = go.GetComponent<WeaponRig>();
            guns.DamageMul = damageScale;
            hp.InvulnerableUntil = Time.time + 1.5f;
            var ai = go.AddComponent<EnemyDriver>();
            ai.Car = car;
            ai.Weapons = guns;
            ai.Health = hp;
            ai.Visual = go.GetComponent<VehicleVisual>();
            ai.Player = PlayerHealth;
            ai.Arena = Arena.Info;
            ai.AimError = Config.AimErrorDeg;
            ai.Aggression = Mathf.Lerp(0.8f, 1.15f, (Config.Number - 1) / 49f);
            ai.Chapter = Config.Chapter;
            ai.DamageScale = damageScale;
            if (Zone != null) { ai.ZoneCenter = Zone.transform.position; ai.ZoneRadius = Zone.Radius; }
            ai.Active = Phase == MatchPhase.Playing;
            car.Locked = Phase != MatchPhase.Playing;
            go.AddComponent<EngineAudio>().Volume = 0.22f;
            hp.Died += OnEnemyDied;
            Enemies.Add(ai);
            if (kind == EnemyKind.WarRig) Boss = hp;
            if (Phase == MatchPhase.Playing)
            {
                Fx.Burst(pos + Vector3.up, new Color(1f, 0.3f, 0.2f), 12);
                if (Config.Kind != ObjectiveKind.Survival || kind == EnemyKind.WarRig)
                    Say(kind == EnemyKind.WarRig ? Loc.T("PATRON GELDİ!", "THE BOSS IS HERE!") : Loc.T("YENİ RAKİP: ", "NEW RIVAL: ") + spec.DisplayName.ToUpperInvariant(), UiKit.Red);
            }
        }

        public int AliveEnemies()
        {
            int n = 0;
            foreach (var e in Enemies) if (e != null && e.Health != null && !e.Health.Dead) n++;
            return n;
        }

        public int TotalEnemies { get { return Config.Enemies.Length; } }

        void OnEnemyDied(VehicleHealth victim, VehicleHealth killer)
        {
            if (Phase != MatchPhase.Playing && Phase != MatchPhase.Countdown) return;
            int scrap = 0;
            var car = victim.GetComponent<ArcadeCar>();
            if (car != null && car.Spec != null) scrap = car.Spec.ScrapValue;
            if (killer == PlayerHealth || killer == null)
            {
                Kills++;
                Say(Loc.T("İMHA! +", "DESTROYED! +") + scrap, UiKit.Amber);
                Haptics(false);
            }
            StartCoroutine(Sink(victim.gameObject, 9f));

            if (victim == Boss)
            {
                Win();
                return;
            }
            if ((Config.Kind == ObjectiveKind.Demolition || Config.Kind == ObjectiveKind.LastStand) && pending.Count == 0 && AliveEnemies() == 0)
            {
                Win();
                return;
            }
            if (Config.Kind == ObjectiveKind.Survival)
            {
                StartCoroutine(SurvivalRespawn(5f));
                return;
            }
            if (Config.Kind == ObjectiveKind.LastStand && AliveEnemies() > 0)
                Say(AliveEnemies() + Loc.T(" RAKİP KALDI", " RIVALS LEFT"), UiKit.Amber);
            if (pending.Count > 0 && Config.Kind != ObjectiveKind.Reactor)
                StartCoroutine(DelayedSpawn(1.5f));
        }

        IEnumerator SurvivalRespawn(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Phase == MatchPhase.Playing && AliveEnemies() < Mathf.Max(1, Config.SurvivalConcurrent))
                SpawnEnemy(NextSurvivalKind(), Enemies.Count);
        }

        IEnumerator DelayedSpawn(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Phase == MatchPhase.Playing && pending.Count > 0) SpawnEnemy(pending.Dequeue(), Enemies.Count);
        }

        IEnumerator Sink(GameObject wreck, float after)
        {
            yield return new WaitForSeconds(after);
            if (wreck == null) yield break;
            var body = wreck.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;
            foreach (var c in wreck.GetComponentsInChildren<Collider>()) c.enabled = false;
            float t = 0f;
            Vector3 start = wreck.transform.position;
            while (t < 2.5f && wreck != null)
            {
                t += Time.deltaTime;
                wreck.transform.position = start + Vector3.down * (t * 1.2f);
                yield return null;
            }
            if (wreck != null) Destroy(wreck);
        }

        // ------------------------------------------------------------------ second chance (rewarded ad)
        /// <summary>
        /// Health hit zero: freeze the match and offer one rewarded "second chance" per match when an ad is ready.
        /// Returns false (normal death) in the countdown, after a revive, or when no ad can be shown.
        /// </summary>
        bool OfferSecondChance(VehicleHealth victim, VehicleHealth attacker)
        {
            if (Phase != MatchPhase.Playing || ReviveUsed) return false;
            var ads = DerbyArena.Ads.AdsManager.Instance;
            if (ads == null || !ads.RewardedReady) return false;
            ReviveUsed = true;
            Phase = MatchPhase.Downed;
            ReviveTimeLeft = DerbyArena.Ads.AdsConfig.ReviveWindowSeconds;
            FreezeAll();
            Time.timeScale = 0f;
            AudioHub.Play2D(SynthAudio.Alarm(), 0.5f, 0.8f);
            HudController.ShowSecondChance(this);
            return true;
        }

        /// <summary>Player accepted: play the rewarded ad, revive on reward, otherwise the car dies.</summary>
        public void AcceptSecondChance()
        {
            if (Phase != MatchPhase.Downed) return;
            ReviveTimeLeft = 999f;   // the countdown stops while the ad plays
            var ads = DerbyArena.Ads.AdsManager.Instance;
            if (ads == null) { DeclineSecondChance(); return; }
            ads.ShowRewarded(earned =>
            {
                if (this == null) return;
                if (earned) Revive();
                else DeclineSecondChance();
            });
        }

        void Revive()
        {
            if (Phase != MatchPhase.Downed) return;
            Phase = MatchPhase.Playing;
            Time.timeScale = 1f;
            if (PlayerHealth != null) PlayerHealth.Revive(DerbyArena.Ads.AdsConfig.ReviveHealthFraction, 40f, 3f);
            if (PlayerCar != null)
            {
                PlayerCar.Locked = false;
                PlayerCar.Nitro = 1f;
            }
            foreach (var e in Enemies)
            {
                if (e == null || e.Health == null || e.Health.Dead) continue;
                e.Active = true;
                if (e.Car != null) e.Car.Locked = false;
                // shove nearby rivals away so the player is not wrecked again in the same second
                Vector3 d = e.transform.position - PlayerCar.transform.position; d.y = 0f;
                if (d.magnitude < 9f && e.Car != null && e.Car.Body != null)
                    e.Car.Body.AddForce((d.normalized * 7f + Vector3.up * 1.5f) * e.Car.Body.mass, ForceMode.Impulse);
            }
            Say(Loc.T("İKİNCİ ŞANS!", "SECOND CHANCE!"), UiKit.Green);
            AudioHub.Play2D(SynthAudio.Go(), 0.7f, 1.1f);
        }

        /// <summary>Player declined, the offer timed out, or the ad gave no reward: the deferred death happens.</summary>
        public void DeclineSecondChance()
        {
            if (Phase != MatchPhase.Downed) return;
            Phase = MatchPhase.Playing;
            Time.timeScale = 1f;
            if (PlayerHealth != null) PlayerHealth.Kill();
            else Lose();
        }

        void OnPlayerDied(VehicleHealth victim, VehicleHealth killer)
        {
            if (Phase != MatchPhase.Playing) return;
            LoseReason = killer != null && killer.GetComponent<ArcadeCar>() != null
                ? killer.GetComponent<ArcadeCar>().Spec.DisplayName + Loc.T(" seni imha etti", " destroyed you")
                : Loc.T("Aracın imha edildi", "Your car was destroyed");
            Lose();
            Haptics(true);
        }

        void Win()
        {
            if (Phase != MatchPhase.Playing) return;
            Phase = MatchPhase.Won;
            endTimer = 1.6f;
            Stars = 1;
            if (Config.Kind == ObjectiveKind.Survival ? Kills >= Config.KillTarget : Elapsed <= Config.ParTime) Stars++;
            if (PlayerHealth != null && PlayerHealth.Fraction >= 0.5f) Stars++;
            Save.Data.Fails[Config.Number] = 0;
            int prevStars = Save.Data.Stars[Config.Number];
            Reward = Config.Reward + Kills * 15;
            if (prevStars > 0) Reward = Mathf.RoundToInt(Reward * 0.45f);
            Save.Data.Stars[Config.Number] = Mathf.Max(prevStars, Stars);
            if (Config.Number < Campaign.Total && Save.Data.Unlocked <= Config.Number)
            {
                Save.Data.Unlocked = Config.Number + 1;
                NewUnlock = true;
            }
            Save.Data.Scrap += Reward;
            Save.Data.TotalKills += Kills;
            Save.Data.Wins++;
            Save.Write();
            FreezeAll();
            AudioHub.Play2D(SynthAudio.Win(), 0.8f, 1f);
            Crowd.Cheer(1f);
            Say(Loc.T("GÖREV TAMAM", "MISSION COMPLETE"), UiKit.Green);
        }

        void Lose()
        {
            if (Phase != MatchPhase.Playing) return;
            Phase = MatchPhase.Lost;
            endTimer = 2.2f;
            Reward = Kills * 10;
            Save.Data.Fails[Config.Number] = Mathf.Min(9, Save.Data.Fails[Config.Number] + 1);
            Save.Data.Scrap += Reward;
            Save.Data.TotalKills += Kills;
            Save.Write();
            FreezeAll();
            AudioHub.Play2D(SynthAudio.Lose(), 0.7f, 1f);
        }

        /// <summary>Rewarded "double scrap" on the result screen: the match reward is granted a second time.</summary>
        public void DoubleReward()
        {
            if (RewardDoubled || Reward <= 0) return;
            RewardDoubled = true;
            Save.Data.Scrap += Reward;
            Save.Write();
            AudioHub.Play2D(SynthAudio.Pickup(), 0.8f, 1f);
        }

        void FreezeAll()
        {
            foreach (var e in Enemies) if (e != null) e.Active = false;
            if (PlayerCar != null) PlayerCar.Locked = true;
        }

        static void Haptics(bool strong)
        {
            if (!Save.Data.Haptics) return;
#if UNITY_ANDROID || UNITY_IOS
            if (strong) Handheld.Vibrate();
#endif
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (Phase)
            {
                case MatchPhase.Countdown: TickCountdown(dt); break;
                case MatchPhase.Playing: TickPlaying(dt); break;
                case MatchPhase.Downed:
                    ReviveTimeLeft -= Time.unscaledDeltaTime;
                    if (ReviveTimeLeft <= 0f) DeclineSecondChance();
                    break;
                case MatchPhase.Won:
                case MatchPhase.Lost:
                    endTimer -= Time.unscaledDeltaTime;
                    if (endTimer <= 0f && !resultsSent)
                    {
                        resultsSent = true;
                        HudController.ShowResults(this);
                    }
                    break;
            }
        }

        void TickCountdown(float dt)
        {
            Countdown -= dt;
            int sec = Mathf.CeilToInt(Countdown);
            if (sec != beepSecond && sec > 0 && sec <= 3)
            {
                beepSecond = sec;
                AudioHub.Play2D(SynthAudio.Beep(), 0.6f, 1f);
            }
            if (Countdown <= 0f)
            {
                Phase = MatchPhase.Playing;
                AudioHub.Play2D(SynthAudio.Go(), 0.7f, 1f);
                PlayerCar.Locked = false;
                PlayerHealth.InvulnerableUntil = Time.time + 2.5f;
                foreach (var e in Enemies)
                {
                    if (e == null) continue;
                    e.Active = true;
                    e.Car.Locked = false;
                }
                Say(Config.ObjectiveLine, Color.white);
            }
        }

        void TickPlaying(float dt)
        {
            Elapsed += dt;
            TimeLeft -= dt;
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                if (Config.Kind == ObjectiveKind.Survival && PlayerHealth != null && !PlayerHealth.Dead)
                {
                    Win();
                    return;
                }
                LoseReason = Loc.T("Süre doldu", "Time's up");
                Lose();
                return;
            }
            if (TimeLeft < 10.5f && Mathf.CeilToInt(TimeLeft) != beepSecond)
            {
                beepSecond = Mathf.CeilToInt(TimeLeft);
                AudioHub.Play2D(SynthAudio.Beep(), 0.35f, 1.3f);
            }
            if (PlayerCar == null || PlayerHealth == null || PlayerHealth.Dead) return;
            Vector3 pp = PlayerCar.transform.position;

            // out of bounds / fell through
            if (pp.y < -6f || !Arena.Info.Inside(pp))
            {
                Vector3 safe = Arena.Info.ClosestSafePoint(pp);
                PlayerCar.Teleport(safe, PlayerCar.transform.eulerAngles.y);
                Say(Loc.T("Arenaya geri döndün", "Back in the arena"), UiKit.TextDim);
            }
            foreach (var e in Enemies)
            {
                if (e == null || e.Health.Dead) continue;
                Vector3 ep = e.transform.position;
                if (ep.y < -6f || !Arena.Info.Inside(ep))
                {
                    float y;
                    e.Car.Teleport(PickSpawn(out y), y);
                }
            }

            // pickups
            foreach (var p in Pickups)
            {
                if (p == null || !p.Available) continue;
                if ((p.transform.position - pp).sqrMagnitude < 2.8f * 2.8f && p.TryCollect(PlayerCar, PlayerHealth, PlayerWeapons))
                    Say(PickupText(p.Kind), Pickup.ColorOf(p.Kind));
            }

            if (Config.Kind == ObjectiveKind.Reactor) TickReactor(pp, dt);
            else if (Config.Kind == ObjectiveKind.Checkpoint) TickCheckpoint(pp);
            if (Zone != null) TickZone(dt);
            TickHazards(pp, dt);
        }

        void TickCheckpoint(Vector3 pp)
        {
            if (GatesPassed >= Gates.Count) return;
            var g = Gates[GatesPassed];
            if (g == null || !g.Reached(pp)) return;
            g.Pass();
            GatesPassed++;
            if (GatesPassed >= Gates.Count)
            {
                Win();
                return;
            }
            TimeLeft += 5f;
            Gates[GatesPassed].SetState(1);
            Say(Loc.T("KAPI ", "GATE ") + GatesPassed + "/" + Gates.Count + Loc.T("  +5 sn", "  +5 s"), UiKit.Amber);
        }

        void TickZone(float dt)
        {
            float t = Mathf.Clamp01((Elapsed - ZoneDelay) / ZoneShrinkTime);
            var info = Arena.Info;
            float r = Mathf.Lerp(info.ZoneStart, info.ZoneEnd, t * t * (3f - 2f * t));
            if (Mathf.Abs(r - Zone.Radius) > 0.01f) Zone.SetRadius(r);
            if (Elapsed > ZoneDelay - 5f && Elapsed - dt <= ZoneDelay - 5f) Say(Loc.T("BÖLGE 5 SN İÇİNDE DARALIYOR", "ZONE SHRINKS IN 5 S"), UiKit.Red);
            PlayerOutsideZone = PlayerCar != null && Zone.Outside(PlayerCar.transform.position);
            foreach (var e in Enemies)
            {
                if (e == null) continue;
                e.ZoneCenter = Zone.transform.position;
                e.ZoneRadius = r;
            }
        }

        void TickHazards(Vector3 pp, float dt)
        {
            if (Zone == null && Acid.Count == 0) return;
            hazardTick -= dt;
            if (hazardTick > 0f) return;
            hazardTick = 0.5f;
            HazardDamage(PlayerHealth);
            foreach (var e in Enemies) if (e != null) HazardDamage(e.Health);
            if (PlayerOutsideZone) Say(Loc.T("BÖLGE DIŞINDASIN!", "OUTSIDE THE ZONE!"), UiKit.Red);
        }

        void HazardDamage(VehicleHealth h)
        {
            if (h == null || h.Dead) return;
            Vector3 p = h.transform.position;
            float dmg = 0f;
            if (Zone != null && Elapsed > ZoneDelay && Zone.Outside(p)) dmg += 4f;
            for (int i = 0; i < Acid.Count; i++)
                if (Acid[i] != null && Acid[i].Contains(p)) { dmg += 3f; break; }
            if (dmg > 0f) h.Apply(dmg, null, p + Vector3.up);
        }

        static string PickupText(PickupKind k)
        {
            switch (k)
            {
                case PickupKind.Health: return Loc.T("+ ONARIM", "+ REPAIR");
                case PickupKind.Nitro: return Loc.T("+ NİTRO", "+ NITRO");
                case PickupKind.Ammo: return Loc.T("+3 ROKET", "+3 ROCKETS");
                default: return Loc.T("+ KALKAN", "+ SHIELD");
            }
        }

        void TickReactor(Vector3 pp, float dt)
        {
            if (!ExtractionOpen)
            {
                foreach (var c in Cells)
                {
                    if (c == null || c.Taken) continue;
                    Vector3 d = c.transform.position - pp;
                    d.y = 0f;
                    if (d.magnitude < 3.4f)
                    {
                        c.Collect();
                        CellsTaken++;
                        TimeLeft += 8f;
                        if (CellsTaken >= Config.Cells)
                        {
                            ExtractionOpen = true;
                            Extraction.gameObject.SetActive(true);
                            Say(Loc.T("TAHLİYE AÇILDI! Yeşil bölgeye git", "EXTRACTION OPEN! Head to the green zone"), UiKit.Green);
                            AudioHub.Play2D(SynthAudio.Alarm(), 0.6f, 1f);
                            while (pending.Count > 0) SpawnEnemy(pending.Dequeue(), Enemies.Count);
                        }
                        else
                        {
                            Say(Loc.T("HÜCRE ", "CELL ") + CellsTaken + "/" + Config.Cells + Loc.T("  +8 sn", "  +8 s"), UiKit.Cyan);
                            if (pending.Count > 0) SpawnEnemy(pending.Dequeue(), Enemies.Count);
                        }
                    }
                }
            }
            else
            {
                Vector3 d = Extraction.transform.position - pp;
                d.y = 0f;
                bool inside = d.magnitude < ExtractionPad.Radius;
                if (inside) ExtractHold += dt;
                else ExtractHold = Mathf.Max(0f, ExtractHold - dt * 0.6f);
                Extraction.SetProgress(ExtractHold / ExtractRequired);
                if (ExtractHold >= ExtractRequired) Win();
            }
        }

        /// <summary>Next objective position for the HUD arrow (or null).</summary>
        public bool ObjectivePoint(out Vector3 p)
        {
            p = Vector3.zero;
            if (Config.Kind == ObjectiveKind.Reactor)
            {
                if (ExtractionOpen && Extraction != null) { p = Extraction.transform.position; return true; }
                float best = float.MaxValue;
                bool any = false;
                Vector3 pp = PlayerCar != null ? PlayerCar.transform.position : Vector3.zero;
                foreach (var c in Cells)
                {
                    if (c == null || c.Taken) continue;
                    float d = (c.transform.position - pp).sqrMagnitude;
                    if (d < best) { best = d; p = c.transform.position; any = true; }
                }
                return any;
            }
            if (Config.Kind == ObjectiveKind.Checkpoint)
            {
                if (GatesPassed < Gates.Count && Gates[GatesPassed] != null) { p = Gates[GatesPassed].transform.position; return true; }
                return false;
            }
            if (Zone != null && PlayerOutsideZone) { p = Zone.transform.position; return true; }
            if (Boss != null && !Boss.Dead) { p = Boss.transform.position; return true; }
            if ((Config.Kind == ObjectiveKind.Demolition || Config.Kind == ObjectiveKind.LastStand) && AliveEnemies() <= 2 && pending.Count == 0 && PlayerCar != null)
            {
                float bestE = float.MaxValue;
                bool found = false;
                Vector3 me = PlayerCar.transform.position;
                foreach (var e in Enemies)
                {
                    if (e == null || e.Health == null || e.Health.Dead) continue;
                    float d2 = (e.transform.position - me).sqrMagnitude;
                    if (d2 < bestE) { bestE = d2; p = e.transform.position; found = true; }
                }
                return found;
            }
            return false;
        }
    }

    /// <summary>Feeds touch input into the player car and weapons (auto-aim, rockets).</summary>
    public sealed class PlayerDriver : MonoBehaviour
    {
        MatchDirector match;
        TouchControls input;
        ArcadeCar car;
        WeaponRig guns;
        VehicleVisual visual;
        VehicleHealth hp;
        public VehicleHealth LockTarget { get; private set; }

        public void Init(MatchDirector m, TouchControls t)
        {
            match = m;
            input = t;
            car = GetComponent<ArcadeCar>();
            guns = GetComponent<WeaponRig>();
            visual = GetComponent<VehicleVisual>();
            hp = GetComponent<VehicleHealth>();
        }

        void Update()
        {
            if (input == null || car == null) return;
            if (hp.Dead || match.Phase != MatchPhase.Playing)
            {
                car.Throttle = 0f; car.Brake = 0f; car.Steer = 0f; car.NitroHeld = false; car.Handbrake = false;
                return;
            }
            car.Steer = input.Steer;
            car.Throttle = input.Throttle;
            car.Brake = input.Brake;
            car.Handbrake = input.Handbrake;
            car.NitroHeld = input.Nitro;

            LockTarget = FindTarget();
            Vector3 aim;
            if (LockTarget != null)
            {
                aim = LockTarget.transform.position + Vector3.up * 0.9f;
                var b = LockTarget.GetComponent<Rigidbody>();
                if (b != null) aim += b.linearVelocity * 0.06f;
            }
            else aim = transform.position + transform.forward * 30f + Vector3.up * 1.1f;
            if (visual != null) visual.AimAt(aim, Time.deltaTime);

            if (input.Fire) guns.TryFireGun(aim);
            if (input.RocketPressed)
            {
                if (!guns.TryFireRocket(aim, LockTarget != null ? LockTarget.transform : null, 0.7f) && guns.Rockets <= 0)
                    DerbyArena.Audio.AudioHub.Play2D(DerbyArena.Audio.SynthAudio.Overheat(), 0.4f, 1.4f);
            }
        }

        VehicleHealth FindTarget()
        {
            float range = guns != null && guns.Spec != null ? guns.Spec.GunRange : 40f;
            float cone = Save.Data.AutoAim ? 0.55f : 0.93f;      // cos of half-angle
            VehicleHealth best = null;
            float bestScore = float.MaxValue;
            Vector3 me = transform.position;
            Vector3 fwd = transform.forward;
            foreach (var e in match.Enemies)
            {
                if (e == null || e.Health == null || e.Health.Dead) continue;
                Vector3 d = e.transform.position - me;
                float dist = d.magnitude;
                if (dist > range || dist < 0.5f) continue;
                float dot = Vector3.Dot(fwd, d / dist);
                if (dot < cone) continue;
                float score = dist * (2f - dot);
                if (e.Health == LockTarget) score *= 0.75f;   // sticky lock
                if (score < bestScore) { bestScore = score; best = e.Health; }
            }
            return best;
        }
    }
}
