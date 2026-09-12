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

This is the whole of Milestone 1's UI scope — no score/health/menu UI belongs here (Milestone 2+).
