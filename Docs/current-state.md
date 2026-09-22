# Current State

## Snapshot
- **Last landed:** Pass 2 — Difficulty ramp curve (`c5e89b5`). Milestone "Difficulty ramp" (MVP phase) **in progress** — 2 of 3 scoped passes landed.
- **Now:** Working **Pass 3 — Combo-timer missing-building penalty** (folds backlog #7).

### Scoped passes — Difficulty ramp
1. ~~**Procedural spawner**~~ — Landed `93874e1`. Replaced the 10 hand-placed `Building_Normal`/`Building_Tough`/`Obstacle_Large`/`Bird_Small`/`HealthPickup` instances in `Prototype.unity` with a runtime `SpawnManager` (`Assets/_Project/Scripts/Spawning/`) that spawns them ahead of the plane along +Z and culls them once they fall behind, distance-based (not timer-based) so it scales with boost speed. Vertical spawn range is per-entry (`SpawnableEntry.VerticalMin`/`VerticalMax`) — ground-pivot buildings/obstacle need `0/0`, small centered-pivot bird/pickup need an off-ground range. See [spawning.md](technical-design/spawning.md).
2. ~~**Difficulty ramp curve**~~ — Landed `c5e89b5`. Per-entry `AnimationCurve` weights + a shared `ComputeWeight`/`ClampProgressAtRampCap` mechanism replace Pass 1's uniform pick — 4 types (Normal/Tough/Obstacle/Bird) ramp toward tougher/denser and hold once `m_difficultyRampDistance` caps; `HealthPickup` stays flat through the ramp then keeps decaying to a non-zero floor past it. Spawn interval also curves denser over distance. Verified "okay for now" by the developer, but **flagged as an incomplete design, not a finished one** — see backlog #17 and `design-doc.md`'s "Difficulty ramp — engagement levers" parking-lot section for concrete follow-up ideas (staggered plateaus, post-plateau oscillator, chunk spawning, etc.). See [spawning.md](technical-design/spawning.md).
3. **Combo-timer missing-building penalty** — not yet designed. Folds backlog #7 (`progress.md`) — Pass 1's `SpawnEvents.EntityCulled` already fires only for buildings that fell behind unhit, so this pass is mostly `ScoreSystem` subscribing to it.

- **Known issues, not folded into this milestone:** backlog #17 — spawn algorithm needs revisiting (see Pass 2 note above); backlog #15/#16 (healing building, health HUD) are separate features, not blocking.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
