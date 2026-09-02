# Redline Legends — Technical Architecture

Unity 6.6 (6000.6.0f1), URP 17.6, Input System 1.20, C# 9. Android first, offline single-player, designed so
online multiplayer can be added without rewriting vehicle, race, progression or input code.

## 1. Assemblies and folders

Everything ships under `Assets/_Project`. Four assembly definitions form the dependency layers; arrows point
toward dependencies.

```
RedlineLegends.Editor  ──►  RedlineLegends.UI  ──►  RedlineLegends.Gameplay  ──►  RedlineLegends.Domain
                                                                                     (no scene / physics code)
```

| Assembly | Folder | Contains |
|---|---|---|
| Domain | `Scripts/Domain` | Core (bootstrap, services, state, scene flow), Utilities, Input abstraction, Save, Content definitions (Vehicles, Tracks, Events, Championships, AI, Rewards, Audio), Upgrades, Tuning, Progression, Economy, Race contracts |
| Gameplay | `Scripts/Gameplay` | Vehicles (simulation), Race, CircuitRace, DragRace, AI drivers, Tracks (layout, checkpoints), Cameras, VFX, Audio |
| UI | `Scripts/UI` | Screens, HUD, Garage, Common |
| Editor | `Scripts/Editor` | Project generator (URP tiers, settings, content, prefabs, scenes) |

Namespaces follow the module (`RedlineLegends.Vehicles`, `RedlineLegends.Race`, …) regardless of assembly, so a
definition asset and its runtime consumer share a namespace while living in different layers.

Other folders: `Content/` (ScriptableObject assets), `Prefabs/`, `Scenes/`, `Materials/`, `Settings/` (URP assets,
post-process profile, lighting), `Input/` (action asset), `Resources/` (only `GameConfig` and `AppRoot`), `Tests/`.

## 2. Boot and services

`GameBootstrap` (on the `Resources/AppRoot` prefab) is instantiated before the first scene by a
`RuntimeInitializeOnLoadMethod`, so pressing Play in any scene boots the game. It is the composition root: it builds
every service in dependency order and registers it in a `ServiceContainer`. Scene objects reach services through
the single static facade `Services.Get<T>()`; plain C# classes take dependencies through constructors.

Boot order: `GameConfig` (Resources) → `ContentCatalog` → `SaveService.Load()` → `SettingsService` (applies quality
level / frame rate) → `PlayerProfileService` → `ProgressionService` → `GarageService` → `MobileInputProvider` →
`SceneFlowService`. Bootstrap scene → main menu; other scenes keep their state (developer convenience).

`GameStateMachine` exposes coarse states (Boot, MainMenu, Garage, Loading, Race, Results). `SceneFlowService`
performs every scene load behind the persistent `LoadingOverlay` and carries the pending `RaceLaunchRequest`
into the race scene.

## 3. Content model (static, ScriptableObjects)

All content has a stable lowercase id that never changes once shipped; display names are never keys.
`ContentDatabase` lists every asset; `ContentCatalog` indexes it at boot and validates ids.

- `VehicleDefinition` — id, class (Street/Sport/Super/Hyper), price, unlock rule, `VehicleStats` (engine curve,
  transmission, chassis, tyres, handling, brakes, suspension, nitrous), upgrade slots, tuning limits, paints,
  visual prefab, audio definition.
- `VehicleUpgradeDefinition` — category × 3 cumulative stages of `StatModifier`s applied by
  `VehicleStatModifierApplier`. Upgrades change the simulated stats, never just UI numbers.
- `TrackDefinition` — scene name, theme, length, loop/drag support. Adding a track = new asset + scene.
- `RaceEventDefinition` (abstract) → `CircuitEventDefinition` (Circuit, Sprint, TimeAttack, Elimination, Checkpoint)
  and `DragEventDefinition` (¼ / ½ mile). Shared: track, rewards, restriction, recommended PR, weather, time of
  day, unlock rule, AI profile, opponent pool, boss flag.
- `ChampionshipDefinition` — ordered events, unlock rule, completion bonus.
- `AIProfile` — driving quality knobs (reaction, aggression, cornering, braking, throttle, mistakes) and drag
  knobs (reaction window, launch, shift accuracy, nitrous strategy). `SpeedScale` is clamped ≤ 1: difficulty never
  grants physics bonuses.
- `ProgressionConfig` — level curve, starting credits, starter car, feature gates.

`VehicleSpecBuilder` clones base stats, applies the owned upgrade stages and tuning, and computes the
Performance Rating (`PerformanceRatingCalculator`: simulated 0–100 time, estimated top speed, grip, braking g,
handling, power-to-weight → 100…999). The resulting `VehicleSpec` is what races and multiplayer would exchange.

## 4. Save model (mutable, plain serializable classes)

