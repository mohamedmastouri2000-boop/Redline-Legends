using System;
using RedlineLegends.AI;
using RedlineLegends.Economy;
using RedlineLegends.Progression;
using RedlineLegends.Race;
using RedlineLegends.Tracks;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Events
{
    public enum WeatherType
    {
        Clear,
        Overcast,
        Rain
    }

    public enum TimeOfDay
    {
        Day,
        Sunset,
        Night
    }

    /// <summary>Which cars may enter. Empty class list and 0 PR bounds = no restriction.</summary>
    [Serializable]
    public sealed class VehicleRestriction
    {
        public VehicleClass[] AllowedClasses = Array.Empty<VehicleClass>();
        [Tooltip("0 = no minimum.")]
        public int MinPerformanceRating;
        [Tooltip("0 = no maximum.")]
        public int MaxPerformanceRating;
        [Tooltip("If set, only these vehicle ids may enter (boss/special events).")]
        public string[] AllowedVehicleIds = Array.Empty<string>();

        public bool Allows(VehicleDefinition definition, int performanceRating)
        {
            if (definition == null) return false;
            if (AllowedVehicleIds != null && AllowedVehicleIds.Length > 0)
            {
                bool found = false;
                for (int i = 0; i < AllowedVehicleIds.Length; i++)
                    if (AllowedVehicleIds[i] == definition.Id) { found = true; break; }
                if (!found) return false;
            }
            if (AllowedClasses != null && AllowedClasses.Length > 0)
            {
                bool found = false;
                for (int i = 0; i < AllowedClasses.Length; i++)
                    if (AllowedClasses[i] == definition.VehicleClass) { found = true; break; }
                if (!found) return false;
            }
            if (MinPerformanceRating > 0 && performanceRating < MinPerformanceRating) return false;
            if (MaxPerformanceRating > 0 && performanceRating > MaxPerformanceRating) return false;
            return true;
        }

        public string Describe()
        {
            if (AllowedVehicleIds != null && AllowedVehicleIds.Length > 0) return "Special entry";
            string cls = AllowedClasses != null && AllowedClasses.Length > 0 ? string.Join("/", AllowedClasses) : "Any class";
            if (MaxPerformanceRating > 0) return cls + " · PR " + MinPerformanceRating + "-" + MaxPerformanceRating;
            if (MinPerformanceRating > 0) return cls + " · PR " + MinPerformanceRating + "+";
            return cls;
        }
    }

    /// <summary>
    /// Base class for every race event. Circuit and drag events share track, rewards, unlock rules,
    /// restrictions and AI so the career, menu and reward code handle both uniformly.
    /// </summary>
    public abstract class RaceEventDefinition : ScriptableObject
    {
        [SerializeField] private string id = "evt_new";
        [SerializeField] private string displayName = "New Event";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private TrackDefinition track;
        [SerializeField] private RewardTable rewards = new RewardTable();
        [SerializeField] private VehicleRestriction restriction = new VehicleRestriction();
        [SerializeField] private int recommendedPerformanceRating = 250;
        [SerializeField] private WeatherType weather = WeatherType.Clear;
        [SerializeField] private TimeOfDay timeOfDay = TimeOfDay.Day;
        [SerializeField] private UnlockRequirement unlockRequirement;
        [SerializeField] private AIProfile aiProfile;
        [Tooltip("Cars the AI may drive. Empty = pick from the database near the recommended PR.")]
        [SerializeField] private VehicleDefinition[] aiVehiclePool = Array.Empty<VehicleDefinition>();
        [SerializeField] private bool isBossEvent;
        [SerializeField] private Sprite thumbnail;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public TrackDefinition Track => track;
        public RewardTable Rewards => rewards;
        public VehicleRestriction Restriction => restriction;
        public int RecommendedPerformanceRating => recommendedPerformanceRating;
        public WeatherType Weather => weather;
        public TimeOfDay TimeOfDay => timeOfDay;
        public UnlockRequirement UnlockRequirement => unlockRequirement;
        public AIProfile AIProfile => aiProfile;
        public VehicleDefinition[] AIVehiclePool => aiVehiclePool;
        public bool IsBossEvent => isBossEvent;
        public Sprite Thumbnail => thumbnail;

        public abstract RaceMode Mode { get; }
        /// <summary>Number of AI opponents in this event.</summary>
        public abstract int OpponentCount { get; }
        public abstract string ModeLabel { get; }

#if UNITY_EDITOR
        public void EditorInitializeBase(string newId, string newName, string newDescription, TrackDefinition newTrack,
            RewardTable newRewards, VehicleRestriction newRestriction, int recommendedPr, WeatherType newWeather,
            TimeOfDay newTime, UnlockRequirement unlock, AIProfile profile, VehicleDefinition[] pool, bool boss)
        {
            id = newId; displayName = newName; description = newDescription; track = newTrack; rewards = newRewards;
            restriction = newRestriction; recommendedPerformanceRating = recommendedPr; weather = newWeather;
            timeOfDay = newTime; unlockRequirement = unlock; aiProfile = profile; aiVehiclePool = pool; isBossEvent = boss;
        }
#endif
    }
}
