# Progress

## Backlog
Open items only — once an item is addressed, its row moves to [Resolved Backlog](#resolved-backlog) below. `#` is a stable reference number; it doesn't change when an item resolves or the table is re-sorted.

| # | Item | Origin | Type |
|---|---|---|---|
| 1 | Plane momentarily stops on crash — both plane and building are colliders, physics resolves a full stop before `Destroy` | [Pass 1, Milestone: Building types + scoring](current-state.md) | Bug |
| 7 | Combo timer should also take a reduction for *missing* a building (flying past without crashing), not just for crashing hazards — needs a way to know a building existed and wasn't hit, which requires building spawning/tracking (not built yet, buildings are hand-placed). Implement once that spawner exists. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement |
| 8 | Alchemy `[ShowInInspector]` debug fields (`CurrentFuel`, `CurrentScore`, `CurrentMultiplier`, `CurrentComboTimer`, etc.) don't visibly update in the Inspector during Play Mode — stuck at their initial value all playtest. The underlying values are confirmed correct (HUD, which reads the same `Observable*` instances via `AddListener`, updates live) — looks like an Inspector repaint issue in the Alchemy package rather than a logic bug, not yet root-caused. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug |
| 12 | W/S pitch input becomes purely cosmetic (visual rotation only, mirroring how sideways/yaw movement already works) — actual vertical movement will use `rb.linearVelocity` directly, same approach as horizontal strafing. On release, vertical velocity returns to level and cosmetic pitch resets to neutral. Confirmed design change. Side effect: crash-impact angle becomes constant once pitch no longer reflects real trajectory, which affects the crash-quality formula — flagged alongside the next item because of that dependency. | Developer note (2026-09-14) | Design/Refactor |
| 13 | Crash-quality calculation (currently speed × angle-into-weak-point) will be simplified so players don't need to reason about precise crash angles — goal is lower cognitive load / easier-to-understand mechanics, likely trading off some depth. **Not yet decided** what the simplified formula looks like — open design question, needs discussion before scoping. Directly follows from the pitch-becomes-cosmetic item above (a constant crash angle makes the current angle-based term meaningless). | Developer note (2026-09-14) | Design |
| 14 | Add upper/lower altitude limits — crossing either ends the run. **Ceiling:** warn the player first; if they don't descend/go further up, run ends (plane flies off fast into the sky, camera stops following, Game Over screen appears). **Ground:** instant run end on impact, same "blast" cosmetic treatment as a building crash or a health-zero death, Game Over screen appears. Confirmed design decision, not yet scoped into a pass. Depends on a Game Over screen existing — no restart/game-over UI has been built yet. | Developer note (2026-09-14) | Feature |

## Resolved Backlog
Bookkeeping only — items move here once addressed, keeping the table above to open items.

| # | Item | Origin | Type | Status |
|---|---|---|---|---|
| 2 | ~~Extend the `IDamageDealer` pattern to other resource-affecting sources — a restore interface for future health/fuel pickups~~ Done: `IRestorer` (Pass 4, Milestone 3), consumed by `HealthSystem`, implemented by `HealthPickup`. | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Resolved (Pass 4, Milestone: Obstacles, combos, recovery) |
| 3 | ~~A symmetric refuel-source interface for `Building.Crashed` (so `FuelSystem` doesn't hardcode `Building` as the crash-source type) is still open — only the pickup/restore half of the original idea was folded into Pass 4.~~ Done: `ICrashSource` (Pass 1, Milestone: Core systems refactor). | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Resolved (Pass 1, Milestone: Core systems refactor) |
| 4 | ~~Replace per-instance `IDamageDealer`/`Crashed` array wiring (manual Inspector drag-drop + cast-at-Awake on `FuelSystem`/`HealthSystem`) with a static/global event any damage dealer or crash source fires, subscribed to once — the array approach doesn't scale to procedurally spawned obstacles, which can't be hand-wired in the Inspector.~~ Done: `CombatEvents` static event bus (Pass 1, Milestone: Core systems refactor). | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 1, Milestone: Core systems refactor) |
| 5 | ~~No overseeing system (`GameManager` or similar) exists yet — nothing owns run/game state above the individual resource systems.~~ Done: `GameManager` (Pass 4, Milestone: Core systems refactor). | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 4, Milestone: Core systems refactor) |
| 6 | ~~`FuelSystem`/`HealthSystem` each independently subscribe to crash/damage sources and independently decide run-end (disabling `PlaneController`, zeroing velocity) — they should be pure resource managers (apply increments/decrements, raise a "depleted" event) with a `PlayerManager` (or equivalent) owning crash/damage/run-end orchestration.~~ Done: `PlayerManager` (Pass 5, Milestone: Core systems refactor); `FuelSystem`/`HealthSystem` are now pure resource managers. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 5, Milestone: Core systems refactor) |
| 9 | ~~Mistimed tough-building hit (`!canBreak` branch) behaves inconsistently with `Building_Normal`'s always-destroys-and-scores pattern: building survived, scored nothing, only reached `HealthSystem`.~~ Done: mistimed hits now destroy with base score (quality=0) and skip the combo reward (Pass 2, Milestone: Core systems refactor) — confirmed design: "requires enough boost" is a scoring-quality gate, not a hard-resistance gate. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug/Design | Resolved (Pass 2, Milestone: Core systems refactor) |
| 10 | ~~More generally: every `IDamageDealer` source should reach all three penalty systems (`HealthSystem`, `FuelSystem`, `ScoreSystem`'s combo timer) uniformly, but today it's manual opt-in per source per array.~~ Done: the `CombatEvents` global bus (Pass 1) makes every source reach every listener uniformly by construction, no more per-source opt-in arrays. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 1, Milestone: Core systems refactor) |
| 11 | ~~`FuelSystem`/`HealthSystem` both drain by the exact same raw `damage` value from any wired `IDamageDealer` source — no independent per-resource tuning.~~ Done: `HealthDamageMultiplier`/`FuelDamageMultiplier` on `IDamageDealer` (Pass 3, Milestone: Core systems refactor), both defaulting to `1f`. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement | Resolved (Pass 3, Milestone: Core systems refactor) |

