using System.Text;
using RedlineLegends.Core;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Tests
{
    /// <summary>Shared diagnostics so a failing physics test explains itself in the log.</summary>
    public static class TestTelemetry
    {
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        public static string Describe(VehicleController car, string tag, float t)
        {
            var tel = car.Telemetry;
            var sb = new StringBuilder(512);
            sb.Append('[').Append(tag).Append("] t=").Append(t.ToString("0.0"))
              .Append(" speed=").Append(tel.SpeedKmh.ToString("0.0"))
              .Append(" gear=").Append(tel.Gear)
              .Append(" rpm=").Append(tel.Rpm.ToString("0"))
              .Append(" thr=").Append(tel.Throttle.ToString("0.00"))
              .Append(" brk=").Append(tel.Brake.ToString("0.00"))
              .Append(" torque=").Append(tel.EngineTorqueNm.ToString("0"))
              .Append(" drive=").Append(tel.DriveForceN.ToString("0"))
              .Append(" grounded=").Append(tel.GroundedWheels)
              .Append(" pos=").Append(car.Body.position.ToString("0.00"))
              .Append(" rot=").Append(car.Body.rotation.eulerAngles.ToString("0"))
              .Append(" vel=").Append(car.Body.linearVelocity.ToString("0.00"));
            foreach (var w in car.Wheels)
                sb.Append(" | ").Append(w.Name).Append(" load=").Append(w.Load.ToString("0")).Append(" spin=").Append(w.SpinSpeed.ToString("0.0"))
                  .Append(" drv=").Append(w.DriveForce.ToString("0")).Append(" brk=").Append(w.BrakeForce.ToString("0"))
                  .Append(" comp=").Append(w.Compression.ToString("0.00")).Append(" vlong=").Append(w.LongVelocity.ToString("0.0"))
                  .Append(" hit=").Append(w.Grounded ? w.ContactPoint.ToString("0.00") : "none");
            return sb.ToString();
        }

        /// <summary>Names of non-vehicle colliders overlapping the car's body box (what is it touching?).</summary>
        public static string Touching(VehicleController car)
        {
            var box = car.GetComponent<BoxCollider>();
            if (box == null) return "(no box)";
            var t = car.transform;
            int count = Physics.OverlapBoxNonAlloc(t.TransformPoint(box.center), box.size * 0.6f, OverlapBuffer, t.rotation,
                ~0, QueryTriggerInteraction.Ignore);
            var sb = new StringBuilder();
            int listed = 0;
            for (int i = 0; i < count; i++)
            {
                var c = OverlapBuffer[i];
                if (c.attachedRigidbody == car.Body) continue;
                if (listed > 0) sb.Append(", ");
                sb.Append(c.name).Append('(').Append(c.GetType().Name).Append(" layer ").Append(c.gameObject.layer).Append(')');
                listed++;
            }
            return listed == 0 ? "nothing" : sb.ToString();
        }
    }
}
