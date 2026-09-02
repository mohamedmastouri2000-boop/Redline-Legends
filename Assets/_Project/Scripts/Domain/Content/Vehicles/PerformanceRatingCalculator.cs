using RedlineLegends.Utilities;
using UnityEngine;

namespace RedlineLegends.Vehicles
{
    /// <summary>
    /// Derives a single comparable number (100..999) from the resolved stats. It runs a cheap
    /// longitudinal acceleration simulation instead of trusting horsepower alone, so a heavy
    /// high-power car with poor grip rates honestly against a light grippy one.
    /// </summary>
    public static class PerformanceRatingCalculator
    {
        public const int Min = 100;
        public const int Max = 999;

        public struct Breakdown
        {
            public float ZeroToHundredSeconds;
            public float TopSpeedKmh;
            public float BrakingG;
            public float GripScore;
            public float HandlingScore;
            public float PowerToWeight;
            public int Rating;
        }

        public static int Compute(VehicleStats stats) => ComputeBreakdown(stats).Rating;

        public static Breakdown ComputeBreakdown(VehicleStats s)
        {
            var b = new Breakdown();
            b.ZeroToHundredSeconds = SimulateAcceleration(s, 100f * MathUtil.KmhToMs);
            b.TopSpeedKmh = EstimateTopSpeed(s);
            b.BrakingG = Mathf.Min(s.Tires.LongitudinalGrip * 0.95f,
                s.Brakes.BrakeTorqueNm / s.Tires.WheelRadiusM / (s.Chassis.MassKg * 9.81f));
            b.GripScore = s.Tires.LateralGrip;
            b.HandlingScore = s.Handling.SteerResponse * (1f - s.Handling.HighSpeedSteerReduction * 0.5f)
                              * (1f + s.Handling.StabilityAssist * 0.2f);
            b.PowerToWeight = s.Engine.PeakPowerHp * s.Engine.TurboBoostMultiplier / (s.Chassis.MassKg / 1000f);

            // Normalize each axis to 0..1 across the intended car roster (street hatch .. hyper car).
            float accel = Mathf.InverseLerp(11f, 2.4f, b.ZeroToHundredSeconds);
            float top = Mathf.InverseLerp(150f, 420f, b.TopSpeedKmh);
            float grip = Mathf.InverseLerp(0.85f, 1.7f, b.GripScore);
            float brake = Mathf.InverseLerp(0.7f, 1.5f, b.BrakingG);
            float handling = Mathf.InverseLerp(4f, 14f, b.HandlingScore);
            float ptw = Mathf.InverseLerp(60f, 800f, b.PowerToWeight);

            float score = accel * 0.30f + top * 0.20f + grip * 0.18f + brake * 0.10f + handling * 0.10f + ptw * 0.12f;
            b.Rating = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(Min, Max, Mathf.Clamp01(score))), Min, Max);
            return b;
        }

        /// <summary>Straight-line time from rest to targetSpeed with ideal shifts (traction limited).</summary>
        public static float SimulateAcceleration(VehicleStats s, float targetSpeedMs)
        {
            const float dt = 0.02f;
            const float maxTime = 30f;
            float mass = s.Chassis.MassKg;
            float radius = Mathf.Max(0.2f, s.Tires.WheelRadiusM);
            float drivenShare = DrivenWeightShare(s);
            float tractionLimit = s.Tires.LongitudinalGrip * mass * 9.81f * drivenShare;
            var ratios = s.Transmission.GearRatios;
            if (ratios == null || ratios.Length == 0) return maxTime;

            float v = 0f;
            float t = 0f;
            int gear = 0;
            float launchRpm = Mathf.Lerp(s.Engine.IdleRpm, s.Engine.RedlineRpm, 0.55f);
            while (v < targetSpeedMs && t < maxTime)
            {
                float overall = ratios[gear] * s.Transmission.FinalDrive;
                float rpm = v / radius * MathUtil.RadPerSecToRpm * overall;
                if (rpm < launchRpm) rpm = launchRpm; // clutch slip during launch
                if (rpm > s.Engine.RedlineRpm && gear < ratios.Length - 1)
                {
                    gear++;
                    t += s.Transmission.ShiftTimeSeconds;
                    continue;
                }
                float torque = s.EvaluateTorque(Mathf.Min(rpm, s.Engine.RedlineRpm)) * s.Engine.TurboBoostMultiplier;
                float driveForce = torque * overall * s.Transmission.DrivelineEfficiency / radius;
                driveForce = Mathf.Min(driveForce, tractionLimit);
                float drag = 0.5f * 1.225f * s.Chassis.DragCoefficient * s.Chassis.FrontalAreaM2 * v * v;
                float rolling = s.Tires.RollingResistance * mass * 9.81f;
                float a = (driveForce - drag - rolling) / mass;
                if (a <= 0.01f) break;
                v += a * dt;
                t += dt;
            }
            return v >= targetSpeedMs ? t : maxTime;
        }

        public static float EstimateTopSpeed(VehicleStats s)
        {
            // Speed where drive force in top gear equals drag, capped by the limiter.
            var ratios = s.Transmission.GearRatios;
            if (ratios == null || ratios.Length == 0) return s.Chassis.TopSpeedKmh;
            float radius = Mathf.Max(0.2f, s.Tires.WheelRadiusM);
            float best = 0f;
            for (int g = 0; g < ratios.Length; g++)
            {
                float overall = ratios[g] * s.Transmission.FinalDrive;
                float vAtRedline = s.Engine.RedlineRpm * MathUtil.RpmToRadPerSec / overall * radius;
                // Check whether drag at that speed can be overcome with peak torque in this gear.
                float force = s.Engine.PeakTorqueNm * s.Engine.TurboBoostMultiplier * 0.8f * overall
                              * s.Transmission.DrivelineEfficiency / radius;
                float drag = 0.5f * 1.225f * s.Chassis.DragCoefficient * s.Chassis.FrontalAreaM2 * vAtRedline * vAtRedline
                             + s.Tires.RollingResistance * s.Chassis.MassKg * 9.81f;
                float v = force >= drag ? vAtRedline
                    : Mathf.Sqrt(Mathf.Max(0f, (force - s.Tires.RollingResistance * s.Chassis.MassKg * 9.81f))
                                 / (0.5f * 1.225f * s.Chassis.DragCoefficient * s.Chassis.FrontalAreaM2));
                if (v > best) best = v;
            }
            return Mathf.Min(best * MathUtil.MsToKmh, s.Chassis.TopSpeedKmh);
        }

        public static float DrivenWeightShare(VehicleStats s)
        {
            switch (s.Transmission.Drivetrain)
            {
                case DrivetrainType.AWD: return 1f;
                case DrivetrainType.RWD: return 0.62f; // weight transfer helps
                default: return 0.5f;
            }
        }
    }
}
