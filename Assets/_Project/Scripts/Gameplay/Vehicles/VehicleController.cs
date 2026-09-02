using System;
using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Save;
using RedlineLegends.Utilities;
using UnityEngine;

namespace RedlineLegends.Vehicles
{
    /// <summary>
    /// Arcade-realistic vehicle simulation on a single Rigidbody with raycast suspension.
    ///
    /// Design notes:
    /// - Consumes only <see cref="IInputProvider"/> commands; it does not know who is driving.
    /// - All numbers come from the resolved <see cref="VehicleStats"/>; nothing vehicle-specific is
    ///   hard-coded here, so upgrades and tuning are real.
    /// - Tyres use a slip-angle/slip-ratio model with a friction ellipse. Wheel spin is modelled as
    ///   extra surface speed that builds when drive force exceeds traction, which gives burnouts,
    ///   launch RPM management and reduced grip when spinning without integrating wheel inertia
    ///   (that route is unstable at 50 Hz and costs more on mobile).
    /// - Custom raycast suspension instead of WheelCollider: predictable, tunable, cheap.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        private const float LowSpeedBlendMs = 2.5f;
        private const float SpinAccel = 30f;      // m/s^2 growth of wheel-spin surface speed
        private const float SpinDecay = 7f;       // per-second decay when traction is regained
        private const float ClutchFullMs = 4.5f;  // speed at which the auto clutch is fully engaged
        private const float MinShiftInterval = 0.35f;

        private static PhysicsMaterial _bodyMaterial;

        private VehicleStats _stats;
        private IInputProvider _input;
        private Rigidbody _rb;
        private WheelState[] _wheels = Array.Empty<WheelState>();
        private VehicleInputState _cmd;
        private VehicleTelemetry _telemetry;

        // Steering / drivetrain state
        private float _steerAngle;
        private float _rpm;
        private float _freeRpm;
        private int _gear = 1;
        private float _shiftTimer;
        private float _timeSinceShift = 10f;
        private bool _limiterCut;
        private float _turboBoost;
        private float _nitrousRemaining;
        private bool _nitrousActive;
        private float _upsideDownTime;
        private Vector3 _prevVelocity;
        private float _roadRpm;
        private bool _lastShiftWasUp;

        public VehicleStats Stats => _stats;
        public Rigidbody Body => _rb;
        public WheelState[] Wheels => _wheels;
        public VehicleTelemetry Telemetry => _telemetry;
        public IInputProvider InputProvider => _input;
        public bool IsInitialized => _stats != null;

        /// <summary>Auto or manual gearbox; set by the spawner from settings (or by AI to Automatic).</summary>
        public TransmissionMode TransmissionMode { get; set; } = TransmissionMode.Automatic;

        /// <summary>When true the car ignores throttle and holds the brakes (grid / staging).</summary>
        public bool HoldBrakes { get; set; }

        /// <summary>Distance driven, for odometer/race progress consumers.</summary>
        public float OdometerMeters { get; private set; }

        public event Action<int, int, float, ShiftQuality> Shifted;
        public event Action<float, Vector3> Collided;
        public event Action LimiterHit;
        public event Action<bool> NitrousChanged;
        public event Action ResetRequested;

        // ------------------------------------------------------------------ setup

        public void Initialize(VehicleStats stats, IInputProvider input, WheelSetup[] wheelSetups)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rb = GetComponent<Rigidbody>();

            _rb.mass = _stats.Chassis.MassKg;
            _rb.centerOfMass = _stats.Chassis.CenterOfMassOffset;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0.5f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rb.maxAngularVelocity = 12f;
            // Inertia stays automatic (from the body collider). Setting inertiaTensorRotation here
            // would switch the rigidbody to a manual tensor that has not been computed yet.
            _rb.ResetInertiaTensor();

            // The prefab's wheel hubs are the authored stance; that stance is the mid-travel rest
            // position, and RideHeightM lifts the body from there (tuning can lower it).
            float attachLift = _stats.Suspension.TravelM * 0.5f + _stats.Suspension.RideHeightM;
            var drive = _stats.Transmission.Drivetrain;
            _wheels = new WheelState[wheelSetups.Length];
            for (int i = 0; i < wheelSetups.Length; i++)
            {
                var s = wheelSetups[i];
                bool driven = drive == DrivetrainType.AWD || (drive == DrivetrainType.FWD && s.IsFront) || (drive == DrivetrainType.RWD && !s.IsFront);
                _wheels[i] = new WheelState
                {
                    Name = s.Name,
                    IsFront = s.IsFront,
                    IsLeft = s.IsLeft,
                    IsSteer = s.IsFront,
                    IsDriven = driven,
                    Radius = s.Radius,
                    LocalAttachPoint = s.LocalHubAtRest + Vector3.up * attachLift,
                    Visual = s.Visual,
                    Compression = 0.5f,
                    PrevCompression = 0.5f,
                    VisualHubLocalY = s.LocalHubAtRest.y
                };
            }

