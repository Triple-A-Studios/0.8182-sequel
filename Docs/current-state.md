# Current State

## Snapshot
- **Last landed:** Milestone "UI/art pass" (Alpha) — **complete** and merged into `main` (8 passes, 2026-10-07 to 2026-10-17). Every mockup screen exists as UI Toolkit UI; systems that belong to later milestones show flagged static placeholder data. Verified in the Editor, on an Android phone (Realme 8s 5G, native build) and in WebGL in the phone's browser. Frozen pass table with commits: [progress.md](progress.md#milestone-uiart-pass--completed). What the UI is and how it works: [technical-design/ui.md](technical-design/ui.md).
- **Now:** milestone **Pause, settings & audio** (Alpha) is scoped (2026-10-17), branch `milestone/pause-settings-audio` cut from `main`. Pass 1 landed; Pass 2a landed; Pass 2b landed; Pass 3 landed (device checks deferred to Pass 7); Pass 4a landed; Pass 4b landed; Pass 5 landed (Android vibration and the manifest build are device-checked in Pass 7); Pass 6 in progress.
- **Branch:** `milestone/pause-settings-audio` (nothing pushed).
- **Pass status** (full scope per pass in [plan.md](plan.md#phase-alpha)):

| Pass | Status | Commit | What |
|---|---|---|---|
| 1 | Verified — Complete | `3c7c54a` | Settings store: PlayerPrefs-backed `SettingsService` replaces `UiSettingsState` ([settings.md](technical-design/settings.md)) |
| 2a | Verified — Complete | `75394ff` | Core pause: `GameManager` `PausedState` (owns `timeScale`), `Pause()` / `Resume()`, hit-stop guard, shared `RestartRun()`, `HudView.SetPaused`, `HudPresenter` wiring (button, popup, 3-2-1 countdown, RESTART, Escape / back) |
| 2b | Verified — Complete | `b6ef40d` | QUIT TO MENU stub: `IsPracticeRun`, leave-run confirm popup (competitive only), toast stub that returns to the pause popup, mode line / note follow the flag |
| 3 | Verified in Editor (Android + WebGL checks deferred to Pass 7) | `af09727` | Auto-pause on focus loss (immediate, `GameManager`, off in Editor by default) + countdown guard + in-session portrait rotate overlay in the HUD |
| 4a | Verified — Complete | `a3da19e` | Audio foundation: generated placeholder WAVs, `SfxLibrary`, `AudioManager` (pool, priority, ducking), `Sfx` facade, Sound slider to `AudioListener.volume`, pause integration, UI / countdown / slider sounds. No mixer, no music |
| 4b | Verified — Complete | `8e16de0` | Gameplay SFX: `GameplaySfx` (crashes, hit, heal, pickup, combo, boost loop, warnings, run end, game over) |
| 5 | Verified in Editor (Android vibration + manifest build deferred to Pass 7) | `90cb47a` | Haptics service + screen-shake slider into `CameraJuice` |
| 6 | In progress | — | Joystick side / size / sensitivity + invert steering applied (folds backlog #25) |
| 7 | Not started | — | Hardening (Android + WebGL) and docs |

- **Decisions of record (this milestone):** pause is state-based through `GameManager`'s state machine, but the Paused state itself sets `Time.timeScale` to 0 (a hybrid: single orchestrator, no per-system freeze gating, since about 13 scripts tick on scaled time); no music or BGM, SFX only (design choice, permanent); no audio mixer: priority, voice stealing and ducking are code in `AudioManager`; stand-in clips are generated WAVs, swappable later with no code change; settings persist in PlayerPrefs, local only; QUIT TO MENU stays a stub toast (real return-to-menu belongs to Daily play structure), practice run goes straight to the stub action, competitive run shows a confirm popup that the attempt is lost without submitting the score.
- **Open items carried in:** none from the backlog (#29, the rotate gate, was verified on an Android phone 2026-10-17). The "Placeholder data registry" in `ui.md` is the checklist for the milestones that make each stand-in real; `CreditsContent.cs` still holds a placeholder name / links / thanks for the developer to fill.
- **Before you test:** reload the open scene after script changes before entering Play Mode. Editor is Unity 6000.6.3f1 (the active build target is WebGL; switching re-imports). All UI scripts use `PanelRenderer`, not `UIDocument`. The leaderboard needs a linked Unity Cloud project and the `daily_scores` board (High to Low, Best Score, daily reset 00:00 UTC) until backlog #28 replaces it. A WebGL Build And Run uses a new localhost port per build, which looks like a new anonymous player each time (dev-only). Every WebGL build rewrites the tracked `Data/Plugins/lib_burst_generated.wasm`. The dev scenes in `Scenes/Dev` are not in Build Settings; their hotkeys are listed in `ui.md`.

## Decisions of record
The UI/art pass decisions are listed in [technical-design/ui.md](technical-design/ui.md#decisions-of-record-uiart-pass).

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md#phase-alpha) for what comes next.
