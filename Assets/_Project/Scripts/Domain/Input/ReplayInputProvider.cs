using System;
using System.Collections.Generic;
using RedlineLegends.Race;

namespace RedlineLegends.Input
{
    /// <summary>Fixed-step input trace. Frame index is the physics step number since race start.</summary>
    [Serializable]
    public sealed class InputRecording
    {
        [Serializable]
        public struct Frame
        {
            public int Step;
            public VehicleInputState State;
        }

        public string VehicleId;
        public float FixedTimestep;
        public List<Frame> Frames = new List<Frame>();

        /// <summary>Appends only when the state changed (edges always recorded), keeping traces small.</summary>
        public void Record(int step, in VehicleInputState state)
        {
            if (Frames.Count > 0)
            {
                var last = Frames[Frames.Count - 1].State;
                bool same = last.Steer == state.Steer && last.Throttle == state.Throttle && last.Brake == state.Brake
                            && last.Handbrake == state.Handbrake && last.Nitrous == state.Nitrous
                            && !state.ShiftUp && !state.ShiftDown && !state.ResetVehicle
                            && !last.ShiftUp && !last.ShiftDown && !last.ResetVehicle;
                if (same) return;
            }
            Frames.Add(new Frame { Step = step, State = state });
        }
    }

    /// <summary>Wraps another provider and records everything it emits (ghost laps, bug repros).</summary>
    public sealed class RecordingInputProvider : IInputProvider
    {
        private readonly IInputProvider _inner;
        private int _step;

        public InputRecording Recording { get; }
        public ControlSource Source => _inner.Source;
        public bool Enabled { get => _inner.Enabled; set => _inner.Enabled = value; }

        public RecordingInputProvider(IInputProvider inner, InputRecording recording)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Recording = recording ?? throw new ArgumentNullException(nameof(recording));
        }

        public void Tick(float deltaTime) => _inner.Tick(deltaTime);

        public VehicleInputState Sample()
        {
            var state = _inner.Sample();
            Recording.Record(_step++, state);
            return state;
        }

        public VehicleInputState Peek() => _inner.Peek();
    }

    /// <summary>Plays back an <see cref="InputRecording"/> step by step.</summary>
    public sealed class ReplayInputProvider : IInputProvider
    {
        private readonly InputRecording _recording;
        private int _cursor;
        private int _step;
        private VehicleInputState _held;

        public ControlSource Source => ControlSource.Replay;
        public bool Enabled { get; set; } = true;
        public bool IsFinished => _cursor >= _recording.Frames.Count;

        public ReplayInputProvider(InputRecording recording)
        {
            _recording = recording ?? throw new ArgumentNullException(nameof(recording));
        }

        public void Tick(float deltaTime) { }

        public VehicleInputState Sample()
        {
            // Edges only fire on the exact step they were recorded; axes hold their last value.
            _held.ShiftUp = _held.ShiftDown = _held.ResetVehicle = false;
            var frames = _recording.Frames;
            while (_cursor < frames.Count && frames[_cursor].Step <= _step)
            {
                _held = frames[_cursor].State;
                _cursor++;
            }
            _step++;
            return Enabled ? _held : VehicleInputState.Neutral;
        }

        public VehicleInputState Peek() => Enabled ? _held : VehicleInputState.Neutral;

        public void Restart()
        {
            _cursor = 0;
            _step = 0;
            _held = VehicleInputState.Neutral;
        }
    }
}
