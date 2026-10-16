# Current State

## Snapshot
- **Last landed:** Milestone "UI/art pass" (Alpha) — **complete** and merged into `main` (8 passes, 2026-10-07 to 2026-10-17). Every mockup screen exists as UI Toolkit UI; systems that belong to later milestones show flagged static placeholder data. Verified in the Editor, on an Android phone (Realme 8s 5G, native build) and in WebGL in the phone's browser. Frozen pass table with commits: [progress.md](progress.md#milestone-uiart-pass--completed). What the UI is and how it works: [technical-design/ui.md](technical-design/ui.md).
- **Now:** milestone **Pause, settings & audio** (Alpha) is scoped (2026-10-17), branch `milestone/pause-settings-audio` cut from `main`. No pass started. Next step: Pass 1.
- **Branch:** `milestone/pause-settings-audio` (nothing pushed).
- **Pass status** (full scope per pass in [plan.md](plan.md#phase-alpha)):

| Pass | Status | Commit | What |
|---|---|---|---|
| 1 | Not started | — | Settings store: PlayerPrefs-backed `SettingsService` replaces `UiSettingsState`'s session-only backing |
| 2 | Not started | — | Functional pause: `GameManager` `PausedState` (sole orchestrator, owns `timeScale`), countdown, RESTART, QUIT TO MENU stub with practice / competitive branching |
| 3 | Not started | — | Auto-pause on focus loss + in-session portrait prompt |
| 4 | Not started | — | Audio system: mixer (Master + SFX), `AudioManager`, generated stand-in clips, SFX wiring. No music |
| 5 | Not started | — | Haptics service + screen-shake slider into `CameraJuice` |
| 6 | Not started | — | Joystick side / size / sensitivity + invert steering applied (folds backlog #25) |
| 7 | Not started | — | Hardening (Android + WebGL) and docs |

- **Decisions of record (this milestone):** pause is state-based through `GameManager`'s state machine, but the Paused state itself sets `Time.timeScale` to 0 (a hybrid: single orchestrator, no per-system freeze gating, since about 13 scripts tick on scaled time); no music or BGM, SFX only (design choice); stand-in clips are generated WAVs, swappable later with no code change; settings persist in PlayerPrefs, local only; QUIT TO MENU stays a stub toast (real return-to-menu belongs to Daily play structure), practice run goes straight to the stub action, competitive run shows a confirm popup that the attempt is lost without submitting the score.
- **Open items carried in:** backlog **#29** (retest the rotate-device gate on a plain page or the custom site). The "Placeholder data registry" in `ui.md` is the checklist for the milestones that make each stand-in real; `CreditsContent.cs` still holds a placeholder name / links / thanks for the developer to fill.
- **Before you test:** reload the open scene after script changes before entering Play Mode. Editor is Unity 6000.6.3f1 (the active build target is WebGL; switching re-imports). All UI scripts use `PanelRenderer`, not `UIDocument`. The leaderboard needs a linked Unity Cloud project and the `daily_scores` board (High to Low, Best Score, daily reset 00:00 UTC) until backlog #28 replaces it. A WebGL Build And Run uses a new localhost port per build, which looks like a new anonymous player each time (dev-only). Every WebGL build rewrites the tracked `Data/Plugins/lib_burst_generated.wasm`. The dev scenes in `Scenes/Dev` are not in Build Settings; their hotkeys are listed in `ui.md`.

## Decisions of record
The UI/art pass decisions are listed in [technical-design/ui.md](technical-design/ui.md#decisions-of-record-uiart-pass).

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md#phase-alpha) for what comes next.
