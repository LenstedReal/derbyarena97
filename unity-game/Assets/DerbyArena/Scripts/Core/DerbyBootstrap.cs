using System;
using DerbyArena.AI;
using DerbyArena.Audio;
using DerbyArena.CameraRig;
using DerbyArena.Combat;
using DerbyArena.Mission;
using DerbyArena.UI;
using DerbyArena.Vehicles;
using DerbyArena.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DerbyArena.Core
{
    public sealed class DerbyBootstrap : MonoBehaviour
    {
        MissionDirector mission;
        HudController hud;
        int spawnedCells;
        ChaseCamera chase;

        void Awake()
        {
            Time.timeScale = 1f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            bool shadowsOk = SystemInfo.systemMemorySize >= 3500;
            QualitySettings.shadowDistance = shadowsOk ? 42f : 0f;
            QualitySettings.shadows = shadowsOk ? ShadowQuality.HardOnly : ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowCascades = 2;
            QualitySettings.pixelLightCount = 4;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

            // Critical: establish a working camera before creating materials, geometry,
            // mission markers or vehicles. Any later failure can no longer leave a
            // camera-less black screen.
            EnsurePrimaryCamera();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.07f, 0.075f, 0.08f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.125f, 0.14f);
            RenderSettings.skybox = null;

            var world = new GameObject("BlackoutSpillway").transform;
            try
            {
                SpillwayBuilder.Build(world);
            }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: arena generation failed; creating emergency arena. " + ex);
                BuildEmergencyArena(world);
            }

            try
            {
                mission = gameObject.AddComponent<MissionDirector>();
                mission.BuildWorldMarkers(world);
            }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: mission marker setup failed; core arena will remain playable. " + ex);
                if (mission == null) mission = GetComponent<MissionDirector>();
            }

            GameObject player = null;
            try
            {
                player = Spawn(VehicleSpec.Interceptor(), new Vector3(0f, 1.1f, -24f), Quaternion.identity, true);
            }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: interceptor setup failed; creating emergency player vehicle. " + ex);
            }

            if (player == null)
                player = SpawnEmergencyVehicle();

            if (player != null)
            {
                if (mission != null) mission.PlayerHealth = player.GetComponent<VehicleHealth>();
                if (chase != null)
                {
                    chase.Target = player.transform;
                    chase.Drive = player.GetComponent<WheelDrive>();
                }
            }

            TouchControls touch = null;
            try
            {
                touch = gameObject.AddComponent<TouchControls>();
                if (player != null && player.GetComponent<WheelDrive>() != null)
                {
                    var playerDrive = player.AddComponent<PlayerDrive>();
                    playerDrive.Drive = player.GetComponent<WheelDrive>();
                    playerDrive.Weapons = player.GetComponent<WeaponRig>();
                    playerDrive.Controls = touch;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: touch-control setup failed. " + ex);
            }

            try
            {
                hud = gameObject.AddComponent<HudController>();
                hud.Mission = mission;
                hud.Health = mission != null ? mission.PlayerHealth : null;
                hud.Drive = chase != null ? chase.Drive : null;
                hud.Controls = touch;
                hud.Weapons = player != null ? player.GetComponent<WeaponRig>() : null;
                hud.Restart = () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                hud.Build();
            }
            catch (Exception ex)
            {
                Debug.LogError("DERBY: HUD setup failed; 3D camera/world remain active. " + ex);
            }

            DerbyStartupDiagnostics.Report("Başlangıç",
                "kamera=" + (Camera.main != null) + " oyuncu=" + (player != null) +
                " görev=" + (mission != null) + " hud=" + (hud != null));

            if (player != null && mission != null)
            {
                try { SpawnEnemy(VehicleSpec.Drifter(), new Vector3(-12f, 1.1f, 6f), player.transform); }
                catch (Exception ex) { Debug.LogError("DERBY: first enemy could not spawn. " + ex); }
            }

            if (player != null)
            {
                try
                {
                    var engine = player.AddComponent<AudioSource>();
                    engine.clip = SynthAudio.EngineLoop();
                    engine.loop = true;
                    engine.spatialBlend = 0.4f;
                    engine.volume = 0.35f;
                    engine.Play();
                    player.AddComponent<EngineAudio>().Source = engine;
                }
                catch (Exception ex) { Debug.LogWarning("DERBY: engine audio unavailable. " + ex.Message); }
            }
        }

        void EnsurePrimaryCamera()
        {
            var camera = Camera.main;
            if (camera == null) camera = FindAnyObjectByType<Camera>();
            GameObject camGo;
            if (camera == null)
            {
                camGo = new GameObject("ChaseCamera");
                camera = camGo.AddComponent<Camera>();
            }
            else camGo = camera.gameObject;

            camGo.tag = "MainCamera";
            camera.enabled = true;
            camera.targetDisplay = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.075f, 0.08f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.cullingMask = ~0;
            camera.depth = 0f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 60f;
            Vector3 initialLookPoint = new Vector3(0f, 1.0f, -18f);
            Vector3 initialPosition = new Vector3(0f, 5.5f, -35f);
            camGo.transform.SetPositionAndRotation(initialPosition,
                Quaternion.LookRotation(initialLookPoint - initialPosition, Vector3.up));

            chase = camGo.GetComponent<ChaseCamera>();
            if (chase == null) chase = camGo.AddComponent<ChaseCamera>();
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
        }

        static void BuildEmergencyArena(Transform parent)
        {
            // Primitive renderers retain Unity's default material if no custom shader
            // is available; this deliberately avoids another Shader.Find failure.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "EmergencyFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -0.45f, 4f);
            floor.transform.localScale = new Vector3(78f, 0.9f, 92f);

            var wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallL.name = "EmergencyWallL";
            wallL.transform.SetParent(parent, false);
            wallL.transform.position = new Vector3(-38f, 3f, 4f);
            wallL.transform.localScale = new Vector3(2f, 6f, 92f);

            var wallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallR.name = "EmergencyWallR";
            wallR.transform.SetParent(parent, false);
            wallR.transform.position = new Vector3(38f, 3f, 4f);
            wallR.transform.localScale = new Vector3(2f, 6f, 92f);

            var lightGo = new GameObject("EmergencyDirectionalLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
        }

        static GameObject SpawnEmergencyVehicle()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Emergency Player Vehicle";
            go.tag = "Player";
            go.transform.position = new Vector3(0f, 1.1f, -24f);
            go.transform.localScale = new Vector3(1.8f, 0.75f, 3.4f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1250f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            return go;
        }

        void Update()
        {
            if (mission == null) return;
            int cells = mission.Rules.Cells;
            if (cells != spawnedCells)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    if (cells == 1) SpawnEnemySafely(VehicleSpec.Rammer(), new Vector3(16f, 1.1f, -12f), player.transform);
                    else if (cells == 2) SpawnEnemySafely(VehicleSpec.Hunter(), new Vector3(-20f, 1.1f, 20f), player.transform);
                    else if (cells >= 3) SpawnEnemySafely(VehicleSpec.WarRig(), new Vector3(0f, 1.4f, 30f), player.transform);
                }
                spawnedCells = cells;
            }
            if (mission.Rules.Phase == MissionPhase.Won || mission.Rules.Phase == MissionPhase.Lost)
            {
                if (KeyboardRestart()) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        void SpawnEnemySafely(VehicleSpec spec, Vector3 pos, Transform player)
        {
            try { SpawnEnemy(spec, pos, player); }
            catch (Exception ex) { Debug.LogError("DERBY: enemy spawn failed for " + spec.DisplayName + ". " + ex); }
        }

        static bool KeyboardRestart()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.enterKey.wasPressedThisFrame;
        }

        GameObject SpawnEnemy(VehicleSpec spec, Vector3 pos, Transform player)
        {
            var go = Spawn(spec, pos, Quaternion.LookRotation(player.position - pos), false);
            var ai = go.AddComponent<EnemyDriver>();
            ai.Drive = go.GetComponent<WheelDrive>();
            ai.Weapons = go.GetComponent<WeaponRig>();
            ai.Health = go.GetComponent<VehicleHealth>();
            ai.Player = player;
            ai.Mission = mission;
            return go;
        }

        GameObject Spawn(VehicleSpec spec, Vector3 pos, Quaternion rot, bool isPlayer)
        {
            var go = new GameObject(spec.DisplayName);
            go.transform.SetPositionAndRotation(pos, rot);
            if (isPlayer) go.tag = "Player";
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1250f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.7f * spec.Scale, 0f);
            box.size = new Vector3(1.8f, 0.7f, 3.3f) * spec.Scale;
            var drive = go.AddComponent<WheelDrive>();
            drive.Configure(spec);
            var hp = go.AddComponent<VehicleHealth>();
            hp.Configure(spec.Health);
            var guns = go.AddComponent<WeaponRig>();
            guns.Configure(spec, hp);
            var visual = go.AddComponent<GltfVehicleVisual>();
            visual.Drive = drive;
            visual.Weapons = guns;
            visual.Begin(spec);
            return go;
        }
    }

    public sealed class PlayerDrive : MonoBehaviour
    {
        public WheelDrive Drive;
        public WeaponRig Weapons;
        public TouchControls Controls;

        void Update()
        {
            if (Drive == null || Controls == null) return;
            var hp = GetComponent<VehicleHealth>();
            if (hp != null && hp.Dead)
            {
                Drive.Throttle = 0f;
                Drive.Brake = 1f;
                return;
            }
            Drive.Steer = Controls.Steer;
            Drive.Throttle = Controls.Throttle;
            Drive.Brake = Controls.Brake;
            Drive.Handbrake = Controls.Handbrake;
            Drive.NitroHeld = Controls.Nitro;
            if (Weapons == null) return;
            Vector3 aim = transform.position + Quaternion.Euler(0f, Controls.AimYaw, 0f) * transform.forward * 30f;
            if (Weapons.Muzzle != null)
                Weapons.Muzzle.rotation = Quaternion.LookRotation(aim - Weapons.Muzzle.position, Vector3.up);
            Weapons.TickFire(Controls.Fire, Controls.Rocket, aim);
        }
    }

    public sealed class EngineAudio : MonoBehaviour
    {
        public AudioSource Source;
        WheelDrive drive;
        void Awake() { drive = GetComponent<WheelDrive>(); }
        void Update()
        {
            if (Source == null || drive == null) return;
            Source.pitch = Mathf.Lerp(0.7f, 1.7f, Mathf.Clamp01(drive.PlanarSpeed() / 22f));
            if (drive.NitroActive) Source.pitch += 0.25f;
        }
    }
}
