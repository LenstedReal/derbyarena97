using UnityEngine;

namespace DerbyArena.World
{
    /// <summary>Slow spin and bob so pickups read as collectables.</summary>
    public sealed class PickupSpin : MonoBehaviour
    {
        public float SpinSpeed = 70f;
        public float BobHeight = 0.18f;
        float baseY;
        float phase;

        void Start()
        {
            baseY = transform.position.y;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            transform.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.World);
            var p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 2f + phase) * BobHeight;
            transform.position = p;
        }
    }
}
