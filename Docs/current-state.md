# Current State

## Snapshot
- **Last landed:** Pass 7 — Camera effects/juice (`1ce5f3d`). Milestone "Feel polish" (MVP phase) **complete** — all 7 scoped passes landed. **MVP phase complete** (design-doc.md: "MVP is considered done here"). See [progress.md](progress.md) for the frozen pass table.
- **Now:** Alpha phase, Milestone "MVP bug fix + backlog + engine upgrade" — Pass 2: Android bug fixes. See [plan.md](plan.md#phase-alpha) for milestone scope.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Game Over screen bug reproduces in-Editor (confirmed 2026-09-27) — not Android-only, debuggable directly without ADB. Main-menu button press effect and the joystick 360-roll edge case still need on-device/ADB confirmation.

## Milestone: MVP bug fix + backlog + engine upgrade — passes

| Pass | Status | Notes / Features | Target |
|---|---|---|---|
| 1 | Verified — Complete (`5e8eaa3`) | Editor upgraded 6000.0.79f1 → 6000.6.3f1 (tech stream, not 6.3 LTS — developer's explicit call despite the shorter support window); compiles clean, played in Play Mode, no regressions found | Evaluate Unity 6.6 upgrade, decide go/no-go, record decision |
| 2 | In Progress | Game Over screen bug reproduces in-Editor too (not Android-only) | Android bug fixes: Game Over screen not appearing on run-end, main-menu stub-button press effect not visible, one-off unrecoverable 360° roll when finger slides off-screen mid-steer |
| 3 | Not Started | — | Touch UI QoL: Panel Settings target resolution/aspect ratio update (off outdated 1920x1080/16:9; visual polish itself deferred to Alpha UI/art pass) + floating joystick rework (origin follows first-touch position; screen-half setting deferred to a later pass) |
| 4 | Not Started | — | Low-altitude warning (folds backlog #22) — soft/hard ground-proximity bound mirroring `LateralSystem` |
| 5 | Not Started | — | Healing building type (folds backlog #15) — new building variant firing `CombatEvents.RaiseRestored` instead of `RaiseCrashed` |

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md#phase-alpha) for what comes after this milestone.
