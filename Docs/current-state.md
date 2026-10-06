# Current State

## Snapshot
- **Last landed:** Pass 5 — Heal building type (`2f1a1ac`). Milestone "MVP bug fix + backlog + engine upgrade" (Alpha phase) **complete** — all passes landed (Unity 6.6 upgrade, Android bug fixes, PanelRenderer migration, floating joystick + 2400x1080 reference resolution, low-altitude warning, heal building). See [progress.md](progress.md) for the frozen pass table.
- **Now:** Milestone "Live leaderboard" (Alpha), branch `milestone/leaderboard`. Scoped incrementally; backlog check done — no open items folded in (none relate to leaderboard). See [plan.md](plan.md#phase-alpha).

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `91d5dca` | Backend research — **Unity Leaderboards (UGS), anonymous sign-in** chosen over LootLocker; recorded in `design-doc.md`/`plan.md` (docs only, no code) |
| 2 | Verified — Complete (uncommitted) | — | UGS spike — anonymous sign-in, submit a random score, fetch top 10. Throwaway `LeaderboardSpike` in `Bootstrap.unity` (IMGUI on-screen log + Re-run button), no UI/gameplay coupling |

**Pass 2 spike results:**
- **WebGL and Android both work** — sign-in, submit and fetch succeeded on each; entries confirmed on the dashboard leaderboard. Packages: `com.unity.services.authentication` 3.8.0, `com.unity.services.leaderboards` 2.3.4 (Core 1.18.0 transitive).
- **Anonymous ID is tied to browser storage on WebGL:** same-origin reload keeps the player; clearing site data creates a new player (expected, see design-doc.md). Build And Run serves from a new localhost port per build, so each rebuilt dev build is a new origin and a new player — a dev-only artifact, production has one stable URL.
- Android ID persistence across kill + relaunch was not explicitly checked.
- Orphaned test entries (one per identity) remain on the `spike_test` leaderboard.

Next: scope Pass 3 (real `LeaderboardService`, `RunEnded` hookup, top-scores UI, name handling) — the throwaway `LeaderboardSpike` gets deleted then.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Editor is Unity 6000.6.3f1. All UI scripts use `PanelRenderer`, not `UIDocument`. Pass 3's resolution/floating-joystick change was verified in-Editor only — worth an on-device Android check.

See [progress.md](progress.md) for the full project history and [plan.md](plan.md#phase-alpha) for what comes next.
