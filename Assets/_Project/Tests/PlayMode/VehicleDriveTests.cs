using System.Collections;
using NUnit.Framework;
using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Race;
using RedlineLegends.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RedlineLegends.Tests
{
    /// <summary>
    /// Drives the real car in the proving ground with a scripted input provider and checks the
    /// physics behaves like a car: accelerates forward, reaches road speed, brakes, and turns.
    /// </summary>
    public sealed class VehicleDriveTests
    {
        private static IEnumerator EnsureBooted()
        {
            if (Services.IsReady) yield break;
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            yield return null;
            var machine = Services.Get<GameStateMachine>();
            float end = Time.realtimeSinceStartup + 10f;
            while (machine.Current != GameStateId.MainMenu && Time.realtimeSinceStartup < end) yield return null;
        }

        private static IEnumerator LoadProvingGround()
        {
            yield return EnsureBooted();
            SceneManager.LoadScene("Track_ProvingGround");
            yield return null;
            yield return null;
        }

        private static void LogTelemetry(VehicleController car, float t)
        {
            var tel = car.Telemetry;
            var sb = new System.Text.StringBuilder();
            sb.Append("[DriveTest] t=").Append(t.ToString("0.0"))
              .Append(" speed=").Append(tel.SpeedKmh.ToString("0.0"))
              .Append(" gear=").Append(tel.Gear)
              .Append(" rpm=").Append(tel.Rpm.ToString("0"))
              .Append(" thr=").Append(tel.Throttle.ToString("0.00"))
              .Append(" torque=").Append(tel.EngineTorqueNm.ToString("0"))
              .Append(" drive=").Append(tel.DriveForceN.ToString("0"))
              .Append(" grounded=").Append(tel.GroundedWheels)
              .Append(" slip=").Append(tel.MaxSlip.ToString("0.00"))
              .Append(" pos=").Append(car.Body.position.ToString("0.00"))
              .Append(" rot=").Append(car.Body.rotation.eulerAngles.ToString("0"))
              .Append(" angVel=").Append(car.Body.angularVelocity.ToString("0.0"))
              .Append(" inertia=").Append(car.Body.inertiaTensor.ToString("0"));
            foreach (var w in car.Wheels)
                sb.Append(" | ").Append(w.Name).Append(" load=").Append(w.Load.ToString("0")).Append(" spin=").Append(w.SpinSpeed.ToString("0.0"))
                  .Append(" drv=").Append(w.DriveForce.ToString("0")).Append(" brk=").Append(w.BrakeForce.ToString("0"))
                  .Append(" comp=").Append(w.Compression.ToString("0.00")).Append(" vlong=").Append(w.LongVelocity.ToString("0.0"));
            Debug.Log(sb.ToString());
        }

        private static IEnumerator WaitForVehicle(TestDriveSession session, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (session.LocalVehicle == null && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsNotNull(session.LocalVehicle, "Test drive session never spawned a vehicle.");
        }

        [UnityTest]
        public IEnumerator Vehicle_AcceleratesBrakesAndSteers()
        {
            yield return LoadProvingGround();
            var session = Object.FindAnyObjectByType<TestDriveSession>();
            Assert.IsNotNull(session, "Proving ground has no TestDriveSession.");
            yield return WaitForVehicle(session, 5f);

            var car = session.LocalVehicle;
            var script = new AIInputProvider();
            car.SetInputProvider(script);
            // Let the suspension settle, logging so a bad stance is visible in the log.
            LogTelemetry(car, 0f);
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForSeconds(0.25f);
                LogTelemetry(car, -1.5f + (i + 1) * 0.25f);
            }
            Vector3 start = car.transform.position;
            Assert.Greater(car.Telemetry.GroundedWheels, 3, "Car should rest on all four wheels.");

            // Full throttle for 5 s, logging telemetry so a failure explains itself.
            script.SetAxes(0f, 1f, 0f, false, false);
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(0.5f);
                LogTelemetry(car, (i + 1) * 0.5f);
            }
            var tel = car.Telemetry;
            float travelled = Vector3.Dot(car.transform.position - start, Vector3.forward);
            // A 150 hp / 1180 kg starter car does roughly 0-100 km/h in 9 s, so 5 s should give 60+ km/h.
            Assert.Greater(tel.SpeedKmh, 60f, "Expected road speed after 5 s of full throttle, got " + tel.SpeedKmh.ToString("0") + " km/h.");
            Assert.Greater(travelled, 35f, "Car should have moved forward, moved " + travelled.ToString("0.0") + " m.");
            Assert.Greater(tel.Gear, 1, "Automatic gearbox should have upshifted (gear " + tel.Gear + ").");
            Assert.Less(Mathf.Abs(car.transform.position.x - start.x), 6f, "Car drifted sideways while driving straight.");
            Assert.Less(tel.Rpm, car.Stats.Engine.LimiterRpm + 200f, "RPM exceeded the limiter.");
            float speedBefore = tel.SpeedKmh;

            // Brake hard for 3 s.
            script.SetAxes(0f, 0f, 1f, false, false);
            yield return new WaitForSeconds(3f);
            tel = car.Telemetry;
            Assert.Less(tel.SpeedKmh, speedBefore * 0.35f, "Braking should shed most speed, still at " + tel.SpeedKmh.ToString("0") + " km/h.");

            // Accelerate again and hold full left for 3 s: heading must change and the car must stay upright.
            script.SetAxes(0f, 1f, 0f, false, false);
            yield return new WaitForSeconds(1.5f);
            float headingBefore = car.transform.eulerAngles.y;
            script.SetAxes(-1f, 0.8f, 0f, false, false);
            yield return new WaitForSeconds(3f);
            float turned = Mathf.Abs(Mathf.DeltaAngle(headingBefore, car.transform.eulerAngles.y));
            Assert.Greater(turned, 35f, "Steering left should rotate the car, turned " + turned.ToString("0") + " deg.");
            Assert.Greater(car.transform.up.y, 0.7f, "Car rolled over while cornering.");
            Assert.IsFalse(car.Telemetry.IsUpsideDown);

            // Reset returns to the spawn.
            script.RequestReset();
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);
            Assert.Less(Vector3.Distance(car.transform.position, start), 3f, "Reset should return the car to the spawn point.");
        }

        [UnityTest]
        public IEnumerator Vehicle_ManualShift_ReportsQuality()
        {
            yield return LoadProvingGround();
            var session = Object.FindAnyObjectByType<TestDriveSession>();
            yield return WaitForVehicle(session, 5f);
            var car = session.LocalVehicle;
            var script = new AIInputProvider();
            car.SetInputProvider(script);
            car.TransmissionMode = Save.TransmissionMode.Manual;
            yield return new WaitForSeconds(0.5f);

            int shifts = 0;
            ShiftQuality lastQuality = ShiftQuality.Good;
            car.Shifted += (from, to, rpm, quality) => { shifts++; lastQuality = quality; };

            script.SetAxes(0f, 1f, 0f, false, false);
            // Wait until near redline while actually driving, then shift: Perfect or Late, never Early.
            float end = Time.realtimeSinceStartup + 8f;
            while ((car.Telemetry.RpmNormalized < 0.92f || car.Telemetry.SpeedKmh < 20f) && Time.realtimeSinceStartup < end) yield return null;
            Assert.GreaterOrEqual(car.Telemetry.RpmNormalized, 0.92f, "Engine never approached the redline in first gear.");
            Assert.GreaterOrEqual(car.Telemetry.GroundedWheels, 3, "Car should be on the ground when revving out first gear.");
            Assert.Greater(car.Telemetry.SpeedKmh, 20f, "Car should be moving when first gear reaches the redline.");
            script.RequestShiftUp();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.AreEqual(1, shifts, "Manual upshift should fire exactly one Shifted event.");
            Assert.AreEqual(2, car.Telemetry.Gear);
            Assert.AreNotEqual(ShiftQuality.Early, lastQuality);
        }
    }
}
