# Movement

`PlaneController.cs` (`Assets/_Project/Scripts/Player/PlaneController.cs`), namespace `Opoint8182.Player`. Sibling component on the `Plane` prefab root alongside [FuelSystem](fuel-system.md).

## Base flight

Forward speed, lateral (X-axis) speed, and vertical (Y-axis) speed are all constant tunables (`m_forwardSpeed`, `m_sideSpeed`, `m_verticalSpeed`). `FixedUpdate` sets `Rigidbody.linearVelocity` directly every frame as the sum of three world-space terms — forward, lateral (from `Move` action X), vertical (from `Move` action Y) — no acceleration/momentum model, and no rotation involved at all: the Rigidbody's actual rotation never changes (there's no yaw either).

Pitch (`Move` Y) and bank (`Move` X) are both **purely cosmetic** — same pattern, mirrored on two axes. Each frame, input maps directly to a target angle (`-input * m_maxPitchAngle` / `-input * m_maxBankAngle`), and `m_currentPitchAngle`/`m_currentBankAngle` move toward that target via `Mathf.MoveTowards` at a fixed rate (`m_pitchSpeedDegPerSec` / `m_bankSpeedDegPerSec`). Both angles are applied in one `m_visualRoot.localRotation = Quaternion.Euler(pitch, 0, bank)` assignment — `m_visualRoot` is optional (null-checked) and never touches the physical Rigidbody. Releasing input returns both real vertical velocity (instantly, since the input term goes straight to 0) and cosmetic pitch (smoothly, via the same `MoveTowards` call) to level.

Pitch was the only source of physical rotation until Pass 1 of the "Movement & fail-state rework" milestone made it cosmetic-only — this was a deliberate step to make crash-impact angle constant, since the crash-quality formula (see [building-crash-system.md](building-crash-system.md)) is being reworked to no longer depend on precise crash angle.

**Rigidbody rotation is frozen** (`Plane.prefab` `m_Constraints: 112`, Freeze Rotation X/Y/Z — Alpha "MVP bug fix" milestone Pass 2.d). All visible rotation is cosmetic via `m_visualRoot`, so the physical body must never rotate. Without the constraint, steering while in solid contact with a non-trigger collider (the ground, `Obstacle_Large`) let PhysX resolve the contact with torque against the force-set `linearVelocity`, spinning the whole plane. Reproduced in-Editor, not Android-specific.

## Boost (Milestone 2 Pass 2)

Reuses the existing `Sprint` action (`InputSystem_Actions.inputactions`, `Player` map — already bound to `<Keyboard>/leftShift`, `<Gamepad>/leftStickPress`, `<XRController>/trigger`) rather than adding a new action to that asset; `PlaneController.m_boostAction` just points at it. While held (`IsPressed()`, read once per `FixedUpdate` into the public `IsBoosting` property):
- `SpeedMultiplier` (previously an unconsumed seam, multiplied into forward velocity since Milestone 1) is set to `m_boostSpeedMultiplier` (default `1.6`) instead of `1`.
- Both `m_pitchRateDegPerSec` and `m_sideSpeed` are scaled down by `m_boostSteerMultiplier` (default `0.5`) — steering gets harder while boosting, per the design doc's risk-lever framing.

No separate "is boosting" flag is exposed to [Building](building-crash-system.md) — boosting just raises real impact speed, which is the same value `Building`'s tough-resist gate already reads. [FuelSystem](fuel-system.md) reads `IsBoosting` directly for its own drain multiplier.

**`m_boostAction` requires a one-time manual Editor wiring step, committed unassigned.** `InputActionReference` fields serialize as a reference to a sub-asset Unity generates inside the `.inputactions` file, keyed by a fileID computed internally (not listed in the asset's `.meta`, same reasoning [ui.md](ui.md) gives for not hand-authoring `PanelSettings`) — not safe to hand-derive. Assign it to `Player/Sprint` in the `Plane` prefab's Inspector before testing boost.

## Touch input (Feel polish, Pass 3)

`TouchSteer` (`Vector2`) and `TouchBoost` (`bool`) are public settable properties, both defaulting to zero/`false` — written externally by the HUD's touch controls (`HudTouchControls` via `HudPresenter`, formerly `TouchControlsUI`; see [ui.md](ui.md#gameplay-hud)), never read from an `InputActionReference`. `PlaneController` has no dependency on the `UI` namespace or Input System touch bindings; it just exposes two properties, same shape as the existing `SpeedMultiplier { get; set; }`.

`FixedUpdate` **additively merges** touch with the physical `Move`/`Sprint` reads rather than branching on platform: `m_steerInput = Vector2.ClampMagnitude(m_moveAction.action.ReadValue<Vector2>() + TouchSteer, 1f)`, and `IsBoosting = m_boostAction.action.IsPressed() || TouchBoost`. Both input paths are always live simultaneously — no platform-detection code exists, and the on-screen touch HUD can be exercised with a mouse in the Editor for free since it's driven by UI Toolkit pointer events (which fire for mouse input too), not a Touch-specific Input System binding.

## Touch control settings (Pause, settings & audio, Pass 6)

The four control settings in `SettingsService` are applied (the Settings screen's live preview uses the same formulas):

- **Sensitivity** (0.5-1.4, default 0.9): `HudTouchControls` multiplies the finger offset by it. The handle follows the scaled offset (clamped to the travel) and steer is `clamp(offset, fullRadius) / fullRadius`, so a higher value reaches full deflection with a smaller finger movement. The default is a hair softer than the old fixed 1.0.
- **Size** (S / M / L = 0.8 / 1.0 / 1.2): scales the base (300 px at M), handle (132), handle travel (84) and the full-steer radius (50) together, applied inline over the `Hud.uss` M defaults at each touch-down. Size and sensitivity are read from `SettingsService` at every touch-down, so they are never stale.
- **Side**: `HudView.ApplyControlSettings()` toggles `hud-root--right-hand` on the HUD root: the joystick zone moves to the right half and the boost button mirrors to the bottom-left. The HUD view is built at scene load, before the player can change settings in the menu, so `HudPresenter` calls it when the view is built and on `SettingsService.Changed`.
- **Invert steering**: `PlaneController.FixedUpdate` negates the final vertical steer after the dead zone, for touch and keyboard alike (vertical axis only); the visual pitch is derived from it and follows.
