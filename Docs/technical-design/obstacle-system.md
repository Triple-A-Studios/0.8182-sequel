# Obstacle System

`ObstacleBuilding.cs` (`Assets/_Project/Scripts/Obstacle/ObstacleBuilding.cs`), namespace `Opoint8182.Obstacle`.

## Avoid, not crash (Milestone 3 Pass 1)

Large obstacle buildings are the first tier of the design doc's two-tier obstacle system (§3/§7) — a permanent, avoid-not-crash hazard rather than a crashable target. `ObstacleBuilding` is a separate component from [`Building`](building-crash-system.md), not a third `BuildingType`, since the behavior shape is different enough that branching it into `Building.OnCollisionEnter` would tangle unrelated logic:

- No weak point, no crash-quality calculation.
- No `Crashed` event, no `Destroy` — hitting it doesn't break it or refuel the plane; it's a solid, permanent obstacle the player is meant to dodge.
- Implements [`IDamageDealer`](common.md) (reuses the existing interface, no changes needed to it): a `Damage` property backed by `m_hitDamage`, firing `DamageDealt` from `OnCollisionEnter` on any Player-tagged collision.

Deliberately never wired into [`ScoreSystem`](score-system.md) (which only listens to `Building.Crashed`) — obstacle hits give no score, matching the design doc's "gives no reward."

## Wiring

`DamageDealt` is consumed by both [`HealthSystem`](health-system.md) and [`FuelSystem`](fuel-system.md#multi-source-wiring-milestone-3-pass-1) once an `ObstacleBuilding` instance is dropped into their respective source arrays in the Editor — matching the design doc's "hitting one costs fuel/health." Both systems already support multiple simultaneous `IDamageDealer` sources as of this pass, so an obstacle instance coexists with `Building_Normal`/`Building_Tough` without any further code change.

## Prefab

`Obstacle_Large.prefab` (`Assets/_Project/Prefabs/Obstacles/`) mirrors `Building_Normal.prefab`'s root+Visual hierarchy but drops the `Weakpoint` child. Placeholder material `Mat_Obstacle_Large.mat` — same shader setup as the building materials, distinct hazard-orange `_BaseColor`, no new art asset (same convention as `Mat_Building_Tough.mat`). Collider/mesh scale and `m_hitDamage` are tuned directly in the Editor as placeholder values get playtested — check the prefab itself for current numbers rather than assuming this doc's original targets.

## Tier 2: small hazards — birds (Milestone 3 Pass 2)

`BirdHazard.cs` (`Assets/_Project/Scripts/Obstacle/BirdHazard.cs`), same namespace as `ObstacleBuilding`. The design doc's "more of a reflex/dodge challenge" tier: lower `m_hitDamage` than `ObstacleBuilding`, and two things that make it read as a dodge challenge rather than a smaller static obstacle:

- **It patrols.** A simple, cheap back-and-forth drift along world X between `±m_patrolDistance` of its spawn position at `m_moveSpeed` — no navmesh/pathing needed for a prototype hazard. Static obstacles can be planned around from a distance; a moving one forces the player to react as they approach.
- **It's a trigger, not solid.** `m_IsTrigger: 1` on its `BoxCollider`, damage fired from `OnTriggerEnter` instead of `OnCollisionEnter` — the plane passes through rather than physically bouncing off it, which would be a disproportionate physical response to something bird-sized.

Otherwise identical shape to `ObstacleBuilding`: implements `IDamageDealer`, no `Destroy`, never wired into `ScoreSystem`. Plugs into the same `FuelSystem.m_damageSourceBehaviours`/`HealthSystem.m_damageDealerBehaviours` arrays `ObstacleBuilding` uses — Pass 1's multi-source generalization needed no further changes for this pass. Prefab: `Bird_Small.prefab`, much smaller than `Obstacle_Large` (`{1, 1, 1.5}` collider/mesh scale), placeholder material `Mat_Bird.mat` (dark tint, same shader-reuse convention).
