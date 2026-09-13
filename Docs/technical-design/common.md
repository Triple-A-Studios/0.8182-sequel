# Common

Shared, engine-agnostic contracts used across systems — not a system of its own, just where cross-cutting interfaces live so no system has to depend on another system's concrete types.

## `IDamageDealer` (Milestone 2 Pass 3)

`Assets/_Project/Scripts/Common/IDamageDealer.cs`, namespace `Opoint8182.Common`.

```csharp
public interface IDamageDealer
{
    float Damage { get; }
    event Action<float> DamageDealt;
}
```

Introduced so damage tuning lives on whatever deals the damage, not on whatever receives it — [`HealthSystem`](health-system.md) used to own a `m_toughHitDamage` field that only made sense for tough buildings specifically; now [`Building`](building-crash-system.md#damage-dealing-milestone-2-pass-3) owns that number itself and `HealthSystem` just reacts to `DamageDealt`, with no idea what kind of source it came from. Any future damage source (large obstacle buildings, hazards) implements this interface and needs zero changes to `HealthSystem`.

**Unity can't serialize a bare interface field in the Inspector.** Consumers store a `[SerializeField] private MonoBehaviour` and cast it to `IDamageDealer` at `Awake` (see `HealthSystem.m_damageDealerBehaviour`) — a plain cast, not a custom PropertyDrawer or asset-based indirection; that's more machinery than a prototype-scope single-source hookup needs.

See the backlog for the broader "interface-ify other resource sources" idea (refuel, health regen pickups) this pattern opens the door to — not done yet, flagged for when those features actually get built.
