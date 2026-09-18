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

`SpawnEvents.EntityCulled` (`Action<SpawnedEntity>`) mirrors `CombatEvents`'s static-bus pattern and fires **only** from the cull path — never for a hit-destroy. This means `EntityCulled` already means "this instance was never hit" by construction, which is exactly the "a building existed and wasn't hit" signal Pass 3 (folds backlog #7) needs — that pass should just subscribe and filter for `SpawnKind.BuildingNormal`/`BuildingTough`, no new plumbing required.

## Type/position selection (Pass 1 placeholder)

`m_spawnables` is a `[Serializable] struct { GameObject Prefab; SpawnKind Kind; float VerticalMin; float VerticalMax }[]` array, five entries wired in the Inspector (the four crash/obstacle types plus `HealthPickup`), picked with uniform `Random.Range(0, length)` — density/type-mix is explicitly **not** the finished ramp design, just a placeholder Pass 2 will build on (likely by adding a `Weight` field to the same struct rather than restructuring, so `HealthPickup` can be tuned to a lower spawn weight than the hazard types once that pass lands). Lateral (X) spread is a shared manager-wide `m_lateralRange`, replacing the old single-lane (`x=0`) hand-placed layout.

**Vertical range is per-entry, not manager-wide.** `Building_Normal`/`Building_Tough`/`Obstacle_Large` have a base pivot and need to sit on the ground (`VerticalMin`/`VerticalMax` both `0`); `Bird_Small`/`HealthPickup` are small, centered-pivot, and need to float clear of the ground (`5`-`15` / `2`-`15`). A single shared range can't satisfy both: pinned near `0` it buries the small floating types in the ground, raised above `0` it floats the ground-pivot buildings in midair. This was caught during Pass 1 manual verification and fixed before commit — the original design (a single manager-level `m_verticalMin`/`m_verticalMax`) didn't account for the pivot-convention difference between building-scale and hazard-scale prefabs.

## Known constraint for future pooling

`BirdHazard.Awake()` captures its patrol origin (`m_startPosition`) once at spawn time — correct for this pass's fresh `Instantiate`-per-spawn approach (each bird naturally gets the right patrol origin), but would need a public reset method added to `BirdHazard` before any future pass could reuse pooled instances instead of `Destroy`+`Instantiate`. Not needed yet; flagged for whoever reaches for pooling later.
