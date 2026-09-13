# Current State

## Snapshot
- **Last landed:** Pass 3 — Combo chain scoring, stepping multiplier (`4101335`).
- **Now:** Milestone "Obstacles, combos, recovery" (MVP phase) in progress. Branch: `milestone/obstacles-combo-recovery`.
- **Next pass:** Pass 4 — Health pickups + restore interface.
- **Known issues to check next session:** see `progress.md` backlog — Inspector debug fields not refreshing live, and a design inconsistency around mistimed tough-building hits (doesn't destroy/score, isn't wired into fuel/combo penalties) needing a decision before it's implemented.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

## Milestone passes: Obstacles, combos, recovery

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `560c21a` | Large obstacle buildings (avoid-not-crash, fuel/health penalty, distinct visual) + generalize Fuel/Health source wiring to multi-source |
| 2 | Verified — Complete | `d25d86a` | Small hazards (birds) — lower-penalty reflex/dodge obstacle |
| 3 | Verified — Complete | `4101335` | Combo chain scoring — crash multiple buildings in quick succession for bonus/multiplier |
| 4 | Not started | | Health pickups + restore interface — generalize `IDamageDealer` into a matching restore-interface for fuel/health pickups (folds backlog item) |

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
