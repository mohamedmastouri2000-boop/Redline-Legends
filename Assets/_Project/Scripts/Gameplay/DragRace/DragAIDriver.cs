using RedlineLegends.AI;
using RedlineLegends.Input;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.DragRace
{
    /// <summary>
    /// Drag opponent: holds a launch rpm during staging, leaves after a sampled reaction time (or
    /// jumps the start with the profile's false-start chance), shifts manually at an accuracy-
    /// dependent rpm and uses nitrous per its strategy. All randomness is seeded.
    /// </summary>
    public sealed class DragAIDriver
    {
        private readonly VehicleController _car;
        private readonly AIInputProvider _input;
        private readonly AIProfile _profile;
        private Xorshift _rng;

        private readonly float _reactionTime;
        private readonly bool _falseStart;
        private readonly float _falseStartLead;
        private readonly float _launchRpmNormalized;
        private readonly float _shiftRpmNormalized;
        private readonly DragNitrousStrategy _nitrous;

        private Vector3 _laneOrigin;
        private Vector3 _laneForward;
        private Vector3 _laneRight;
        private float _distanceMeters;
        private float _greenTime = float.MaxValue;
        private bool _lightsStarted;
        private int _shiftedFromGear;
        private float _throttle;

        public bool Active { get; set; } = true;
        public float ReactionTime => _reactionTime;
        public bool WillFalseStart => _falseStart;

        public DragAIDriver(AIProfile profile, VehicleController car, AIInputProvider input, int seed)
        {
            _profile = profile;
            _car = car;
            _input = input;
            _rng = new Xorshift(seed);

            float rMin = profile != null ? profile.DragReactionTimeMin : 0.3f;
            float rMax = profile != null ? profile.DragReactionTimeMax : 0.6f;
            _reactionTime = _rng.Range(Mathf.Min(rMin, rMax), Mathf.Max(rMin, rMax));
            _falseStart = _rng.NextFloat() < (profile != null ? profile.FalseStartChance : 0.02f);
            _falseStartLead = _rng.Range(0.05f, 0.3f);

            // Ideal launch sits around 55-65% of the redline for a street car; error grows as launch quality drops.
            float launchQuality = profile != null ? profile.LaunchQuality : 0.6f;
            float launchError = (1f - launchQuality) * _rng.Range(-0.3f, 0.35f);
            _launchRpmNormalized = Mathf.Clamp(0.6f + launchError, 0.25f, 0.98f);

            float shiftAccuracy = profile != null ? profile.ShiftAccuracy : 0.6f;
            float shiftError = (1f - shiftAccuracy) * _rng.Range(-0.25f, 0.12f);
            _shiftRpmNormalized = Mathf.Clamp(0.94f + shiftError, 0.6f, 1.02f);

            _nitrous = profile != null ? profile.NitrousStrategy : DragNitrousStrategy.Never;
            if (_nitrous == DragNitrousStrategy.Random)
                _nitrous = (DragNitrousStrategy)_rng.Range(1, 4);
        }

        public void SetLane(Vector3 origin, Vector3 forward, float distanceMeters)
        {
            _laneOrigin = origin;
            _laneForward = forward.normalized;
            _laneRight = Vector3.Cross(Vector3.up, _laneForward);
            _distanceMeters = distanceMeters;
        }

        /// <summary>Called when the amber sequence starts and again when green shows.</summary>
        public void NotifyLightsStarted() => _lightsStarted = true;
        public void NotifyGreen(float greenTime) => _greenTime = greenTime;

        private float LaunchTime => _falseStart && _lightsStarted && _greenTime < float.MaxValue
            ? _greenTime - _falseStartLead
            : _greenTime + _reactionTime;

        public void FixedTick(float dt, float now, float lightsStartTime, float expectedGreenTime)
        {
            if (!Active)
            {
                _input.SetAxes(0f, 0f, 1f, false, false);
                return;
            }
            var tel = _car.Telemetry;
            float launchAt = _greenTime < float.MaxValue ? LaunchTime : (_falseStart && _lightsStarted ? expectedGreenTime - _falseStartLead : float.MaxValue);
            bool launched = now >= launchAt;

            if (!launched)
            {
                // Build and hold the launch rpm: bang-bang throttle with smoothing.
                float target = _launchRpmNormalized;
                float desired = tel.RpmNormalized < target - 0.03f ? 1f : tel.RpmNormalized > target + 0.03f ? 0.15f : 0.55f;
                _throttle = MathUtil.Damp(_throttle, desired, 18f, dt);
                _input.SetAxes(0f, _throttle, 1f, false, false);
                return;
            }

            // Launched: full throttle, keep the lane, shift at the target rpm.
            _throttle = MathUtil.Damp(_throttle, 1f, 30f, dt);
            Vector3 offset = _car.transform.position - _laneOrigin;
            float lateral = Vector3.Dot(offset, _laneRight);
            float lateralVel = Vector3.Dot(_car.Body.linearVelocity, _laneRight);
            float steer = Mathf.Clamp(-lateral * 0.12f - lateralVel * 0.08f, -0.35f, 0.35f);

            if (tel.Gear > 0 && tel.Gear < tel.GearCount && tel.Gear != _shiftedFromGear && tel.RpmNormalized >= _shiftRpmNormalized && !tel.IsShifting)
            {
                _shiftedFromGear = tel.Gear;
                _input.RequestShiftUp();
            }

            float progress01 = _distanceMeters > 0f ? Vector3.Dot(offset, _laneForward) / _distanceMeters : 0f;
            bool nitrous = false;
            switch (_nitrous)
            {
                case DragNitrousStrategy.AtLaunch: nitrous = true; break;
                case DragNitrousStrategy.AfterSecondShift: nitrous = tel.Gear >= 3; break;
                case DragNitrousStrategy.FinalStretch: nitrous = progress01 > 0.55f; break;
            }
            _input.SetAxes(steer, _throttle, 0f, false, nitrous && tel.Nitrous01 > 0.05f);
        }
    }
}
