# Movement

`PlaneController.cs` (`Assets/_Project/Scripts/Player/PlaneController.cs`), namespace `Opoint8182.Player`. Sibling component on the `Plane` prefab root alongside [FuelSystem](fuel-system.md).

## Base flight

Forward speed, lateral (X-axis) speed, and vertical (Y-axis) speed are all constant tunables (`m_forwardSpeed`, `m_sideSpeed`, `m_verticalSpeed`). `FixedUpdate` sets `Rigidbody.linearVelocity` directly every frame as the sum of three world-space terms — forward, lateral (from `Move` action X), vertical (from `Move` action Y) — no acceleration/momentum model, and no rotation involved at all: the Rigidbody's actual rotation never changes (there's no yaw either).

Pitch (`Move` Y) and bank (`Move` X) are both **purely cosmetic** — same pattern, mirrored on two axes. Each frame, input maps directly to a target angle (`-input * m_maxPitchAngle` / `-input * m_maxBankAngle`), and `m_currentPitchAngle`/`m_currentBankAngle` move toward that target via `Mathf.MoveTowards` at a fixed rate (`m_pitchSpeedDegPerSec` / `m_bankSpeedDegPerSec`). Both angles are applied in one `m_visualRoot.localRotation = Quaternion.Euler(pitch, 0, bank)` assignment — `m_visualRoot` is optional (null-checked) and never touches the physical Rigidbody. Releasing input returns both real vertical velocity (instantly, since the input term goes straight to 0) and cosmetic pitch (smoothly, via the same `MoveTowards` call) to level.

Pitch was the only source of physical rotation until Pass 1 of the "Movement & fail-state rework" milestone made it cosmetic-only — this was a deliberate step to make crash-impact angle constant, since the crash-quality formula (see [building-crash-system.md](building-crash-system.md)) is being reworked to no longer depend on precise crash angle.

## Boost (Milestone 2 Pass 2)

Reuses the existing `Sprint` action (`InputSystem_Actions.inputactions`, `Player` map — already bound to `<Keyboard>/leftShift`, `<Gamepad>/leftStickPress`, `<XRController>/trigger`) rather than adding a new action to that asset; `PlaneController.m_boostAction` just points at it. While held (`IsPressed()`, read once per `FixedUpdate` into the public `IsBoosting` property):
- `SpeedMultiplier` (previously an unconsumed seam, multiplied into forward velocity since Milestone 1) is set to `m_boostSpeedMultiplier` (default `1.6`) instead of `1`.
- Both `m_pitchRateDegPerSec` and `m_sideSpeed` are scaled down by `m_boostSteerMultiplier` (default `0.5`) — steering gets harder while boosting, per the design doc's risk-lever framing.

No separate "is boosting" flag is exposed to [Building](building-crash-system.md) — boosting just raises real impact speed, which is the same value `Building`'s tough-resist gate already reads. [FuelSystem](fuel-system.md) reads `IsBoosting` directly for its own drain multiplier.

**`m_boostAction` requires a one-time manual Editor wiring step, committed unassigned.** `InputActionReference` fields serialize as a reference to a sub-asset Unity generates inside the `.inputactions` file, keyed by a fileID computed internally (not listed in the asset's `.meta`, same reasoning [ui.md](ui.md) gives for not hand-authoring `PanelSettings`) — not safe to hand-derive. Assign it to `Player/Sprint` in the `Plane` prefab's Inspector before testing boost.
