# Handover: 3D car assets for Redline Legends

Audience: the agent taking over car visuals (GPT6 Astra). Everything else in the project is out of
scope for this handover and must keep working: physics, race flow, UI, save, tests, Android build.

## 1. Goal

Replace the procedural placeholder car bodies with real, game-ready car models for the 15 fictional
vehicles, without changing gameplay code. Target: realistic-looking cars on a mid-range Android phone.

Current state (commit `eb1ad98`, branch `main`, remote
https://github.com/mohamedmastouri2000-boop/Redline-Legends):

- All 15 cars are generated procedurally by `Assets/_Project/Scripts/Editor/Setup/CarMeshBuilder.cs`
  (sectional loft, carved arches, greenhouse, lathed wheels, boxes for details). They are ~9k
  triangles each, look like stylised low-poly cars, and are considered ~20 % of the desired quality.
- The user wants "100 % or above". Realistic real-brand models are not acceptable (trademark risk,
  and the design is 15 fictional cars). Fictional realistic packs are the target.

## 2. The contract a car prefab must satisfy

Gameplay and garage code never reference a specific model. They rely on these conventions, defined
in `Assets/_Project/Scripts/Domain/Content/Vehicles/VehicleVisualUtility.cs`:

| Item | Rule |
|---|---|
| Prefab root | Pivot on the ground plane at the car's centre, +Z forward, +X right, +Y up. Units in metres. |
| Wheels | Four transforms named exactly `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`, each pivoted at the hub centre. Gameplay spins and steers these transforms (`VehicleVisuals`) and reads their positions for the suspension rays (`VehicleFactory.BuildWheelSetups`). Anything under them is the wheel visual. |
| Paint | Any renderer whose material name contains `Paint` is repainted at runtime via MaterialPropertyBlock (`_BaseColor`, `_Metallic`, `_Smoothness`). Use one shared paint material per car or the shared `Car_Paint*` materials; do not bake colour into textures. |
| Anchors | Empty transforms named `CockpitCamera` (driver eye point) and `Exhaust` (rear, where exhaust particles spawn). |
| Colliders | None in the prefab. `VehicleFactory.ConfigureBodyCollider` builds a BoxCollider from the renderer bounds, excluding wheel renderers. |
| Materials | URP. Body: `Shader "Universal Render Pipeline/Complex Lit"` with clear coat (see `MaterialFactory.CarPaint`). Glass: URP Lit transparent (`MaterialFactory.Glass`). Cull Off on paint is currently set because the procedural body is an open shell; a closed real mesh can use back-face culling. |
| Wheel radius | Physics uses `VehicleStats.Tires.WheelRadiusM` from the vehicle definition, not the mesh. Keep the visual wheel radius within ±5 % of the stat or the tyre floats/sinks. Values per class come from `PlaceholderCarBuilder.ShapeFor`. |
| Scale | Street ~4.2 m long, sport ~4.5 m, super ~4.6 m, hyper ~4.7 m. Track ~1.55–1.7 m. Match these so grid slots, cameras and the drag lanes keep working. |
| Layer | Nothing to set; the factory sets `GameLayers.Vehicle` recursively at spawn. |

How prefabs are wired to content: `ContentGenerator` (editor) builds each vehicle definition and
calls `PlaceholderCarBuilder.BuildPrefab(id, class, materials...)`, which saves
`Assets/_Project/Prefabs/Vehicles/{id}_visual.prefab` and assigns it to the definition's
`visualPrefab` field. To use a real model, either:

1. Save your prefab at the same path with the same name, and make `BuildPrefab` skip generation when a
   hand-made prefab exists (add a `{id}_visual.prefab` existence check or a `Prefabs/Vehicles/Handmade/`
   folder that the generator prefers), or
2. Assign the prefab to the definition asset directly (`Assets/_Project/Content/Vehicles/*.asset`)
   and stop calling `BuildPrefab` for that id. Regeneration (`SetupMenu.GenerateAllBatch`) rewrites
   content assets, so option 1 is safer.

Vehicle ids (15): see `ContentGenerator.Vehicles` rows; classes Street / Sport / Super / Hyper.
Starter car: `veh_street_kestrel`.

## 3. Budgets

- Triangles: LOD0 15–25k per car, LOD1 ~8k, LOD2 ~1.5k. Eight cars on track at once.
- Textures: one 2k atlas per car (or shared), plus paint mask if used. ASTC on Android.
- Draw calls: body, glass, wheels, lights. Merge small trim parts into the body mesh.
- Add `LODGroup` on the prefab root if the pack has LODs.

## 4. Asset options already researched

Free, CC0 (no attribution), stylised low-poly, separate wheels:

- Kenney Car Kit (45+ vehicles, FBX/OBJ/glTF): https://kenney.nl/assets/car-kit
- Quaternius LowPoly Cars (8 cars, FBX/OBJ/Blend): https://quaternius.itch.io/lowpoly-cars
- Eclair Car Kit GLB (50 models): https://eclair-assets.itch.io/car-kit-glb-pack-50-free-cc0-3d-models
- OpenGameArt 3D Vehicles Pack: https://opengameart.org/content/3d-vehicles-pack

Free, CC-BY (credit line required in Settings): RgsDev Free Low Poly Vehicles Pack, 17 vehicles,
39.6k tris total, separated wheels: https://rgsdev.itch.io/free-low-poly-vehicles-pack

Paid, realistic, game-ready (Unity Asset Store EULA, user must buy with their Unity login):

- Best Sports CARS vol. 1 (Pro 3D models), $69, 10 sports cars, LOD0 15–18k / LOD1 8–10k / LOD2 1–1.3k,
  PBR 4k/2k, separate paintable wheels, rigged suspension. Top pick for the realistic look.
  https://assetstore.unity.com/packages/package/id/139912
- Modular Racing Cars (itHappy / Drippy 3D), $69 ($39.50 on sale), 15 cars, ~10k stock / ~18k dressed,
  16 wheel types, modular bumpers/spoilers/exhausts (maps onto the upgrade system), URP/HDRP, Unity 6.
  https://assetstore.unity.com/packages/3d/vehicles/land/modular-racing-cars-low-poly-game-ready-car-pack-3d-model-386024
- Mobile Ready Realistic Car Pack 1 (Scorp), $5, URP; specs unpublished, good $5 pipeline test.
  https://assetstore.unity.com/packages/3d/vehicles/mobile-ready-realistic-car-pack-1-244456

Rejected: Patricars "Complete Car Collection" on Payhip ($219.99). Real Ferrari/Bugatti/Lamborghini
etc. designs, render meshes (STL, 75 MB per car), no licence text. Trademark and licence risk.

Also rejected: the image-to-3D generation service available in the previous session had zero credits.

## 5. Import pipeline (what worked, what to watch)

- Unity 6000.6.0f1 at `C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe`. Only one
  Unity instance per project at a time. Headless commands (run from the repo root):

```bash
# regenerate content, prefabs and scenes
Unity.exe -batchmode -nographics -quit -projectPath "D:/Redline Legends" -executeMethod RedlineLegends.Editor.SetupMenu.GenerateAllBatch -logFile gen.log
# render review PNGs of every scene, both garage angles and UI (needs graphics; set REDLINE_CAPTURE_DIR)
Unity.exe -batchmode -quit -projectPath "D:/Redline Legends" -executeMethod RedlineLegends.Editor.SceneCapture.CaptureAllBatch -logFile cap.log
# tests (19 PlayMode tests, ~3 min)
Unity.exe -batchmode -projectPath "D:/Redline Legends" -runTests -testPlatform PlayMode -testResults tests.xml -logFile tests.log
# Android APK -> Builds/Android/RedlineLegends.apk
Unity.exe -batchmode -nographics -quit -projectPath "D:/Redline Legends" -buildTarget Android -executeMethod RedlineLegends.Editor.BuildScripts.BuildAndroidApk -logFile build.log
```

- glTF: no importer is installed. Prefer FBX from the packs. If glTF is needed, add
  `com.unity.cloud.gltfast` to `Packages/manifest.json`.
- FBX import: set Scale Factor so the car is the right length in metres, Convert Units on, Read/Write
  off, Mesh Compression medium, Generate Lightmap UVs off. Materials: extract, then switch to URP Lit
  or Complex Lit; the pack's Standard shader materials render pink in URP until converted
  (Edit > Rendering > Materials > Convert Selected Built-in Materials to URP).
- Paint: give the body a material whose asset name contains `Paint` (e.g. `Car_Paint_Kestrel`).
  Metallic/smoothness get overridden per paint option at runtime, so leave them default.
- Wheel pivots: most packs ship wheels as separate meshes but pivoted at the model origin. Re-parent
  each wheel mesh under an empty at its bounds centre and name the empty `Wheel_FL` etc. The empty's
  position is what physics reads.
- Verify with `SceneCapture` renders (garage shows one car per class from two angles), then the
  drivability tests (`TrackDrivabilityTests`) which spawn a car on every track.

## 6. Constraints and gotchas from this project

- Keep the visual prefab free of MonoBehaviours; `VehicleVisuals` and friends are added at spawn.
- A MonoBehaviour must live in a file named after its class or serialized refs silently become null.
- Never edit scene or prefab YAML by hand; regenerate through the editor tooling.
- Do not change `ContentGenerator` vehicle ids; save data and achievements reference them.
- Bash heredocs longer than ~200 lines get truncated by the agent tooling; write files whole.
- When patching C# with Python string slicing, assert the anchor is unique first (see memory note).
- The user reads screenshots, not logs: after any visual change, render with `SceneCapture` and send
  the PNGs.

## 7. Definition of done

1. All 15 vehicle definitions reference real-model prefabs that satisfy the contract in section 2.
2. `GenerateAllBatch` no longer overwrites those prefabs.
3. 19 PlayMode tests pass; the drivability test drives every track with the new cars.
4. Garage, menu showcase and in-race paint selection work (paint options recolour the body only).
5. APK builds, and the size increase is reported.
6. Licence file for the chosen pack added under `Docs/Licenses/` and, for CC-BY assets, a credit
   line in the Settings screen.
