using UnityEngine;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Physics layers the game relies on. The editor generator writes them into the TagManager;
    /// runtime code resolves them once here so no magic numbers spread through the codebase.
    /// </summary>
    public static class GameLayers
    {
        public const string VehicleName = "Vehicle";
        public const string TrackName = "Track";
        public const string CheckpointName = "Checkpoint";
        public const int VehicleIndex = 8;
        public const int TrackIndex = 9;
        public const int CheckpointIndex = 10;

        public static int Vehicle => VehicleIndex;
        public static int Track => TrackIndex;
        public static int Checkpoint => CheckpointIndex;

        /// <summary>What wheels can drive on: everything except vehicles and triggers.</summary>
        public static int GroundMask => ~((1 << VehicleIndex) | (1 << CheckpointIndex) | (1 << 2));
        public static int VehicleMask => 1 << VehicleIndex;

        public static void SetLayerRecursive(GameObject root, int layer)
        {
            root.layer = layer;
            var t = root.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i).gameObject, layer);
        }
    }
}
