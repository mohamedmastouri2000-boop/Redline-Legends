using System;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Tuning
{
    /// <summary>
    /// Player-adjustable setup, stored in the save file per owned car. All values are normalized
    /// (-1..1 or 0..1) so the same save data stays valid when a car's limits are rebalanced.
    /// </summary>
    [Serializable]
    public sealed class VehicleTuningData
    {
        [Range(-1f, 1f)] public float FinalDrive;
        /// <summary>Per gear, -1 shorter .. +1 taller. Missing entries mean 0.</summary>
        public float[] GearRatios = Array.Empty<float>();
        [Range(-1f, 1f)] public float SuspensionStiffness;
        [Range(-1f, 1f)] public float RideHeight;
        /// <summary>-1 = grip biased to front (understeer), +1 = to rear (oversteer).</summary>
        [Range(-1f, 1f)] public float GripBias;
        /// <summary>0 = long weak nitrous, 1 = short strong nitrous.</summary>
        [Range(0f, 1f)] public float NitrousBalance = 0.5f;

        public static VehicleTuningData Default() => new VehicleTuningData();

        public VehicleTuningData Clone()
        {
            var c = (VehicleTuningData)MemberwiseClone();
            c.GearRatios = GearRatios != null ? (float[])GearRatios.Clone() : Array.Empty<float>();
            return c;
        }

        public float GetGearRatioTune(int gearIndex)
        {
            if (GearRatios == null || gearIndex < 0 || gearIndex >= GearRatios.Length) return 0f;
            return Mathf.Clamp(GearRatios[gearIndex], -1f, 1f);
        }
    }

    /// <summary>How far each tuning slider may move the underlying stat for a given car.</summary>
    [Serializable]
    public sealed class TuningLimits
    {
        [Tooltip("Max fractional change of final drive at slider extremes (0.12 = +/-12%).")]
        public float FinalDriveRange = 0.12f;
        public float GearRatioRange = 0.10f;
        public float SuspensionStiffnessRange = 0.35f;
        [Tooltip("Ride height change in metres at slider extremes.")]
        public float RideHeightRangeM = 0.05f;
        [Tooltip("Grip moved between axles at slider extremes (0.08 = 8%).")]
        public float GripBiasRange = 0.08f;
        [Tooltip("Nitrous power/duration trade at extremes.")]
        public float NitrousBalanceRange = 0.25f;
    }

    /// <summary>Applies tuning to a stats clone. Called after upgrades in VehicleSpecBuilder.</summary>
    public static class VehicleTuningApplier
    {
        public static void Apply(VehicleStats stats, VehicleTuningData tuning, TuningLimits limits)
        {
            if (stats == null || tuning == null || limits == null) return;

            // Taller (+) means numerically lower ratio.
            stats.Transmission.FinalDrive *= 1f - Mathf.Clamp(tuning.FinalDrive, -1f, 1f) * limits.FinalDriveRange;
            var gears = stats.Transmission.GearRatios;
            for (int i = 0; i < gears.Length; i++)
                gears[i] *= 1f - tuning.GetGearRatioTune(i) * limits.GearRatioRange;

            float stiff = 1f + Mathf.Clamp(tuning.SuspensionStiffness, -1f, 1f) * limits.SuspensionStiffnessRange;
            stats.Suspension.SpringRate *= stiff;
            stats.Suspension.Damping *= Mathf.Sqrt(stiff);
            stats.Suspension.AntiRoll *= stiff;
            stats.Suspension.RideHeightM = Mathf.Clamp(
                stats.Suspension.RideHeightM + Mathf.Clamp(tuning.RideHeight, -1f, 1f) * limits.RideHeightRangeM, -0.06f, 0.1f);
            // Lower car = lower centre of mass.
            stats.Chassis.CenterOfMassOffset.y += Mathf.Clamp(tuning.RideHeight, -1f, 1f) * limits.RideHeightRangeM;

            // Grip bias is consumed by the vehicle controller as a per-axle multiplier; we store it
            // on the stats by nudging stability assist (rear bias = looser car).
            float bias = Mathf.Clamp(tuning.GripBias, -1f, 1f) * limits.GripBiasRange;
            stats.Handling.StabilityAssist = Mathf.Clamp01(stats.Handling.StabilityAssist - bias * 2f);
            stats.Tires.LateralGrip *= 1f - Mathf.Abs(bias) * 0.25f;

            if (stats.Nitrous.IsFitted)
            {
                float n = (Mathf.Clamp01(tuning.NitrousBalance) - 0.5f) * 2f; // -1..1
                float power = 1f + n * limits.NitrousBalanceRange;
                float extra = stats.Nitrous.PowerMultiplier - 1f;
                stats.Nitrous.PowerMultiplier = 1f + extra * power;
                stats.Nitrous.CapacitySeconds *= 1f - n * limits.NitrousBalanceRange;
            }
        }
    }
}
