using System.Collections;
using NUnit.Framework;
using RedlineLegends.AI;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Progression;
using RedlineLegends.Race;
using RedlineLegends.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RedlineLegends.Tests
{
    /// <summary>
    /// The full circuit loop: menu launches the first career event, the player's car is driven by
    /// an AI driver (autopilot), the race completes, rewards land in the profile and the save.
    /// Runs at 4x time scale; physics steps stay identical.
    /// </summary>
    public sealed class CircuitRaceTests
    {
        private static IEnumerator EnsureMenu()
        {
            if (!Services.IsReady)
            {
                SceneManager.LoadScene(SceneNames.Bootstrap);
                yield return null;
                yield return null;
            }
            var machine = Services.Get<GameStateMachine>();
            // The test runner boots services in its own scene; ask the flow for the menu explicitly.
            if (machine.Current != GameStateId.MainMenu && machine.Current != GameStateId.Loading)
                Services.Get<SceneFlowService>().LoadMainMenu();
            float end = Time.realtimeSinceStartup + 10f;
            while (machine.Current != GameStateId.MainMenu && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.MainMenu, machine.Current);
        }

        [UnityTest]
        public IEnumerator SprintEvent_CompletesAndRewardsPlayer()
        {
            yield return EnsureMenu();
            var catalog = Services.Get<ContentCatalog>();
            var garage = Services.Get<GarageService>();
            var profile = Services.Get<PlayerProfileService>();
            var progression = Services.Get<ProgressionService>();
            var flow = Services.Get<SceneFlowService>();
            var machine = Services.Get<GameStateMachine>();

            var evt = catalog.GetEvent("evt_c01_e01");
            Assert.IsNotNull(evt, "First career event missing.");
            int creditsBefore = profile.Credits;
            int attemptsBefore = progression.FindEvent(evt.Id)?.Attempts ?? 0;
            int bestBefore = progression.FindEvent(evt.Id)?.BestPosition ?? 0;

            var request = new RaceLaunchBuilder(catalog, garage, profile).Build(evt, out string reason);
            Assert.IsNotNull(request, "Launch request failed: " + reason);
            flow.LoadRace(request);
            float end = Time.realtimeSinceStartup + 20f;
            while (machine.Current != GameStateId.Race && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.Race, machine.Current, "Race scene did not load.");

            var session = Object.FindAnyObjectByType<RaceSession>();
            Assert.IsNotNull(session, "Track scene has no RaceSession.");
            yield return null;
            yield return null;
            // A fresh profile shows the first-race tutorial; dismiss it the way a player would.
            var overlay = Object.FindAnyObjectByType<UI.TutorialOverlay>(FindObjectsInactive.Include);
            if (overlay != null && overlay.IsShowing) overlay.Finish();
            else if (session.WaitingForTutorial) session.BeginRace();
            Assert.IsNotNull(session.Player, "Session did not spawn the player.");
            Assert.AreEqual(request.Participants.Count, session.Racers.Count, "Not every participant was spawned.");
            Assert.Greater(session.Layout.CheckpointCount, 4, "Track needs checkpoints.");

            // Autopilot: drive the player's car with the same AI as opponents.
            var car = session.Player.Vehicle;
            var autopilot = new AIInputProvider();
            car.SetInputProvider(autopilot);
            car.TransmissionMode = TransmissionMode.Automatic;
            var driver = new AIDriver(catalog.GetAIProfile("ai_pro"), session.Layout.RacingLine, car, autopilot, 42);

            Time.timeScale = 4f;
            try
            {
                float raceEnd = Time.realtimeSinceStartup + 180f;
                float lastLog = -100f;
                float stuckTime = 0f;
                while (session.State != RaceState.Finished && Time.realtimeSinceStartup < raceEnd)
                {
                    yield return new WaitForFixedUpdate();
                    if (session.State == RaceState.Racing || session.State == RaceState.Finishing)
                    {
                        driver.FixedTick(Time.fixedDeltaTime, session.RaceTime);
                        // A stuck autopilot uses the reset button like a player would.
                        stuckTime = car.Telemetry.SpeedKmh < 2f ? stuckTime + Time.fixedDeltaTime : 0f;
                        if (stuckTime > 6f)
                        {
                            Debug.Log("[RaceTest] autopilot stuck at " + car.Body.position.ToString("0.0") + ", resetting");
                            session.ResetRacer(session.Player);
                            stuckTime = 0f;
                        }
                    }
                    float interval = session.RaceTime < 20f ? 2f : 10f;
                    if (session.RaceTime - lastLog >= interval)
                    {
                        lastLog = session.RaceTime;
                        var p = session.Player;
                        var cmd = autopilot.Peek();
                        Debug.Log("[RaceTest] lap=" + p.Lap + " next=" + p.NextCheckpoint + " pos=P" + p.Position
                                  + " progress=" + p.TotalProgress.ToString("0")
                                  + " cmd(s/t/b)=" + cmd.Steer.ToString("0.00") + "/" + cmd.Throttle.ToString("0.00") + "/" + cmd.Brake.ToString("0.00")
                                  + " hold=" + car.HoldBrakes + " state=" + session.State + " touching=" + TestTelemetry.Touching(car)
                                  + "\n" + TestTelemetry.Describe(car, "RaceTest", session.RaceTime));
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }

            Assert.AreEqual(RaceState.Finished, session.State, "Race did not finish within the time limit (player lap " + session.Player.Lap + ", next gate " + session.Player.NextCheckpoint + ").");
            var outcome = session.Outcome;
            Assert.IsNotNull(outcome);
            var playerResult = outcome.FindLocalPlayer();
            Assert.IsNotNull(playerResult);
            Assert.IsTrue(playerResult.Finished, "Player should have finished.");
            Assert.That(playerResult.Position, Is.InRange(1, request.Participants.Count));
            Assert.Greater(playerResult.TotalTimeSeconds, 20f, "A 1.6 km lap cannot take under 20 s.");
            Assert.Greater(playerResult.BestLapSeconds, 0f);

            Assert.Greater(session.Reward.Credits, 0, "Finishing must pay credits.");
            Assert.Greater(profile.Credits, creditsBefore, "Credits were not added to the profile.");
            var progress = progression.FindEvent(evt.Id);
            Assert.IsNotNull(progress, "Event progress was not recorded.");
            Assert.AreEqual(attemptsBefore + 1, progress.Attempts);
            int expectedBest = bestBefore == 0 ? playerResult.Position : Mathf.Min(bestBefore, playerResult.Position);
            Assert.AreEqual(expectedBest, progress.BestPosition, "Best position must only improve.");

            // Reload the save from disk: the reward must have been persisted.
            var config = Services.Get<GameConfig>();
            var reloaded = new SaveService(new FileSaveStore(), new SaveMigrationPipeline(), config.SaveFileName, config.DefaultSettings, 0);
            reloaded.Load();
            Assert.AreEqual(SaveLoadResult.Loaded, reloaded.LastLoadResult);
            Assert.AreEqual(profile.Credits, reloaded.Data.Profile.Credits, "Saved credits differ from the live profile.");

            session.ContinueToMenu();
            end = Time.realtimeSinceStartup + 15f;
            while (machine.Current != GameStateId.MainMenu && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.MainMenu, machine.Current, "Continue should return to the main menu.");
        }
    }
}
