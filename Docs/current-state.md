# Current State

## Snapshot
- **Last landed:** Pass 1 — Procedural spawner (`93874e1`). Milestone "Difficulty ramp" (MVP phase) **in progress** — 1 of 3 scoped passes landed.
- **Now:** Working **Pass 2 — Difficulty ramp curve** (not yet designed — density/type-mix scaling with `SpawnManager.DistanceTraveled`).

### Scoped passes — Difficulty ramp
1. ~~**Procedural spawner**~~ — Landed `93874e1`. Replaced the 10 hand-placed `Building_Normal`/`Building_Tough`/`Obstacle_Large`/`Bird_Small`/`HealthPickup` instances in `Prototype.unity` with a runtime `SpawnManager` (`Assets/_Project/Scripts/Spawning/`) that spawns them ahead of the plane along +Z and culls them once they fall behind, distance-based (not timer-based) so it scales with boost speed. Density/type-mix stays a uniform-random placeholder across all 5 types. Vertical spawn range is per-entry (`SpawnableEntry.VerticalMin`/`VerticalMax`), not manager-wide — ground-pivot buildings/obstacle need `0/0`, small centered-pivot bird/pickup need an off-ground range; a single shared range couldn't satisfy both (caught during manual verification). `Building_Normal`/`Building_Tough`/`Bird_Small`/`Obstacle_Large` prefabs got matching pivot-offset tuning (visual child moved up so the base-pivot root sits correctly at `y=0`). See [spawning.md](technical-design/spawning.md).
2. **Difficulty ramp curve** — not yet designed. Density and type-mix (Normal→Tough ratio, obstacle/bird frequency) scale up with `SpawnManager.DistanceTraveled` (exposed by Pass 1 for exactly this).
3. **Combo-timer missing-building penalty** — not yet designed. Folds backlog #7 (`progress.md`) — Pass 1's `SpawnEvents.EntityCulled` already fires only for buildings that fell behind unhit, so this pass is mostly `ScoreSystem` subscribing to it.

- **Known issues, not folded into this milestone:** none currently open.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
