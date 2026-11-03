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

- **Tile:** the best tier earned colours it (bronze / silver / gold) and the colour is the only tier cue: **the Profile tiles carry no pips** (removed after 6f; the Achievements rows keep their three pips). Each tier is three flat tones, no gradient or glow: a face (`--bronze`, `--silver`, `--gold`), a lighter rim that is the tile's border (`--*-rim`) and a darker lip (`--*-lip`), all in `Theme.uss` (the one place to tune them: bronze a copper orange `#D97A40`, silver a cool grey `#CBD4DC`, gold a deep gold `#EDB22B`, chosen so bronze does not read as brown and gold does not read as lemon yellow). A counted badge with no tier is a dark "?" tile. A hidden one is earned-only (see above).
- **Popup** (`popup-badge` in `MainMenu.uxml`, `BadgePopup`): the badge name and description, one row per tier ("TIER 3 · 100 buildings flattened" with DONE; the next tier shows "312 / 500"; the rest are dimmed) and, once **every tier is done**, a muted all-time line ("Total: 12,345 buildings flattened" or "Best: 1:42 in one run"). A hidden badge shows only "Secret badge · description". The menu controller and the `MenuScreensGallery` driver both open it from `ProfileScreen.BadgeRequested`; CLOSE and a backdrop tap close it.
- **New cue for a hidden badge:** an earned hidden badge the player has not opened yet carries a coral pip on its Profile tile and on the menu's PROFILE chip (`BadgeService.IsUnseen` / `HasUnseenHidden`, saved as `badge.<id>.seen`). Tapping the tile marks it seen and removes both pips (the chip refreshes when the menu is shown again). A proper unlock animation is a later polish item (plan.md, Polish pass).
- The share card of the Profile draws the same tiles.

## Testing

Editor menu `Tools > Badges` (`Scripts/Editor/BadgeMenu.cs`): Log Badges, Evaluate Now, Reset Badges (the stats stay, so Evaluate Now earns them back), Add Big Sample Run (records a run big enough to earn several tiers). `Tools > Player Stats > Reset Stats` clears the stats but not the badges.

## Out of scope

Distinct looks for the future paid Android build, other profile effects, and badge effects on Game Over and the boards: later (Beta).

## Pass 6 plan: badge UI changes

Status: **planned (6a), not built.** Decided with the developer on 2026-11-02. No mockup file exists for these screens; the layout below was reviewed as a sketch in the planning chat, so sizes are proposals to adjust when each sub-pass is built.

### What changes

1. **Profile badge card:** shows only the badges the player **picked** (up to 6) plus every **earned hidden** badge (hidden ones first, outside the 6). Tiles get a little bigger so the full name reads: **152 reference px wide** (was 128), square, and the row **shrinks the tiles slightly to fit** instead of scrolling when 6 + hidden are too wide. If a name still does not fit, each of the two title lines ends in an ellipsis ("HOARD..."). The line under the row ("Tap a badge to see its tiers.") is **removed**; the card header becomes the entry point: **BADGES  x / 11  ›** with a chevron, the whole header row tappable (at least 80 px tall). Tapping a tile still opens the tier popup, as now.
2. **Achievements screen** (new, route **Menu > Profile > Achievements**, back returns to the Profile): a vertically scrollable list of **every counted badge plus the earned hidden ones**. Row = name as the heading, the description under it, the **three tier pips on the right** (bronze, silver, gold; earned ones filled in their colour, unearned hollow). Locked badges show name, description and empty pips, dimmed. Earned hidden badges come first with the gold outline and the same coral "new" pip until opened; unearned hidden badges are not listed. Tapping a row opens the **same tier popup** as on the Profile (the popup layer is shared, so it opens over the Achievements screen). The header carries the back chevron, ACHIEVEMENTS, the `x / 11` chip and the **PICK** button on the right.
3. **Pick mode** (PICK turns the same list into a checklist): a checkbox per **earned counted** badge (locked rows and hidden rows have none), the chip reads "x / 6 picked", PICK becomes DONE. At 6 ticked, the unticked rows' checkboxes grey out (untick one to free a slot; no error message). The whole row toggles the tick in this mode (no popup). **DONE saves**; back or Escape **discards** the ticks (same rule as the name edit). PICK is **hidden** until at least one counted badge is earned.

### Rules

