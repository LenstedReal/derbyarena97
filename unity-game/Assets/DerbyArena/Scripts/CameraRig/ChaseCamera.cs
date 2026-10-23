using DerbyArena.Vehicles;
using UnityEngine;

namespace DerbyArena.CameraRig
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target;
        public WheelDrive Drive;
        public float Distance = 8.2f;
        public float Height = 2.7f;
        float shake;
        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = gameObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 180f;
            if (GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        void LateUpdate()
        {
            if (Target == null)
                return;
            float fov = 58f;
            if (Drive != null && Drive.NitroActive)
                fov = 68f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-4f * Time.deltaTime));
            Vector3 look = Target.position + Vector3.up * 1.2f + Target.forward * 3.2f;
            Vector3 desired = Target.position - Target.forward * Distance + Vector3.up * Height;
            Vector3 dir = desired - look;
            float len = dir.magnitude;
            if (len > 0.1f && Physics.SphereCast(look, 0.35f, dir.normalized, out var hit, len, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != Target && hit.transform.root != Target)
                    desired = look + dir.normalized * Mathf.Max(1.2f, hit.distance - 0.4f);
            }
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-6f * Time.deltaTime));
            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - Time.deltaTime);
                transform.position += Random.insideUnitSphere * shake;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
