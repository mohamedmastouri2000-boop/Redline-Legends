using UnityEngine;

namespace RedlineLegends.Progression
{
    /// <summary>Level curve, starting state and feature gates. Balance lives here, not in code.</summary>
    [CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Redline Legends/Progression Config")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [SerializeField] private int maxLevel = 60;
        [Tooltip("XP needed to go from level 1 to 2.")]
        [SerializeField] private int baseXpPerLevel = 500;
        [Tooltip("Each level needs this much more than the previous (1.12 = +12%).")]
        [SerializeField] private float xpGrowth = 1.12f;
        [SerializeField] private int startingCredits = 12000;
        [SerializeField] private string starterVehicleId = "veh_street_kestrel";
        [Tooltip("Player level at which gear/final-drive/grip tuning unlocks.")]
        [SerializeField] private int advancedTuningLevel = 8;
        [Tooltip("Credits granted per level up.")]
        [SerializeField] private int levelUpCredits = 750;

        public int MaxLevel => maxLevel;
        public int StartingCredits => startingCredits;
        public string StarterVehicleId => starterVehicleId;
        public int AdvancedTuningLevel => advancedTuningLevel;
        public int LevelUpCredits => levelUpCredits;

        /// <summary>XP required to advance from the given level to the next.</summary>
        public int XpForLevel(int level)
        {
            if (level < 1) level = 1;
            return Mathf.RoundToInt(baseXpPerLevel * Mathf.Pow(xpGrowth, level - 1));
        }

#if UNITY_EDITOR
        public void EditorInitialize(int credits, string starter) { startingCredits = credits; starterVehicleId = starter; }
#endif
    }
}
