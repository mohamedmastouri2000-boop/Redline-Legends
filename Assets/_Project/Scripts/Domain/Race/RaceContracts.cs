using System;
using System.Collections.Generic;
using RedlineLegends.Vehicles;

namespace RedlineLegends.Race
{
    public enum RaceMode
    {
        Circuit,
        Drag
    }

    /// <summary>Who produces input for a participant's vehicle.</summary>
    public enum ControlSource
    {
        LocalPlayer,
        AI,
        Replay,
        /// <summary>Reserved for the multiplayer update; no runtime support in v1.</summary>
        Remote
    }

    /// <summary>
    /// Everything needed to spawn one participant. Built by the menu/career layer, consumed by the
    /// race session. Plain serializable data so it can be sent over the wire later.
    /// </summary>
    [Serializable]
    public sealed class RaceParticipantSpec
    {
        public RacerId Id;
        public string DisplayName;
        public string VehicleId;
        public ControlSource ControlSource;
        /// <summary>AI profile id when <see cref="ControlSource"/> is AI; otherwise empty.</summary>
        public string AIProfileId;
        /// <summary>Grid slot, 0 = pole.</summary>
        public int GridSlot;
        /// <summary>Resolved vehicle spec (base stats + upgrades + tuning) at launch time.</summary>
        public VehicleSpec VehicleSpec;
        /// <summary>Paint chosen for this participant (index into the vehicle's paint list).</summary>
        public int PaintIndex;
    }

    /// <summary>
    /// Data handed from the menus to a race scene. Contains no scene objects, so it can be created
    /// offline, by a lobby, or by a replay.
    /// </summary>
    [Serializable]
    public sealed class RaceLaunchRequest
    {
        public string EventId;
        public string EventDisplayName;
        public RaceMode Mode;
        public string TrackId;
        public string TrackSceneName;
        public List<RaceParticipantSpec> Participants = new List<RaceParticipantSpec>();
        /// <summary>Deterministic seed for AI mistakes/grid shuffles; shared by all peers later.</summary>
        public int Seed;
        /// <summary>Free drive: no rules, no rewards, no records.</summary>
        public bool IsPractice;

        public RaceParticipantSpec FindLocalPlayer()
        {
            for (int i = 0; i < Participants.Count; i++)
                if (Participants[i].ControlSource == ControlSource.LocalPlayer)
                    return Participants[i];
            return null;
        }
    }

    /// <summary>Final classification of one participant.</summary>
    [Serializable]
    public sealed class RacerResult
    {
        public RacerId Id;
        public string DisplayName;
        public string VehicleId;
        public ControlSource ControlSource;
        public int Position;
        public float TotalTimeSeconds;
        public float BestLapSeconds;
        public bool Finished;
        /// <summary>Drag only: reaction time in seconds (negative = jumped the start).</summary>
        public float ReactionTimeSeconds;
        /// <summary>Drag only: true when the racer red-lighted.</summary>
        public bool FalseStart;
    }

    /// <summary>What a race produced; the progression layer turns this into rewards and records.</summary>
    [Serializable]
    public sealed class RaceOutcome
    {
        public string EventId;
        public RaceMode Mode;
        public List<RacerResult> Results = new List<RacerResult>();
        public bool Aborted;

        public RacerResult FindLocalPlayer()
        {
            for (int i = 0; i < Results.Count; i++)
                if (Results[i].ControlSource == ControlSource.LocalPlayer)
                    return Results[i];
            return null;
        }
    }
}
