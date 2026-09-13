# Building & Crash System

`Building.cs` (`Assets/_Project/Scripts/Building/Building.cs`) and `WeakPoint.cs` (`Assets/_Project/Scripts/Building/WeakPoint.cs`), namespace `Opoint8182.Building`.

## Collider / marker split

The building has one non-trigger `BoxCollider` (the real, solid obstacle the plane physically hits) plus a plain `WeakPoint` marker component (no collider) on a child object. `Building.OnCollisionEnter` compares the actual collision contact point's distance to the marker's position against a tunable `hitRadius`, rather than using a second overlapping trigger collider — Unity doesn't guarantee ordering between a solid collision and an overlapping trigger event in the same physics step, and an inset trigger risked becoming a hole the plane could clip through.

Hierarchy is flexible — nesting the weak point under a `Visual` wrapper (matching `Plane.prefab`'s physics/visual split) works fine, since both the distance check and the alignment check operate on world-space `transform.position`/`transform.forward`, independent of hierarchy depth.

## Crash-quality formula

```
impactVelocity = collision.relativeVelocity
speed01        = Mathf.Clamp01(impactVelocity.magnitude / referenceMaxSpeed)
alignment01    = Mathf.Clamp01(Vector3Math.GetDotProduct(impactVelocity.normalized, -weakPoint.OutwardNormal))
quality        = hitWeakPoint ? speed01 * alignment01 : 0f
```

`referenceMaxSpeed` (tunable per building) is the speed at/above which the speed component of quality maxes out — set above the plane's default cruise speed since no boost exists yet in Milestone 1. `OutwardNormal` is the weak-point marker's `transform.forward`. A dead-on hit at/above `referenceMaxSpeed` scores 1.0 ("perfect crash"). A hit on the building's body outside `hitRadius` is still a valid, physical crash but scores `0` — the design doc ([Docs/design-doc.md](../design-doc.md)) only defines the quality metric "into" the weak point; no partial-credit zone is invented for a body hit.

**Uses `collision.relativeVelocity`, not `plane.Velocity`/`plane.Speed`.** Unity resolves the collision response (the actual velocity change from impact) before dispatching `OnCollisionEnter`, so reading the plane's `Rigidbody` velocity inside that callback gives the *post-impact* velocity — against a static, heavy building that's close to zero regardless of how fast or well-aligned the approach was. `collision.relativeVelocity` is computed pre-resolution and is the correct value for an impact calculation like this. (Also: `Vector3Math.GetDotProduct(vector, direction)` only normalizes `direction`, not `vector` — passing an unnormalized velocity into it double-counts speed since `speed01` already accounts for magnitude separately. Both `impactVelocity.magnitude` and `impactVelocity.normalized` are used explicitly to avoid this.)

`Vector3Math.GetDotProduct` comes from `TripleA.Utils.Extensions` (already a project dependency, see [dependencies.md](dependencies.md)).

## Seam for later passes

`Building` fires `public event Action<float> Crashed` with the computed quality value immediately before destroying itself (`Destroy(gameObject)`, no VFX — deferred to the Feel Polish milestone). **No consumer exists yet** — Milestone 1 Pass 3 (fuel system) is expected to subscribe to `Crashed` to apply refuel amount; nothing currently needs to be edited on `Building`/`WeakPoint` to add that subscriber. A `Debug.Log` and Alchemy `[ReadOnly, ShowInInspector]` debug fields (`_lastCrashQuality`, `_lastHitWeakPoint`) surface the result in the meantime, since no fuel/UI exists to display it.

## Building types (Milestone 2 Pass 1)

`BuildingType.cs` (`Assets/_Project/Scripts/Building/BuildingType.cs`) defines `Normal`/`Tough`. `Building.m_buildingType` picks the variant; `Tough` additionally needs `m_toughBreakSpeed` cleared by `collision.relativeVelocity.magnitude` before it will break at all — checked unconditionally (both weak-point and body hits), before the quality/refuel calculation runs. Below the threshold, `OnCollisionEnter` returns early: no `Crashed` event, no destroy, no debug fields updated — the building survives and the plane just gets Unity's normal solid-collider bounce (see [Damage dealing](#damage-dealing-milestone-2-pass-3) below for the scripted consequence added in Pass 3).

Boost (Pass 2) raises the plane's real speed via `PlaneController.SpeedMultiplier` rather than exposing a dedicated "is boosting" flag — the gate here is a plain impact-speed threshold, reusing the same `collision.relativeVelocity` the crash-quality formula already computes. Boosting is simply the practical way to clear `m_toughBreakSpeed`; no rework was needed here when Pass 2 landed.

`Building_Tough.prefab` (`Assets/_Project/Prefabs/Buildings/`) duplicates `Building_Normal.prefab` with `m_buildingType: Tough` and a distinct placeholder material (`Mat_Building_Tough.mat`, dark red tint) — no new art asset, just a different `_BaseColor`. One instance is manually placed in `Prototype.unity` alongside `Building_Normal`.

**Known limitation:** [`FuelSystem`](fuel-system.md) still hardcodes a single `m_building` reference, and [`HealthSystem`](health-system.md) hardcodes a single damage-source reference (a Milestone-1 scaffold constraint — see fuel-system.md's "Scene/prefab constraint"). Currently both happen to be wired to `Building_Tough` in `Prototype.unity` — `FuelSystem` refuels off it breaking, `HealthSystem` takes damage off it resisting; `Building_Normal` has no subscribers at all right now, so crashing it still destroys it (visible via its own `Debug.Log`) but doesn't refuel. Revisit once multiple simultaneous crashable buildings are the norm (likely the Obstacles milestone) rather than a Prototype-scope fix.

## Damage dealing (Milestone 2 Pass 3)

`Building` implements [`IDamageDealer`](common.md) (`Assets/_Project/Scripts/Common/IDamageDealer.cs`): a `Damage` property backed by its own `m_toughHitDamage` tunable, and `public event Action<float> DamageDealt`, fired with that value from the same `!canBreak` branch described above, right before the early `return`. A mistimed tough-building hit is a distinct outcome from `Crashed` (broke, refuel), so it gets its own event rather than overloading `Crashed` with a zero/negative quality value.

Damage tuning lives here, on the source, rather than on whatever receives it — [`HealthSystem`](health-system.md) is the only current subscriber, but it has no idea the damage came from a building specifically; it just reacts to `IDamageDealer.DamageDealt`. A future damage source (large obstacle buildings, hazards) only needs to implement the interface — no changes needed on the receiving end.
