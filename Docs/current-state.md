# Current State

## Snapshot
- **Last landed:** Pass 3 — Combo-timer missing-building penalty (`1542362`). Milestone "Difficulty ramp" (MVP phase) **complete** — all 3 scoped passes landed. See [progress.md](progress.md) for the frozen pass table.
- **Now:** Milestone "Feel polish" (MVP phase, last one before MVP is considered done — see [plan.md](plan.md)) — 7 passes scoped, 2 landed. Redefined beyond the original camera/juice-only scope to fold in backlog #16, #17, #18 plus new menu/mobile-input work (developer decision, 2026-09-23).

### Scoped passes — Feel polish
1. ~~**Lateral bounds**~~ — Landed `dd01b18`. Mirrors `AltitudeSystem`'s ceiling/ground pattern but for the horizontal axis, on `Opoint8182.Lateral.LateralSystem` (`Assets/_Project/Scripts/Lateral/`) — `Mathf.Abs(transform.position.x)` against `m_softBoundX`/`m_hardBoundX` (12/16, symmetric both sides, chosen against `SpawnManager.m_lateralRange`'s `10`). No countdown timer (direct developer correction from `AltitudeSystem`'s ceiling-warning pattern) — soft bound just toggles `IsWarningValue`, hard bound raises `HardBoundExceeded` once. Hard bound wired to fly-away (mirrors `AltitudeSystem.CeilingExceeded`: plane keeps its last velocity, camera stops following — not a freeze-in-place) on both sides; `PlayerManager`'s handler renamed `HandleFlyAway` since it's now shared by ceiling and lateral. New HUD element `LateralWarningUI` (`lateral-warning-root` in `LateralWarning.uxml`) added to the `HUD` GameObject in `Prototype.unity`, and the `LateralSystem` component added to `Plane.prefab`. Resolves backlog #18. See [lateral-system.md](technical-design/lateral-system.md).
2. ~~**Health HUD element**~~ — Landed `a78fc3b`. Structural copy of `FuelGaugeUI`/`FuelGauge.uxml`/`.uss` for `HealthSystem` (`HealthGaugeUI.cs`, `Assets/_Project/Scripts/UI/`) — `HealthSystem` already had the matching `HealthValue`/`HealthFraction` API, so no system changes needed, only the UI layer. Green fill bar positioned below the fuel gauge in the HUD (`top: 56px`, left column). New HUD child `HealthGauge` added to `Prototype.unity`. Resolves backlog #16. See [ui.md](technical-design/ui.md#health-gauge-feel-polish-pass-2).
3. **Android touch HUD** — not started. On-screen joystick + buttons for mobile input on the Android build.
4. **Main menu scene UI** — not started. New scene, UITK, placeholder UI (player name field, Play button, etc.) — real visuals/animated transitions deferred to the Alpha UI/art pass.
5. **Game flow** — not started. Splash → main menu → game scene transitions. Unity's default splash stands in until a dedicated splash scene is built to fully replace it.
6. **Spawn algorithm rework** — not started. Revisits `SpawnManager`'s known-incomplete Pass 2 design (independent-per-spawn weighted roll + single global ramp plateau). See `design-doc.md`'s "Difficulty ramp — engagement levers" parking-lot section for candidate fixes. Resolves backlog #17.
7. **Camera effects / juice** — not started. Shake, follow-tightening on boost, general juice. Last pass — MVP is considered done once this lands.

- **Before you test:** Reload the open scene (`Prototype.unity`) after script changes before entering Play Mode.

See [progress.md](progress.md) for the full project history and backlog, and [plan.md](plan.md) for what comes after this milestone.
