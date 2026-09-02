using System;
using UnityEngine;

namespace RedlineLegends.Economy
{
    [Serializable]
    public struct PositionReward
    {
        public int Credits;
        public int Xp;
        [Range(0, 3)] public int Stars;

        public PositionReward(int credits, int xp, int stars)
        {
            Credits = credits;
            Xp = xp;
            Stars = stars;
        }
    }

    /// <summary>
    /// Rewards for one event. Position rewards are indexed 1st..Nth; positions past the table's end
    /// receive the completion reward only. A difficulty multiplier from the AI profile is applied
    /// by RewardCalculator, not stored here.
    /// </summary>
    [Serializable]
    public sealed class RewardTable
    {
        [Tooltip("Index 0 = 1st place.")]
        public PositionReward[] ByPosition =
        {
            new PositionReward(1500, 300, 3),
            new PositionReward(1000, 200, 2),
            new PositionReward(700, 150, 1),
            new PositionReward(400, 100, 1),
        };
        [Tooltip("Paid to anyone who finishes, in addition to the position reward.")]
        public int CompletionCredits = 200;
        public int CompletionXp = 50;
        [Tooltip("Time attack only: finish under these times (seconds) for 3/2/1 stars.")]
        public float[] TimeAttackStarThresholds = Array.Empty<float>();

        public PositionReward ForPosition(int position)
        {
            if (ByPosition == null || ByPosition.Length == 0 || position < 1) return default;
            int index = Mathf.Min(position, ByPosition.Length) - 1;
            return position <= ByPosition.Length ? ByPosition[index] : default;
        }
    }

    /// <summary>Concrete reward paid out after a race.</summary>
    [Serializable]
    public struct RewardResult
    {
        public int Credits;
        public int Xp;
        public int Stars;
        public bool FirstTimeCompletion;
        public bool NewPersonalBest;
        public float DifficultyMultiplier;

        public override string ToString() => Credits + " CR, " + Xp + " XP, " + Stars + " stars";
    }
}
