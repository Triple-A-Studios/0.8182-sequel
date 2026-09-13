# Score System

`ScoreSystem.cs` (`Assets/_Project/Scripts/Score/ScoreSystem.cs`), namespace `Opoint8182.Score`. Sibling component on the `Plane` prefab root alongside [FuelSystem](fuel-system.md) and [HealthSystem](health-system.md) — same location convention as the other two player-resource systems, even though score doesn't touch physics or end the run, so it skips their `[RequireComponent(PlaneController/Rigidbody)]` and run-ended guard entirely.

## Formula (Milestone 3 Pass 3)

Each crash scores the building's own `ScoreValue` (see [building-crash-system.md](building-crash-system.md#scoring-milestone-3-pass-3)) plus a quality-scaled bonus up to another full `ScoreValue` on a perfect weak-point hit, all multiplied by the current combo multiplier:

```
bonus      = round(quality * scoreValue)
crashScore = (scoreValue + bonus) * multiplier
newScore   = currentScore + crashScore
```

A body hit (`quality = 0`, per the crash-quality formula's "no partial-credit zone off the weak point") scores just the base `scoreValue`. Subscribes to `Building.Crashed` (`Action<float, int>` — quality and scoreValue together, widened from the original quality-only signature specifically for this).

## Combo multiplier

A stepping multiplier, not a flat per-crash bonus:

- **Chain count.** Every crash (via `m_buildings`) increments `m_chainCrashCount` and refreshes a countdown timer to `m_comboWindow` (default 3s).
- **Tiers.** `m_comboStepSizes` (default `{3, 5, 8}`) is how many chained crashes are needed to advance from the current multiplier to the next — indexed by `multiplier - 1`, so tiers can require different counts as the multiplier climbs. Crossing the current tier's threshold increments the multiplier and resets the chain count toward the next tier. The multiplier is hard-capped at `1 + m_comboStepSizes.Length` — once the list is exhausted, further crashes keep scoring at the max multiplier but stop climbing.
- **Timeout steps down one tier at a time, doesn't reset straight to x1.** If the window lapses with no new crash, the multiplier drops by one tier (chain count resets to 0) and, if it's still above x1, the countdown restarts for another full `m_comboWindow` — so an idle player decays x4 → x3 → x2 → x1 one tier per window rather than dropping once and parking at 0. `Update()` ticks the timer down every frame via a shared `Tick`/step-down path.
- **Hazards eat the timer, don't step it down directly.** `m_hazardBehaviours` (`MonoBehaviour[]`, cast to `IDamageDealer[]` at `Awake` — same idiom `FuelSystem`/`HealthSystem` use) wires `ObstacleBuilding`/`BirdHazard` instances; a hit subtracts `m_hazardTimerPenalty` (default 1.5s) from the remaining combo timer through the same `Tick` path, so it can trigger an immediate step-down if it crosses zero rather than just silently going negative. Deliberately *not* wired to a mistimed tough-building hit (`Building.DamageDealt`) — that's a crash-target fail state, not a "hazard" in the design doc's sense.
- **Out of scope this pass:** reducing the timer for flying past a building without hitting it. That needs a way to know a building existed and wasn't hit — requires the not-yet-built spawning/tracking system (see `progress.md` backlog).

`MultiplierValue` (`ObservableInt`, same lazy-construction idiom as `Score`) exposes the current tier, and `ComboTimerValue` (`ObservableFloat`, raw seconds remaining) + `ComboTimerFraction` (computed, same shape as `FuelSystem.FuelFraction`) expose the countdown — both for [`ScoreUI`](ui.md#combo-label-milestone-3-pass-3). The countdown is pushed every frame via `Tick`, same as the multiplier/chain-count logic it's part of.

## Multiple sources, not a single hardcoded reference

`ScoreSystem.m_buildings` was a small array from the start (score should count every crash regardless of which building), the same array-of-sources shape `FuelSystem`/`HealthSystem` were later generalized to in Milestone 3 Pass 1 (see fuel-system.md's "Multi-source wiring"). `m_hazardBehaviours` (Pass 3) follows the same shape for the combo-timer-penalty sources.

This still isn't a real solution for dynamically-spawned buildings/hazards — once a spawner exists, all these systems will need some form of registration (new instances announcing themselves) rather than a fixed list wired in the Editor. Flagged, not solved here — this is exactly the "static/global event" item in the planned "Core systems refactor" milestone.

## Value storage — `ObservableInt`

`TripleA.Utils.Observables.Primaries.ObservableInt` — same package, same shape as `ObservableFloat`/`ObservableBool`. Both `Score` and `Multiplier` are lazily constructed for the same Awake-ordering reason as `FuelSystem.Fuel`/`HealthSystem.Health`.

## UI

[`ScoreUI.cs`](ui.md#score-ui-milestone-2-pass-4) mirrors `FuelGaugeUI` almost exactly — queries a `Label` instead of a fill bar, sets its text to `"Score: {n}"` on every change (and once immediately in `OnEnable`). Pass 3 adds a second label the same way for the combo multiplier — see [ui.md](ui.md#combo-label-milestone-3-pass-3).
