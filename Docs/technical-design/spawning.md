# Spawning

`Assets/_Project/Scripts/Spawning/` (`SpawnManager.cs`, `SpawnedEntity.cs`, `SpawnEvents.cs`), namespace `Opoint8182.Spawning`.

## Procedural spawner (Milestone: Difficulty ramp, Pass 1)

Replaces the previously hand-placed `Building_Normal`/`Building_Tough`/`Obstacle_Large`/`Bird_Small`/`HealthPickup` instances in `Prototype.unity` with runtime spawning ahead of the plane along +Z, and distance-based culling once an instance falls behind it. `SpawnManager` is a bare scene `GameObject` singleton (`GenericSingleton<SpawnManager>`), matching [`GameManager`](../technical-design/common.md)'s pattern — no `Prefabs/Managers` folder exists in this project, so this is deliberately not a prefab.

Zero changes were needed to `Building`, `ObstacleBuilding`, `BirdHazard`, or `HealthPickup` — all five spawn targets are self-contained (no `Awake`-time dependency lookups beyond what's already baked into the prefab), so the spawner only needs to `Instantiate` them at a computed position. `CombatEvents` also didn't need any changes.

## Spawn scheduling: distance-based, not timer-based

`SpawnManager.Update()` tracks the plane's `transform.position.z` and spawns the next entry once `planeZ + m_spawnAheadDistance >= m_nextSpawnZ`, advancing `m_nextSpawnZ` by `m_spawnInterval ± m_spawnIntervalJitter` each time (a `while` loop, capped at `k_MaxSpawnsPerFrame`, so a frame hitch that covers more than one interval's worth of distance doesn't leave spawning permanently behind). Distance-based rather than `Time.deltaTime`-based scheduling means density scales naturally with boost speed — a boosted plane covers ground faster and encounters obstacles sooner in wall-clock time, but spacing stays even in world-space, which is what "density" should mean for a difficulty ramp.

This also gives Pass 2 (difficulty ramp curve) a free distance tracker: `SpawnManager.DistanceTraveled` (`plane.position.z - m_startZ`) exists specifically so Pass 2 doesn't need to add one.

## Despawn: manager-owned list scan, not per-instance polling

`SpawnManager` holds `List<SpawnedEntity> m_activeEntities` and walks it once per frame in the same `Update()` that handles spawn scheduling — `CullBehindPlane` destroys anything whose `transform.position.z` has fallen more than `m_despawnBehindDistance` behind the plane. This centralizes the "culled vs. destroyed by its own hit logic" distinction in exactly one place: a `Building` that self-destroys on hit is detected via `entity == null` on the next scan and simply dropped from the list (no event); an entity that falls behind unhit goes through `SpawnEvents.RaiseEntityCulled` before being destroyed. `SpawnedEntity` itself stays a dumb marker component with no `Update()` of its own.

## `SpawnedEntity` marker + `SpawnEvents`

Every spawned instance gets a runtime-`AddComponent`'d `SpawnedEntity` (not baked into the prefabs) carrying a `SpawnKind` enum (`BuildingNormal`/`BuildingTough`/`ObstacleLarge`/`BirdSmall`/`HealthPickup`) set via `Initialize(kind)` at spawn time. This lets `SpawnManager` track/cull without `GetComponent<Building>/<ObstacleBuilding>/<BirdHazard>` type-switching.

`SpawnEvents.EntityCulled` (`Action<SpawnedEntity>`) mirrors `CombatEvents`'s static-bus pattern and fires **only** from the cull path — never for a hit-destroy. This means `EntityCulled` already means "this instance was never hit" by construction, which is exactly the "a building existed and wasn't hit" signal Pass 3 (folds backlog #7) needs. Confirmed: Pass 3's [`ScoreSystem`](score-system.md#combo-multiplier) subscribes directly and filters for `SpawnKind.BuildingNormal`/`BuildingTough` — no changes needed to this file's own code.

## Type/position selection (Pass 1 placeholder)

`m_spawnables` is a `[Serializable] struct { GameObject Prefab; SpawnKind Kind; float VerticalMin; float VerticalMax; AnimationCurve WeightCurve; bool ClampProgressAtRampCap }[]` array, five entries wired in the Inspector (the four crash/obstacle types plus `HealthPickup`). Selection is weighted (see [Difficulty ramp](#difficulty-ramp-pass-2) below) — Pass 1's uniform placeholder is gone. Lateral (X) spread is a shared manager-wide `m_lateralRange`, replacing the old single-lane (`x=0`) hand-placed layout.

**Vertical range is per-entry, not manager-wide.** `Building_Normal`/`Building_Tough`/`Obstacle_Large` have a base pivot and need to sit on the ground (`VerticalMin`/`VerticalMax` both `0`); `Bird_Small`/`HealthPickup` are small, centered-pivot, and need to float clear of the ground (`5`-`15` / `2`-`15`). A single shared range can't satisfy both: pinned near `0` it buries the small floating types in the ground, raised above `0` it floats the ground-pivot buildings in midair. This was caught during Pass 1 manual verification and fixed before commit — the original design (a single manager-level `m_verticalMin`/`m_verticalMax`) didn't account for the pivot-convention difference between building-scale and hazard-scale prefabs.

## Difficulty ramp (Pass 2, staggered per-entry in Pass 6)

`SpawnManager.DifficultyProgress01` is `Mathf.Clamp01(DistanceTraveled / m_difficultyRampDistance)` — the manager-wide ramp reaches full difficulty at `m_difficultyRampDistance` (default `3000f`) and holds there for the rest of the run, per the milestone's "increase over the length of a run" scope (not unbounded forever-escalation). This value still drives `ComputeSpawnInterval` (the density/spawn-rate axis) directly.

**One shared weight-sampling mechanism covers both the capped hazard/building types and `HealthPickup`'s uncapped decay, with no `if (kind == ...)` branching.** Pass 2 sampled every entry against the same shared `DifficultyProgress01`/raw-progress; Pass 6 (backlog #17 — see [design-doc.md](../design-doc.md)'s "Difficulty ramp — engagement levers" parking lot) generalized this to a **per-entry effective ramp distance**, so different stats can plateau at different distances instead of all freezing into one steady state together:
```csharp
private float ComputeWeight(SpawnableEntry entry)
{
    var rampDistance = entry.RampDistanceOverride > 0f ? entry.RampDistanceOverride : m_difficultyRampDistance;
    var rawProgress = rampDistance > 0f ? DistanceTraveled / rampDistance : 1f;
    var sampleT = entry.ClampProgressAtRampCap ? Mathf.Clamp01(rawProgress) : rawProgress;
    return Mathf.Max(0f, entry.WeightCurve.Evaluate(sampleT));
}
```
`RampDistanceOverride` (`0` = fall back to `m_difficultyRampDistance`) is what makes the staggering possible. `BuildingNormal`/`BuildingTough` set it to `1800` — the tough/normal ratio locks into its final mix well before density (still keyed to the global `3000`) finishes ramping, matching the design-doc's "ratio caps earliest, density a bit later" ordering. `ObstacleLarge`/`BirdSmall` are left at `0` (unchanged, plateau with density at `3000`). `HealthPickup` also needs no override: `ClampProgressAtRampCap = false` was already sampling on ever-growing raw progress against the global base, decaying to its floor at raw progress `2.0` (distance `6000`) — already the latest-plateauing stat, this pass just made the underlying mechanism generic instead of that being an accidental side effect of the uncapped-sampling special case.

Default curves (all keys given Linear tangent mode explicitly, to avoid Auto-tangent overshoot dipping below 0 and getting silently floored by the `Mathf.Max(0f, ...)` above):

| Entry | `ClampProgressAtRampCap` | `RampDistanceOverride` | Keyframes |
|---|---|---|---|
| BuildingNormal | `true` | `1800` | `(0, 10) → (1, 3)` |
| BuildingTough | `true` | `1800` | `(0, 0) → (1, 6)` |
| ObstacleLarge | `true` | `0` (→3000) | `(0, 2) → (1, 5)` |
| BirdSmall | `true` | `0` (→3000) | `(0, 2) → (1, 5)` |
| HealthPickup | `false` | `0` (→3000, unclamped) | `(0, 3), (1, 3), (2, 0.5)` |

**Weighted pick** (`TryPickWeightedEntry`) replaces Pass 1's uniform `Random.Range`: sums `ComputeWeight` across all 5 entries, rolls `Random.Range(0, total)`, walks a cumulative sum to find the picked entry. On an all-zero-weight edge case (misconfigured curves during tuning) the spawn is skipped outright rather than falling back to uniform — `m_nextSpawnZ` already advanced, so nothing hangs, it just under-spawns until curves are fixed.

**Spawn density** also ramps: `m_spawnInterval` (a flat float in Pass 1) is now `m_spawnIntervalCurve` (`AnimationCurve`, default `(0, 18) → (1, 8)`), sampled at the same clamped `DifficultyProgress01` via `ComputeSpawnInterval()` — deliberately still tied to the global `m_difficultyRampDistance`, not staggered by this pass. `m_spawnIntervalJitter` is still an unchanged additive term on top, but the advance is now floored (`Mathf.Max(1f, advance)`) — a flat `18 ± 6` interval could never go non-positive, but a curve legitimately shrinking toward `8` combined with jitter could, which would walk `m_nextSpawnZ` backward and spam spawns at the same spot every frame (bounded only by `k_MaxSpawnsPerFrame`).

**Known gotcha, will recur if `SpawnableEntry` gains more fields later:** struct array elements don't pick up C# field-initializer defaults on deserialization (Unity zero-inits them) — only top-level `MonoBehaviour` fields (like `m_difficultyRampDistance`/`m_spawnIntervalCurve`) reliably get their C# defaults on an already-existing component instance. Every field Pass 1, Pass 2, and now Pass 6 added to `SpawnableEntry` (`m_verticalMin`/`m_verticalMax`/`m_weightCurve`/`m_clampProgressAtRampCap`/`m_rampDistanceOverride`) needed explicit authoring on the array elements that actually need a non-default value after the code change landed — an unauthored `AnimationCurve` field in particular degrades silently (empty curve, `Evaluate` returns `0`, entry never gets picked) rather than erroring, so it's easy to miss without explicitly verifying keyframes post-wiring. `m_rampDistanceOverride`'s zero-init is harmless by design (it's the "use the global default" sentinel), which is why only `BuildingNormal`/`BuildingTough` needed an explicit value hand-authored — the other three entries correctly stay at `0` by doing nothing.

**CLI note:** the Unity Pipeline bridge's `set_serialized_field` command does not support `AnimationCurve` as a JSON value (`COMMAND_FAILED: unsupported type 'AnimationCurve'`) — wiring the 5 curves used the CLI's `eval_file` command running raw `SerializedProperty` C# instead (`FindPropertyRelative("m_weightCurve").animationCurveValue = ...`). That eval context wraps submitted code as a method body, not a top-level script — `using` directives at the top of the file fail to parse (fully-qualify types instead, e.g. `UnityEngine.AnimationCurve`) and the result value needs an explicit `return expr;` as the final statement.

## Chunk/pattern-based spawning (Pass 6)

Resolves the other half of backlog #17: `SpawnAt` used to treat every scheduled spawn event as one fresh, memoryless weighted roll — no clustering, no authored arrangement, nothing a flat probability table couldn't already produce, so a skilled player eventually pattern-matches the steady state (design-doc.md's parking lot, "Add pattern/chunk-based spawning" lever).

Two new nested private structs alongside `SpawnableEntry`: `SpawnChunkSlot` (`ZOffset` from the chunk's anchor point, `SpawnableIndex` — `-1` means "weighted roll," `>= 0` means a fixed authored index into `m_spawnables`) and `SpawnChunk` (`MinDistance` gate, `Slots[]`). `m_chunks` is a new top-level serialized array, same direct-on-the-component authoring style as `m_spawnables` — no `ScriptableObject` introduced, consistent with the rest of this file's config.

`TryPickChunk` filters `m_chunks` to those with `MinDistance <= DistanceTraveled` and picks uniformly among the eligible set — this is the "pool grows with distance" lever: early game only the always-eligible `"Solo"` chunk (`MinDistance: 0`, one slot, `SpawnableIndex: -1`) is available, so early spawning is indistinguishable from Pass 2's plain independent roll.

`SpawnAt` was replaced by `SpawnChunkAt(anchorZ) : float`, called once per scheduled spawn event from `Update()`'s existing distance-based loop. It places every slot in the picked chunk (`PlaceEntry`, the old `SpawnAt` body extracted so it's reusable per-slot) at `anchorZ + slot.ZOffset`, and returns the chunk's max `ZOffset` so the caller adds it on top of the normal `ComputeSpawnInterval() + jitter` gap — a multi-slot chunk's own length doesn't eat into the next spawn event's spacing, preserving the distance-based-not-timer-based scheduling invariant above. If no chunk is eligible at all (e.g. `m_chunks` left unauthored), it falls back to exactly Pass 2's single independent roll rather than spawning nothing — a defensive fallback, not the normal path, since the authored `"Solo"` chunk is always eligible.

Starter content (placeholder/tunable, freely retunable since fully data-driven): `"Solo"` (`MinDistance 0`, reproduces Pass 2 behavior), `"ToughPair"` (`MinDistance 1200`, two fixed slots — `BuildingTough` then `BuildingNormal` 30 units later — an authored pattern), `"GauntletMixed"` (`MinDistance 2500`, three slots — `ObstacleLarge`, `BirdSmall`, then a weighted-roll slot — mixing authored structure with random fill for later-game complexity).

**Checked for compatibility, no changes needed:** [`ScoreSystem.HandleEntityCulled`](score-system.md#combo-multiplier) reacts per-`SpawnedEntity` to `SpawnEvents.EntityCulled`, independent of how many buildings a single spawn event placed — a chunk placing two buildings close together (`"ToughPair"`) is safe, each culls and applies its own missed-building combo penalty independently, no shared-state assumption to break.

## Known constraint for future pooling

`BirdHazard.Awake()` captures its patrol origin (`m_startPosition`) once at spawn time — correct for this pass's fresh `Instantiate`-per-spawn approach (each bird naturally gets the right patrol origin), but would need a public reset method added to `BirdHazard` before any future pass could reuse pooled instances instead of `Destroy`+`Instantiate`. Not needed yet; flagged for whoever reaches for pooling later.
