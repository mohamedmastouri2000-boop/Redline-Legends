using System;
using RedlineLegends.AI;
using RedlineLegends.Career;
using RedlineLegends.Events;
using RedlineLegends.Progression;
using RedlineLegends.Tracks;
using RedlineLegends.Upgrades;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Content
{
    /// <summary>
    /// The single authored list of all content assets. Adding a car/track/event means adding it
    /// here (the editor tooling does it automatically). Runtime code uses <see cref="ContentCatalog"/>
    /// for id lookups; this asset is just the serialized source.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentDatabase", menuName = "Redline Legends/Content Database")]
    public sealed class ContentDatabase : ScriptableObject
    {
        [SerializeField] private VehicleDefinition[] vehicles = Array.Empty<VehicleDefinition>();
        [SerializeField] private VehicleUpgradeDefinition[] upgrades = Array.Empty<VehicleUpgradeDefinition>();
        [SerializeField] private TrackDefinition[] tracks = Array.Empty<TrackDefinition>();
        [SerializeField] private RaceEventDefinition[] events = Array.Empty<RaceEventDefinition>();
        [SerializeField] private ChampionshipDefinition[] championships = Array.Empty<ChampionshipDefinition>();
        [SerializeField] private AIProfile[] aiProfiles = Array.Empty<AIProfile>();
        [SerializeField] private AchievementDefinition[] achievements = Array.Empty<AchievementDefinition>();

        public VehicleDefinition[] Vehicles => vehicles;
        public AchievementDefinition[] Achievements => achievements;
        public VehicleUpgradeDefinition[] Upgrades => upgrades;
        public TrackDefinition[] Tracks => tracks;
        public RaceEventDefinition[] Events => events;
        public ChampionshipDefinition[] Championships => championships;
        public AIProfile[] AIProfiles => aiProfiles;

#if UNITY_EDITOR
        public void EditorSetContent(VehicleDefinition[] v, VehicleUpgradeDefinition[] u, TrackDefinition[] t,
            RaceEventDefinition[] e, ChampionshipDefinition[] c, AIProfile[] a, AchievementDefinition[] ach)
        {
            vehicles = v; upgrades = u; tracks = t; events = e; championships = c; aiProfiles = a; achievements = ach;
        }
#endif
    }
}
