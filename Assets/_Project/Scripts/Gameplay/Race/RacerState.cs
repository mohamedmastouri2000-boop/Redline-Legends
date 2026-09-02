using System.Collections.Generic;
using RedlineLegends.AI;
using RedlineLegends.Input;
using RedlineLegends.Tracks;
using RedlineLegends.Vehicles;

namespace RedlineLegends.Race
{
    /// <summary>Runtime record of one participant. Owned by the race session; UI reads it.</summary>
    public sealed class RacerState
    {
        public RaceParticipantSpec Spec;
        public VehicleController Vehicle;
        public IInputProvider Input;
        public AIDriver Driver;

        /// <summary>Current lap, 1-based. Becomes Laps + 1 when the race is complete.</summary>
        public int Lap = 1;
        public int NextCheckpoint;
        public Checkpoint LastCheckpoint;
        /// <summary>False until the racer crosses the start line for the first time (the grid sits behind it).</summary>
        public bool StartLineCrossed;
        public int LineHint = -1;
        public float DistanceAlongLap;
        public float TotalProgress;
        public float CurrentLapStart;
        public float BestLap = -1f;
        public readonly List<float> LapTimes = new List<float>(8);
        public float FinishTime = -1f;
        public bool Finished;
        public bool Eliminated;
        public int Position;
        public float WrongWayTime;
        public bool WrongWay;
        public float StoppedTime;
        public float ReactionTime = -1f;
        public bool FalseStart;

        public RacerId Id => Spec.Id;
        public bool IsLocalPlayer => Spec.ControlSource == ControlSource.LocalPlayer;
        public bool IsActive => !Finished && !Eliminated;
    }
}
