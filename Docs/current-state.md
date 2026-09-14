# Current State

## Snapshot
- **Last landed:** Pass 5 — `PlayerManager` (`ee01e77`). Milestone "Core systems refactor" (MVP phase) complete — see [progress.md](progress.md).
- **Now:** Next milestone, "Difficulty ramp" (MVP phase), has no passes scoped yet — see [plan.md](plan.md#phase-mvp).
- **Known issues to check before scoping/starting next:** see `progress.md` backlog — plane momentarily stops on crash before `Destroy` (physics bug); combo-timer missing-building penalty (blocked on a building spawner that doesn't exist yet — likely relevant once difficulty ramp introduces spawning); Alchemy `[ShowInInspector]` debug fields not refreshing live in Play Mode (Inspector repaint issue, not a logic bug).
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
