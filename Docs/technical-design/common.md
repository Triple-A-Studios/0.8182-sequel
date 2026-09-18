# Common

Shared, engine-agnostic contracts used across systems — not a system of its own, just where cross-cutting interfaces live so no system has to depend on another system's concrete types.

## `CombatEvents` (Milestone: Core systems refactor, Pass 1)

`Assets/_Project/Scripts/Common/CombatEvents.cs`, namespace `Opoint8182.Common`. A static event bus — the current wiring mechanism for all three interfaces below. Replaced the earlier per-instance `[SerializeField] MonoBehaviour` cast-at-`Awake` array pattern this doc used to describe (see [Resolved Backlog](../progress.md#resolved-backlog) items #4/#10): every damage/restore/crash source raises into the bus, every listener subscribes to the bus, with no source ever needing to be hand-wired into a receiving system's Inspector array.

```csharp
public static class CombatEvents
{
    public static event Action<IDamageDealer, float> DamageDealt;
    public static event Action<IRestorer, float> Restored;
    public static event Action<ICrashSource, float, bool> Crashed;   // (source, quality, countsForCombo = true)

    public static void RaiseDamageDealt(IDamageDealer source, float damage);
    public static void RaiseRestored(IRestorer source, float amount);
    public static void RaiseCrashed(ICrashSource source, float quality, bool countsForCombo = true);
}
```

Consumers: [`PlayerManager`](building-crash-system.md) subscribes to all three and applies the resulting deltas to `FuelSystem`/`HealthSystem` (which are pure resource managers with no knowledge of where a delta came from); [`ScoreSystem`](score-system.md) subscribes to `Crashed` (score + combo) and `DamageDealt` (combo-timer penalty).

## `IDamageDealer` (Milestone 2 Pass 3)

`Assets/_Project/Scripts/Common/IDamageDealer.cs`, namespace `Opoint8182.Common`.

```csharp
public interface IDamageDealer
{
    float Damage { get; }
    float HealthDamageMultiplier => 1f;
    float FuelDamageMultiplier => 1f;
}
```

Introduced so damage tuning lives on whatever deals the damage, not on whatever receives it — [`HealthSystem`](health-system.md) used to own a `m_toughHitDamage` field that only made sense for tough buildings specifically; now [`Building`](building-crash-system.md#damage-dealing-milestone-2-pass-3) owns that number itself. `HealthDamageMultiplier`/`FuelDamageMultiplier` (Milestone: Core systems refactor, Pass 3) let a source tune its damage independently per resource — both default to `1f` via C# 8 default interface implementations, so existing implementers didn't need updating when these were added. A source no longer carries its own `event Action<float> DamageDealt` — it raises `CombatEvents.RaiseDamageDealt(this, damage)` instead; see `CombatEvents` above.

Implemented by [`Building`](building-crash-system.md) (mistimed tough hits only), [`ObstacleBuilding`](obstacle-system.md), and [`BirdHazard`](obstacle-system.md#tier-2-small-hazards-birds-milestone-3-pass-2).

## `IRestorer` (Milestone 3 Pass 4)

`Assets/_Project/Scripts/Common/IRestorer.cs`, namespace `Opoint8182.Common`.

```csharp
public interface IRestorer
{
    float Restore { get; }
}
```

Same shape as `IDamageDealer`, inverted — a resource-restoring source instead of a resource-draining one, raising `CombatEvents.RaiseRestored(this, amount)` rather than carrying its own event. [`HealthPickup`](pickup-system.md) is the only implementation today; a future fuel pickup would implement the same interface with zero changes to it.

## `ICrashSource` (Milestone: Core systems refactor, Pass 1)

`Assets/_Project/Scripts/Common/ICrashSource.cs`, namespace `Opoint8182.Common`.

```csharp
public interface ICrashSource
{
    int ScoreValue { get; }
}
```

The symmetric "what broke and how much is it worth" contract for `CombatEvents.Crashed` — resolves the backlog item that used to be flagged here as open (a refuel-source interface for `Building.Crashed` so `FuelSystem` didn't hardcode `Building` as the crash-source type). [`Building`](building-crash-system.md) is the only implementation today; any future crashable/destructible source implements this and raises `CombatEvents.RaiseCrashed(this, quality)` with no changes needed on `ScoreSystem`'s or `PlayerManager`'s end.