## Phase: Prototype — Completed

### Milestone: Core loop, bare minimum — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `b482138` | Plane movement + joystick steering |
| 2 | Verified — Complete | `01fc0ad` (crash-quality fix: `4f463f8`) | Single crashable building + weak-point detection |
| 3 | Verified — Complete | `ad1cfde` | Fuel system (drain + crash-precision refuel + run-end at zero) |
| 4 | Verified — Complete | `ad1cfde` | Minimal UI (fuel gauge) |

### Milestone: Building types + scoring — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `9145687` | Tough building type (variant requiring hold-boost-to-break, distinct visual, own weak point) |
| 2 | Verified — Complete | `2692147` | Boost mechanic (input, faster fuel drain, harder steering while active) |
| 3 | Verified — Complete | `968025f` | Health resource (second stat; lost on mistimed tough-building crash; death behavior — instant vs. gradual — TBD by playtest feel) |
| 4 | Verified — Complete | `1d93f12` | Score (simple visible score, increments per crash, shown in UI) |

Prototype phase complete — design doc: "Prototype is considered done here."

## Phase: MVP

### Milestone: Obstacles, combos, recovery — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `560c21a` | Large obstacle buildings (avoid-not-crash, fuel/health penalty, distinct visual) + generalize Fuel/Health source wiring to multi-source |
| 2 | Verified — Complete | `d25d86a` | Small hazards (birds) — lower-penalty reflex/dodge obstacle |
| 3 | Verified — Complete | `4101335` | Combo chain scoring — crash multiple buildings in quick succession for bonus/multiplier |
| 4 | Verified — Complete | `e9fce2d` | Health pickups + restore interface — generalize `IDamageDealer` into a matching restore-interface for fuel/health pickups (folds backlog item) |

### Milestone: Core systems refactor — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `0118f74` | Event-driven damage/restore wiring — `CombatEvents` static event bus + `ICrashSource` interface replace per-instance Inspector array wiring (folds backlog items: array wiring, symmetric refuel-source interface) |
| 2 | Verified — Complete | `2c22213` | Uniform penalty wiring + tough-building mistimed-hit fix — mistimed hits now destroy with base score, skip combo reward (folds backlog items: uniform 3-system wiring, mistimed tough-hit design) |
| 3 | Verified — Complete | `56a9ab7` | Per-resource damage tuning — `HealthDamageMultiplier`/`FuelDamageMultiplier` on `IDamageDealer` (folds backlog item: per-resource damage tuning) |
| 4 | Verified — Complete | `6173841` | `GameManager` — single source of truth for run-active state (folds backlog item: no overseeing system) |
| 5 | Verified — Complete | `ee01e77` | `PlayerManager` — owns crash/damage/run-end orchestration; `FuelSystem`/`HealthSystem` become pure resource managers (folds backlog item: pure resource managers) |

### Milestone: Movement & fail-state rework — In Progress
See [current-state.md](current-state.md) for live pass-by-pass status.

### Milestone: Difficulty ramp — Upcoming
See [plan.md](plan.md#phase-mvp) for scope. No passes defined yet.

## Phase: Alpha — Upcoming
See [plan.md](plan.md#phase-alpha) for scope.

## Phase: Beta — Upcoming
See [plan.md](plan.md#phase-beta) for scope.

## Phase: Full Launch — Upcoming
See [plan.md](plan.md#phase-full-launch) for scope.
