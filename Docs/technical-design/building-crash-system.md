# Building & Crash System

`Building.cs` (`Assets/_Project/Scripts/Building/Building.cs`) and `WeakPoint.cs` (`Assets/_Project/Scripts/Building/WeakPoint.cs`), namespace `Opoint8182.Building`.

## Collider / marker split

The building has one `BoxCollider` — a trigger (`m_IsTrigger: 1`, Pass 5) rather than a solid collider — plus a plain `WeakPoint` marker component (no collider of its own) on a child object. `Building.OnTriggerEnter` compares the closest point on the plane's own `CapsuleCollider` to the marker's world position (`other.ClosestPoint(m_weakPoint.Position)`) against a tunable `hitRadius`.

This is a *replacement* of the building's one collider, not a second, inset trigger collider stacked on top of a still-solid main collider (an idea earlier passes considered and rejected) — that idea was dropped because Unity doesn't guarantee ordering between a solid collision and an overlapping trigger event in the same physics step, and an inset trigger risked becoming a hole the plane could clip through. Pass 5 sidesteps that hazard entirely by not having two colliders to order in the first place: the single existing `BoxCollider` just stops being solid.

Hierarchy is flexible — nesting the weak point under a `Visual` wrapper (matching `Plane.prefab`'s physics/visual split) works fine, since both the distance check and the alignment check operate on world-space `transform.position`/`transform.forward`, independent of hierarchy depth.

## Trigger-based detection (Pass 5)

**Resolved bug:** the plane visibly stopped for one frame on crash before `Destroy` ran (`Docs/progress.md` backlog). Cause: with a solid `BoxCollider` on `Building` and no `Rigidbody` on it, PhysX resolved the collision response (clamping the plane's dynamic `Rigidbody` velocity against the static building) *before* dispatching `OnCollisionEnter` that same physics tick — and `Destroy` doesn't pull the collider out of the physics world until end of frame, so that tick's stop was already baked in. `PlaneController.FixedUpdate` unconditionally overwrites velocity from input every tick with no momentum carried over, so the very next tick snapped velocity back — the stutter was exactly that one-tick gap.

Fix: `Building`'s `BoxCollider` is now a trigger. Trigger overlaps skip collision resolution entirely, so PhysX never applies a stopping impulse to the plane on contact — there's nothing left to cause the stutter. `Building.OnTriggerEnter(Collider other)` replaces `OnCollisionEnter(Collision collision)`, with two substitutions for data a `Collision` provided that a `Collider` doesn't:

- **Impact velocity:** `other.attachedRigidbody.linearVelocity` replaces `collision.relativeVelocity`. The building has no `Rigidbody` (static as far as PhysX is concerned), so its velocity is always zero — `relativeVelocity = velocityPlane − velocityBuilding` collapses to exactly the plane's own velocity, so this is an exact equivalent, not an approximation. It also reads the plane's live, un-clamped, input-driven velocity, since there's no resolution step to worry about pre/post-impact anymore.
- **Contact point** (for the weak-point distance check): `other.ClosestPoint(m_weakPoint.Position)` replaces `collision.GetContact(0).point` — the closest point on the plane's `CapsuleCollider` surface to the weak point, standing in for "how close did the plane's body get."

This doesn't apply to [`ObstacleBuilding`](obstacle-system.md) — it never calls `Destroy`, so there's no destroy-timing race for it, and its solid collider is intentional (the plane should feel a real physical obstruction from it).

## Crash-quality formula

```
impactVelocity = plane.attachedRigidbody.linearVelocity
speed01        = Mathf.Clamp01(impactVelocity.magnitude / referenceMaxSpeed)
quality        = hitWeakPoint ? speed01 : 0f
```

`referenceMaxSpeed` (tunable per building) is the speed at/above which quality maxes out — set above the plane's default cruise speed since no boost exists yet in Milestone 1. A hit at/above `referenceMaxSpeed` scores 1.0 ("perfect crash"). A hit on the building's body outside `hitRadius` is still a valid, physical crash but scores `0` — the design doc ([Docs/design-doc.md](../design-doc.md)) only defines the quality metric "into" the weak point; no partial-credit zone is invented for a body hit.

**Pass 2 (Movement & fail-state rework) dropped the angle-alignment term** (`dot(impactVelocity, -weakPoint.OutwardNormal)`) that used to multiply into `speed01`. Cause: Pass 1 made pitch purely cosmetic (see [movement.md](movement.md)), so the visual angle a player aims with no longer reliably matches the real impact velocity direction, making angle-precision unreadable by feel. Quality is now speed-only, gated on hitting the weak point radius at all — `WeakPoint.OutwardNormal` was removed as dead code along with it.

## Seam for later passes

`Building` fires `public event Action<float, int> Crashed` — crash quality plus the building's own `m_scoreValue` (see [Scoring](#scoring-milestone-3-pass-3) below) — immediately before destroying itself (`Destroy(gameObject)`, no VFX — deferred to the Feel Polish milestone). A `Debug.Log` and Alchemy `[ReadOnly, ShowInInspector]` debug fields (`_lastCrashQuality`, `_lastHitWeakPoint`) surface the quality result too, since no fuel/UI exists to display it (historical - fuel/score both consume it directly now).

## Building types (Milestone 2 Pass 1)

`BuildingType.cs` (`Assets/_Project/Scripts/Building/BuildingType.cs`) defines `Normal`/`Tough`. `Building.m_buildingType` picks the variant; `Tough` additionally needs `m_toughBreakSpeed` cleared by the impact velocity's magnitude before it will break at all — checked unconditionally (both weak-point and body hits), before the quality/refuel calculation runs. Below the threshold, `OnTriggerEnter` deals `m_toughHitDamage`, raises `Crashed` with `quality: 0f` and `countsForCombo: false`, and destroys the building anyway — a mistimed tough hit was reworked in Pass 3 to always break the building with base score rather than let it survive (see [Damage dealing](#damage-dealing-milestone-2-pass-3) below). Every branch of `Building.OnTriggerEnter` destroys the building; there is no surviving-building case. (Pass 5, Movement & fail-state rework, also dropped the physical bounce this section used to describe — the collider is a trigger now, so there's no solid-collider response left even for the instant before `Destroy` takes effect; see [Trigger-based detection](#trigger-based-detection-pass-5) above.)

Boost (Milestone 2 Pass 2) raises the plane's real speed via `PlaneController.SpeedMultiplier` rather than exposing a dedicated "is boosting" flag — the gate here is a plain impact-speed threshold, reusing the same impact velocity the crash-quality formula already computes. Boosting is simply the practical way to clear `m_toughBreakSpeed`; no rework was needed here when that pass landed.

`Building_Tough.prefab` (`Assets/_Project/Prefabs/Buildings/`) duplicates `Building_Normal.prefab` with `m_buildingType: Tough` and a distinct placeholder material (`Mat_Building_Tough.mat`, dark red tint) — no new art asset, just a different `_BaseColor`. One instance is manually placed in `Prototype.unity` alongside `Building_Normal`.

**Resolved (Milestone 3 Pass 1):** [`FuelSystem`](fuel-system.md) and [`HealthSystem`](health-system.md) previously each hardcoded a single source reference (a Milestone-1 scaffold constraint), so only `Building_Tough` ever refueled/damaged the plane and `Building_Normal` had no subscribers. Both systems now take arrays and subscribe to every entry, mirroring [`ScoreSystem`](score-system.md)'s pre-existing `Bldng[] m_buildings` pattern — see fuel-system.md's "Multi-source wiring" section.

## Damage dealing (Milestone 2 Pass 3)

`Building` implements [`IDamageDealer`](common.md) (`Assets/_Project/Scripts/Common/IDamageDealer.cs`): a `Damage` property backed by its own `m_toughHitDamage` tunable, and `public event Action<float> DamageDealt`, fired with that value from the same `!canBreak` branch described above, right before the early `return`. A mistimed tough-building hit is a distinct outcome from `Crashed` (broke, refuel), so it gets its own event rather than overloading `Crashed` with a zero/negative quality value.

Damage tuning lives here, on the source, rather than on whatever receives it — [`HealthSystem`](health-system.md) is the only current subscriber, but it has no idea the damage came from a building specifically; it just reacts to `IDamageDealer.DamageDealt`. A future damage source (large obstacle buildings, hazards) only needs to implement the interface — no changes needed on the receiving end.

## Scoring (Milestone 3 Pass 3)

`m_scoreValue` (default `10` on `Building_Normal`, `20` on `Building_Tough`) is a plain per-instance tunable, same "lives on the source" treatment as `m_toughHitDamage`/`m_referenceMaxSpeed` — exposed as `public int ScoreValue`. It's carried on `Crashed` alongside `quality` rather than requiring [`ScoreSystem`](score-system.md) to look it up separately, since a shared `Action<float, int>` subscriber (one handler for every building in an array, see `ScoreSystem.m_buildings`) has no other way to know which specific instance fired without per-instance closures. [`FuelSystem`](fuel-system.md) also had to widen its `HandleCrashed` signature to match even though it only uses `quality` — flagged as exactly the kind of coupling the planned "Core systems refactor" milestone exists to remove.
