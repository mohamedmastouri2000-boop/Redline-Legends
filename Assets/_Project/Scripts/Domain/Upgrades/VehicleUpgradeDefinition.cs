using System;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Upgrades
{
    public enum UpgradeCategory
    {
        Engine,
        Turbo,
        ECU,
        Transmission,
        Tires,
        Suspension,
        Brakes,
        WeightReduction,
        Nitrous
    }

    /// <summary>Which physical value an upgrade stage changes.</summary>
    public enum VehicleStatId
    {
        PeakTorque,
        PeakPower,
        Redline,
        TurboBoost,
        TurboSpool,
        EngineInertia,
        MassKg,
        TopSpeedKmh,
        Drag,
        LateralGrip,
        LongitudinalGrip,
        SlideGripFraction,
        BrakeTorque,
        HandbrakeTorque,
        SteerResponse,
        StabilityAssist,
        SpringRate,
        Damping,
        AntiRoll,
        ShiftTime,
        DrivelineEfficiency,
        NitrousCapacity,
        NitrousPower,
        NitrousRefill,
        Downforce
    }

    public enum ModifierOp
    {
        /// <summary>value is added.</summary>
        Add,
        /// <summary>value is a multiplier (1.1 = +10%).</summary>
        Multiply,
        /// <summary>value replaces the stat (used for fitting nitrous to a car that had none).</summary>
        Set
    }

    [Serializable]
    public struct StatModifier
    {
        public VehicleStatId Stat;
        public ModifierOp Op;
        public float Value;

        public StatModifier(VehicleStatId stat, ModifierOp op, float value)
        {
            Stat = stat;
            Op = op;
            Value = value;
        }
    }

    [Serializable]
    public sealed class UpgradeStage
    {
        public string DisplayName = "Stage 1";
        public int Price = 2000;
        [Tooltip("Player level required to buy this stage.")]
        public int RequiredPlayerLevel = 1;
        public StatModifier[] Modifiers = Array.Empty<StatModifier>();
    }

    /// <summary>
    /// One upgrade line (e.g. "Street Engine Kit") with progressive stages. Stage 0 is stock and
    /// has no entry; <see cref="Stages"/>[0] is Stage 1. Applying stage N applies stages 1..N
    /// cumulatively so each stage only describes its own gain.
    /// </summary>
    [CreateAssetMenu(fileName = "upg_new", menuName = "Redline Legends/Upgrade Definition")]
    public sealed class VehicleUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string id = "upg_new";
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private UpgradeCategory category;
        [SerializeField] private UpgradeStage[] stages = Array.Empty<UpgradeStage>();

        public string Id => id;
        public string DisplayName => displayName;
        public UpgradeCategory Category => category;
        public UpgradeStage[] Stages => stages;
        public int MaxStage => stages.Length;

        public UpgradeStage GetStage(int stage)
        {
            if (stage < 1 || stage > stages.Length) return null;
            return stages[stage - 1];
        }

        /// <summary>Applies stages 1..stage cumulatively.</summary>
        public void ApplyTo(VehicleStats stats, int stage)
        {
            int clamped = Mathf.Clamp(stage, 0, stages.Length);
            for (int s = 0; s < clamped; s++)
            {
                var mods = stages[s].Modifiers;
                for (int i = 0; i < mods.Length; i++)
                    VehicleStatModifierApplier.Apply(stats, mods[i]);
            }
        }

#if UNITY_EDITOR
        public void EditorInitialize(string newId, string newDisplayName, UpgradeCategory newCategory, UpgradeStage[] newStages)
        {
            id = newId;
            displayName = newDisplayName;
            category = newCategory;
            stages = newStages;
        }
#endif
    }
}
