using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Command-line builds. Android APK (development-friendly) and AAB (store) targets share the
    /// same scene list and player settings from ProjectSettingsSetup.
    ///   Unity.exe -batchmode -quit -buildTarget Android -executeMethod RedlineLegends.Editor.BuildScripts.BuildAndroidApk
    /// </summary>
    public static class BuildScripts
    {
        public const string OutputFolder = "Builds/Android";

        [MenuItem("Redline Legends/Build/Android APK (development)", priority = 50)]
        public static void BuildAndroidApkMenu() => BuildAndroid(false, true);

        [MenuItem("Redline Legends/Build/Android AAB (release)", priority = 51)]
        public static void BuildAndroidAabMenu() => BuildAndroid(true, false);

        /// <summary>Batch entry: exits non-zero on failure.</summary>
        public static void BuildAndroidApk()
        {
            var report = BuildAndroid(false, false);
            if (Application.isBatchMode) EditorApplication.Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void BuildAndroidAab()
        {
            var report = BuildAndroid(true, false);
            if (Application.isBatchMode) EditorApplication.Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildReport BuildAndroid(bool appBundle, bool development)
        {
            try
            {
                ProjectSettingsSetup.Apply();
                ProjectSettingsSetup.SetBuildScenes(SetupMenu.CollectScenePaths());
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("[Build] Android build support is not installed.");
                    return null;
                }
                EditorUserBuildSettings.buildAppBundle = appBundle;
                Directory.CreateDirectory(OutputFolder);
                string file = Path.Combine(OutputFolder, "RedlineLegends" + (appBundle ? ".aab" : ".apk"));

                var options = new BuildPlayerOptions
                {
                    scenes = SetupMenu.CollectScenePaths(),
                    locationPathName = file,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = development ? BuildOptions.Development : BuildOptions.None
                };
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log("[Build] " + summary.result + " -> " + summary.outputPath + " (" + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, "
                          + summary.totalTime.TotalMinutes.ToString("0.0") + " min, errors " + summary.totalErrors + ")");
                return report;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
