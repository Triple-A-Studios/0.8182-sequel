# Current State

## Snapshot
- **Last landed:** Pass 2 — crash-quality formula simplified to speed-only (`cd40dcd`). Milestone "Movement & fail-state rework" (MVP phase) in progress — see [progress.md](progress.md).
- **Now:** Milestone "Movement & fail-state rework" (MVP phase), Pass 3 next (not started) — branch `milestone/movement-and-fail-state-rework` already checked out.

### Scoped passes — Movement & fail-state rework
1. ~~**Pitch-cosmetic / velocity-based vertical movement**~~ — Landed `a5f58c0`. `m_verticalSpeed` drives real vertical velocity directly (mirrors `m_sideSpeed`); pitch is now purely cosmetic via a `MoveTowards`-driven angle on `m_visualRoot`, mirroring the existing bank pattern exactly — combined into one `Quaternion.Euler(pitch, 0, bank)` assignment. `m_rigidbody.MoveRotation` and `m_pitchDeg` are gone; the Rigidbody no longer physically rotates at all. `m_pitchRateDegPerSec` renamed to `m_pitchSpeedDegPerSec` (`[FormerlySerializedAs]` preserves the prefab's tuned value). See [movement.md](technical-design/movement.md) for the rewritten spec.
2. ~~**Crash-quality formula rework**~~ — Landed `cd40dcd`. Formula: speed-only, gated on hitting the weak point (`quality = hitWeakPoint ? speed01 : 0f`), dropping the angle-alignment term entirely since cosmetic-only pitch (Pass 1) made angle unreadable by feel. `WeakPoint.OutwardNormal` removed as dead code. See [building-crash-system.md](technical-design/building-crash-system.md#crash-quality-formula). Verification also caught in-editor tuning: plane collider box->capsule, forward speed 20->25, `Building_Normal.referenceMaxSpeed` 25->10.
3. **Minimal Game Over screen** — no restart/game-over UI exists yet for any run-end today (not even the current fuel/health depletion path). Prerequisite for Pass 4.
4. **Altitude bounds** — new fail states. Ceiling: warn the player first, then end the run if they don't descend (plane flies off fast into the sky, camera stops following). Ground: instant run end on impact, same "blast" cosmetic treatment as a building crash or a health-zero death. Both show the Pass 3 Game Over screen.
5. **Crash-stutter physics fix** — plane momentarily stops on crash before `Destroy` runs, since both plane and building are colliders and physics resolves the impact first. (Backlog row, Pass 1 origin: Milestone "Building types + scoring".)
6. **Alchemy debug-inspector refresh fix** — `[ShowInInspector]` debug fields (`CurrentFuel`, `CurrentScore`, etc.) don't visibly update in the Inspector during Play Mode; underlying values and HUD are confirmed correct, looks like an Alchemy repaint issue. (Backlog row, Pass 3 origin: Milestone "Obstacles, combos, recovery".)

- **Known issues, not folded into this milestone:** see `progress.md` backlog — combo-timer missing-building penalty, blocked on the building spawner that "Difficulty ramp" (the milestone after this one) introduces.
- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
