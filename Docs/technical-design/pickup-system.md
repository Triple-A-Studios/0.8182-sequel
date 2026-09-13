# Pickup System

`HealthPickup.cs` (`Assets/_Project/Scripts/Pickup/HealthPickup.cs`), namespace `Opoint8182.Pickup`.

## Collect and gone (Milestone 3 Pass 4)

Folds the standing backlog item asking for a restore-interface counterpart to [`IDamageDealer`](common.md) — health pickups to recover from bad crashes/obstacle hits, per the design doc. Implements [`IRestorer`](common.md#irestorer-milestone-3-pass-4): a `Restore` property backed by `m_restoreAmount`, firing `Restored` from `OnTriggerEnter` on any Player-tagged collision — same trigger-based shape as [`BirdHazard`](obstacle-system.md#tier-2-small-hazards--birds-milestone-3-pass-2), since a pickup should be flown through, not physically blocked.

Unlike every hazard/obstacle so far, a pickup is single-use: it calls `Destroy(gameObject)` right after firing `Restored`, so it can't be collected twice. This is the first `Destroy`-on-trigger component in the project — `Building` destroys on a solid `OnCollisionEnter`, `ObstacleBuilding`/`BirdHazard` never destroy at all.

## Wiring

`Restored` is consumed by [`HealthSystem`](health-system.md#health-pickups-milestone-3-pass-4) once a `HealthPickup` instance is dropped into `m_restorerBehaviours` in the Editor — same opt-in array pattern as every other multi-source wiring in the project. Not wired into `FuelSystem` or `ScoreSystem` — a health pickup restores health only, and (like every hazard/obstacle) gives no score.

## Prefab

`HealthPickup.prefab` (`Assets/_Project/Prefabs/Pickups/`) mirrors `Bird_Small.prefab`'s root+Visual hierarchy: small trigger `BoxCollider` (`{1.5, 1.5, 1.5}`), placeholder material `Mat_HealthPickup.mat` (green tint, same shader-reuse convention as every other placeholder material in the project).
