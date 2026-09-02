using RedlineLegends.Race;
using RedlineLegends.Save;
using RedlineLegends.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedlineLegends.Input
{
    /// <summary>
    /// Local player input. Merges three sources into one command state:
    /// 1. On-screen touch controls (the HUD writes into <see cref="Touch"/>),
    /// 2. device tilt when the control style is Tilt,
    /// 3. Input System actions (keyboard/gamepad) for editor testing and controllers.
    /// Steering is smoothed here, not in the vehicle, so AI and replays get raw values.
    /// </summary>
    public sealed class MobileInputProvider : IInputProvider
    {
        /// <summary>Raw state written by on-screen controls each frame.</summary>
        public struct TouchState
        {
            public float Steer;       // buttons: -1/0/+1, wheel: continuous
            public bool SteerActive;  // true while a steering control is held
            public float Throttle;
            public float Brake;
            public bool Handbrake;
            public bool Nitrous;
        }

        private const float SteerReturnSharpness = 12f;

        private readonly InputActionAsset _actions;
        private InputAction _steer, _throttle, _brake, _handbrake, _nitrous, _shiftUp, _shiftDown, _reset;

        private SettingsData _settings;
        private float _smoothedSteer;
        private bool _shiftUpLatched, _shiftDownLatched, _resetLatched;
        private VehicleInputState _current;
        private bool _tiltCalibrated;
        private float _tiltZero;

        public TouchState Touch;
        public ControlSource Source => ControlSource.LocalPlayer;
        public bool Enabled { get; set; } = true;
        public ControlStyle ControlStyle => _settings != null ? _settings.ControlStyle : ControlStyle.Buttons;

        public MobileInputProvider(InputActionAsset actions, SettingsData settings)
        {
            _actions = actions;
            _settings = settings ?? new SettingsData();
            if (_actions != null)
            {
                var map = _actions.FindActionMap("Vehicle", false);
                if (map != null)
                {
                    _steer = map.FindAction("Steer", false);
                    _throttle = map.FindAction("Throttle", false);
                    _brake = map.FindAction("Brake", false);
                    _handbrake = map.FindAction("Handbrake", false);
                    _nitrous = map.FindAction("Nitrous", false);
                    _shiftUp = map.FindAction("ShiftUp", false);
                    _shiftDown = map.FindAction("ShiftDown", false);
                    _reset = map.FindAction("Reset", false);
                    map.Enable();
                }
            }
            EnableTiltIfNeeded();
        }

        public void ApplySettings(SettingsData settings)
        {
            _settings = settings ?? _settings;
            EnableTiltIfNeeded();
        }

        /// <summary>HUD buttons call these; keeps the HUD free of provider internals.</summary>
        public void RequestShiftUp() => _shiftUpLatched = true;
        public void RequestShiftDown() => _shiftDownLatched = true;
        public void RequestReset() => _resetLatched = true;

        /// <summary>Treat the current device angle as centre (called when a race starts).</summary>
        public void CalibrateTilt()
        {
            _tiltZero = ReadRawTilt();
            _tiltCalibrated = true;
        }

        public void Tick(float deltaTime)
        {
            if (!Enabled)
            {
                _current = VehicleInputState.Neutral;
                _smoothedSteer = MathUtil.Damp(_smoothedSteer, 0f, SteerReturnSharpness, deltaTime);
                return;
            }

            float targetSteer = 0f;
            bool steerHeld = false;

            switch (ControlStyle)
            {
                case ControlStyle.Tilt:
                    if (!_tiltCalibrated) CalibrateTilt();
                    float tilt = (ReadRawTilt() - _tiltZero) * 2.2f * _settings.TiltSensitivity;
                    targetSteer = Mathf.Clamp(tilt, -1f, 1f);
                    steerHeld = Mathf.Abs(targetSteer) > 0.02f;
                    break;
                case ControlStyle.SteeringWheel:
                    targetSteer = Touch.Steer;
                    steerHeld = Touch.SteerActive;
                    break;
                default:
                    targetSteer = Mathf.Clamp(Touch.Steer, -1f, 1f);
                    steerHeld = Touch.SteerActive;
                    break;
            }

            // Keyboard/gamepad overrides touch when active.
            if (_steer != null)
            {
                float axis = _steer.ReadValue<float>();
                if (Mathf.Abs(axis) > 0.05f)
                {
                    targetSteer = axis;
                    steerHeld = true;
                }
            }

            // Buttons ramp in at a sensitivity-controlled rate and snap back quickly, which is what
            // makes digital steering feel analogue. Continuous sources track directly.
            float sharpness = steerHeld
                ? (ControlStyle == ControlStyle.Buttons ? 5f * _settings.SteeringSensitivity : 18f)
                : SteerReturnSharpness;
            _smoothedSteer = MathUtil.Damp(_smoothedSteer, targetSteer, sharpness, deltaTime);

            float throttle = Touch.Throttle;
            float brake = Touch.Brake;
            bool handbrake = Touch.Handbrake;
            bool nitrous = Touch.Nitrous;

            if (_throttle != null) throttle = Mathf.Max(throttle, _throttle.ReadValue<float>());
            if (_brake != null) brake = Mathf.Max(brake, _brake.ReadValue<float>());
            if (_handbrake != null) handbrake |= _handbrake.IsPressed();
            if (_nitrous != null) nitrous |= _nitrous.IsPressed();
            if (_shiftUp != null && _shiftUp.WasPressedThisFrame()) _shiftUpLatched = true;
            if (_shiftDown != null && _shiftDown.WasPressedThisFrame()) _shiftDownLatched = true;
            if (_reset != null && _reset.WasPressedThisFrame()) _resetLatched = true;

            _current.Steer = _smoothedSteer;
            _current.Throttle = Mathf.Clamp01(throttle);
            _current.Brake = Mathf.Clamp01(brake);
            _current.Handbrake = handbrake;
            _current.Nitrous = nitrous;
        }

        public VehicleInputState Sample()
        {
            var s = _current;
            s.ShiftUp = _shiftUpLatched;
            s.ShiftDown = _shiftDownLatched;
            s.ResetVehicle = _resetLatched;
            _shiftUpLatched = _shiftDownLatched = _resetLatched = false;
            return Enabled ? s : VehicleInputState.Neutral;
        }

        public VehicleInputState Peek()
        {
            var s = _current;
            s.ShiftUp = _shiftUpLatched;
            s.ShiftDown = _shiftDownLatched;
            s.ResetVehicle = _resetLatched;
            return Enabled ? s : VehicleInputState.Neutral;
        }

        private void EnableTiltIfNeeded()
        {
            if (ControlStyle != ControlStyle.Tilt) return;
            var accel = Accelerometer.current;
            if (accel != null && !accel.enabled) InputSystem.EnableDevice(accel);
        }

        /// <summary>Device roll in landscape, -1..1 range roughly at +/-45 degrees.</summary>
        private static float ReadRawTilt()
        {
            var accel = Accelerometer.current;
            if (accel == null) return 0f;
            Vector3 a = accel.acceleration.ReadValue();
            // In landscape the device's Y axis points along the screen's long edge; rolling the
            // phone left/right changes acceleration.y. LandscapeRight flips the sign.
            float roll = Screen.orientation == ScreenOrientation.LandscapeRight ? -a.y : a.y;
            return Mathf.Clamp(roll, -1f, 1f);
        }

        public void Dispose()
        {
            if (_actions == null) return;
            var map = _actions.FindActionMap("Vehicle", false);
            map?.Disable();
        }
    }
}
