# Leaderboard

> **Superseded in design (2026-10-07):** the board shape below (single daily `daily_scores`, daily rank trend) is what shipped in the Live leaderboard milestone. It is being replaced by a weekly (archived) + all-time structure — see [design-doc.md](../design-doc.md#leaderboards--daily-attempts-decided-2026-10-07) and backlog #28 in [progress.md](../progress.md). No code has changed yet; this document describes the current code until that work lands.

`LeaderboardService` (`Assets/_Project/Scripts/Leaderboard/LeaderboardService.cs`, namespace `Opoint8182.Leaderboard`) is a thin wrapper over Unity Leaderboards (UGS) with anonymous sign-in. Backend choice and the reasoning behind it are in [design-doc.md](../design-doc.md) (Alpha section); packages in [dependencies.md](dependencies.md).

## Service (Live leaderboard, Pass 3)

A `PersistentSingleton<LeaderboardService>` (`TripleA.Utils.Singletons`) on the `Bootstrap` scene object next to `BootstrapLoader`, so UGS init + anonymous sign-in start at boot, in parallel with the scene loads. Consumers use `LeaderboardService.TryGetInstance()` (never `.Instance`, which would auto-create a stand-in): a scene opened without Bootstrap gets `null` and Game Over shows "Leaderboard unavailable" instead of throwing. One caveat: the singleton base destroys the whole GameObject if a duplicate instance appears, and that object also holds `BootstrapLoader` — fine today because Bootstrap loads once.

- `State` (`Idle`/`Initializing`/`Ready`/`Failed`) + `StateChanged`. `EnsureReadyAsync()` is idempotent, concurrent callers share one attempt, and a `Failed` state retries on the next call.
- `SubmitScoreAsync(int score)` returns a `SubmitResult` (`Success`, 1-based `Rank`, `Score`, `Trend`) and never throws — failures come back as `Success == false`. Scores `<= 0` are not submitted.
- **UGS ranks are zero-based**; the service adds 1 for display.
- Everything is `Task`-based on the main thread (no `Task.Run`/`.Wait()`/`.Result`), which is what WebGL needs.

**Top scores (Pass 4):** `GetTopScoresAsync(limit = 10)` returns a `TopScoresResult` (`Success`, `Rows`, and `Self` — the player's own row only when it isn't already inside the top N). Own-row detection compares each entry's `PlayerId` to `AuthenticationService.Instance.PlayerId`; the extra `GetPlayerScoreAsync` call throws when the player has no entry yet, which is treated as "no own row", not an error.

**Offline fast-fail (Pass 4):** both `SubmitScoreAsync` and `GetTopScoresAsync` return failure immediately when `Application.internetReachability == NotReachable`, so the UI never waits on a hanging request. 

**Request timeout (Pass 4):** `SubmitScoreAsync` and `GetTopScoresAsync` race the whole operation (sign-in included) against `m_requestTimeoutSeconds` (default 10s) and return failure if the timer wins, so a hung request ends in the existing error/Retry states instead of an endless "Loading..."/"Submitting...". The timer is PrimeTween's `Tween.Delay` (unscaled time), not `Task.Delay`: `Task.Delay` is backed by a `System.Threading` timer that doesn't work on WebGL, while PrimeTween runs off its own update loop with no threads (its docs: tweens can be awaited on all platforms including WebGL). The abandoned request keeps running but is flagged timed-out so a late submit success doesn't write the cached rank/date (that would make the player's retry show "Same" instead of the real trend). If the timeout hits while UGS init is still in flight, the service drops the stuck init task and goes `Failed`, so the next call starts a fresh init (unverified: how UGS reacts to a second `InitializeAsync` while the first is hung).

## Board configuration

One board, `daily_scores` (serialized `m_leaderboardId`), created in the Unity dashboard: High to Low, **Best Score** strategy, scheduled reset **daily at 00:00 UTC** (UGS archives prior days). Score submission is client-side via the SDK; Cloud Code validation is deferred to the Daily play structure milestone (design-doc.md).

## Rank trend

Because the board is Best Score, the rank is the player's best-of-day rank. `Compare(hasPreviousToday, previousRank, newRank)` (pure, static): first attempt of the UTC day => `Up` (new placement); otherwise a lower rank number is `Up`, higher is `Down`, equal is `Same`. A worse attempt therefore leaves the rank unchanged (`Same`); `Down` only happens when other players overtake between attempts.

The previous rank and its UTC date live in `PlayerPrefs` (`lb_last_rank`, `lb_last_utc_date`), written after every successful submit. This is deliberately soft: clearing storage resets it, the same weakness as the anonymous ID — and there is no attempt cap yet (3/day arrives with the Daily play structure milestone).

## Anonymous identity (from the Pass 2 spike)

UGS anonymous sign-in stores its token in local storage (PlayerPrefs / IndexedDB on WebGL). Clearing site data creates a brand-new player with no score history. In dev, Unity's WebGL Build And Run serves each build from a new localhost port, which is a new origin and so a new player every rebuild — a dev-only artifact. Verified working on Android and WebGL in the spike; sign-in, submit and fetch all succeeded.

## UI

See the Game Over rank readout (with Retry on a failed submit) under [ui.md](ui.md#game-over-screen-movement--fail-state-rework-pass-3) and the Top-10 panel under [ui.md](ui.md#leaderboard-panel-live-leaderboard-pass-4). The display-name field is deferred to the UI/art pass (the UGS auto-generated name is used).
