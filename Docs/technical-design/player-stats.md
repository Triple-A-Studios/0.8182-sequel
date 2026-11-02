# Player stats (Player profile & run stats, Passes 1-3 and 5)

`Scripts/Stats/PlayerStats.cs`, namespace `Opoint8182.Stats`. A static class like [`SettingsService`](settings.md), so it needs no scene object and works from `Bootstrap` or a standalone scene. It holds the player's lifetime stats, the six tiles on the Profile screen. Local only (`PlayerPrefs`), no cloud, soft: clearing storage resets them.

## Values

| Property | Meaning | PlayerPrefs key |
|---|---|---|
| `HighScore` | best score of a **competitive** run (0 = none) | `stats.high_score` |
| `DistanceMeters` | total distance flown | `stats.distance_m` (float) |
| `BuildingsDestroyed` | total buildings brought down | `stats.buildings` |
| `BestCombo` | longest combo streak ever: buildings crashed in a row without the combo being disrupted (not the capped multiplier) | `stats.best_combo` |
| `RunsFlown` | finished runs | `stats.runs` |
| `PerfectCrashes` | total perfect crashes | `stats.perfect` |

Values load lazily and are clamped to zero or more. Integer totals saturate at `int.MaxValue` instead of wrapping.

## Recording

`PlayerStats.RecordRun(RunStats)` is the only writer. `RunStats` is a readonly struct (score, distance in meters, buildings, best combo, perfect crashes, `IsPractice`) whose constructor clamps negatives to zero. `RecordRun` adds the run, saves to disk at once (a run ends rarely, so there is nothing to batch and nothing left dirty for an app kill to lose) and raises `Changed`. It returns true when the run set a new `HighScore`.

**Decision (Pass 1):** every run counts toward the totals, practice included; only `HighScore` is competitive-only, matching the boards (a practice run never reaches a board and never counts as a new best, see Game Over). `Reset()` clears everything (Editor menu now, a "delete my data" path later).

`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` drops the cache and the `Changed` handlers, so Editor Play with domain reload off keeps no stale values.

## Wiring

Recorded by `GameManager.EndRun` since Pass 2 (see the last section); the Profile tiles read `PlayerStats` since Pass 3 (below).

## Testing

Editor menu `Tools > Player Stats` (`Scripts/Editor/PlayerStatsMenu.cs`): Log Stats, Record Sample Competitive Run, Record Sample Practice Run, Reset Stats.

## Per-run stats and NEW BEST (Pass 2)

**`RunStatsTracker`** (`Scripts/Stats`, a `MonoBehaviour` that `GameManager.Awake` adds to its own object, so no scene edit) counts the run in progress: buildings brought down (every `CombatEvents.Crashed`, which only buildings raise, a mistimed tough hit included), perfect crashes (the `perfect` flag of `ScoreSystem.PointsAwarded`), the longest combo streak (`ScoreSystem.ComboStreakChanged`, see [score-system.md](score-system.md): +1 per combo crash, reset by a hazard hit or a missed building, not by the window lapsing; heal buildings never feed it). The game scene reloads for every run, so it always starts from zero. `Snapshot(isPractice)` freezes it (a plane flying off after the end no longer counts) and adds the score and the distance (`SpawnManager.DistanceTraveled`, world units = meters).

**`GameManager.EndRun`** takes the snapshot, keeps it as `LastRun`, keeps the lifetime high score as it stood as `PreviousHighScore`, calls `PlayerStats.RecordRun`, then raises `RunEnded`. So Game Over (and anything on `RunEnded`) reads a run that is already recorded. A run left before it ends (QUIT TO MENU, a mid-run restart) is **not** recorded.

**Game Over** (`GameOverUI`): the DISTANCE, FLATTENED and BEST COMBO chips come from `LastRun` through `RunStatsFormat` ("850 m" / "2.4 km", "1,204", "7": a plain count, no multiplication sign); the share card gets the same through `ShareCardData.ForRun(..., stats)`. EARNED stays `GameOverPlaceholderData.Earned` (currency milestone).

**NEW BEST decision:** a competitive run is a new best when its score beats `max(LeaderboardService.StoredAllTimeBest, PreviousHighScore)`; otherwise YOUR BEST shows that larger value (a practice run never sets one). The two stores are both soft `PlayerPrefs`: the server-confirmed best covers a player who had ranked runs before the local store existed, the local one covers runs the server never confirmed (offline, refused).

## Profile tiles (Pass 3)

`ProfileScreen.BindLifetimeStats` fills the six tiles (also on every `Open()` / `Refresh()`, so the screen is current after a run; the share card builds a fresh screen so it is too): HIGH SCORE `PlayerBest.Current`, DISTANCE FLOWN `RunStatsFormat.TotalDistance` ("850 m", "12.4 km", whole km from 100 km), BUILDINGS DOWN, BEST COMBO (a plain count), RUNS FLOWN, PERFECT CRASHES (`N0` counts). A fresh install shows zeros.

**`PlayerBest`** (`Scripts/Stats`) is the one definition of "the player's best score": the larger of the local `PlayerStats.HighScore` and `LeaderboardService.StoredAllTimeBest`. The HIGH SCORE tile and Game Over's NEW BEST / YOUR BEST both use it (`PlayerBest.Of(previousHighScore)` for the run being shown), so the two never disagree. The dev galleries (`MenuScreensGallery`, share samples) now show whatever is in the Editor's PlayerPrefs instead of the old sample numbers.

## Stats for the badges (Pass 5)

Six more lifetime values feed [badges](badges.md), carried by `RunStats` (new optional constructor arguments) and collected by `RunStatsTracker`: `ToughBroken` (`stats.tough`), `HealsUsed` (`stats.heals`), `BestPerfectStreak` (`stats.perfect_streak`, a maximum), `LongestRunSeconds` (`stats.longest_run_s`, a maximum) `TotalScore` (`stats.total_score`) and `CoinsCollected` (`stats.coins`, always 0 until the currency exists). They have no tile on the Profile; only the badges read them.
