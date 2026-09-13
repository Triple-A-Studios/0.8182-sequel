# Health System

`HealthSystem.cs` (`Assets/_Project/Scripts/Health/HealthSystem.cs`), namespace `Opoint8182.Health`. Sibling component on the `Plane` prefab root alongside [PlaneController](movement.md) and [FuelSystem](fuel-system.md). Structurally mirrors `FuelSystem` on purpose — same `ObservableFloat`-backed value, same lazy-construction reasoning, same local `EndRun()` shape — rather than routing both resources through a shared run-controller; a few duplicated lines beat a premature abstraction at this scale.

## Damage — via `IDamageDealer`, not a hardcoded source

`HealthSystem` doesn't know or care that damage currently comes from a tough building. It holds zero Building-specific knowledge: a `[SerializeField] private MonoBehaviour m_damageDealerBehaviour` field (Unity can't serialize a bare interface reference in the Inspector, so it's typed `MonoBehaviour` and cast to [`IDamageDealer`](common.md) in `Awake`), and subscribes to that interface's `event Action<float> DamageDealt`. Whatever fires that event and however much damage it carries is entirely the source's business — `HealthSystem` just clamps it in:

```
newHealth = Mathf.Clamp(currentHealth - damage, 0, m_maxHealth)
```

`Building` (`Assets/_Project/Scripts/Building/Building.cs`) is the only `IDamageDealer` today — it owns its own `m_toughHitDamage` tunable and fires `DamageDealt` from the same `!canBreak` branch that used to just log-and-return (see [building-crash-system.md](building-crash-system.md#damage-dealing-milestone-2-pass-3)). Default `m_maxHealth: 100`, `Building.m_toughHitDamage: 35` — three mistimed tough hits to die. Both are plain tunables; instant-vs-gradual was left open in the design doc for playtest feel, resolved this way for now but easy to retune (e.g. `m_toughHitDamage = m_maxHealth` for instant death without any code change).

This is deliberately set up so a future damage source — a large obstacle building, a bird hazard — just needs to implement `IDamageDealer` on its own component; `HealthSystem` doesn't change at all. (It still only wires up a single source at a time, same single-reference scaffold limitation as `FuelSystem` — see below.)

## Value storage — `ObservableFloat`

Same reasoning as `FuelSystem.Fuel`: backed by `TripleA.Utils.Observables.Primaries.ObservableFloat`, exposed through a lazily-constructed private property (`Health => m_health ??= new ObservableFloat(m_maxHealth)`) so reads are safe regardless of cross-object `Awake` ordering. No UI consumer yet — a debug `[ShowInInspector] public float CurrentHealth` field surfaces it in the meantime, same as `FuelSystem` had before the fuel gauge existed. A health bar isn't in this milestone's scope (Pass 4 is "Score," not health UI); it'll come with a later UI pass.

## Run end

At health = 0: disables `PlaneController` and zeros the Rigidbody's `linearVelocity`, same as `FuelSystem.EndRun`. Fires `public event Action Died` — no consumer yet. Calling this independently of `FuelSystem`'s own end-of-run handling is redundant but harmless (disabling an already-disabled `PlaneController` is a no-op) — there's no single "run ended" authority, each resource just ends the run its own way when it hits zero.

## Scene/prefab constraint

Same constraint as `FuelSystem.building` (see [fuel-system.md](fuel-system.md#sceneprefab-constraint)): `HealthSystem.m_damageDealerBehaviour` can't be baked into `Plane.prefab` since the only `Building` instances are scene-only. It's assigned as a prefab-instance override in `Prototype.unity`, wired to `Building_Tough` specifically — `Building_Normal` can never fire `DamageDealt` (`canBreak` is always true for `BuildingType.Normal`), so wiring `HealthSystem` to it would mean it never takes damage at all.
