using System.Collections.Generic;
using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Race;
using RedlineLegends.Save;
using UnityEngine;

namespace RedlineLegends.Vehicles
{
    /// <summary>
    /// Builds a drivable vehicle from a participant spec: rigidbody root, body collider sized from
    /// the visual, wheel setups read from the prefab's named wheel transforms, controller, visuals.
    /// Works for player, AI and future remote cars alike; only the input provider differs.
    /// </summary>
    public static class VehicleFactory
    {
        private static readonly List<Renderer> RendererBuffer = new List<Renderer>(32);

        public static VehicleController Spawn(RaceParticipantSpec spec, VehicleDefinition definition, IInputProvider input,
            Vector3 position, Quaternion rotation, TransmissionMode transmission, Transform parent = null)
        {
            var stats = spec.VehicleSpec != null ? spec.VehicleSpec.Stats : VehicleSpecBuilder.BuildStock(definition).Stats;
            var root = new GameObject("Vehicle_" + spec.Id.Value + "_" + definition.Id);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = stats.Chassis.MassKg;

            GameObject visual = null;
            if (definition.VisualPrefab != null)
            {
                visual = Object.Instantiate(definition.VisualPrefab, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                VehicleVisualUtility.ApplyPaint(visual, definition, spec.PaintIndex);
            }

            var wheels = BuildWheelSetups(root.transform, visual != null ? visual.transform : root.transform, stats.Tires.WheelRadiusM);
            var collider = root.AddComponent<BoxCollider>();
            ConfigureBodyCollider(collider, visual, wheels, stats);
            collider.sharedMaterial = VehicleController.BodyMaterial;

            var controller = root.AddComponent<VehicleController>();
            controller.TransmissionMode = transmission;
            controller.Initialize(stats, input, wheels);
            root.AddComponent<VehicleVisuals>().Initialize(controller);

            GameLayers.SetLayerRecursive(root, GameLayers.Vehicle);
            GameLog.Info("Spawned " + root.name + " collider center=" + collider.center.ToString("0.00") + " size=" + collider.size.ToString("0.00")
                         + " wheelFL=" + wheels[0].LocalHubAtRest.ToString("0.00") + " wheelRR=" + wheels[3].LocalHubAtRest.ToString("0.00")
                         + " radius=" + stats.Tires.WheelRadiusM.ToString("0.00") + " travel=" + stats.Suspension.TravelM.ToString("0.00")
                         + " ride=" + stats.Suspension.RideHeightM.ToString("0.00") + " com=" + stats.Chassis.CenterOfMassOffset.ToString("0.00"));
            return controller;
        }

        private static WheelSetup[] BuildWheelSetups(Transform root, Transform visualRoot, float radius)
        {
            string[] names = { VehicleVisualUtility.WheelFL, VehicleVisualUtility.WheelFR, VehicleVisualUtility.WheelRL, VehicleVisualUtility.WheelRR };
            var setups = new List<WheelSetup>(4);
            for (int i = 0; i < names.Length; i++)
            {
                var t = VehicleVisualUtility.FindDeep(visualRoot, names[i]);
                Vector3 local;
                if (t != null) local = root.InverseTransformPoint(t.position);
                else
                {
                    GameLog.Warn("Vehicle visual lacks wheel transform '" + names[i] + "'; using default placement.");
                    local = new Vector3(i % 2 == 0 ? -0.8f : 0.8f, radius, i < 2 ? 1.3f : -1.3f);
                }
                setups.Add(new WheelSetup
                {
                    Name = names[i],
                    LocalHubAtRest = local,
                    Radius = radius,
                    IsFront = i < 2,
                    IsLeft = i % 2 == 0,
                    Visual = t
                });
            }
            return setups.ToArray();
        }

        /// <summary>Body box from the renderers that are not wheels, slightly inset so wheels do the ground contact.</summary>
        private static void ConfigureBodyCollider(BoxCollider collider, GameObject visual, WheelSetup[] wheels, VehicleStats stats)
        {
            if (visual == null)
            {
                collider.center = new Vector3(0f, 0.6f, 0f);
                collider.size = new Vector3(1.8f, 0.9f, 4.2f);
                return;
            }
            RendererBuffer.Clear();
            visual.GetComponentsInChildren(true, RendererBuffer);
            var bounds = new Bounds();
            bool any = false;
            var root = visual.transform.parent;
            for (int i = 0; i < RendererBuffer.Count; i++)
            {
                var r = RendererBuffer[i];
                if (IsWheelRenderer(r.transform, wheels)) continue;
                var b = r.bounds;
                // Convert world bounds to root local (root is at the spawn pose already).
                Vector3 min = root.InverseTransformPoint(b.min);
                Vector3 max = root.InverseTransformPoint(b.max);
                var local = new Bounds((min + max) * 0.5f, Abs(max - min));
                if (!any) { bounds = local; any = true; }
                else bounds.Encapsulate(local);
            }
            if (!any)
            {
                collider.center = new Vector3(0f, 0.6f, 0f);
                collider.size = new Vector3(1.8f, 0.9f, 4.2f);
                return;
            }
            var size = bounds.size;
            var center = bounds.center;
            // Keep the box clear of the road even at full compression (rest is mid-travel), so the
            // wheels, not the body, carry the car on flat ground.
            float minY = stats.Suspension.TravelM * 0.5f + 0.06f;
            if (bounds.min.y < minY)
            {
                float cut = minY - bounds.min.y;
                size.y -= cut;
                center.y += cut * 0.5f;
            }
            collider.center = center;
            collider.size = new Vector3(size.x * 0.96f, size.y, size.z * 0.98f);
        }

        private static bool IsWheelRenderer(Transform t, WheelSetup[] wheels)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i].Visual;
                if (w != null && (t == w || t.IsChildOf(w))) return true;
            }
            return false;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
