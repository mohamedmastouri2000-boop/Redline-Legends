using System;
using System.Collections.Generic;
using RedlineLegends.AI;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Events;
using RedlineLegends.Progression;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;

namespace RedlineLegends.Race
{
    /// <summary>
    /// Builds a <see cref="RaceLaunchRequest"/> for an event: the local player in the selected car
    /// plus AI opponents in cars chosen from the event's pool (or the database, matched to the
    /// event's recommended rating). A lobby would produce the same request from remote players.
    /// </summary>
    public sealed class RaceLaunchBuilder
    {
        private const string LocalPlayerName = "You";
        private static readonly string[] OpponentNames =
        {
            "Kai", "Nova", "Drift", "Rex", "Zed", "Mira", "Jax", "Vex", "Ash", "Ryo", "Lux", "Sable"
        };

        private readonly ContentCatalog _catalog;
        private readonly GarageService _garage;
        private readonly PlayerProfileService _profile;

        public RaceLaunchBuilder(ContentCatalog catalog, GarageService garage, PlayerProfileService profile)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>Free drive on a track with the selected car and no opponents.</summary>
        public RaceLaunchRequest BuildPractice(Tracks.TrackDefinition track)
        {
            if (track == null) return null;
            var vehicle = _garage.SelectedVehicle;
            if (vehicle == null) return null;
            var request = new RaceLaunchRequest
            {
                EventId = "",
                EventDisplayName = "Test Drive",
                Mode = RaceMode.Circuit,
                TrackId = track.Id,
                TrackSceneName = track.SceneName,
                Seed = Environment.TickCount,
                IsPractice = true
            };
            request.Participants.Add(new RaceParticipantSpec
            {
                Id = new RacerId(1),
                DisplayName = _profile.DisplayName ?? LocalPlayerName,
                VehicleId = vehicle.Id,
                ControlSource = ControlSource.LocalPlayer,
                GridSlot = 0,
                VehicleSpec = _garage.BuildSpec(vehicle.Id),
                PaintIndex = _garage.GetOwned(vehicle.Id)?.PaintIndex ?? 0
            });
            return request;
        }

        /// <summary>Returns null (with a reason) when the selected car cannot enter.</summary>
        public RaceLaunchRequest Build(RaceEventDefinition evt, out string failReason)
        {
            failReason = null;
            if (evt == null || evt.Track == null)
            {
                failReason = "Event has no track.";
                return null;
            }

            var playerVehicle = _garage.SelectedVehicle;
            if (playerVehicle == null)
            {
                failReason = "No vehicle selected.";
                return null;
            }
            var playerSpec = _garage.BuildSpec(playerVehicle.Id);
            if (!evt.Restriction.Allows(playerVehicle, playerSpec.PerformanceRating))
            {
                failReason = "Your car does not meet the entry requirements: " + evt.Restriction.Describe();
                return null;
            }

            int seed = unchecked(Environment.TickCount * 397) ^ evt.Id.GetHashCode();
            var rng = new Xorshift(seed);

            var request = new RaceLaunchRequest
            {
                EventId = evt.Id,
                EventDisplayName = evt.DisplayName,
                Mode = evt.Mode,
                TrackId = evt.Track.Id,
                TrackSceneName = evt.Track.SceneName,
                Seed = seed
            };

            int racerCount = Math.Min(evt.OpponentCount + 1, Math.Max(1, evt.Track.MaxParticipants));
            int opponents = racerCount - 1;

            // Player starts at the back of the grid in circuit races (earning position is the game);
            // drag races have two lanes so slot is lane index.
            int playerSlot = evt.Mode == RaceMode.Drag ? 0 : racerCount - 1;

            request.Participants.Add(new RaceParticipantSpec
            {
                Id = new RacerId(1),
                DisplayName = _profile.DisplayName ?? LocalPlayerName,
                VehicleId = playerVehicle.Id,
                ControlSource = ControlSource.LocalPlayer,
                GridSlot = playerSlot,
                VehicleSpec = playerSpec,
                PaintIndex = _garage.GetOwned(playerVehicle.Id)?.PaintIndex ?? 0
            });

            var profile = evt.AIProfile;
            var pool = BuildOpponentPool(evt, playerSpec.PerformanceRating);
            var usedNames = new HashSet<string>();
            int slot = 0;
            for (int i = 0; i < opponents; i++)
            {
                if (slot == playerSlot) slot++;
                VehicleDefinition car = null;
                if (evt is DragEventDefinition drag && drag.OpponentVehicle != null)
                    car = drag.OpponentVehicle;
                else if (pool.Count > 0)
                    car = pool[rng.Range(0, pool.Count)];
                if (car == null) car = playerVehicle;

                int stage = profile != null ? profile.VehicleUpgradeStage : 0;
                var spec = VehicleSpecBuilder.BuildAtUniformStage(car, stage);

                string name = evt is DragEventDefinition d2 && !string.IsNullOrEmpty(d2.OpponentName)
                    ? d2.OpponentName
                    : PickName(ref rng, usedNames);

                request.Participants.Add(new RaceParticipantSpec
                {
                    Id = new RacerId(2 + i),
                    DisplayName = name,
                    VehicleId = car.Id,
                    ControlSource = ControlSource.AI,
                    AIProfileId = profile != null ? profile.Id : "",
                    GridSlot = slot++,
                    VehicleSpec = spec,
                    PaintIndex = car.PaintOptions.Length > 0 ? rng.Range(0, car.PaintOptions.Length) : 0
                });
            }

            return request;
        }

        /// <summary>Event pool if authored, otherwise cars whose stock rating is close to the event's target.</summary>
        private List<VehicleDefinition> BuildOpponentPool(RaceEventDefinition evt, int playerRating)
        {
            var pool = new List<VehicleDefinition>();
            if (evt.AIVehiclePool != null)
                for (int i = 0; i < evt.AIVehiclePool.Length; i++)
                    if (evt.AIVehiclePool[i] != null) pool.Add(evt.AIVehiclePool[i]);
            if (pool.Count > 0) return pool;

            int target = evt.RecommendedPerformanceRating > 0 ? evt.RecommendedPerformanceRating : playerRating;
            int stage = evt.AIProfile != null ? evt.AIProfile.VehicleUpgradeStage : 0;
            int window = 60;
            while (pool.Count == 0 && window <= 400)
            {
                var vehicles = _catalog.Vehicles;
                for (int i = 0; i < vehicles.Count; i++)
                {
                    var def = vehicles[i];
                    if (!evt.Restriction.Allows(def, VehicleSpecBuilder.BuildAtUniformStage(def, stage).PerformanceRating)) continue;
                    int pr = VehicleSpecBuilder.BuildAtUniformStage(def, stage).PerformanceRating;
                    if (Math.Abs(pr - target) <= window) pool.Add(def);
                }
                window *= 2;
            }
            if (pool.Count == 0)
                GameLog.Warn("No opponent vehicles fit event '" + evt.Id + "'; opponents will mirror the player's car.");
            return pool;
        }

        private static string PickName(ref Xorshift rng, HashSet<string> used)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                string name = OpponentNames[rng.Range(0, OpponentNames.Length)];
                if (used.Add(name)) return name;
            }
            return "Racer " + (used.Count + 1);
        }
    }
}
