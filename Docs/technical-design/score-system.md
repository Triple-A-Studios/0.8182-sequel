# Score System

`ScoreSystem.cs` (`Assets/_Project/Scripts/Score/ScoreSystem.cs`), namespace `Opoint8182.Score`. Sibling component on the `Plane` prefab root alongside [FuelSystem](fuel-system.md) and [HealthSystem](health-system.md) — same location convention as the other two player-resource systems, even though score doesn't touch physics or end the run, so it skips their `[RequireComponent(PlaneController/Rigidbody)]` and run-ended guard entirely.

## Formula

Flat `+1` per successful crash, no quality or combo weighting:

```
newScore = currentScore + 1
```

Subscribes to `Building.Crashed` (see [building-crash-system.md](building-crash-system.md)) — `quality` is accepted but intentionally unused. Combo-chain scoring is explicitly Milestone 3 scope ("Obstacles, combos, recovery"); this pass is deliberately the simplest thing that satisfies "simple visible score, increments per crash."

## Multiple sources, not a single hardcoded reference

Unlike `FuelSystem.m_building`/`HealthSystem.m_damageDealerBehaviour` (each a single reference — a documented Milestone-1 scaffold limitation, see fuel-system.md's "Scene/prefab constraint"), `ScoreSystem.m_buildings` is a small array, wired to both `Building_Normal` and `Building_Tough` in `Prototype.unity`. Score conceptually should count every crash regardless of which building, and with exactly two hand-placed buildings existing right now, subscribing to both is no more complex than subscribing to one.

This still isn't a real solution for dynamically-spawned buildings — once the Obstacles milestone introduces a spawner, all three systems (`FuelSystem`, `HealthSystem`, `ScoreSystem`) will need some form of registration (new buildings announcing themselves) rather than a fixed list wired in the Editor. Flagged, not solved here — see the backlog entry from Pass 3 about generalizing source interfaces (`IDamageDealer` and friends) for the same underlying reason.

## Value storage — `ObservableInt`

`TripleA.Utils.Observables.Primaries.ObservableInt` — same package, same shape as `ObservableFloat`/`ObservableBool`. Lazily constructed for the same Awake-ordering reason as `FuelSystem.Fuel`/`HealthSystem.Health`.

## UI

[`ScoreUI.cs`](ui.md#score-ui-milestone-2-pass-4) mirrors `FuelGaugeUI` almost exactly — queries a `Label` instead of a fill bar, sets its text to `"Score: {n}"` on every change (and once immediately in `OnEnable`).
