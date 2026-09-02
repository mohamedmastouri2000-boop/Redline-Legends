using System.Collections.Generic;
using RedlineLegends.Cameras;
using RedlineLegends.Core;
using RedlineLegends.DragRace;
using RedlineLegends.Race;
using RedlineLegends.Tracks;
using RedlineLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>Generates track scenes. The proving ground is a flat handling test area with obstacles and a ramp.</summary>
    public static class TrackSceneBuilder
    {
        public static string ProvingGroundPath => EditorPaths.Scenes + "/" + ContentGenerator.ProvingGroundSceneName + ".unity";
        private const string CheckerTexturePath = EditorPaths.Materials + "/Tex_Checker.asset";

        public static string HarborStripPath => EditorPaths.Scenes + "/" + ContentGenerator.DragSceneName + ".unity";
        private const string StripMeshFolder = EditorPaths.Root + "/Tracks/HarborStrip";

        public static void BuildAll()
        {
            BuildProvingGround();
            foreach (var spec in TrackSpecs.All) CircuitBuilder.Build(spec);
            BuildHarborStrip();
            Debug.Log("[Setup] Track scenes generated (" + (TrackSpecs.All.Length + 2) + ").");
        }

        /// <summary>Harbor Strip: a floodlit night drag strip with a half-mile run and 250 m of run-off.</summary>
        public static void BuildHarborStrip()
        {
            var scene = SceneBuilder.NewScene();
            var moon = SceneBuilder.CreateSun(new Vector3(35f, -60f, 0f), new Color(0.55f, 0.65f, 0.9f), 0.35f, true);
            var nightSky = EditorPaths.GetOrCreateMaterial(EditorPaths.Materials + "/Sky_Night.mat", Shader.Find("Skybox/Procedural"));
            nightSky.SetFloat("_SunSize", 0.02f);
            nightSky.SetFloat("_AtmosphereThickness", 0.4f);
            nightSky.SetColor("_SkyTint", new Color(0.05f, 0.07f, 0.15f));
            nightSky.SetColor("_GroundColor", new Color(0.03f, 0.03f, 0.05f));
            nightSky.SetFloat("_Exposure", 0.45f);
            EditorUtility.SetDirty(nightSky);
            SceneBuilder.ApplyLighting(moon);
            RenderSettings.skybox = nightSky;
            RenderSettings.ambientIntensity = 0.35f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.07f);
            RenderSettings.fogDensity = 0.0025f;
            SceneBuilder.CreateGlobalVolume();
            EditorPaths.EnsureFolder(StripMeshFolder);
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { StripMeshFolder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            Vector3[] control = { new Vector3(0f, 0f, -80f), new Vector3(0f, 0f, 300f), new Vector3(0f, 0f, 700f), new Vector3(0f, 0f, 1080f) };
            var halfWidths = new[] { 8f, 8f, 8f, 8f };
            var samples = TrackMeshBuilder.SampleSpline(control, halfWidths, false, 6f);

            var road = MaterialFactory.Opaque("Strip_Asphalt", new Color(0.15f, 0.15f, 0.16f), 0f, 0.5f);
            road.mainTexture = GetOrCreateCheckerTexture();
            EditorUtility.SetDirty(road);
            var kerb = MaterialFactory.Opaque("Strip_Edge", new Color(0.8f, 0.8f, 0.75f), 0f, 0.4f);
            var barrier = MaterialFactory.Opaque("Strip_Barrier", new Color(0.3f, 0.32f, 0.36f), 0.3f, 0.5f);
            var ground = MaterialFactory.Opaque("Strip_Ground", new Color(0.09f, 0.09f, 0.1f), 0f, 0.3f);
            var lamp = MaterialFactory.Emissive("Strip_Lamp", Color.white, new Color(4f, 3.8f, 3.2f));
            var post = MaterialFactory.Opaque("Strip_Post", new Color(0.25f, 0.25f, 0.27f), 0.6f, 0.5f);
            var crate = MaterialFactory.Opaque("Strip_Container", new Color(0.55f, 0.25f, 0.18f), 0.2f, 0.45f);

            var trackRoot = new GameObject("Track");
            TrackMeshBuilder.BuildRoad(trackRoot.transform, samples, false, 40, road, kerb, StripMeshFolder, "HarborStrip", GameLayers.Track);
            TrackMeshBuilder.BuildBarriers(trackRoot.transform, samples, false, 2.5f, 1.1f, barrier, GameLayers.Track, StripMeshFolder, "HarborStrip");
            Wall(trackRoot.transform, "EndWall", new Vector3(0f, 1.5f, 1085f), new Vector3(24f, 3f, 1f), barrier);
            Wall(trackRoot.transform, "BackWall", new Vector3(0f, 1.5f, -88f), new Vector3(24f, 3f, 1f), barrier);

            var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "Ground";
            groundPlane.transform.SetParent(trackRoot.transform, false);
            groundPlane.transform.position = new Vector3(0f, -0.05f, 500f);
            groundPlane.transform.localScale = new Vector3(60f, 1f, 140f);
            groundPlane.GetComponent<MeshRenderer>().sharedMaterial = ground;
            groundPlane.isStatic = true;
            groundPlane.layer = GameLayers.Track;

            // Floodlight posts every 60 m; four real spot lights near the start, emissive heads elsewhere.
            var lights = new GameObject("Floodlights");
            lights.transform.SetParent(trackRoot.transform, false);
            for (int i = 0; i < 18; i++)
            {
                float z = -60f + i * 60f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var pole = Wall(lights.transform, "Post" + i + (side < 0 ? "L" : "R"), new Vector3(side * 13f, 6f, z), new Vector3(0.35f, 12f, 0.35f), post);
                    var head = Wall(lights.transform, "Lamp" + i + (side < 0 ? "L" : "R"), new Vector3(side * 12.2f, 11.8f, z), new Vector3(1.6f, 0.4f, 0.8f), lamp);
                    Object.DestroyImmediate(head.GetComponent<Collider>());
                    if (i < 2)
                    {
                        var spot = new GameObject("Spot" + i + (side < 0 ? "L" : "R"), typeof(Light));
                        spot.transform.SetParent(lights.transform, false);
                        spot.transform.position = new Vector3(side * 12f, 11.5f, z);
                        spot.transform.rotation = Quaternion.Euler(70f, side < 0 ? 90f : -90f, 0f);
                        var l = spot.GetComponent<Light>();
                        l.type = LightType.Spot;
                        l.spotAngle = 110f;
                        l.range = 60f;
                        l.intensity = 900f;
                        l.color = new Color(1f, 0.95f, 0.85f);
                        l.shadows = LightShadows.None;
                    }
                }
            }
            // Harbour dressing: stacked containers along the sides.
            var rng = new System.Random(77);
            for (int i = 0; i < 40; i++)
            {
                float z = -40f + (float)rng.NextDouble() * 1100f;
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float x = side * (24f + (float)rng.NextDouble() * 30f);
                var c = Wall(trackRoot.transform, "Container" + i, new Vector3(x, 1.3f, z), new Vector3(2.4f, 2.6f, 12f), crate);
                c.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 20f - 10f, 0f);
            }

            // Layout: straight racing line, start at z=0, lanes 5 m apart.
            var linePoints = new Vector3[samples.Count];
            var lineWidths = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++) { linePoints[i] = samples[i].Position; lineWidths[i] = samples[i].HalfWidth; }
            var racingLine = RacingLine.Build(linePoints, lineWidths, false, 0.8f, 110f);
            var layoutGo = new GameObject("TrackLayout");
            var layout = layoutGo.AddComponent<TrackLayout>();
            var dragStart = new GameObject("DragStart");
            dragStart.transform.SetParent(layoutGo.transform, false);
            dragStart.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, 0f), Quaternion.identity);
            var startLine = Wall(trackRoot.transform, "StartLine", new Vector3(0f, 0.02f, 0f), new Vector3(16f, 0.02f, 0.4f), kerb);
            Object.DestroyImmediate(startLine.GetComponent<Collider>());
            var quarter = Wall(trackRoot.transform, "QuarterMile", new Vector3(0f, 0.02f, 402.336f), new Vector3(16f, 0.02f, 0.4f), kerb);
            Object.DestroyImmediate(quarter.GetComponent<Collider>());
            var half = Wall(trackRoot.transform, "HalfMile", new Vector3(0f, 0.02f, 804.672f), new Vector3(16f, 0.02f, 0.4f), kerb);
            Object.DestroyImmediate(half.GetComponent<Collider>());
            layout.EditorInitialize(ContentGenerator.DragTrackId, new Checkpoint[0], new Transform[0], racingLine, false, dragStart.transform, 5f);

            var camera = SceneBuilder.CreateCamera("Camera", new Vector3(0f, 3f, -12f), new Vector3(0f, 0.5f, 10f), 58f, Color.black, true);
            var rig = camera.gameObject.AddComponent<VehicleCameraRig>();

            var sessionGo = new GameObject("DragSession");
            var session = sessionGo.AddComponent<DragSession>();
            session.EditorWire(layout, rig);

            var ui = RaceUiBuilder.Build(session, rig);
            var dragPanel = RaceUiBuilder.BuildDragPanel(ui.Canvas.transform);
            var screen = ui.Canvas.gameObject.AddComponent<DragScreenController>();
            screen.EditorWire(session, ui.Hud, dragPanel, ui.Countdown, ui.PausePanel, ui.ResumeButton, ui.RestartButton, ui.QuitButton,
                ui.Results, ui.Controls.gameObject, ui.Tutorial);
            CreateSkidMarks();

            EditorSceneManager.SaveScene(scene, HarborStripPath);
        }

        public static void BuildProvingGround()
        {
            var scene = SceneBuilder.NewScene();
            var sun = SceneBuilder.CreateSun(new Vector3(52f, -28f, 0f), new Color(1f, 0.95f, 0.88f), 2.4f, true);
            SceneBuilder.ApplyLighting(sun);
            SceneBuilder.CreateGlobalVolume();

            var ground = MaterialFactory.Opaque("Ground_Asphalt", new Color(0.32f, 0.32f, 0.33f), 0f, 0.35f);
            ground.mainTexture = GetOrCreateCheckerTexture();
            ground.mainTextureScale = new Vector2(80f, 80f);
            EditorUtility.SetDirty(ground);
            var concrete = MaterialFactory.Opaque("Obstacle_Concrete", new Color(0.7f, 0.68f, 0.62f), 0f, 0.4f);
            var barrier = MaterialFactory.Opaque("Barrier_Red", new Color(0.8f, 0.15f, 0.12f), 0.1f, 0.5f);

            var environment = new GameObject("Environment");
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.SetParent(environment.transform, false);
            plane.transform.localScale = new Vector3(80f, 1f, 80f);
            plane.GetComponent<MeshRenderer>().sharedMaterial = ground;
            plane.isStatic = true;
            plane.layer = GameLayers.Track;

            // Boundary walls so the car cannot fall off the world.
            float half = 400f;
            Wall(environment.transform, "WallN", new Vector3(0f, 1.5f, half), new Vector3(2f * half, 3f, 1f), barrier);
            Wall(environment.transform, "WallS", new Vector3(0f, 1.5f, -half), new Vector3(2f * half, 3f, 1f), barrier);
            Wall(environment.transform, "WallE", new Vector3(half, 1.5f, 0f), new Vector3(1f, 3f, 2f * half), barrier);
            Wall(environment.transform, "WallW", new Vector3(-half, 1.5f, 0f), new Vector3(1f, 3f, 2f * half), barrier);

            // Slalom cones, a wall to hit and a ramp to test suspension.
            for (int i = 0; i < 8; i++)
                Wall(environment.transform, "Cone" + i, new Vector3(i % 2 == 0 ? -3f : 3f, 0.5f, 60f + i * 18f), new Vector3(0.6f, 1f, 0.6f), barrier);
            Wall(environment.transform, "TestWall", new Vector3(40f, 1f, 80f), new Vector3(1f, 2f, 20f), concrete);
            var ramp = Wall(environment.transform, "Ramp", new Vector3(-40f, 0f, 90f), new Vector3(8f, 0.5f, 24f), concrete);
            ramp.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            ramp.transform.position = new Vector3(-40f, 2.2f, 90f);
            var ring = Wall(environment.transform, "SkidPadMarker", new Vector3(120f, 0.02f, 0f), new Vector3(60f, 0.04f, 60f), concrete);
            ring.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Opaque("Ground_Paint", new Color(0.85f, 0.85f, 0.8f), 0f, 0.3f);

            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(0f, 0.6f, 0f);
            spawn.transform.rotation = Quaternion.identity;

            var camera = SceneBuilder.CreateCamera("Camera", new Vector3(0f, 2.5f, -7f), new Vector3(0f, 0.6f, 0f), 58f, Color.black, true);
            var rig = camera.gameObject.AddComponent<VehicleCameraRig>();

            var sessionGo = new GameObject("TestDriveSession");
            var session = sessionGo.AddComponent<TestDriveSession>();
            session.EditorWire(spawn.transform, rig);

            var ui = RaceUiBuilder.Build(session, rig);
            var hudController = ui.Canvas.gameObject.AddComponent<TestDriveHudController>();
            hudController.EditorWire(session, ui.Hud, ui.PauseButton);
            CreateSkidMarks();

            EditorSceneManager.SaveScene(scene, ProvingGroundPath);
        }

        /// <summary>Scene-wide skid mark mesh; VehicleEffects finds it once at spawn.</summary>
        public static void CreateSkidMarks()
        {
            var vfx = AssetDatabase.LoadAssetAtPath<VfxLibrary>(ContentGenerator.VfxLibraryPath);
            if (vfx == null) return;
            var go = new GameObject("SkidMarks", typeof(MeshFilter), typeof(MeshRenderer), typeof(RedlineLegends.VFX.SkidMarkRenderer));
            var skids = go.GetComponent<RedlineLegends.VFX.SkidMarkRenderer>();
            var initializer = go.AddComponent<RedlineLegends.VFX.SkidMarkBootstrap>();
            initializer.EditorWire(skids, vfx);
        }

        public static GameObject Wall(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.isStatic = true;
            go.layer = GameLayers.Track;
            return go;
        }

        public static Texture2D GetOrCreateCheckerTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(CheckerTexturePath);
            if (existing != null) return existing;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool dark = ((x / 32) + (y / 32)) % 2 == 0;
                byte v = dark ? (byte)150 : (byte)185;
                // subtle noise keeps the ground from looking flat-shaded
                int n = ((x * 31 + y * 17) % 13) - 6;
                byte c = (byte)Mathf.Clamp(v + n, 0, 255);
                pixels[y * size + x] = new Color32(c, c, c, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            AssetDatabase.CreateAsset(tex, CheckerTexturePath);
            return tex;
        }
    }
}
