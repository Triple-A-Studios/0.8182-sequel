# Player stats (Player profile & run stats, Pass 1)

`Scripts/Stats/PlayerStats.cs`, namespace `Opoint8182.Stats`. A static class like [`SettingsService`](settings.md), so it needs no scene object and works from `Bootstrap` or a standalone scene. It holds the player's lifetime stats, the six tiles on the Profile screen. Local only (`PlayerPrefs`), no cloud, soft: clearing storage resets them.

## Values

| Property | Meaning | PlayerPrefs key |
|---|---|---|
| `HighScore` | best score of a **competitive** run (0 = none) | `stats.high_score` |
| `DistanceMeters` | total distance flown | `stats.distance_m` (float) |
| `BuildingsDestroyed` | total buildings brought down | `stats.buildings` |
| `BestCombo` | highest combo multiplier ever reached | `stats.best_combo` |
| `RunsFlown` | finished runs | `stats.runs` |
| `PerfectCrashes` | total perfect crashes | `stats.perfect` |

Values load lazily and are clamped to zero or more. Integer totals saturate at `int.MaxValue` instead of wrapping.

## Recording

`PlayerStats.RecordRun(RunStats)` is the only writer. `RunStats` is a readonly struct (score, distance in meters, buildings, best combo, perfect crashes, `IsPractice`) whose constructor clamps negatives to zero. `RecordRun` adds the run, saves to disk at once (a run ends rarely, so there is nothing to batch and nothing left dirty for an app kill to lose) and raises `Changed`. It returns true when the run set a new `HighScore`.

**Decision (Pass 1):** every run counts toward the totals, practice included; only `HighScore` is competitive-only, matching the boards (a practice run never reaches a board and never counts as a new best, see Game Over). `Reset()` clears everything (Editor menu now, a "delete my data" path later).

`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` drops the cache and the `Changed` handlers, so Editor Play with domain reload off keeps no stale values.

## Not wired yet

Nothing calls `RecordRun` in play: collecting the per-run values (distance, buildings, best combo, perfect crashes) and the Game Over chips is Pass 2; the Profile tiles reading `PlayerStats` instead of `ProfilePlaceholderData` is Pass 3. Game Over's NEW BEST still compares against `LeaderboardService.StoredAllTimeBest`; Pass 2 decides whether the local `HighScore` joins or replaces it.

## Testing

Editor menu `Tools > Player Stats` (`Scripts/Editor/PlayerStatsMenu.cs`): Log Stats, Record Sample Competitive Run, Record Sample Practice Run, Reset Stats.
