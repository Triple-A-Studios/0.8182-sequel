# Current State

## Snapshot
- **Last landed:** Pass 5 — Heal building type (`2f1a1ac`). Milestone "MVP bug fix + backlog + engine upgrade" (Alpha phase) **complete** — all passes landed (Unity 6.6 upgrade, Android bug fixes, PanelRenderer migration, floating joystick + 2400x1080 reference resolution, low-altitude warning, heal building). See [progress.md](progress.md) for the frozen pass table.
- **Now:** Milestone "Live leaderboard" (Alpha), branch `milestone/leaderboard`. Scoped incrementally; backlog check done — no open items folded in (none relate to leaderboard). See [plan.md](plan.md#phase-alpha).

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `91d5dca` | Backend research — **Unity Leaderboards (UGS), anonymous sign-in** chosen over LootLocker; recorded in `design-doc.md`/`plan.md` (docs only, no code) |
| 2 | Verified — Complete | `f890bab` | UGS spike — anonymous sign-in, submit a random score, fetch top 10. Throwaway `LeaderboardSpike` (IMGUI on-screen log + Re-run button), no UI/gameplay coupling. Spike deleted in Pass 3 |
| 3 | Verified in Editor — Android + WebGL checks deferred until after Pass 4 | `2b03eca` | Real `LeaderboardService` (daily Best Score board `daily_scores`), score submit on run end, rank + up/down trend on Game Over. See [leaderboard.md](technical-design/leaderboard.md) |
| 4 | Verified in Editor — Android + WebGL builds in progress (covers Pass 3 + 4 together) | — | Menu leaderboard tile opens a minimal Top-10 panel (top 10 + own row if outside, loading/empty/error + Retry) via `LeaderboardPanelUI`; Game Over failed-submit Retry button; offline fast-fail in `LeaderboardService` |

PrimeTween is now in the project (local tarball package `Packages/com.kyrylokuzyk.primetween.tgz` + installer under `Assets/Plugins/PrimeTween`); `LeaderboardService` uses `Tween.Delay` for a 10s request timeout (WebGL-safe; `Task.Delay` isn't). Timeout test: block UGS traffic (or throttle to a hang) and confirm the panel/Game Over fall into the error + Retry state after ~10s.

**Pass 4 test notes:** `MainMenu.unity` was edited as YAML (new `LeaderboardPanelUI` component on `MainMenuUI`) — open it in the Editor and check no "missing script". The leaderboard tile is no longer `stub-clickable` (no toast); it uses the new `bounce-clickable` class for the press bounce. Android + WebGL checks for Pass 3 and 4 together are still outstanding.

**Pass 3 setup + test notes:** create the `daily_scores` leaderboard in the dashboard first (High to Low, Best Score, daily reset 00:00 UTC) — without it every submit fails and Game Over shows "Leaderboard unavailable". `Bootstrap.unity` was edited as YAML (spike component swapped for `LeaderboardService`) — open it in the Editor once and check the component resolved (no "missing script"). Check ▲/▼ glyphs render on Android. Rank is zero-based in UGS; the UI adds 1 — verify against the dashboard.

**Pass 2 spike results:**
- **WebGL and Android both work** — sign-in, submit and fetch succeeded on each; entries confirmed on the dashboard leaderboard. Packages: `com.unity.services.authentication` 3.8.0, `com.unity.services.leaderboards` 2.3.4 (Core 1.18.0 transitive).
- **Anonymous ID is tied to browser storage on WebGL:** same-origin reload keeps the player; clearing site data creates a new player (expected, see design-doc.md). Build And Run serves from a new localhost port per build, so each rebuilt dev build is a new origin and a new player — a dev-only artifact, production has one stable URL.
- Android ID persistence across kill + relaunch was not explicitly checked.
- Orphaned test entries (one per identity) remain on the `spike_test` leaderboard.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode. Editor is Unity 6000.6.3f1. All UI scripts use `PanelRenderer`, not `UIDocument`. Pass 3's resolution/floating-joystick change was verified in-Editor only — worth an on-device Android check.

See [progress.md](progress.md) for the full project history and [plan.md](plan.md#phase-alpha) for what comes next.
