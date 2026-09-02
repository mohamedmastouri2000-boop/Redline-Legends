using System;
using System.Collections.Generic;
using RedlineLegends.Tuning;
using UnityEngine;

namespace RedlineLegends.Save
{
    public enum ControlStyle
    {
        Buttons,
        SteeringWheel,
        Tilt
    }

    public enum TransmissionMode
    {
        Automatic,
        Manual
    }

    public enum CameraMode
    {
        Chase,
        Hood,
        Cockpit
    }

    public enum GraphicsPreset
    {
        Low,
        Medium,
        High
    }

    public enum SpeedUnit
    {
        Kmh,
        Mph
    }

    /// <summary>User preferences. Part of the save so they follow the profile.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public GraphicsPreset Graphics = GraphicsPreset.Medium;
        public int TargetFrameRate = 60;
        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Range(0f, 1f)] public float MusicVolume = 0.7f;
        [Range(0f, 1f)] public float SfxVolume = 1f;
        public ControlStyle ControlStyle = ControlStyle.Buttons;
        [Range(0.5f, 2f)] public float SteeringSensitivity = 1f;
        [Range(0.5f, 2f)] public float TiltSensitivity = 1f;
        public bool Vibration = true;
        public TransmissionMode Transmission = TransmissionMode.Automatic;
        public CameraMode Camera = CameraMode.Chase;
        public SpeedUnit Units = SpeedUnit.Kmh;
        [Range(0f, 1f)] public float CameraShake = 0.6f;
        public bool TutorialsEnabled = true;
        public bool ShowRacingLine = true;

        public SettingsData Clone() => (SettingsData)MemberwiseClone();
    }

    [Serializable]
    public sealed class PlayerProfileData
    {
        public string ProfileId = "";
        public string DisplayName = "Racer";
        public int Credits;
        public int Xp;
        public int Level = 1;
        public long CreatedUtcTicks;
        public float TotalPlaySeconds;
        public int RacesEntered;
        public int RacesWon;
    }

    [Serializable]
    public sealed class OwnedVehicleData
    {
        public string VehicleId = "";
        /// <summary>Installed stage per UpgradeCategory (index = enum value, 0 = stock).</summary>
        public int[] UpgradeStages = Array.Empty<int>();
        public VehicleTuningData Tuning = new VehicleTuningData();
        public int PaintIndex;
        public int WheelIndex;
        public long PurchasedUtcTicks;
        public float OdometerMeters;
    }

    [Serializable]
    public sealed class GarageData
    {
        public List<OwnedVehicleData> Owned = new List<OwnedVehicleData>();
        public string SelectedVehicleId = "";
    }

    [Serializable]
    public sealed class EventProgressData
    {
        public string EventId = "";
        /// <summary>Best finishing position, 0 = never finished.</summary>
        public int BestPosition;
        public int Stars;
        public float BestTimeSeconds = -1f;
        public float BestLapSeconds = -1f;
        public float BestReactionSeconds = -1f;
        public int Attempts;
        public int Wins;
    }

    [Serializable]
    public sealed class ChampionshipProgressData
    {
        public string ChampionshipId = "";
        public bool CompletionRewardClaimed;
    }

    [Serializable]
    public sealed class ProgressionData
    {
        public List<EventProgressData> Events = new List<EventProgressData>();
        public List<ChampionshipProgressData> Championships = new List<ChampionshipProgressData>();
        public List<string> CompletedTutorials = new List<string>();
    }

    [Serializable]
    public sealed class AchievementData
    {
        public string AchievementId = "";
        public int Progress;
        public long UnlockedUtcTicks;
        public bool Unlocked => UnlockedUtcTicks != 0;
    }

    /// <summary>
    /// Root of the persisted profile. Only mutable player state lives here; balance and content
    /// stay in ScriptableObjects and are looked up by the ids stored in this file.
    /// Bump <see cref="CurrentVersion"/> and add an ISaveMigration whenever the layout changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public long LastSavedUtcTicks;
        public PlayerProfileData Profile = new PlayerProfileData();
        public GarageData Garage = new GarageData();
        public ProgressionData Progression = new ProgressionData();
        public SettingsData Settings = new SettingsData();
        public List<AchievementData> Achievements = new List<AchievementData>();

        public static SaveData CreateNew(SettingsData defaultSettings, int startingCredits)
        {
            return new SaveData
            {
                Version = CurrentVersion,
                Profile = new PlayerProfileData
                {
                    ProfileId = Guid.NewGuid().ToString("N"),
                    Credits = startingCredits,
                    Level = 1,
                    CreatedUtcTicks = DateTime.UtcNow.Ticks
                },
                Settings = defaultSettings != null ? defaultSettings.Clone() : new SettingsData()
            };
        }

        /// <summary>JsonUtility leaves missing members null; normalize so consumers never null-check.</summary>
        public void EnsureIntegrity()
        {
            Profile ??= new PlayerProfileData();
            Garage ??= new GarageData();
            Garage.Owned ??= new List<OwnedVehicleData>();
            Progression ??= new ProgressionData();
            Progression.Events ??= new List<EventProgressData>();
            Progression.Championships ??= new List<ChampionshipProgressData>();
            Progression.CompletedTutorials ??= new List<string>();
            Settings ??= new SettingsData();
            Achievements ??= new List<AchievementData>();
            if (string.IsNullOrEmpty(Profile.ProfileId)) Profile.ProfileId = Guid.NewGuid().ToString("N");
            if (Profile.Level < 1) Profile.Level = 1;
            for (int i = 0; i < Garage.Owned.Count; i++)
            {
                var owned = Garage.Owned[i];
                owned.UpgradeStages ??= Array.Empty<int>();
                owned.Tuning ??= new VehicleTuningData();
            }
        }
    }
}
