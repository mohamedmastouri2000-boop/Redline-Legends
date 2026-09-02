using System;
using System.Collections.Generic;
using RedlineLegends.AI;
using RedlineLegends.Career;
using RedlineLegends.Core;
using RedlineLegends.Events;
using RedlineLegends.Tracks;
using RedlineLegends.Upgrades;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;

namespace RedlineLegends.Content
{
    /// <summary>
    /// Runtime id-indexed view over the ContentDatabase. Built once at boot; validates that every
    /// id is stable and unique so a bad asset fails loudly in the editor rather than silently in a
    /// player's save.
    /// </summary>
    public sealed class ContentCatalog
    {
        private readonly Dictionary<string, VehicleDefinition> _vehicles = new Dictionary<string, VehicleDefinition>();
        private readonly Dictionary<string, VehicleUpgradeDefinition> _upgrades = new Dictionary<string, VehicleUpgradeDefinition>();
        private readonly Dictionary<string, TrackDefinition> _tracks = new Dictionary<string, TrackDefinition>();
        private readonly Dictionary<string, RaceEventDefinition> _events = new Dictionary<string, RaceEventDefinition>();
        private readonly Dictionary<string, ChampionshipDefinition> _championships = new Dictionary<string, ChampionshipDefinition>();
        private readonly Dictionary<string, AIProfile> _aiProfiles = new Dictionary<string, AIProfile>();
        private readonly Dictionary<string, ChampionshipDefinition> _eventToChampionship = new Dictionary<string, ChampionshipDefinition>();

        public ContentDatabase Source { get; }
        public IReadOnlyList<VehicleDefinition> Vehicles { get; }
        public IReadOnlyList<TrackDefinition> Tracks { get; }
        public IReadOnlyList<RaceEventDefinition> Events { get; }
        public IReadOnlyList<ChampionshipDefinition> Championships { get; }
        public IReadOnlyList<AIProfile> AIProfiles { get; }

        public ContentCatalog(ContentDatabase database)
        {
            Source = database ?? throw new ArgumentNullException(nameof(database));
            Vehicles = Index(database.Vehicles, _vehicles, v => v.Id, "Vehicle");
            Index(database.Upgrades, _upgrades, u => u.Id, "Upgrade");
            Tracks = Index(database.Tracks, _tracks, t => t.Id, "Track");
            Events = Index(database.Events, _events, e => e.Id, "Event");
            Championships = Index(database.Championships, _championships, c => c.Id, "Championship");
            AIProfiles = Index(database.AIProfiles, _aiProfiles, a => a.Id, "AIProfile");

            foreach (var championship in Championships)
            {
                var events = championship.Events;
                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i] == null)
                    {
                        GameLog.Error("Championship '" + championship.Id + "' has a null event at index " + i + ".");
                        continue;
                    }
                    _eventToChampionship[events[i].Id] = championship;
                }
            }
        }

        private static List<T> Index<T>(T[] source, Dictionary<string, T> map, Func<T, string> idOf, string kind)
            where T : UnityEngine.Object
        {
            var list = new List<T>(source != null ? source.Length : 0);
            if (source == null) return list;
            for (int i = 0; i < source.Length; i++)
            {
                var item = source[i];
                if (item == null)
                {
                    GameLog.Error(kind + " database entry " + i + " is null.");
                    continue;
                }
                string id = idOf(item);
                if (!StableId.IsValid(id))
                {
                    GameLog.Error(kind + " '" + item.name + "' has invalid id '" + id + "'.");
                    continue;
                }
                if (map.ContainsKey(id))
                {
                    GameLog.Error(kind + " id '" + id + "' is duplicated by '" + item.name + "'.");
                    continue;
                }
                map.Add(id, item);
                list.Add(item);
            }
            return list;
        }

        public VehicleDefinition GetVehicle(string id) => Get(_vehicles, id, "Vehicle");
        public VehicleUpgradeDefinition GetUpgrade(string id) => Get(_upgrades, id, "Upgrade");
        public TrackDefinition GetTrack(string id) => Get(_tracks, id, "Track");
        public RaceEventDefinition GetEvent(string id) => Get(_events, id, "Event");
        public ChampionshipDefinition GetChampionship(string id) => Get(_championships, id, "Championship");
        public AIProfile GetAIProfile(string id) => Get(_aiProfiles, id, "AIProfile");

        public bool TryGetVehicle(string id, out VehicleDefinition def) => _vehicles.TryGetValue(id ?? "", out def);
        public bool TryGetEvent(string id, out RaceEventDefinition def) => _events.TryGetValue(id ?? "", out def);
        public bool TryGetChampionship(string id, out ChampionshipDefinition def) => _championships.TryGetValue(id ?? "", out def);
        public bool TryGetAIProfile(string id, out AIProfile def) => _aiProfiles.TryGetValue(id ?? "", out def);

        public ChampionshipDefinition FindChampionshipForEvent(string eventId)
        {
            _eventToChampionship.TryGetValue(eventId ?? "", out var championship);
            return championship;
        }

        private static T Get<T>(Dictionary<string, T> map, string id, string kind) where T : class
        {
            if (id != null && map.TryGetValue(id, out var value)) return value;
            GameLog.Error(kind + " with id '" + id + "' is not in the content database.");
            return null;
        }
    }
}
