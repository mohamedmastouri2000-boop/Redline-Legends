using RedlineLegends.Vehicles;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Generates stand-in car visuals from primitives that follow the VehicleVisualUtility naming
    /// contract (Wheel_FL etc.). Real modelled cars replace these prefabs one-for-one; nothing in
    /// gameplay references the primitive shapes.
    /// </summary>
    public static class PlaceholderCarBuilder
    {
        public struct Shape
        {
            public float Length;      // metres, along +Z
            public float Width;
            public float BodyHeight;  // sill to roofline of the lower body
            public float CabinLength;
            public float CabinHeight;
            public float CabinOffset; // cabin centre along Z (negative = rearwards)
            public float WheelRadius;
            public float WheelWidth;
            public float Wheelbase;
            public float Track;
            public float RideHeight;
        }

        public static Shape StreetShape => new Shape
        {
            Length = 4.2f, Width = 1.78f, BodyHeight = 0.55f, CabinLength = 1.9f, CabinHeight = 0.55f, CabinOffset = -0.1f,
            WheelRadius = 0.32f, WheelWidth = 0.22f, Wheelbase = 2.6f, Track = 1.55f, RideHeight = 0.32f
        };

        public static Shape SportShape => new Shape
        {
            Length = 4.5f, Width = 1.88f, BodyHeight = 0.5f, CabinLength = 1.7f, CabinHeight = 0.45f, CabinOffset = -0.25f,
            WheelRadius = 0.34f, WheelWidth = 0.26f, Wheelbase = 2.7f, Track = 1.62f, RideHeight = 0.28f
        };

        public static Shape SuperShape => new Shape
        {
            Length = 4.6f, Width = 1.98f, BodyHeight = 0.45f, CabinLength = 1.5f, CabinHeight = 0.4f, CabinOffset = -0.35f,
            WheelRadius = 0.35f, WheelWidth = 0.3f, Wheelbase = 2.65f, Track = 1.68f, RideHeight = 0.25f
        };

        public static Shape HyperShape => new Shape
        {
            Length = 4.7f, Width = 2.02f, BodyHeight = 0.42f, CabinLength = 1.4f, CabinHeight = 0.38f, CabinOffset = -0.4f,
            WheelRadius = 0.36f, WheelWidth = 0.32f, Wheelbase = 2.75f, Track = 1.7f, RideHeight = 0.24f
        };

        public static Shape ShapeFor(VehicleClass cls)
        {
            switch (cls)
            {
                case VehicleClass.Sport: return SportShape;
                case VehicleClass.Super: return SuperShape;
                case VehicleClass.Hyper: return HyperShape;
                default: return StreetShape;
            }
        }

        public static GameObject BuildPrefab(string vehicleId, VehicleClass cls, Material paint, Material glass,
            Material tire, Material rim, Material trim, Material lightFront, Material lightRear)
        {
            var shape = ShapeFor(cls);
            string path = EditorPaths.VehiclePrefabs + "/" + vehicleId + "_visual.prefab";
            EditorPaths.EnsureFolder(EditorPaths.VehiclePrefabs);

            var root = new GameObject(vehicleId + "_visual");
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);

            float floorY = shape.RideHeight;
            // Lower body
            Box(body.transform, "Chassis", paint,
                new Vector3(0f, floorY + shape.BodyHeight * 0.5f, 0f),
                new Vector3(shape.Width, shape.BodyHeight, shape.Length));
            // Cabin: slightly narrower, sits on the body
            float cabinY = floorY + shape.BodyHeight + shape.CabinHeight * 0.5f - 0.02f;
            Box(body.transform, "CabinPillars", paint,
                new Vector3(0f, cabinY, shape.CabinOffset),
                new Vector3(shape.Width * 0.9f, shape.CabinHeight, shape.CabinLength));
            Box(body.transform, "Glass", glass,
                new Vector3(0f, cabinY, shape.CabinOffset),
                new Vector3(shape.Width * 0.91f, shape.CabinHeight * 0.75f, shape.CabinLength * 1.02f));
            // Bumpers / sills
            Box(body.transform, "FrontBumper", trim,
                new Vector3(0f, floorY + 0.18f, shape.Length * 0.5f - 0.02f),
                new Vector3(shape.Width * 0.98f, 0.28f, 0.16f));
            Box(body.transform, "RearBumper", trim,
                new Vector3(0f, floorY + 0.18f, -shape.Length * 0.5f + 0.02f),
                new Vector3(shape.Width * 0.98f, 0.28f, 0.16f));
            // Lights
            float lightY = floorY + shape.BodyHeight * 0.72f;
            Box(body.transform, "HeadlightL", lightFront, new Vector3(-shape.Width * 0.34f, lightY, shape.Length * 0.5f + 0.005f), new Vector3(0.36f, 0.12f, 0.03f));
            Box(body.transform, "HeadlightR", lightFront, new Vector3(shape.Width * 0.34f, lightY, shape.Length * 0.5f + 0.005f), new Vector3(0.36f, 0.12f, 0.03f));
            Box(body.transform, "TaillightL", lightRear, new Vector3(-shape.Width * 0.34f, lightY, -shape.Length * 0.5f - 0.005f), new Vector3(0.36f, 0.1f, 0.03f));
            Box(body.transform, "TaillightR", lightRear, new Vector3(shape.Width * 0.34f, lightY, -shape.Length * 0.5f - 0.005f), new Vector3(0.36f, 0.1f, 0.03f));

            // Wheels: pivot at hub so the controller can spin/steer them.
            float half = shape.Wheelbase * 0.5f;
            Wheel(root.transform, VehicleVisualUtility.WheelFL, new Vector3(-shape.Track * 0.5f, shape.WheelRadius, half), shape, tire, rim);
            Wheel(root.transform, VehicleVisualUtility.WheelFR, new Vector3(shape.Track * 0.5f, shape.WheelRadius, half), shape, tire, rim);
            Wheel(root.transform, VehicleVisualUtility.WheelRL, new Vector3(-shape.Track * 0.5f, shape.WheelRadius, -half), shape, tire, rim);
            Wheel(root.transform, VehicleVisualUtility.WheelRR, new Vector3(shape.Track * 0.5f, shape.WheelRadius, -half), shape, tire, rim);

            Anchor(root.transform, VehicleVisualUtility.CockpitCameraAnchor, new Vector3(-0.35f, cabinY + 0.05f, shape.CabinOffset + 0.2f));
            Anchor(root.transform, VehicleVisualUtility.ExhaustAnchor, new Vector3(shape.Width * 0.3f, floorY + 0.08f, -shape.Length * 0.5f - 0.05f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject Box(Transform parent, string name, Material material, Vector3 center, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void Wheel(Transform parent, string name, Vector3 hub, Shape shape, Material tire, Material rim)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = hub;

            var tyre = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(tyre.GetComponent<Collider>());
            tyre.name = "Tyre";
            tyre.transform.SetParent(pivot.transform, false);
            tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tyre.transform.localScale = new Vector3(shape.WheelRadius * 2f, shape.WheelWidth * 0.5f, shape.WheelRadius * 2f);
            tyre.GetComponent<MeshRenderer>().sharedMaterial = tire;

            var wheelRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(wheelRim.GetComponent<Collider>());
            wheelRim.name = "Rim";
            wheelRim.transform.SetParent(pivot.transform, false);
            wheelRim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheelRim.transform.localScale = new Vector3(shape.WheelRadius * 1.3f, shape.WheelWidth * 0.52f, shape.WheelRadius * 1.3f);
            wheelRim.GetComponent<MeshRenderer>().sharedMaterial = rim;
        }

        private static void Anchor(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
        }
    }
}
