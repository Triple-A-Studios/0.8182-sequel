# Badges (Player profile & run stats, Pass 5)

`Scripts/Badges` (`BadgeDefinition`, `BadgeCatalog`, `BadgeService`), namespace `Opoint8182.Badges`; the UI is in `Scripts/UI/Profile` (`ProfileScreen` tiles, `BadgePopup`). Local only (`PlayerPrefs`, `badge.<id>` = tiers earned), like the stats the tiers read.

## Model

- **Counted badges** (11) have three tiers with ascending thresholds: tier 3 first (**bronze**), then tier 2 (**silver**), then tier 1 (**gold**, the hardest). They read a stat through a `Progress` function. The Profile header is `x / 11`: the counted badges with at least one tier.
- **Hidden badges** have one unlock, are **not counted** (neither in x nor in y), show **only once earned**, are drawn **first** in the row, and look different (a dark tile with a gold label and an outline that pulses between two golds). They unlock either from a `Condition` (checked by `BadgeService.Evaluate`) or by `BadgeService.Unlock("id")` from anywhere in the game (an easter egg, see the Beta milestone "Easter eggs" in [plan.md](../plan.md)).
- Earning only goes up: a stat reset does not take a badge away. `Evaluate()` earns every tier whose threshold the stat has reached (a big jump can earn several), then the hidden conditions (so a hidden badge about the others sees the new state).

## The catalog (`BadgeCatalog.All`, the one place to edit)

| Id | Tile | Measures | Tiers (3 / 2 / 1) | Kind |
|---|---|---|---|---|
| `flattened` | DEMO CREW | buildings flattened, total | 100 / 500 / 2,500 | total |
| `streak` | HOT STREAK | best streak of buildings in a row (`BestCombo`) | 8 / 15 / 30 | best |
| `perfect` | PERFECT PILOT | perfect crashes, total | 25 / 150 / 600 | total |
| `bigrun` | BIG RUN | best score of a ranked run (`HighScore`) | 3,000 / 8,000 / 20,000 | best |
| `roadtrip` | ROAD TRIP | distance flown, total | 25 / 100 / 300 km | total |
| `heavy` | HEAVY HITTER | tough buildings broken properly | 20 / 100 / 400 | total |
| `endurance` | LONG HAUL | longest single run (time in the air) | 1:30 / 3:00 / 5:00 | best |
| `precision` | DEAD EYE | most perfect crashes in a row in a run | 3 / 6 / 12 | best |
| `hoarder` | POINT HOARDER | points scored, all runs | 25,000 / 150,000 / 1,000,000 | total |
| `medic` | MEDIC | heal buildings used | 5 / 25 / 100 | total |
| `coins` | COIN STASH (COINS COLLECTOR) | coins collected, total earned | 1,000 / 10,000 / 50,000 | total |
| `allstar` (hidden) | ALL STAR | every counted badge has at least tier 3 | n/a | condition |

**COINS COLLECTOR cannot be earned yet:** nothing collects coins until the currency exists (Beta, "Shop, currency, economy"). The stat (`PlayerStats.CoinsCollected`, `RunStats.Coins`) is wired and stays 0, so the badge shows as a locked "?" and its popup reads 0 / 1,000; the currency milestone only has to pass the run's coins into `RunStats`. Until then ALL STAR cannot be earned either (it needs every counted badge). Its tile text is the stand-in COIN STASH (nine letters do not fit a tile).

**The thresholds are placeholders**, picked without real run data: tune them from play (each is one array of three values in `BadgeCatalog`). The tile titles are short two-line stand-ins for the badge names (FLATTENED, STREAK, PERFECT, BIG RUN, ROAD TRIP, HEAVY HITTER, ENDURANCE, PRECISION, POINT HOARDER, MEDIC, "unlock all"): rename in the same table. To add a hidden badge: add a `BadgeDefinition.Secret(id, title, description, condition)` entry (condition null for an egg that calls `BadgeService.Unlock(id)`); never change an id once released.

## Stats added for the badges ([player-stats.md](player-stats.md))

`ToughBroken` (a tough building broken properly, i.e. a crash that feeds the combo; a mistimed hit does not count), `HealsUsed` (a `HealBuilding` that restored health), `BestPerfectStreak` (perfect crashes in a row, ended by any non-perfect crash, a mistimed hit included), `LongestRunSeconds` (time spent flying: paused time and the menu excluded, a frame counts at most 0.5 s), `TotalScore` (all runs). `RunStatsTracker` collects them, `RunStats` carries them, `PlayerStats.RecordRun` stores them.

## Where it runs

- `GameManager.EndRun`: after `PlayerStats.RecordRun`, `BadgeService.Evaluate()`, then `LastUnlocks = BadgeService.TakeNewUnlocks()` (this also includes any easter egg unlocked during the run) before `RunEnded` fires.
- **Game Over toast** (`GameOverUI.AnnounceBadges`, 2.2 s after the screen shows, so the entry cascade has settled): always **one** toast. A badge that gained several tiers in the run counts once, at its best tier. One badge: "BADGE UNLOCKED · DEMO CREW TIER 3"; two: "BADGES UNLOCKED · DEMO CREW TIER 3, HOT STREAK TIER 3"; three or more: the first two and "+N MORE" (the Profile shows the rest). Several badges keep the toast up for 4 s instead of 2.5 s (it wraps to two lines at most). A hidden badge reads without a tier.
- **Profile** (`ProfileScreen.Open`): evaluates silently (drains the queue, no toast), so badges for stats that existed before the badges appear, then rebuilds the tiles.

## UI

- **Tile:** the best tier earned colours it (bronze / silver / gold; `--bronze`, `--silver`, `--gold` and their lips in `Theme.uss`), with one pip per tier (filled for the tiers earned). A counted badge with no tier is a dark "?" tile. A hidden one is earned-only (see above).
- **Popup** (`popup-badge` in `MainMenu.uxml`, `BadgePopup`): the badge name and description, one row per tier ("TIER 3 · 100 buildings flattened" with DONE; the next tier shows "312 / 500"; the rest are dimmed) and, once **every tier is done**, a muted all-time line ("Total: 12,345 buildings flattened" or "Best: 1:42 in one run"). A hidden badge shows only "Secret badge · description". The menu controller and the `MenuScreensGallery` driver both open it from `ProfileScreen.BadgeRequested`; CLOSE and a backdrop tap close it.
- **New cue for a hidden badge:** an earned hidden badge the player has not opened yet carries a coral pip on its Profile tile and on the menu's PROFILE chip (`BadgeService.IsUnseen` / `HasUnseenHidden`, saved as `badge.<id>.seen`). Tapping the tile marks it seen and removes both pips (the chip refreshes when the menu is shown again). A proper unlock animation is a later polish item (plan.md, Polish pass).
- The share card of the Profile draws the same tiles.

## Testing

Editor menu `Tools > Badges` (`Scripts/Editor/BadgeMenu.cs`): Log Badges, Evaluate Now, Reset Badges (the stats stay, so Evaluate Now earns them back), Add Big Sample Run (records a run big enough to earn several tiers). `Tools > Player Stats > Reset Stats` clears the stats but not the badges.

## Out of scope

Distinct looks for the future paid Android build, other profile effects, and badge effects on Game Over and the boards: later (Beta).