            _rpm = _freeRpm = _stats.Engine.IdleRpm;
            _nitrousRemaining = _stats.Nitrous.CapacitySeconds;
            _gear = 1;
            _telemetry.TopSpeedKmh = _stats.Chassis.TopSpeedKmh;
            _telemetry.RedlineRpm = _stats.Engine.RedlineRpm;
            _telemetry.IdleRpm = _stats.Engine.IdleRpm;
            _telemetry.GearCount = _stats.Transmission.GearCount;
        }

        public void SetInputProvider(IInputProvider input) => _input = input ?? throw new ArgumentNullException(nameof(input));

        public static PhysicsMaterial BodyMaterial
        {
            get
            {
                if (_bodyMaterial == null)
                {
                    _bodyMaterial = new PhysicsMaterial("VehicleBody")
                    {
                        dynamicFriction = 0.15f,
                        staticFriction = 0.15f,
                        bounciness = 0.05f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Minimum
                    };
                }
                return _bodyMaterial;
            }
        }

        /// <summary>
        /// Moves the car and kills its motion (respawn / grid placement). The car is settled onto
        /// the ground under the target so it never drops or spawns intersecting the road.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            position = SnapToGround(position, rotation);
            _rb.position = position;
            _rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _prevVelocity = Vector3.zero;
            _upsideDownTime = 0f;
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i].SpinSpeed = 0f;
                _wheels[i].Compression = _wheels[i].PrevCompression = 0.5f;
            }
            if (_gear < 1) _gear = 1;
            _rpm = _freeRpm = _stats.Engine.IdleRpm;
        }

        /// <summary>Returns the root position that puts the wheels at rest ride height on the ground below.</summary>
        public Vector3 SnapToGround(Vector3 position, Quaternion rotation)
        {
            if (_wheels.Length == 0) return position;
            float travel = _stats.Suspension.TravelM;
            // Rest hub height in local space: mid-travel below the attach point.
            float restHubLocalY = _wheels[0].LocalAttachPoint.y - travel * 0.5f;
            float radius = _wheels[0].Radius;
            Vector3 up = rotation * Vector3.up;
            Vector3 origin = position + up * 3f;
            if (Physics.Raycast(origin, -up, out RaycastHit hit, 30f, GameLayers.GroundMask, QueryTriggerInteraction.Ignore))
            {
                // root = ground + radius - restHubLocalY along up
                float offset = radius - restHubLocalY;
                return hit.point + up * offset;
            }
            return position;
        }

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (!IsInitialized) return;
            _input.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!IsInitialized) return;
            float dt = Time.fixedDeltaTime;
            _cmd = _input.Sample();
            if (_cmd.ResetVehicle) ResetRequested?.Invoke();

            Vector3 localVel = transform.InverseTransformDirection(_rb.linearVelocity);
            float forwardSpeed = localVel.z;
            float speed = _rb.linearVelocity.magnitude;

            float throttle, brake;
            ResolvePedals(forwardSpeed, out throttle, out brake);
            // Launch hold keeps the brakes on but lets the driver rev the engine (drag staging).
            if (HoldBrakes) brake = 1f;

            UpdateSteering(speed, dt);
            UpdateTransmission(throttle, forwardSpeed, dt);
            float wheelTorque = UpdateEngine(throttle, forwardSpeed, dt);

            int grounded = 0;
            for (int i = 0; i < _wheels.Length; i++)
            {
                var w = _wheels[i];
                UpdateSuspension(w, dt);
                if (w.Grounded) grounded++;
            }
            ApplyAntiRoll();

            float drivenCount = 0f;
            for (int i = 0; i < _wheels.Length; i++) if (_wheels[i].IsDriven) drivenCount++;
            float frontShare = _stats.Transmission.Drivetrain == DrivetrainType.AWD ? _stats.Transmission.AwdFrontTorqueSplit : 0f;

            float maxSlip = 0f;
            float totalDrive = 0f;
            for (int i = 0; i < _wheels.Length; i++)
            {
                var w = _wheels[i];
                float torqueShare = 0f;
                if (w.IsDriven && drivenCount > 0f)
                {
                    torqueShare = _stats.Transmission.Drivetrain == DrivetrainType.AWD
                        ? (w.IsFront ? frontShare : 1f - frontShare) * 0.5f
                        : 1f / drivenCount;
                }
                UpdateTyre(w, wheelTorque * torqueShare, brake, _cmd.Handbrake, dt);
                if (w.SlipAmount > maxSlip) maxSlip = w.SlipAmount;
                totalDrive += w.DriveForce;
            }

            ApplyAerodynamics(localVel);
            ApplyStabilityAssist(forwardSpeed, grounded);
            UpdateWheelVisualState(dt);
            UpdateUpsideDown(grounded, dt);

            OdometerMeters += Mathf.Abs(forwardSpeed) * dt;
            FillTelemetry(localVel, throttle, brake, grounded, maxSlip, totalDrive, dt);
        }

        // ------------------------------------------------------------------ pedals & steering

        /// <summary>In reverse the pedals swap so brake = go backwards, matching arcade convention.</summary>
        private void ResolvePedals(float forwardSpeed, out float throttle, out float brake)
        {
            if (_gear < 0)
            {
                throttle = _cmd.Brake;
                brake = _cmd.Throttle;
            }
            else
            {
                throttle = _cmd.Throttle;
                brake = _cmd.Brake;
            }
        }

        private void UpdateSteering(float speed, float dt)
        {
            var h = _stats.Handling;
            float speedFactor = Mathf.Clamp01(speed / (_stats.Chassis.TopSpeedKmh * MathUtil.KmhToMs));
            float maxAngle = h.MaxSteerAngleDeg * (1f - h.HighSpeedSteerReduction * Mathf.Pow(speedFactor, 0.7f));
            float target = _cmd.Steer * maxAngle;
            _steerAngle = MathUtil.Damp(_steerAngle, target, h.SteerResponse, dt);
            for (int i = 0; i < _wheels.Length; i++)
                _wheels[i].SteerAngleDeg = _wheels[i].IsSteer ? _steerAngle : 0f;
        }

        // ------------------------------------------------------------------ transmission

        private void UpdateTransmission(float throttle, float forwardSpeed, float dt)
        {
            _timeSinceShift += dt;
            if (_shiftTimer > 0f)
            {
                _shiftTimer -= dt;
                return;
            }

            var t = _stats.Transmission;
            float redline = _stats.Engine.RedlineRpm;
            float absSpeed = Mathf.Abs(forwardSpeed);

            if (TransmissionMode == TransmissionMode.Manual)
            {
                if (_cmd.ShiftUp)
                {
                    if (_gear < 0) { if (absSpeed < 2f) ChangeGear(1); }
                    else if (_gear < t.GearCount) ChangeGear(_gear + 1);
                }
                else if (_cmd.ShiftDown)
                {
                    if (_gear > 1) ChangeGear(_gear - 1);
                    else if (_gear == 1 && absSpeed < 2f) ChangeGear(-1);
                }
                return;
            }

            // Automatic: reverse engagement when stopped and braking, forward when stopped and accelerating.
            if (_gear == 1 && absSpeed < 0.6f && _cmd.Brake > 0.5f && _cmd.Throttle < 0.1f)
            {
                ChangeGear(-1, silent: true);
                return;
            }
            if (_gear == -1 && absSpeed < 0.6f && _cmd.Throttle > 0.5f && _cmd.Brake < 0.1f)
            {
                ChangeGear(1, silent: true);
                return;
            }
            if (_gear < 1 || _timeSinceShift < MinShiftInterval || HoldBrakes) return;

            // Decisions follow road speed; wheelspin may pull the shift point forward by at most 25%,
            // so a burnout upshifts like a real automatic while airborne wheels never do.
            float decisionRpm = Mathf.Max(_roadRpm, Mathf.Min(_rpm, _roadRpm * 1.25f));
            float upshiftRpm = redline * Mathf.Lerp(0.72f, 0.93f, throttle);
            if (decisionRpm > upshiftRpm && _gear < t.GearCount && forwardSpeed > 1f)
            {
                ChangeGear(_gear + 1);
                return;
            }
            if (_gear > 1)
            {
                // Downshift only when the lower gear would not be near the redline, and never right
                // after an upshift (the rev drop between gears would otherwise cause hunting).
                float lowerRatio = t.GearRatios[_gear - 2] / t.GearRatios[_gear - 1];
                float rpmAfterDown = decisionRpm * lowerRatio;
                float downshiftRpm = redline * (throttle > 0.7f ? 0.45f : 0.33f);
                bool recentUpshift = _lastShiftWasUp && _timeSinceShift < 1.5f;
                if (decisionRpm < downshiftRpm && rpmAfterDown < redline * 0.85f && (!recentUpshift || decisionRpm < redline * 0.3f))
                    ChangeGear(_gear - 1);
            }
        }

        private void ChangeGear(int newGear, bool silent = false)
        {
            if (newGear == _gear) return;
            int from = _gear;
            float rpmAtShift = _rpm;
            _gear = newGear;
            _timeSinceShift = 0f;
            _lastShiftWasUp = newGear > from;
            _shiftTimer = silent ? 0f : _stats.Transmission.ShiftTimeSeconds;
            _freeRpm = _rpm;
            if (silent) return;

            var quality = newGear > from ? JudgeShift(rpmAtShift) : ShiftQuality.Good;
            Shifted?.Invoke(from, newGear, rpmAtShift, quality);
        }

        /// <summary>Perfect window sits just under the redline; too early wastes power, too late hits the limiter.</summary>
        private ShiftQuality JudgeShift(float rpm)
        {
            float redline = _stats.Engine.RedlineRpm;
            float n = rpm / redline;
            if (n >= 0.9f && n <= 0.985f) return ShiftQuality.Perfect;
            if (n >= 0.8f && n < 0.9f) return ShiftQuality.Good;
            if (n < 0.8f) return ShiftQuality.Early;
            return ShiftQuality.Late;
        }

        // ------------------------------------------------------------------ engine

        /// <summary>Returns total torque at the driven wheels (Nm) for this step.</summary>
        private float UpdateEngine(float throttle, float forwardSpeed, float dt)
        {
            var e = _stats.Engine;
            var t = _stats.Transmission;
            float absSpeed = Mathf.Abs(forwardSpeed);

            // Effective throttle after the top-speed limiter; nothing drives the wheels during a launch hold.
            float topSpeedMs = _stats.Chassis.TopSpeedKmh * MathUtil.KmhToMs;
            float speedLimiter = Mathf.Clamp01((topSpeedMs - absSpeed) / (topSpeedMs * 0.03f));
            float effThrottle = HoldBrakes ? 0f : throttle * speedLimiter;

            bool inGear = _gear != 0 && _shiftTimer <= 0f && !HoldBrakes;
            float ratio = 0f;
            if (_gear > 0) ratio = t.GearRatios[Mathf.Clamp(_gear - 1, 0, t.GearCount - 1)] * t.FinalDrive;
            else if (_gear < 0) ratio = t.ReverseRatio * t.FinalDrive;

            // Engaged rpm from driven wheel surface speed (including wheel spin); road rpm without spin.
            float drivenSurfaceSpeed = 0f;
            float drivenRoadSpeed = 0f;
            int drivenGrounded = 0;
            for (int i = 0; i < _wheels.Length; i++)
            {
                var w = _wheels[i];
                if (!w.IsDriven) continue;
                float road = w.Grounded ? Mathf.Abs(w.LongVelocity) : absSpeed;
                drivenRoadSpeed += road;
                drivenSurfaceSpeed += road + w.SpinSpeed;
                drivenGrounded++;
            }
            if (drivenGrounded > 0)
            {
                drivenSurfaceSpeed /= drivenGrounded;
                drivenRoadSpeed /= drivenGrounded;
            }
            float radius = Mathf.Max(0.2f, _stats.Tires.WheelRadiusM);
            // Spinning wheels can imply absurd rpm; the real engine hits the limiter long before.
            float engagedRpm = Mathf.Min(drivenSurfaceSpeed / radius * MathUtil.RadPerSecToRpm * ratio, e.LimiterRpm + 300f);
            _roadRpm = drivenRoadSpeed / radius * MathUtil.RadPerSecToRpm * ratio;

            // Auto clutch slips at low speed so the engine can rev for a launch.
            float clutch = inGear ? Mathf.Clamp01((absSpeed - 0.3f) / ClutchFullMs) : 0f;

            // Free-revving engine (clutch open or slipping) integrates torque against inertia.
            _limiterCut = _rpm > e.LimiterRpm;
            float freeThrottle = (_shiftTimer > 0f || _limiterCut) ? 0f : (HoldBrakes ? throttle : effThrottle);
            float freeTorque = _stats.EvaluateTorque(_freeRpm) * freeThrottle - e.EngineBrakingNm * (_freeRpm / e.RedlineRpm) * (1f - freeThrottle) - 8f;
            float freeAccel = freeTorque / Mathf.Max(0.05f, e.EngineInertia); // rad/s^2
            float prevFreeRpm = _freeRpm;
            _freeRpm += freeAccel * MathUtil.RadPerSecToRpm * dt;
            _freeRpm = Mathf.Clamp(_freeRpm, e.IdleRpm, e.LimiterRpm + 150f);

            // A slipping clutch under load behaves like a torque converter: unless the driver held
            // the revs before launch, the engine cannot rise above a throttle-dependent stall speed,
            // and revs held above it bleed off as the clutch bites.
            if (inGear && clutch < 0.999f)
            {
                float stallCap = Mathf.Lerp(e.IdleRpm, e.RedlineRpm * 0.6f, effThrottle);
                float ceiling = Mathf.Max(stallCap, prevFreeRpm - 2500f * dt);
                if (_freeRpm > ceiling) _freeRpm = ceiling;
            }

            float targetRpm = inGear ? Mathf.Lerp(_freeRpm, Mathf.Max(e.IdleRpm, engagedRpm), clutch) : _freeRpm;
            _rpm = Mathf.Lerp(_rpm, targetRpm, 1f - Mathf.Exp(-25f * dt));
            if (clutch >= 0.999f) _freeRpm = _rpm; // keep free rev continuous for the next shift
            if (_limiterCut) LimiterHit?.Invoke();

            // Turbo spools with throttle and rpm, dumps quickly off throttle.
            float boostTarget = effThrottle * Mathf.Clamp01((_rpm - e.IdleRpm) / (e.RedlineRpm * 0.35f));
            float spoolRate = boostTarget > _turboBoost ? 1f / Mathf.Max(0.05f, e.TurboSpoolSeconds) : 4f;
            _turboBoost = Mathf.MoveTowards(_turboBoost, boostTarget, spoolRate * dt);

            // Nitrous
            var n = _stats.Nitrous;
            bool wantNitrous = _cmd.Nitrous && n.IsFitted && _nitrousRemaining > 0.02f && effThrottle > 0.3f && _gear > 0;
            if (wantNitrous != _nitrousActive)
            {
                _nitrousActive = wantNitrous;
                NitrousChanged?.Invoke(_nitrousActive);
            }
            if (_nitrousActive) _nitrousRemaining = Mathf.Max(0f, _nitrousRemaining - dt);
            else if (n.RefillPerSecond > 0f) _nitrousRemaining = Mathf.Min(n.CapacitySeconds, _nitrousRemaining + n.CapacitySeconds * n.RefillPerSecond * dt);

            // Torque to wheels
            float engineTorque = 0f;
            if (inGear)
            {
                float cutThrottle = _limiterCut ? 0f : effThrottle;
                engineTorque = _stats.EvaluateTorque(_rpm) * cutThrottle;
                engineTorque *= 1f + (e.TurboBoostMultiplier - 1f) * _turboBoost;
                if (_nitrousActive) engineTorque *= n.PowerMultiplier;
                if (cutThrottle < 0.05f && absSpeed > 1f)
                    engineTorque = -e.EngineBrakingNm * (_rpm / e.RedlineRpm); // engine braking
                // Slipping clutch limits what can be transmitted at a standstill.
                float clutchCapacity = Mathf.Lerp(0.55f, 1f, clutch);
                engineTorque *= clutchCapacity;
            }
            _telemetry.EngineTorqueNm = engineTorque;
            _telemetry.TurboBoost01 = _turboBoost;
            return engineTorque * ratio * t.DrivelineEfficiency * (_gear < 0 ? -1f : 1f);
        }

        // ------------------------------------------------------------------ suspension

        private void UpdateSuspension(WheelState w, float dt)
        {
            var s = _stats.Suspension;
            Vector3 attach = transform.TransformPoint(w.LocalAttachPoint);
            Vector3 down = -transform.up;
            float rayLength = s.TravelM + w.Radius;

            bool wasGrounded = w.Grounded;
            w.PrevCompression = w.Compression;
            if (Physics.Raycast(attach, down, out RaycastHit hit, rayLength, GameLayers.GroundMask, QueryTriggerInteraction.Ignore))
            {
                w.Grounded = true;
                w.HitDistance = hit.distance;
                w.ContactPoint = hit.point;
                w.ContactNormal = hit.normal;
                w.Compression = Mathf.Clamp01(1f - (hit.distance - w.Radius) / s.TravelM);
                // Landing: compression jumps from zero in one step; a damper reading that jump would
                // catapult the car, so the first contact frame is spring-only.
                if (!wasGrounded) w.PrevCompression = w.Compression;

                // Preload: the static quarter-weight is carried at mid-travel, so the authored ride
                // height is the equilibrium and SpringRate only changes stiffness, not stance.
                float staticShare = _rb.mass * 9.81f / Mathf.Max(1, _wheels.Length);
                float springForce = staticShare + s.SpringRate * ((w.Compression - 0.5f) * s.TravelM);
                float damperForce = s.Damping * ((w.Compression - w.PrevCompression) * s.TravelM / dt);
                damperForce = Mathf.Clamp(damperForce, -staticShare * 2f, staticShare * 3f);
                // Progressive bump stop over the last 15% of travel so hard landings never push the
                // wheel through the road (which would drop the ray origin below a one-sided surface).
                float bump = w.Compression > 0.85f ? (w.Compression - 0.85f) / 0.15f : 0f;
                float bumpForce = bump * bump * staticShare * 8f;
                float load = Mathf.Max(0f, springForce + damperForce + bumpForce);
                w.Load = load;
                _rb.AddForceAtPosition(-down * load, attach, ForceMode.Force);

                w.ContactVelocity = _rb.GetPointVelocity(hit.point);
            }
            else
            {
                w.Grounded = false;
                w.Load = 0f;
                w.Compression = 0f;
                w.ContactVelocity = Vector3.zero;
                w.HitDistance = rayLength;
            }
        }

        private void ApplyAntiRoll()
        {
            float k = _stats.Suspension.AntiRoll;
            if (k <= 0f) return;
            ApplyAntiRollAxle(true, k);
            ApplyAntiRollAxle(false, k);
        }

        private void ApplyAntiRollAxle(bool front, float k)
        {
            WheelState left = null, right = null;
            for (int i = 0; i < _wheels.Length; i++)
            {
                var w = _wheels[i];
                if (w.IsFront != front) continue;
                if (w.IsLeft) left = w; else right = w;
            }
            if (left == null || right == null) return;
            // The bar moves load toward the more compressed side: extra upward push there, less on the other.
            float travel = _stats.Suspension.TravelM;
            float force = (left.Compression - right.Compression) * travel * k;
            if (left.Grounded) _rb.AddForceAtPosition(transform.up * force, transform.TransformPoint(left.LocalAttachPoint), ForceMode.Force);
            if (right.Grounded) _rb.AddForceAtPosition(transform.up * -force, transform.TransformPoint(right.LocalAttachPoint), ForceMode.Force);
        }

        // ------------------------------------------------------------------ tyres

        private void UpdateTyre(WheelState w, float driveTorque, float brake, bool handbrake, float dt)
        {
            var tire = _stats.Tires;
            w.DriveForce = 0f;
            w.BrakeForce = 0f;
            w.Locked = false;

            Quaternion steer = Quaternion.AngleAxis(w.SteerAngleDeg, transform.up);
            Vector3 fwd = steer * transform.forward;
            if (!w.Grounded)
            {
                w.Forward = fwd;
                w.Right = steer * transform.right;
                w.LongVelocity = Vector3.Dot(_rb.linearVelocity, fwd);
                w.LatVelocity = 0f;
                w.SpinSpeed = Mathf.MoveTowards(w.SpinSpeed, driveTorque > 0f ? 25f : 0f, SpinAccel * dt);
                w.SlipRatio = 0f;
                w.SlipAmount = 0f;
                return;
            }

            // Project wheel axes onto the contact plane.
            Vector3 n = w.ContactNormal;
            fwd = Vector3.ProjectOnPlane(fwd, n).normalized;
            Vector3 right = Vector3.Cross(n, fwd).normalized;
            w.Forward = fwd;
            w.Right = right;

            Vector3 v = w.ContactVelocity;
            float vLong = Vector3.Dot(v, fwd);
            float vLat = Vector3.Dot(v, right);
            w.LongVelocity = vLong;
            w.LatVelocity = vLat;
            float absLong = Mathf.Abs(vLong);

            float load = w.Load;
            float latLimit = load * tire.LateralGrip;
            float longLimit = load * tire.LongitudinalGrip;

            // ---- longitudinal: drive
            float driveRequest = driveTorque / Mathf.Max(0.2f, w.Radius);
            // Traction control (part of the stability assist) caps how far torque may exceed grip.
            float assist = _stats.Handling.StabilityAssist;
            if (assist > 0.2f)
            {
                float cap = longLimit * (1f + 0.25f * (1f - assist));
                driveRequest = Mathf.Clamp(driveRequest, -cap, cap);
            }
            float demand = Mathf.Abs(driveRequest) / Mathf.Max(1f, longLimit);
            if (demand > 1f) w.SpinSpeed += (demand - 1f) * SpinAccel * dt;
            else w.SpinSpeed = Mathf.Max(0f, w.SpinSpeed - w.SpinSpeed * SpinDecay * dt - 2f * dt);
            w.SpinSpeed = Mathf.Min(w.SpinSpeed, 40f);
            bool spinning = w.SpinSpeed > 0.5f;
            float slipRatio = w.SpinSpeed / Mathf.Max(2f, absLong);
            // Grip falls off progressively with spin: a chirp keeps most of it, a burnout does not.
            float spinGrip = Mathf.Lerp(1f, tire.SlideGripFraction, Mathf.Clamp01(w.SpinSpeed / 8f));
            float longForce = spinning
                ? Mathf.Sign(driveRequest) * longLimit * spinGrip
                : Mathf.Clamp(driveRequest, -longLimit, longLimit);

            // ---- longitudinal: brakes (oppose motion)
            var brakes = _stats.Brakes;
            float brakeTorque = brake * brakes.BrakeTorqueNm * (w.IsFront ? brakes.BrakeBias : 1f - brakes.BrakeBias) * 0.5f;
            if (handbrake && !w.IsFront) brakeTorque += brakes.HandbrakeTorqueNm * 0.5f;
            float brakeForce = brakeTorque / Mathf.Max(0.2f, w.Radius);
            float lateralGripScale = 1f;
            if (brakeForce > 0f && absLong > 0.2f)
            {
                bool abs = _stats.Handling.StabilityAssist > 0.2f && !handbrake;
                float lockLimit = longLimit * (abs ? 0.95f : 1f);
                if (brakeForce >= lockLimit && !abs)
                {
                    w.Locked = true;
                    brakeForce = longLimit * tire.SlideGripFraction;
                    lateralGripScale = tire.SlideGripFraction * 0.8f;
                }
                else brakeForce = Mathf.Min(brakeForce, lockLimit);
                longForce += -Mathf.Sign(vLong) * brakeForce;
                w.BrakeForce = brakeForce;
            }
            else if (brakeForce > 0f)
            {
                // Holding the car still: cancel residual creep.
                longForce += -vLong * load * 2f;
                w.BrakeForce = brakeForce;
            }
            if (handbrake && !w.IsFront) lateralGripScale = Mathf.Min(lateralGripScale, 0.45f);
            if (spinning) lateralGripScale = Mathf.Min(lateralGripScale, spinGrip);

            // ---- lateral
            float slipAngle = Mathf.Atan2(Mathf.Abs(vLat), Mathf.Max(absLong, 0.5f)) * Mathf.Rad2Deg;
            float x = slipAngle / Mathf.Max(1f, tire.PeakSlipAngleDeg);
            float curve = x <= 1f ? x * (2f - x) : Mathf.Lerp(1f, tire.SlideGripFraction, Mathf.Clamp01((x - 1f) * 0.5f));
            float latForce = -Mathf.Sign(vLat) * latLimit * curve * lateralGripScale;
            // Low speed: proportional damping avoids slip-angle noise and stops sideways creep.
            float lowSpeedForce = -vLat * load * 3f;
            float blend = Mathf.Clamp01(absLong / LowSpeedBlendMs);
            latForce = Mathf.Lerp(Mathf.Clamp(lowSpeedForce, -latLimit, latLimit), latForce, blend);

            // ---- friction ellipse
            float limit = load * Mathf.Max(tire.LateralGrip, tire.LongitudinalGrip);
            float mag = Mathf.Sqrt(longForce * longForce + latForce * latForce);
            if (mag > limit && mag > 1e-3f)
            {
                float scale = limit / mag;
                longForce *= scale;
                latForce *= scale;
            }

            _rb.AddForceAtPosition(fwd * longForce + right * latForce, w.ContactPoint, ForceMode.Force);

            w.DriveForce = longForce;
            w.SlipRatio = slipRatio;
            w.SlipAngleDeg = slipAngle;
            float latSlip = Mathf.Clamp01((x - 0.8f) / 1.2f) * blend;
            float longSlip = Mathf.Clamp01(slipRatio);
            float slip = latSlip > longSlip ? latSlip : longSlip;
            if (w.Locked) slip = 1f;
            w.SlipAmount = slip * Mathf.Clamp01(load / (_rb.mass * 2.45f));
        }

        // ------------------------------------------------------------------ body forces

        private void ApplyAerodynamics(Vector3 localVel)
        {
            var c = _stats.Chassis;
            float v2 = _rb.linearVelocity.sqrMagnitude;
            if (v2 < 0.01f) return;
            Vector3 dir = _rb.linearVelocity.normalized;
            float drag = 0.5f * 1.225f * c.DragCoefficient * c.FrontalAreaM2 * v2;
            _rb.AddForce(-dir * drag, ForceMode.Force);
            float rolling = _stats.Tires.RollingResistance * _rb.mass * 9.81f * Mathf.Clamp01(v2 / 4f);
            _rb.AddForce(-dir * rolling, ForceMode.Force);
            if (c.DownforceCoefficient > 0f)
                _rb.AddForce(-transform.up * (c.DownforceCoefficient * localVel.z * localVel.z), ForceMode.Force);
        }

        private void ApplyStabilityAssist(float forwardSpeed, int grounded)
        {
            float assist = _stats.Handling.StabilityAssist;
            if (assist <= 0f || grounded < 2) return;
            float yawRate = Vector3.Dot(_rb.angularVelocity, transform.up);
            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 12f);
            float torque = -yawRate * assist * _rb.mass * 0.6f * speedFactor;
            _rb.AddTorque(transform.up * torque, ForceMode.Force);
        }

        private void UpdateUpsideDown(int grounded, float dt)
        {
            bool upsideDown = transform.up.y < 0.15f && grounded == 0 && _rb.linearVelocity.sqrMagnitude < 4f;
            _upsideDownTime = upsideDown ? _upsideDownTime + dt : 0f;
            _telemetry.IsUpsideDown = _upsideDownTime > 2f;
        }

        private void UpdateWheelVisualState(float dt)
        {
            float travel = _stats.Suspension.TravelM;
            for (int i = 0; i < _wheels.Length; i++)
            {
                var w = _wheels[i];
                float surface = w.Locked ? 0f : w.LongVelocity + (w.IsDriven ? w.SpinSpeed * Mathf.Sign(_gear >= 0 ? 1f : -1f) : 0f);
                w.AngularVelocity = surface / Mathf.Max(0.2f, w.Radius);
                w.RotationDeg = Mathf.Repeat(w.RotationDeg + w.AngularVelocity * Mathf.Rad2Deg * dt, 360f);
                float hubBelowAttach = w.Grounded ? (w.HitDistance - w.Radius) : travel;
                w.VisualHubLocalY = w.LocalAttachPoint.y - Mathf.Clamp(hubBelowAttach, 0f, travel);
            }
        }

        private void FillTelemetry(Vector3 localVel, float throttle, float brake, int grounded, float maxSlip, float totalDrive, float dt)
        {
            var e = _stats.Engine;
            _telemetry.LocalVelocity = localVel;
            _telemetry.SpeedMs = localVel.z;
            _telemetry.SpeedKmh = _rb.linearVelocity.magnitude * MathUtil.MsToKmh;
            _telemetry.Rpm = _rpm;
            _telemetry.RpmNormalized = Mathf.InverseLerp(e.IdleRpm, e.RedlineRpm, _rpm);
            _telemetry.Gear = _gear;
            _telemetry.IsShifting = _shiftTimer > 0f;
            _telemetry.LimiterActive = _limiterCut;
            _telemetry.Throttle = throttle;
            _telemetry.Brake = brake;
            _telemetry.Steer = _stats.Handling.MaxSteerAngleDeg > 0f ? _steerAngle / _stats.Handling.MaxSteerAngleDeg : 0f;
            _telemetry.Handbrake = _cmd.Handbrake;
            _telemetry.Nitrous01 = _stats.Nitrous.IsFitted ? _nitrousRemaining / _stats.Nitrous.CapacitySeconds : 0f;
            _telemetry.NitrousActive = _nitrousActive;
            _telemetry.DriveForceN = totalDrive;
            _telemetry.GroundedWheels = grounded;
            _telemetry.IsAirborne = grounded == 0;
            _telemetry.MaxSlip = maxSlip;
            Vector3 flatVel = new Vector3(localVel.x, 0f, localVel.z);
            _telemetry.DriftAngleDeg = flatVel.sqrMagnitude > 4f ? Vector3.SignedAngle(Vector3.forward, flatVel, Vector3.up) : 0f;
            _telemetry.IsDrifting = grounded >= 2 && Mathf.Abs(_telemetry.DriftAngleDeg) > 12f && flatVel.magnitude > 8f;
            _telemetry.YawRate = Vector3.Dot(_rb.angularVelocity, transform.up);
            Vector3 accel = (_rb.linearVelocity - _prevVelocity) / dt;
            _telemetry.LocalAcceleration = transform.InverseTransformDirection(accel);
            _prevVelocity = _rb.linearVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsInitialized) return;
            float impulse = collision.impulse.magnitude / Mathf.Max(1f, _rb.mass);
            if (impulse < 0.5f) return;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Collided?.Invoke(impulse, point);
        }
    }
}
