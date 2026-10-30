# Leaderboard

`LeaderboardService` (`Assets/_Project/Scripts/Leaderboard/LeaderboardService.cs`, namespace `Opoint8182.Leaderboard`) is a thin wrapper over Unity Leaderboards (UGS) with anonymous sign-in, serving two boards: a **weekly** board (resets Monday 00:00 UTC, archived) and an **all-time** board. Backend choice and the reasoning behind it are in [design-doc.md](../design-doc.md) (Alpha section, "Leaderboards & daily attempts"); packages in [dependencies.md](dependencies.md). The weekly + all-time rework (backlog #28) landed in Daily play structure Passes 2-3 (the leaderboard screen's archive browsing and the Last week's results screen are Pass 4). Since Pass 7 the client no longer writes scores: the server does (see [Submission](#submission-pass-3b-through-the-server-since-pass-7e) and [Server scripts](#server-scripts-daily-play-structure-pass-7)).

## Service

A `PersistentSingleton<LeaderboardService>` (`TripleA.Utils.Singletons`) on the `Bootstrap` scene object next to `BootstrapLoader`, so UGS init + anonymous sign-in start at boot, in parallel with the scene loads. Consumers use `LeaderboardService.TryGetInstance()` (never `.Instance`, which would auto-create a stand-in): a scene opened without Bootstrap gets `null` and Game Over shows "Leaderboard unavailable" instead of throwing. One caveat: the singleton base destroys the whole GameObject if a duplicate instance appears, and that object also holds `BootstrapLoader` — fine today because Bootstrap loads once.

- `State` (`Idle`/`Initializing`/`Ready`/`Failed`) + `StateChanged`. `EnsureReadyAsync()` is idempotent, concurrent callers share one attempt, and a `Failed` state retries on the next call.
- Never throws to callers: failures come back as `Success == false`.
- **UGS ranks are zero-based**; the service adds 1 for display.
- Everything is `Task`-based on the main thread (no `Task.Run`/`.Wait()`/`.Result`), which is what WebGL needs.
- **Offline fast-fail:** every call returns failure immediately when `Application.internetReachability == NotReachable`, so the UI never waits on a hanging request.
- **Request timeout:** each request races `m_requestTimeoutSeconds` (default 10 s, sign-in included) and returns failure if the timer wins. The timer is PrimeTween's `Tween.Delay` (unscaled time), not `Task.Delay`: `Task.Delay` is backed by a `System.Threading` timer that doesn't work on WebGL, while PrimeTween runs off its own update loop with no threads. The abandoned request keeps running, so nothing it would write (stored bests, caches) is written from inside the request: the caller writes only after a result that beat the timer. If the timeout hits while UGS init is still in flight, the service drops the stuck init task and goes `Failed`, so the next call starts a fresh init (unverified: how UGS reacts to a second `InitializeAsync` while the first is hung).

## Boards

Created in the Unity dashboard, once per environment (the game talks to the environment named by the serialized `m_environmentName` on the same object: `dev` while the server side is built, production at Pass 9); the ids are the serialized `m_weeklyBoardId` / `m_allTimeBoardId` on the Bootstrap scene object (a stale serialized value overrides a code default, so change both).

| Board | Id | Config |
|---|---|---|
| Weekly | `weekly_scores` | High to Low, Best Score, reset Monday 00:00 UTC, archive on reset |
| All-time | `alltime_scores` | High to Low, Best Score, never resets |

The old `daily_scores` board is no longer used by any code (delete it in the dashboard). Scores are written by the server, not the client (Submission and Server scripts below).

## Submission (Pass 3b, through the server since Pass 7e)

`SubmitScoreAsync(int score)` returns a `SubmitResult` (`Success`, 1-based `Rank`, `Score`, `Trend`, `Skipped`, `Failure`). Scores of zero or less are not submitted. The client does not write to the boards (the access policy denies it): the score goes through the `SubmitScore` script with the run's ticket (`DailyPlay.CurrentTicketId`, kept by `DailyPlay.BeginRunAsync`).

1. **No ticket** (a game scene opened on its own) is `Failure == Unavailable`; **offline** is a `Retryable` failure. Nothing is called in either case.
2. Read the weekly version info (`GetWeeklyVersionsAsync`, cached). The stored weekly best is only valid for the live version ID: a different ID (a new week) counts as no stored best. If the versions call fails the stored values are trusted (the server keeps the best anyway).
3. The score is only sent when it **beats the stored weekly best** (`ShouldSubmit`). This is the only client gate: the server writes both boards, keeps the best on each, and an all-time best is never below a week's, so one gate saves the calls. A gated run leaves its ticket unused.
4. `DailyServer.SubmitScoreAsync(ticketId, score)` sorts the reply (table below).
5. On success the stored weekly best, rank and version come from the reply, the all-time best too when the reply has it (so a wiped local best self-corrects), the cached live-board reads are dropped, and the trend is worked out as before.

| Server reply | `SubmitFailure` | Game Over shows |
|---|---|---|
| Offline, timed out, `BUSY`, `BOARD_ERROR`, `ERROR` | `Retryable` | "Couldn't submit score" with the chip's RETRY (the ticket is still valid, nothing stored changed) |
| `NO_TICKET`, `TICKET_USED`, `TICKET_EXPIRED`, `IMPLAUSIBLE` | `NotAccepted` | "Score not accepted", no retry |
| No ticket on the client | `Unavailable` | "Leaderboard unavailable", no retry |

**A run that does not beat the weekly best is `Skipped`:** no call is made, `Rank` and `Score` are the last known weekly ones (rank 0 if there are none) and the trend is `Same`. Game Over shows that rank alone (and nothing when it is 0). A practice run never submits: `GameOverUI` skips it (`GameManager.IsPracticeRun`).

**Rank trend:** `Compare(hasPrevious, previousRank, newRank)` (pure, static): `Up` for a first entry in the version or a lower rank number, otherwise `Same`. A worse rank is never reported as a drop (UX rules): `RankTrend` has no `Down`.

**Stored values** (`PlayerPrefs`, deliberately soft: clearing storage only costs a redundant submit): `lb_weekly_best`, `lb_weekly_version`, `lb_weekly_rank`, `lb_alltime_best`. The old daily-board keys `lb_last_rank` / `lb_last_utc_date` are deleted on startup.

## Server scripts (Daily play structure, Pass 7)

JavaScript Cloud Code scripts in `Assets/_Project/CloudCode/` (each self-contained: scripts deploy one file at a time, nothing is bundled) and the access policy `Access/LeaderboardsAccessControl.ac`. The client path is `LeaderboardService.CallServerAsync` (signs in first, fails fast offline, 10 s timeout, null on any failure) then `DailyServer` (typed replies, never throws) then `DailyPlay` / `LeaderboardService`.

**State** is ONE Cloud Save private custom item per player, `daily`, keyed only by the authenticated player id (`context.playerId`; nothing is keyed to the device): `{ day, used, ticket }` where `day` is the server UTC day number and `ticket` is `{ id, startedAtMs, used }` or null. Counter and ticket share the item, so one write lock guards both (a stale lock is a 409: a second `StartRun` at the same moment retries once and then sees the new count). A ticket survives the day roll (a run that crosses UTC midnight can still submit); the count does not.

| Script | Parameters | Does | Returns |
|---|---|---|---|
| `GetDailyState` | none | reads today's count on the server's day (nothing written) | `{ ok, attemptsUsed, attemptsLeft, attemptsMax, nowMs, nextResetMs }` |
| `StartRun` | none | spends one of the 3 attempts and writes a fresh ticket (an unused earlier ticket is replaced, so an abandoned run still counts and cannot be submitted) | `{ ok, ticketId, attemptNumber, attemptsLeft, nowMs, nextResetMs }` or `{ ok: false, code: NO_ATTEMPTS / BUSY / ERROR }` |
| `SubmitScore` | `ticketId` (String), `score` (Numeric) | checks the ticket and the score, marks the ticket used, writes both boards with the service token and reads each entry back (the add call returns an empty body) | `{ ok, weekly: { rank (1-based), score }, allTime: { score } or null }` or `{ ok: false, code: NO_TICKET / TICKET_USED / TICKET_EXPIRED / IMPLAUSIBLE / BUSY / BOARD_ERROR / ERROR }` |
| `DevSetState` | `used`, `dayOffset`, `ticketStartedAgoMs` (Numeric, optional) | sets the state for checks and the Tools > Daily Play reset item; refuses when `context.environmentName` is `production` | `{ ok, used, day, ticketId, nowMs }` or `{ ok: false, code: NOT_ALLOWED }` |

**`SubmitScore` checks, in order:** the score is a positive whole number within int range; the caller has a ticket with that id; it is unused; it is younger than `MAX_RUN_AGE_MS` (3 h); and `score <= MAX_SCORE_PER_SECOND (1500) * server elapsed seconds since the ticket + SCORE_SLACK (5000)` (elapsed time is the server's own, never client-reported). An implausible score burns the ticket, so the bound cannot be probed. The ticket is marked used with the lock before the boards are written; a weekly-board error restores it so the client's retry works, an all-time error only leaves that board unwritten. Every accepted submit logs `score` and `elapsedSec` (Cloud Code logs in the dashboard): tune the bound from those. The combo multiplier tops out at x4 and building values are small, so real runs sit far below it.

**Access policy.** `Deny` / `Write` / `Player` on `urn:ugs:leaderboards:*` (a Player token cannot write a score; reads and the scripts' service-token writes still work). Unity's documented narrower resource (`.../scores/players/*`) did not block client writes in the spike; the broad one does.

**Deploying** (Editor, Window > Services > Deployment, with the right environment selected; no CLI needed): reimport the scripts first (right-click > Reimport: the Editor loses the loaded scripts' names after a recompile and the deploy fails with "found duplicated"), then deploy the four scripts and the `.ac`. Script parameters live in each script's `.meta` (`Parameters:` with `ParameterType` 0 String / 1 Numeric and `Required`), not in the code: the dashboard and the Editor ignore `module.exports.params` here. The boards themselves are created in the dashboard, once per environment.

**Environments.** The Editor and the game use `dev` (the existing environment) while the server side is built: `LeaderboardService.m_environmentName` (serialized on the Bootstrap scene object) and the Editor's selected environment (`ProjectSettings/Packages/com.unity.services.core/Settings.json`). Go-live (Pass 9): recreate both boards in production, deploy the four scripts and the policy to production, set `m_environmentName` to production, then delete the leftover spike scripts, the `spike_counter` Cloud Save data and the test entries from production.

## Reads and cache (Pass 3a)

- `GetTopScoresAsync(LeaderboardBoard board, int limit = 10)` returns a `TopScoresResult` (`Success`, `Rows`, `Self`, `Total`): the top `limit` rows of a live board, the player's own row when it is outside them (an extra `GetPlayerScoreAsync`; only `EntryNotFound`, code 27009, means "no entry", any other error fails the read), and the board's entry count (`Total`, 0 when empty). Own rows are detected by comparing `PlayerId` to `AuthenticationService.Instance.PlayerId`. `GetTopScoresAsync(limit)` is the weekly shorthand.
- **Cache:** a successful result is reused for 90 s per (board, limit), concurrent callers share one in-flight task, a failure is never cached, and `InvalidateLiveBoards()` (called after a successful submit) drops it. No polling anywhere.
- `GetWeeklyVersionsAsync()` returns `WeeklyVersions` (`LiveVersionId`, `NextResetUtc`, `TotalArchived`, `Archived`: up to `ArchivedWeeksLimit` = 4 `ArchivedWeek { Id, StartUtc, EndUtc }`, newest first) from one `GetVersionsAsync` call, reused until `NextResetUtc` passes (then fetched again, which is how a week rolling over mid-session is noticed); a failure is not cached. If the device clock is ahead of the server, the cached result would look stale at once (and cost a call on every use): a result fetched after its own reset time already passed locally is reused for 5 minutes instead.

## Closed weeks and the results flow (Daily play structure, Pass 4)

- `GetArchivedWeekAsync(versionId)` returns a `TopScoresResult` for a closed week: the top `ArchivedRowsLimit` = 10 rows (`GetVersionScoresAsync`), `Total` from the page (0 for a week nobody played), and `Self` from `GetVersionPlayerScoreAsync` when the player is outside the rows. Both reads start together (`Task.WhenAll`, no threads), so a week costs two calls. `EntryNotFound` means the player did not play that week (`Self` null, not an error); any other failure fails the read. Only ids from `GetWeeklyVersionsAsync` are passed (a malformed id throws `InvalidArgument`).
- **Cache:** by version id, no TTL (closed weeks never change), shared in-flight task, a failure is dropped, never cleared by a submit, and served offline once read. The leaderboard screen and the results screen share it (10 rows; the results screen uses the first 5).
- **Leaderboard screen:** the WEEKLY stepper is built from the cached versions (limit = closed weeks, arrows hidden with none), week N reads `Archived[N-1].Id`, dates come from the server (`LeaderboardWeek.RangeText(start, end)` prints the end minus one second), the reset chip comes from `NextResetUtc` (days, hours only on the last day, never minutes).
- **Last week's results (S11):** `LeaderboardLastWeekResultsSource` (an `ILastWeekResultsSource`) compares the newest closed version with `lb_last_shown_version`:
  - the versions call fails: nothing, nothing recorded;
  - nothing stored (fresh install): record the newest closed week silently, nothing shown; no closed week yet: record a `none` marker, so the first week ever closed is not mistaken for a fresh install;
  - same id: nothing;
  - a new week for a player who never ranked (`LeaderboardService.HasEverRanked()`: a stored best above 0) or a week nobody played: nothing, recorded;
  - otherwise the week is read; a read failure is not recorded (next launch retries); the screen shows the first 5 rows, the player's rank, "of N pilots", top percent (rank over total, rounded up, 1 to 100) and best score, and the no-rank variant only for a player who has ranked before but not that week;
  - the week is recorded by `MarkShown()`, which the launch flow calls only when it actually shows the screen (so a result dropped by the deadline comes back next launch). Someone away for weeks only sees the newest closed week.
- **Launch timing:** `LaunchFlowHost` starts the lookup at the first progress report so it overlaps the loading screen's 1.5 s minimum, then waits at most `LaunchFlow.RESULTS_LOOKUP_SECONDS` = 3 s (frame-polled, no `Task.Delay`); a late result is dropped, never shown mid-session. The Editor / `DEBUG` `Debug Results` field still forces the static sample through the real flow and touches neither the service nor the stored id. Unverified until a real run: the 3 s cap itself (an unfocused Editor jumps the game clock in one step).
- Call budget: launch with nothing new is 1 versions call; a new week adds 2 (the week); a fresh install is 1.

## Weekly + all-time spike results (Daily play structure, Pass 2)

Run 2026-10-25 against the real boards. The spike was a throwaway script run through the Unity CLI in play mode.

- **`GetVersionsAsync(id, new GetVersionsOptions { Limit = 4 })`** returns, in one call: `versionId` (the live version), `nextReset` (weekly only; absent on all-time), `totalArchivedVersions` (absent while 0) and `results`, newest first, each `{ id, start, end }`. `Limit` is honoured as a display limit. Version IDs are 23-digit timestamp strings.
- **Archiving works.** A dashboard Reset (manual, same as the scheduled one) closes the live version into `results` immediately, and the new live version starts empty. `nextReset` read `2026-10-26T00:00:00Z`, which confirmed the schedule; the real scheduled reset on 2026-10-26 then archived a version as expected.
- **The no-entry case is a `LeaderboardsException`, `Reason == EntryNotFound`, `ErrorCode` 27009**, for all three of: the live `GetPlayerScoreAsync` after a reset, `GetVersionPlayerScoreAsync` on an archive the player never entered, and `GetVersionPlayerRangeAsync` on the same. It is expected, not an error to log.
- **`GetVersionScoresAsync` on an empty archive** returns `results: []` with `total` absent (read as 0); on a populated one it carries `total` (the "of N pilots" number).
- **Archived own entry** (`GetVersionPlayerScoreAsync`) returns `rank` (zero-based, add 1), `score`, `playerName`, plus the version `{ id, start, end }`.
- **A malformed version ID** throws `LeaderboardsException` with `Reason == InvalidArgument`, `ErrorCode` 1004, so only pass IDs that came from `GetVersionsAsync`.
- **Costs per call for the budget:** one `GetVersionsAsync` covers the reset check, the reset label and the browse list; each archived week costs 2 calls (own entry + top scores); the player-range call is not needed for the screens as designed.

## Cloud Code spike results (Daily play structure, Pass 7a)

Run 2026-10-29 in the `dev` environment (the existing one, reused). Throwaway scripts driven from the Unity CLI in play mode; the findings below are what the real scripts (7b) and client (7c) build on.

- **Environment.** Everything is per environment: boards, scripts, the access policy and Cloud Save data. The game picks the environment with `LeaderboardService.m_environmentName` (`InitializationOptions.SetEnvironmentName`; `dev` for now, Pass 9 moves it to production). `weekly_scores` / `alltime_scores` were recreated in `dev`.
- **Packages.** `com.unity.services.cloudcode` **2.10.4** (3.0.0 needs Unity 6000.7), `com.unity.services.deployment` 1.7.2 and `com.unity.services.tooling` 1.4.1 (the last adds Create > Services > Access Control Configuration). Scripts and the policy deploy from the Editor's Deployment window; no CLI needed.
- **Script context.** `{ params, context, logger }`; `context` has `playerId`, `projectId`, `environmentId`, `environmentName`, `accessToken` and `serviceToken`. The server clock is `Date.now()`.
- **Client call.** `CloudCodeService.Instance.CallEndpointAsync<JToken>(name, dict)`: an object result cannot be read as `string` (`DeserializationException`).
- **Parameters are dropped unless the server knows them.** A script only receives the parameters declared for it. The Editor's in-script reading (`module.exports.params`) needs a Node project (`package.json`) that this project does not have, so the importer found none. Put them in the script's `.meta`: `Parameters:` entries with `ParameterType` (0 String, 1 Numeric, 2 Boolean, 3 JSON, 4 Any) and `Required`, then redeploy. If a `module.exports.params` block is also written it must come after `module.exports = ...` (assigning `module.exports` replaces the object).
- **Importer.** A `.js` under `Assets` must use the Cloud Code `ScriptedImporter` (copy the `.meta` of a script made with Assets > Create > Services > Cloud Code Js Script). After a recompile the Editor loses the loaded scripts' names and the deploy fails with "was found duplicated with other files"; reimport the scripts (right-click > Reimport) before deploying.
- **Cloud Save from a script.** `new DataApi(context)` with `setPrivateCustomItem` / `getPrivateCustomItems` works, keyed by `context.playerId`. The write lock works: a write with a stale `writeLock` is rejected with 409 (code 7012), so two quick `StartRun` calls cannot both spend an attempt.
- **Leaderboards from a script.** `new LeaderboardsApi({ accessToken: context.serviceToken })` (the service token, not the player's, so the call is not a `Player` call) with `addLeaderboardPlayerScore(projectId, boardId, playerId, { score })`. The response body is **empty**; read the entry back with `getLeaderboardPlayerScore` for the rank (zero-based) and score.
- **Revoking client writes.** The access policy is an Access Control Configuration asset (`.ac`) deployed from the Editor. Unity's documented narrow resource (`urn:ugs:leaderboards:/v1/projects/*/leaderboards/*/scores/players/*`) did **not** block a client write (a Cloud Save deny with the same form did: 403, code 56). The broad form works: `Deny` / `Write` / `Player` on `urn:ugs:leaderboards:*` makes the client's `AddPlayerScoreAsync` throw `LeaderboardsException` "Access has been restricted", while player reads and the script's service-token write still work.
- **Consequence.** With the policy live in an environment, the game's direct `AddPlayerScoreAsync` submit fails there (resolved in Pass 7e: scores now go through the `SubmitScore` script). The Editor's anonymous test player in `dev` is a new player (the spike signed out once).

## Anonymous identity

UGS anonymous sign-in stores its token in local storage (PlayerPrefs / IndexedDB on WebGL). Clearing site data creates a brand-new player with no score history, and the stored bests above are lost with it. In dev, Unity's WebGL Build And Run serves each build from a new localhost port, which is a new origin and so a new player every rebuild — a dev-only artifact. Verified working on Android and WebGL in the Live leaderboard spike; sign-in, submit and fetch all succeeded.

## UI

See the Game Over rank readout (with Retry on a failed submit) under [ui.md](ui.md#game-over) and the leaderboard screen under [ui.md](ui.md#main-menu-and-its-screens): its WEEKLY (this week) and ALL-TIME tabs read `GetTopScoresAsync` for the two boards; past weeks step through the real closed weeks (above). The display-name field is deferred to the Player profile & run stats milestone (the UGS auto-generated name is used). The "Last week's results" screen (S11) reads its data from `LeaderboardLastWeekResultsSource` (see [ui.md](ui.md#launch-flow)); its SHARE button stays a toast until Pass 8.
