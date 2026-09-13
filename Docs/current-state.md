# Current State

## Snapshot
- **Last landed:** Pass 1 — Large obstacle buildings + multi-source Fuel/Health wiring (`560c21a`).
- **Now:** Milestone "Obstacles, combos, recovery" (MVP phase) in progress. Branch: `milestone/obstacles-combo-recovery`.
- **Next pass:** Pass 2 — Small hazards (birds), lower-penalty reflex/dodge obstacle.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

## Milestone passes: Obstacles, combos, recovery

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `560c21a` | Large obstacle buildings (avoid-not-crash, fuel/health penalty, distinct visual) + generalize Fuel/Health source wiring to multi-source |
| 2 | Not started | | Small hazards (birds) — lower-penalty reflex/dodge obstacle |
| 3 | Not started | | Combo chain scoring — crash multiple buildings in quick succession for bonus/multiplier |
| 4 | Not started | | Health pickups + restore interface — generalize `IDamageDealer` into a matching restore-interface for fuel/health pickups (folds backlog item) |

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
