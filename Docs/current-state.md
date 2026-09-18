# Current State

## Snapshot
- **Last landed:** Pass 6 — Alchemy debug-inspector fields removed (`b7a8725`). Milestone "Movement & fail-state rework" (MVP phase) **complete** — all 6 scoped passes landed. See [progress.md](progress.md) for the frozen pass table.
- **Now:** Milestone "Difficulty ramp" (MVP phase) is active — 3 passes scoped (below). Currently working **Pass 1 — Procedural spawner**.

### Scoped passes — Difficulty ramp
1. **Procedural spawner** — in progress. Replaces the 10 hand-placed `Building_Normal`/`Building_Tough`/`Obstacle_Large`/`Bird_Small`/`HealthPickup` instances in `Prototype.unity` with a runtime `SpawnManager` (`Assets/_Project/Scripts/Spawning/`) that spawns them ahead of the plane along +Z and culls them once they fall behind, distance-based (not timer-based) so it scales with boost speed. Density/type-mix stays a uniform-random placeholder across all 5 types — the actual ramp curve (and any weighting to make health pickups rarer than hazards) is Pass 2. See [spawning.md](technical-design/spawning.md).
2. **Difficulty ramp curve** — not yet designed. Density and type-mix (Normal→Tough ratio, obstacle/bird frequency) scale up with `SpawnManager.DistanceTraveled` (exposed by Pass 1 for exactly this).
3. **Combo-timer missing-building penalty** — not yet designed. Folds backlog #7 (`progress.md`) — Pass 1's `SpawnEvents.EntityCulled` already fires only for buildings that fell behind unhit, so this pass is mostly `ScoreSystem` subscribing to it.

- **Known issues, not folded into this milestone:** none currently open — backlog #7 is being folded into Pass 3 above.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
