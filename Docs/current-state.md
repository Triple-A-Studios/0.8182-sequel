# Current State

## Snapshot
- **Last landed:** Pass 4 — Menu Top-10 panel + offline/error states (`005e412`). Milestone "Live leaderboard" (Alpha phase) **complete** — Unity Leaderboards (UGS) with anonymous sign-in, daily Best Score board, score submit + rank trend on Game Over, menu Top-10 panel, Retry/timeout handling. Verified in Editor, Android and WebGL. See [progress.md](progress.md) for the frozen pass table and [technical-design/leaderboard.md](technical-design/leaderboard.md) for the system doc.
- **Now:** Next milestone in Alpha is "UI/art pass" — no passes scoped yet. Check the backlog before scoping (open items #19–21, #23–26; #25 joystick settings and #26 WebGL ▲/▼ glyphs are already targeted at this milestone, and the display-name field was deferred here). See [plan.md](plan.md#phase-alpha).

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Editor is Unity 6000.6.3f1. All UI scripts use `PanelRenderer`, not `UIDocument`. The leaderboard needs a linked Unity Cloud project and the `daily_scores` board (High to Low, Best Score, daily reset 00:00 UTC). WebGL Build And Run uses a new localhost port per build, which looks like a new anonymous player each time (dev-only). `ProjectSettings/ProjectSettings.asset` currently has an uncommitted `serializedVersion` 30→28 downgrade — check what wrote it before committing that file.

See [progress.md](progress.md) for the full project history and [plan.md](plan.md#phase-alpha) for what comes next.
