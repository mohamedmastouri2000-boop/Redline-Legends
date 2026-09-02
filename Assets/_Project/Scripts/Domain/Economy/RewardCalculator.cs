using RedlineLegends.Events;
using RedlineLegends.Race;
using RedlineLegends.Save;
using UnityEngine;

namespace RedlineLegends.Economy
{
    /// <summary>
    /// Converts a race outcome into credits/XP/stars. Position and completion come from the event's
    /// reward table; difficulty scales it through the AI profile; stars follow mode-specific rules.
    /// Rewards are paid in full on every attempt (replaying is a valid way to earn, no grind wall).
    /// </summary>
    public static class RewardCalculator
    {
        public static RewardResult Compute(RaceEventDefinition evt, RaceOutcome outcome, EventProgressData previous)
        {
            var result = new RewardResult { DifficultyMultiplier = 1f };
            if (evt == null || outcome == null || outcome.Aborted) return result;
            var player = outcome.FindLocalPlayer();
            if (player == null || !player.Finished) return result;

            var table = evt.Rewards;
            float multiplier = evt.AIProfile != null ? Mathf.Max(0.5f, evt.AIProfile.RewardMultiplier) : 1f;
            if (evt.IsBossEvent) multiplier *= 1.5f;
            result.DifficultyMultiplier = multiplier;

            var position = table.ForPosition(player.Position);
            int credits = position.Credits + table.CompletionCredits;
            int xp = position.Xp + table.CompletionXp;
            int stars = position.Stars;

            if (evt is CircuitEventDefinition circuit && circuit.EventType == CircuitEventType.TimeAttack)
                stars = StarsFromTime(player.TotalTimeSeconds, table.TimeAttackStarThresholds);
            else if (evt.Mode == RaceMode.Drag)
                stars = player.Position == 1 ? (player.FalseStart ? 2 : 3) : 1;

            result.Credits = Mathf.RoundToInt(credits * multiplier);
            result.Xp = Mathf.RoundToInt(xp * multiplier);
            result.Stars = Mathf.Clamp(stars, 0, 3);
            result.FirstTimeCompletion = previous == null || previous.BestPosition == 0;
            result.NewPersonalBest = previous == null || previous.BestTimeSeconds < 0f
                                     || player.TotalTimeSeconds < previous.BestTimeSeconds;
            if (result.FirstTimeCompletion)
            {
                result.Credits += Mathf.RoundToInt(table.CompletionCredits * 0.5f);
                result.Xp += table.CompletionXp;
            }
            return result;
        }

        /// <summary>thresholds[0] = time for 3 stars, [1] = 2 stars, [2] = 1 star.</summary>
        public static int StarsFromTime(float time, float[] thresholds)
        {
            if (thresholds == null || thresholds.Length == 0) return 1;
            for (int i = 0; i < thresholds.Length && i < 3; i++)
                if (time <= thresholds[i]) return 3 - i;
            return 0;
        }
    }
}
