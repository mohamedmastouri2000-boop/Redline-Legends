using RedlineLegends.Vehicles;

namespace RedlineLegends.Upgrades
{
    /// <summary>
    /// The one place that maps <see cref="VehicleStatId"/> to a concrete field. Upgrades and
    /// tuning both go through here so an upgrade genuinely changes the simulated car.
    /// </summary>
    public static class VehicleStatModifierApplier
    {
        public static void Apply(VehicleStats stats, in StatModifier modifier)
        {
            float current = Read(stats, modifier.Stat);
            float next;
            switch (modifier.Op)
            {
                case ModifierOp.Add: next = current + modifier.Value; break;
                case ModifierOp.Multiply: next = current * modifier.Value; break;
                default: next = modifier.Value; break;
            }
            Write(stats, modifier.Stat, next);
        }

        public static float Read(VehicleStats s, VehicleStatId id)
        {
            switch (id)
            {
                case VehicleStatId.PeakTorque: return s.Engine.PeakTorqueNm;
                case VehicleStatId.PeakPower: return s.Engine.PeakPowerHp;
                case VehicleStatId.Redline: return s.Engine.RedlineRpm;
                case VehicleStatId.TurboBoost: return s.Engine.TurboBoostMultiplier;
                case VehicleStatId.TurboSpool: return s.Engine.TurboSpoolSeconds;
                case VehicleStatId.EngineInertia: return s.Engine.EngineInertia;
                case VehicleStatId.MassKg: return s.Chassis.MassKg;
                case VehicleStatId.TopSpeedKmh: return s.Chassis.TopSpeedKmh;
                case VehicleStatId.Drag: return s.Chassis.DragCoefficient;
                case VehicleStatId.Downforce: return s.Chassis.DownforceCoefficient;
                case VehicleStatId.LateralGrip: return s.Tires.LateralGrip;
                case VehicleStatId.LongitudinalGrip: return s.Tires.LongitudinalGrip;
                case VehicleStatId.SlideGripFraction: return s.Tires.SlideGripFraction;
                case VehicleStatId.BrakeTorque: return s.Brakes.BrakeTorqueNm;
                case VehicleStatId.HandbrakeTorque: return s.Brakes.HandbrakeTorqueNm;
                case VehicleStatId.SteerResponse: return s.Handling.SteerResponse;
                case VehicleStatId.StabilityAssist: return s.Handling.StabilityAssist;
                case VehicleStatId.SpringRate: return s.Suspension.SpringRate;
                case VehicleStatId.Damping: return s.Suspension.Damping;
                case VehicleStatId.AntiRoll: return s.Suspension.AntiRoll;
                case VehicleStatId.ShiftTime: return s.Transmission.ShiftTimeSeconds;
                case VehicleStatId.DrivelineEfficiency: return s.Transmission.DrivelineEfficiency;
                case VehicleStatId.NitrousCapacity: return s.Nitrous.CapacitySeconds;
                case VehicleStatId.NitrousPower: return s.Nitrous.PowerMultiplier;
                case VehicleStatId.NitrousRefill: return s.Nitrous.RefillPerSecond;
                default: return 0f;
            }
        }

        public static void Write(VehicleStats s, VehicleStatId id, float v)
        {
            switch (id)
            {
                case VehicleStatId.PeakTorque: s.Engine.PeakTorqueNm = v; break;
                case VehicleStatId.PeakPower: s.Engine.PeakPowerHp = v; break;
                case VehicleStatId.Redline:
                    s.Engine.RedlineRpm = v;
                    if (s.Engine.LimiterRpm < v) s.Engine.LimiterRpm = v + 200f;
                    break;
                case VehicleStatId.TurboBoost: s.Engine.TurboBoostMultiplier = v; break;
                case VehicleStatId.TurboSpool: s.Engine.TurboSpoolSeconds = v; break;
                case VehicleStatId.EngineInertia: s.Engine.EngineInertia = v; break;
                case VehicleStatId.MassKg: s.Chassis.MassKg = v; break;
                case VehicleStatId.TopSpeedKmh: s.Chassis.TopSpeedKmh = v; break;
                case VehicleStatId.Drag: s.Chassis.DragCoefficient = v; break;
                case VehicleStatId.Downforce: s.Chassis.DownforceCoefficient = v; break;
                case VehicleStatId.LateralGrip: s.Tires.LateralGrip = v; break;
                case VehicleStatId.LongitudinalGrip: s.Tires.LongitudinalGrip = v; break;
                case VehicleStatId.SlideGripFraction: s.Tires.SlideGripFraction = v; break;
                case VehicleStatId.BrakeTorque: s.Brakes.BrakeTorqueNm = v; break;
                case VehicleStatId.HandbrakeTorque: s.Brakes.HandbrakeTorqueNm = v; break;
                case VehicleStatId.SteerResponse: s.Handling.SteerResponse = v; break;
                case VehicleStatId.StabilityAssist: s.Handling.StabilityAssist = v; break;
                case VehicleStatId.SpringRate: s.Suspension.SpringRate = v; break;
                case VehicleStatId.Damping: s.Suspension.Damping = v; break;
                case VehicleStatId.AntiRoll: s.Suspension.AntiRoll = v; break;
                case VehicleStatId.ShiftTime: s.Transmission.ShiftTimeSeconds = v; break;
                case VehicleStatId.DrivelineEfficiency: s.Transmission.DrivelineEfficiency = v; break;
                case VehicleStatId.NitrousCapacity: s.Nitrous.CapacitySeconds = v; break;
                case VehicleStatId.NitrousPower: s.Nitrous.PowerMultiplier = v; break;
                case VehicleStatId.NitrousRefill: s.Nitrous.RefillPerSecond = v; break;
            }
        }
    }
}
