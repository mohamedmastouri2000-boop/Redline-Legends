using System;
using RedlineLegends.Career;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Economy;
using RedlineLegends.Events;
using RedlineLegends.Race;
using RedlineLegends.Save;

namespace RedlineLegends.Progression
{
    /// <summary>
    /// Event/championship completion, records and unlock evaluation. Applies rewards to the
    /// profile and persists. Content is never mutated; only the save's progression lists are.
    /// </summary>
    public sealed class ProgressionService : IProgressQuery
    {
        private readonly SaveService _save;
        private readonly ContentCatalog _catalog;
        private readonly PlayerProfileService _profile;

        public event Action<RaceEventDefinition, RewardResult> EventCompleted;
        public event Action<ChampionshipDefinition> ChampionshipCompleted;
        /// <summary>Raised with the full outcome after records and rewards are applied (achievements listen).</summary>
        public event Action<RaceOutcome> OutcomeRecorded;

        public ProgressionService(SaveService save, ContentCatalog catalog, PlayerProfileService profile)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        private ProgressionData Data => _save.Data.Progression;

        // ---- IProgressQuery ----
        public int PlayerLevel => _profile.Level;

        public int TotalStars
        {
            get
            {
                int total = 0;
                var events = Data.Events;
                for (int i = 0; i < events.Count; i++) total += events[i].Stars;
                return total;
            }
        }

        public bool IsEventCompleted(string eventId)
        {
            var p = FindEvent(eventId);
            return p != null && p.BestPosition > 0;
        }

        public bool IsChampionshipCompleted(string championshipId)
        {
            if (!_catalog.TryGetChampionship(championshipId, out var championship)) return false;
            var events = championship.Events;
            for (int i = 0; i < events.Length; i++)
                if (events[i] != null && !IsEventCompleted(events[i].Id)) return false;
            return events.Length > 0;
        }

        public int GetChampionshipStars(string championshipId)
        {
            if (!_catalog.TryGetChampionship(championshipId, out var championship)) return 0;
            int stars = 0;
            var events = championship.Events;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i] == null) continue;
                var p = FindEvent(events[i].Id);
                if (p != null) stars += p.Stars;
            }
            return stars;
        }

        // ---- Queries ----
        public bool IsEventUnlocked(RaceEventDefinition evt)
        {
            if (evt == null) return false;
            var championship = _catalog.FindChampionshipForEvent(evt.Id);
            if (championship != null && !IsChampionshipUnlocked(championship)) return false;
            return evt.UnlockRequirement.IsMet(this);
        }

        public bool IsChampionshipUnlocked(ChampionshipDefinition championship)
            => championship != null && championship.UnlockRequirement.IsMet(this);

        public EventProgressData FindEvent(string eventId)
        {
            var events = Data.Events;
            for (int i = 0; i < events.Count; i++)
                if (events[i].EventId == eventId) return events[i];
            return null;
        }

        public bool IsTutorialCompleted(string tutorialId) => Data.CompletedTutorials.Contains(tutorialId);

        public void MarkTutorialCompleted(string tutorialId)
        {
            if (string.IsNullOrEmpty(tutorialId) || Data.CompletedTutorials.Contains(tutorialId)) return;
            Data.CompletedTutorials.Add(tutorialId);
            _save.Save();
        }

        // ---- Mutation ----

        /// <summary>Applies an outcome: records, rewards, championship bonus, then saves. Returns the reward paid.</summary>
        public RewardResult RecordOutcome(RaceOutcome outcome)
        {
            if (outcome == null) return default;
            if (!_catalog.TryGetEvent(outcome.EventId, out var evt))
            {
                GameLog.Warn("RecordOutcome: unknown event '" + outcome.EventId + "'.");
                return default;
            }

            var progress = FindEvent(evt.Id);
            var reward = RewardCalculator.Compute(evt, outcome, progress);
            var player = outcome.FindLocalPlayer();

            if (progress == null)
            {
                progress = new EventProgressData { EventId = evt.Id };
                Data.Events.Add(progress);
            }
            progress.Attempts++;

            if (player != null && player.Finished && !outcome.Aborted)
            {
                bool won = player.Position == 1;
                if (won) progress.Wins++;
                if (progress.BestPosition == 0 || player.Position < progress.BestPosition)
                    progress.BestPosition = player.Position;
                if (progress.BestTimeSeconds < 0f || player.TotalTimeSeconds < progress.BestTimeSeconds)
                    progress.BestTimeSeconds = player.TotalTimeSeconds;
                if (player.BestLapSeconds > 0f && (progress.BestLapSeconds < 0f || player.BestLapSeconds < progress.BestLapSeconds))
                    progress.BestLapSeconds = player.BestLapSeconds;
                if (outcome.Mode == RaceMode.Drag && player.ReactionTimeSeconds >= 0f
                    && (progress.BestReactionSeconds < 0f || player.ReactionTimeSeconds < progress.BestReactionSeconds))
                    progress.BestReactionSeconds = player.ReactionTimeSeconds;
                if (reward.Stars > progress.Stars) progress.Stars = reward.Stars;

                _profile.RecordRaceEntered(won);
                _profile.AddCredits(reward.Credits);
                _profile.AddXp(reward.Xp);
                EventCompleted?.Invoke(evt, reward);

                TryPayChampionshipBonus(evt);
            }
            else
            {
                _profile.RecordRaceEntered(false);
            }

            OutcomeRecorded?.Invoke(outcome);
            _save.Save();
            return reward;
        }

        private void TryPayChampionshipBonus(RaceEventDefinition evt)
        {
            var championship = _catalog.FindChampionshipForEvent(evt.Id);
            if (championship == null || !IsChampionshipCompleted(championship.Id)) return;

            ChampionshipProgressData record = null;
            var list = Data.Championships;
            for (int i = 0; i < list.Count; i++)
                if (list[i].ChampionshipId == championship.Id) { record = list[i]; break; }
            if (record == null)
            {
                record = new ChampionshipProgressData { ChampionshipId = championship.Id };
                list.Add(record);
            }
            if (record.CompletionRewardClaimed) return;

            record.CompletionRewardClaimed = true;
            _profile.AddCredits(championship.CompletionCredits);
            _profile.AddXp(championship.CompletionXp);
            ChampionshipCompleted?.Invoke(championship);
        }
    }
}
