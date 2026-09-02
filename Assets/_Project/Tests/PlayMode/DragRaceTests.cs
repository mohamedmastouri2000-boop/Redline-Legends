using System.Collections;
using NUnit.Framework;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.DragRace;
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
    /// Drag loop: launch the first drag event, drive the player's car with a drag AI (manual shifts),
    /// race to the quarter mile, verify timing, reaction, shift scoring, rewards and persistence.
    /// </summary>
    public sealed class DragRaceTests
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
            if (machine.Current != GameStateId.MainMenu && machine.Current != GameStateId.Loading)
                Services.Get<SceneFlowService>().LoadMainMenu();
            float end = Time.realtimeSinceStartup + 10f;
            while (machine.Current != GameStateId.MainMenu && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.MainMenu, machine.Current);
        }

        [UnityTest]
        public IEnumerator QuarterMile_CompletesWithTimingAndReward()
        {
            yield return EnsureMenu();
            var catalog = Services.Get<ContentCatalog>();
            var garage = Services.Get<GarageService>();
            var profile = Services.Get<PlayerProfileService>();
            var progression = Services.Get<ProgressionService>();
            var flow = Services.Get<SceneFlowService>();
            var machine = Services.Get<GameStateMachine>();

            var evt = catalog.GetEvent("evt_c01_e04");
            Assert.IsNotNull(evt, "Drag event missing.");
            Assert.AreEqual(RaceMode.Drag, evt.Mode);
            int creditsBefore = profile.Credits;

            var request = new RaceLaunchBuilder(catalog, garage, profile).Build(evt, out string reason);
            Assert.IsNotNull(request, "Launch request failed: " + reason);
            Assert.AreEqual(2, request.Participants.Count, "Drag races are one-on-one.");
            flow.LoadRace(request);
            float end = Time.realtimeSinceStartup + 20f;
            while (machine.Current != GameStateId.Race && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.Race, machine.Current, "Drag scene did not load.");

            var session = Object.FindAnyObjectByType<DragSession>();
            Assert.IsNotNull(session, "Strip scene has no DragSession.");
            yield return null;
            yield return null;
            var overlay = Object.FindAnyObjectByType<UI.TutorialOverlay>(FindObjectsInactive.Include);
            if (overlay != null && overlay.IsShowing) overlay.Finish();
            else if (session.WaitingForTutorial) session.BeginRace();
            Assert.IsNotNull(session.Player);
            Assert.AreEqual(2, session.Racers.Count);

            // Autopilot with manual shifting so shift scoring is exercised.
            var car = session.Player.Vehicle;
            var autopilot = new AIInputProvider();
            car.SetInputProvider(autopilot);
            car.TransmissionMode = TransmissionMode.Manual;
            var driver = new DragAIDriver(catalog.GetAIProfile("ai_expert"), car, autopilot, 7);
            driver.SetLane(car.transform.position, session.transform.forward, session.DistanceMeters);
            int shifts = 0;
            session.PlayerShifted += q => shifts++;
            bool sawGreen = false;
            session.LightChanged += stage => { if (stage >= 4) sawGreen = true; };

            Time.timeScale = 4f;
            try
            {
                float raceEnd = Time.realtimeSinceStartup + 120f;
                float lastLog = -10f;
                bool lightsNotified = false;
                bool greenNotified = false;
                while (session.State != DragState.Finished && Time.realtimeSinceStartup < raceEnd)
                {
                    yield return new WaitForFixedUpdate();
                    if (session.State == DragState.Lights && !lightsNotified) { driver.NotifyLightsStarted(); lightsNotified = true; }
                    if (session.State >= DragState.Racing && !greenNotified)
                    {
                        driver.NotifyGreen(session.SessionTime - session.RaceTime);
                        greenNotified = true;
                    }
                    if (session.State != DragState.Finished)
                        driver.FixedTick(Time.fixedDeltaTime, session.SessionTime, 0f, float.MaxValue);
                    if (session.SessionTime - lastLog >= 2f)
                    {
                        lastLog = session.SessionTime;
                        var p = session.Player;
                        Debug.Log("[DragTest] state=" + session.State + " lights=" + session.LightStage + " rt=" + p.ReactionTime.ToString("0.000")
                                  + " false=" + p.FalseStart + " along=" + p.TotalProgress.ToString("0") + " opp=" + (session.Opponent?.TotalProgress ?? 0f).ToString("0")
                                  + "\n" + TestTelemetry.Describe(car, "DragTest", session.SessionTime));
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }

            Assert.IsTrue(sawGreen, "Light tree never reached green.");
            Assert.AreEqual(DragState.Finished, session.State, "Drag race did not finish (player at " + session.Player.TotalProgress.ToString("0") + " m).");
            var outcome = session.Outcome;
            var player = outcome.FindLocalPlayer();
            Assert.IsNotNull(player);
            Assert.IsTrue(player.Finished, "Player should have crossed the finish line.");
            Assert.IsFalse(player.FalseStart, "Expert autopilot should not red-light.");
            Assert.That(player.ReactionTimeSeconds, Is.InRange(0.05f, 1.0f), "Reaction time should be sane, got " + player.ReactionTimeSeconds);
            Assert.That(player.TotalTimeSeconds, Is.InRange(8f, 30f), "Quarter mile time out of range: " + player.TotalTimeSeconds);
            Assert.Greater(shifts, 1, "Manual autopilot should have shifted at least twice.");
            Assert.That(player.Position, Is.InRange(1, 2));

            Assert.Greater(session.Reward.Credits, 0, "Finishing a drag race must pay credits.");
            Assert.Greater(profile.Credits, creditsBefore);
            var progress = progression.FindEvent(evt.Id);
            Assert.IsNotNull(progress);
            Assert.Greater(progress.BestReactionSeconds, 0f, "Best reaction time must be recorded.");
            Assert.Greater(progress.BestTimeSeconds, 0f);

            var config = Services.Get<GameConfig>();
            var reloaded = new SaveService(new FileSaveStore(), new SaveMigrationPipeline(), config.SaveFileName, config.DefaultSettings, 0);
            reloaded.Load();
            Assert.AreEqual(profile.Credits, reloaded.Data.Profile.Credits);

            session.ContinueToMenu();
            end = Time.realtimeSinceStartup + 15f;
            while (machine.Current != GameStateId.MainMenu && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.MainMenu, machine.Current);
        }
    }
}
