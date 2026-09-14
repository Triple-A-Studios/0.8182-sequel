# Progress

## Backlog

| Item | Origin | Type | Status |
|---|---|---|---|
| Plane momentarily stops on crash — both plane and building are colliders, physics resolves a full stop before `Destroy` | [Pass 1, Milestone: Building types + scoring](current-state.md) | Bug | Open |
| ~~Extend the `IDamageDealer` pattern to other resource-affecting sources — a restore interface for future health/fuel pickups~~ Done: `IRestorer` (Pass 4, Milestone 3), consumed by `HealthSystem`, implemented by `HealthPickup`. | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Resolved (Pass 4, Milestone: Obstacles, combos, recovery) |
| A symmetric refuel-source interface for `Building.Crashed` (so `FuelSystem` doesn't hardcode `Building` as the crash-source type) is still open — only the pickup/restore half of the original idea was folded into Pass 4. | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Open |
| Replace per-instance `IDamageDealer`/`Crashed` array wiring (manual Inspector drag-drop + cast-at-Awake on `FuelSystem`/`HealthSystem`) with a static/global event any damage dealer or crash source fires, subscribed to once — the array approach doesn't scale to procedurally spawned obstacles, which can't be hand-wired in the Inspector. Needed before obstacle *spawning* (as opposed to hand-placed instances) is implemented. Part of the "Core systems refactor" milestone. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |
| No overseeing system (`GameManager` or similar) exists yet — nothing owns run/game state above the individual resource systems. Part of the "Core systems refactor" milestone. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |
| `FuelSystem`/`HealthSystem` each independently subscribe to crash/damage sources and independently decide run-end (disabling `PlaneController`, zeroing velocity) — they should be pure resource managers (apply increments/decrements, raise a "depleted" event) with a `PlayerManager` (or equivalent) owning crash/damage/run-end orchestration: it subscribes to sources, tells Fuel/Health what to apply, and reacts to their depleted events itself. Part of the "Core systems refactor" milestone. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |
| Combo timer should also take a reduction for *missing* a building (flying past without crashing), not just for crashing hazards — needs a way to know a building existed and wasn't hit, which requires building spawning/tracking (not built yet, buildings are hand-placed). Implement once that spawner exists. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement | Open |
| Alchemy `[ShowInInspector]` debug fields (`CurrentFuel`, `CurrentScore`, `CurrentMultiplier`, `CurrentComboTimer`, etc.) don't visibly update in the Inspector during Play Mode — stuck at their initial value all playtest. The underlying values are confirmed correct (HUD, which reads the same `Observable*` instances via `AddListener`, updates live) — looks like an Inspector repaint issue in the Alchemy package rather than a logic bug, not yet root-caused. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug | Open |
| Mistimed tough-building hit (`!canBreak` branch) behaves inconsistently with `Building_Normal`'s always-destroys-and-scores pattern: currently the building survives (no `Destroy`), scores nothing at all (not even a base value — `Crashed` never fires), and only reaches `HealthSystem` (not wired into `FuelSystem.m_damageSourceBehaviours` or `ScoreSystem.m_hazardBehaviours`). Desired behavior per developer: no quality bonus + health damage + fuel penalty + combo-timer penalty + building still destroys (implying a base score like a normal-building body-hit gets, quality=0). **Two of the three penalties are pure Editor wiring today** — `Building` already implements `IDamageDealer` and already fires `DamageDealt` correctly, so dragging the `Building_Tough` instance into `FuelSystem.m_damageSourceBehaviours` and `ScoreSystem.m_hazardBehaviours` gets the fuel/combo penalties with no code change. The destroy+base-score part needs actual code (`Crashed` never fires on this branch) and a deliberate design call first — **making a mistimed hit always destroy the building removes "tough requires enough boost to break" as a hard-resistance mechanic**, turning "tough" into "harder to score cleanly + more punishing if mistimed" rather than "may not break at all." Needs explicit confirmation before implementing, not just flagging. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug/Design | Open |
| More generally: every `IDamageDealer` source should reach all three penalty systems (`HealthSystem`, `FuelSystem`, `ScoreSystem`'s combo timer) uniformly, but today it's manual opt-in per source per array — exactly how `Building_Tough` above ended up only wired to one of the three. Same root cause as the already-flagged static/global-event refactor (Core systems refactor milestone): per-instance array wiring makes partial/inconsistent wiring easy to end up with by accident. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |
| `FuelSystem`/`HealthSystem` both drain by the exact same raw `damage` value from any wired `IDamageDealer` source — no independent per-resource tuning (e.g. a hazard that should hurt health a lot but fuel only a little isn't expressible today). Minor, but worth a tunable multiplier or a second value on the interface if it matters once more hazard types exist. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement | Open |

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

### Milestone: Core systems refactor — In Progress
See [current-state.md](current-state.md) for live pass-by-pass status.

## Phase: Alpha — Upcoming
See [plan.md](plan.md#phase-alpha) for scope.

## Phase: Beta — Upcoming
See [plan.md](plan.md#phase-beta) for scope.

## Phase: Full Launch — Upcoming
See [plan.md](plan.md#phase-full-launch) for scope.
