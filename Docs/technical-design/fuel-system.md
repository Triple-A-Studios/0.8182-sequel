# Fuel System

`FuelSystem.cs` (`Assets/_Project/Scripts/Fuel/FuelSystem.cs`), namespace `Opoint8182.Fuel`. Sibling component on the `Plane` prefab root alongside `PlaneController` — fuel is the player's plane's resource, not a scene-level manager concern.

## Drain and refuel

Drains at a tunable `drainPerSecond` every frame. Refuels on `Building.Crashed` (see [building-crash-system.md](building-crash-system.md)):

```
newFuel = Mathf.Clamp(currentFuel + quality * maxFuel, 0, maxFuel)
```

A quality-1.0 ("perfect crash") hit always clamps to exactly `maxFuel` regardless of current level, matching the design doc's "a perfect crash fully refuels." Partial quality adds a proportional, clamped amount.

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

## Scene/prefab constraint

`FuelSystem.building` (the crash-quality source) cannot be baked into `Plane.prefab` — the only `Building` instance is scene-only, and a prefab asset cannot hold a reference to a scene object (Unity silently nulls it on save). It's assigned as a prefab-instance override on the `Plane` inside `Prototype.unity` instead.
