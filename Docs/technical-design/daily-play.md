# Daily Play (Daily play structure, Passes 1, 5 and 6)

The healthy-usage structure from the design doc ([Leaderboards & daily attempts](../design-doc.md#leaderboards--daily-attempts-decided-2026-10-07)): **3 leaderboard attempts per UTC day** (at most 21 a week, unused ones never bank) and a separate **practice mode capped at 30 minutes a day**. This file covers the local service that counts them, how a run starts, restarts and ends, and how the menu and HUD read it. The boards those attempts feed are in [leaderboard.md](leaderboard.md); the pause popup and QUIT TO MENU are in [game-flow.md](game-flow.md).

## `DailyPlay` (Pass 5a)

`Assets/_Project/Scripts/Daily/DailyPlay.cs`, namespace `Opoint8182.Daily`. A **static class** like `SettingsService`: no scene object, so it works from Bootstrap, the menu, a standalone game scene and the galleries, and it survives every game scene reload (restart, return to menu).

- **State** (`PlayerPrefs`, prefix `daily.`): the UTC day key, attempts used, practice seconds used, and the "don't ask again" day. Everything resets when the day key changes, so nothing banks by construction. The day key never goes backwards (the highest day seen is kept), so setting the device clock back does not hand attempts back; setting it forward only starts a fresh day. This is a **soft cap**: a device clock can still be moved forward, and clearing storage resets it. Server time is Pass 7.
- **Clock seam:** `IDailyClock { UtcNow }`, settable through `DailyPlay.Clock` (`SystemDailyClock` by default, `ManualDailyClock` for checks and tools). Reads stay synchronous and cached so Pass 7 can add an async refresh without touching callers.
- **Read:** `ATTEMPTS_MAX` (3), `AttemptsUsed` / `AttemptsLeft`, `PRACTICE_CAP_SECONDS` (1800), `PracticeUsedSeconds` / `PracticeRemainingSeconds`, `NextResetUtc` (the next UTC midnight), `DontAskAgainToday` (get / set), `Changed` (raised when a run begins or ends, on a new day and on a reset, not on practice ticks).
- **Run state** (static, in memory): `Mode` (`None` / `Competitive` / `Practice`), `CurrentAttemptNumber` (1 to 3, 0 for practice), `LastMode` (kept after the run ends so the menu comes back on the same tab), `IsPracticeRun`, `RunCountsAsAttempt` (true for competitive and for a run with no recorded start, i.e. a standalone game scene, so testing there still submits).
- **`CanBeginRun(mode)` / `BeginRun(mode)`** return a `BeginRunResult` (`Started`, `NoAttemptsLeft`, `NoPracticeTimeLeft`). `BeginRun` **spends the attempt at run start** (after the menu's confirm): a kill, a quit or an abandoned run all count, so no leave hook is needed. `EndRun()` commits practice time and clears the mode.
- **Practice time:** `TickPractice(dt)` is called from `GameManager.Update` only while Playing and the run is active (`IsPlaying` is false while paused and through the resume countdown), with `min(unscaledDeltaTime, 0.5 s)` so a stalled frame cannot burn the day. Written every 10 s of practice and on `Flush` (run end, `EndRun`, focus loss, quit). **The cap is soft:** a running practice run finishes; only the next practice `BeginRun` is refused.
- **Testing helpers:** `ResetToday()` and the menu item **Tools > Daily Play > Reset Today's Attempts And Practice Time** (`Scripts/Editor/DailyPlayMenu.cs`) delete only the `daily.*` keys; `ReloadFromPrefs()` drops the in-memory copy.

## Run lifecycle

| Step | What happens |
|---|---|
| Menu PLAY (Pass 5b) | A locked PLAY (no attempts, or no practice time) does nothing. Competitive opens the attempt-confirm popup unless `DontAskAgainToday`; LET'S FLY (or practice PLAY) calls `BeginRun(mode)` **before** `GameManager.EnterPlayingState()` so the HUD builds with the right mode; a refused `BeginRun` (a UTC-midnight race) just refreshes the menu. |
| In run | `GameManager.IsPracticeRun` = the Inspector override (`m_isPracticeRun`, for a standalone scene) OR `DailyPlay.IsPracticeRun`. `HudPresenter.OnViewReady` passes the mode and `CurrentAttemptNumber` to `HudView.SetRun`: PRACTICE tag, pause popup "COMPETITIVE · ATTEMPT n/3", quit note "Quitting still burns attempt n of 3." |
| Run ends | Counters are flushed. `GameOverUI` submits to the boards only when the run is not practice (`IsPracticeRun`); see [leaderboard.md](leaderboard.md). |
| RESTART RUN / RETRY (Pass 5c) | `GameManager.RestartRun()` returns a bool: for a run started from the menu it calls `BeginRun(DailyPlay.Mode)` first. A competitive restart **spends the next attempt**; a practice restart is free until the cap. A refused restart changes nothing and does not reload; the caller shows `DailyPlayText.RefusedLine` ("NEW ATTEMPTS IN 5h" / "PRACTICE RETURNS IN 5h", whole hours, never minutes). `CanRestartRun()` lets the pause popup grey RESTART RUN in advance. A standalone scene (mode `None`) restarts as before. In a competitive run the pause popup asks first (`hud-restart-popup`: "RESTART THIS RUN?", "This attempt is lost without submitting your score. The new run uses attempt n of 3."); Game Over RETRY does not (that attempt has already ended). |
| QUIT TO MENU / MAIN MENU | `BootstrapLoader.ReturnToMenu()` (Pass 1): calls `DailyPlay.EndRun()`, then reloads `Prototype` into the menu state behind the loading cover and loads `MainMenu` additively. The attempt stays spent. The menu opens on the tab of `LastMode`. |

`GameManager.RunStarted` is missed on the restart path (the new `GameManager` starts in Playing during `Awake`, before anything subscribes), so attempts are spent at the call sites above, never from that event.

## Return to menu and the loading cover (Pass 1)

`BootstrapLoader` (the object that survives scene loads) owns `ReturnToMenu()` and `RestartRun()`: both freeze the old scene (`timeScale` 0, `Sfx.StopAll()`), show the loading screen as a cover (`LaunchFlowHost.ShowCover`, no launch minimum, no rotate gate; a serialized `Min Cover Seconds`, default 1 s, with the bar filling over it), set `GameManager.ManagedByBootstrap` (true for the menu, false for a restart so the new `GameManager` starts in Playing), single-mode load `Prototype` (and `MainMenu` additively for the menu), wait two frames so the UI has drawn, and hide the cover. The new `GameManager.Awake` resets `timeScale` and the audio pause. A failed load clears the flag and restores the clock; a second call while one is running is ignored. No state machine change: the reload bypasses `GameManager`'s FSM, so `Paused` to `MainMenu` needs no transition.

## Menu (Pass 5b)

`MainMenuController.RefreshDaily()` fills the status chip (`n/3` and pips), the practice clock (`mm:ss`), the PLAY pips and sub-label and the confirm popup body from the service, on build, on `DailyPlay.Changed` and every 30 s (reading the counters applies a new UTC day). The wording lives in `DailyPlayText` (pure, testable): "3 ATTEMPTS LEFT TODAY" / "1 ATTEMPT LEFT TODAY", "28:14 LEFT TODAY". With nothing left PLAY gets `menu-play--locked` (opacity only) and is inert, the sub-label reads "NEW ATTEMPTS IN 7h" or "PRACTICE RETURNS IN 7h" (whole hours, never minutes, never red, no countdown styling), and the other tab stays usable. FLY NOW on an empty leaderboard goes through the same guard.

## Game Over (Pass 6)

`GameOverRow.Derive` (`Scripts/UI/GameOver/GameOverRow.cs`, pure) reads `DailyPlay` when the run ends and returns the button row, the note under the primary button, whether the primary and PRACTICE are usable, and the rank chip status. Attempts are already spent at run start, so Game Over only reads; it consumes nothing.

| Run | Primary | Note | Secondary | Rank status |
|---|---|---|---|---|
| Competitive, attempts left | RETRY | "2 attempts left today" | MAIN MENU, LEADERBOARD | "THIS WEEK · 2 LEFT TODAY" |
| Competitive, last attempt used | LEADERBOARD | "new attempts in 7h" | MAIN MENU, PRACTICE (greyed with no practice time) | "THIS WEEK · DONE TODAY" |
| Practice, time left | RETRY | "28:14 left today" | MAIN MENU, LEADERBOARD | none (practice has no rank) |
| Practice, time gone | RETRY greyed | "practice returns in 7h" | MAIN MENU, LEADERBOARD | none |
| Standalone scene (mode `None`) | RETRY | none | MAIN MENU, LEADERBOARD | none |

**BANK IT and RETRY NOW do not exist** (decided 2026-10-28). They came from the daily-board mockup ("save the other two attempts"); with the weekly keep-best board the best score is saved at death, an unused attempt never carries over, and the UX rules forbid banked attempts. Nothing is "placed well", so there is no such rule either.

- **PRACTICE** (`GameManager.StartPracticeRun`): `BeginRun(Practice)` (no attempt spent; refused when the day's practice time is gone, which shows "PRACTICE RETURNS IN Nh"), then the same reload as RESTART RUN. No confirm, practice never asks.
- **LEADERBOARD and the rank-chip tap** (`BootstrapLoader.ReturnToLeaderboard`): `ReturnToMenu` with `MainMenuController.OpenLeaderboardOnLoad` set. The flag is set after the loader's re-entry guard (an ignored call leaves no stale request), consumed once when the menu builds (`m_router.Show(leaderboard)`, so back is the menu) and cleared if the reload fails. A standalone scene has no menu and shows the toast.
- The up arrow on the rank chip (rank improved) is the only "change" shown; there is no delta vs yesterday.

## Open / later

- **Server-enforced day and attempts, Cloud Code validation of scores:** Pass 7.
- Share to socials: Pass 8. Device checks of all of this: Pass 9.
