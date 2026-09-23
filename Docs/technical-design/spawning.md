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

## Difficulty ramp (Pass 2)

`SpawnManager.DifficultyProgress01` is `Mathf.Clamp01(DistanceTraveled / m_difficultyRampDistance)` — the ramp reaches full difficulty at `m_difficultyRampDistance` (default `3000f`) and holds there for the rest of the run, per the milestone's "increase over the length of a run" scope (not unbounded forever-escalation).

**One shared weight-sampling mechanism covers both the capped hazard/building types and `HealthPickup`'s uncapped decay, with no `if (kind == ...)` branching:**
```csharp
private float ComputeWeight(SpawnableEntry entry)
{
    var rawProgress = m_difficultyRampDistance > 0f ? DistanceTraveled / m_difficultyRampDistance : 1f;
    var sampleT = entry.ClampProgressAtRampCap ? DifficultyProgress01 : rawProgress;
    return Mathf.Max(0f, entry.WeightCurve.Evaluate(sampleT));
}
```
`BuildingNormal`/`BuildingTough`/`ObstacleLarge`/`BirdSmall` set `ClampProgressAtRampCap = true` — their curves are authored over `t: 0..1` and hold the `t=1` value forever once the ramp caps. `HealthPickup` sets it `false` — its curve is authored flat (`t=0..1`, weight `3`), then decays down to a floor (`t=1→2`, `3→0.5`) on `rawProgress`, which keeps growing past `1` even after the main ramp caps. `AnimationCurve`'s default `postWrapMode` (`ClampForever`) holds that `0.5` floor for any `t` beyond the last keyframe — this is the entire mechanism for "pickups get rare but never fully vanish," authored as data, not code.

Default curves (all keys given Linear tangent mode explicitly, to avoid Auto-tangent overshoot dipping below 0 and getting silently floored by the `Mathf.Max(0f, ...)` above):

| Entry | `ClampProgressAtRampCap` | Keyframes |
|---|---|---|
| BuildingNormal | `true` | `(0, 10) → (1, 3)` |
| BuildingTough | `true` | `(0, 0) → (1, 6)` |
| ObstacleLarge | `true` | `(0, 2) → (1, 5)` |
| BirdSmall | `true` | `(0, 2) → (1, 5)` |
| HealthPickup | `false` | `(0, 3), (1, 3), (2, 0.5)` |

**Weighted pick** (`TryPickWeightedEntry`) replaces Pass 1's uniform `Random.Range`: sums `ComputeWeight` across all 5 entries, rolls `Random.Range(0, total)`, walks a cumulative sum to find the picked entry. On an all-zero-weight edge case (misconfigured curves during tuning) the spawn is skipped outright rather than falling back to uniform — `m_nextSpawnZ` already advanced, so nothing hangs, it just under-spawns until curves are fixed.

**Spawn density** also ramps: `m_spawnInterval` (a flat float in Pass 1) is now `m_spawnIntervalCurve` (`AnimationCurve`, default `(0, 18) → (1, 8)`), sampled at the same clamped `DifficultyProgress01` via `ComputeSpawnInterval()`. `m_spawnIntervalJitter` is still an unchanged additive term on top, but the advance is now floored (`Mathf.Max(1f, advance)`) — a flat `18 ± 6` interval could never go non-positive, but a curve legitimately shrinking toward `8` combined with jitter could, which would walk `m_nextSpawnZ` backward and spam spawns at the same spot every frame (bounded only by `k_MaxSpawnsPerFrame`).

**Known gotcha, will recur if `SpawnableEntry` gains more fields later:** struct array elements don't pick up C# field-initializer defaults on deserialization (Unity zero-inits them) — only top-level `MonoBehaviour` fields (like `m_difficultyRampDistance`/`m_spawnIntervalCurve`) reliably get their C# defaults on an already-existing component instance. Every field Pass 1 and Pass 2 added to `SpawnableEntry` itself (`m_verticalMin`/`m_verticalMax`/`m_weightCurve`/`m_clampProgressAtRampCap`) needed explicit live-Editor authoring on all 5 existing array elements after the code change landed — an unauthored `AnimationCurve` field in particular degrades silently (empty curve, `Evaluate` returns `0`, entry never gets picked) rather than erroring, so it's easy to miss without explicitly verifying keyframes post-wiring.

**CLI note:** the Unity Pipeline bridge's `set_serialized_field` command does not support `AnimationCurve` as a JSON value (`COMMAND_FAILED: unsupported type 'AnimationCurve'`) — wiring the 5 curves used the CLI's `eval_file` command running raw `SerializedProperty` C# instead (`FindPropertyRelative("m_weightCurve").animationCurveValue = ...`). That eval context wraps submitted code as a method body, not a top-level script — `using` directives at the top of the file fail to parse (fully-qualify types instead, e.g. `UnityEngine.AnimationCurve`) and the result value needs an explicit `return expr;` as the final statement.

## Known constraint for future pooling

`BirdHazard.Awake()` captures its patrol origin (`m_startPosition`) once at spawn time — correct for this pass's fresh `Instantiate`-per-spawn approach (each bird naturally gets the right patrol origin), but would need a public reset method added to `BirdHazard` before any future pass could reuse pooled instances instead of `Destroy`+`Instantiate`. Not needed yet; flagged for whoever reaches for pooling later.
