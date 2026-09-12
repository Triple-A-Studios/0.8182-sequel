# Current State

## Snapshot
- **Last landed:** Pass 2 — Single crashable building + weak-point detection (`01fc0ad`)
- **Now:** Pass 3 — Fuel system (drain + crash-precision refuel + run-end at zero)
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

## Milestone: Core loop, bare minimum — passes

| Pass | Status | Notes / Features | Target |
|---|---|---|---|
| 1 | Verified — Complete (`b482138`) | Lateral steer is direct sideways velocity, not yaw rotation — roll stays cosmetic-only on the visual child; camera follow is bare-minimum, tightening/juice deferred to Feel Polish milestone | Plane movement + joystick steering |
| 2 | Verified — Complete (`01fc0ad`) | See [Building & Crash System](technical-design/building-crash-system.md) — `Building.Crashed` event has no consumer yet, Pass 3 subscribes to it | Single crashable building + weak-point detection |
| 3 | Not Started | — | Fuel system (drain + crash-precision refuel + run-end at zero) |
| 4 | Not Started | — | Minimal UI (fuel gauge) |

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
