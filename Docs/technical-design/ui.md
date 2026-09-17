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
