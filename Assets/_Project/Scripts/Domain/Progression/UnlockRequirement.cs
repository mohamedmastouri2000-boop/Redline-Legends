using System;
using UnityEngine;

namespace RedlineLegends.Progression
{
    /// <summary>
    /// Read-only view of player progress that content can query. Implemented by ProgressionService;
    /// abstracted so the editor validator and future server-side checks can evaluate the same rules.
    /// </summary>
    public interface IProgressQuery
    {
        int PlayerLevel { get; }
        int TotalStars { get; }
        bool IsEventCompleted(string eventId);
        bool IsChampionshipCompleted(string championshipId);
        int GetChampionshipStars(string championshipId);
    }

    [Serializable]
    public struct UnlockRequirement
    {
        [Tooltip("0 = no level requirement.")]
        public int PlayerLevel;
        [Tooltip("Total stars across all championships. 0 = none.")]
        public int TotalStars;
        [Tooltip("Event that must be completed first. Empty = none.")]
        public string RequiredEventId;
        [Tooltip("Championship that must be completed first. Empty = none.")]
        public string RequiredChampionshipId;
        [Tooltip("Stars required inside the championship named above.")]
        public int RequiredChampionshipStars;

        public static readonly UnlockRequirement None = default;

        public bool IsMet(IProgressQuery progress)
        {
            if (progress == null) return PlayerLevel <= 1 && TotalStars <= 0
                                         && string.IsNullOrEmpty(RequiredEventId)
                                         && string.IsNullOrEmpty(RequiredChampionshipId);
            if (PlayerLevel > 0 && progress.PlayerLevel < PlayerLevel) return false;
            if (TotalStars > 0 && progress.TotalStars < TotalStars) return false;
            if (!string.IsNullOrEmpty(RequiredEventId) && !progress.IsEventCompleted(RequiredEventId)) return false;
            if (!string.IsNullOrEmpty(RequiredChampionshipId))
            {
                if (RequiredChampionshipStars > 0)
                {
                    if (progress.GetChampionshipStars(RequiredChampionshipId) < RequiredChampionshipStars) return false;
                }
                else if (!progress.IsChampionshipCompleted(RequiredChampionshipId)) return false;
            }
            return true;
        }

        public string Describe()
        {
            if (PlayerLevel > 0) return "Reach level " + PlayerLevel;
            if (TotalStars > 0) return "Earn " + TotalStars + " stars";
            if (!string.IsNullOrEmpty(RequiredChampionshipId))
                return RequiredChampionshipStars > 0
                    ? "Earn " + RequiredChampionshipStars + " stars in the previous championship"
                    : "Complete the previous championship";
            if (!string.IsNullOrEmpty(RequiredEventId)) return "Complete the previous event";
            return "Available";
        }
    }
}
