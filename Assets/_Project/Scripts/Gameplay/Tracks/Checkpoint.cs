using System;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Tracks
{
    /// <summary>
    /// Trigger gate across the track. Index 0 is the start/finish line. Also the respawn pose:
    /// forward = track direction. Reports vehicles that pass; the race session decides validity.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private float halfWidth = 8f;

        public int Index => index;
        public float HalfWidth => halfWidth;
        public Vector3 RespawnPosition => transform.position + Vector3.up * 0.3f;
        public Quaternion RespawnRotation => Quaternion.LookRotation(Flat(transform.forward), Vector3.up);

        public event Action<Checkpoint, VehicleController> Passed;

        private void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            var vehicle = rb.GetComponent<VehicleController>();
            if (vehicle == null) return;
            Passed?.Invoke(this, vehicle);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }

#if UNITY_EDITOR
        public void EditorInitialize(int newIndex, float newHalfWidth)
        {
            index = newIndex;
            halfWidth = newHalfWidth;
        }
#endif
    }
}
