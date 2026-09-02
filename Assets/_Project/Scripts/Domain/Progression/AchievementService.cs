using System;
using System.Collections.Generic;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Economy;
using RedlineLegends.Events;
using RedlineLegends.Race;
using RedlineLegends.Save;

namespace RedlineLegends.Progression
{
    /// <summary>
    /// Keeps lifetime stats and unlocks achievements when a counter reaches a definition's target.
    /// Listens to the other services so gameplay code only reports raw facts (a perfect shift, a
    /// top speed) and never knows what an achievement is.
    /// </summary>
    public sealed class AchievementService : IDisposable
    {
        private readonly SaveService _save;
        private readonly ContentCatalog _catalog;
        private readonly PlayerProfileService _profile;
        private readonly ProgressionService _progression;
        private readonly GarageService _garage;
        private bool _suppressCreditTracking;

        public event Action<AchievementDefinition> Unlocked;

        public AchievementService(SaveService save, ContentCatalog catalog, PlayerProfileService profile,
            ProgressionService progression, GarageService garage)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));

            _progression.EventCompleted += OnEventCompleted;
            _progression.ChampionshipCompleted += OnChampionshipCompleted;
            _garage.VehiclePurchased += OnVehiclePurchased;
            _garage.UpgradeInstalled += OnUpgradeInstalled;
            _profile.CreditsEarned += OnCreditsEarned;
        }

        public void Dispose()
        {
            _progression.EventCompleted -= OnEventCompleted;
            _progression.ChampionshipCompleted -= OnChampionshipCompleted;
            _garage.VehiclePurchased -= OnVehiclePurchased;
            _garage.UpgradeInstalled -= OnUpgradeInstalled;
            _profile.CreditsEarned -= OnCreditsEarned;
        }

        public PlayerStatsData Stats => _save.Data.Stats;
        public IReadOnlyList<AchievementDefinition> Definitions => _catalog.Achievements;

        public bool IsUnlocked(string achievementId)
        {
            var data = Find(achievementId);
            return data != null && data.Unlocked;
        }

        public int GetProgress(AchievementDefinition def) => def == null ? 0 : Math.Min(def.Target, CurrentValue(def.Stat));

        // ---- facts reported by gameplay
        public void RecordPerfectShift()
        {
            Stats.PerfectShifts++;
            Evaluate(AchievementStat.PerfectShifts);
        }

        public void RecordTopSpeed(float kmh)
        {
            if (kmh <= Stats.TopSpeedKmh) return;
            Stats.TopSpeedKmh = kmh;
            Evaluate(AchievementStat.TopSpeedKmh);
        }

        public void RecordDistance(float meters)
        {
            if (meters > 0f) Stats.DistanceDrivenMeters += meters;
        }

        // ---- listeners
        private void OnEventCompleted(RaceEventDefinition evt, RewardResult reward)
        {
            Stats.RacesEntered++;
            Evaluate(AchievementStat.RacesEntered);
            Evaluate(AchievementStat.TotalStars);
        }

        /// <summary>Called by ProgressionService after it knows the finishing position.</summary>
        public void RecordRaceResult(RaceOutcome outcome)
        {
            var player = outcome?.FindLocalPlayer();
            if (player == null || !player.Finished || outcome.Aborted) return;
            if (player.Position == 1)
            {
                Stats.RacesWon++;
                Evaluate(AchievementStat.RacesWon);
                if (outcome.Mode == RaceMode.Drag)
                {
                    Stats.DragWins++;
                    Evaluate(AchievementStat.DragWins);
                }
            }
        }

        private void OnChampionshipCompleted(Career.ChampionshipDefinition championship) => Evaluate(AchievementStat.ChampionshipsCompleted);
        private void OnVehiclePurchased(string vehicleId) => Evaluate(AchievementStat.CarsOwned);

        private void OnUpgradeInstalled(string vehicleId)
        {
            Stats.UpgradesInstalled++;
            Evaluate(AchievementStat.UpgradesInstalled);
        }

        private void OnCreditsEarned(int amount)
        {
            if (_suppressCreditTracking) return;
            Stats.CreditsEarned += amount;
            Evaluate(AchievementStat.CreditsEarned);
        }

        private int CurrentValue(AchievementStat stat)
        {
            switch (stat)
            {
                case AchievementStat.RacesEntered: return Stats.RacesEntered;
                case AchievementStat.RacesWon: return Stats.RacesWon;
                case AchievementStat.DragWins: return Stats.DragWins;
                case AchievementStat.PerfectShifts: return Stats.PerfectShifts;
                case AchievementStat.TopSpeedKmh: return (int)Stats.TopSpeedKmh;
                case AchievementStat.CreditsEarned: return (int)Math.Min(int.MaxValue, Stats.CreditsEarned);
                case AchievementStat.TotalStars: return _progression.TotalStars;
                case AchievementStat.CarsOwned: return _garage.Owned.Count;
                case AchievementStat.ChampionshipsCompleted:
                    int count = 0;
                    foreach (var c in _catalog.Championships)
                        if (_progression.IsChampionshipCompleted(c.Id)) count++;
                    return count;
                case AchievementStat.UpgradesInstalled: return Stats.UpgradesInstalled;
                default: return 0;
            }
        }

        private void Evaluate(AchievementStat stat)
        {
            var defs = _catalog.Achievements;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def.Stat != stat) continue;
                var data = FindOrCreate(def.Id);
                if (data.Unlocked) continue;
                data.Progress = CurrentValue(stat);
                if (data.Progress < def.Target) continue;
                data.UnlockedUtcTicks = DateTime.UtcNow.Ticks;
                // Achievement payouts must not count toward the "credits earned" counter.
                _suppressCreditTracking = true;
                _profile.AddCredits(def.RewardCredits);
                _profile.AddXp(def.RewardXp);
                _suppressCreditTracking = false;
                GameLog.Info("Achievement unlocked: " + def.Id);
                Unlocked?.Invoke(def);
            }
        }

        private AchievementData Find(string id)
        {
            var list = _save.Data.Achievements;
            for (int i = 0; i < list.Count; i++)
                if (list[i].AchievementId == id) return list[i];
            return null;
        }

        private AchievementData FindOrCreate(string id)
        {
            var data = Find(id);
            if (data != null) return data;
            data = new AchievementData { AchievementId = id };
            _save.Data.Achievements.Add(data);
            return data;
        }
    }
}
