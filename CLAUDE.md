# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Opoint8182 (sequel)" — a solo hobby project, casual endless arcade / vehicular-destruction game. Player flies a plane with limited fuel and deliberately crashes into buildings (aiming for a visible weak point) to refuel; crash precision determines refuel amount. Endless run, ends when fuel hits zero. Full design context, resolved decisions, and open questions live in [Docs/design-doc.md](Docs/design-doc.md) — read it before making gameplay/design decisions, since many mechanics (tough buildings, health as a second resource, obstacles, daily-attempt retention structure, monetization guardrails) are specified there and not yet in code.

The project is at an early skeleton stage: no gameplay scripts exist yet (`Assets/_Project/Scripts` is currently empty). The design doc's five-milestone prototype/MVP plan is the roadmap — check it to see what milestone is being targeted before assuming scope.

## Engine & tooling

- Unity **6000.0.79f1** (Unity 6), Universal Render Pipeline.
- Input via the new Input System (`com.unity.inputsystem`) — action map asset at [Assets/_Project/Settings/InputSystem_Actions.inputactions](Assets/_Project/Settings/InputSystem_Actions.inputactions).
- Two render pipeline asset pairs under `Assets/_Project/Settings`: `PC_RPAsset`/`PC_Renderer` and `Mobile_RPAsset`/`Mobile_Renderer` — the project targets both desktop (editor/dev) and mobile/web, so don't assume a single quality tier.
- Notable third-party packages (see `Packages/manifest.json`): `com.annulusgames.alchemy` (inspector/attribute extensions), `com.triple-a-studios.core-setup` and `com.triple-a-studios.utils` (pulled from private git repos — Editor and gameplay helper utilities), `com.unity.cinemachine` (camera work — follow/shake for the third-person chase cam and crash-impact juice).
- **UI is built with UI Toolkit (UITK), not uGUI.** `com.unity.modules.uielements` (UITK runtime, built into Unity 6) is the package to reach for when adding UI — don't add Canvas/uGUI-based UI even though `com.unity.ugui` is still present in the manifest (kept as an engine-level dependency, not for building UI screens).
- There is no CI, no asmdef split yet, and no automated test suite — this project doesn't have a build/lint/test CLI workflow; verification is done by opening/running the project in the Unity Editor.

## Structure

- `Assets/_Project/` — all first-party content (Scripts, Prefabs, Scenes, Materials, Animation, Art, Settings). Only scene so far: `Scenes/Prototype.unity`.
- `Assets/_IGNORE/` — excluded scratch/reference content, not part of the shipped project.
- `Docs/` — design documentation (`design-doc.md`). Treat this as the source of truth for game design decisions; update it when a design decision it documents actually changes, rather than letting the doc drift from the implementation.

## Platform constraints

- Landscape-only. The web build must detect portrait orientation on Android browsers and prompt the player to rotate.
- Build/deploy order per the design doc: iterate in-editor first, test on Android once a substantial milestone set is built, deploy web (Vercel or itch.io) only after full MVP (all five prototype milestones) is complete.

<!-- game-dev-docs:start -->
## Game Dev Docs

This project uses the `game-dev-docs` skill. Before doing any work in a fresh session:
1. Read `Docs/current-state.md` first — the live snapshot of what's landed and what's next.
2. Read `Docs/plan.md` and `Docs/progress.md` for phase/milestone context as needed.
3. Read the relevant file(s) in `Docs/technical-design/` before touching a system you haven't worked in this session.

**Project settings**
- Engine: Unity
- Unity testing: Play-mode only
- Co-author line: ask once per milestone

**Working hygiene**
- One git branch per milestone.
- Never commit without the developer's explicit manual verification.
- Commit messages: use the `caveman` skill if installed, else keep them short and plain.
- Update `Docs/current-state.md` after every commit.
<!-- game-dev-docs:end -->
