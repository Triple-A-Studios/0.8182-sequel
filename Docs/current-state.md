# Current State

## Snapshot
- **Last landed:** Pass 3 — Combo-timer missing-building penalty (`1542362`). Milestone "Difficulty ramp" (MVP phase) **complete** — all 3 scoped passes landed. See [progress.md](progress.md) for the frozen pass table.
- **Now:** Milestone "Feel polish" (MVP phase, last one before MVP is considered done — see [plan.md](plan.md)) — 7 passes scoped, none landed yet. Redefined beyond the original camera/juice-only scope to fold in backlog #16, #17, #18 plus new menu/mobile-input work (developer decision, 2026-09-23).

### Scoped passes — Feel polish
1. **Lateral bounds** — not started. Mirrors `AltitudeSystem`'s ceiling/ground pattern but for the horizontal axis: a soft limit displays a warning, a hard limit past it ends the run. Resolves backlog #18.
2. **Health HUD element** — not started. Mirrors `FuelGaugeUI`'s pattern for `HealthSystem` — no on-screen health readout exists today. Resolves backlog #16.
3. **Android touch HUD** — not started. On-screen joystick + buttons for mobile input on the Android build.
4. **Main menu scene UI** — not started. New scene, UITK, placeholder UI (player name field, Play button, etc.) — real visuals/animated transitions deferred to the Alpha UI/art pass.
5. **Game flow** — not started. Splash → main menu → game scene transitions. Unity's default splash stands in until a dedicated splash scene is built to fully replace it.
6. **Spawn algorithm rework** — not started. Revisits `SpawnManager`'s known-incomplete Pass 2 design (independent-per-spawn weighted roll + single global ramp plateau). See `design-doc.md`'s "Difficulty ramp — engagement levers" parking-lot section for candidate fixes. Resolves backlog #17.
7. **Camera effects / juice** — not started. Shake, follow-tightening on boost, general juice. Last pass — MVP is considered done once this lands.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
