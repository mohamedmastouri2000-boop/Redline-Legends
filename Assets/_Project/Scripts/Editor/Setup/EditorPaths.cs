using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>All generated asset locations in one place, plus idempotent create-or-load helpers.</summary>
    public static class EditorPaths
    {
        public const string Root = "Assets/_Project";
        public const string Settings = Root + "/Settings";
        public const string Content = Root + "/Content";
        public const string Prefabs = Root + "/Prefabs";
        public const string VehiclePrefabs = Prefabs + "/Vehicles";
        public const string Scenes = Root + "/Scenes";
        public const string Materials = Root + "/Materials";
        public const string Resources = Root + "/Resources";
        public const string InputActions = Root + "/Input/RedlineControls.inputactions";

        public static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// Loads the asset if it exists (keeping its GUID so scene/prefab references survive
        /// regeneration), otherwise creates it.
        /// </summary>
        public static T GetOrCreateAsset<T>(string path, out bool created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                created = false;
                return existing;
            }
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            created = true;
            return instance;
        }

        public static T GetOrCreateAsset<T>(string path) where T : ScriptableObject => GetOrCreateAsset<T>(path, out _);

        public static Material GetOrCreateMaterial(string path, Shader shader)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.shader != shader) existing.shader = shader;
                return existing;
            }
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
