using System;
using RedlineLegends.Core;
using RedlineLegends.Save;

namespace RedlineLegends.Progression
{
    /// <summary>
    /// Credits, XP, level. Every mutation goes through here so UI can subscribe to one event and
    /// so a future server-authoritative economy has a single seam to replace.
    /// </summary>
    public sealed class PlayerProfileService
    {
        private readonly SaveService _save;
        private readonly ProgressionConfig _config;

        public event Action Changed;
        public event Action<int> LeveledUp;
        /// <summary>Raised for every credit gain (rewards, level-ups, achievements).</summary>
        public event Action<int> CreditsEarned;

        public PlayerProfileService(SaveService save, ProgressionConfig config)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        private PlayerProfileData Profile => _save.Data.Profile;

        public string ProfileId => Profile.ProfileId;
        public string DisplayName => Profile.DisplayName;
        public int Credits => Profile.Credits;
        public int Xp => Profile.Xp;
        public int Level => Profile.Level;
        public int XpForNextLevel => _config.XpForLevel(Profile.Level);
        public float LevelProgress01 => Level >= _config.MaxLevel ? 1f : (float)Xp / XpForNextLevel;
        public int RacesEntered => Profile.RacesEntered;
        public int RacesWon => Profile.RacesWon;

        public void SetDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Profile.DisplayName = name.Trim();
            Changed?.Invoke();
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            Profile.Credits = checked(Profile.Credits + amount);
            CreditsEarned?.Invoke(amount);
            Changed?.Invoke();
        }

        public bool CanAfford(int price) => price <= Profile.Credits;

        public bool TrySpendCredits(int price)
        {
            if (price < 0 || !CanAfford(price)) return false;
            Profile.Credits -= price;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Adds XP and resolves every level-up it causes. Returns the number of levels gained.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0) return 0;
            int gained = 0;
            Profile.Xp += amount;
            while (Profile.Level < _config.MaxLevel && Profile.Xp >= _config.XpForLevel(Profile.Level))
            {
                Profile.Xp -= _config.XpForLevel(Profile.Level);
                Profile.Level++;
                Profile.Credits += _config.LevelUpCredits;
                CreditsEarned?.Invoke(_config.LevelUpCredits);
                gained++;
                GameLog.Info("Level up: " + Profile.Level);
                LeveledUp?.Invoke(Profile.Level);
            }
            if (Profile.Level >= _config.MaxLevel) Profile.Xp = 0;
            Changed?.Invoke();
            return gained;
        }

        public void RecordRaceEntered(bool won)
        {
            Profile.RacesEntered++;
            if (won) Profile.RacesWon++;
            Changed?.Invoke();
        }

        public void AddPlayTime(float seconds)
        {
            if (seconds > 0f) Profile.TotalPlaySeconds += seconds;
        }
    }
}
