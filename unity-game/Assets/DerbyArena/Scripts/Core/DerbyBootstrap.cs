using System;
using DerbyArena.Audio;
using DerbyArena.CameraRig;
using DerbyArena.Combat;
using DerbyArena.Mission;
using DerbyArena.UI;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>
    /// Single entry point (attached in BlackoutSpillway.unity). Builds either the main menu over a live
    /// arena, or a match for GameFlow.PendingLevel. Camera first, so a later failure can never leave a
    /// black screen; every stage is isolated and reports to the diagnostics overlay.
    /// </summary>
    public sealed class DerbyBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Save.Load();
            Loc.AutoDetect();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Physics.defaultSolverIterations = 8;
            Time.fixedDeltaTime = 1f / 60f;
            Save.AutoQuality();
            DerbyBootstrapQuality.Apply();
            try { DerbyArena.Ads.AdsManager.Ensure(); }
            catch (Exception ex) { Debug.LogWarning("DERBY: ads unavailable. " + ex.Message); }

            var cam = EnsureCamera();
            var sceneRoot = new GameObject("Scene").transform;
            AudioHub.Init(sceneRoot, Save.Data.Sound);

            int level = GameFlow.PendingLevel;
            if (level <= 0)
            {
                try { IntroSplash.Play(); }   // cold start only; the menu builds behind it
                catch (Exception ex) { Debug.LogWarning("DERBY: intro skipped. " + ex.Message); }
                StartCoroutine(BootMenu(cam));
            }
            else BuildMatch(level, cam, sceneRoot);
        }

        /// <summary>
        /// 4.0 menu boot: the heavy steps (arena, shader warm-up, vehicle model, UI) run one per frame and report
        /// progress to Boot, so the cinematic intro keeps animating and its bar shows real work. On a warm start
        /// (no intro) the same steps run in two or three frames.
        /// </summary>
        System.Collections.IEnumerator BootMenu(Camera cam)
        {
            Boot.Reset();
            Boot.Set(0.05f, Loc.T("ARENA KURULUYOR", "BUILDING THE ARENA"));
            yield return null;
            int lvl = Mathf.Clamp(Save.Data.LastPlayed, 1, Campaign.Total);
            var cfg = Campaign.Get(lvl);
            ArenaLoader arena = null;
            try { arena = ArenaLoader.Build(cfg.Arena, cfg.Theme, cam, Save.Data.Quality); }
            catch (Exception ex) { Debug.LogError("DERBY: menu arena failed. " + ex); }
            AttachPost(cam, cfg.Theme);
            Boot.Set(0.35f, Loc.T("IŞIKLAR VE GÖLGELER", "LIGHTS AND SHADOWS"));
            yield return null;

            // shader warm-up once per session: the first frame that uses a Standard variant otherwise hitches
            if (!Boot.ShadersWarm)
            {
                Boot.ShadersWarm = true;
                float t0 = Time.realtimeSinceStartup;
                try { Shader.WarmupAllShaders(); } catch (Exception ex) { Debug.LogWarning("DERBY: shader warm-up skipped. " + ex.Message); }
                DerbyStartupDiagnostics.Report("Shader warm-up", Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
            }
            Boot.Set(0.5f, Loc.T("ARAÇ HAZIRLANIYOR", "PREPARING THE VEHICLE"));
            yield return null;

            Showcase sc = null;
            Vector3 pos = arena != null ? arena.Info.PlayerSpawn + new Vector3(0f, -0.55f, 8f) : Vector3.zero;
            try
            {
                var show = new GameObject("Showcase");
                show.transform.position = pos;
                sc = show.AddComponent<Showcase>();
                sc.Rebuild();
                var chase = cam.GetComponent<ChaseCamera>();
                chase.Orbit = true;
                chase.OrbitCenter = pos;
                chase.OrbitRadius = 9.5f;
                chase.OrbitHeight = cfg.Arena == ArenaKind.Docks ? 4.2f : 2.6f;   // docks: clear the 2.6 m cover container
                cam.transform.position = pos + new Vector3(4f, 3f, -9f);
                cam.transform.LookAt(pos + Vector3.up);
                if (IntroSplash.Active) chase.Dolly(60f);                           // parked far and high behind the intro; its fade-out starts the real 3.2 s dolly
            }
            catch (Exception ex) { Debug.LogError("DERBY: showcase failed. " + ex); }

            // wait for the vehicle model and the arena details (bounded: a missing file must never hold the menu)
            float wait = 0f;
            while (wait < 8f && ((sc != null && !sc.Loaded) || (arena != null && !arena.Ready)))
            {
                wait += Time.unscaledDeltaTime;
                Boot.Set(Mathf.Lerp(0.5f, 0.85f, Mathf.Clamp01(wait / 3f)), arena != null && !arena.Ready ? Loc.T("ARENA DETAYLARI", "ARENA DETAILS") : Loc.T("ARAÇ HAZIRLANIYOR", "PREPARING THE VEHICLE"));
                yield return null;
            }
            Boot.Set(0.9f, Loc.T("MENÜ", "MENU"));
            yield return null;
            try { MenuController.Create(GameFlow.MenuPage); }
            catch (Exception ex) { Debug.LogError("DERBY: menu failed. " + ex); }
            AudioHub.Ambience(0.4f, arena != null && arena.Info != null && arena.Info.Kind == ArenaKind.Ruins ? SynthAudio.Wind() : SynthAudio.Ambience());
            Boot.Set(1f, Loc.T("HAZIR", "READY"));
        }

        Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null) cam = new GameObject("MainCamera").AddComponent<Camera>();
            var go = cam.gameObject;
            go.tag = "MainCamera";
            cam.enabled = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.085f, 0.1f);
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 60f;
            cam.allowHDR = false;
            cam.allowMSAA = Save.Data.Quality >= 2;
            cam.useOcclusionCulling = false;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 8f, -40f), Quaternion.Euler(10f, 0f, 0f));
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            if (go.GetComponent<ChaseCamera>() == null) go.AddComponent<ChaseCamera>();
            return cam;
        }

        static void AttachPost(Camera cam, int theme)
        {
            try { PostFx.Attach(cam, Save.Data.Quality, Atmosphere.Get(theme)); }
            catch (Exception ex) { Debug.LogError("DERBY: post-processing disabled. " + ex); }
        }

        void BuildMatch(int level, Camera cam, Transform sceneRoot)
        {
            var cfg = Campaign.Get(level);
            Fx.Init(sceneRoot, Save.Data.Quality);
            Projectiles.Init(sceneRoot);
            ArenaLoader arena = null;
            try { arena = ArenaLoader.Build(cfg.Arena, cfg.Theme, cam, Save.Data.Quality); }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: arena failed, falling back to spillway shell. " + ex);
                arena = ArenaLoader.Build(ArenaKind.Spillway, 0, cam, 0);
            }

            AttachPost(cam, arena != null && arena.Info != null && arena.Info.Kind == cfg.Arena ? cfg.Theme : 0);
            Fx.FollowEmbers(cam.transform);

            var controls = gameObject.AddComponent<TouchControls>();
            gameObject.AddComponent<AdaptivePerformance>();
            gameObject.AddComponent<PerfLog>();
            AudioHub.Ambience(0.35f, cfg.Arena == ArenaKind.Ruins ? SynthAudio.Wind() : SynthAudio.Ambience());
            AudioHub.Reverb(cfg.Arena);
            var match = MatchDirector.Begin(cfg, arena, controls, sceneRoot);
            try { HudController.Create(match, controls); }
            catch (Exception ex) { Debug.LogError("DERBY: HUD failed. " + ex); }
        }
    }

    /// <summary>Menu boot progress shared with the cinematic intro (4.0).</summary>
    public static class Boot
    {
        public static float Progress { get; private set; }
        public static string Status { get; private set; } = "";
        public static bool ShadersWarm;
        public static void Reset() { Progress = 0f; Status = ""; }
        public static void Set(float p, string status) { Progress = Mathf.Clamp01(p); Status = status ?? ""; }
    }

    /// <summary>Applies the quality preset (shadows, pixel lights, frame rate).</summary>
    public static class DerbyBootstrapQuality
    {
        public static void Apply()
        {
            int q = Save.Data.Quality;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Save.Data.FrameRate;
            // 0 low, 1 medium, 2 high, 3 ultra (2.1: sharper shadows, 4x MSAA, 1440p, more lights)
            QualitySettings.shadows = q == 0 ? ShadowQuality.Disable : (q >= 2 ? ShadowQuality.All : ShadowQuality.HardOnly);
            QualitySettings.shadowResolution = q >= 3 ? ShadowResolution.VeryHigh : (q == 2 ? ShadowResolution.High : ShadowResolution.Medium);
            QualitySettings.shadowDistance = q == 0 ? 0f : (q >= 3 ? 75f : (q == 2 ? 60f : 45f));
            QualitySettings.shadowCascades = q >= 2 ? 2 : 1;
            QualitySettings.pixelLightCount = q == 0 ? 1 : (q >= 3 ? 6 : (q == 2 ? 4 : 2));
            QualitySettings.antiAliasing = q >= 3 ? 4 : (q == 2 ? 2 : 0);
            QualitySettings.anisotropicFiltering = q == 0 ? AnisotropicFiltering.Disable : (q >= 3 ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable);
            QualitySettings.lodBias = q == 0 ? 0.7f : (q >= 3 ? 1.5f : 1f);
            // 2.1: a phone that already had to drop shadows this session keeps them off in every later match
            if (Save.Data.DynamicResolution && AdaptivePerformance.Step >= 1) QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = true;
            CapResolution(q);
        }

        static int nativeW, nativeH;
        /// <summary>Extra render scale set by AdaptivePerformance when the phone cannot hold the frame rate (1 = off).</summary>
        public static float DynamicScale = 1f;

        /// <summary>
        /// Renders at most 1080p (high), 900p (medium) or 720p (low) on the short side. 1440p+ phones were
        /// rendering 2-3x more pixels than needed, which cost far more frame rate than any effect.
        /// </summary>
        static void CapResolution(int q)
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                if (nativeW <= 0)
                {
                    nativeW = Display.main.systemWidth;
                    nativeH = Display.main.systemHeight;
                }
                int longSide = Mathf.Max(nativeW, nativeH);
                int shortSide = Mathf.Min(nativeW, nativeH);
                if (shortSide <= 0) return;
                int cap = q == 0 ? 720 : (q == 1 ? 900 : (q == 2 ? 1080 : 1440));
                float scale = (shortSide > cap ? (float)cap / shortSide : 1f) * (Save.Data.DynamicResolution ? DynamicScale : 1f);
                int w = Mathf.RoundToInt(longSide * scale);
                int h = Mathf.RoundToInt(shortSide * scale);
                if (Screen.width != w || Screen.height != h) Screen.SetResolution(w, h, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DERBY: resolution cap skipped. " + ex.Message);
            }
#endif
        }
    }

    /// <summary>
    /// Menu showcase: the player's vehicle slowly turning on a glowing pad. 4.0: rebuilt on demand so the garage
    /// shows the selected car, a previewed car, and every equipped or previewed cosmetic (rims, kit parts, neon).
    /// </summary>
    public sealed class Showcase : MonoBehaviour
    {
        public static Showcase Instance;
        /// <summary>The current vehicle model is in (or failed), so the boot can continue.</summary>
        public bool Loaded { get; private set; }
        Transform visual, pad;
        int build;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            pad = MeshFactory.Make("Pad", MeshFactory.UnitRing(), Mats.Glow(new Color(1f, 0.55f, 0.12f), 2.4f), transform, new Vector3(0f, 0.04f, 0f), new Vector3(3.8f, 1f, 3.8f)).transform;
            var l = new GameObject("KeyLight").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = new Vector3(3f, 4f, -3f);
            l.type = LightType.Point;
            l.range = 14f;
            l.intensity = 2.2f;
            l.color = new Color(1f, 0.85f, 0.7f);
            if (Save.Data.Quality > 0) Embers();
        }

        /// <summary>Slow embers rising around the pad (menu atmosphere, 4.0).</summary>
        void Embers()
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new Color(1f, 0.6f, 0.2f, 0.9f);
            var em = ps.emission; em.rateOverTime = 9f;
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 2.4f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.2f); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var mat = Mats.FxAdditive();
            if (mat != null) r.sharedMaterial = mat;
            ps.Play();
        }

        /// <summary>Spawns the current player vehicle (selection, preview and garage look included).</summary>
        public void Rebuild()
        {
            int id = ++build;
            Loaded = false;
            if (visual != null) Destroy(visual.gameObject);
            foreach (Transform t in transform) if (t.name == "BattleKit") Destroy(t.gameObject);
            var holder = new GameObject("Visual");
            holder.transform.SetParent(transform, false);
            holder.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visual = holder.transform;
            DerbyArena.Vehicles.VehicleSpec spec;
            try { spec = DerbyArena.Vehicles.VehicleSpec.ForPlayer(); }
            catch (Exception ex) { Debug.LogWarning("DERBY: showcase spec: " + ex.Message); spec = DerbyArena.Vehicles.VehicleSpec.Interceptor(); }
            holder.transform.localScale = Vector3.one * spec.Scale;
            if (pad != null) pad.localScale = new Vector3(3.8f, 1f, 3.8f) * (spec.Tank ? 1.6f : Mathf.Max(1f, spec.Scale));
            var snapshot = spec;
            GlbLibrary.Spawn(spec.GlbFile, spec.ModelOptions(), holder.transform, (model, err) =>
            {
                if (this == null) return;
                if (id == build) Loaded = true;
                if (model == null || id != build) { if (model != null) Destroy(model); return; }
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.gameObject.layer = DerbyArena.Vehicles.VehicleVisual.VehicleLayer;
                try { DerbyArena.Vehicles.VehicleVisual.DressRims(transform, snapshot); } catch (Exception ex) { Debug.LogWarning("DERBY: showcase rims: " + ex.Message); }
                try { DerbyArena.Vehicles.VehicleKit.Build(transform, snapshot, null); } catch (Exception ex) { Debug.LogWarning("DERBY: showcase kit: " + ex.Message); }
                try { DerbyArena.Vehicles.VehicleVisual.AnimateIdle(model.transform, snapshot); } catch (Exception) { }
            });
        }

        void Update() { transform.Rotate(0f, 14f * Time.unscaledDeltaTime, 0f, Space.World); }
    }
}
