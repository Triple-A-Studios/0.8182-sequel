# Progress

## Backlog

| Item | Origin | Type | Status |
|---|---|---|---|
| Plane momentarily stops on crash — both plane and building are colliders, physics resolves a full stop before `Destroy` | [Pass 1, Milestone: Building types + scoring](current-state.md) | Bug | Open |
| Extend the `IDamageDealer` pattern to other resource-affecting sources (e.g. a refuel-source interface for `Building.Crashed`, a restore interface for future health/fuel pickups) so `FuelSystem`/`HealthSystem` don't hardcode concrete source types — worth doing once pickups/obstacles actually get built (Milestone 3+), not before | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Open |

## Phase: Prototype

### Milestone: Core loop, bare minimum — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `b482138` | Plane movement + joystick steering |
| 2 | Verified — Complete | `01fc0ad` (crash-quality fix: `4f463f8`) | Single crashable building + weak-point detection |
| 3 | Verified — Complete | `ad1cfde` | Fuel system (drain + crash-precision refuel + run-end at zero) |
| 4 | Verified — Complete | `ad1cfde` | Minimal UI (fuel gauge) |

### Milestone: Building types + scoring — In Progress
See [current-state.md](current-state.md) for live pass status.

## Phase: MVP — Upcoming
See [plan.md](plan.md#phase-mvp) for scope.

## Phase: Alpha — Upcoming
See [plan.md](plan.md#phase-alpha) for scope.

## Phase: Beta — Upcoming
See [plan.md](plan.md#phase-beta) for scope.

## Phase: Full Launch — Upcoming
See [plan.md](plan.md#phase-full-launch) for scope.
