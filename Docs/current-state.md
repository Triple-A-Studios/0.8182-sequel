# Current State

## Snapshot
- **Last landed:** Pass 3 — Health resource (`968025f`)
- **Now:** Pass 4 — Score
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

## Milestone: Building types + scoring — passes

| Pass | Status | Notes / Features | Target |
|---|---|---|---|
| 1 | Verified — Complete (`9145687`) | Backlog: plane momentarily stops on crash before `Destroy` (both colliders) — see [backlog](progress.md#backlog) | Tough building type (variant requiring hold-boost-to-break, distinct visual, own weak point) |
| 2 | Verified — Complete (`2692147`) | Reused existing `Sprint` action for boost input | Boost mechanic (input, faster fuel drain, harder steering while active) |
| 3 | Verified — Complete (`968025f`) | Damage moved to `Building` via new `IDamageDealer` interface, not hardcoded on `HealthSystem` | Health resource (second stat; lost on mistimed tough-building crash; death behavior — instant vs. gradual — TBD by playtest feel) |
| 4 | Not Started | — | Score (simple visible score, increments per crash, shown in UI) |

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
