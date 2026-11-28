# Planes (selectable plane models)

Built in 3D art pass Pass 1. Status: landed and verified in the Editor (flight, banking, weak-point crash feel, swapping between planes, saved choice). The Android / WebGL performance check is deferred until after the obstacles and pickups pass.

## Decisions of record
- **Planes are cosmetic only.** Every plane shares the root `CapsuleCollider` (radius 0.5, height 4, along Z), the `Rigidbody` and the `PlaneController` numbers on `Plane.prefab`. Scores stay comparable on the weekly and all-time boards and a shop plane is never an advantage. Per-plane gameplay stats are out of scope unless `design-doc.md` changes.
- **Planes are drawn at a similar size, not to real-world scale.** An airliner is not drawn bigger than a bomber or a fighter. The game is not a simulator and a bigger model must not mean a bigger hit shape, and the buildings are scaled against the airliner anyway. Each model is scaled to the common footprint below. The nose may stick out a little past the capsule on purpose (visible crash feel).

## Pieces (`Scripts/Planes`, namespace `Opoint8182`)
| Piece | Job |
|---|---|
| `PlaneDefinition` (ScriptableObject) | One plane: stable `Id` (saved on the device, never shown, never renamed once shipped), `DisplayName`, `VisualPrefab`, optional `Thumbnail`. `OnValidate` warns about colliders, a rigidbody, a size outside the footprint, or an off-centre pivot. Assets live in `Scripts/ScriptableObjects/PlaneDefinitions`. |
| `PlaneCatalog` (ScriptableObject, `Resources/PlaneCatalog.asset`) | Ordered list of definitions plus `DefaultId`. `TryGet(id)`, `Default` (default id, else the first plane). |
| `PlaneSelection` (static) | The equipped plane id, `PlayerPrefs` key `plane.equipped`. `Equipped`, `EquippedId`, `Equip(id)` (false for an unknown id), `Reset()`, `Changed`. A saved id missing from the catalog reads as the default. Writes straight to disk on a change. No ownership yet. |
| `PlaneVisualHost` (on the `Plane` root) | Instantiates the equipped plane's `VisualPrefab` under `Visual` (the object `PlaneController` banks and pitches) on enable, and swaps it on `PlaneSelection.Changed`. `PlaneVisualHost.Spawn(definition, parent)` is the one spawn path (strips colliders, rigidbodies and animators from the clone). |
| `PlaneCatalogBuilder` (Editor) | `Tools > Planes`: Create Or Update Catalog (keeps order, appends new, keeps the default id), Log Equipped, Equip Next Plane, Reset Equipped. |

`Visual` is empty in `Plane.prefab` at edit time: the model only appears in Play Mode.

## Art spec for a plane visual prefab
- Prefab root at the origin, the model a child, **pivot at the model's centre** (banking and pitching rotate around the `Visual` origin; keep `Visual` at 0 and offset the model inside its own prefab). Keep the centre within 0.5 of the origin.
- Nose along **+Z**, up is +Y.
- Footprint about 4 to 5 long (Z) and 5 to 6 wide (X); `OnValidate` warns outside 2-8 long and 3-10 wide.
- Meshes and materials only: no collider, rigidbody or animator (they are stripped at runtime anyway).
- URP Lit or Simple Lit materials, one or two per plane. The Airbus is about 3.5k vertices, 2k triangles, one material. WebGL and Android use the Mobile quality level (SRP Batcher on, dynamic batching off, one shadow cascade).

## Stills and the Profile portrait
(3D art pass, Pass 8.)
- **`Tools > Art > Render Art Stills`** (`ArtStillsBuilder`) renders into `Art/Textures/Stills`: one transparent 640 x 400 sprite per catalog plane (assigned as `PlaneDefinition.Thumbnail`), the crash effect (`Tex_CrashFx_ClassicBoom`, 256 x 256) and the loading key art (`Tex_KeyArt`, 1600 x 624: the default plane flying into a building's weak point, staged in the game scene for its sky, sun and fog; the scene is closed without saving, and must not be open when the tool runs). Run it again after a plane, building or effect changes. From the Unity CLI the call can report a time-out while the render still finishes.
- **`PlanePortraitRig`** (`Scripts/UI/Profile`): the Profile's turntable (24 degrees per second). Its own camera renders the equipped plane into a transparent 840 x 600 texture, far below the world, lit by the game scene's sun. `ProfileScreen.Open` creates it, `Close` releases it (also when the menu unloads for a run). Without it (the share card, which has no camera) the slot shows the plane's still.
- **Names:** the airliner's display name is **Bonk 747** (id `airbus`, asset `Plane_Airbus`, unchanged); `PlaneDefinition.Tagline` is the line on its shop card. The one crash effect is shown as **Classic Boom** (a constant in `ProfileScreen`, the still set in `Profile.uss`).
- Keep UI elements with background pictures free of `overflow: hidden` on rounded corners: in the off-screen share card render such an element came out plain white.

## Hook points for later
- **Beta shop:** an owned-planes set checked before `PlaneSelection.Equip`; `Thumbnail` for the cards; price data next to the definition.
- **Menu rooftop slot, Profile portrait and thumbnails, share card:** `PlaneVisualHost.Spawn` into a preview parent under a camera.
- **Server:** if the equipped plane is ever shown to other players, send `Id`.
