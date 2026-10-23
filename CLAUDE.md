# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Opoint8182 (sequel)" — a solo hobby project, casual endless arcade / vehicular-destruction game. Player flies a plane with limited fuel and deliberately crashes into buildings (aiming for a visible weak point) to refuel; crash precision determines refuel amount. Endless run, ends when fuel hits zero. Full design context, resolved decisions, and open questions live in [Docs/design-doc.md](Docs/design-doc.md) — read it before making gameplay/design decisions, since some mechanics (daily-attempt retention structure, monetization guardrails, the weekly/all-time leaderboard structure) are specified there and not yet in code.

The project is in the **Alpha** phase. The prototype and MVP phases are done (the full core loop is playable: flying, fuel + health, tough buildings, obstacles, combos, spawning with a difficulty ramp, run-end causes, a live Unity Leaderboards daily board) and the Alpha "UI/art pass" milestone has landed (the whole UI Toolkit UI, verified on Android and WebGL). What is next — the 3D art pass, Daily play structure, Pause/settings/audio, Player profile & run stats — is not scoped yet. [Docs/current-state.md](Docs/current-state.md) is the live snapshot, [Docs/plan.md](Docs/plan.md) the roadmap and [Docs/progress.md](Docs/progress.md) the history and backlog; check them to see what is being targeted before assuming scope.

## Engine & tooling

- Unity **6000.6.3f1** (Unity 6, tech stream), Universal Render Pipeline 17.6.
- Input via the new Input System (`com.unity.inputsystem`) — action map asset at [Assets/_Project/Settings/InputSystem_Actions.inputactions](Assets/_Project/Settings/InputSystem_Actions.inputactions). On-screen touch controls are part of the HUD (UITK), not the Input System's on-screen components.
- Two render pipeline asset pairs under `Assets/_Project/Settings`: `PC_RPAsset`/`PC_Renderer` and `Mobile_RPAsset`/`Mobile_Renderer` — the project targets both desktop (editor/dev) and mobile/web, so don't assume a single quality tier.
- Notable third-party packages (see `Packages/manifest.json`): `com.annulusgames.alchemy` (inspector/attribute extensions), `com.triple-a-studios.core-setup`, `.utils` and `.statemachine` (pulled from private git repos — Editor and gameplay helpers, singletons, observables, state machine), `com.unity.cinemachine` (camera work — follow/shake for the third-person chase cam and crash-impact juice), `com.kyrylokuzyk.primetween` (tweens/delays; a local `.tgz`), `com.unity.services.authentication` + `com.unity.services.leaderboards` (Unity Gaming Services: anonymous sign-in and the leaderboard, behind one `LeaderboardService`).
- **UI is built with UI Toolkit (UITK), not uGUI.** `com.unity.modules.uielements` (UITK runtime, built into Unity 6) is the package to reach for when adding UI — don't add Canvas/uGUI-based UI even though `com.unity.ugui` is still present in the manifest (kept as an engine-level dependency, not for building UI screens). Use `PanelRenderer` (not the frozen `UIDocument`), the single shared `PanelSettings` asset (`Settings/FuelGaugePanelSettings.asset`, 2400x1080 reference, Match 0.5), the theme and components in `UI/Theme/Theme.uss` and `Scripts/UI/Foundation` (`LipBox`, `LipButton`, `TabBar`, `ThemedSlider`, `ScreenRouter`, `SafeAreaHelper`, `UiToast`), and **read [Docs/technical-design/ui.md](Docs/technical-design/ui.md) before touching any UI** — it has the structure, the USS limits and gotchas, the rules (safe-area flow layout, 80 reference px touch targets, 24 px minimum text, glyphs only from the baked set), the placeholder-data registry and the dev galleries.
- There is no CI, no asmdef split yet (everything is in the default assembly), and no automated test suite. Verification is the developer opening/running the project in the Unity Editor, on an Android phone and in WebGL. For checks from outside the Editor the Unity CLI (`unity command ...`: `eval_file`, `console`, `editor_play`, `open_scene`, `recompile`, `build`, screenshots by scheduling `ScreenCapture`) can drive a running Editor; `Application.runInBackground = true` is needed for play mode to advance when the Editor is not focused (more in the "Driving the UI from outside the Editor" notes in `ui.md`).

