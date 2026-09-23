# Progress

## Backlog
Open items only — once an item is addressed, its row moves to [Resolved Backlog](#resolved-backlog) below. `#` is a stable reference number; it doesn't change when an item resolves or the table is re-sorted.

| # | Item | Origin | Type |
|---|---|---|---|
| 15 | New building type: heals the plane on crash, same precision-matters feel as `Building`'s damage buildings — weak point + speed-gated crash-quality formula, but firing `CombatEvents.RaiseRestored` (scaled by quality) instead of `RaiseCrashed`. Needs its own component (`HealthSystem`/`Building`'s `IRestorer`/`ICrashSource` split means `HealthPickup`'s flat touch-heal can't be reused as-is) plus a new prefab and `SpawnKind` entry. | Developer note (2026-09-18) | Feature |
| 16 | Plane health HUD element (MVP phase) — no on-screen health readout exists today, only `FuelGaugeUI`/`ScoreUI`/`AltitudeWarningUI`/`GameOverUI`. Mirror `FuelGaugeUI`'s pattern for `HealthSystem`. Once an art pass (Alpha or Feel Polish) adds visual damage feedback on the plane itself (smoke, sparks, etc.), revisit whether the HUD element gets removed in favor of the visual cue or kept alongside it. | Developer note (2026-09-18) | Feature |
| 17 | Spawn algorithm (`SpawnManager`) needs revisiting — Pass 2's independent-per-spawn weighted roll + single global ramp plateau is a known-incomplete design, not a finished one (it plateaus into a flat, pattern-matchable steady state). See `design-doc.md`'s "Difficulty ramp — engagement levers" (parking lot section) for specific candidate fixes: staggered per-stat plateaus, a post-plateau oscillator, chunk-based spawning, the golden-building idea as the plateau answer, a separate flight-speed axis, risk-positioned pickups. | Developer note (2026-09-22) | Design |
| 18 | Lateral (sideward) bounds — mirrors `AltitudeSystem`'s ceiling/ground pattern but for the horizontal axis: a soft limit displays a warning, a hard limit past it ends the run. | Developer note (2026-09-23) | Feature |

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
| 13 | ~~Crash-quality calculation (speed × angle-into-weak-point) simplified so players don't need to reason about precise crash angles.~~ Done: dropped angle term, quality now speed-only gated on weak-point hit (`Building.cs`); `WeakPoint.OutwardNormal` removed as dead code (Pass 2, Milestone: Movement & fail-state rework). | Developer note (2026-09-14) | Design | Resolved (Pass 2, Milestone: Movement & fail-state rework) |
| 1 | ~~Plane momentarily stops on crash — both plane and building are colliders, physics resolves a full stop before `Destroy`~~ Done: `Building`'s `BoxCollider` is now a trigger, `OnTriggerEnter` replaces `OnCollisionEnter` — trigger overlaps skip collision resolution entirely, so there's no stopping impulse left to cause the stutter (Pass 5, Milestone: Movement & fail-state rework). | [Pass 1, Milestone: Building types + scoring](current-state.md) | Bug | Resolved (Pass 5, Milestone: Movement & fail-state rework) |
| 12 | ~~W/S pitch input becomes purely cosmetic (visual rotation only, mirroring how sideways/yaw movement already works) — actual vertical movement uses `rb.linearVelocity` directly, same approach as horizontal strafing.~~ Done: `m_verticalSpeed` drives real velocity, cosmetic pitch mirrors the existing bank `MoveTowards` pattern on `m_visualRoot` (Pass 1, Milestone: Movement & fail-state rework). | Developer note (2026-09-14) | Design/Refactor | Resolved (Pass 1, Milestone: Movement & fail-state rework) |
| 14 | ~~Add upper/lower altitude limits — crossing either ends the run.~~ Done: `AltitudeSystem` — ceiling warns then ends the run (plane keeps its last velocity, camera stops following); ground ends the run instantly, same freeze as a building crash or health-zero death (Pass 4, Milestone: Movement & fail-state rework). | Developer note (2026-09-14) | Feature | Resolved (Pass 4, Milestone: Movement & fail-state rework) |
| 8 | ~~Alchemy `[ShowInInspector]` debug fields (`CurrentFuel`, `CurrentScore`, `CurrentMultiplier`, `CurrentComboTimer`, etc.) don't visibly update in the Inspector during Play Mode.~~ Done: root cause known but out of scope (Alchemy repaint bug) — resolved instead by dropping `[ShowInInspector]`/`[ReadOnly]` for debug-only variables project-wide; use the Inspector's Debug mode instead (Pass 6, Milestone: Movement & fail-state rework). | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug | Resolved (Pass 6, Milestone: Movement & fail-state rework) |
| 7 | ~~Combo timer should also take a reduction for *missing* a building (flying past without crashing), not just for crashing hazards — needs a way to know a building existed and wasn't hit, which requires building spawning/tracking (not built yet, buildings are hand-placed). Implement once that spawner exists.~~ Done: `ScoreSystem` subscribes to `SpawnEvents.EntityCulled` (fires only for unhit buildings), drains `m_missedBuildingTimerPenalty` off the combo timer (Pass 3, Milestone: Difficulty ramp). | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement | Resolved (Pass 3, Milestone: Difficulty ramp) |

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

### Milestone: Movement & fail-state rework — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `a5f58c0` | Pitch-cosmetic / velocity-based vertical movement |
| 2 | Verified — Complete | `cd40dcd` | Crash-quality formula rework — speed-only, dropped angle-alignment term |
| 3 | Verified — Complete | `2f2e1b4` | Minimal Game Over screen |
| 4 | Verified — Complete | `d76fd2e` | Altitude bounds — ceiling warning + fly-away, ground instant freeze |
| 5 | Verified — Complete | `875f6d9` | Crash-stutter physics fix — trigger collider replaces solid collision (folds backlog item: crash stutter) |
| 6 | Verified — Complete | `b7a8725` | Alchemy debug-inspector refresh fix — dropped `[ShowInInspector]`/`[ReadOnly]` reliance for debug vars project-wide (folds backlog item: Alchemy inspector refresh) |

### Milestone: Difficulty ramp — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `93874e1` | Procedural spawner — `SpawnManager` replaces hand-placed buildings/obstacles/hazards/pickups, distance-based spawn ahead + cull behind |
| 2 | Verified — Complete | `c5e89b5` | Difficulty ramp curve — per-entry `AnimationCurve` weights + spawn-interval curve, capped at a tunable ramp distance (`HealthPickup` decays to a floor instead) |
| 3 | Verified — Complete | `1542362` | Combo-timer missing-building penalty — `ScoreSystem` subscribes to `SpawnEvents.EntityCulled` (folds backlog item: missed-building combo penalty) |

### Milestone: Feel polish — In Progress
See [current-state.md](current-state.md) for live pass-by-pass status.

## Phase: Alpha — Upcoming
See [plan.md](plan.md#phase-alpha) for scope.

## Phase: Beta — Upcoming
See [plan.md](plan.md#phase-beta) for scope.

## Phase: Full Launch — Upcoming
See [plan.md](plan.md#phase-full-launch) for scope.
