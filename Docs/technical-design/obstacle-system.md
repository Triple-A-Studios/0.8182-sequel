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

`Obstacle_Large.prefab` (`Assets/_Project/Prefabs/Obstacles/`) mirrors `Building_Normal.prefab`'s root+Visual hierarchy but drops the `Weakpoint` child and uses a bigger `BoxCollider`/mesh scale (`{10, 14, 10}` vs. buildings' `{6, 8, 6}`) to read as a larger, more imposing hazard. Placeholder material `Mat_Obstacle_Large.mat` — same shader setup as the building materials, distinct hazard-orange `_BaseColor`, no new art asset (same convention as `Mat_Building_Tough.mat`).
