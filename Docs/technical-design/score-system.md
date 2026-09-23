# Score System

`ScoreSystem.cs` (`Assets/_Project/Scripts/Score/ScoreSystem.cs`), namespace `Opoint8182.Score`. Sibling component on the `Plane` prefab root alongside [FuelSystem](fuel-system.md) and [HealthSystem](health-system.md) — same location convention as the other two player-resource systems, even though score doesn't touch physics or end the run, so it skips their `[RequireComponent(PlaneController/Rigidbody)]` and run-ended guard entirely.

## Formula (Milestone 3 Pass 3)

Each crash scores the building's own `ScoreValue` (see [building-crash-system.md](building-crash-system.md#scoring-milestone-3-pass-3)) plus a quality-scaled bonus up to another full `ScoreValue` on a perfect weak-point hit, all multiplied by the current combo multiplier:

```
bonus      = round(quality * scoreValue)
crashScore = (scoreValue + bonus) * multiplier
newScore   = currentScore + crashScore
```

A body hit (`quality = 0`, per the crash-quality formula's "no partial-credit zone off the weak point") scores just the base `scoreValue`. Subscribes to [`CombatEvents.Crashed`](common.md#combatevents-milestone-core-systems-refactor-pass-1) (`Action<ICrashSource, float, bool>` — source, quality, `countsForCombo`) — not a per-instance `Building.Crashed` event or a hand-wired array; see "Multiple sources" below.

## Combo multiplier

A stepping multiplier, not a flat per-crash bonus:

- **Chain count.** Every crash where `countsForCombo` is true increments `m_chainCrashCount` and refreshes a countdown timer to `m_comboWindow` (default 3s).
- **Tiers.** `m_comboStepSizes` (default `{3, 5, 8}`) is how many chained crashes are needed to advance from the current multiplier to the next — indexed by `multiplier - 1`, so tiers can require different counts as the multiplier climbs. Crossing the current tier's threshold increments the multiplier and resets the chain count toward the next tier. The multiplier is hard-capped at `1 + m_comboStepSizes.Length` — once the list is exhausted, further crashes keep scoring at the max multiplier but stop climbing.
- **Timeout steps down one tier at a time, doesn't reset straight to x1.** If the window lapses with no new crash, the multiplier drops by one tier (chain count resets to 0) and, if it's still above x1, the countdown restarts for another full `m_comboWindow` — so an idle player decays x4 → x3 → x2 → x1 one tier per window rather than dropping once and parking at 0. `Update()` ticks the timer down every frame via a shared `Tick`/step-down path.
- **Hazards eat the timer, don't step it down directly.** Subscribes to `CombatEvents.DamageDealt` (`Action<IDamageDealer, float>`) — any hit (`ObstacleBuilding`/`BirdHazard`/mistimed tough-building) subtracts `m_hazardTimerPenalty` (default 1.5s) from the remaining combo timer through the same `Tick` path, guarded so it's a no-op when no combo is currently running (`ComboTimer.Value <= 0f`). Can trigger an immediate step-down if it crosses zero rather than just silently going negative.
- **Missing a building also eats the timer (Difficulty ramp, Pass 3).** Subscribes to [`SpawnEvents.EntityCulled`](spawning.md#difficulty-ramp-pass-2) (`Action<SpawnedEntity>`) — fires only when `SpawnManager` despawns an instance for falling behind the plane unhit, never for a hit-destroy, so "an unhit building was culled" is already the exact signal needed. `HandleEntityCulled` filters to `SpawnKind.BuildingNormal`/`BuildingTough` only (missing an `ObstacleLarge`/`BirdSmall`/`HealthPickup` is the intended, non-punished outcome — obstacles are "avoid, not crash" by design) and subtracts `m_missedBuildingTimerPenalty` (default 1.5s, a separate tunable from `m_hazardTimerPenalty` — missing a building and getting hit by a hazard are different mistakes, may want different weight later) through the same guarded `Tick` path. Also fires a `Debug.LogWarning` for in-editor confirmation during tuning. This resolves backlog #7 (`progress.md`), which had been blocked on a spawner existing to know "a building existed and wasn't hit."

`MultiplierValue` (`ObservableInt`, same lazy-construction idiom as `Score`) exposes the current tier, and `ComboTimerValue` (`ObservableFloat`, raw seconds remaining) + `ComboTimerFraction` (computed, same shape as `FuelSystem.FuelFraction`) expose the countdown — both for [`ScoreUI`](ui.md#combo-label-milestone-3-pass-3). The countdown is pushed every frame via `Tick`, same as the multiplier/chain-count logic it's part of.

## Multiple sources, not a single hardcoded reference

`ScoreSystem` has no per-source array (`m_buildings`, `m_hazardBehaviours`, etc.) — every building/hazard/obstacle raises into the static [`CombatEvents`](common.md#combatevents-milestone-core-systems-refactor-pass-1) bus and `ScoreSystem` subscribes once, regardless of how many instances exist or whether they're hand-placed or spawned at runtime. This is what let Pass 1's `SpawnManager` (see [spawning.md](spawning.md)) start `Instantiate`-ing buildings/hazards with zero changes needed here — the "static/global event, no per-instance registration" design the "Core systems refactor" milestone introduced was built with exactly this future use in mind.

## Value storage — `ObservableInt`

`TripleA.Utils.Observables.Primaries.ObservableInt` — same package, same shape as `ObservableFloat`/`ObservableBool`. Both `Score` and `Multiplier` are lazily constructed for the same Awake-ordering reason as `FuelSystem.Fuel`/`HealthSystem.Health`.

## UI

[`ScoreUI.cs`](ui.md#score-ui-milestone-2-pass-4) mirrors `FuelGaugeUI` almost exactly — queries a `Label` instead of a fill bar, sets its text to `"Score: {n}"` on every change (and once immediately in `OnEnable`). Pass 3 adds a second label the same way for the combo multiplier — see [ui.md](ui.md#combo-label-milestone-3-pass-3).
