using System.Collections;
using System.Collections.Generic;
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
    /// Every generated circuit must be drivable by the AI: a free-drive session with an autopilot
    /// must pass several gates in 60 s of race time without getting stuck. Catches layouts whose
    /// corners are too tight for the racing line or whose barriers trap cars.
    /// </summary>
    public sealed class TrackDrivabilityTests
    {
        public static IEnumerable<string> CircuitTrackIds()
        {
            yield return "trk_sunset_loop";
            yield return "trk_city_circuit";
            yield return "trk_night_run";
            yield return "trk_dune_pass";
            yield return "trk_alpine_climb";
            yield return "trk_cargo_yard";
            yield return "trk_ridge_highway";
            yield return "trk_grand_circuit";
        }

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
        public IEnumerator Track_IsDrivableByAutopilot([ValueSource(nameof(CircuitTrackIds))] string trackId)
        {
            yield return EnsureMenu();
            var catalog = Services.Get<ContentCatalog>();
            var garage = Services.Get<GarageService>();
            var profile = Services.Get<PlayerProfileService>();
            var flow = Services.Get<SceneFlowService>();
            var machine = Services.Get<GameStateMachine>();
            Assert.IsTrue(catalog.TryGetTrack(trackId, out var track), "Track missing: " + trackId);

            var request = new RaceLaunchBuilder(catalog, garage, profile).BuildPractice(track);
            Assert.IsNotNull(request);
            flow.LoadRace(request);
            float end = Time.realtimeSinceStartup + 20f;
            while (machine.Current != GameStateId.Race && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(GameStateId.Race, machine.Current, "Scene did not load for " + trackId);

            var session = Object.FindAnyObjectByType<RaceSession>();
            Assert.IsNotNull(session);
            yield return null;
            yield return null;
            Assert.IsNotNull(session.Player);
            Assert.Greater(session.Layout.CheckpointCount, 6, trackId + " has too few gates.");

            var car = session.Player.Vehicle;
            var autopilot = new AIInputProvider();
            car.SetInputProvider(autopilot);
            car.TransmissionMode = TransmissionMode.Automatic;
            var driver = new AIDriver(catalog.GetAIProfile("ai_pro"), session.Layout.RacingLine, car, autopilot, 11);

            int resets = 0;
            int gatesPassed = 0;
            float stuck = 0f;
            int airborneSteps = 0, steps = 0;
            float lastLog = -10f;
            Time.timeScale = 4f;
            try
            {
                while (session.RaceTime < 60f && session.State == RaceState.Racing)
                {
                    yield return new WaitForFixedUpdate();
                    driver.FixedTick(Time.fixedDeltaTime, session.RaceTime);
                    steps++;
                    // Running maximum: a fast lap wraps NextCheckpoint to 0 (and a finished practice
                    // lap ends the loop), so the count is sampled every step, not at the end.
                    gatesPassed = Mathf.Max(gatesPassed, (session.Player.Lap - 1) * session.Layout.CheckpointCount + session.Player.NextCheckpoint);
                    if (session.Player.Finished) gatesPassed = Mathf.Max(gatesPassed, session.Layout.CheckpointCount * session.Laps);
                    if (car.Telemetry.IsAirborne) airborneSteps++;
                    if (session.RaceTime - lastLog >= 3f)
                    {
                        lastLog = session.RaceTime;
                        var st = session.Player;
                        Debug.Log("[Drivability] " + trackId + " t=" + session.RaceTime.ToString("0") + " pos=" + car.Body.position.ToString("0.0")
                                  + " speed=" + car.Telemetry.SpeedKmh.ToString("0") + " grounded=" + car.Telemetry.GroundedWheels
                                  + " hint=" + driver.LineHint + " along=" + st.DistanceAlongLap.ToString("0") + " next=" + st.NextCheckpoint + " lap=" + st.Lap
                                  + " wrongWay=" + (st.WrongWayTime > 0.5f) + " touching=" + TestTelemetry.Touching(car));
                    }
                    stuck = car.Telemetry.SpeedKmh < 2f ? stuck + Time.fixedDeltaTime : 0f;
                    if (stuck > 6f)
                    {
                        resets++;
                        stuck = 0f;
                        session.ResetRacer(session.Player);
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }

            var p = session.Player;
            gatesPassed = Mathf.Max(gatesPassed, (p.Lap - 1) * session.Layout.CheckpointCount + p.NextCheckpoint);
            Debug.Log("[Drivability] " + trackId + " gates=" + gatesPassed + " resets=" + resets + " airborne=" + (100f * airborneSteps / Mathf.Max(1, steps)).ToString("0") + "%");
            Assert.GreaterOrEqual(gatesPassed, 4, trackId + ": autopilot passed only " + gatesPassed + " gates in 60 s.");
            Assert.LessOrEqual(resets, 1, trackId + ": autopilot got stuck " + resets + " times.");
            Assert.Less(airborneSteps, steps * 0.15f, trackId + ": car airborne too often (bumpy road?).");

            session.QuitToMenu();
            end = Time.realtimeSinceStartup + 15f;
            while (machine.Current != GameStateId.MainMenu && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
