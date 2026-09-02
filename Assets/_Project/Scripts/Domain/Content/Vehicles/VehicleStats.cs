using System;
using UnityEngine;

namespace RedlineLegends.Vehicles
{
    public enum DrivetrainType
    {
        FWD,
        RWD,
        AWD
    }

    public enum VehicleClass
    {
        Street,
        Sport,
        Super,
        Hyper
    }

    [Serializable]
    public sealed class EngineStats
    {
        [Tooltip("Peak power, used for display and the performance rating.")]
        public float PeakPowerHp = 180f;
        [Tooltip("Peak torque at the crank in Nm. The torque curve scales this.")]
        public float PeakTorqueNm = 260f;
        public float IdleRpm = 900f;
        public float RedlineRpm = 7000f;
        [Tooltip("Rev limiter cuts fuel above this. Slightly above redline for over-rev feel.")]
        public float LimiterRpm = 7200f;
        [Tooltip("x = normalized rpm (0 = idle, 1 = redline), y = fraction of PeakTorqueNm.")]
        public AnimationCurve TorqueCurve = DefaultTorqueCurve();
        [Tooltip("How fast the engine revs with the clutch open (kg m^2). Lower = snappier.")]
        public float EngineInertia = 0.22f;
        [Tooltip("Engine braking torque at redline with closed throttle (Nm).")]
        public float EngineBrakingNm = 45f;
        [Tooltip("1 = no turbo. 1.25 = +25% torque at full boost.")]
        public float TurboBoostMultiplier = 1f;
        [Tooltip("Seconds for the turbo to reach full boost at wide-open throttle.")]
        public float TurboSpoolSeconds = 0.6f;

