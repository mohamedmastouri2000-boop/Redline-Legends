using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Tracks;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.AI
{
    /// <summary>
    /// Circuit AI: follows the racing line with pure-pursuit steering, brakes for corners using the
    /// line's target speeds (so it never exceeds what its car can do), avoids and overtakes other
    /// cars, and makes believable, seeded mistakes at the profile's frequency. Plain class ticked by
    /// the race session in FixedUpdate; no per-driver MonoBehaviour or Update.
    /// </summary>
    public sealed class AIDriver
    {
        private enum Mistake { None, LateBrake, WideLine, Lift }

        private readonly AIProfile _profile;
        private readonly RacingLine _line;
        private readonly VehicleController _car;
        private readonly AIInputProvider _input;
        private Xorshift _rng;

        private int _hint = -1;
        private float _steer;
        private float _throttle;
        private float _brake;
        private float _lateralOffset;
        private float _lateralTarget;
        private float _noiseSeed;
        private float _mistakeTimer;
        private Mistake _mistake;
        private float _stuckTimer;
        private float _reverseTimer;
        private float _overtakeSide;
        private float _overtakeTimer;
        private readonly RaycastHit[] _hits = new RaycastHit[4];

        public bool Active { get; set; } = true;
        /// <summary>Cruise after finishing: still drives the line, but gently.</summary>
        public bool Cruise { get; set; }
        public float SpeedScale { get; set; } = 1f;
        public int LineHint => _hint;

        public AIDriver(AIProfile profile, RacingLine line, VehicleController car, AIInputProvider input, int seed)
        {
            _profile = profile;
            _line = line;
            _car = car;
            _input = input;
            _rng = new Xorshift(seed);
            _noiseSeed = _rng.Range(0f, 100f);
            SpeedScale = profile != null ? profile.SpeedScale : 0.85f;
        }

        public void FixedTick(float dt, float raceTime)
        {
            if (!Active)
            {
                _input.SetAxes(0f, 0f, 1f, false, false);
                return;
            }
            var tel = _car.Telemetry;
            Vector3 pos = _car.transform.position;
            Vector3 fwd = _car.transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            float speed = Mathf.Max(0f, tel.SpeedMs);

            _hint = _line.FindNearest(pos, _hint);
            float along = _line.DistanceAlong(pos, _hint);

            UpdateMistakes(dt, raceTime);
            UpdateOvertaking(dt, pos, fwd, speed, out float aheadBrake);

            // ---- steering: chase a point ahead on the line, offset laterally for overtakes/mistakes
            float accuracy = _profile != null ? _profile.CorneringAccuracy : 0.7f;
            float lookAhead = Mathf.Clamp(6f + speed * 0.75f, 7f, 42f);
            float wobble = (Mathf.PerlinNoise(_noiseSeed, raceTime * 0.35f) - 0.5f) * 2f * (1f - accuracy) * 2.2f;
            _lateralTarget = _overtakeSide * 3f + (_mistake == Mistake.WideLine ? 2.5f : 0f) + wobble;
            _lateralOffset = MathUtil.Damp(_lateralOffset, _lateralTarget, 2.5f, dt);
            float halfWidth = _line.HalfWidth(_hint) - 1.5f;
            _lateralOffset = Mathf.Clamp(_lateralOffset, -halfWidth, halfWidth);

            int targetIndex = _line.IndexAtDistance(_line.Loop ? Mathf.Repeat(along + lookAhead, _line.TotalLength) : Mathf.Min(along + lookAhead, _line.TotalLength - 0.1f));
            Vector3 target = _line.PointAtDistance(along + lookAhead) + _line.RightAt(targetIndex) * _lateralOffset;
            Vector3 toTarget = target - pos;
            toTarget.y = 0f;
            float angle = Vector3.SignedAngle(fwd, toTarget, Vector3.up);
            float maxSteer = Mathf.Max(8f, _car.Stats.Handling.MaxSteerAngleDeg * (1f - _car.Stats.Handling.HighSpeedSteerReduction * Mathf.Clamp01(speed / 60f)));
            float desiredSteer = Mathf.Clamp(angle / maxSteer, -1f, 1f);

            // Counter-steer when the car is sliding beyond the drift the line asks for.
            if (tel.IsDrifting) desiredSteer += Mathf.Clamp(-tel.YawRate * 0.15f, -0.4f, 0.4f);

            // ---- speed: lowest reachable speed over the braking horizon
            float brakingQuality = _profile != null ? _profile.BrakingQuality : 0.7f;
            float grip = _car.Stats.Tires.LongitudinalGrip;
            float decel = grip * 9.81f * Mathf.Lerp(0.55f, 0.92f, brakingQuality);
            if (_mistake == Mistake.LateBrake) decel *= 1.35f; // believes it can brake later than it can
            float horizon = Mathf.Clamp(speed * speed / (2f * decel) + 25f, 30f, 160f);
            float allowed = float.MaxValue;
            int steps = Mathf.CeilToInt(horizon / 5f);
            for (int s = 0; s <= steps; s++)
            {
                float d = s * 5f;
                float distAhead = along + d;
                if (!_line.Loop && distAhead > _line.TotalLength) break;
                int idx = _line.IndexAtDistance(_line.Loop ? Mathf.Repeat(distAhead, _line.TotalLength) : distAhead);
                float v = _line.TargetSpeed(idx) * SpeedScale;
                float reach = Mathf.Sqrt(v * v + 2f * decel * d);
                if (reach < allowed) allowed = reach;
            }
            if (Cruise) allowed = Mathf.Min(allowed, 14f);
            if (_mistake == Mistake.Lift) allowed = Mathf.Min(allowed, speed * 0.85f);

            float throttleQuality = _profile != null ? _profile.ThrottleQuality : 0.7f;
            float error = allowed - speed;
            float desiredThrottle = Mathf.Clamp01(error / 3f) * Mathf.Lerp(0.75f, 1f, throttleQuality);
            float desiredBrake = Mathf.Clamp01(-error / 4f);
            desiredBrake = Mathf.Max(desiredBrake, aheadBrake);
            // Ease the throttle while turning hard so the exit is not a spin.
            float steerLoad = Mathf.Abs(desiredSteer);
            if (steerLoad > 0.5f && speed > 12f) desiredThrottle *= Mathf.Lerp(1f, 0.55f, (steerLoad - 0.5f) * 2f);
            // Tyres already sliding: back off proportionally to how sharp the driver is.
            if (tel.MaxSlip > 0.6f) desiredThrottle *= Mathf.Lerp(0.5f, 0.85f, throttleQuality);

            // ---- stuck recovery: reverse briefly then continue
            bool wantsToMove = desiredThrottle > 0.4f;
            if (speed < 0.8f && wantsToMove && _reverseTimer <= 0f) _stuckTimer += dt; else if (_reverseTimer <= 0f) _stuckTimer = 0f;
            if (_stuckTimer > 2.5f)
            {
                _reverseTimer = 1.6f;
                _stuckTimer = 0f;
            }
            if (_reverseTimer > 0f)
            {
                _reverseTimer -= dt;
                desiredThrottle = 0f;
                desiredBrake = 1f;           // in an automatic, brake from rest engages reverse
                desiredSteer = -desiredSteer;
            }

            // ---- reaction time: first-order lag on every input
            float reaction = Mathf.Max(0.04f, _profile != null ? _profile.ReactionTime : 0.3f);
            float k = 1f - Mathf.Exp(-dt / reaction);
            _steer += (desiredSteer - _steer) * Mathf.Min(1f, k * 2f);
            _throttle += (desiredThrottle - _throttle) * k;
            _brake += (desiredBrake - _brake) * Mathf.Min(1f, k * 1.5f);

            bool nitrous = _car.Stats.Nitrous.IsFitted && !Cruise && tel.Nitrous01 > 0.25f && _throttle > 0.9f
                           && Mathf.Abs(_steer) < 0.15f && allowed - speed > 6f;

            _input.SetAxes(_steer, _throttle, _brake, false, nitrous);
        }

        private void UpdateMistakes(float dt, float raceTime)
        {
            if (_mistakeTimer > 0f)
            {
                _mistakeTimer -= dt;
                if (_mistakeTimer <= 0f) _mistake = Mistake.None;
                return;
            }
            float perMinute = _profile != null ? _profile.MistakeFrequency : 1f;
            // Bernoulli trial per step, expected rate = perMinute / 60 s.
            if (_rng.NextFloat() < perMinute / 60f * dt)
            {
                int roll = _rng.Range(0, 3);
                _mistake = roll == 0 ? Mistake.LateBrake : roll == 1 ? Mistake.WideLine : Mistake.Lift;
                _mistakeTimer = _mistake == Mistake.Lift ? 0.8f : 1.8f;
            }
        }

        /// <summary>Spherecast ahead for cars; brake if closing, pick a side to pass if faster.</summary>
        private void UpdateOvertaking(float dt, Vector3 pos, Vector3 fwd, float speed, out float aheadBrake)
        {
            aheadBrake = 0f;
            if (_overtakeTimer > 0f)
            {
                _overtakeTimer -= dt;
                if (_overtakeTimer <= 0f) _overtakeSide = 0f;
            }
            float range = Mathf.Clamp(8f + speed * 0.9f, 10f, 40f);
            int count = Physics.SphereCastNonAlloc(pos + Vector3.up * 0.6f + fwd * 2.5f, 1.2f, fwd, _hits, range, GameLayers.VehicleMask, QueryTriggerInteraction.Ignore);
            float aggression = _profile != null ? _profile.Aggression : 0.3f;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                var rb = hit.rigidbody;
                if (rb == null || rb == _car.Body) continue;
                float dist = hit.distance;
                float otherSpeed = Vector3.Dot(rb.linearVelocity, fwd);
                float closing = speed - otherSpeed;
                // Aggressive drivers tolerate a smaller gap before braking.
                float safeGap = Mathf.Lerp(9f, 4f, aggression) + closing * 0.6f;
                if (closing > 0.5f && dist < safeGap)
                    aheadBrake = Mathf.Max(aheadBrake, Mathf.Clamp01((safeGap - dist) / safeGap));
                if (closing > 1.5f && _overtakeTimer <= 0f)
                {
                    // Pass on the side with more room relative to the line.
                    Vector3 right = _line.RightAt(_hint);
                    float otherLateral = Vector3.Dot(rb.position - pos, right);
                    _overtakeSide = otherLateral > 0f ? -1f : 1f;
                    _overtakeTimer = 3.5f;
                }
            }
        }
    }
}
