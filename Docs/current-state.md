# Current State

## Snapshot
- **Last landed:** Pass 3 — Per-resource damage tuning (`56a9ab7`). Milestone "Core systems refactor" (MVP phase), branch `milestone/core-systems-refactor`.
- **Now:** Pass 4 next — `GameManager`.

### Scoped passes — Core systems refactor
1. ~~**Event-driven damage/restore wiring**~~ Done (`0118f74`) — New `Common/CombatEvents.cs` static event bus (`DamageDealt`/`Restored`/`Crashed`) and `Common/ICrashSource.cs` interface replace all per-instance Inspector array wiring in `FuelSystem`/`HealthSystem`/`ScoreSystem`; `Building` implements `ICrashSource` instead of `FuelSystem`/`ScoreSystem` hardcoding the concrete type. `IDamageDealer`/`IRestorer` dropped their per-instance events — sources fire through `CombatEvents` directly. Runtime behavior unchanged (mistimed tough-hit logic untouched, that's Pass 2). Note: `ScoreSystem` now also receives `Building`'s tough-mistimed-hit damage via the global bus (previously unwired) — expected side effect, not a bug, matches Pass 2/backlog row 16's uniform-wiring goal.
2. ~~**Uniform penalty wiring + tough-building mistimed-hit fix**~~ Done (`2c22213`) — `CombatEvents.Crashed`/`RaiseCrashed` gained a `countsForCombo` flag (default `true`). `Building`'s mistimed-hit branch now fires `RaiseDamageDealt` (health/fuel penalty, unchanged) followed by `RaiseCrashed(this, 0f, countsForCombo: false)` and destroys the building — `ScoreSystem.HandleCrashed` always adds score but skips the chain-count/timer-reset/multiplier-step reward block when `countsForCombo` is false, so the hit scores a base value and destroys the building without rewarding the combo (the existing hazard-tick penalty from `DamageDealt` still applies to the combo timer). Confirmed design: "requires enough boost" is now a scoring-quality gate, not a hard-resistance gate.
3. ~~**Per-resource damage tuning**~~ Done (`56a9ab7`) — `IDamageDealer` gained `HealthDamageMultiplier`/`FuelDamageMultiplier` as C# 8 default interface members (both default `1f`); `FuelSystem`/`HealthSystem` multiply incoming damage by the respective multiplier. `Building`/`ObstacleBuilding`/`BirdHazard` needed no changes — behavior unchanged until a future source overrides one of the defaults.
4. **`GameManager`** — Overseeing system for run/game state (run active/ended), above the individual resource systems.
5. **`PlayerManager`** — Owns crash/damage/run-end orchestration: subscribes to sources via the Pass 1 event, tells `FuelSystem`/`HealthSystem` what to apply, reacts to their "depleted" events to end the run (disabling `PlaneController`, zeroing velocity). `FuelSystem`/`HealthSystem` become pure resource managers with no run-end logic of their own.

- **Known issues, not folded into this milestone:** see `progress.md` backlog — plane momentarily stops on crash before `Destroy` (physics bug); combo-timer missing-building penalty (blocked on a building spawner that doesn't exist yet); Alchemy `[ShowInInspector]` debug fields not refreshing live in Play Mode (Inspector repaint issue, not a logic bug).
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
