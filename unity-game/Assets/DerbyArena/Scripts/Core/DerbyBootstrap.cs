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

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            QualitySettings.shadowDistance = SystemInfo.systemMemorySize >= 3500 ? 42f : 0f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.07f, 0.075f, 0.08f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.16f, 0.18f);
            var world = new GameObject("BlackoutSpillway").transform;
            SpillwayBuilder.Build(world);
            mission = gameObject.AddComponent<MissionDirector>();
            mission.BuildWorldMarkers(world);
            var player = Spawn(VehicleSpec.Interceptor(), new Vector3(0f, 1.1f, -24f), Quaternion.identity, true);
            mission.PlayerHealth = player.GetComponent<VehicleHealth>();
            var camGo = new GameObject("ChaseCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            var chase = camGo.AddComponent<ChaseCamera>();
            chase.Target = player.transform;
            chase.Drive = player.GetComponent<WheelDrive>();
            var touch = gameObject.AddComponent<TouchControls>();
            var playerDrive = player.AddComponent<PlayerDrive>();
            playerDrive.Drive = player.GetComponent<WheelDrive>();
            playerDrive.Weapons = player.GetComponent<WeaponRig>();
            playerDrive.Controls = touch;
            hud = gameObject.AddComponent<HudController>();
            hud.Mission = mission;
            hud.Health = mission.PlayerHealth;
            hud.Drive = chase.Drive;
            hud.Controls = touch;
            hud.Restart = () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            hud.Build();
            SpawnEnemy(VehicleSpec.Drifter(), new Vector3(-12f, 1.1f, 6f), player.transform);
            var engine = player.AddComponent<AudioSource>();
            engine.clip = SynthAudio.EngineLoop();
            engine.loop = true;
            engine.spatialBlend = 0.4f;
            engine.volume = 0.35f;
            engine.Play();
            player.AddComponent<EngineAudio>().Source = engine;
        }

        void Update()
        {
            if (mission == null)
                return;
            int cells = mission.Rules.Cells;
            if (cells != spawnedCells)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (cells == 1)
                    SpawnEnemy(VehicleSpec.Rammer(), new Vector3(16f, 1.1f, -12f), player.transform);
                else if (cells == 2)
                    SpawnEnemy(VehicleSpec.Hunter(), new Vector3(-20f, 1.1f, 20f), player.transform);
                else if (cells >= 3)
                    SpawnEnemy(VehicleSpec.WarRig(), new Vector3(0f, 1.4f, 30f), player.transform);
                spawnedCells = cells;
            }
            if (mission.Rules.Phase == MissionPhase.Won || mission.Rules.Phase == MissionPhase.Lost)
            {
                if (KeyboardRestart())
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
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

        GameObject Spawn(VehicleSpec spec, Vector3 pos, Quaternion rot, bool player)
        {
            var go = new GameObject(spec.DisplayName);
            go.transform.SetPositionAndRotation(pos, rot);
            if (player)
                go.tag = "Player";
            var rb = go.AddComponent<Rigidbody>();
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
            if (Drive == null || Controls == null)
                return;
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
            if (Weapons == null)
                return;
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

        void Awake()
        {
            drive = GetComponent<WheelDrive>();
        }

        void Update()
        {
            if (Source == null || drive == null)
                return;
            Source.pitch = Mathf.Lerp(0.7f, 1.7f, Mathf.Clamp01(drive.PlanarSpeed() / 22f));
            if (drive.NitroActive)
                Source.pitch += 0.25f;
        }
    }
}
