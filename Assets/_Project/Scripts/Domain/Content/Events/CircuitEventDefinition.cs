using RedlineLegends.Race;
using UnityEngine;

namespace RedlineLegends.Events
{
    public enum CircuitEventType
    {
        /// <summary>Multi-lap race, classic finish order.</summary>
        Circuit,
        /// <summary>Point-to-point, one lap of a non-loop track.</summary>
        Sprint,
        /// <summary>Solo against the clock; stars from time thresholds.</summary>
        TimeAttack,
        /// <summary>Last place is eliminated every interval until one remains.</summary>
        Elimination,
        /// <summary>Beat a countdown by passing checkpoints that add time.</summary>
        Checkpoint
    }

    [CreateAssetMenu(fileName = "evt_circuit", menuName = "Redline Legends/Circuit Event Definition")]
    public sealed class CircuitEventDefinition : RaceEventDefinition
    {
        [SerializeField] private CircuitEventType eventType = CircuitEventType.Circuit;
        [SerializeField, Min(1)] private int laps = 3;
        [SerializeField, Range(0, 11)] private int aiCount = 5;
        [Tooltip("Elimination: seconds between eliminations.")]
        [SerializeField] private float eliminationIntervalSeconds = 20f;
        [Tooltip("Checkpoint: starting time on the clock.")]
        [SerializeField] private float checkpointStartSeconds = 30f;
        [Tooltip("Checkpoint: seconds added per checkpoint.")]
        [SerializeField] private float checkpointBonusSeconds = 8f;

        public CircuitEventType EventType => eventType;
        public int Laps => eventType == CircuitEventType.Sprint ? 1 : laps;
        public float EliminationIntervalSeconds => eliminationIntervalSeconds;
        public float CheckpointStartSeconds => checkpointStartSeconds;
        public float CheckpointBonusSeconds => checkpointBonusSeconds;

        public override RaceMode Mode => RaceMode.Circuit;
        public override int OpponentCount => eventType == CircuitEventType.TimeAttack ? 0 : aiCount;
        public override string ModeLabel
        {
            get
            {
                switch (eventType)
                {
                    case CircuitEventType.Sprint: return "Sprint";
                    case CircuitEventType.TimeAttack: return "Time Attack";
                    case CircuitEventType.Elimination: return "Elimination";
                    case CircuitEventType.Checkpoint: return "Checkpoint";
                    default: return "Circuit";
                }
            }
        }

#if UNITY_EDITOR
        public void EditorInitializeCircuit(CircuitEventType type, int lapCount, int opponents,
            float eliminationInterval = 20f, float checkpointStart = 30f, float checkpointBonus = 8f)
        {
            eventType = type;
            laps = lapCount;
            aiCount = opponents;
            eliminationIntervalSeconds = eliminationInterval;
            checkpointStartSeconds = checkpointStart;
            checkpointBonusSeconds = checkpointBonus;
        }
#endif
    }
}
