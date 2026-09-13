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
