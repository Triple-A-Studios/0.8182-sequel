# Leaderboard

`LeaderboardService` (`Assets/_Project/Scripts/Leaderboard/LeaderboardService.cs`, namespace `Opoint8182.Leaderboard`) is a thin wrapper over Unity Leaderboards (UGS) with anonymous sign-in. Backend choice and the reasoning behind it are in [design-doc.md](../design-doc.md) (Alpha section); packages in [dependencies.md](dependencies.md).

## Service (Live leaderboard, Pass 3)

A `PersistentSingleton<LeaderboardService>` (`TripleA.Utils.Singletons`) on the `Bootstrap` scene object next to `BootstrapLoader`, so UGS init + anonymous sign-in start at boot, in parallel with the scene loads. Consumers use `LeaderboardService.TryGetInstance()` (never `.Instance`, which would auto-create a stand-in): a scene opened without Bootstrap gets `null` and Game Over shows "Leaderboard unavailable" instead of throwing. One caveat: the singleton base destroys the whole GameObject if a duplicate instance appears, and that object also holds `BootstrapLoader` — fine today because Bootstrap loads once.

- `State` (`Idle`/`Initializing`/`Ready`/`Failed`) + `StateChanged`. `EnsureReadyAsync()` is idempotent, concurrent callers share one attempt, and a `Failed` state retries on the next call.
- `SubmitScoreAsync(int score)` returns a `SubmitResult` (`Success`, 1-based `Rank`, `Score`, `Trend`) and never throws — failures come back as `Success == false`. Scores `<= 0` are not submitted.
- **UGS ranks are zero-based**; the service adds 1 for display.
- Everything is `Task`-based on the main thread (no `Task.Run`/`.Wait()`/`.Result`), which is what WebGL needs.

## Board configuration

One board, `daily_scores` (serialized `m_leaderboardId`), created in the Unity dashboard: High to Low, **Best Score** strategy, scheduled reset **daily at 00:00 UTC** (UGS archives prior days). Score submission is client-side via the SDK; Cloud Code validation is deferred to the Daily play structure milestone (design-doc.md).

## Rank trend

Because the board is Best Score, the rank is the player's best-of-day rank. `Compare(hasPreviousToday, previousRank, newRank)` (pure, static): first attempt of the UTC day => `Up` (new placement); otherwise a lower rank number is `Up`, higher is `Down`, equal is `Same`. A worse attempt therefore leaves the rank unchanged (`Same`); `Down` only happens when other players overtake between attempts.

The previous rank and its UTC date live in `PlayerPrefs` (`lb_last_rank`, `lb_last_utc_date`), written after every successful submit. This is deliberately soft: clearing storage resets it, the same weakness as the anonymous ID — and there is no attempt cap yet (3/day arrives with the Daily play structure milestone).

## Anonymous identity (from the Pass 2 spike)

UGS anonymous sign-in stores its token in local storage (PlayerPrefs / IndexedDB on WebGL). Clearing site data creates a brand-new player with no score history. In dev, Unity's WebGL Build And Run serves each build from a new localhost port, which is a new origin and so a new player every rebuild — a dev-only artifact. Verified working on Android and WebGL in the spike; sign-in, submit and fetch all succeeded.

## UI

See the rank readout under [ui.md](ui.md#game-over-screen-movement--fail-state-rework-pass-3). Top-10 panel and offline/error states are Pass 4; the display-name field is deferred to the UI/art pass (the UGS auto-generated name is used).
