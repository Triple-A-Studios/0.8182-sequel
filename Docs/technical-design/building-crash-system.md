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

`Building` destruction is unconditional on any qualifying player collision — Milestone 2 ("Building types + scoring") is where a tough-building variant that resists non-boosted crashes belongs; this system has no such gating.