## Structure

- `Assets/_Project/` — all first-party content: `Scripts` (one folder per system: `Altitude`, `Bootstrap`, `Building`, `CameraFeel`, `Common`, `Fuel`, `Game`, `Health`, `Lateral`, `Leaderboard`, `Obstacle`, `Pickup`, `Player`, `Score`, `Spawning`, `UI`), `UI` (UXML/USS), `Prefabs`, `Scenes`, `Materials`, `Animation`, `Art` (`Fonts`, `Textures/UI`), `Settings`.
- Scenes (`Assets/_Project/Scenes`): `Bootstrap` (the entry scene: loading screen, loads the other two additively), `MainMenu`, `Prototype` (the game). `Scenes/Dev` holds the dev galleries (`UIGallery`, `MenuScreensGallery`, `HudGallery`, `GameOverGallery`, `LaunchFlowGallery`) that are **not** in Build Settings. Play from `Bootstrap` for the real flow; `MainMenu` and `Prototype` also run on their own.
- `Assets/_IGNORE/` — excluded scratch/reference content, not part of the shipped project.
- `Docs/` — design documentation. `design-doc.md` is the source of truth for game design decisions (update it when a decision it documents actually changes, rather than letting it drift from the implementation); `plan.md` / `progress.md` / `current-state.md` are the roadmap, history and live snapshot; `technical-design/` has one file per system; `Mockups/` holds the UI mockups and gap analysis.
- `Tools/generate_ui_textures.py` regenerates the baked UI textures (Pillow). `Tools/generate_placeholder_sfx.py` regenerates the placeholder sound effects (stdlib only); audio is documented in [Docs/technical-design/audio.md](Docs/technical-design/audio.md).

## Code style

Naming is enforced by `.editorconfig` (ReSharper naming rules) — follow it for all C# code:
- Private instance fields: `m_` prefix + camelCase (`m_maxFuel`, `m_planeController`), including `[SerializeField] private` fields.
- Protected fields: `_` prefix + camelCase.
- Private constants: `k_` prefix + PascalCase (`k_MaxSpeed`). Non-private constants: `ALL_UPPER_SNAKE_CASE`.
- Private static readonly: `m_S_` prefix + PascalCase; private static non-readonly: `m_s_` prefix + camelCase. Non-private static readonly: `S_` prefix + PascalCase.
- Public/non-private instance fields (rare — prefer properties): plain camelCase, no prefix.
- Indentation in `.cs` files is tabs, not spaces (`.editorconfig` overrides the repo-wide space default for this extension).
- No `[ShowInInspector]` / `[ReadOnly]` Alchemy fields for debug-only values: use the Inspector's Debug mode instead.

## Platform constraints

- Landscape-only. Android builds are locked to landscape; the web build detects a portrait page on the loading screen and shows a "rotate your device" prompt (it holds the hand-off to the menu until the page is landscape).
- Build/deploy: iterate in the Editor, check on an Android phone and on WebGL (the developer uses a private itch.io draft page for the phone-browser check); a public web deploy (itch.io or a custom site) is for after the Alpha systems are in. The project builds WebGL with Brotli and no decompression fallback (an itch upload may need Gzip or the fallback), and every WebGL build rewrites the tracked `Data/Plugins/lib_burst_generated.wasm` (restore it with `git checkout` rather than committing it).

<!-- game-dev-docs:start -->
## Game Dev Docs

This project uses the `game-dev-docs` skill. Before doing any work in a fresh session:
1. Read `Docs/current-state.md` first — the live snapshot of what's landed and what's next.
2. Read `Docs/plan.md` and `Docs/progress.md` for phase/milestone context as needed.
3. Read the relevant file(s) in `Docs/technical-design/` before touching a system you haven't worked in this session.

**Project settings**
- Engine: Unity
- Unity testing: Play-mode only
- Co-author line: never add it
- docs-conformant: true

**Working hygiene**
- One git branch per milestone.
- Never commit without the developer's explicit manual verification.
- Commit messages: use the `caveman` skill if installed, else keep them short and plain.
- Update `Docs/current-state.md` after every commit.
<!-- game-dev-docs:end -->
