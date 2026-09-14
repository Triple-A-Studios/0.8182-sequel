# Current State

## Snapshot
- **Last landed:** Pass 1 — Event-driven damage/restore wiring (`0118f74`). Milestone "Core systems refactor" (MVP phase), branch `milestone/core-systems-refactor`.
- **Now:** Pass 2 next — uniform penalty wiring + tough-building mistimed-hit fix.

### Scoped passes — Core systems refactor
1. ~~**Event-driven damage/restore wiring**~~ Done (`0118f74`) — New `Common/CombatEvents.cs` static event bus (`DamageDealt`/`Restored`/`Crashed`) and `Common/ICrashSource.cs` interface replace all per-instance Inspector array wiring in `FuelSystem`/`HealthSystem`/`ScoreSystem`; `Building` implements `ICrashSource` instead of `FuelSystem`/`ScoreSystem` hardcoding the concrete type. `IDamageDealer`/`IRestorer` dropped their per-instance events — sources fire through `CombatEvents` directly. Runtime behavior unchanged (mistimed tough-hit logic untouched, that's Pass 2). Note: `ScoreSystem` now also receives `Building`'s tough-mistimed-hit damage via the global bus (previously unwired) — expected side effect, not a bug, matches Pass 2/backlog row 16's uniform-wiring goal.
2. **Uniform penalty wiring + tough-building mistimed-hit fix** — Every damage source reaches Health/Fuel/Score-combo uniformly via the Pass 1 event. `Building_Tough`'s `!canBreak` branch now fires `Crashed` with quality=0 (destroys, base score) alongside the health penalty — confirmed design: mistimed tough-building hits still destroy the building, "requires enough boost" stays a scoring-quality gate, not a hard-resistance gate. (Folds backlog rows: uniform 3-system wiring, mistimed tough-hit design.)
3. **Per-resource damage tuning** — Tunable multiplier/second value on the damage interface so a source can hurt fuel vs. health by different amounts. (Folds backlog row: per-resource damage tuning.)
4. **`GameManager`** — Overseeing system for run/game state (run active/ended), above the individual resource systems.
5. **`PlayerManager`** — Owns crash/damage/run-end orchestration: subscribes to sources via the Pass 1 event, tells `FuelSystem`/`HealthSystem` what to apply, reacts to their "depleted" events to end the run (disabling `PlaneController`, zeroing velocity). `FuelSystem`/`HealthSystem` become pure resource managers with no run-end logic of their own.

- **Known issues, not folded into this milestone:** see `progress.md` backlog — plane momentarily stops on crash before `Destroy` (physics bug); combo-timer missing-building penalty (blocked on a building spawner that doesn't exist yet); Alchemy `[ShowInInspector]` debug fields not refreshing live in Play Mode (Inspector repaint issue, not a logic bug).
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
