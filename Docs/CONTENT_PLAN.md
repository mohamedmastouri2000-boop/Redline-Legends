# Content Plan (Phase 6)

Status: implemented as generator tables (`ContentGenerator`, `TrackSpecs`) on 2026-09-03 — 15 cars, 8 circuits +
drag strip + proving ground, 10 championships / 50 events, 12-round drag ladder, 17 achievements. Numbers below
are the design intent; the generator derives physics per class and computes recommended PR from the roster.

Targets for the initial release: 15 fictional cars across four tiers, 10 championships of five events (50
circuit-mode events including specials), plus a drag ladder. All ids are stable snake_case and never reused.
Everything here becomes rows in `ContentGenerator` (or hand-authored assets in `Assets/_Project/Content`).

## Vehicles (15)

| Id | Name | Brand | Class | Drive | hp | Nm | kg | Top km/h | Price | Unlock |
|---|---|---|---|---|---|---|---|---|---|---|
| veh_street_kestrel | Kestrel GT | Aster | Street | FWD | 150 | 205 | 1180 | 196 | 12 000 | starter |
| veh_street_vulcan | Vulcan 240 | Norrad | Street | RWD | 240 | 320 | 1350 | 236 | 18 500 | — |
| veh_street_ibex | Ibex Rally | Toran | Street | AWD | 210 | 300 | 1320 | 218 | 21 000 | level 3 |
| veh_street_corsa_v | Corsa V | Aster | Street | FWD | 185 | 240 | 1210 | 215 | 16 000 | level 2 |
| veh_sport_stratos | Stratos R | Veloce | Sport | AWD | 380 | 480 | 1480 | 276 | 46 000 | level 4 |
| veh_sport_meridian | Meridian S | Norrad | Sport | RWD | 420 | 540 | 1560 | 288 | 58 000 | chp_02 |
| veh_sport_harrier | Harrier Turbo | Toran | Sport | AWD | 340 | 450 | 1420 | 268 | 42 000 | level 5 |
| veh_sport_lyra | Lyra GT-S | Aster | Sport | RWD | 365 | 470 | 1390 | 282 | 52 000 | chp_03 |
| veh_super_tempest | Tempest | Veloce | Super | RWD | 620 | 720 | 1450 | 330 | 145 000 | chp_04 |
| veh_super_helion | Helion | Kurai | Super | AWD | 680 | 800 | 1620 | 335 | 165 000 | chp_05 |
| veh_super_viper_x | Viperone X | Norrad | Super | RWD | 590 | 760 | 1520 | 322 | 130 000 | level 12 |
| veh_super_ardent | Ardent GT3 | Toran | Super | RWD | 560 | 640 | 1350 | 312 | 150 000 | chp_06 |
| veh_hyper_zenith | Zenith | Veloce | Hyper | AWD | 1050 | 1200 | 1550 | 380 | 420 000 | chp_07 |
| veh_hyper_solaris | Solaris | Kurai | Hyper | AWD | 1200 | 1350 | 1680 | 400 | 520 000 | chp_08 |
| veh_hyper_wraith | Wraith Evo | Norrad | Hyper | RWD | 980 | 1100 | 1420 | 372 | 380 000 | chp_09 |

Brands are fictional. Placeholder visuals come from `PlaceholderCarBuilder` per class; real models replace
the `*_visual.prefab` assets while keeping the wheel/anchor naming contract.

## Tracks (8 environments, variants via reverse/short layouts)

| Id | Name | Theme | Length | Notes |
|---|---|---|---|---|
| trk_sunset_loop | Sunset Loop | Coast | 1.65 km | done (procedural) |
| trk_harbor_strip | Harbor Strip | Industrial (night) | drag | done (procedural) |
| trk_proving_ground | Proving Ground | Industrial | test drive | done |
| trk_city_circuit | Meridian Downtown | Modern city | 2.4 km | tight 90° corners, chicane |
| trk_night_run | Neon Loop | Night city | 2.1 km | fast, lit boulevards |
| trk_dune_pass | Dune Pass | Desert | 3.0 km | long sweepers, elevation |
| trk_alpine_climb | Alpine Climb | Mountains | 2.8 km sprint | point-to-point hill climb |
| trk_cargo_yard | Cargo Yard | Industrial | 1.9 km | technical, shortcuts enabled |
| trk_ridge_highway | Ridge Highway | Highway | 4.2 km | top-speed track |
| trk_grand_circuit | Grand Circuit | Race circuit | 3.6 km | championship finale |

Each new track = control polygon + theme dressing in `TrackSceneBuilder`, one `TrackDefinition`, no code changes
in race logic.

## Championships (10 × 5 events)

| Id | Name | Tracks | AI | Class gate | PR band | Special |
|---|---|---|---|---|---|---|
| chp_01_beginner_streets | Beginner Streets | Sunset Loop, Harbor Strip | Rookie | Street | 170–220 | boss race vs Vulcan (done) |
| chp_02_city_challenge | City Challenge | Meridian Downtown, Sunset Loop | Rookie→Amateur | Street | 200–260 | time attack, checkpoint |
| chp_03_desert_series | Desert Series | Dune Pass, Ridge Highway | Amateur | Street/Sport | 240–320 | elimination |
| chp_04_mountain_cup | Mountain Cup | Alpine Climb, Dune Pass | Amateur→Pro | Sport | 300–380 | sprint hill climb boss |
| chp_05_night_racing | Night Racing | Neon Loop, Harbor Strip | Pro | Sport | 340–420 | drag ½ mile, night circuit |
| chp_06_industrial_open | Industrial Open | Cargo Yard, Meridian | Pro | Sport/Super | 400–500 | shortcuts on, elimination |
| chp_07_coastal_gp | Coastal GP | Sunset Loop rev., Ridge Highway | Pro→Expert | Super | 480–580 | endurance 6 laps |
| chp_08_neon_nights | Neon Nights | Neon Loop, Harbor Strip | Expert | Super | 540–640 | drag tournament ladder |
| chp_09_summit_series | Summit Series | Alpine Climb, Grand Circuit | Expert | Super/Hyper | 620–760 | checkpoint sprint |
| chp_10_legends_cup | Legends Cup | Grand Circuit, all | Legend | Hyper | 760–999 | 3 boss events |

Event mix per championship: 2 circuit races, 1 sprint or time attack, 1 elimination/checkpoint, 1 drag or
boss. Rewards scale ≈ ×1.35 per championship; AI upgrade stage follows the profile tier.

## Drag ladder

Quarter-mile rounds 1–8 then half-mile 9–12 against named rivals with specific cars (`DragEventDefinition.
OpponentVehicle`), one boss every four rounds. Rewards: credits + a paint unlock for the boss wins.

## Balance rules

- Recommended PR of an event = median PR of the intended car list at the AI's upgrade stage.
- AI never exceeds `SpeedScale` 1.0; difficulty comes from reaction, accuracy, braking, mistakes.
- Star thresholds for time attack = best AI lap × 1.02 / 1.08 / 1.18.
- No event should require more than ~3 replays of earlier content to afford the entry car.
