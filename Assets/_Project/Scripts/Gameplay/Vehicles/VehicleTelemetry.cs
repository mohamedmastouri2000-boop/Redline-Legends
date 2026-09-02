using UnityEngine;

namespace RedlineLegends.Vehicles
{
    public enum ShiftQuality
    {
        Perfect,
        Good,
        Early,
        Late
    }

    /// <summary>
    /// Read-only snapshot of the vehicle for HUD, audio, VFX, cameras and AI. Refreshed every
    /// physics step; consumers never poke the controller internals.
    /// </summary>
    public struct VehicleTelemetry
    {
        public float SpeedMs;             // signed along vehicle forward
        public float SpeedKmh;            // absolute
        public float Rpm;
        public float RpmNormalized;       // 0 at idle .. 1 at redline
        public int Gear;                  // -1 reverse, 0 neutral, 1..n
        public bool IsShifting;
        public bool LimiterActive;
        public float Throttle;            // effective throttle after limiter/reverse handling
        public float Brake;
        public float Steer;               // -1..1 smoothed
        public bool Handbrake;
        public float TurboBoost01;
        public float Nitrous01;           // bottle level
        public bool NitrousActive;
        public float EngineTorqueNm;
        public float DriveForceN;
        public int GroundedWheels;
        public bool IsAirborne;
        public bool IsUpsideDown;
        public float MaxSlip;             // worst wheel slip 0..1
        public float DriftAngleDeg;       // angle between velocity and forward on the ground plane
        public bool IsDrifting;
        public Vector3 LocalVelocity;
        public Vector3 LocalAcceleration;
        public float YawRate;             // rad/s
        public float TopSpeedKmh;
        public float RedlineRpm;
        public float IdleRpm;
        public int GearCount;
    }
}
