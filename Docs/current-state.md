# Current State

## Snapshot
- **Last landed:** Pass 5 — Heal building type (`2f1a1ac`). Milestone "MVP bug fix + backlog + engine upgrade" (Alpha phase) **complete** — all passes landed (Unity 6.6 upgrade, Android bug fixes, PanelRenderer migration, floating joystick + 2400x1080 reference resolution, low-altitude warning, heal building). See [progress.md](progress.md) for the frozen pass table.
- **Now:** Next milestone in Alpha is "Live leaderboard" — no passes scoped yet. Backend choice (Unity Leaderboards vs a free third-party option, under the "no accounts, anonymous" constraint) is still unresolved research; check the backlog (open items #19–21, #23–25) before scoping. See [plan.md](plan.md#phase-alpha).

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Editor is Unity 6000.6.3f1. All UI scripts use `PanelRenderer`, not `UIDocument`. Pass 3's resolution/floating-joystick change was verified in-Editor only — worth an on-device Android check.

See [progress.md](progress.md) for the full project history and [plan.md](plan.md#phase-alpha) for what comes next.
