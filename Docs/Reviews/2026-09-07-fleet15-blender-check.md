# Fleet15 Blender check, 7 September 2026

The 15 real-brand supercars in the 3D agent's gallery ("Redline Legends / 15, actual Blender
models, private development review"). Checked headlessly with the portable Blender 4.5.9 in the
Codex workspace (`work/gta-evaluation/tools/fleet15/*.blend`, plus `ford-review` and
`jesko-review`), using the same audit script as the other car review: per-object topology,
materials, textures, dimensions and Workbench renders from four angles.

## Technical results

| Car | Objects | Triangles | Materials | Textures | Non-manifold | Zero-area | Size L×W×H (m) |
|---|---:|---:|---:|---:|---:|---:|---|
| Ford GT 2017 | 196 | 270,069 | 137 | 34 | 15 | 4 | 4.77 × 2.21 × 1.13 |
| Koenigsegg Jesko Attack | 122 | 445,310 | 40 | 19 | 2 | 169 | 4.63 × 2.22 × 1.22 |
| Lamborghini Revuelto | 108 | 339,329 | 48 | 31 | 18,704 | 0 | 4.70 × 2.16 × 1.34 |
| McLaren P1 | 556 | 971,803 | 184 | 102 | 30,140 | 98 | 4.59 × 2.14 × 1.53 |
| Ferrari FXX-K Evo | 145 | 257,363 | 96 | 44 | 168 | 319 | 4.77 × 2.06 × 1.15 |
| Pagani Huayra BC | 116 | 289,899 | 60 | 27 | 6 | 212 | 4.73 × 2.28 × 1.16 |
| Bugatti Divo | 172 | 279,774 | 114 | 36 | 136 | 15 | 4.63 × 2.19 × 1.20 |
| Aston Martin Valhalla | 179 | 826,516 | 46 | 17 | 0 | 548 | 4.77 × 2.02 × 1.23 |
| Rimac Nevera Time Attack | 56 | 223,396 | 43 | 24 | 0 | 0 | 4.74 × 2.21 × 1.19 |
| Mercedes-AMG One | 177 | 524,038 | 77 | 31 | 7 | 16 | 4.76 × 2.18 × 1.24 |
| Ferrari SF90 XX | 127 | 251,378 | 67 | 37 | 0 | 3 | 4.84 × 2.30 × 1.21 |
| Lamborghini Huracán STO | 99 | 311,733 | 39 | 24 | 75 | 170 | 4.50 × 2.23 × 1.21 |
| McLaren Senna | 107 | 312,159 | 46 | 22 | 10 | 5 | 4.70 × 2.14 × 1.20 |
| Lotus Evija | 117 | 242,224 | 70 | 33 | 28 | 8 | 4.46 × 2.03 × 1.14 |
| Pagani Utopia | 77 | 360,819 | 51 | 33 | 0 | 5 | 4.63 × 2.33 × 1.18 |

Observations:

- All 15 are genuine high-detail game meshes at real-world scale, with UVs on every object,
  packed DDS textures (alcantara, carbon, brake discs, tyre sidewalls) and separate wheel parts
  (`wheel_lf_0…N` etc., 16–90 pieces per car). Surfaces are clean; the renders read as the real cars.
- Only the GTA "hi" mesh is present. There are no lower LODs in the scenes. Every car is 10–40×
  over the game's LOD0 budget (15–25k triangles) and 5–20× over the material budget (4–7 per car).
- Revuelto and P1 report tens of thousands of non-manifold edges. For the P1 this is the agent's
  "derived display" copy sitting on top of the preserved source meshes (both are in the file, hence
  972k triangles and geometry 0.33 m below ground). For the Revuelto two body shells overlap. Both
  need cleanup before any reduction.
- Wheel pivots exist as GTA bone positions, not as `Wheel_FL/FR/RL/RR` empties; `CockpitCamera`
  and `Exhaust` anchors are absent. Materials are numbered (`material_81`), not named for the paint
  contract.

## Provenance (from the agent's own manifest and listings)

| Origin of the underlying model | Cars |
|---|---|
| Forza Horizon 3/4/5, Forza 7, Turn10 | Ford GT, McLaren P1, Mercedes-AMG One, McLaren Senna, Bugatti Divo (interior) |
| CSR2 (NaturalMotion) | Revuelto, Divo, SF90 XX, Lotus Evija, Pagani Utopia |
| Project Cars | Pagani Huayra BC |
| Racing Master | Jesko Attack |
| Unidentified TurboSquid / Sketchfab / "fz4" | Valhalla, Nevera, FXX-K Evo |
| Not named | Huracán STO |

Permissions received: 0 of 15 (agent's manifest). The mod converters do not own these models;
they are extracted from other commercial games. No converter can grant a licence to them, and the
original publishers do not license them to third parties. On top of that, every car carries a
manufacturer's trademarked name and protected design.

## Verdict

- **Quality**: excellent. These are the best-looking cars on the machine by a wide margin.
- **Usability in the game**: none, on two independent grounds. Legally, the geometry is ripped from
  Forza, CSR2 and Project Cars and the brands are trademarked, so there is no path to a store release
  even with modder consent. Technically, they would still need a full retopology to mobile budgets,
  material merging, LODs and the prefab contract, which is roughly the same effort as modelling
  fictional cars from scratch.
- **Legitimate use**: private visual reference for proportion, stance, panel lines and wheel design
  when building the fictional Redline roster. Keep them where they are, outside the repo and out of
  every build. Do not copy any mesh, texture or badge into `Assets/`.

## Recommendation

Use them as the reference sheets they were downloaded to be. Put the modelling effort into original
fictional cars in the same idiom (the hand-built Kestrel master is the right example), one hero per
class, then vary trim, aero and wheels per car. If the goal is to license real cars, that is a
manufacturer licensing conversation, not a modder one, and it is a separate budget item.
