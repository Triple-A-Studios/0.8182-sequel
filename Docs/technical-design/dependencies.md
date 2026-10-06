# Dependencies

Third-party packages/plugins used by the project, beyond default Unity modules.

| Package | Purpose | Notes |
|---|---|---|
| Unity Editor 6000.6.3f1 | Engine | Upgraded from 6000.0.79f1 (6.0 LTS, support ends 2026-10-16) in Alpha's "MVP bug fix" milestone Pass 1. 6.6 is a tech-stream release, not LTS (a developer call over 6.3 LTS). All private/third-party packages below compiled clean on upgrade |
| `com.annulusgames.alchemy` | Inspector/attribute extensions | Pulled via git URL from `annulusgames/Alchemy` |
| `com.triple-a-studios.core-setup` | Editor/project core setup utilities | Pulled from private repo `Triple-A-Studios/TripleA-CoreSetup` |
| `com.triple-a-studios.utils` | Gameplay/editor helper utilities | Pulled via git URL from `Triple-A-Studios/TripleA-Utils` — `GenericSingleton<T>` backs `GameManager`/`SpawnManager` |
| `com.triple-a-studios.statemachine` | Finite state machine | Pulled via git URL from `Triple-A-Studios/TripleA-StateMachine`. Plain C#, predicate-driven (`TripleA.StateMachine.FSM`). Backs `GameManager`'s `MainMenuState`/`PlayingState` gate (Feel polish, Pass 5 — see [game-flow.md](game-flow.md)) |
| `com.unity.inputsystem` | New Input System | Action map: `Assets/_Project/Settings/InputSystem_Actions.inputactions` |
| `com.unity.render-pipelines.universal` | URP rendering | Separate PC/Mobile render pipeline assets in `Assets/_Project/Settings` |
| `com.unity.cinemachine` (3.1.5) | Camera work | Third-person follow cam, crash-impact shake/juice |
| `com.unity.services.authentication` (3.8.0) | Anonymous sign-in for the leaderboard | Live leaderboard milestone, Pass 2 spike. Anonymous ID lives in local storage — clearing it creates a new player. Pulls in `com.unity.services.core` (1.18.0). Needs a linked Unity Cloud project |
| `com.unity.services.leaderboards` (2.3.4) | Unity Leaderboards (UGS) client | Backend chosen in Pass 1 (see [design-doc.md](../design-doc.md)). Verified on Android and WebGL in the Pass 2 spike |
| `com.kyrylokuzyk.primetween` | Tween/delay library | Local tarball package (`Packages/com.kyrylokuzyk.primetween.tgz`, referenced from `manifest.json`) plus the installer stub in `Assets/Plugins/PrimeTween`, imported via the TripleA menu `Tools/TripleA/Setup/Import Prime Tween`. Used by `LeaderboardService` only for `Tween.Delay` as a WebGL-safe request timeout (no `System.Threading`) |
| `com.unity.modules.uielements` | UI Toolkit (UITK) runtime | Project's chosen UI framework — build screens with UITK, not uGUI |
| `com.unity.ugui` | uGUI runtime | Present as an engine-level dependency only; not used to build UI screens (UITK is) |
| `com.unity.ai.navigation` | NavMesh/AI navigation | Not yet used in any script |
| `com.unity.timeline` | Timeline/cutscene sequencing | Not yet used in any script |
| `com.unity.visualscripting` | Visual scripting | Not yet used in any script |
| `com.unity.test-framework` | NUnit/Unity Test Framework | Present in manifest; testing preference is play-mode only for this project (see CLAUDE.md) |
