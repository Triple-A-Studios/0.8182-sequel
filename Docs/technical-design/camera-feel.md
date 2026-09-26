# Camera Feel (Feel polish, Pass 7)

`Assets/_Project/Scripts/CameraFeel/CameraJuice.cs`, namespace `Opoint8182.CameraFeel` — deliberately not `Opoint8182.Camera`, to avoid shadowing `UnityEngine.Camera` inside a same-named namespace. Lives on `Prototype.unity`'s `CM Plane Follow Cam` GameObject, alongside its existing `CinemachineCamera` + `CinemachineFollow` (`FollowOffset: (0,3,-10)`, base `PositionDamping: (0.5,0.5,0.5)`, base FOV `40`). Before this pass, zero juice code existed anywhere in the project — no shake, no tweening library, no Cinemachine Impulse components.

Tone directive this pass works to (design-doc.md §4): *"Cartoonish, over-the-top destruction — chunky debris, screen shake, silly rather than gritty. The feeling should be satisfying/comedic impact, not tense realism."*

## Why manual `FollowOffset` jitter, not Cinemachine's Impulse system

Cinemachine 3.1.7 ships `CinemachineImpulseSource`/`CinemachineImpulseListener` for exactly this job, but they were never used in this project, and hand-authoring their full serialized YAML (`ImpulseDefinition`'s nested shape/duration/dissipation fields) blind — the way this project hand-edits scenes while the Editor is live — had no existing example in the scene to copy the exact layout from, unlike `AnimationCurve` (5 working examples in `SpawnManager` before Pass 2 needed one). Shake is instead a hand-rolled decaying jitter added to `CinemachineFollow.FollowOffset` each frame, entirely inside `CameraJuice`'s own fields — no new Cinemachine component types needed, same low-risk "new fields on a new `MonoBehaviour`" pattern every prior pass used.

## The five effects

All driven from one `Update()` on `CameraJuice`, reading `PlaneController.IsBoosting`/`SteerInput` (both already public, the second added this pass) and public methods `PlayerManager` calls into from its existing crash/hit/depleted handlers:

- **Follow-tighten on boost**: lerps the live `CinemachineFollow.TrackerSettings.PositionDamping` toward a tighter value (`(0.2,0.2,0.2)` default) while boosting, back to the cached base otherwise — eased via `Time.deltaTime * m_dampingLerpSpeed`, never a snap.
- **Boost FOV**: sustained FOV kick while boosting (`+4`), plus a separate one-shot punch (`+6`, decaying) that fires only on the boost engage edge (`IsBoosting` false→true) — two independent knobs so "boost feels stronger" and "boost just kicked in" tune separately. Written through a full `LensSettings` read-modify-write (`var lens = m_vcam.Lens; lens.FieldOfView = ...; m_vcam.Lens = lens;`) since `Lens` is a struct property, not a mutable reference.
- **Steering roll**: eases Cinemachine's built-in `Lens.Dutch` (camera Z-tilt) toward `-SteerInput.x * m_maxRollAngle` (default max `6°`). Deliberately drives `Dutch`, not `transform.localRotation` directly — `CinemachineFollow`'s own `TrackerSettings`/`BindingMode` already controls the vcam's rotation each frame, so writing the transform directly would fight it; `Dutch` is Cinemachine's dedicated escape hatch for an independent camera tilt on top of whatever the body/aim components are doing.
- **Shake**: `Shake(magnitude, duration)` sets runtime state; each `Update`, while time remains, adds `Random.insideUnitSphere * (magnitude * linearFalloff)` (Z zeroed, so shake never pushes the camera through/away from the plane) on top of the cached base `FollowOffset`. Three call-site wrappers, each with independent magnitude/duration tuning: `ShakeForCrash(quality)` (floored at `quality 0.3` so even a mistimed hit still reads as impact), `ShakeForHit()`, `ShakeForDepleted()` (the biggest of the three — run-ending ground hit / fuel-or-health-zero).
- **Hit-stop**: `HitStop()` sets `Time.timeScale = 0.05` for `0.08` real-time seconds (`WaitForSecondsRealtime`, so the freeze duration isn't itself scaled away to nothing), then restores `1f`. `OnDisable` also unconditionally resets `Time.timeScale = 1f` as cheap insurance against a coroutine getting cut off mid-freeze.

## Trigger wiring — `PlayerManager`

`PlayerManager` already centralizes every crash/hit/depleted reaction (`HandleCrashed`/`HandleDamageDealt`/`HandleDepleted`/`HandleFlyAway`); this pass adds one more cross-reference (`m_cameraJuice`, wired the same `PrefabInstance.m_Modifications` way as `m_followCamera`/`m_hud`, since `CameraJuice` lives on the scene-only `CM Plane Follow Cam`, not a `Plane`-prefab sibling) rather than giving `CameraJuice` its own independent event subscriptions — one subscriber set, matching the "minimal gate" precedent from Pass 5's game-flow work.

- `HandleCrashed`: `ShakeForCrash(quality)` always fires. `HitStop()` additionally fires only when `countsForCombo && quality >= 1f && source is Building building && building.BuildingType == BuildingType.Tough` (new `Building.BuildingType` public accessor, alongside the existing `Damage`/`ScoreValue`).
- `HandleDamageDealt`: `ShakeForHit()`.
- `HandleDepleted` (ground hit, fuel/health zero): `ShakeForDepleted()`.
- `HandleFlyAway` (ceiling/lateral hard bound): no shake — this path is deliberately the non-violent "camera stops following, plane drifts off" treatment (`m_followCamera.enabled = false`), already established in Pass 1/game-flow work; adding an impact shake here would contradict that intent.

### Why the hit-stop gate is Tough-only, not just quality-based

`Building_Normal.prefab`'s `m_referenceMaxSpeed: 10` is below the plane's base cruise speed (`20`), so `Building.OnTriggerEnter`'s `quality = Clamp01(impactVelocity.magnitude / m_referenceMaxSpeed)` (`Building.cs`) is already pinned at `1.0` on almost every Normal weak-point hit, boosted or not — gating hit-stop on `quality >= 1f` alone would fire on nearly every Normal crash, cheapening the effect. `Building_Tough.prefab`'s `m_referenceMaxSpeed: 25` is above base cruise, so a true `quality >= 1f` hit there is a real, boost-dependent feat (`20 * 1.6 = 32 > 25`) worth punctuating — the explicit `BuildingType.Tough` check in `HandleCrashed` is load-bearing, not a stylistic preference.

## Scene wiring

`CameraJuice` was added as a new `MonoBehaviour` on `CM Plane Follow Cam` (hand-edited into `Prototype.unity`, fresh script GUID). Its `m_planeController` field reuses the Plane prefab instance's already-existing stripped `PlaneController` reference (`fileID: 900000060005`, the same stripped entry other cross-references in this scene already point at) rather than creating a new one. All of `CameraJuice`'s tuning fields (damping/FOV/roll/shake/hit-stop numbers) were left out of the hand-authored YAML block entirely and rely on their C# field-initializer defaults applying on first load — this only works because they're plain top-level `MonoBehaviour` fields on a freshly-added component (a class, always constructed via its C# initializers before YAML overlay), not elements of an existing serialized struct array (`SpawnableEntry`/`SpawnChunk`'s gotcha from `spawning.md` doesn't apply here).
