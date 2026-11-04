using UnityEngine;

namespace DerbyArena.Vehicles
{
    /// <summary>
    /// Tracked-vehicle dressing (4.0, Tiger I T-12): scrolls the track texture with the car's speed, spins the road
    /// wheels, waves the antenna flag and plays the gun recoil. Attached to the model root by VehicleVisual (match)
    /// and the showcase (menu, idle). Filled in with the T-12 model contract: Track_L / Track_R (UV scroll along V),
    /// Road_* wheel nodes (axis X), Flag (vertex wave), Barrel (recoil along -Z).
    /// </summary>
    public sealed class TankVisual : MonoBehaviour
    {
        public static TankVisual Attach(Transform model, VehicleSpec spec, ArcadeCar car)
        {
            if (model == null) return null;
            var tv = model.GetComponent<TankVisual>();
            if (tv == null) tv = model.gameObject.AddComponent<TankVisual>();
            tv.car = car;
            tv.Collect();
            return tv;
        }

        ArcadeCar car;
        Renderer[] tracks;
        Transform[] wheels;
        Transform barrel;
        MeshFilter flag;
        Vector3[] flagBase;
        Mesh flagMesh;
        float scroll, recoil;
        MaterialPropertyBlock block;

        void Collect()
        {
            var tl = new System.Collections.Generic.List<Renderer>();
            var wl = new System.Collections.Generic.List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Track_")) { var r = t.GetComponentInChildren<Renderer>(); if (r != null) tl.Add(r); }
                else if (t.name.StartsWith("Road_") || t.name.StartsWith("Sprocket_") || t.name.StartsWith("Idler_")) wl.Add(t);
                else if (t.name == "Barrel") barrel = t;
                else if (t.name == "Flag") flag = t.GetComponentInChildren<MeshFilter>();
            }
            tracks = tl.ToArray();
            wheels = wl.ToArray();
            if (flag != null && flag.sharedMesh != null && flag.sharedMesh.isReadable)
            {
                flagMesh = Instantiate(flag.sharedMesh);
                flag.sharedMesh = flagMesh;
                flagBase = flagMesh.vertices;
            }
            block = new MaterialPropertyBlock();
        }

        /// <summary>Kick the barrel back (called by the weapon rig on a cannon shot).</summary>
        public void Recoil() { recoil = 1f; }

        void Update()
        {
            float dt = Time.deltaTime;
            float speed = car != null ? car.ForwardSpeed : 0.8f;
            scroll += speed * dt * 0.35f;
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Length; i++)
                {
                    if (tracks[i] == null) continue;
                    tracks[i].GetPropertyBlock(block);
                    block.SetVector("_MainTex_ST", new Vector4(1f, 1f, 0f, scroll));
                    tracks[i].SetPropertyBlock(block);
                }
            }
            if (wheels != null)
            {
                float deg = speed * dt * Mathf.Rad2Deg / 0.4f;
                for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null) wheels[i].Rotate(deg, 0f, 0f, Space.Self);
            }
            if (barrel != null)
            {
                recoil = Mathf.Max(0f, recoil - dt * 3.2f);
                float k = recoil < 0.85f ? recoil / 0.85f : (1f - recoil) / 0.15f;
                barrel.localPosition = new Vector3(0f, 0f, -0.45f * Mathf.SmoothStep(0f, 1f, k));
            }
            if (flagMesh != null && flagBase != null)
            {
                var v = flagMesh.vertices;
                float t = Time.time;
                float wind = 0.06f + Mathf.Min(0.1f, Mathf.Abs(speed) * 0.006f);
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 b = flagBase[i];
                    float u = Mathf.Clamp01(b.x / 0.9f);                 // distance from the pole along the flag
                    float w = Mathf.Sin(t * 6f - u * 5f) * wind * u + Mathf.Sin(t * 2.3f - u * 3f + b.y * 4f) * wind * 0.5f * u;
                    v[i] = new Vector3(b.x, b.y + w * 0.3f, b.z + w);
                }
                flagMesh.vertices = v;
                flagMesh.RecalculateNormals();
            }
        }

        void OnDestroy() { if (flagMesh != null) Destroy(flagMesh); }
    }
}
