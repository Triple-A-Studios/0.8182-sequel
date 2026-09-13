# Progress

## Backlog

| Item | Origin | Type | Status |
|---|---|---|---|
| Plane momentarily stops on crash — both plane and building are colliders, physics resolves a full stop before `Destroy` | [Pass 1, Milestone: Building types + scoring](current-state.md) | Bug | Open |
| Extend the `IDamageDealer` pattern to other resource-affecting sources (e.g. a refuel-source interface for `Building.Crashed`, a restore interface for future health/fuel pickups) so `FuelSystem`/`HealthSystem` don't hardcode concrete source types — worth doing once pickups/obstacles actually get built (Milestone 3+), not before | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Open |
| Replace per-instance `IDamageDealer`/`Crashed` array wiring (manual Inspector drag-drop + cast-at-Awake on `FuelSystem`/`HealthSystem`) with a static/global event any damage dealer or crash source fires, subscribed to once — the array approach doesn't scale to procedurally spawned obstacles, which can't be hand-wired in the Inspector. Needed before obstacle *spawning* (as opposed to hand-placed instances) is implemented. Part of the "Core systems refactor" milestone. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |
| No overseeing system (`GameManager` or similar) exists yet — nothing owns run/game state above the individual resource systems. Part of the "Core systems refactor" milestone. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |
| `FuelSystem`/`HealthSystem` each independently subscribe to crash/damage sources and independently decide run-end (disabling `PlaneController`, zeroing velocity) — they should be pure resource managers (apply increments/decrements, raise a "depleted" event) with a `PlayerManager` (or equivalent) owning crash/damage/run-end orchestration: it subscribes to sources, tells Fuel/Health what to apply, and reacts to their depleted events itself. Part of the "Core systems refactor" milestone. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Open |

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

### Milestone: Obstacles, combos, recovery — In Progress
See [current-state.md](current-state.md) for live pass status.

## Phase: Alpha — Upcoming
See [plan.md](plan.md#phase-alpha) for scope.

## Phase: Beta — Upcoming
See [plan.md](plan.md#phase-beta) for scope.

## Phase: Full Launch — Upcoming
See [plan.md](plan.md#phase-full-launch) for scope.
