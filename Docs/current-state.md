# Current State

## Snapshot
- **Last landed:** Pass 4 — Health pickups + `IRestorer` interface (`e9fce2d`). Milestone "Obstacles, combos, recovery" complete — see [progress.md](progress.md).
- **Now:** Next milestone, "Core systems refactor" (MVP phase), has no passes scoped yet — see [plan.md](plan.md#phase-mvp). No branch created for it yet.
- **Known issues to check before scoping/starting next:** see `progress.md` backlog — Inspector debug fields not refreshing live, and a design inconsistency around mistimed tough-building hits (doesn't destroy/score, isn't wired into fuel/combo penalties) needing a decision before it's implemented. Several other backlog rows are exactly what "Core systems refactor" is scoped to fix.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
