# Redline Legends

Offline-first 3D racing game for Android built with Unity 6 (6000.6), URP and C#. Two modes: drag racing and
circuit racing. Single-player at launch, architected so online multiplayer can be added without rewriting the
vehicle, race, progression or input systems.

## Requirements

- Unity 6000.6.0f1 with Android Build Support (SDK, NDK, OpenJDK modules)
- Packages are resolved from `Packages/manifest.json` (URP 17.6, Input System 1.20, uGUI/TextMeshPro)

## First open / regenerate

The project is generated from code so every scene, prefab, material and content asset can be rebuilt:

- In the editor: **Redline Legends > Setup > Generate Project (all steps)**
- Headless:

```bash
Unity.exe -batchmode -nographics -quit -projectPath "D:\Redline Legends" -executeMethod RedlineLegends.Editor.SetupMenu.GenerateAllBatch -logFile generate.log
```

This creates the URP quality tiers, Android player settings, content (cars, upgrades, tracks, events,
championships, AI profiles), the `AppRoot` prefab, and the scenes: Bootstrap, MainMenu, Garage,
Track_ProvingGround (test drive), Track_SunsetLoop (circuit), Track_HarborStrip (drag).

Play from **any** scene: the bootstrap creates itself before the first scene loads. Playing from Bootstrap goes
to the main menu; playing from a track scene starts the first event on that track (or a free drive).

## Tests

PlayMode tests cover boot → menu → garage, vehicle physics (settle, accelerate, brake, corner, reset, manual
shift quality), a full autopilot circuit race with rewards and save reload, and a full drag race.

```bash
Unity.exe -batchmode -projectPath "D:\Redline Legends" -runTests -testPlatform PlayMode -testResults results.xml -logFile tests.log
```

## Build

```bash
Unity.exe -batchmode -nographics -quit -projectPath "D:\Redline Legends" -buildTarget Android -executeMethod RedlineLegends.Editor.BuildScripts.BuildAndroidApk -logFile build.log
```

Output: `Builds/Android/RedlineLegends.apk` (IL2CPP, ARM64, Vulkan + GLES3, landscape).

## Layout

See `Docs/ARCHITECTURE.md` for the module map, boot sequence, content model, save format, input abstraction,
race contracts, vehicle simulation, circuit and drag sessions, and the phase plan.

## Controls (editor testing)

W/S or arrows throttle/brake, A/D steer, Space handbrake, Left Shift nitrous, E/Q shift up/down, R reset,
C camera, Esc pause. Gamepad is mapped too. On device the on-screen controls follow the control-style setting
(buttons, steering wheel or tilt).
