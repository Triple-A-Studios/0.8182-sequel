# Leaderboard

`LeaderboardService` (`Assets/_Project/Scripts/Leaderboard/LeaderboardService.cs`, namespace `Opoint8182.Leaderboard`) is a thin wrapper over Unity Leaderboards (UGS) with anonymous sign-in, serving two boards: a **weekly** board (resets Monday 00:00 UTC, archived) and an **all-time** board. Backend choice and the reasoning behind it are in [design-doc.md](../design-doc.md) (Alpha section, "Leaderboards & daily attempts"); packages in [dependencies.md](dependencies.md). The weekly + all-time rework (backlog #28) landed in Daily play structure Passes 2-3 (the leaderboard screen's archive browsing and the Last week's results screen are Pass 4).

## Service

A `PersistentSingleton<LeaderboardService>` (`TripleA.Utils.Singletons`) on the `Bootstrap` scene object next to `BootstrapLoader`, so UGS init + anonymous sign-in start at boot, in parallel with the scene loads. Consumers use `LeaderboardService.TryGetInstance()` (never `.Instance`, which would auto-create a stand-in): a scene opened without Bootstrap gets `null` and Game Over shows "Leaderboard unavailable" instead of throwing. One caveat: the singleton base destroys the whole GameObject if a duplicate instance appears, and that object also holds `BootstrapLoader` — fine today because Bootstrap loads once.

- `State` (`Idle`/`Initializing`/`Ready`/`Failed`) + `StateChanged`. `EnsureReadyAsync()` is idempotent, concurrent callers share one attempt, and a `Failed` state retries on the next call.
- Never throws to callers: failures come back as `Success == false`.
- **UGS ranks are zero-based**; the service adds 1 for display.
- Everything is `Task`-based on the main thread (no `Task.Run`/`.Wait()`/`.Result`), which is what WebGL needs.
- **Offline fast-fail:** every call returns failure immediately when `Application.internetReachability == NotReachable`, so the UI never waits on a hanging request.
- **Request timeout:** each request races `m_requestTimeoutSeconds` (default 10 s, sign-in included) and returns failure if the timer wins. The timer is PrimeTween's `Tween.Delay` (unscaled time), not `Task.Delay`: `Task.Delay` is backed by a `System.Threading` timer that doesn't work on WebGL, while PrimeTween runs off its own update loop with no threads. The abandoned request keeps running, so nothing it would write (stored bests, caches) is written from inside the request: the caller writes only after a result that beat the timer. If the timeout hits while UGS init is still in flight, the service drops the stuck init task and goes `Failed`, so the next call starts a fresh init (unverified: how UGS reacts to a second `InitializeAsync` while the first is hung).

## Boards

Created in the Unity dashboard (the project has no deployment package, so boards cannot be created from code); the ids are the serialized `m_weeklyBoardId` / `m_allTimeBoardId` on the Bootstrap scene object (a stale serialized value overrides a code default, so change both).

| Board | Id | Config |
|---|---|---|
| Weekly | `weekly_scores` | High to Low, Best Score, reset Monday 00:00 UTC, archive on reset |
| All-time | `alltime_scores` | High to Low, Best Score, never resets |

The old `daily_scores` board is no longer used by any code (delete it in the dashboard). Score submission is client-side via the SDK; Cloud Code validation is Daily play structure Pass 7 (design-doc.md).

## Submission (Pass 3b)

`SubmitScoreAsync(int score)` returns a `SubmitResult` (`Success`, 1-based `Rank`, `Score`, `Trend`, `Skipped`). Scores of zero or less are not submitted.

1. Read the weekly version info (`GetWeeklyVersionsAsync`, cached). The stored weekly best is only valid for the live version ID: a different ID (a new week) counts as no stored best. If the versions call fails the stored values are trusted (the server keeps the best anyway).
2. A board is only sent the score when it **beats the stored best** for that board (`ShouldSubmit`); the server's keep-best would ignore a lower score, so this only saves calls.
3. The weekly board is sent first and its result is the one returned. If it fails the result is `Failed` and **nothing stored changes**, so a retry (Game Over's retry button) sends it again.
4. The all-time board is sent best-effort in the background (the run's result does not wait for it); a failure only means the next qualifying run sends it again.
5. After a successful submit the best stored is the **server's best** from the response (so a wiped local best self-corrects), and the cached live-board reads are dropped.

**A run that does not beat the weekly best is `Skipped`:** no call is made, `Rank` and `Score` are the last known weekly ones (rank 0 if there are none) and the trend is `Same`. Game Over shows that rank alone (and nothing when it is 0). A practice run never submits: `GameOverUI` skips it (`GameManager.IsPracticeRun`) until the attempts API (Pass 5).

**Rank trend:** `Compare(hasPrevious, previousRank, newRank)` (pure, static): `Up` for a first entry in the version or a lower rank number, otherwise `Same`. A worse rank is never reported as a drop (UX rules): `RankTrend` has no `Down`.

**Stored values** (`PlayerPrefs`, deliberately soft: clearing storage only costs a redundant submit): `lb_weekly_best`, `lb_weekly_version`, `lb_weekly_rank`, `lb_alltime_best`. The old daily-board keys `lb_last_rank` / `lb_last_utc_date` are deleted on startup.

## Reads and cache (Pass 3a)

- `GetTopScoresAsync(LeaderboardBoard board, int limit = 10)` returns a `TopScoresResult` (`Success`, `Rows`, `Self`, `Total`): the top `limit` rows of a live board, the player's own row when it is outside them (an extra `GetPlayerScoreAsync`; only `EntryNotFound`, code 27009, means "no entry", any other error fails the read), and the board's entry count (`Total`, 0 when empty). Own rows are detected by comparing `PlayerId` to `AuthenticationService.Instance.PlayerId`. `GetTopScoresAsync(limit)` is the weekly shorthand.
- **Cache:** a successful result is reused for 90 s per (board, limit), concurrent callers share one in-flight task, a failure is never cached, and `InvalidateLiveBoards()` (called after a successful submit) drops it. No polling anywhere.
- `GetWeeklyVersionsAsync()` returns `WeeklyVersions` (`LiveVersionId`, `NextResetUtc`, `TotalArchived`, `Archived`: up to `ArchivedWeeksLimit` = 4 `ArchivedWeek { Id, StartUtc, EndUtc }`, newest first) from one `GetVersionsAsync` call, reused until `NextResetUtc` passes (then fetched again, which is how a week rolling over mid-session is noticed); a failure is not cached. Archived reads (own entry, top scores per week) and the results flow are Pass 4.

## Weekly + all-time spike results (Daily play structure, Pass 2)

Run 2026-10-25 against the real boards. The spike was a throwaway script run through the Unity CLI in play mode.

- **`GetVersionsAsync(id, new GetVersionsOptions { Limit = 4 })`** returns, in one call: `versionId` (the live version), `nextReset` (weekly only; absent on all-time), `totalArchivedVersions` (absent while 0) and `results`, newest first, each `{ id, start, end }`. `Limit` is honoured as a display limit. Version IDs are 23-digit timestamp strings.
- **Archiving works.** A dashboard Reset (manual, same as the scheduled one) closes the live version into `results` immediately, and the new live version starts empty. `nextReset` read `2026-10-26T00:00:00Z`, which confirmed the schedule; the real scheduled reset on 2026-10-26 then archived a version as expected.
- **The no-entry case is a `LeaderboardsException`, `Reason == EntryNotFound`, `ErrorCode` 27009**, for all three of: the live `GetPlayerScoreAsync` after a reset, `GetVersionPlayerScoreAsync` on an archive the player never entered, and `GetVersionPlayerRangeAsync` on the same. It is expected, not an error to log.
- **`GetVersionScoresAsync` on an empty archive** returns `results: []` with `total` absent (read as 0); on a populated one it carries `total` (the "of N pilots" number).
- **Archived own entry** (`GetVersionPlayerScoreAsync`) returns `rank` (zero-based, add 1), `score`, `playerName`, plus the version `{ id, start, end }`.
- **A malformed version ID** throws `LeaderboardsException` with `Reason == InvalidArgument`, `ErrorCode` 1004, so only pass IDs that came from `GetVersionsAsync`.
- **Costs per call for the budget:** one `GetVersionsAsync` covers the reset check, the reset label and the browse list; each archived week costs 2 calls (own entry + top scores); the player-range call is not needed for the screens as designed.

## Anonymous identity

UGS anonymous sign-in stores its token in local storage (PlayerPrefs / IndexedDB on WebGL). Clearing site data creates a brand-new player with no score history, and the stored bests above are lost with it. In dev, Unity's WebGL Build And Run serves each build from a new localhost port, which is a new origin and so a new player every rebuild — a dev-only artifact. Verified working on Android and WebGL in the Live leaderboard spike; sign-in, submit and fetch all succeeded.

## UI

See the Game Over rank readout (with Retry on a failed submit) under [ui.md](ui.md#game-over) and the leaderboard screen under [ui.md](ui.md#main-menu-and-its-screens): its WEEKLY (this week) and ALL-TIME tabs read `GetTopScoresAsync` for the two boards; past weeks are still sample data until Pass 4. The display-name field is deferred to the Player profile & run stats milestone (the UGS auto-generated name is used). The "Last week's results" screen (S11) exists as a UI shell: Pass 4 supplies the real data by implementing `ILastWeekResultsSource` and assigning it to `LaunchFlowHost.ResultsSource` (see [ui.md](ui.md#launch-flow)).