`SaveData` (versioned, `CurrentVersion`) holds profile, garage (owned cars with upgrade stages, tuning, paint),
progression (per-event records, championship bonuses, tutorials), settings, achievements. Written by `SaveService`
as an envelope `{v, payload, sig}` with an HMAC-SHA256 signature, through `FileSaveStore` (temp file → replace →
`.bak`). Load order: main file → backup → fresh profile; unreadable files are quarantined. `ISaveMigration`
steps upgrade raw JSON version-by-version. Content is never stored in the save; only ids and numbers.

## 5. Input

`VehicleInputState` is the only thing a vehicle consumes. `IInputProvider` implementations:
`MobileInputProvider` (touch buttons / wheel / tilt, plus keyboard & gamepad through the Input System asset;
smoothing and sensitivity live here), `AIInputProvider` (mailbox written by AI drivers), `RecordingInputProvider`
and `ReplayInputProvider` (fixed-step traces for ghosts/repros). A future `NetworkInputProvider` is another
implementation; nothing downstream changes.

## 6. Race contracts (multiplayer-ready seams)

- `RacerId` — stable per-session identity; no code assumes index 0 is the player.
- `RaceParticipantSpec` — id, name, vehicle id + resolved spec, `ControlSource` (LocalPlayer, AI, Replay, Remote),
  grid slot, paint.
- `RaceLaunchRequest` — event, mode, track scene, participants, deterministic seed. Built by `RaceLaunchBuilder`
  from the menu; a lobby would build the same object.
- `RaceOutcome` / `RacerResult` — what the race produced; `ProgressionService.RecordOutcome` turns it into rewards
  (`RewardCalculator`), records and championship bonuses, then saves.

## 7. Rendering targets

Three URP assets (Low / Medium / High) bound to quality levels of the same name; `SettingsService` switches them.
High: HDR, MSAA 4×, 4 soft shadow cascades to 150 m, SSAO, per-pixel additional lights. Global post-process
volume: ACES tonemapping, bloom, colour adjustments, vignette, light motion blur. Car paint uses Complex Lit
with clear coat; paint colours are applied per instance via `MaterialPropertyBlock` (no material instances).
Linear colour space, Vulkan then GLES3, IL2CPP ARM64, landscape only.

## 8. Generation tooling

`Redline Legends > Setup > Generate Project` (or `-executeMethod RedlineLegends.Editor.SetupMenu.GenerateAllBatch`)
imports TMP essentials, creates URP tiers, applies player settings, writes content assets (idempotent, GUIDs kept),
builds placeholder car prefabs that follow the `VehicleVisualUtility` naming contract, the AppRoot prefab, and
the Bootstrap / MainMenu / Garage scenes, then sets the build scene list. Track scenes are generated by the
track tooling added with the circuit slice.

## 9. Vehicle simulation (Phase 2)

`VehicleController` runs on one Rigidbody with four raycast wheels (chosen over WheelCollider for predictable,
tunable arcade-realistic handling at 50 Hz):

- Suspension: spring with static preload (authored stance = mid-travel rest), damper (spring-only on the
  landing frame, clamped), progressive bump stop, anti-roll bar per axle. `RideHeightM` is an offset from the
  model's stance; the body collider always clears full compression.
- Tyres: slip-angle lateral curve with a low-speed damping blend, longitudinal drive/brake clamped by load ×
  grip, friction ellipse, wheelspin modelled as extra surface speed that grows when torque exceeds grip and
  degrades grip progressively (burnouts, launch management). ABS/traction control scale with `StabilityAssist`.
- Drivetrain: torque curve × throttle × turbo boost × nitrous; auto clutch with a torque-converter-style stall
  speed and a free-rev launch hold (`HoldBrakes`) for drag staging; FWD/RWD/AWD torque split; automatic gearbox
  decided on road-speed rpm with hysteresis, or manual with `ShiftQuality` (Perfect/Good/Early/Late) events.
- Everything the HUD, audio, VFX, AI and cameras need is in `VehicleTelemetry`, refreshed every step.
- `VehicleFactory` builds a car from a `RaceParticipantSpec` + `VehicleDefinition` and any `IInputProvider`;
  `VehicleCameraRig` provides chase (speed/acceleration/drift/collision reactive), hood and cockpit views.
- Verified by `VehicleDriveTests` (settle, accelerate, brake, corner, reset, manual shift) in the proving ground.

## 10. Circuit racing (Phase 3)

- **Track scene contract**: a `TrackLayout` with ordered `Checkpoint` triggers (index 0 = start/finish, also the
  respawn pose), grid slot transforms just behind the line, and a `RacingLine` (dense polyline with per-node
  target speeds derived from curvature × grip, back-propagated for braking zones). `TrackMeshBuilder` generates
  chunked road/kerb meshes and solid extruded barriers from a control polygon; `Track_SunsetLoop` is the first.
