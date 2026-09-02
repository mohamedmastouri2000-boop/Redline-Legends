using System.Collections;
using NUnit.Framework;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Progression;
using RedlineLegends.Save;
using RedlineLegends.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RedlineLegends.Tests
{
    /// <summary>
    /// End-to-end smoke test of the Phase 1 loop: boot from the Bootstrap scene, land in the main
    /// menu with services alive, hop to the garage and back. Runs against the real generated
    /// content and a real save file in the persistent data path.
    /// </summary>
    public sealed class BootFlowTests
    {
        private static IEnumerator WaitForState(GameStateMachine machine, GameStateId state, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (machine.Current != state && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(state, machine.Current, "Timed out waiting for state " + state + " (current " + machine.Current + ").");
        }

        [UnityTest]
        public IEnumerator Boot_LoadsMainMenu_WithServicesAndStarterCar()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            yield return null;

            Assert.IsTrue(Services.IsReady, "Services were not installed after Bootstrap loaded.");
            var machine = Services.Get<GameStateMachine>();
            yield return WaitForState(machine, GameStateId.MainMenu, 10f);
            Assert.AreEqual(SceneNames.MainMenu, SceneManager.GetActiveScene().name);

            var catalog = Services.Get<ContentCatalog>();
            Assert.Greater(catalog.Vehicles.Count, 0, "Content database has no vehicles.");
            Assert.Greater(catalog.Events.Count, 0, "Content database has no events.");

            var garage = Services.Get<GarageService>();
            Assert.IsNotNull(garage.SelectedVehicle, "No starter vehicle selected on a fresh profile.");
            var spec = garage.BuildSelectedSpec();
            Assert.IsNotNull(spec);
            Assert.GreaterOrEqual(spec.PerformanceRating, PerformanceRatingCalculator.Min);
            Assert.LessOrEqual(spec.PerformanceRating, PerformanceRatingCalculator.Max);

            var save = Services.Get<SaveService>();
            Assert.IsTrue(save.IsLoaded);
            Assert.AreNotEqual(SaveLoadResult.CorruptResetToNew, save.LastLoadResult);
        }

        [UnityTest]
        public IEnumerator Menu_ToGarage_AndBack()
        {
            if (!Services.IsReady)
            {
                SceneManager.LoadScene(SceneNames.Bootstrap);
                yield return null;
                yield return null;
            }
            var machine = Services.Get<GameStateMachine>();
            var flow = Services.Get<SceneFlowService>();
            yield return WaitForState(machine, GameStateId.MainMenu, 10f);

            flow.LoadGarage();
            yield return WaitForState(machine, GameStateId.Garage, 10f);
            Assert.AreEqual(SceneNames.Garage, SceneManager.GetActiveScene().name);
            // Give the garage a frame to instantiate the car visual and refresh its UI.
            yield return null;

            flow.LoadMainMenu();
            yield return WaitForState(machine, GameStateId.MainMenu, 10f);
            Assert.AreEqual(SceneNames.MainMenu, SceneManager.GetActiveScene().name);
        }

        [Test]
        public void PerformanceRating_OrdersCarsByCapability()
        {
            var config = GameConfig.Load();
            Assert.IsNotNull(config, "GameConfig missing from Resources.");
            var catalog = new ContentCatalog(config.ContentDatabase);
            int street = catalog.GetVehicle("veh_street_kestrel") != null
                ? VehicleSpecBuilder.BuildStock(catalog.GetVehicle("veh_street_kestrel")).PerformanceRating : -1;
            int sport = catalog.GetVehicle("veh_sport_stratos") != null
                ? VehicleSpecBuilder.BuildStock(catalog.GetVehicle("veh_sport_stratos")).PerformanceRating : -1;
            Assert.Greater(street, 0);
            Assert.Greater(sport, street, "Sport car should out-rate the starter street car.");

            var upgraded = VehicleSpecBuilder.BuildAtUniformStage(catalog.GetVehicle("veh_street_kestrel"), 3);
            Assert.Greater(upgraded.PerformanceRating, street, "Stage 3 upgrades must raise the rating.");
            Assert.Greater(upgraded.Stats.Engine.PeakTorqueNm, catalog.GetVehicle("veh_street_kestrel").BaseStats.Engine.PeakTorqueNm,
                "Upgrades must change the simulated stats, not just the number.");
        }
    }
}
