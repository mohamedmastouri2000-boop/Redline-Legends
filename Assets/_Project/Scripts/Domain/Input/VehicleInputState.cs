using System;

namespace RedlineLegends.Input
{
    /// <summary>
    /// The complete command set a vehicle understands. Every input source (touch, AI, replay,
    /// network) produces this and nothing else, so the vehicle never knows where input came from.
    /// Axis values are already smoothed/normalized by the provider. Button edges (ShiftUp,
    /// ShiftDown, Reset) are latched by the provider until sampled by the vehicle's physics step.
    /// </summary>
    [Serializable]
    public struct VehicleInputState
    {
        /// <summary>-1 = full left, +1 = full right.</summary>
        public float Steer;
        /// <summary>0..1</summary>
        public float Throttle;
        /// <summary>0..1; also engages reverse when stopped.</summary>
        public float Brake;
        public bool Handbrake;
        public bool Nitrous;
        public bool ShiftUp;
        public bool ShiftDown;
        public bool ResetVehicle;

        public static readonly VehicleInputState Neutral = default;

        public VehicleInputState Clamped()
        {
            var s = this;
            s.Steer = s.Steer < -1f ? -1f : (s.Steer > 1f ? 1f : s.Steer);
            s.Throttle = s.Throttle < 0f ? 0f : (s.Throttle > 1f ? 1f : s.Throttle);
            s.Brake = s.Brake < 0f ? 0f : (s.Brake > 1f ? 1f : s.Brake);
            return s;
        }
    }
}
