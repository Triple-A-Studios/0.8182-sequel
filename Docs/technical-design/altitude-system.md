# Altitude System

`AltitudeSystem.cs` (`Assets/_Project/Scripts/Altitude/AltitudeSystem.cs`), namespace `Opoint8182.Altitude`. Sibling component on the `Plane` prefab root alongside [PlaneController](movement.md), [FuelSystem](fuel-system.md), [HealthSystem](health-system.md). A pure sensor, same role `FuelSystem`/`HealthSystem` settled into after the Core systems refactor: it only reads `transform.position.y` and raises events — it never touches `PlaneController`/`Rigidbody` itself. [`PlayerManager`](#run-end) owns what actually happens to the plane.

## Two independent thresholds, two distinct events

`m_ceilingY` (default `30`) and `m_groundY` (default `-1`, matching the scene's `Ground` mesh's actual top surface) are both plain `[SerializeField]` tunables, checked every `FixedUpdate` against `transform.position.y`. Crossing either raises its own parameterless event exactly once, guarded the same way `HealthSystem.MarkDepleted`/`FuelSystem.MarkDepleted` guard `Depleted`:

- `public event Action GroundHit` — instant, no grace period.
- `public event Action CeilingExceeded` — only after the ceiling warning countdown (below) expires.

Kept as two separate events rather than one generic "altitude depleted," specifically because [`PlayerManager`](#run-end) needs to react differently per cause (ground freezes the plane in place, ceiling lets it fly on) even though [`GameManager`](#run-end)/`GameOverUI` don't care which one fired.

Y-threshold checks were chosen over a physical ground collider deliberately — the scene's `Ground` GameObject does have a real `MeshCollider`, but using it as the fail trigger would inherit the project's known crash-stutter physics bug (`Docs/progress.md` backlog: plane momentarily stops before `Destroy` runs, since both colliders resolve a full stop first). A position check sidesteps that whole bug class and is symmetric with the "altitude bounds" framing (an upper and lower Y limit, not a collision).

## Ceiling warning: pause, don't just reset

While `transform.position.y >= m_ceilingY`, a countdown (`m_ceilingWarningSeconds`, default `4`) ticks down each `FixedUpdate`. Reaching zero raises `CeilingExceeded`. The countdown has three states, not two:

- **Descending** (`PlaneController.Velocity.y < 0`, i.e. actively holding the down input): countdown **pauses in place** — doesn't tick down, doesn't reset. Releasing the down input resumes ticking from wherever it paused.
- **Above ceiling, not descending**: countdown ticks down normally.
- **Below `m_ceilingY` (safe bounds)**: countdown **fully resets** to `m_ceilingWarningSeconds`.

Descent detection reads `PlaneController.Velocity` (already public) rather than the raw input action directly — `AltitudeSystem` already requires `PlaneController` as a sibling, and velocity is a strictly simpler dependency than reaching into the Input System action asset.

The countdown is exposed as `ObservableBool IsWarningValue` + `ObservableFloat WarningFractionValue` (both `TripleA.Utils.Observables.Primaries`, same lazy-construction pattern as `FuelSystem.Fuel`) — two observables rather than a fraction alone, since a fraction can't distinguish "freshly reset to 1.0" from "not warning at all" (both read `1.0`). [`AltitudeWarningUI`](ui.md#altitude-warning-movement--fail-state-rework-pass-4) binds to both.

## Run end

`AltitudeSystem` itself does nothing when either event fires — all orchestration lives in `PlayerManager` (`Assets/_Project/Scripts/Player/PlayerManager.cs`), which already owns crash/damage/run-end handling for `FuelSystem`/`HealthSystem`'s `Depleted` (see [building-crash-system.md](building-crash-system.md#seam-for-later-passes) for the wider event chain). `PlayerManager` wires the two new events differently:

- `GroundHit` → the existing `HandleDepleted` (same freeze-in-place as fuel/health-zero: disable `PlaneController`, zero `linearVelocity`). No new method needed — ground's fail behavior is identical to the two existing depletion paths.
- `CeilingExceeded` → `HandleFlyAway` (renamed from `HandleCeilingExceeded` once [`LateralSystem`](lateral-system.md#run-end)'s hard bound started sharing it): disables `PlaneController` and disables the follow camera's `CinemachineFollow` component (`[SerializeField] private CinemachineFollow m_followCamera`, wired in the scene to `CM Plane Follow Cam`'s `CinemachineFollow` — this reference can't live on `Plane.prefab` since the vcam is scene-only). Deliberately **does not** touch `linearVelocity` — the plane keeps whatever velocity it was last actually moving at (forward, diagonal, whatever the player was doing) and drifts on forever, since `Plane.prefab`'s Rigidbody has no gravity or drag. This was a direct developer correction to an initial "force straight up" implementation — the design doc's "flies off fast into the sky" is a dramatic bystander description, not a literal upward-launch spec.

`GameManager` subscribes both `CeilingExceeded` and `GroundHit` to its own cause-agnostic `HandleRunEnded` (same `FindAnyObjectByType`/subscribe pattern already used for `FuelSystem`/`HealthSystem`), so `GameOverUI` needed no changes at all — it only ever reacts to `GameManager.RunEnded`.

## No VFX

"Same cosmetic 'blast' treatment as a building crash or a health-zero death" (design doc) currently means no VFX at all — building crashes and health/fuel-zero deaths have none yet ("deferred to Feel Polish milestone," see [building-crash-system.md](building-crash-system.md#seam-for-later-passes)). Ground-hit matches that same fidelity; no new visual work landed with this pass.
