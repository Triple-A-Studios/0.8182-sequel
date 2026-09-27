# Current State

## Snapshot
- **Last landed:** Pass 7 — Camera effects/juice (`1ce5f3d`). Milestone "Feel polish" (MVP phase) **complete** — all 7 scoped passes landed. **MVP phase complete** (design-doc.md: "MVP is considered done here"). See [progress.md](progress.md) for the frozen pass table.
- **Now:** Alpha phase, Milestone "MVP bug fix + backlog + engine upgrade" — Pass 2.b/2.c: main-menu button press effect + joystick 360-roll, both need on-device/ADB logcat before diagnosing. See [plan.md](plan.md#phase-alpha) for milestone scope.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Game Over screen bug was Editor-reproducible (not Android-only) and is now fixed. The remaining two Pass 2 bugs haven't reproduced in-Editor yet — developer to connect device + pull `adb logcat` output for diagnosis.

## Milestone: MVP bug fix + backlog + engine upgrade — passes

| Pass | Status | Notes / Features | Target |
|---|---|---|---|
| 1 | Verified — Complete (`5e8eaa3`) | Editor upgraded 6000.0.79f1 → 6000.6.3f1 (tech stream, not 6.3 LTS — developer's explicit call despite the shorter support window); compiles clean, played in Play Mode, no regressions found | Evaluate Unity 6.6 upgrade, decide go/no-go, record decision |
| 2.a | Verified — Complete (`68473fc`) | Game Over screen not appearing on run-end via Bootstrap — root cause: `GameOverUI`'s `RunEnded` subscription lived only in `Start()`, but `m_hud` toggles active/inactive across the menu\<->play transition (re-firing OnEnable/OnDisable, never Start again), silently dropping the subscription after the first round-trip. Fixed: also subscribe in `OnEnable` once `m_gameManager` is cached | Android bug fix: Game Over screen not appearing on run-end |
| 2.b | Not Started | Needs ADB logcat repro | Android bug fix: main-menu stub-button press effect not visible |
| 2.c | Not Started | Needs ADB logcat repro; one-off, not yet reproduced | Android bug fix: unrecoverable 360° roll when finger slides off-screen mid-steer |
| 3 | Not Started | — | Touch UI QoL: Panel Settings target resolution/aspect ratio update (off outdated 1920x1080/16:9; visual polish itself deferred to Alpha UI/art pass) + floating joystick rework (origin follows first-touch position; screen-half setting deferred to a later pass) |
| 4 | Not Started | — | Low-altitude warning (folds backlog #22) — soft/hard ground-proximity bound mirroring `LateralSystem` |
| 5 | Not Started | — | Healing building type (folds backlog #15) — new building variant firing `CombatEvents.RaiseRestored` instead of `RaiseCrashed` |

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md#phase-alpha) for what comes after this milestone.
