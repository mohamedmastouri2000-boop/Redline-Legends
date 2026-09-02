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

            float y = -260f;
            var circuitBtn = BigButton(home, "CircuitButton", "CIRCUIT RACING", ref y);
            var dragBtn = BigButton(home, "DragButton", "DRAG RACING", ref y);
            var garageBtn = BigButton(home, "GarageButton", "GARAGE", ref y);

            var carText = UiKit.CreateText(home, "SelectedCar", "Selected car", 28f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)carText.transform, new Vector2(0f, 0f), new Vector2(80f, 40f), new Vector2(900f, 40f));

            var banner = UiKit.CreateText(home, "ResultsBanner", "", 30f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)banner.transform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1200f, 44f));

            // ---- Event panels
            var circuitPanel = BuildEventPanel(canvas.transform, "CircuitPanel");
            var dragPanel = BuildEventPanel(canvas.transform, "DragPanel");

            controller.EditorWire(home.gameObject, circuitPanel, dragPanel, circuitBtn, dragBtn, garageBtn,
                nameText, creditsText, levelText, xpFill, carText, banner);

            circuitPanel.gameObject.SetActive(false);
            dragPanel.gameObject.SetActive(false);
            Save(scene, MainMenuPath);
        }

        private static Button BigButton(Transform parent, string name, string label, ref float y)
        {
            var button = UiKit.CreateButton(parent, name, label, UiKit.ButtonNormal, 36f, out _);
            UiKit.Anchor((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(80f, y), new Vector2(560f, 110f));
            var edge = UiKit.CreatePanel(button.transform, "Edge", UiKit.Accent);
            edge.raycastTarget = false;
            UiKit.AnchorRange((RectTransform)edge.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 0f));
            y -= 130f;
            return button;
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

            controller.EditorWire(turntable.transform, name, cls, rating, stats, credits, status, prev, next, action, actionLabel, back,
                testDrive, upgradeContent, upgradeTemplate);

            Save(scene, GaragePath);
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
