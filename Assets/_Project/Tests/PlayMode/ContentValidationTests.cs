using System.Collections.Generic;
using NUnit.Framework;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Events;
using RedlineLegends.Race;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Tests
{
    /// <summary>Static checks over the generated content: counts, references, scenes, unlock chains, ratings.</summary>
    public sealed class ContentValidationTests
    {
        private static ContentCatalog LoadCatalog()
        {
            var config = GameConfig.Load();
            Assert.IsNotNull(config, "GameConfig missing.");
            return new ContentCatalog(config.ContentDatabase);
        }

        [Test]
        public void Roster_HasFifteenCarsAcrossFourTiers()
        {
            var catalog = LoadCatalog();
            Assert.GreaterOrEqual(catalog.Vehicles.Count, 15);
            var perClass = new Dictionary<VehicleClass, int>();
            foreach (var v in catalog.Vehicles)
            {
                perClass[v.VehicleClass] = perClass.TryGetValue(v.VehicleClass, out int n) ? n + 1 : 1;
                Assert.IsNotNull(v.VisualPrefab, v.Id + " has no visual prefab.");
                Assert.AreEqual(9, v.UpgradeSlots.Length, v.Id + " should expose every upgrade category.");
                foreach (var slot in v.UpgradeSlots) Assert.IsNotNull(slot.Definition, v.Id + " slot " + slot.Category + " is empty.");
                Assert.Greater(v.PaintOptions.Length, 1, v.Id + " needs paint options.");
                int pr = VehicleSpecBuilder.BuildStock(v).PerformanceRating;
                Assert.That(pr, Is.InRange(PerformanceRatingCalculator.Min, PerformanceRatingCalculator.Max), v.Id + " PR out of range: " + pr);
                if (!string.IsNullOrEmpty(v.UnlockRequirement.RequiredChampionshipId))
                    Assert.IsTrue(catalog.TryGetChampionship(v.UnlockRequirement.RequiredChampionshipId, out _), v.Id + " unlock references a missing championship.");
            }
            foreach (VehicleClass cls in System.Enum.GetValues(typeof(VehicleClass)))
                Assert.GreaterOrEqual(perClass.TryGetValue(cls, out int count) ? count : 0, 3, "Need at least 3 cars in class " + cls);
        }

        [Test]
        public void Ratings_IncreaseByClass()
        {
            var catalog = LoadCatalog();
            var maxByClass = new Dictionary<VehicleClass, int>();
            var minByClass = new Dictionary<VehicleClass, int>();
            foreach (var v in catalog.Vehicles)
            {
                int pr = VehicleSpecBuilder.BuildStock(v).PerformanceRating;
                maxByClass[v.VehicleClass] = Mathf.Max(maxByClass.TryGetValue(v.VehicleClass, out int mx) ? mx : 0, pr);
                minByClass[v.VehicleClass] = Mathf.Min(minByClass.TryGetValue(v.VehicleClass, out int mn) ? mn : 9999, pr);
            }
            Assert.Less(maxByClass[VehicleClass.Street], minByClass[VehicleClass.Super], "Street cars must rate below Super cars.");
            Assert.Less(maxByClass[VehicleClass.Sport], minByClass[VehicleClass.Hyper], "Sport cars must rate below Hyper cars.");
            Assert.Less(minByClass[VehicleClass.Street], minByClass[VehicleClass.Sport]);
        }

        [Test]
        public void Career_HasTenChampionshipsAndFiftyEvents()
        {
            var catalog = LoadCatalog();
            int career = 0, careerEvents = 0, dragEvents = 0;
            foreach (var c in catalog.Championships)
            {
                if (c.Id == "chp_drag_ladder") { dragEvents += c.Events.Length; continue; }
                career++;
                Assert.AreEqual(5, c.Events.Length, c.Id + " should have five events.");
                careerEvents += c.Events.Length;
                for (int i = 0; i < c.Events.Length; i++)
                {
                    var evt = c.Events[i];
                    Assert.IsNotNull(evt, c.Id + " has a null event.");
                    Assert.IsNotNull(evt.Track, evt.Id + " has no track.");
                    Assert.IsTrue(Application.CanStreamedLevelBeLoaded(evt.Track.SceneName), evt.Id + " scene not in build: " + evt.Track.SceneName);
                    Assert.Greater(evt.RecommendedPerformanceRating, 0, evt.Id + " has no recommended PR.");
                    Assert.Greater(evt.Rewards.ForPosition(1).Credits, 0, evt.Id + " pays nothing.");
                    if (i > 0)
                    {
                        Assert.AreEqual(c.Events[i - 1].Id, evt.UnlockRequirement.RequiredEventId, evt.Id + " must unlock after the previous event.");
                    }
                    if (evt.Mode == RaceMode.Drag) Assert.IsTrue(evt.Track.SupportsDrag, evt.Id + " drag event on a non-drag track.");
                    if (evt is CircuitEventDefinition circuit && circuit.EventType == CircuitEventType.TimeAttack)
                        Assert.AreEqual(3, evt.Rewards.TimeAttackStarThresholds.Length, evt.Id + " time attack needs three star thresholds.");
                }
                Assert.IsTrue(c.Events[c.Events.Length - 1].IsBossEvent, c.Id + " should end with a boss event.");
            }
            Assert.AreEqual(10, career, "Expected ten career championships.");
            Assert.AreEqual(50, careerEvents, "Expected fifty career events.");
            Assert.AreEqual(12, dragEvents, "Expected a twelve-round drag ladder.");
        }

        [Test]
        public void Championships_UnlockInOrderWithReachableRatings()
        {
            var catalog = LoadCatalog();
            RedlineLegends.Career.ChampionshipDefinition previous = null;
            int previousPr = 0;
            foreach (var c in catalog.Championships)
            {
                if (c.Id == "chp_drag_ladder") continue;
                if (previous != null)
                {
                    Assert.AreEqual(previous.Id, c.UnlockRequirement.RequiredChampionshipId, c.Id + " must require the previous championship.");
                    Assert.GreaterOrEqual(c.Events[0].RecommendedPerformanceRating, previousPr, c.Id + " should not be easier than " + previous.Id);
                }
                previous = c;
                previousPr = c.Events[0].RecommendedPerformanceRating;
            }
        }
    }
}
