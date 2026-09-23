# UI (UI Toolkit)

Project convention: UI Toolkit (UITK), not uGUI (see [CLAUDE.md](../../CLAUDE.md)). First UI in the project — establishes the folder/asset layout for future HUD/menu work.

## Asset layout

- `Assets/_Project/Settings/FuelGaugePanelSettings.asset` — `PanelSettings` ScriptableObject. **Created via the Editor menu** (`Assets > Create > UI Toolkit > Panel Settings Asset`), not hand-authored — it wires an internal default theme reference (`Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss`, Unity's own auto-generated default, committed as a normal dependency) that isn't safe to reconstruct by hand. Scale Mode = Scale With Screen Size, reference resolution ~1920x1080 (landscape, matching the project's landscape-only target), Match 0.5.
- `Assets/_Project/UI/FuelGauge.uxml` / `FuelGauge.uss` — plain text, hand-authored directly (safe, unlike PanelSettings — no embedded GUID fragility at this level).
- `Assets/_Project/Scripts/UI/FuelGaugeUI.cs`, namespace `Opoint8182.UI`.

`UI/` is a new top-level `_Project` folder (visual/behavioral assets), distinct from `Settings/` (config-style ScriptableObjects, where `PanelSettings` lives to match the render-pipeline-asset convention).

## Scene setup

`HUD` GameObject in `Prototype.unity` (top-level, not nested under `Plane` — not tied to plane lifetime): `UIDocument` (Source Asset = `FuelGauge.uxml`, Panel Settings = `FuelGaugePanelSettings.asset`) + `FuelGaugeUI`, wired to the `Plane`'s `FuelSystem` component (a plain scene-to-scene reference, no prefab constraint here since both are scene objects).

## Update pattern

`FuelGaugeUI` queries the fill element once via UQuery (`rootVisualElement.Q<VisualElement>("fuel-gauge-fill")`) in `OnEnable`, then subscribes to [FuelSystem](fuel-system.md)'s `ObservableFloat` (`AddListener`) and pushes the initial value immediately — `AddListener` alone doesn't fire retroactively for the current value. Each change sets `style.width = new StyleLength(Length.Percent(fraction * 100f))`. Unsubscribes in `OnDisable`.

This was the whole of Milestone 1's UI scope. No health UI exists yet — [HealthSystem](health-system.md) shipped Pass 3 with only a debug Inspector field, same as `FuelSystem` had before this UI existed; a health bar isn't scoped into this milestone at all.

## Score UI (Milestone 2 Pass 4)

Second HUD element, `ScoreDisplay` GameObject (sibling of the fuel gauge's `UIDocument` object, both children of `HUD`): `UIDocument` (Source Asset = `Score.uxml`, Panel Settings = the *same* `FuelGaugePanelSettings.asset` — `PanelSettings` is a shared config asset by design, reusing it avoids another Editor-menu-only asset creation for a second HUD element) + `ScoreUI.cs`, wired to the `Plane`'s `ScoreSystem`.

`ScoreUI` mirrors `FuelGaugeUI`'s update pattern exactly, just with a `Label` instead of a fill bar: queries it once in `OnEnable` (`rootVisualElement.Q<Label>("score-label")`), subscribes to [ScoreSystem](score-system.md)'s `ObservableInt`, and sets `.text = $"Score: {score}"` on every change (plus once immediately on enable).

## Combo label (Milestone 3 Pass 3)

Second `Label` in `Score.uxml` (`combo-label`, `.combo-label` in `Score.uss` — same look as the score label, positioned just below it, gold-tinted to stand out). `ScoreUI` queries it alongside the score label and subscribes to [`ScoreSystem.MultiplierValue`](score-system.md#combo-multiplier) the same way, setting `.text = $"x{multiplier}"` on every change plus once on enable. Always visible (shows `x1` at baseline) rather than hidden below x2 — simplest option, no visibility-toggling logic needed for a prototype-scope HUD element.

A third element, `combo-timer-fill` (a `fuel-gauge-fill`-style bar, not a label — `combo-timer-background`/`.combo-timer-fill` in `Score.uss`, gold-tinted, positioned below the combo label), shows the countdown to the next step-down. `ScoreUI` subscribes to `ScoreSystem.ComboTimerValue` and sets `style.width` from `ScoreSystem.ComboTimerFraction`, same width-percent pattern [`FuelGaugeUI`](#scene-setup) already uses for the fuel gauge.

## Game Over screen (Movement & fail-state rework, Pass 3)

Third HUD element, `GameOver` GameObject (sibling of the fuel gauge and `ScoreDisplay` `UIDocument`s, all children of `HUD`): `UIDocument` (Source Asset = `GameOver.uxml`, Panel Settings = the same shared `FuelGaugePanelSettings.asset`) + `GameOverUI.cs`, wired to the `Plane`'s `ScoreSystem`. Unlike the other two HUD elements, it's hidden by default and only appears once the run ends — the first interactive/toggling element in the project.

`GameOverUI` subscribes to `GameManager`'s `public event Action RunEnded` — already fired by `GameManager` on either `FuelSystem.Depleted`/`HealthSystem.Depleted`, previously with zero subscribers. Two ordering details matter here, both because `GameManager` is a `GenericSingleton<GameManager>` (`TripleA.Utils.Singletons`):

- **Subscribes from `Start()`, not `OnEnable()`.** Unity only guarantees every object's `Awake()` runs before any object's `Start()` — there's no such guarantee between `Awake()` and `OnEnable()` of *different* objects. `GenericSingleton<T>` sets its static instance in `Awake()`, so subscribing from `OnEnable()` risked silently never subscribing if `HUD`'s subtree happened to be processed before `GameManager`'s (confirmed in testing: no exception, `GameManager.IsRunActive` still correctly flipped to `false`, but the screen never appeared, since `TryGetInstance()` returned `null` before `GameManager`'s own `Awake` had run).
- **Uses `GameManager.TryGetInstance()`, not `.Instance`.** `TryGetInstance()` returns `null` if the singleton hasn't been created yet instead of `GenericSingleton`'s `.Instance` auto-instantiating a stand-in `GameManager` GameObject — a stand-in would never receive `RunEnded` from the real fuel/health systems anyway, so a quiet no-op is preferable to a broken auto-created object.

The root `game-over-root` `VisualElement`'s `style.display` starts at `DisplayStyle.None` (set in code on `OnEnable`, queried once via UQuery same as the other HUD elements) and flips to `DisplayStyle.Flex` when `RunEnded` fires — a full-screen centered takeover (`position: absolute` pinned to all four edges plus flex centering in `GameOver.uss`), unlike the corner-anchored fuel/score HUD. The final score is read once, synchronously, from `ScoreSystem.CurrentScore` at the moment `RunEnded` fires — not a live `AddListener` subscription like `ScoreUI` uses, since the score is frozen the instant the run ends (`PlayerManager` disables `PlaneController` on the same event).

The Restart `Button`'s `.clicked` event calls `SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)` — a full scene reload, chosen over building manual `Reset()`/`ResetState()` methods on every system, since `GameManager`/`FuelSystem`/`HealthSystem`/`ScoreSystem`/`PlayerManager` are all scene-scoped with no persistent/`DontDestroyOnLoad` singleton behavior — a reload cleanly resets all of them for free.

## Altitude warning (Movement & fail-state rework, Pass 4)

Fourth HUD element, `AltitudeWarning` GameObject (sibling of `UIDocument`/`ScoreDisplay`/`GameOver`, all children of `HUD`): `UIDocument` (Source Asset = `AltitudeWarning.uxml`, Panel Settings = the same shared `FuelGaugePanelSettings.asset`) + `AltitudeWarningUI.cs`, wired to the `Plane`'s [`AltitudeSystem`](altitude-system.md).

Unlike `GameOverUI`, `AltitudeSystem` is a plain sibling-of-`Plane` component, not `GenericSingleton`-backed, so `AltitudeWarningUI` subscribes from `OnEnable()` same as `FuelGaugeUI`/`ScoreUI` — the Awake/OnEnable ordering risk documented above only applies to singleton lookups. Binds to two observables: `IsWarningValue` (`ObservableBool`) toggles `altitude-warning-root`'s `style.display` (mirrors `GameOverUI`'s show/hide), `WarningFractionValue` (`ObservableFloat`) drives `altitude-warning-fill`'s `style.width` percent (mirrors `ScoreUI`'s combo-timer-fill). Both are pushed once on `OnEnable` in addition to `AddListener`, matching every other HUD element's established convention.

## Lateral warning (Feel polish, Pass 1)

Fifth HUD element, `LateralWarning` GameObject (sibling of the other four, all children of `HUD`): `UIDocument` (Source Asset = `LateralWarning.uxml`, Panel Settings = the same shared `FuelGaugePanelSettings.asset`) + `LateralWarningUI.cs`, wired to the `Plane`'s [`LateralSystem`](lateral-system.md).

Simpler than `AltitudeWarningUI` — `LateralSystem` has no countdown/fraction (see [lateral-system.md](lateral-system.md)), so `LateralWarningUI` binds only `IsWarningValue` (`ObservableBool`) to toggle `lateral-warning-root`'s `style.display`, no fill-bar element at all. Same `OnEnable` subscribe-and-push-once pattern as every other HUD element.

## Health gauge (Feel polish, Pass 2)

Sixth HUD element, `HealthGauge` GameObject (sibling of the other five, all children of `HUD`): `UIDocument` (Source Asset = `HealthGauge.uxml`, Panel Settings = the same shared `FuelGaugePanelSettings.asset`) + `HealthGaugeUI.cs`, wired to the `Plane`'s [`HealthSystem`](health-system.md). Resolves backlog #16.

A structural copy of `FuelGaugeUI`/`FuelGauge.uxml`/`.uss` — `HealthSystem` already exposed a matching `HealthValue`/`HealthFraction` API (`Health` as a resource has mirrored `Fuel`'s shape since it was introduced), so no changes to `HealthSystem` itself were needed, only the UI layer. `HealthGaugeUI` queries `health-gauge-fill` once in `OnEnable`, subscribes to `HealthValue.AddListener`, and drives `style.width` from `HealthFraction` — identical update pattern to `FuelGaugeUI`. Positioned directly below the fuel gauge in the HUD's left column (`top: 56px`, same `220x28` size), green fill (`rgb(60, 200, 60)`) to read as a distinct resource from fuel's orange at a glance.

## Touch controls (Feel polish, Pass 3)

Seventh HUD element, `TouchControls` GameObject (sibling of the other six, all children of `HUD`): `UIDocument` (Source Asset = `TouchControls.uxml`, Panel Settings = the same shared `FuelGaugePanelSettings.asset`) + `TouchControlsUI.cs`, wired to the `Plane`'s [`PlaneController`](movement.md). The first interactive (not just display) HUD element in the project — genuinely greenfield, no prior UITK pointer-event or on-screen-control code existed anywhere in the codebase.

Unity's built-in `OnScreenStick`/`OnScreenButton` (`UnityEngine.InputSystem.OnScreen`) require uGUI (`Image`/`Canvas`/`EventSystem`), which the project's UITK-only convention (see [CLAUDE.md](../../CLAUDE.md)) rules out for UI screens, so this is hand-built directly on `VisualElement`s using raw pointer events — a deliberate developer decision after confirming the built-in components weren't usable here.

**Joystick** (`touch-joystick-background` containing `touch-joystick-handle`): `PointerDownEvent` on the background captures the pointer (`VisualElement.CapturePointer`) and records the element's `worldBound.center`; `PointerMoveEvent` (guarded by `HasPointerCapture`) computes the delta from center, clamps its magnitude to `m_joystickMaxRadius` (50px default), offsets the handle visually via `style.translate`, and writes the normalized value onto `PlaneController.TouchSteer` — Y is inverted (UI space grows downward, `PlaneController`'s steer Y is "up positive", matching `Velocity.y`). `PointerUpEvent`/`PointerCancelEvent` release capture, snap the handle back, and zero `TouchSteer`.

**Boost button** (`touch-boost-button`): same capture-on-down/release-on-up pattern, toggling `PlaneController.TouchBoost` instead of a continuous value.

**Merge strategy — additive, not exclusive**: rather than branching on platform, `PlaneController.FixedUpdate` adds `TouchSteer` directly onto the Input System's `Move` action read (`Vector2.ClampMagnitude(..., 1f)`) and ORs `TouchBoost` into the `Sprint` action's `IsPressed()` check. Both control paths are always live — keyboard/gamepad and touch simply combine, so there's no platform-detection code and the touch HUD can be exercised with a mouse in the Editor for free (a deliberate developer decision — the touch HUD renders unconditionally for now rather than being gated behind a touch-capability check; revisit if it looks cluttered on desktop). This also keeps the project's established UI→gameplay dependency direction intact: `TouchControlsUI` holds a reference to `PlaneController` and pushes values into two new public settable properties (`TouchSteer`, `TouchBoost`), mirroring the existing `SpeedMultiplier { get; set; }` precedent — `PlaneController` has no dependency on the `UI` namespace.
