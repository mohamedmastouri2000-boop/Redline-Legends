# Car asset review, 7 September 2026

Reviewer: Claude (Unity side). Subject: the vehicles produced by the 3D agent (GPT6 Astra / Codex
workspace `C:\Users\moham\Documents\Codex\2026-09-05\files-pasted-by-the-user-handover`).

Method: headless Unity audit of every vehicle definition's prefab (`VehiclePrefabAudit.RunBatch`),
Unity garage renders from two angles, the full PlayMode suite, and a headless Blender 4.5.9 audit of
every `.blend` (topology, materials, dimensions, contract names, Workbench renders) using the
portable Blender found in the Codex workspace. Blender is not installed system-wide.

## 1. What is wired into the game right now

All 15 vehicle definitions point at `Assets/_Project/Prefabs/Vehicles/Handmade/{id}_visual.prefab`.
These are **not imported models**; they are a second procedural generator (`CoachbuiltCars.cs`,
"Coachwork") with per-car dimensioned designs. They are a clear step up from `CarMeshBuilder`.

| Check | Result |
|---|---|
| Contract (Wheel_FL/FR/RL/RR, CockpitCamera, Exhaust, Paint renderers) | 15/15 pass |
| LODs | 3 per car; LOD0 23.0–24.1k tris, LOD1 8.1–9.8k, LOD2 1.6–2.0k |
| Materials | 7 shared: Coach_Paint (Complex Lit, clear coat), Glass, Rubber, Titanium, Graphite, Optics, Taillights |
| Renderers per car | 15 (14 draws at LOD0) |
| Geometry below ground | none |
| PlayMode tests | 19/19 pass, including autopilot drivability on every track |
| Provenance | `Docs/Licenses/Original-Coachwork.md`: original geometry, no third-party inputs |

Visual verdict: proper sedan / coupé / mid-engine silhouettes, panel seams, light clusters, deep-dish
wheels, wing on the sport. Weak spots: mirrors are plain ellipsoids, the hyper's front is very flat,
the street cars share one face design. Width incl. mirrors is 2.3–2.5 m, so the auto body collider is
wider than before; the drivability tests still pass but barrier contact is tighter.

**Everything above is uncommitted** in the working tree (15 definition assets, 4 new editor
scripts, 90 mesh assets, 15 prefabs, materials, `Docs/Licenses`). It should be committed once the
3D agent confirms the batch is final.

## 2. Blender files (`outputs/models-v3`, 15 cars)

These are the AI-reconstruction-derived drafts (TripoSG retopo). Audit per car:

| Metric | Range across the 15 |
|---|---|
| LOD0 tris (body + 4 wheels) | 24.1–24.6k |
| Non-manifold edges | 42–465 (Tempest 0) |
| Zero-area faces | 0–358 |
| Boundary edges on LOD0 | 32–194 |
| Body length | 4.07–4.62 m, width 1.89–2.10 m |
| Contract names present | yes (Wheel_*, CockpitCamera, Exhaust as empties) |
| UVs, unit scale | all objects have UVs, all unit scale |

Visual verdict: blobby. Bodies read as smoothed lumps with glass painted on; the Kestrel v3 rear is a
rounded blob, Solaris and Lyra have no panel definition, Tempest has a rippled rear quarter. They
are **worse** than the Coachwork prefabs currently in the game and should not replace them. Worst
topology: Vulcan (465 non-manifold, 358 zero-area), Corsa V (429 / 345), Lyra (426 / 312),
Kestrel v3 (402 / 300).

## 3. Hero Kestrel (`outputs/kestrel-reference-master/veh_street_kestrel.blend`)

This is the one genuinely good model: a hand-built panelled sedan with bonnet, fenders, doors,
quarters, bumpers, tailgate, light units, wipers, badges, calipers, rotors and 21 materials.

| Metric | Value |
|---|---|
| Objects | 1,682 mesh objects |
| Triangles | 169.6k (no LODs) |
| Non-manifold edges | 0 |
| N-gons | 1,319; zero-area faces 335 |
| Objects without UVs | 1,622 of 1,682 |
| Dimensions | 4.21 × 2.05 × 1.28 m, bottom at ground |

Verdict: excellent reference, not game-ready. It needs joining into ~6 meshes, triangulation of
n-gons, UVs, and a 20–25k LOD0 with two lower LODs before it can enter the prefab contract. The
`kestrel-panel-rebuild` variant (19.5k body tris, 0 non-manifold, 458 n-gons) is the right
starting point for that: it already fits the budget and reads as the same car.

## 4. Legal flags

- `work/gta-evaluation/` and `outputs/fleet15-private-evaluation/` hold downloaded GTA V mod
  conversions of real cars (Aston Martin Valhalla, Bugatti Divo, Ferrari SF90 XX, FXX-K Evo,
  Huracán STO, Lotus Evija, McLaren P1 and Senna, Mercedes-AMG One, Pagani Huayra and Utopia,
  Revuelto, Rimac Nevera, Ford GT, Jesko). The agent's own notes say none is cleared. None of it may
  enter the repo or the build: mod redistribution is prohibited by the source sites, several derive
  from Forza assets, and the brands are trademarked. Keep them out of `Assets/`.
- `outputs/creator-outreach/` contains drafted emails to mod authors about real-brand cars. If this
  outreach happens, ask for original, unbranded work; a licence from a modder does not clear the
  manufacturer's design rights.
- The Coachwork and Kestrel-master assets are original and safe.

## 5. Other findings

- `ProjectSettings/QualitySettings.asset` was changed (anti-aliasing 0 → 2x MSAA) outside the
  graphics-preset system (`RenderPipelineSetup`). Either revert it or move it into the presets.
- `Assets/InitTestScene1f78254f-….unity` is a stray Unity test-runner scene; delete it.
- `ReferenceModelImport` refuses production import unless `REDLINE_MODEL_STAGE=1`, so the two staged
  drafts (Kestrel, Tempest v2) sit in `Handmade/ReferenceReview/` and are not used by the game. Good.

## 6. Recommendation

1. Commit the Coachwork batch as the shipping cars; it beats every alternative on hand.
2. Do not import `models-v3`; retire the TripoSG line.
3. Promote the Kestrel: reduce the reference master (or start from `kestrel-panel-rebuild`), give it
   three LODs and UVs, and bring it in through `Handmade/` as the first real hero car. Repeat the
   same hand-built approach per class rather than per car.
4. Keep the GTA evaluation folder outside the repo and out of any build.

Artefacts: Unity audit CSV/TXT and renders, Blender audit JSON and renders were produced in the
session scratchpad; the audit script itself is `Assets/_Project/Scripts/Editor/Setup/VehiclePrefabAudit.cs`
(menu: Redline Legends > Capture > Audit vehicle prefabs).