- **RaceSession** (one per track scene, `ILocalRacerSource`): consumes the `RaceLaunchRequest`, spawns every
  participant through `VehicleFactory` (player → `MobileInputProvider`, AI → `AIInputProvider` + `AIDriver`),
  runs countdown → racing → finishing → finished, validates checkpoint order (shortcuts cannot rank), tracks
  laps/lap times/progress/positions at 10 Hz, wrong-way and upside-down detection, occupancy-aware respawns,
  and the rules for Circuit, Sprint, Time Attack, Elimination and Checkpoint events. It emits a `RaceOutcome`,
  hands it to `ProgressionService.RecordOutcome` (rewards, records, championship bonus, save) and to the UI.
- **AIDriver**: pure-pursuit steering on the line with a speed-scaled look-ahead, braking horizon over the
  line's target speeds (never faster than its own car can corner), throttle easing in corners and on slip,
  spherecast car awareness with aggression-scaled gaps and side selection for overtakes, seeded mistakes
  (late braking, wide line, lift) at the profile's frequency, reaction-time lag on all inputs, stuck recovery.
- **Race UI**: `RaceScreenController` binds HUD info/countdown/messages, pause menu and `ResultsPanel`.
- Verified by `CircuitRaceTests`: menu → launch → autopilot race → finish → reward → save reload → menu.

## 11. Drag racing (Phase 4)

- **DragSession** (`Track_HarborStrip`): staging on the brakes (rev freely, `HoldBrakes` launch hold), a
  three-amber light tree at 0.5 s intervals, brakes released on the first amber so jumping the start is
  possible; a car that moves before green red-lights and is classified last. Reaction time is green-to-launch
  (car leaves the line). Elapsed time and trap speed at the ¼ or ½ mile; player shift quality is scored per
  shift (Perfect/Good/Early/Late) from the vehicle's own `Shifted` event; results go through the same
  `RaceOutcome` → `ProgressionService` path (best ET and best reaction are persisted).
- **DragAIDriver**: seeded reaction time inside the profile's window, false-start chance, launch rpm target
  with quality-dependent error (bang-bang throttle hold), manual shifts at an accuracy-dependent rpm, lane
  keeping, nitrous strategy (launch / after 2nd shift / final stretch / random).
- **UI**: `DragHudPanel` (light tree, RT, shift feedback, lane gap bar) on top of the shared `RaceHud`,
  bound by `DragScreenController`.
- Verified by `DragRaceTests` (expert autopilot with manual shifts: green, launch, ≥2 shifts, finish, reward,
  reaction and ET persisted, save reload, back to menu).

## 12. Garage, progression polish, audio, VFX (Phase 5)

- **Settings** (`SettingsPanel`): control style, gearbox, camera, graphics tier, 30/60 fps, units, vibration,
  tutorials, steering/tilt sensitivity, camera shake, master/music/effects volume. Applied immediately through
  `SettingsService`; consumers (input, cameras, audio, quality) subscribe to its change event.
- **Garage**: browse/buy/select, upgrade rows per category, paint selector (paid options), `TuningPanel` with a
  live PR and 0–100/top-speed readout, gear ratios gated by `ProgressionConfig.AdvancedTuningLevel`, Test Drive.
- **Achievements**: `AchievementDefinition` assets watch lifetime counters in `PlayerStatsData`; the
  `AchievementService` listens to progression/garage/profile events, pays rewards, and the menu lists them.
- **Haptics**: `HapticsService` pulses on perfect shifts and collisions when vibration is enabled.
- **Audio**: `AudioService` (master → AudioListener; music/effects multipliers). `VehicleAudio` per car: engine
  layer crossfade (or a synthesised placeholder when the `VehicleAudioDefinition` slot is empty), tyre squeal,
  wind, nitrous, shift/limiter/impact one-shots; UI clicks per canvas. All sources created once.
- **VFX**: `VfxLibrary` materials (generated textures); `VehicleEffects` per car with pre-created particle
  systems for tyre smoke, sparks, backfire and nitrous; `SkidMarkRenderer` ring-buffer mesh per scene.
- **Tutorials**: `TutorialService` + `TutorialOverlay`; first circuit and first drag hold the session in
  Preparing until dismissed, first upgrade shows on the first garage visit. Skippable, remembered in the save.

## 13. Phase status

- Phase 1 Foundation — done.
- Phase 2 Vehicle prototype — done (proving ground test drive from the garage).
- Phase 3 Circuit slice — done.
- Phase 4 Drag slice — done.
- Phase 5 Garage/progression polish, audio, VFX, tutorials — done.
- Phase 6 Content — next: 15 cars, 10 championships / 50 events, drag ladder, more tracks (see
  `Docs/CONTENT_PLAN.md`); real car and environment models replacing the generated placeholders.
- Phase 7 Mobile optimisation — device profiling, LOD groups, light baking, texture compression review,
  thermal testing on low/mid/high presets.
