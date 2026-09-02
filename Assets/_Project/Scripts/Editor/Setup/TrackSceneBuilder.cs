using System.Collections.Generic;
using RedlineLegends.Cameras;
using RedlineLegends.Core;
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

        public static string SunsetLoopPath => EditorPaths.Scenes + "/" + ContentGenerator.CircuitSceneName + ".unity";
        private const string TrackMeshFolder = EditorPaths.Root + "/Tracks/SunsetLoop";

        public static void BuildAll()
        {
            BuildProvingGround();
            BuildSunsetLoop();
            Debug.Log("[Setup] Track scenes generated.");
        }

        /// <summary>
        /// Sunset Loop: a 1.6 km coastal circuit with a long straight, a hairpin, a fast sweeper and
        /// an esses section. Control points are hand-placed; everything else is derived.
        /// </summary>
        public static void BuildSunsetLoop()
        {
            var scene = SceneBuilder.NewScene();
            var sun = SceneBuilder.CreateSun(new Vector3(18f, 140f, 0f), new Color(1f, 0.72f, 0.45f), 2.6f, true);
            SceneBuilder.ApplyLighting(sun);
            RenderSettings.ambientIntensity = 1.15f;
            SceneBuilder.CreateGlobalVolume();
            EditorPaths.EnsureFolder(TrackMeshFolder);
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { TrackMeshFolder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            Vector3[] control =
            {
                new Vector3(0f, 0f, 0f),        // start/finish on the main straight
                new Vector3(0f, 0f, 120f),
                new Vector3(0f, 0f, 240f),
                new Vector3(-25f, 0f, 320f),    // turn 1, medium left
                new Vector3(-95f, 0f, 350f),
                new Vector3(-160f, 0f, 320f),   // turn 2 right sweeper
                new Vector3(-190f, 0f, 250f),
                new Vector3(-175f, 0f, 170f),   // esses
                new Vector3(-215f, 0f, 110f),
                new Vector3(-200f, 0f, 30f),
                new Vector3(-235f, 0f, -50f),   // hairpin approach
                new Vector3(-205f, 0f, -110f),
                new Vector3(-140f, 0f, -95f),   // hairpin exit
                new Vector3(-90f, 0f, -40f),
                new Vector3(-60f, 0f, -110f),   // fast right-left
                new Vector3(-10f, 0f, -130f),
                new Vector3(15f, 0f, -70f),     // onto the straight
            };
            var halfWidths = new float[control.Length];
            for (int i = 0; i < halfWidths.Length; i++) halfWidths[i] = 6.5f;
            halfWidths[10] = halfWidths[11] = 7.5f; // wider hairpin

            var samples = TrackMeshBuilder.SampleSpline(control, halfWidths, true, 4f);

            var road = MaterialFactory.Opaque("Track_Asphalt", new Color(0.24f, 0.24f, 0.25f), 0f, 0.42f);
            road.mainTexture = GetOrCreateCheckerTexture();
            road.mainTextureScale = new Vector2(1f, 1f);
            EditorUtility.SetDirty(road);
            var kerb = MaterialFactory.Opaque("Track_Kerb", new Color(0.85f, 0.2f, 0.15f), 0f, 0.45f);
            var barrier = MaterialFactory.Opaque("Track_Barrier", new Color(0.82f, 0.82f, 0.85f), 0.2f, 0.5f);
            var grass = MaterialFactory.Opaque("Track_Ground", new Color(0.36f, 0.42f, 0.24f), 0f, 0.2f);
            var sea = MaterialFactory.Opaque("Track_Sea", new Color(0.05f, 0.25f, 0.4f), 0.1f, 0.95f);
            var rock = MaterialFactory.Opaque("Track_Rock", new Color(0.45f, 0.4f, 0.36f), 0f, 0.35f);

            var trackRoot = new GameObject("Track");
            TrackMeshBuilder.BuildRoad(trackRoot.transform, samples, true, 40, road, kerb, TrackMeshFolder, "SunsetLoop", GameLayers.Track);
            TrackMeshBuilder.BuildBarriers(trackRoot.transform, samples, true, 3.5f, 1.1f, barrier, GameLayers.Track, TrackMeshFolder, "SunsetLoop");

            // Ground, sea and a few landmarks for speed perception.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(trackRoot.transform, false);
            ground.transform.position = new Vector3(-100f, -0.05f, 100f);
            ground.transform.localScale = new Vector3(90f, 1f, 90f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = grass;
            ground.isStatic = true;
            ground.layer = GameLayers.Track;
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Sea";
            water.transform.SetParent(trackRoot.transform, false);
            water.transform.position = new Vector3(-100f, -6f, 100f);
            water.transform.localScale = new Vector3(400f, 1f, 400f);
            water.GetComponent<MeshRenderer>().sharedMaterial = sea;
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.isStatic = true;
            var rocks = new GameObject("Rocks");
            rocks.transform.SetParent(trackRoot.transform, false);
            var rng = new System.Random(1234);
            for (int i = 0; i < 60; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float radius = 330f + (float)rng.NextDouble() * 120f;
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.name = "Rock" + i;
                r.transform.SetParent(rocks.transform, false);
                r.transform.position = new Vector3(-100f + Mathf.Cos(angle) * radius, 2f, 100f + Mathf.Sin(angle) * radius);
                r.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f);
                float size = 6f + (float)rng.NextDouble() * 14f;
                r.transform.localScale = new Vector3(size, size * 0.6f, size);
                r.GetComponent<MeshRenderer>().sharedMaterial = rock;
                r.isStatic = true;
                r.layer = GameLayers.Track;
            }

            // Layout: racing line, checkpoints every ~110 m, grid behind the line.
            var linePoints = new Vector3[samples.Count];
            var lineWidths = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                linePoints[i] = samples[i].Position + Vector3.up * 0.05f;
                lineWidths[i] = samples[i].HalfWidth;
            }
            // 0.8 g leaves margin under the street cars' ~1.0 lateral grip; profiles scale from there.
            var racingLine = RacingLine.Build(linePoints, lineWidths, true, 0.8f, 75f);

            var layoutGo = new GameObject("TrackLayout");
            var layout = layoutGo.AddComponent<TrackLayout>();
            var gatesRoot = new GameObject("Checkpoints");
            gatesRoot.transform.SetParent(layoutGo.transform, false);
            var gates = new List<Checkpoint>();
            float gateSpacing = 110f;
            float nextGate = 0f;
            float travelled = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                if (i > 0) travelled += Vector3.Distance(samples[i - 1].Position, samples[i].Position);
                if (travelled + 0.01f < nextGate) continue;
                if (racingLine.TotalLength - travelled < gateSpacing * 0.5f && gates.Count > 0) break;
                var s = samples[i];
                var gate = new GameObject("Checkpoint" + gates.Count, typeof(BoxCollider), typeof(Checkpoint));
                gate.transform.SetParent(gatesRoot.transform, false);
                gate.transform.SetPositionAndRotation(s.Position + Vector3.up * 2f, Quaternion.LookRotation(s.Forward, Vector3.up));
                gate.layer = GameLayers.Checkpoint;
                var box = gate.GetComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3((s.HalfWidth + 4f) * 2f, 6f, 1.5f);
                var cp = gate.GetComponent<Checkpoint>();
                cp.EditorInitialize(gates.Count, s.HalfWidth + 4f);
                gates.Add(cp);
                nextGate += gateSpacing;
            }

            // Grid: 2 columns, 8 slots, starting 12 m behind the line, spaced 8 m.
            var gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(layoutGo.transform, false);
            var slots = new Transform[8];
            var start = samples[0];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = new GameObject("GridSlot" + i);
                slot.transform.SetParent(gridRoot.transform, false);
                float back = 12f + (i / 2) * 8f;
                float side = (i % 2 == 0 ? -1f : 1f) * 2.6f;
                slot.transform.SetPositionAndRotation(start.Position - start.Forward * back + start.Right * side + Vector3.up * 0.1f,
                    Quaternion.LookRotation(start.Forward, Vector3.up));
                slots[i] = slot.transform;
            }
            // Start line marking
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "StartLine";
            line.transform.SetParent(trackRoot.transform, false);
            line.transform.SetPositionAndRotation(start.Position + Vector3.up * 0.02f, Quaternion.LookRotation(start.Forward, Vector3.up));
            line.transform.localScale = new Vector3(start.HalfWidth * 2f, 0.02f, 1.2f);
            line.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Opaque("Track_Line", Color.white, 0f, 0.3f);
            Object.DestroyImmediate(line.GetComponent<Collider>());
            line.isStatic = true;

            layout.EditorInitialize(ContentGenerator.CircuitTrackId, gates.ToArray(), slots, racingLine, true, null, 5f);

            var camera = SceneBuilder.CreateCamera("Camera", start.Position - start.Forward * 20f + Vector3.up * 4f, start.Position, 58f, Color.black, true);
            var rig = camera.gameObject.AddComponent<VehicleCameraRig>();

            var probeGo = new GameObject("ReflectionProbe", typeof(ReflectionProbe));
            var probe = probeGo.GetComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.resolution = 64;
            probe.size = new Vector3(600f, 80f, 600f);
            probeGo.transform.position = new Vector3(-100f, 5f, 100f);

            var sessionGo = new GameObject("RaceSession");
            var session = sessionGo.AddComponent<RaceSession>();
            session.EditorWire(layout, rig);

            var ui = RaceUiBuilder.Build(session, rig);
            var screen = ui.Canvas.gameObject.AddComponent<RaceScreenController>();
            screen.EditorWire(session, ui.Hud, ui.Countdown, ui.PausePanel, ui.ResumeButton, ui.RestartButton, ui.QuitButton,
                ui.Results, ui.Controls.gameObject);

            EditorSceneManager.SaveScene(scene, SunsetLoopPath);
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

            EditorSceneManager.SaveScene(scene, ProvingGroundPath);
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
