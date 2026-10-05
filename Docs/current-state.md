# Current State

## Snapshot
- **Last landed:** Pass 5 — Heal building type (`2f1a1ac`). Milestone "MVP bug fix + backlog + engine upgrade" (Alpha phase) **complete** — all passes landed (Unity 6.6 upgrade, Android bug fixes, PanelRenderer migration, floating joystick + 2400x1080 reference resolution, low-altitude warning, heal building). See [progress.md](progress.md) for the frozen pass table.
- **Now:** Milestone "Live leaderboard" (Alpha), branch `milestone/leaderboard`. Scoped incrementally; backlog check done — no open items folded in (none relate to leaderboard). See [plan.md](plan.md#phase-alpha).

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Done, awaiting verification | — | Backend research — **Unity Leaderboards (UGS), anonymous sign-in** chosen over LootLocker; recorded in `design-doc.md`/`plan.md` (docs only, no code) |

Pass 2 candidate (not yet confirmed): UGS Authentication + Leaderboards spike on WebGL and Android before building any UI — WebGL support is unconfirmed in docs. Remaining passes scoped after the developer confirms.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Editor is Unity 6000.6.3f1. All UI scripts use `PanelRenderer`, not `UIDocument`. Pass 3's resolution/floating-joystick change was verified in-Editor only — worth an on-device Android check.

See [progress.md](progress.md) for the full project history and [plan.md](plan.md#phase-alpha) for what comes next.
