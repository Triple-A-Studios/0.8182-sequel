# Dependencies

Third-party packages/plugins used by the project, beyond default Unity modules.

| Package | Purpose | Notes |
|---|---|---|
| `com.annulusgames.alchemy` | Inspector/attribute extensions | Pulled via git URL from `annulusgames/Alchemy` |
| `com.triple-a-studios.core-setup` | Editor/project core setup utilities | Pulled from private repo `Triple-A-Studios/TripleA-CoreSetup` |
| `com.triple-a-studios.utils` | Gameplay/editor helper utilities | Pulled via git URL from `Triple-A-Studios/TripleA-Utils` |
| `com.unity.inputsystem` | New Input System | Action map: `Assets/_Project/Settings/InputSystem_Actions.inputactions` |
| `com.unity.render-pipelines.universal` | URP rendering | Separate PC/Mobile render pipeline assets in `Assets/_Project/Settings` |
| `com.unity.cinemachine` (3.1.5) | Camera work | Third-person follow cam, crash-impact shake/juice |
| `com.unity.modules.uielements` | UI Toolkit (UITK) runtime | Project's chosen UI framework — build screens with UITK, not uGUI |
| `com.unity.ugui` | uGUI runtime | Present as an engine-level dependency only; not used to build UI screens (UITK is) |
| `com.unity.ai.navigation` | NavMesh/AI navigation | Not yet used in any script |
| `com.unity.timeline` | Timeline/cutscene sequencing | Not yet used in any script |
| `com.unity.visualscripting` | Visual scripting | Not yet used in any script |
| `com.unity.test-framework` | NUnit/Unity Test Framework | Present in manifest; testing preference is play-mode only for this project (see CLAUDE.md) |
