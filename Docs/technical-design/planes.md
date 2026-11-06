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

## Hook points for later
- **Beta shop:** an owned-planes set checked before `PlaneSelection.Equip`; `Thumbnail` for the cards; price data next to the definition.
- **Menu rooftop slot, Profile portrait and thumbnails, share card:** `PlaneVisualHost.Spawn` into a preview parent under a camera.
- **Server:** if the equipped plane is ever shown to other players, send `Id`.