- **Picked set:** earned counted badges only, **catalog order** on the Profile (not pick order). Before the player has ever pressed DONE, the Profile shows the **first 6 earned** in catalog order (derived live). After the first DONE the set is stored and **never changes by itself**: a badge earned later appears only once picked; fewer than 6 picked leaves no placeholder slots (only real tiles are drawn). Hidden earned badges are always shown and never count toward the 6.
- **Names:** the tile and the row heading both use the current stand-in titles (DEMO CREW, HOT STREAK...), single line in the list; the descriptive names stay in the catalog notes.
- **Storage:** `PlayerPrefs` like the rest (a `badge.picked` string of ids plus a "has picked" flag); local only.
- **Chevron** is drawn as a shape (rotated border corner), not a glyph, because only baked glyphs may be used.
- **Ellipsis:** UI Toolkit ellipsises only a single non-wrapping line, so a tile title becomes two labels (one per line), each `white-space: nowrap; overflow: hidden; text-overflow: ellipsis`.
- **Share card:** the Profile share card shows the picked tiles and drops the chevron (`PrepareForShare`).
- **Pip cue for a hidden badge** stays as in Pass 5 (Profile tile, menu PROFILE chip) and is also shown on the hidden row in the Achievements list; opening its popup from either place marks it seen.
- **Layout budget:** the tiles grow 24 px but the caption line (about 40 px) goes, so the card is no taller; the aspect sweep (panels down to 1029 px logical height) must still pass.
- **Touch targets:** list rows, the header row, PICK / DONE and the checkboxes are at least 80 reference px.

### Sub-passes

| Sub-pass | Content |
|---|---|
| 6a | Plan the new UI (this section) |
| 6b | Badge picks store: picked ids, "has picked", default first 6 earned, catalog-order read, save on DONE, Editor test menu (**landed**, see below) |
| 6c | Profile badge card: picked + hidden tiles, 152 px shrink-to-fit, two-line ellipsis labels, chevron header, caption removed, header tap raises the route (**landed**, see below) |
| 6d | Achievements screen: `Achievements.uxml/uss`, router route, list rows with name / description / tier pips, hidden rows first, popup from rows, unseen pip, back (**landed**, see below) |
| 6e | Pick mode: PICK / DONE, checkbox rows, counter chip, 6-limit greying, discard on back, hide PICK with nothing earned (**landed**, see below) |
| 6f | Hardening: share card, `MenuScreensGallery` states (key for Achievements and pick mode), aspect sweep, 80 px targets, docs (`ui.md`, this file), `current-state.md` (**landed**, see below) |

### 6b: the picks store (`BadgePicks`)

`Scripts/Badges/BadgePicks.cs`, static, `PlayerPrefs` (`badge.picked` = comma-separated ids, `badge.has_picked` = 1 after the first save). No UI uses it yet; 6c reads it for the Profile row and 6e writes it from DONE.

- `Current()`: the picked counted badges in **catalog order**, at most `MAX` (6). Until the first `Save` it is derived: the first 6 earned counted badges in catalog order. After it, the stored ids are used (ids that are no longer pickable are skipped) and the set never changes by itself.
- `ProfileBadges()`: what the Profile row shows: every earned hidden badge first, then `Current()`.
- `IsPickable(badge)`: a counted badge with at least one tier. `AnythingToPick()`: any pickable badge (the PICK button shows only then). `IsPicked(badge)`.
- `Save(ids)`: keeps the ids that are known, counted, earned and not repeated, in catalog order, at most 6; marks the set as stored; raises `Changed`; returns how many were kept. `Reset()` forgets it (the set is derived again); `BadgeService.Reset()` calls it.
- Test menu `Tools > Badges`: Log Picks, Save Picks: Last 3 Earned, Save Picks: Try 8 (keeps 6), Clear Picks.

### 6c: the Profile badge card

- **Row:** `ProfileScreen.BuildBadges` draws `BadgePicks.ProfileBadges()` (earned hidden first, then the picked ones). The old horizontal `ScrollView` is a plain flex row (`profile-badge-row`, `overflow: hidden`): tiles are **152 x 152** reference px with `flex-shrink: 1` and `min-width: 100px`, so a crowded row (4:3, or 6 picks plus hidden ones) squeezes them evenly instead of scrolling. The tier colours, pips, glow and "new" pip are unchanged.
- **Names:** a tile title is a column of one `Label` per line (`badge__title` > `badge__label`), each `white-space: nowrap; overflow: hidden; text-overflow: ellipsis`, so a squeezed tile reads "HOARD..." instead of clipping (UI Toolkit ellipsises only a single non-wrapping line, which is why the two lines are separate). Unearned counted badges are no longer drawn (a locked "?" tile can only appear for a picked badge that is not earned, which `BadgePicks` does not allow).
- **Header:** the caption line is gone. The BADGES row (`profile-badges-header`, at least 80 px tall, pressed feedback) is the tap target and ends in a chevron drawn as a rotated corner (`profile-badges__chevron`, no glyph). Its tap raises `ProfileScreen.AchievementsRequested`; until 6d the menu answers with the "ARRIVES IN A LATER UPDATE" toast. The count reads `x / 11`. `PrepareForShare` hides the chevron.
- `BadgeCatalog.VisibleOrdered()` (hidden earned first, then every counted badge) is no longer used by the Profile; 6d's Achievements list uses it.