        public static AnimationCurve DefaultTorqueCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.35f, 0.9f),
                new Keyframe(0.6f, 1f),
                new Keyframe(0.85f, 0.92f),
                new Keyframe(1f, 0.75f));
        }

        public EngineStats Clone()
        {
            var c = (EngineStats)MemberwiseClone();
            c.TorqueCurve = TorqueCurve != null ? new AnimationCurve(TorqueCurve.keys) : DefaultTorqueCurve();
            return c;
        }
    }

    [Serializable]
    public sealed class TransmissionStats
    {
        public DrivetrainType Drivetrain = DrivetrainType.RWD;
        public float[] GearRatios = { 3.4f, 2.1f, 1.5f, 1.15f, 0.92f, 0.78f };
        public float ReverseRatio = 3.2f;
        public float FinalDrive = 3.9f;
        [Tooltip("Time the clutch is open during a shift.")]
        public float ShiftTimeSeconds = 0.22f;
        [Tooltip("Fraction of engine torque that reaches the wheels.")]
        public float DrivelineEfficiency = 0.88f;
        [Tooltip("AWD only: fraction of drive torque sent to the front axle.")]
        [Range(0f, 1f)] public float AwdFrontTorqueSplit = 0.4f;

        public int GearCount => GearRatios != null ? GearRatios.Length : 0;

        public TransmissionStats Clone()
        {
            var c = (TransmissionStats)MemberwiseClone();
            c.GearRatios = (float[])GearRatios.Clone();
            return c;
        }
    }

    [Serializable]
    public sealed class ChassisStats
    {
        public float MassKg = 1300f;
        [Tooltip("Centre of mass in vehicle space. The vehicle origin is at ground level under the model, so this is the COM height above the road. Lower = more stable.")]
        public Vector3 CenterOfMassOffset = new Vector3(0f, 0.45f, 0.02f);
        public float DragCoefficient = 0.32f;
        public float FrontalAreaM2 = 2.2f;
        [Tooltip("Downforce in newtons per (m/s)^2. 0 for street cars.")]
        public float DownforceCoefficient = 0f;
        [Tooltip("Electronic limiter / gearing ceiling in km/h.")]
        public float TopSpeedKmh = 220f;

        public ChassisStats Clone() => (ChassisStats)MemberwiseClone();
    }

    [Serializable]
    public sealed class TireStats
    {
        [Tooltip("Peak lateral friction coefficient (1.0 = road tyre).")]
        public float LateralGrip = 1.05f;
        [Tooltip("Peak longitudinal friction coefficient.")]
        public float LongitudinalGrip = 1.1f;
        [Tooltip("Slip angle in degrees where lateral force peaks.")]
        public float PeakSlipAngleDeg = 9f;
        [Tooltip("Slip ratio where longitudinal force peaks.")]
        public float PeakSlipRatio = 0.14f;
        [Tooltip("Grip retained past the peak (0.7 = loses 30% when sliding).")]
        [Range(0.4f, 1f)] public float SlideGripFraction = 0.78f;
        public float WheelRadiusM = 0.33f;
        public float RollingResistance = 0.014f;

        public TireStats Clone() => (TireStats)MemberwiseClone();
    }

    [Serializable]
    public sealed class HandlingStats
    {
        public float MaxSteerAngleDeg = 32f;
        [Tooltip("Steering response sharpness (higher = snappier).")]
        public float SteerResponse = 7f;
        [Tooltip("Fraction of steering angle removed at top speed.")]
        [Range(0f, 0.9f)] public float HighSpeedSteerReduction = 0.62f;
        [Tooltip("Arcade yaw damping that keeps slides catchable. 0 = off.")]
        [Range(0f, 1f)] public float StabilityAssist = 0.35f;

        public HandlingStats Clone() => (HandlingStats)MemberwiseClone();
    }

    [Serializable]
    public sealed class BrakeStats
    {
        [Tooltip("Total brake torque at full pedal (Nm, all wheels).")]
        public float BrakeTorqueNm = 6500f;
        public float HandbrakeTorqueNm = 5000f;
        [Tooltip("Fraction of brake torque on the front axle.")]
        [Range(0.3f, 0.8f)] public float BrakeBias = 0.62f;

        public BrakeStats Clone() => (BrakeStats)MemberwiseClone();
    }

    [Serializable]
    public sealed class SuspensionStats
    {
        [Tooltip("Spring rate per wheel (N/m).")]
        public float SpringRate = 38000f;
        [Tooltip("Damping per wheel (Ns/m).")]
        public float Damping = 4200f;
        public float TravelM = 0.22f;
        [Tooltip("Body lift relative to the model's authored stance in metres (0 = as modelled; tuning lowers or raises it).")]
        public float RideHeightM = 0f;
        [Tooltip("Anti-roll stiffness (N/m of travel difference).")]
        public float AntiRoll = 9000f;

        public SuspensionStats Clone() => (SuspensionStats)MemberwiseClone();
    }

    [Serializable]
    public sealed class NitrousStats
    {
        [Tooltip("Seconds of boost in a full bottle. 0 = no nitrous fitted.")]
        public float CapacitySeconds = 0f;
        [Tooltip("Engine torque multiplier while active.")]
        public float PowerMultiplier = 1.3f;
        [Tooltip("Fraction of the bottle refilled per second while not boosting (0 = drag-style single use).")]
        public float RefillPerSecond = 0f;

        public bool IsFitted => CapacitySeconds > 0.01f;

        public NitrousStats Clone() => (NitrousStats)MemberwiseClone();
    }

    /// <summary>
    /// Complete physical description of a vehicle. Lives in a VehicleDefinition as base values,
    /// is cloned and modified by upgrades and tuning, and the result is what the VehicleController
    /// simulates. The controller never reads a definition asset directly.
    /// </summary>
    [Serializable]
    public sealed class VehicleStats
    {
        public EngineStats Engine = new EngineStats();
        public TransmissionStats Transmission = new TransmissionStats();
        public ChassisStats Chassis = new ChassisStats();
        public TireStats Tires = new TireStats();
        public HandlingStats Handling = new HandlingStats();
        public BrakeStats Brakes = new BrakeStats();
        public SuspensionStats Suspension = new SuspensionStats();
        public NitrousStats Nitrous = new NitrousStats();

        public VehicleStats Clone()
        {
            return new VehicleStats
            {
                Engine = Engine.Clone(),
                Transmission = Transmission.Clone(),
                Chassis = Chassis.Clone(),
                Tires = Tires.Clone(),
                Handling = Handling.Clone(),
                Brakes = Brakes.Clone(),
                Suspension = Suspension.Clone(),
                Nitrous = Nitrous.Clone()
            };
        }

        /// <summary>Crank torque at the given rpm and throttle, before turbo and nitrous.</summary>
        public float EvaluateTorque(float rpm)
        {
            float t = Mathf.InverseLerp(Engine.IdleRpm, Engine.RedlineRpm, rpm);
            return Engine.PeakTorqueNm * Mathf.Max(0f, Engine.TorqueCurve.Evaluate(t));
        }
    }

    /// <summary>
    /// A fully resolved vehicle ready to spawn: base stats with upgrades and tuning applied.
    /// This is what travels inside a RaceParticipantSpec.
    /// </summary>
    [Serializable]
    public sealed class VehicleSpec
    {
        public string VehicleId;
        public VehicleStats Stats;
        public int PerformanceRating;
    }
}
