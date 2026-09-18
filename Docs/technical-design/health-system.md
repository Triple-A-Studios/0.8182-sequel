# Health System

`HealthSystem.cs` (`Assets/_Project/Scripts/Health/HealthSystem.cs`), namespace `Opoint8182.Health`. Sibling component on the `Plane` prefab root alongside [PlaneController](movement.md) and [FuelSystem](fuel-system.md). Structurally mirrors `FuelSystem` on purpose — same `ObservableFloat`-backed value, same lazy-construction reasoning, same local `EndRun()` shape — rather than routing both resources through a shared run-controller; a few duplicated lines beat a premature abstraction at this scale.

## Damage — via `IDamageDealer`, not a hardcoded source

`HealthSystem` doesn't know or care what building or obstacle damage comes from. It holds zero source-specific knowledge: a `[SerializeField] private MonoBehaviour[] m_damageDealerBehaviours` array (Unity can't serialize a bare interface reference in the Inspector, so it's typed `MonoBehaviour[]` and each entry cast to [`IDamageDealer`](common.md) in `Awake`), and subscribes to every entry's `event Action<float> DamageDealt` in `OnEnable`/`OnDisable` (same foreach pattern [`ScoreSystem`](score-system.md) uses for `Building.Crashed`). Whatever fires that event and however much damage it carries is entirely the source's business — `HealthSystem` just clamps it in:

```
newHealth = Mathf.Clamp(currentHealth - damage, 0, m_maxHealth)
```

`Building` (`Assets/_Project/Scripts/Building/Building.cs`) and [`ObstacleBuilding`](obstacle-system.md) (Milestone 3 Pass 1) are the two `IDamageDealer` implementations today. `Building` owns its own `m_toughHitDamage` tunable and fires `DamageDealt` from the same `!canBreak` branch that used to just log-and-return (see [building-crash-system.md](building-crash-system.md#damage-dealing-milestone-2-pass-3)). Default `m_maxHealth: 100`, `Building.m_toughHitDamage: 35` — three mistimed tough hits to die. Both are plain tunables; instant-vs-gradual was left open in the design doc for playtest feel, resolved this way for now but easy to retune (e.g. `m_toughHitDamage = m_maxHealth` for instant death without any code change).

This is deliberately set up so a future damage source just needs to implement `IDamageDealer` on its own component; `HealthSystem` doesn't change at all. The array generalization (Milestone 3 Pass 1) replaced the original single-`MonoBehaviour`-field version — it fixed the previous single-reference scaffold limitation shared with `FuelSystem` (see below), which meant only one damage source at a time could ever reach `HealthSystem` even with multiple in the scene.

## Health pickups (Milestone 3 Pass 4)

Same array-of-sources shape as damage, mirrored: `[SerializeField] private MonoBehaviour[] m_restorerBehaviours`, cast to [`IRestorer[]`](common.md#irestorer-milestone-3-pass-4) in `Awake`, subscribed to every entry's `Restored` event in `OnEnable`/`OnDisable`. `HandleRestored` clamps the opposite direction from `HandleDamageDealt`:

```
newHealth = Mathf.Clamp(currentHealth + amount, 0, m_maxHealth)
```

No run-end check needed on the damage side's reasoning (restoring can't end a run), but it's guarded by `m_isRunEnded` anyway for consistency with `HandleDamageDealt` — harmless either way since a pickup collected after the plane's already disabled just wouldn't matter gameplay-wise. [`HealthPickup`](pickup-system.md) is the only source today; unlike damage dealers, a pickup is single-use (destroys itself after firing `Restored` once) rather than a persistent hazard.

Fuel doesn't get a matching restorer array this pass — no fuel pickup exists, fuel is deliberately restored only through the crash-refuel loop per the design doc, and an unused array would be dead wiring. Trivial to add later if a fuel pickup is ever actually built (same `IRestorer` interface, no changes needed to it).

## Value storage — `ObservableFloat`

Same reasoning as `FuelSystem.Fuel`: backed by `TripleA.Utils.Observables.Primaries.ObservableFloat`, exposed through a lazily-constructed private property (`Health => m_health ??= new ObservableFloat(m_maxHealth)`) so reads are safe regardless of cross-object `Awake` ordering. No UI consumer yet — `HealthValue`/`HealthFraction` are inspectable via the Inspector's Debug mode in the meantime, same as `FuelSystem` had before the fuel gauge existed. A health bar isn't in this milestone's scope (Pass 4 is "Score," not health UI); it'll come with a later UI pass. (A `CurrentHealth` debug property with Alchemy's `[ShowInInspector]` used to sit here — removed in Pass 6, Movement & fail-state rework, see [current-state.md](../current-state.md); the project no longer uses `[ShowInInspector]`/`[ReadOnly]` for debug-only variables.)

## Run end

At health = 0: disables `PlaneController` and zeros the Rigidbody's `linearVelocity`, same as `FuelSystem.EndRun`. Fires `public event Action Died` — no consumer yet. Calling this independently of `FuelSystem`'s own end-of-run handling is redundant but harmless (disabling an already-disabled `PlaneController` is a no-op) — there's no single "run ended" authority, each resource just ends the run its own way when it hits zero.

## Scene/prefab constraint

Same constraint as `FuelSystem.m_buildings`/`m_damageSourceBehaviours` (see [fuel-system.md](fuel-system.md#multi-source-wiring-milestone-3-pass-1)): `HealthSystem.m_damageDealerBehaviours` can't be baked into `Plane.prefab` since damage-source instances are scene-only. It's assigned as a prefab-instance array override in `Prototype.unity`. `Building_Normal` can never fire `DamageDealt` (`canBreak` is always true for `BuildingType.Normal`), so wiring it in is harmless but pointless — it just never fires.
