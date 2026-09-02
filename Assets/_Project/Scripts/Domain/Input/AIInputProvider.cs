using RedlineLegends.Race;

namespace RedlineLegends.Input
{
    /// <summary>
    /// Mailbox between an AI driver and its vehicle. The driver writes a desired state; button
    /// edges accumulate until the vehicle samples them so a shift requested between physics
    /// steps is never lost.
    /// </summary>
    public sealed class AIInputProvider : IInputProvider
    {
        private VehicleInputState _pending;
        private bool _shiftUp;
        private bool _shiftDown;
        private bool _reset;

        public ControlSource Source => ControlSource.AI;
        public bool Enabled { get; set; } = true;

        public void Tick(float deltaTime) { }

        public void SetAxes(float steer, float throttle, float brake, bool handbrake, bool nitrous)
        {
            _pending.Steer = steer;
            _pending.Throttle = throttle;
            _pending.Brake = brake;
            _pending.Handbrake = handbrake;
            _pending.Nitrous = nitrous;
        }

        public void RequestShiftUp() => _shiftUp = true;
        public void RequestShiftDown() => _shiftDown = true;
        public void RequestReset() => _reset = true;

        public VehicleInputState Sample()
        {
            if (!Enabled)
            {
                _shiftUp = _shiftDown = _reset = false;
                return VehicleInputState.Neutral;
            }
            var s = _pending.Clamped();
            s.ShiftUp = _shiftUp;
            s.ShiftDown = _shiftDown;
            s.ResetVehicle = _reset;
            _shiftUp = _shiftDown = _reset = false;
            return s;
        }

        public VehicleInputState Peek()
        {
            var s = _pending.Clamped();
            s.ShiftUp = _shiftUp;
            s.ShiftDown = _shiftDown;
            s.ResetVehicle = _reset;
            return Enabled ? s : VehicleInputState.Neutral;
        }
    }
}
