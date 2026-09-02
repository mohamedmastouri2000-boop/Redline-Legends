using RedlineLegends.Core;
using RedlineLegends.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Generates the framework scenes (Bootstrap, MainMenu, Garage). Track scenes are built by
    /// TrackSceneBuilder once the vehicle and race systems exist. Every scene is regenerable so
    /// layout tweaks live in code, not in hand-edited YAML.
    /// </summary>
    public static class SceneBuilder
    {
        public const string BootstrapPath = EditorPaths.Scenes + "/" + SceneNames.Bootstrap + ".unity";
        public const string MainMenuPath = EditorPaths.Scenes + "/" + SceneNames.MainMenu + ".unity";
        public const string GaragePath = EditorPaths.Scenes + "/" + SceneNames.Garage + ".unity";
        public const string SkyMaterialPath = EditorPaths.Materials + "/Sky_Procedural.mat";
        public const string LightingSettingsPath = EditorPaths.Settings + "/Lighting_Default.lighting";

        public static string[] FrameworkScenePaths => new[] { BootstrapPath, MainMenuPath, GaragePath };

        public static void BuildAll()
        {
            EditorPaths.EnsureFolder(EditorPaths.Scenes);
            BuildBootstrap();
            BuildMainMenu();
            BuildGarage();
            Debug.Log("[Setup] Framework scenes generated.");
        }

        // ------------------------------------------------------------------ shared
        public static Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyLighting(sun: null);
            return scene;
        }

        public static Camera CreateCamera(string name, Vector3 position, Vector3 lookAt, float fov, Color background, bool postProcessing)
        {
            var go = new GameObject(name, typeof(Camera), typeof(AudioListener));
            var camera = go.GetComponent<Camera>();
            go.transform.position = position;
            go.transform.LookAt(lookAt);
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 1500f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = background;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = postProcessing;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.None;
            go.tag = "MainCamera";
            return camera;
        }

        public static Light CreateSun(Vector3 eulerAngles, Color color, float intensity, bool shadows)
        {
            var go = new GameObject("Sun", typeof(Light));
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.85f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.5f;
            go.transform.rotation = Quaternion.Euler(eulerAngles);
            RenderSettings.sun = light;
            return light;
        }

        public static Material GetOrCreateSky()
        {
            var shader = Shader.Find("Skybox/Procedural");
            var sky = EditorPaths.GetOrCreateMaterial(SkyMaterialPath, shader);
            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_SunSizeConvergence", 5f);
            sky.SetFloat("_AtmosphereThickness", 0.95f);
            sky.SetColor("_SkyTint", new Color(0.45f, 0.6f, 0.85f));
            sky.SetColor("_GroundColor", new Color(0.32f, 0.3f, 0.28f));
            sky.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        public static void ApplyLighting(Light sun)
        {
            RenderSettings.skybox = GetOrCreateSky();
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = false;
            if (sun != null) RenderSettings.sun = sun;

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { bakedGI = false, realtimeGI = false };
                EditorPaths.EnsureFolder(EditorPaths.Settings);
                AssetDatabase.CreateAsset(settings, LightingSettingsPath);
            }
            Lightmapping.lightingSettings = settings;
        }

        public static Volume CreateGlobalVolume()
        {
            var go = new GameObject("GlobalVolume", typeof(Volume));
            var volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RenderPipelineSetup.VolumeProfilePath);
            return volume;
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

        // ------------------------------------------------------------------ Bootstrap
        public static void BuildBootstrap()
        {
            var scene = NewScene();
            var camera = CreateCamera("Camera", new Vector3(0f, 0f, -10f), Vector3.zero, 60f, Color.black, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.03f, 0.04f);

            var canvas = UiKit.CreateCanvas("SplashCanvas", 0);
            var bg = UiKit.CreatePanel(canvas.transform, "Background", new Color(0.03f, 0.03f, 0.04f, 1f));
            UiKit.Stretch((RectTransform)bg.transform);
            var title = UiKit.CreateText(canvas.transform, "Title", "REDLINE LEGENDS", 72f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 100f));
            Save(scene, BootstrapPath);
        }

        // ------------------------------------------------------------------ Main menu
        public static void BuildMainMenu()
        {
            var scene = NewScene();
            var camera = CreateCamera("Camera", new Vector3(0f, 0f, -10f), Vector3.zero, 60f, Color.black, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);

            var canvas = UiKit.CreateCanvas("MenuCanvas", 0);
            var controller = canvas.gameObject.AddComponent<MainMenuController>();
            canvas.gameObject.AddComponent<UiClickSound>();

            var bg = UiKit.CreatePanel(canvas.transform, "Background", new Color(0.06f, 0.06f, 0.08f, 1f));
            UiKit.Stretch((RectTransform)bg.transform);
            var stripe = UiKit.CreatePanel(canvas.transform, "AccentStripe", UiKit.Accent);
            UiKit.AnchorRange((RectTransform)stripe.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(0f, 0f));

            // ---- Home
            var home = UiKit.CreateRect(canvas.transform, "Home");
            UiKit.Stretch(home);

            var title = UiKit.CreateText(home, "Title", "REDLINE LEGENDS", 64f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(80f, -50f), new Vector2(1000f, 80f));

            var profilePanel = UiKit.CreatePanel(home, "ProfileBar", UiKit.PanelMid);
            UiKit.Anchor((RectTransform)profilePanel.transform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(520f, 110f));
            var nameText = UiKit.CreateText(profilePanel.transform, "Name", "Racer", 30f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)nameText.transform, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(300f, 36f));
            var levelText = UiKit.CreateText(profilePanel.transform, "Level", "LVL 1", 26f, UiKit.Accent, TextAlignmentOptions.Right, FontStyles.Bold);
            UiKit.Anchor((RectTransform)levelText.transform, new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(180f, 36f));
            var creditsText = UiKit.CreateText(profilePanel.transform, "Credits", "0 CR", 26f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)creditsText.transform, new Vector2(0f, 1f), new Vector2(16f, -50f), new Vector2(300f, 32f));
            var xpBar = UiKit.CreateFillBar(profilePanel.transform, "XpBar", new Color(0.1f, 0.1f, 0.12f, 1f), UiKit.Accent, out var xpFill);
            UiKit.AnchorRange((RectTransform)xpBar.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 10f), new Vector2(-16f, 18f));

            float y = -220f;
            var circuitBtn = BigButton(home, "CircuitButton", "CIRCUIT RACING", ref y);
            var dragBtn = BigButton(home, "DragButton", "DRAG RACING", ref y);
            var garageBtn = BigButton(home, "GarageButton", "GARAGE", ref y);
            var achievementsBtn = BigButton(home, "AchievementsButton", "ACHIEVEMENTS", ref y);
            var settingsBtn = BigButton(home, "SettingsButton", "SETTINGS", ref y);

            var carText = UiKit.CreateText(home, "SelectedCar", "Selected car", 28f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)carText.transform, new Vector2(0f, 0f), new Vector2(80f, 40f), new Vector2(900f, 40f));

            var banner = UiKit.CreateText(home, "ResultsBanner", "", 30f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)banner.transform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1200f, 44f));

            // ---- Event panels
            var circuitPanel = BuildEventPanel(canvas.transform, "CircuitPanel");
            var dragPanel = BuildEventPanel(canvas.transform, "DragPanel");
            var settingsPanel = BuildSettingsPanel(canvas.transform);
            var achievementsPanel = BuildAchievementsPanel(canvas.transform);

            controller.EditorWire(home.gameObject, circuitPanel, dragPanel, circuitBtn, dragBtn, garageBtn,
                nameText, creditsText, levelText, xpFill, carText, banner, settingsPanel, settingsBtn, achievementsPanel, achievementsBtn);

            circuitPanel.gameObject.SetActive(false);
            dragPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            achievementsPanel.gameObject.SetActive(false);
            Save(scene, MainMenuPath);
        }

        private static Button BigButton(Transform parent, string name, string label, ref float y)
        {
            var button = UiKit.CreateButton(parent, name, label, UiKit.ButtonNormal, 32f, out _);
            UiKit.Anchor((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(80f, y), new Vector2(560f, 92f));
            var edge = UiKit.CreatePanel(button.transform, "Edge", UiKit.Accent);
            edge.raycastTarget = false;
            UiKit.AnchorRange((RectTransform)edge.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 0f));
            y -= 108f;
            return button;
        }

        // ------------------------------------------------------------------ shared widgets
        public static CycleRow BuildCycleRow(Transform parent, string name, float y, float width = 760f)
        {
            var bg = UiKit.CreatePanel(parent, name, UiKit.PanelMid);
            UiKit.Anchor((RectTransform)bg.transform, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, 64f));
            var row = bg.gameObject.AddComponent<CycleRow>();
            var label = UiKit.CreateText(bg.transform, "Label", name, 26f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)label.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(16f, 0f), Vector2.zero);
            var prev = UiKit.CreateButton(bg.transform, "Prev", "<", UiKit.ButtonNormal, 28f, out _);
            UiKit.AnchorRange((RectTransform)prev.transform, new Vector2(0.52f, 0.1f), new Vector2(0.62f, 0.9f), Vector2.zero, Vector2.zero);
            var value = UiKit.CreateText(bg.transform, "Value", "", 26f, UiKit.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)value.transform, new Vector2(0.62f, 0f), new Vector2(0.88f, 1f), Vector2.zero, Vector2.zero);
            var next = UiKit.CreateButton(bg.transform, "Next", ">", UiKit.ButtonNormal, 28f, out _);
            UiKit.AnchorRange((RectTransform)next.transform, new Vector2(0.88f, 0.1f), new Vector2(0.98f, 0.9f), Vector2.zero, Vector2.zero);
            row.EditorWire(label, value, prev, next);
            return row;
        }

        public static SliderRow BuildSliderRow(Transform parent, string name, float y, float width = 760f)
        {
            var bg = UiKit.CreatePanel(parent, name, UiKit.PanelMid);
            UiKit.Anchor((RectTransform)bg.transform, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, 64f));
            var row = bg.gameObject.AddComponent<SliderRow>();
            var label = UiKit.CreateText(bg.transform, "Label", name, 24f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)label.transform, new Vector2(0f, 0f), new Vector2(0.42f, 1f), new Vector2(16f, 0f), Vector2.zero);
            var slider = UiKit.CreateSlider(bg.transform, "Slider", out _);
            UiKit.AnchorRange((RectTransform)slider.transform, new Vector2(0.44f, 0.3f), new Vector2(0.86f, 0.7f), Vector2.zero, Vector2.zero);
            var value = UiKit.CreateText(bg.transform, "Value", "", 22f, UiKit.Accent, TextAlignmentOptions.Right);
            UiKit.AnchorRange((RectTransform)value.transform, new Vector2(0.87f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-12f, 0f));
            row.EditorWire(label, value, slider);
            return row;
        }

        private static SettingsPanel BuildSettingsPanel(Transform parent)
        {
            var root = UiKit.CreatePanel(parent, "SettingsPanel", new Color(0.06f, 0.06f, 0.08f, 1f));
            UiKit.Stretch((RectTransform)root.transform);
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            var title = UiKit.CreateText(root.transform, "Title", "SETTINGS", 52f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(80f, -40f), new Vector2(800f, 70f));
            var back = UiKit.CreateButton(root.transform, "BackButton", "BACK", UiKit.ButtonNormal, 30f, out _);
            UiKit.Anchor((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(220f, 70f));

            UiKit.CreateScrollList(root.transform, "List", out var content);
            UiKit.AnchorRange((RectTransform)content.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(80f, 40f), new Vector2(-60f, -130f));
            // Two columns inside the scroll list: left = controls/camera, right = graphics/audio.
            var left = UiKit.CreateRect(content, "Left");
            var right = UiKit.CreateRect(content, "Right");
            var grid = content.gameObject.GetComponent<VerticalLayoutGroup>();
            Object.DestroyImmediate(grid);
            var fitter = content.gameObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            content.sizeDelta = new Vector2(0f, 760f);
            UiKit.AnchorRange(left, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(-10f, 0f));
            UiKit.AnchorRange(right, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(0f, 0f));

            float y = -8f;
            var style = BuildCycleRow(left, "Control style", y, 860f); y -= 72f;
            var steer = BuildSliderRow(left, "Steering sensitivity", y, 860f); y -= 72f;
            var tilt = BuildSliderRow(left, "Tilt sensitivity", y, 860f); y -= 72f;
            var gearbox = BuildCycleRow(left, "Gearbox", y, 860f); y -= 72f;
            var cam = BuildCycleRow(left, "Camera", y, 860f); y -= 72f;
            var shake = BuildSliderRow(left, "Camera shake", y, 860f); y -= 72f;
            var vib = BuildCycleRow(left, "Vibration", y, 860f); y -= 72f;
            var tut = BuildCycleRow(left, "Tutorials", y, 860f);
            foreach (RectTransform child in left) { var a = child.anchorMin; child.anchorMin = new Vector2(0f, 1f); child.anchorMax = new Vector2(0f, 1f); child.pivot = new Vector2(0f, 1f); }

            y = -8f;
            var gfx = BuildCycleRow(right, "Graphics", y, 860f); y -= 72f;
            var fps = BuildCycleRow(right, "Frame rate", y, 860f); y -= 72f;
            var unit = BuildCycleRow(right, "Units", y, 860f); y -= 72f;
            var master = BuildSliderRow(right, "Master volume", y, 860f); y -= 72f;
            var music = BuildSliderRow(right, "Music volume", y, 860f); y -= 72f;
            var sfx = BuildSliderRow(right, "Effects volume", y, 860f);
            foreach (RectTransform child in right) { child.anchorMin = new Vector2(0f, 1f); child.anchorMax = new Vector2(0f, 1f); child.pivot = new Vector2(0f, 1f); }

            panel.EditorWire(style, gearbox, cam, gfx, fps, unit, vib, tut, steer, tilt, shake, master, music, sfx, back);
            return panel;
        }

        private static AchievementsPanel BuildAchievementsPanel(Transform parent)
        {
            var root = UiKit.CreatePanel(parent, "AchievementsPanel", new Color(0.06f, 0.06f, 0.08f, 1f));
            UiKit.Stretch((RectTransform)root.transform);
            var panel = root.gameObject.AddComponent<AchievementsPanel>();
            var title = UiKit.CreateText(root.transform, "Title", "ACHIEVEMENTS", 52f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(80f, -40f), new Vector2(1000f, 70f));
            var back = UiKit.CreateButton(root.transform, "BackButton", "BACK", UiKit.ButtonNormal, 30f, out _);
            UiKit.Anchor((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(220f, 70f));
            UiKit.CreateScrollList(root.transform, "List", out var content);
            UiKit.AnchorRange((RectTransform)content.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(80f, 50f), new Vector2(-60f, -140f));

            var bg = UiKit.CreatePanel(content, "RowTemplate", UiKit.PanelMid);
            UiKit.SetPreferredHeight(bg, 84f);
            var row = bg.gameObject.AddComponent<AchievementRow>();
            var name = UiKit.CreateText(bg.transform, "Name", "Name", 30f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)name.transform, new Vector2(0f, 0.5f), new Vector2(0.75f, 1f), new Vector2(24f, -4f), new Vector2(0f, -8f));
            var desc = UiKit.CreateText(bg.transform, "Description", "", 22f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.AnchorRange((RectTransform)desc.transform, new Vector2(0f, 0f), new Vector2(0.75f, 0.5f), new Vector2(24f, 8f), new Vector2(0f, 2f));
            var progress = UiKit.CreateText(bg.transform, "Progress", "0/1", 30f, UiKit.Accent, TextAlignmentOptions.Right, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)progress.transform, new Vector2(0.75f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-24f, 0f));
            row.EditorWire(name, desc, progress, bg);

            panel.EditorWire(title, back, content, row);
            return panel;
        }

        private static EventListPanel BuildEventPanel(Transform parent, string name)
        {
            var root = UiKit.CreatePanel(parent, name, new Color(0.06f, 0.06f, 0.08f, 1f));
            UiKit.Stretch((RectTransform)root.transform);
            var panel = root.gameObject.AddComponent<EventListPanel>();

            var title = UiKit.CreateText(root.transform, "Title", "EVENTS", 52f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(80f, -40f), new Vector2(1000f, 70f));

            var back = UiKit.CreateButton(root.transform, "BackButton", "BACK", UiKit.ButtonNormal, 30f, out _);
            UiKit.Anchor((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(220f, 70f));

            UiKit.CreateScrollList(root.transform, "List", out var content);
            UiKit.AnchorRange((RectTransform)content.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(80f, 50f), new Vector2(-60f, -140f));

            var template = BuildEventRow(content);
            panel.EditorWire(title, back, content, template);
            return panel;
        }

        private static EventRow BuildEventRow(Transform content)
        {
            var bg = UiKit.CreatePanel(content, "RowTemplate", UiKit.PanelMid);
            UiKit.SetPreferredHeight(bg, 104f);
            var row = bg.gameObject.AddComponent<EventRow>();

            var title = UiKit.CreateText(bg.transform, "Title", "Event", 32f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)title.transform, new Vector2(0f, 0.5f), new Vector2(0.7f, 1f), new Vector2(24f, -4f), new Vector2(0f, -10f));
            var detail = UiKit.CreateText(bg.transform, "Detail", "Details", 22f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.AnchorRange((RectTransform)detail.transform, new Vector2(0f, 0f), new Vector2(0.7f, 0.5f), new Vector2(24f, 10f), new Vector2(0f, 4f));
            var stars = UiKit.CreateText(bg.transform, "Stars", "---", 30f, new Color(1f, 0.8f, 0.2f), TextAlignmentOptions.Center);
            UiKit.AnchorRange((RectTransform)stars.transform, new Vector2(0.7f, 0f), new Vector2(0.82f, 1f), Vector2.zero, Vector2.zero);
            var launch = UiKit.CreateButton(bg.transform, "Launch", "RACE", UiKit.Accent, 28f, out _);
            UiKit.AnchorRange((RectTransform)launch.transform, new Vector2(0.84f, 0.15f), new Vector2(0.985f, 0.85f), Vector2.zero, Vector2.zero);

            row.EditorWire(title, detail, stars, launch, bg);
            return row;
        }

        // ------------------------------------------------------------------ Garage
        public static void BuildGarage()
        {
            var scene = NewScene();
            var sun = CreateSun(new Vector3(48f, -35f, 0f), new Color(1f, 0.96f, 0.9f), 2.2f, true);
            ApplyLighting(sun);
            CreateGlobalVolume();

            var camera = CreateCamera("Camera", new Vector3(0f, 1.45f, -6.2f), new Vector3(0f, 0.55f, 0f), 42f, Color.black, true);

            var floorMat = MaterialFactory.Opaque("Garage_Floor", new Color(0.16f, 0.16f, 0.17f), 0.05f, 0.55f);
            var wallMat = MaterialFactory.Opaque("Garage_Wall", new Color(0.09f, 0.09f, 0.1f), 0f, 0.3f);
            var discMat = MaterialFactory.Opaque("Garage_Turntable", new Color(0.22f, 0.22f, 0.24f), 0.4f, 0.75f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(6f, 1f, 6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.position = new Vector3(0f, 4f, 14f);
            backWall.transform.localScale = new Vector3(60f, 8f, 0.5f);
            backWall.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            backWall.isStatic = true;

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "TurntableDisc";
            disc.transform.position = new Vector3(0f, 0.02f, 0f);
            disc.transform.localScale = new Vector3(6.5f, 0.02f, 6.5f);
            disc.GetComponent<MeshRenderer>().sharedMaterial = discMat;

            var turntable = new GameObject("Turntable");
            turntable.transform.position = new Vector3(0f, 0.04f, 0f);

            var fill = new GameObject("FillLight", typeof(Light));
            var fillLight = fill.GetComponent<Light>();
            fillLight.type = LightType.Spot;
            fillLight.color = new Color(0.85f, 0.9f, 1f);
            fillLight.intensity = 60f;
            fillLight.range = 18f;
            fillLight.spotAngle = 80f;
            fillLight.shadows = LightShadows.None;
            fill.transform.position = new Vector3(-5f, 4.5f, -4f);
            fill.transform.LookAt(new Vector3(0f, 0.6f, 0f));

            var probeGo = new GameObject("ReflectionProbe", typeof(ReflectionProbe));
            var probe = probeGo.GetComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128;
            probe.size = new Vector3(30f, 12f, 30f);
            probeGo.transform.position = new Vector3(0f, 1.2f, 0f);

            // ---- UI
            var canvas = UiKit.CreateCanvas("GarageCanvas", 0);
            var dragArea = UiKit.CreatePanel(canvas.transform, "DragArea", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch((RectTransform)dragArea.transform);
            var controller = dragArea.gameObject.AddComponent<GarageSceneController>();

            var info = UiKit.CreatePanel(canvas.transform, "InfoPanel", UiKit.PanelDark);
            UiKit.AnchorRange((RectTransform)info.transform, new Vector2(0f, 0.28f), new Vector2(0f, 1f), new Vector2(40f, 0f), new Vector2(520f, -40f));
            var name = UiKit.CreateText(info.transform, "Name", "Car", 40f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)name.transform, new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(440f, 50f));
            var cls = UiKit.CreateText(info.transform, "Class", "STREET CLASS", 22f, UiKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)cls.transform, new Vector2(0f, 1f), new Vector2(20f, -66f), new Vector2(440f, 30f));
            var rating = UiKit.CreateText(info.transform, "Rating", "PR 000", 54f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)rating.transform, new Vector2(0f, 1f), new Vector2(20f, -104f), new Vector2(440f, 64f));
            var stats = UiKit.CreateText(info.transform, "Stats", "", 24f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)stats.transform, new Vector2(0f, 1f), new Vector2(20f, -176f), new Vector2(440f, 140f));
            var status = UiKit.CreateText(info.transform, "Status", "", 22f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)status.transform, new Vector2(0f, 0f), new Vector2(20f, 16f), new Vector2(440f, 60f));

            var credits = UiKit.CreateText(canvas.transform, "Credits", "0 CR", 30f, UiKit.TextMain, TextAlignmentOptions.Right, FontStyles.Bold);
            UiKit.Anchor((RectTransform)credits.transform, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 40f));

            var back = UiKit.CreateButton(canvas.transform, "BackButton", "BACK", UiKit.ButtonNormal, 28f, out _);
            UiKit.Anchor((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(-40f, -90f), new Vector2(200f, 64f));
            var testDrive = UiKit.CreateButton(canvas.transform, "TestDriveButton", "TEST DRIVE", UiKit.ButtonNormal, 26f, out _);
            UiKit.Anchor((RectTransform)testDrive.transform, new Vector2(1f, 1f), new Vector2(-40f, -164f), new Vector2(200f, 64f));
            var tune = UiKit.CreateButton(canvas.transform, "TuneButton", "TUNE", UiKit.ButtonNormal, 26f, out _);
            UiKit.Anchor((RectTransform)tune.transform, new Vector2(1f, 1f), new Vector2(-40f, -238f), new Vector2(200f, 64f));

            // Paint selector under the info panel.
            var paintPrev = UiKit.CreateButton(canvas.transform, "PaintPrev", "<", UiKit.ButtonNormal, 26f, out _);
            UiKit.Anchor((RectTransform)paintPrev.transform, new Vector2(0f, 0f), new Vector2(40f, 200f), new Vector2(64f, 56f));
            var paintLabel = UiKit.CreateText(canvas.transform, "PaintName", "Paint", 24f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)paintLabel.transform, new Vector2(0f, 0f), new Vector2(110f, 200f), new Vector2(340f, 56f));
            var paintNext = UiKit.CreateButton(canvas.transform, "PaintNext", ">", UiKit.ButtonNormal, 26f, out _);
            UiKit.Anchor((RectTransform)paintNext.transform, new Vector2(0f, 0f), new Vector2(456f, 200f), new Vector2(64f, 56f));

            var tuningPanel = BuildTuningPanel(canvas.transform);

            var prev = UiKit.CreateButton(canvas.transform, "PrevButton", "<", UiKit.ButtonNormal, 40f, out _);
            UiKit.Anchor((RectTransform)prev.transform, new Vector2(0.5f, 0f), new Vector2(-330f, 60f), new Vector2(110f, 90f));
            var action = UiKit.CreateButton(canvas.transform, "ActionButton", "SELECT", UiKit.Accent, 32f, out var actionLabel);
            UiKit.Anchor((RectTransform)action.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(520f, 90f));
            var next = UiKit.CreateButton(canvas.transform, "NextButton", ">", UiKit.ButtonNormal, 40f, out _);
            UiKit.Anchor((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(330f, 60f), new Vector2(110f, 90f));

            var upgradesPanel = UiKit.CreatePanel(canvas.transform, "UpgradesPanel", UiKit.PanelDark);
            UiKit.AnchorRange((RectTransform)upgradesPanel.transform, new Vector2(1f, 0.2f), new Vector2(1f, 0.82f), new Vector2(-520f, 0f), new Vector2(-40f, 0f));
            var upgTitle = UiKit.CreateText(upgradesPanel.transform, "Title", "UPGRADES", 28f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)upgTitle.transform, new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(300f, 36f));
            UiKit.CreateScrollList(upgradesPanel.transform, "List", out var upgradeContent);
            UiKit.AnchorRange((RectTransform)upgradeContent.parent, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -56f));
            var upgradeTemplate = BuildUpgradeRow(upgradeContent);

            var garageTutorial = UiKit.CreateTutorialOverlay(canvas.transform);
            canvas.gameObject.AddComponent<UiClickSound>();
            controller.EditorWire(turntable.transform, name, cls, rating, stats, credits, status, prev, next, action, actionLabel, back,
                testDrive, upgradeContent, upgradeTemplate, tune, tuningPanel, paintPrev, paintNext, paintLabel, garageTutorial);
            tuningPanel.gameObject.SetActive(false);

            Save(scene, GaragePath);
        }

        private static TuningPanel BuildTuningPanel(Transform parent)
        {
            var root = UiKit.CreatePanel(parent, "TuningPanel", new Color(0.04f, 0.04f, 0.06f, 0.96f));
            UiKit.Stretch((RectTransform)root.transform);
            var panel = root.gameObject.AddComponent<TuningPanel>();
            var title = UiKit.CreateText(root.transform, "Title", "TUNING", 48f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(80f, -36f), new Vector2(900f, 64f));
            var rating = UiKit.CreateText(root.transform, "Rating", "", 28f, UiKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)rating.transform, new Vector2(0f, 1f), new Vector2(80f, -100f), new Vector2(1000f, 40f));
            var close = UiKit.CreateButton(root.transform, "Close", "CLOSE", UiKit.ButtonNormal, 28f, out _);
            UiKit.Anchor((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-60f, -36f), new Vector2(200f, 64f));

            var left = UiKit.CreateRect(root.transform, "Left");
            UiKit.AnchorRange(left, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(80f, 120f), new Vector2(-20f, -150f));
            var right = UiKit.CreateRect(root.transform, "Right");
            UiKit.AnchorRange(right, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(20f, 120f), new Vector2(-60f, -150f));

            float y = 0f;
            var fd = BuildSliderRow(left, "Final drive", y, 860f); y -= 72f;
            var susp = BuildSliderRow(left, "Suspension", y, 860f); y -= 72f;
            var ride = BuildSliderRow(left, "Ride height", y, 860f); y -= 72f;
            var grip = BuildSliderRow(left, "Grip bias", y, 860f); y -= 72f;
            var nos = BuildSliderRow(left, "Nitrous", y, 860f);
            foreach (RectTransform child in left) { child.anchorMin = new Vector2(0f, 1f); child.anchorMax = new Vector2(0f, 1f); child.pivot = new Vector2(0f, 1f); }

            y = 0f;
            var gears = new SliderRow[6];
            for (int i = 0; i < gears.Length; i++)
            {
                gears[i] = BuildSliderRow(right, "Gear " + (i + 1), y, 860f);
                y -= 72f;
            }
            foreach (RectTransform child in right) { child.anchorMin = new Vector2(0f, 1f); child.anchorMax = new Vector2(0f, 1f); child.pivot = new Vector2(0f, 1f); }
            var locked = UiKit.CreateText(right, "Locked", "", 26f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)locked.transform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(860f, 64f));

            var apply = UiKit.CreateButton(root.transform, "Apply", "APPLY", UiKit.Accent, 30f, out _);
            UiKit.Anchor((RectTransform)apply.transform, new Vector2(0.5f, 0f), new Vector2(180f, 36f), new Vector2(320f, 72f));
            var reset = UiKit.CreateButton(root.transform, "Reset", "RESET", UiKit.ButtonNormal, 30f, out _);
            UiKit.Anchor((RectTransform)reset.transform, new Vector2(0.5f, 0f), new Vector2(-180f, 36f), new Vector2(320f, 72f));

            panel.EditorWire(title, rating, locked, fd, susp, ride, grip, nos, gears, apply, reset, close);
            return panel;
        }

        private static UpgradeRow BuildUpgradeRow(Transform content)
        {
            var bg = UiKit.CreatePanel(content, "UpgradeTemplate", UiKit.PanelMid);
            UiKit.SetPreferredHeight(bg, 60f);
            var row = bg.gameObject.AddComponent<UpgradeRow>();
            var name = UiKit.CreateText(bg.transform, "Name", "Engine", 22f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)name.transform, new Vector2(0f, 0f), new Vector2(0.45f, 1f), new Vector2(12f, 0f), Vector2.zero);
            var stage = UiKit.CreateText(bg.transform, "Stage", "...", 24f, UiKit.Accent, TextAlignmentOptions.Center);
            UiKit.AnchorRange((RectTransform)stage.transform, new Vector2(0.45f, 0f), new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero);
            var buy = UiKit.CreateButton(bg.transform, "Buy", "0 CR", UiKit.AccentDim, 20f, out var buyLabel);
            UiKit.AnchorRange((RectTransform)buy.transform, new Vector2(0.66f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            row.EditorWire(name, stage, buy, buyLabel);
            return row;
        }
    }
}
