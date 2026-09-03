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
            var body = new GameObject("Body", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);

            // Lofted body hull (paint + glass submeshes) plus detail parts.
            var profile = CarMeshBuilder.ProfileFor(shape, cls);
            EditorPaths.EnsureFolder(EditorPaths.VehiclePrefabs + "/Meshes");
            var bodyMesh = CarMeshBuilder.BuildBody(profile, EditorPaths.VehiclePrefabs + "/Meshes/" + vehicleId + "_body.asset");
            body.GetComponent<MeshFilter>().sharedMesh = bodyMesh;
            body.GetComponent<MeshRenderer>().sharedMaterials = new[] { paint, glass };
            CarMeshBuilder.AddDetails(body.transform, profile, trim, lightFront, lightRear, glass, paint);

            // Wheels: pivot at hub so the controller can spin/steer them.
            float half = shape.Wheelbase * 0.5f;
            Wheel(root.transform, VehicleVisualUtility.WheelFL, new Vector3(-shape.Track * 0.5f, shape.WheelRadius, half), profile, tire, rim, trim);
            Wheel(root.transform, VehicleVisualUtility.WheelFR, new Vector3(shape.Track * 0.5f, shape.WheelRadius, half), profile, tire, rim, trim);
            Wheel(root.transform, VehicleVisualUtility.WheelRL, new Vector3(-shape.Track * 0.5f, shape.WheelRadius, -half), profile, tire, rim, trim);
            Wheel(root.transform, VehicleVisualUtility.WheelRR, new Vector3(shape.Track * 0.5f, shape.WheelRadius, -half), profile, tire, rim, trim);

            float cabinZ = (profile.CabinStart + profile.RoofFront) * 0.5f * profile.Length * 0.5f;
            Anchor(root.transform, VehicleVisualUtility.CockpitCameraAnchor, new Vector3(-0.35f, profile.RoofHeight - 0.18f, cabinZ));
            Anchor(root.transform, VehicleVisualUtility.ExhaustAnchor, new Vector3(shape.Width * 0.22f, profile.SillHeight + 0.1f, -shape.Length * 0.5f - 0.08f));

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

        private static void Wheel(Transform parent, string name, Vector3 hub, CarMeshBuilder.Profile profile, Material tire, Material rim, Material trim)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = hub;
            CarMeshBuilder.BuildWheel(pivot.transform, profile, tire, rim, trim);
        }

        private static void Anchor(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
        }
    }
}
