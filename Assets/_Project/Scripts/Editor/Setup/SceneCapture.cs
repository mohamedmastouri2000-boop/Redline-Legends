using System.IO;
using RedlineLegends.Tracks;
using RedlineLegends.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Headless visual review: opens each scene, drops a car in a sensible spot, renders the scene
    /// camera to a PNG. Output folder from REDLINE_CAPTURE_DIR. Batch entry point:
    ///   Unity.exe -batchmode -quit -executeMethod RedlineLegends.Editor.SceneCapture.CaptureAllBatch
    /// (needs graphics: do not pass -nographics).
    /// </summary>
    public static class SceneCapture
    {
        private const int Width = 1920, Height = 1080;

        public static void CaptureAllBatch()
        {
            try
            {
                CaptureAll();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Redline Legends/Capture/Render all scenes", priority = 60)]
        public static void CaptureAll()
        {
            string dir = System.Environment.GetEnvironmentVariable("REDLINE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Captures");
            Directory.CreateDirectory(dir);
            DumpTextures(dir);

            var vehicles = AssetDatabase.FindAssets("t:VehicleDefinition", new[] { EditorPaths.Content + "/Vehicles" });
            var cars = new System.Collections.Generic.List<VehicleDefinition>();
            foreach (var guid in vehicles) cars.Add(AssetDatabase.LoadAssetAtPath<VehicleDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
            cars.Sort((a, b) => a.VehicleClass != b.VehicleClass ? a.VehicleClass.CompareTo(b.VehicleClass) : string.CompareOrdinal(a.Id, b.Id));

            // Garage with one car per class.
            int shot = 0;
            foreach (var car in PickPerClass(cars))
            {
                var scene = EditorSceneManager.OpenScene(SceneBuilder.GaragePath, OpenSceneMode.Single);
                var turntable = GameObject.Find("Turntable");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(car.VisualPrefab);
                instance.transform.SetParent(turntable != null ? turntable.transform : null, false);
                VehicleVisualUtility.ApplyPaint(instance, car, 0);
                instance.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                Render(Camera.main, Path.Combine(dir, (shot++).ToString("00") + "_garage_" + car.Id + "_rear.png"));
                instance.transform.localRotation = Quaternion.Euler(0f, 215f, 0f);
                Render(Camera.main, Path.Combine(dir, (shot++).ToString("00") + "_garage_" + car.Id + "_front.png"));
            }

            // Main menu with its UI and the showcase car.
            {
                EditorSceneManager.OpenScene(SceneBuilder.MainMenuPath, OpenSceneMode.Single);
                var turntable = GameObject.Find("Turntable");
                if (turntable != null && cars.Count > 1)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(cars[1].VisualPrefab);
                    instance.transform.SetParent(turntable.transform, false);
                    instance.transform.localRotation = Quaternion.Euler(0f, 210f, 0f);
                    VehicleVisualUtility.ApplyPaint(instance, cars[1], 0);
                }
                RenderWithUi(Camera.main, Path.Combine(dir, (shot++).ToString("00") + "_ui_menu.png"));
            }
            // Garage with its UI.
            {
                EditorSceneManager.OpenScene(SceneBuilder.GaragePath, OpenSceneMode.Single);
                var turntable = GameObject.Find("Turntable");
                if (turntable != null && cars.Count > 0)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(cars[0].VisualPrefab);
                    instance.transform.SetParent(turntable.transform, false);
                    instance.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                    VehicleVisualUtility.ApplyPaint(instance, cars[0], 0);
                }
                RenderWithUi(Camera.main, Path.Combine(dir, (shot++).ToString("00") + "_ui_garage.png"));
            }

            // Every track from its start camera with a car on the grid.
            bool hudShot = false;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { EditorPaths.Scenes }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith("Track_")) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var layout = Object.FindAnyObjectByType<TrackLayout>();
                var camera = Camera.main;
                var building = GameObject.Find("Building0");
                if (building != null)
                {
                    var m = building.GetComponent<MeshRenderer>().sharedMaterial;
                    Debug.Log("[Capture] " + Path.GetFileName(path) + " Building0 material=" + m.name + " emissionKeyword=" + m.IsKeywordEnabled("_EMISSION")
                              + " emissionMap=" + (m.GetTexture("_EmissionMap") != null ? m.GetTexture("_EmissionMap").name : "null")
                              + " emissionColor=" + m.GetColor("_EmissionColor") + " ambient=" + RenderSettings.ambientMode + "/" + RenderSettings.ambientIntensity);
                }
                if (layout != null && camera != null)
                {
                    Transform anchor = layout.GridSlotCount > 0 ? layout.GetGridSlot(0) : layout.DragStart;
                    if (anchor != null)
                    {
                        var car = cars.Count > 0 ? cars[Mathf.Min(1, cars.Count - 1)] : null;
                        if (car != null)
                        {
                            var instance = (GameObject)PrefabUtility.InstantiatePrefab(car.VisualPrefab);
                            instance.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                            VehicleVisualUtility.ApplyPaint(instance, car, 1);
                        }
                        // Chase-camera framing: behind and above the grid car looking down the road.
                        camera.transform.position = anchor.position - anchor.forward * 7f + Vector3.up * 2.6f;
                        camera.transform.rotation = Quaternion.LookRotation(anchor.forward * 20f + Vector3.up * -1.2f, Vector3.up);
                    }
                }
                Render(camera, Path.Combine(dir, (shot++).ToString("00") + "_" + Path.GetFileNameWithoutExtension(path) + ".png"));
                if (!hudShot && layout != null && layout.GridSlotCount > 0)
                {
                    hudShot = true;
                    RenderWithUi(camera, Path.Combine(dir, (shot++).ToString("00") + "_ui_hud_" + Path.GetFileNameWithoutExtension(path) + ".png"));
                }
            }
            Debug.Log("[Capture] wrote " + shot + " images to " + dir);
        }

        private static System.Collections.Generic.IEnumerable<VehicleDefinition> PickPerClass(System.Collections.Generic.List<VehicleDefinition> cars)
        {
            var seen = new System.Collections.Generic.HashSet<VehicleClass>();
            foreach (var car in cars)
                if (seen.Add(car.VehicleClass)) yield return car;
        }

        /// <summary>Writes every generated Tex_* asset as PNG so the procedural textures can be reviewed.</summary>
        private static void DumpTextures(string dir)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D Tex_", new[] { EditorPaths.Materials + "/Textures" }))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                if (tex == null) continue;
                try
                {
                    File.WriteAllBytes(Path.Combine(dir, "tex_" + tex.name + ".png"), tex.EncodeToPNG());
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Capture] could not dump " + tex.name + ": " + e.Message);
                }
            }
        }

        /// <summary>
        /// Overlay canvases are not part of a camera render, so for the shot every canvas is switched
        /// to screen-space-camera mode on this camera (the scene is not saved afterwards).
        /// </summary>
        private static void RenderWithUi(Camera camera, string file)
        {
            if (camera == null) return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas.transform.parent != null && canvas.transform.parent.GetComponentInParent<Canvas>() != null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            Render(camera, file);
        }

        private static void Render(Camera camera, string file)
        {
            if (camera == null)
            {
                Debug.LogWarning("[Capture] no camera for " + file);
                return;
            }
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render(); // warm-up: the first frame after a scene load has stale shadows/probes
            camera.Render();
            camera.targetTexture = previous;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log("[Capture] " + file);
        }
    }
}
