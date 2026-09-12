# Movement

`PlaneController.cs` (`Assets/_Project/Scripts/Player/PlaneController.cs`), namespace `Opoint8182.Player`. Sibling component on the `Plane` prefab root alongside [FuelSystem](fuel-system.md).

## Base flight

Pitch is player-controlled (new Input System `Move` action's Y axis, clamped to `m_maxPitchAngle`), forward speed and lateral (X-axis) speed are constant tunables. `FixedUpdate` sets `Rigidbody.linearVelocity` directly every frame from the current pitch rotation — no acceleration/momentum model. `m_visualRoot` gets a separate cosmetic bank rotation driven by lateral input; it doesn't affect the physical Rigidbody.

## Boost (Milestone 2 Pass 2)

Reuses the existing `Sprint` action (`InputSystem_Actions.inputactions`, `Player` map — already bound to `<Keyboard>/leftShift`, `<Gamepad>/leftStickPress`, `<XRController>/trigger`) rather than adding a new action to that asset; `PlaneController.m_boostAction` just points at it. While held (`IsPressed()`, read once per `FixedUpdate` into the public `IsBoosting` property):
- `SpeedMultiplier` (previously an unconsumed seam, multiplied into forward velocity since Milestone 1) is set to `m_boostSpeedMultiplier` (default `1.6`) instead of `1`.
- Both `m_pitchRateDegPerSec` and `m_sideSpeed` are scaled down by `m_boostSteerMultiplier` (default `0.5`) — steering gets harder while boosting, per the design doc's risk-lever framing.

No separate "is boosting" flag is exposed to [Building](building-crash-system.md) — boosting just raises real impact speed, which is the same value `Building`'s tough-resist gate already reads. [FuelSystem](fuel-system.md) reads `IsBoosting` directly for its own drain multiplier.

**`m_boostAction` requires a one-time manual Editor wiring step, committed unassigned.** `InputActionReference` fields serialize as a reference to a sub-asset Unity generates inside the `.inputactions` file, keyed by a fileID computed internally (not listed in the asset's `.meta`, same reasoning [ui.md](ui.md) gives for not hand-authoring `PanelSettings`) — not safe to hand-derive. Assign it to `Player/Sprint` in the `Plane` prefab's Inspector before testing boost.
