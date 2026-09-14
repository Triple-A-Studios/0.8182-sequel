# Current State

## Snapshot
- **Last landed:** Pass 5 — `PlayerManager` (`ee01e77`). Milestone "Core systems refactor" (MVP phase) complete — see [progress.md](progress.md).
- **Now:** Milestone "Movement & fail-state rework" (MVP phase) — 6 passes scoped, none started. No branch created yet; create `milestone/movement-fail-state-rework` before writing Pass 1 code.

### Scoped passes — Movement & fail-state rework
1. **Pitch-cosmetic / velocity-based vertical movement** — W/S becomes purely cosmetic (visual rotation only), mirroring how horizontal/yaw movement already works. Actual vertical movement uses `rb.linearVelocity` directly. Releasing the input returns both vertical velocity and cosmetic pitch to level.
2. **Crash-quality formula rework** — currently speed × angle-into-weak-point; simplified so players don't have to reason about precise crash angles. Exact replacement formula **not yet decided** — Pass 1 makes crash angle constant (pitch no longer reflects real trajectory), so design this once Pass 1 has landed and shows exactly what data is still available.
3. **Minimal Game Over screen** — no restart/game-over UI exists yet for any run-end today (not even the current fuel/health depletion path). Prerequisite for Pass 4.
4. **Altitude bounds** — new fail states. Ceiling: warn the player first, then end the run if they don't descend (plane flies off fast into the sky, camera stops following). Ground: instant run end on impact, same "blast" cosmetic treatment as a building crash or a health-zero death. Both show the Pass 3 Game Over screen.
5. **Crash-stutter physics fix** — plane momentarily stops on crash before `Destroy` runs, since both plane and building are colliders and physics resolves the impact first. (Backlog row, Pass 1 origin: Milestone "Building types + scoring".)
6. **Alchemy debug-inspector refresh fix** — `[ShowInInspector]` debug fields (`CurrentFuel`, `CurrentScore`, etc.) don't visibly update in the Inspector during Play Mode; underlying values and HUD are confirmed correct, looks like an Alchemy repaint issue. (Backlog row, Pass 3 origin: Milestone "Obstacles, combos, recovery".)

- **Known issues, not folded into this milestone:** see `progress.md` backlog — combo-timer missing-building penalty, blocked on the building spawner that "Difficulty ramp" (the milestone after this one) introduces.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
