using System.Collections.Generic;
using RedlineLegends.Cameras;
using RedlineLegends.Core;
using RedlineLegends.Race;
using RedlineLegends.Tracks;
using RedlineLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Builds a complete circuit scene from a <see cref="CircuitSpec"/>: road, kerbs, barriers,
    /// themed dressing, lighting, layout (gates, grid, racing line), camera, session and UI.
    /// </summary>
    public static class CircuitBuilder
    {
        public static string ScenePath(CircuitSpec spec) => EditorPaths.Scenes + "/" + spec.SceneName + ".unity";
        private static string MeshFolder(CircuitSpec spec) => EditorPaths.Root + "/Tracks/" + spec.SceneName.Replace("Track_", "");

        public static void Build(CircuitSpec spec)
        {
            var scene = SceneBuilder.NewScene();
            var sun = SceneBuilder.CreateSun(spec.SunEuler, spec.SunColor, spec.SunIntensity, true);
            var sky = EditorPaths.GetOrCreateMaterial(EditorPaths.Materials + "/Sky_" + spec.SceneName.Replace("Track_", "") + ".mat", Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_AtmosphereThickness", spec.Atmosphere);
            sky.SetColor("_SkyTint", spec.SkyTint);
            sky.SetColor("_GroundColor", spec.SkyGround);
            sky.SetFloat("_Exposure", spec.SkyExposure);
            EditorUtility.SetDirty(sky);
            SceneBuilder.ApplyLighting(sun);
            RenderSettings.skybox = sky;
            RenderSettings.ambientIntensity = spec.AmbientIntensity;
            if (spec.NightAmbient)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = spec.AmbientSky;
                RenderSettings.ambientEquatorColor = spec.AmbientEquator;
                RenderSettings.ambientGroundColor = spec.AmbientGround;
            }
            RenderSettings.fog = spec.Fog;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = spec.FogColor;
            RenderSettings.fogDensity = spec.FogDensity;
            SceneBuilder.CreateGlobalVolume();

            string meshFolder = MeshFolder(spec);
            EditorPaths.EnsureFolder(meshFolder);
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { meshFolder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            var halfWidths = new float[spec.Control.Length];
            for (int i = 0; i < halfWidths.Length; i++) halfWidths[i] = spec.HalfWidth;
            var samples = TrackMeshBuilder.SampleSpline(spec.Control, halfWidths, spec.Loop, 4f);
            string prefix = spec.SceneName.Replace("Track_", "");

            // Road UVs: u across the width, v = metres / 8, so one asphalt tile (with markings) spans 8 m.
            var road = MaterialFactory.Textured(prefix + "_Asphalt", ProceduralTextures.Asphalt(), spec.Asphalt * 2.4f, 0f, 0.38f, Vector2.one);
            var kerbTint = Color.Lerp(spec.Ground, Color.white, 0.35f) * 1.6f;
            var kerb = spec.KerbEmission.maxColorComponent > 0.01f
                ? MaterialFactory.TexturedEmissive(prefix + "_Kerb", ProceduralTextures.Shoulder(), ProceduralTextures.ShoulderEmission(), kerbTint, spec.KerbEmission, Vector2.one)
                : MaterialFactory.Textured(prefix + "_Kerb", ProceduralTextures.Shoulder(), kerbTint, 0f, 0.3f, Vector2.one);
            var barrier = MaterialFactory.Textured(prefix + "_Barrier", ProceduralTextures.Concrete(), spec.Barrier * 0.9f, 0.1f, 0.35f, new Vector2(0.25f, 1f));
            var ground = MaterialFactory.Textured(prefix + "_Ground", ProceduralTextures.Ground(), spec.Ground * 1.7f, 0f, 0.15f, new Vector2(60f, 60f));

            var trackRoot = new GameObject("Track");
            TrackMeshBuilder.BuildRoad(trackRoot.transform, samples, spec.Loop, 40, road, kerb, meshFolder, prefix, GameLayers.Track);
            TrackMeshBuilder.BuildBarriers(trackRoot.transform, samples, spec.Loop, 3.5f, 1.1f, barrier, GameLayers.Track, meshFolder, prefix);

            var bounds = new Bounds(samples[0].Position, Vector3.zero);
            for (int i = 1; i < samples.Count; i++) bounds.Encapsulate(samples[i].Position);

            // Rolling terrain replaces the flat plane: level beside the road, hills toward the horizon.
            var rock = MaterialFactory.Textured(prefix + "_Cliff", ProceduralTextures.Concrete(), spec.CliffColor * 1.4f, 0f, 0.2f, new Vector2(4f, 4f));
            var terrain = TerrainBuilder.Build(trackRoot.transform, samples, bounds, ground, meshFolder, prefix,
                margin: 900f, cell: 10f, nearFlat: spec.TerrainNear, farHills: spec.TerrainFar, seed: spec.Id.GetHashCode() % 977,
                steepMaterial: rock, blendDistance: spec.TerrainBlend, farDistance: spec.TerrainFarDistance);

            TrackDressing.Dress(spec, samples, trackRoot.transform, bounds, terrain.HeightAt);

            // ---- layout
            var linePoints = new Vector3[samples.Count];
            var lineWidths = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                linePoints[i] = samples[i].Position + Vector3.up * 0.05f;
                lineWidths[i] = samples[i].HalfWidth;
            }
            var racingLine = RacingLine.Build(linePoints, lineWidths, spec.Loop, spec.LateralG, spec.MaxSpeedMs);

            // Point-to-point tracks start 60 m in so the grid has road behind it.
            int startIndex = 0;
            if (!spec.Loop)
            {
                float travelledStart = 0f;
                for (int i = 1; i < samples.Count; i++)
                {
                    travelledStart += Vector3.Distance(samples[i - 1].Position, samples[i].Position);
                    if (travelledStart >= 60f) { startIndex = i; break; }
                }
            }

            var layoutGo = new GameObject("TrackLayout");
            var layout = layoutGo.AddComponent<TrackLayout>();
            var gatesRoot = new GameObject("Checkpoints");
            gatesRoot.transform.SetParent(layoutGo.transform, false);
            var gates = new List<Checkpoint>();
            float gateSpacing = 110f;
            float nextGate = 0f;
            float travelled = 0f;
            for (int i = startIndex; i < samples.Count; i++)
            {
                if (i > startIndex) travelled += Vector3.Distance(samples[i - 1].Position, samples[i].Position);
                bool last = !spec.Loop && i == samples.Count - 1;
                if (travelled + 0.01f < nextGate && !last) continue;
                if (spec.Loop && racingLine.TotalLength - travelled < gateSpacing * 0.5f && gates.Count > 0) break;
                gates.Add(CreateGate(gatesRoot.transform, samples[i], gates.Count));
                nextGate += gateSpacing;
            }

            var gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(layoutGo.transform, false);
            var slots = new Transform[spec.GridSlots];
            var start = samples[startIndex];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = new GameObject("GridSlot" + i);
                slot.transform.SetParent(gridRoot.transform, false);
                float back = 12f + (i / 2) * 8f;
                float side = (i % 2 == 0 ? -1f : 1f) * Mathf.Min(2.6f, spec.HalfWidth * 0.4f);
                Vector3 pos = start.Position - start.Forward * back + start.Right * side + Vector3.up * 0.1f;
                // Follow the road surface height behind the line (climbs start on a slope).
                pos.y = SurfaceHeight(samples, pos) + 0.1f;
                slot.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(start.Forward, Vector3.up));
                slots[i] = slot.transform;
            }
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "StartLine";
            line.transform.SetParent(trackRoot.transform, false);
            line.transform.SetPositionAndRotation(start.Position + Vector3.up * 0.02f, Quaternion.LookRotation(start.Forward, Vector3.up));
            line.transform.localScale = new Vector3(start.HalfWidth * 2f, 0.02f, 1.2f);
            line.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Opaque("Track_Line", Color.white, 0f, 0.3f);
            Object.DestroyImmediate(line.GetComponent<Collider>());
            line.isStatic = true;

            layout.EditorInitialize(spec.Id, gates.ToArray(), slots, racingLine, spec.Loop, null, 5f);

            var camera = SceneBuilder.CreateCamera("Camera", start.Position - start.Forward * 20f + Vector3.up * 4f, start.Position, 58f, Color.black, true);
            var rig = camera.gameObject.AddComponent<VehicleCameraRig>();

            var probeGo = new GameObject("ReflectionProbe", typeof(ReflectionProbe));
            var probe = probeGo.GetComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.resolution = 64;
            probe.size = new Vector3(bounds.size.x + 300f, 200f, bounds.size.z + 300f);
            probeGo.transform.position = bounds.center + Vector3.up * 5f;

            var sessionGo = new GameObject("RaceSession");
            var session = sessionGo.AddComponent<RaceSession>();
            session.EditorWire(layout, rig);

            var ui = RaceUiBuilder.Build(session, rig);
            var screen = ui.Canvas.gameObject.AddComponent<RaceScreenController>();
            screen.EditorWire(session, ui.Hud, ui.Countdown, ui.PausePanel, ui.ResumeButton, ui.RestartButton, ui.QuitButton,
                ui.Results, ui.Controls.gameObject, ui.Tutorial);
            TrackSceneBuilder.CreateSkidMarks();

            SceneBuilder.FinalizeLighting();
            EditorSceneManager.SaveScene(scene, ScenePath(spec));
        }

        private static Checkpoint CreateGate(Transform parent, TrackMeshBuilder.Sample s, int index)
        {
            var gate = new GameObject("Checkpoint" + index, typeof(BoxCollider), typeof(Checkpoint));
            gate.transform.SetParent(parent, false);
            gate.transform.SetPositionAndRotation(s.Position + Vector3.up * 2f, Quaternion.LookRotation(s.Forward, Vector3.up));
            gate.layer = GameLayers.Checkpoint;
            var box = gate.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3((s.HalfWidth + 4f) * 2f, 6f, 1.5f);
            var cp = gate.GetComponent<Checkpoint>();
            cp.EditorInitialize(index, s.HalfWidth + 4f);
            return cp;
        }

        private static float SurfaceHeight(List<TrackMeshBuilder.Sample> samples, Vector3 pos)
        {
            float best = float.MaxValue;
            float y = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                float dx = samples[i].Position.x - pos.x, dz = samples[i].Position.z - pos.z;
                float d = dx * dx + dz * dz;
                if (d < best) { best = d; y = samples[i].Position.y; }
            }
            return y;
        }
    }
}