### 6d: the Achievements screen

- **Files:** `UI/Achievements.uxml` + `Achievements.uss` (a template instanced in `MainMenu.uxml` as `screen-achievements`) and `Scripts/UI/Profile/AchievementsScreen.cs`. Route id `achievements`: Menu > Profile > (BADGES header) > Achievements; back pops to the Profile. `MainMenuController` registers it, shows it on `ProfileScreen.AchievementsRequested` (the 6c stand-in toast is gone) and calls `Open()` / `Close()` from `OnScreenChanged`. `MenuScreensGallery` has it too (key **B**).
- **List:** a vertical `ScrollView` of `BadgeCatalog.VisibleOrdered()` (earned hidden first, then every counted badge, in catalog order; unearned hidden badges are not listed). Header: back chevron, ACHIEVEMENTS, the `x / 11` count. No PICK button yet (6e).
- **Row** (`ach-row`, at least 112 px tall, whole row tappable): the badge title on one line as the heading, the description under it, and on the right one pip per tier. Pips are bronze, silver, gold for tiers 3, 2, 1 when earned and hollow otherwise (a hidden badge has one gold pip). A badge with no tier is dimmed. An earned hidden badge has the gold outline and gold name; while unseen it also carries the coral "new" pip, which a tap removes and marks seen.
- **Popup:** a tap raises `BadgeRequested`; the menu (and the gallery) open the same `popup-badge` as on the Profile, over the Achievements screen. `Open()` evaluates the badges silently, like the Profile.

### 6e: pick mode

All in `AchievementsScreen` (plus a PICK button in `Achievements.uxml`).

- **PICK** sits at the right of the header and shows only when `BadgePicks.AnythingToPick()` (at least one counted badge earned). It starts pick mode with the ticks seeded from `BadgePicks.Current()` (so a player who never picked starts from the derived first six).
- **In pick mode** the title reads PICK BADGES, the count chip reads "x / 6 picked", PICK becomes **DONE** (primary variant). Earned counted rows get a checkbox on the left (a drawn tick, no glyph); locked rows and hidden rows have none and cannot be ticked. **The whole row toggles** the tick and the tier popup is not available. At 6 ticks the **unticked rows grey out** and ignore taps (untick one to free a slot); no message.
- **DONE** saves through `BadgePicks.Save` (catalog order, so the tick order does not matter) and leaves pick mode; zero ticks is allowed and gives a Profile row with only hidden badges. The Profile re-reads the picks when it is opened, so it is current on return.
- **Back / Escape** while picking only **leaves pick mode and drops the ticks**; the next one leaves the screen (the same rule as the name edit). Leaving the screen another way (`Close`) also drops them. `MainMenuController.Update` and the gallery driver both check `CancelPick()` before going back (not while a popup is open).
- The list keeps its scroll position when the mode flips.

### 6f: hardening

- **Aspect sweep** (driven from the Unity CLI in the `MenuScreensGallery`, the method in [ui.md](ui.md): the menu's `safe-area` element sized to each logical panel, each screen shown, the visual tree scanned for elements outside their screen, interactive targets under 80 px, text under 24 px and truncated labels), with 6 picks plus the hidden badge and the full 12-row list: **1800x1350 (4:3), 1920x1200 (16:10), 2400x1080 (16:9), 2514x1029 (22:9)** x Profile, Achievements, Achievements in pick mode. **No overflow, no target under 80 px, no text under 24 px on any of them.** The only truncation: at 4:3 the seven Profile tiles squeeze to about 126 px and the longest title word (POINT **HOARDER**) ends in an ellipsis; 16:10 and wider fit every title. The squeeze was tightened in this pass (tile gap 14 to 12 px, title padding 8 to 2 px) so PERFECT PILOT now fits at 4:3 too.
- **Share card:** the Profile card draws the picked tiles and the hidden badge, with no chevron; the hidden badge's coral "new" pip is now hidden on it too (`PrepareForShare`; it was showing on the rendered image).
- **Gallery and test menu:** `MenuScreensGallery` keys: **B** Achievements, **K** toggles pick mode while on it. `Tools > Badges > Unlock Hidden: ALL STAR (test)` earns the hidden badge.
- **Not covered:** a real phone (touch-drag scrolling of the list against row taps, the safe area on a notch) is for Pass 7's device checks.
