using UnityEngine;

namespace RedlineLegends.AI
{
    public enum AIDifficultyTier
    {
        Rookie,
        Amateur,
        Pro,
        Expert,
        Legend
    }

    public enum DragNitrousStrategy
    {
        Never,
        AtLaunch,
        AfterSecondShift,
        FinalStretch,
        Random
    }

    /// <summary>
    /// Behavioural parameters for AI drivers. Difficulty changes how well the AI drives, never the
    /// physics: <see cref="SpeedScale"/> is clamped to 1 so the AI cannot exceed what its car can do.
    /// </summary>
    [CreateAssetMenu(fileName = "ai_new", menuName = "Redline Legends/AI Profile")]
    public sealed class AIProfile : ScriptableObject
    {
        [SerializeField] private string id = "ai_new";
        [SerializeField] private string displayName = "Rookie";
        [SerializeField] private AIDifficultyTier tier = AIDifficultyTier.Rookie;
        [Tooltip("Reward multiplier for events using this profile.")]
        [SerializeField] private float rewardMultiplier = 1f;

        [Header("Circuit driving")]
        [Tooltip("Seconds between perceiving a situation and reacting to it.")]
        [SerializeField] private float reactionTime = 0.35f;
        [Tooltip("0 = polite, 1 = will lean on you.")]
        [SerializeField, Range(0f, 1f)] private float aggression = 0.3f;
        [Tooltip("1 = hits apexes; lower adds line error.")]
        [SerializeField, Range(0f, 1f)] private float corneringAccuracy = 0.7f;
        [Tooltip("1 = brakes at the last possible metre; lower brakes early.")]
        [SerializeField, Range(0f, 1f)] private float brakingQuality = 0.7f;
        [Tooltip("1 = smooth full throttle; lower is hesitant on exits.")]
        [SerializeField, Range(0f, 1f)] private float throttleQuality = 0.7f;
        [Tooltip("Expected mistakes per minute of driving.")]
        [SerializeField] private float mistakeFrequency = 1.0f;
        [Tooltip("Fraction of the car's cornering limit the AI targets. Never above 1.")]
        [SerializeField, Range(0.5f, 1f)] private float speedScale = 0.85f;
        [Tooltip("Upgrade stage the AI's car is built at (0 = stock).")]
        [SerializeField, Range(0, 3)] private int vehicleUpgradeStage = 0;

        [Header("Drag racing")]
        [SerializeField] private float dragReactionTimeMin = 0.25f;
        [SerializeField] private float dragReactionTimeMax = 0.6f;
        [Tooltip("1 = perfect launch rpm; lower bogs or spins.")]
        [SerializeField, Range(0f, 1f)] private float launchQuality = 0.6f;
        [Tooltip("1 = always shifts in the perfect window.")]
        [SerializeField, Range(0f, 1f)] private float shiftAccuracy = 0.6f;
        [SerializeField] private DragNitrousStrategy nitrousStrategy = DragNitrousStrategy.AfterSecondShift;
        [Tooltip("Chance per race of a red light.")]
        [SerializeField, Range(0f, 0.3f)] private float falseStartChance = 0.02f;

        public string Id => id;
        public string DisplayName => displayName;
        public AIDifficultyTier Tier => tier;
        public float RewardMultiplier => rewardMultiplier;
        public float ReactionTime => reactionTime;
        public float Aggression => aggression;
        public float CorneringAccuracy => corneringAccuracy;
        public float BrakingQuality => brakingQuality;
        public float ThrottleQuality => throttleQuality;
        public float MistakeFrequency => mistakeFrequency;
        public float SpeedScale => Mathf.Min(1f, speedScale);
        public int VehicleUpgradeStage => vehicleUpgradeStage;
        public float DragReactionTimeMin => dragReactionTimeMin;
        public float DragReactionTimeMax => dragReactionTimeMax;
        public float LaunchQuality => launchQuality;
        public float ShiftAccuracy => shiftAccuracy;
        public DragNitrousStrategy NitrousStrategy => nitrousStrategy;
        public float FalseStartChance => falseStartChance;

#if UNITY_EDITOR
        public void EditorInitialize(string newId, string newName, AIDifficultyTier newTier, float reward,
            float reaction, float aggr, float cornering, float braking, float throttle, float mistakes, float speed, int upgradeStage,
            float dragReactMin, float dragReactMax, float launch, float shift, DragNitrousStrategy nitrous, float falseStart)
        {
            id = newId; displayName = newName; tier = newTier; rewardMultiplier = reward;
            reactionTime = reaction; aggression = aggr; corneringAccuracy = cornering; brakingQuality = braking;
            throttleQuality = throttle; mistakeFrequency = mistakes; speedScale = speed; vehicleUpgradeStage = upgradeStage;
            dragReactionTimeMin = dragReactMin; dragReactionTimeMax = dragReactMax; launchQuality = launch;
            shiftAccuracy = shift; nitrousStrategy = nitrous; falseStartChance = falseStart;
        }
#endif
    }
}
