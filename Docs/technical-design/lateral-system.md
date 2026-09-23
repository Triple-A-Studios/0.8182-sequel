# Lateral System

`LateralSystem.cs` (`Assets/_Project/Scripts/Lateral/LateralSystem.cs`), namespace `Opoint8182.Lateral`. Sibling component on the `Plane` prefab root alongside [PlaneController](movement.md), [FuelSystem](fuel-system.md), [HealthSystem](health-system.md), [AltitudeSystem](altitude-system.md). Same pure-sensor role as the others: it only reads `transform.position.x` and raises an event — it never touches `PlaneController`/`Rigidbody` itself. [`PlayerManager`](#run-end) owns what actually happens to the plane.

## One axis, two thresholds, one event

Mirrors `AltitudeSystem`'s ceiling/ground shape but on the horizontal axis, and deliberately simpler — a direct developer correction during scoping: no countdown/grace-period timer, just a plain in/out warning state.

`m_softBoundX` (default `12`) and `m_hardBoundX` (default `16`) are both plain `[SerializeField]` tunables, checked every `FixedUpdate` against `Mathf.Abs(transform.position.x)` — one pair of thresholds covers both left and right since the play field is symmetric around `x = 0` (same symmetry `SpawnManager`'s `m_lateralRange` already assumes for spawn positioning).

- Crossing `m_softBoundX` sets `IsWarningValue` (`ObservableBool`, same lazy-construction pattern as `AltitudeSystem.IsWarningValue`) true — no timer, just on while beyond the soft bound and below the hard bound, off otherwise.
- Crossing `m_hardBoundX` raises `public event Action HardBoundExceeded` exactly once, guarded the same way `AltitudeSystem.MarkCeilingExceeded`/`MarkGroundHit` guard their events.

## Run end

Matches `AltitudeSystem.CeilingExceeded`, not `GroundHit` — direct developer correction after playtesting the first cut (which froze in place like a depletion). `PlayerManager` wires `HardBoundExceeded` to `HandleFlyAway` (renamed from `HandleCeilingExceeded` once it started serving both sources): disables `PlaneController` and the follow camera's `CinemachineFollow`, but deliberately doesn't zero `linearVelocity` — the plane keeps flying off sideways in whatever direction it was last moving and drifts on forever (`Plane.prefab`'s Rigidbody has no gravity/drag), camera left behind. Same treatment on both the left and right hard bound, since `LateralSystem` only exposes one symmetric `HardBoundExceeded` event regardless of which side triggered it. `GameManager` subscribes `HardBoundExceeded` to its own cause-agnostic `HandleRunEnded`, same pattern as the other three fail sources — `GameOverUI` needed no changes.

## No VFX

Same fidelity as every other fail state in the project so far — no VFX on hard-bound hit (deferred to Feel Polish's camera-effects/juice pass, which lands later in this same milestone).
