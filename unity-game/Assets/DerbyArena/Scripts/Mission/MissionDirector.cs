using DerbyArena.Audio;
using DerbyArena.Vehicles;
using DerbyArena.World;
using UnityEngine;

namespace DerbyArena.Mission
{
    public sealed class MissionDirector : MonoBehaviour
    {
        public MissionRules Rules = new MissionRules();
        public Vector3 ExtractPoint = new Vector3(0f, 0.4f, 40f);
        public bool InsideExtract;
        public VehicleHealth PlayerHealth;

        GameObject extractVisual;
        int announcedCells;

        public void BuildWorldMarkers(Transform parent)
        {
            PlaceCell(parent, new Vector3(-22f, 0.6f, 8f));
            PlaceCell(parent, new Vector3(24f, 0.6f, -6f));
            PlaceCell(parent, new Vector3(-4f, 0.6f, 28f));
            extractVisual = SpillwayBuilder.Marker(parent, "Extract", ExtractPoint, new Color(0.15f, 0.85f, 0.45f), new Vector3(8f, 0.08f, 8f));
            extractVisual.SetActive(false);
            var zone = extractVisual.AddComponent<ExtractZone>();
            zone.Director = this;
            PlacePower(parent, new Vector3(-14f, 0.5f, -18f), PowerKind.Nitro);
            PlacePower(parent, new Vector3(8f, 0.5f, 6f), PowerKind.Repair);
            PlacePower(parent, new Vector3(-18f, 0.5f, 16f), PowerKind.Armor);
            PlacePower(parent, new Vector3(18f, 0.5f, 24f), PowerKind.Ammo);
        }

        void PlaceCell(Transform parent, Vector3 pos)
        {
            var go = SpillwayBuilder.Marker(parent, "ReactorCell", pos, new Color(0.2f, 0.75f, 1f), new Vector3(1.1f, 1.4f, 1.1f));
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var pickup = go.AddComponent<PowerPickup>();
            pickup.Kind = PowerKind.Cell;
            pickup.Director = this;
        }

        void PlacePower(Transform parent, Vector3 pos, PowerKind kind)
        {
            Color color = kind == PowerKind.Nitro ? new Color(0.2f, 0.55f, 1f)
                : kind == PowerKind.Repair ? new Color(0.3f, 0.9f, 0.35f)
                : kind == PowerKind.Armor ? new Color(0.75f, 0.75f, 0.8f)
                : new Color(1f, 0.55f, 0.15f);
            var go = SpillwayBuilder.Marker(parent, kind.ToString(), pos, color, new Vector3(0.8f, 0.8f, 0.8f));
            go.GetComponent<Collider>().isTrigger = true;
            var pickup = go.AddComponent<PowerPickup>();
            pickup.Kind = kind;
            pickup.Director = this;
        }

        void Update()
        {
            if (PlayerHealth != null && PlayerHealth.Dead)
                Rules.PlayerDead = true;
            Rules.Tick(Time.deltaTime, InsideExtract);
            if (Rules.Cells != announcedCells)
            {
                announcedCells = Rules.Cells;
                AudioSource.PlayClipAtPoint(SynthAudio.Ping(), ExtractPoint, 0.8f);
            }
            if (Rules.Phase == MissionPhase.Extract && extractVisual != null && !extractVisual.activeSelf)
            {
                extractVisual.SetActive(true);
                AudioSource.PlayClipAtPoint(SynthAudio.Ping(), ExtractPoint, 1f);
            }
        }
    }

    public enum PowerKind
    {
        Cell,
        Nitro,
        Repair,
        Armor,
        Ammo
    }

    public sealed class PowerPickup : MonoBehaviour
    {
        public PowerKind Kind;
        public MissionDirector Director;
        bool taken;

        void Update()
        {
            if (taken)
                return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && Vector3.Distance(player.transform.position, transform.position) < 2.4f)
                Collect(player);
        }

        void OnTriggerEnter(Collider other)
        {
            var body = other.GetComponentInParent<Rigidbody>();
            if (body != null && body.CompareTag("Player"))
                Collect(body.gameObject);
        }

        void Collect(GameObject player)
        {
            if (taken)
                return;
            taken = true;
            var health = player.GetComponent<VehicleHealth>();
            var drive = player.GetComponent<WheelDrive>();
            var guns = player.GetComponent<DerbyArena.Combat.WeaponRig>();
            if (Kind == PowerKind.Cell)
            {
                if (Director != null)
                    Director.Rules.CollectCell();
            }
            else if (drive != null && Kind == PowerKind.Nitro)
                drive.Nitro = 1f;
            else if (health != null && Kind == PowerKind.Repair)
                health.Repair(45f);
            else if (health != null && Kind == PowerKind.Armor)
                health.AddArmor(30f);
            else if (guns != null && Kind == PowerKind.Ammo)
                guns.AddRockets(4);
            AudioSource.PlayClipAtPoint(SynthAudio.Ping(), transform.position, 0.7f);
            gameObject.SetActive(false);
        }
    }

    public sealed class ExtractZone : MonoBehaviour
    {
        public MissionDirector Director;

        void OnTriggerStay(Collider other)
        {
            var body = other.GetComponentInParent<Rigidbody>();
            if (body != null && body.CompareTag("Player") && Director != null)
                Director.InsideExtract = true;
        }

        void OnTriggerExit(Collider other)
        {
            var body = other.GetComponentInParent<Rigidbody>();
            if (body != null && body.CompareTag("Player") && Director != null)
                Director.InsideExtract = false;
        }
    }
}
