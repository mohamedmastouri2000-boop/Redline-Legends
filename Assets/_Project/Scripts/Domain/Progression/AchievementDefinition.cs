using UnityEngine;

namespace RedlineLegends.Progression
{
    /// <summary>Which running counter an achievement watches. Counters live in PlayerStatsData.</summary>
    public enum AchievementStat
    {
        RacesEntered,
        RacesWon,
        DragWins,
        PerfectShifts,
        TopSpeedKmh,
        CreditsEarned,
        TotalStars,
        CarsOwned,
        ChampionshipsCompleted,
        UpgradesInstalled
    }

    [CreateAssetMenu(fileName = "ach_new", menuName = "Redline Legends/Achievement Definition")]
    public sealed class AchievementDefinition : ScriptableObject
    {
        [SerializeField] private string id = "ach_new";
        [SerializeField] private string displayName = "Achievement";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private AchievementStat stat;
        [SerializeField] private int target = 1;
        [SerializeField] private int rewardCredits = 500;
        [SerializeField] private int rewardXp = 100;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public AchievementStat Stat => stat;
        public int Target => target;
        public int RewardCredits => rewardCredits;
        public int RewardXp => rewardXp;

#if UNITY_EDITOR
        public void EditorInitialize(string newId, string name, string desc, AchievementStat newStat, int newTarget, int credits, int xp)
        {
            id = newId; displayName = name; description = desc; stat = newStat; target = newTarget; rewardCredits = credits; rewardXp = xp;
        }
#endif
    }
}
