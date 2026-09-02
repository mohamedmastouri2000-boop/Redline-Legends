using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// One-click (or batch-mode) project generation. Run after a fresh clone:
    /// Redline Legends > Setup > Generate Project, or
    /// Unity.exe -batchmode -executeMethod RedlineLegends.Editor.SetupMenu.GenerateAllBatch
    /// </summary>
    public static class SetupMenu
    {
        private const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        private const string TmpEssentialsMarker = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Redline Legends/Setup/Generate Project (all steps)", priority = 0)]
        public static void GenerateAll()
        {
            ImportTmpEssentials();
            RenderPipelineSetup.Generate();
            ProjectSettingsSetup.Apply();
            ContentGenerator.Generate();
            AppRootPrefabBuilder.Build();
            SceneBuilder.BuildAll();
            ProjectSettingsSetup.SetBuildScenes(CollectScenePaths());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Setup] Project generation complete.");
        }

        /// <summary>Batch entry point: exits non-zero on any exception so CI/CLI runs fail loudly.</summary>
        public static void GenerateAllBatch()
        {
            try
            {
                GenerateAll();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [MenuItem("Redline Legends/Setup/1. Import TextMeshPro Essentials", priority = 10)]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(TmpEssentialsMarker) != null) return;
            AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Setup] TextMeshPro essential resources imported.");
        }

        [MenuItem("Redline Legends/Setup/2. Render Pipeline (URP tiers)", priority = 11)]
        public static void MenuRenderPipeline() => RenderPipelineSetup.Generate();

        [MenuItem("Redline Legends/Setup/3. Project Settings (Android)", priority = 12)]
        public static void MenuProjectSettings() => ProjectSettingsSetup.Apply();

        [MenuItem("Redline Legends/Setup/4. Content Assets", priority = 13)]
        public static void MenuContent() => ContentGenerator.Generate();

        [MenuItem("Redline Legends/Setup/5. AppRoot Prefab", priority = 14)]
        public static void MenuAppRoot() => AppRootPrefabBuilder.Build();

        [MenuItem("Redline Legends/Setup/6. Framework Scenes", priority = 15)]
        public static void MenuScenes()
        {
            SceneBuilder.BuildAll();
            ProjectSettingsSetup.SetBuildScenes(CollectScenePaths());
        }

        [MenuItem("Redline Legends/Setup/Switch Build Target to Android", priority = 30)]
        public static void MenuSwitchAndroid() => ProjectSettingsSetup.SwitchToAndroid();

        /// <summary>Framework scenes first (Bootstrap must be index 0), then every generated track scene.</summary>
        public static string[] CollectScenePaths()
        {
            var paths = new List<string>(SceneBuilder.FrameworkScenePaths);
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { EditorPaths.Scenes }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!paths.Contains(path) && System.IO.Path.GetFileName(path).StartsWith("Track_"))
                    paths.Add(path);
            }
            return paths.ToArray();
        }
    }
}
