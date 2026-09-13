# Fuel System

`FuelSystem.cs` (`Assets/_Project/Scripts/Fuel/FuelSystem.cs`), namespace `Opoint8182.Fuel`. Sibling component on the `Plane` prefab root alongside `PlaneController` — fuel is the player's plane's resource, not a scene-level manager concern.

## Drain and refuel

Drains at a tunable `drainPerSecond` every frame. Refuels on `Building.Crashed` (see [building-crash-system.md](building-crash-system.md)):

```
newFuel = Mathf.Clamp(currentFuel + quality * maxFuel, 0, maxFuel)
```

`Crashed` widened to `Action<float, int>` in Milestone 3 Pass 3 (quality + the building's `ScoreValue`, for [`ScoreSystem`](score-system.md)) — `HandleCrashed` picked up the unused `scoreValue` param just to keep the signature matching. Flagged as exactly the kind of coupling the planned "Core systems refactor" milestone (a mediating system between crash/damage sources and Fuel/Health/Score) exists to remove.

A quality-1.0 ("perfect crash") hit always clamps to exactly `maxFuel` regardless of current level, matching the design doc's "a perfect crash fully refuels." Partial quality adds a proportional, clamped amount.

Drain rate is multiplied by `m_boostDrainMultiplier` (default `2`) whenever [`PlaneController.IsBoosting`](movement.md#boost-milestone-2-pass-2) is true — boost costs fuel faster, per the design doc's risk-lever framing.

## Value storage — `ObservableFloat`

The current fuel value is backed by `TripleA.Utils.Observables.Primaries.ObservableFloat` (an existing project dependency) instead of a plain float, so [FuelGaugeUI](ui.md) can react to changes via `AddListener` rather than polling every frame.

It's exposed through a **lazily-constructed private property**, not built in `Awake`:
```csharp
private ObservableFloat Fuel => _fuel ??= new ObservableFloat(maxFuel);
```
This matters because Unity does not guarantee `Awake` order across different GameObjects — only that a component's own `Awake` runs before its own `OnEnable`. `FuelGaugeUI` (on a separate `HUD` object) reading `FuelSystem.FuelValue` in its own `OnEnable` could run before `FuelSystem.Awake` executes, and did in practice during testing (NullReferenceException). Lazy construction sidesteps the ordering question entirely — safe on first access regardless of which object initializes first, since serialized fields like `maxFuel` are already deserialized before any script's `Awake`/`OnEnable` runs.

`ObservableFloat` has no built-in clamping — `FuelSystem` always clamps before calling `Set`. Its `Value` setter is public, so anything holding the object could technically bypass the clamping (the only actual consumer, `FuelGaugeUI`, only reads `FuelFraction` and subscribes via `AddListener` — never touches `.Value` directly). Not worth wrapping further at this scale.

## Run end

At fuel = 0: disables `PlaneController` (which disables its own input action via its existing `OnDisable`) and zeros the Rigidbody's `linearVelocity` so the plane visibly stops rather than coasting forever (no gravity/drag on the Plane). Fires `public event Action RunEnded` — **no consumer yet**, same unconsumed-seam pattern as `Building.Crashed`; a future pass (game-over UI, restart) is expected to subscribe.

## Multi-source wiring (Milestone 3 Pass 1)

`m_building` (single `Building`) generalized to `m_buildings` (`Building[]`), subscribing/unsubscribing to each one's `Crashed` in `OnEnable`/`OnDisable` — same foreach pattern [`ScoreSystem`](score-system.md) already used. Fixes a known limitation: previously only one `Building` instance could ever refuel the plane even with multiple crashable buildings in the scene.

A second array, `m_damageSourceBehaviours` (`MonoBehaviour[]`, cast to `IDamageDealer[]` at `Awake` — same cast idiom as [`HealthSystem`](health-system.md)), subscribes to `DamageDealt` and drains fuel by the same amount on `HandleDamagePenalty`. This is opt-in per source: empty by default, so wiring nothing into it leaves existing tough-building mistimed-hit behavior (health-only penalty) unchanged. [`ObstacleBuilding`](obstacle-system.md) is the first source meant to be wired in here, since obstacles cost both fuel and health per the design doc.

## Scene/prefab constraint

`FuelSystem.m_buildings`/`m_damageSourceBehaviours` (the crash/damage sources) cannot be baked into `Plane.prefab` — `Building`/obstacle instances are scene-only, and a prefab asset cannot hold a reference to a scene object (Unity silently nulls it on save). They're assigned as prefab-instance array overrides on the `Plane` inside `Prototype.unity` instead.
