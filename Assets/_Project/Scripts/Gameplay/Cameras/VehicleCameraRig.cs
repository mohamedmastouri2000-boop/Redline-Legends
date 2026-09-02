using RedlineLegends.Save;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Cameras
{
    /// <summary>
    /// Chase / hood / cockpit camera for one vehicle. Chase mode reacts to speed (distance, FOV),
    /// acceleration and braking (pitch/lean), drift (yaw offset) and collisions (shake). Runs in
    /// LateUpdate on the interpolated rigidbody pose.
    /// </summary>
    public sealed class VehicleCameraRig : MonoBehaviour
    {
        [Header("Chase")]
        [SerializeField] private float distance = 5.8f;
        [SerializeField] private float height = 2.0f;
        [SerializeField] private float lookHeight = 0.7f;
        [SerializeField] private float positionSharpness = 9f;
        [SerializeField] private float rotationSharpness = 12f;
        [SerializeField] private float speedDistanceGain = 1.4f;
        [SerializeField] private float baseFov = 58f;
        [SerializeField] private float maxFov = 78f;
        [SerializeField] private float driftYawGain = 0.35f;
        [SerializeField] private float accelerationPitch = 1.6f;

        [Header("Shake")]
        [SerializeField] private float collisionShake = 0.25f;
        [SerializeField] private float shakeDecay = 6f;

        private Camera _camera;
        private VehicleController _target;
        private CameraMode _mode = CameraMode.Chase;
        private float _shakeIntensity = 1f;
        private float _shake;
        private Vector3 _velocity;
        private float _fov;
        private float _smoothedAccel;
        private Transform _cockpitAnchor;
        private Vector3 _hoodLocal;
        private bool _cockpitSupported;

        public CameraMode Mode => _mode;
        public VehicleController Target => _target;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _fov = baseFov;
        }

        public void Follow(VehicleController target, bool cockpitSupported)
        {
            _target = target;
            _cockpitSupported = cockpitSupported;
            if (_target != null)
            {
                _target.Collided += OnCollided;
                _cockpitAnchor = VehicleVisualUtility.FindDeep(_target.transform, VehicleVisualUtility.CockpitCameraAnchor);
                var box = _target.GetComponent<BoxCollider>();
                _hoodLocal = box != null
                    ? new Vector3(0f, box.center.y + box.size.y * 0.5f + 0.05f, box.center.z + box.size.z * 0.28f)
                    : new Vector3(0f, 1.1f, 0.8f);
                SnapBehind();
            }
        }

        public void SetMode(CameraMode mode)
        {
            if (mode == CameraMode.Cockpit && (!_cockpitSupported || _cockpitAnchor == null)) mode = CameraMode.Hood;
            _mode = mode;
        }

        public void CycleMode()
        {
            var next = (CameraMode)(((int)_mode + 1) % 3);
            SetMode(next);
        }

        public void SetShakeIntensity(float intensity01) => _shakeIntensity = Mathf.Clamp01(intensity01);

        private void OnDestroy()
        {
            if (_target != null) _target.Collided -= OnCollided;
        }

        private void OnCollided(float impulse, Vector3 point)
        {
            _shake = Mathf.Min(1f, _shake + Mathf.Clamp01(impulse / 12f) * collisionShake * 4f);
        }

        public void SnapBehind()
        {
            if (_target == null) return;
            var t = _target.transform;
            transform.position = t.position - t.forward * distance + Vector3.up * height;
            transform.rotation = Quaternion.LookRotation(t.position + Vector3.up * lookHeight - transform.position, Vector3.up);
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            float dt = Time.deltaTime;
            var tel = _target.Telemetry;
            var t = _target.transform;

            switch (_mode)
            {
                case CameraMode.Hood:
                    transform.position = t.TransformPoint(_hoodLocal);
                    transform.rotation = t.rotation;
                    break;
                case CameraMode.Cockpit:
                    transform.position = _cockpitAnchor.position;
                    transform.rotation = t.rotation;
                    break;
                default:
                    UpdateChase(tel, t, dt);
                    break;
            }

            // Speed FOV and collision shake apply to every mode, shake scaled by the user setting.
            float speed01 = Mathf.Clamp01(tel.SpeedKmh / Mathf.Max(60f, tel.TopSpeedKmh));
            float targetFov = Mathf.Lerp(baseFov, maxFov, speed01 * speed01) + (tel.NitrousActive ? 6f : 0f);
            _fov = MathUtil.Damp(_fov, targetFov, 4f, dt);
            _camera.fieldOfView = _fov;

            if (_shake > 0.001f)
            {
                float s = _shake * _shakeIntensity * 0.12f;
                transform.position += transform.right * (Mathf.PerlinNoise(Time.time * 37f, 0.3f) - 0.5f) * s
                                      + transform.up * (Mathf.PerlinNoise(0.7f, Time.time * 41f) - 0.5f) * s;
                _shake = Mathf.MoveTowards(_shake, 0f, shakeDecay * dt);
            }
        }

        private void UpdateChase(in VehicleTelemetry tel, Transform t, float dt)
        {
            float speed01 = Mathf.Clamp01(tel.SpeedKmh / Mathf.Max(60f, tel.TopSpeedKmh));
            float dist = distance + speedDistanceGain * speed01;

            // Follow the velocity direction when moving so drifts show the car's angle.
            Vector3 forward = t.forward;
            Vector3 flatVel = new Vector3(_target.Body.linearVelocity.x, 0f, _target.Body.linearVelocity.z);
            if (flatVel.sqrMagnitude > 9f && tel.SpeedMs > 0f)
            {
                Vector3 velDir = flatVel.normalized;
                float drift = Mathf.Clamp01(Mathf.Abs(tel.DriftAngleDeg) / 45f) * driftYawGain;
                forward = Vector3.Slerp(new Vector3(forward.x, 0f, forward.z).normalized, velDir, 0.6f - drift).normalized;
            }
            else
            {
                forward = new Vector3(forward.x, 0f, forward.z).normalized;
                if (tel.SpeedMs < -1f) forward = -forward; // reversing: look back over the car
            }

            _smoothedAccel = MathUtil.Damp(_smoothedAccel, tel.LocalAcceleration.z, 3f, dt);
            float accelOffset = Mathf.Clamp(_smoothedAccel / 9.81f, -0.6f, 0.6f);
            Vector3 desired = t.position - forward * (dist + accelOffset * 0.6f) + Vector3.up * (height - accelOffset * 0.15f);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 1f / positionSharpness, Mathf.Infinity, dt);

            Vector3 lookAt = t.position + Vector3.up * lookHeight + forward * (speed01 * 1.5f);
            Quaternion wanted = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
            wanted *= Quaternion.Euler(-accelOffset * accelerationPitch, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, wanted, 1f - Mathf.Exp(-rotationSharpness * dt));
        }
    }
}
