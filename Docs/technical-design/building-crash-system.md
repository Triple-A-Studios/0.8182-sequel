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

`BuildingType.cs` (`Assets/_Project/Scripts/Building/BuildingType.cs`) defines `Normal`/`Tough`. `Building.m_buildingType` picks the variant; `Tough` additionally needs `m_toughBreakSpeed` cleared by `collision.relativeVelocity.magnitude` before it will break at all — checked unconditionally (both weak-point and body hits), before the quality/refuel calculation runs. Below the threshold, `OnCollisionEnter` returns early: no `Crashed` event, no destroy, no debug fields updated — the building survives and the plane just gets Unity's normal solid-collider bounce, no scripted penalty (yet — health loss on a mistimed tough hit is Pass 3).

No real boost mechanic exists yet (Pass 2), so there's no dedicated "is boosting" flag — the gate is a plain impact-speed threshold, reusing the same `collision.relativeVelocity` the crash-quality formula already computes. This is intentional: `PlaneController.SpeedMultiplier` is the unconsumed seam Pass 2 is expected to drive up during boost, and once it does, clearing `m_toughBreakSpeed` naturally requires boosting — no rework needed here.

`Building_Tough.prefab` (`Assets/_Project/Prefabs/Buildings/`) duplicates `Building_Normal.prefab` with `m_buildingType: Tough` and a distinct placeholder material (`Mat_Building_Tough.mat`, dark red tint) — no new art asset, just a different `_BaseColor`. One instance is manually placed in `Prototype.unity` alongside `Building_Normal`.

**Known limitation, unchanged by this pass:** [`FuelSystem`](fuel-system.md) still hardcodes a single `m_building` reference (a Milestone-1 scaffold constraint — see fuel-system.md's "Scene/prefab constraint"). With two buildings now in the scene, only whichever one `FuelSystem` is wired to will actually refuel on crash; the other still crashes/resists correctly (visible via Debug.Log and physical bounce) but won't feed the fuel system. Revisit once multiple simultaneous crashable buildings are the norm (likely the Obstacles milestone) rather than a Prototype-scope fix.
