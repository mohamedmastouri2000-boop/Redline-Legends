using RedlineLegends.Cameras;
using RedlineLegends.Core;
using RedlineLegends.Race;
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

        public static void BuildAll()
        {
            BuildProvingGround();
            Debug.Log("[Setup] Track scenes generated.");
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
