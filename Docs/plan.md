# Plan

The roadmap only: phases and milestones as headings, passes and sub-passes as nested bullets, one line each. Status, commits and every detail live in the milestone tables in [progress.md](progress.md) (live status in [current-state.md](current-state.md)).

## Phase: Prototype

### Milestone: Core loop, bare minimum
Validate that flying and crashing feels good before anything else is built.

- **Pass 1** — Plane movement and joystick steering
- **Pass 2** — One crashable building with weak-point detection
- **Pass 3** — Fuel system (drain, crash-precision refuel, run ends at zero)
- **Pass 4** — Minimal UI (fuel gauge)

### Milestone: Building types + scoring
Completes the core resource loop (fuel + health + score); Prototype is done here.

- **Pass 1** — Tough building type
- **Pass 2** — Boost mechanic
- **Pass 3** — Health resource
- **Pass 4** — Score

## Phase: MVP

### Milestone: Obstacles, combos, recovery
Two-tier obstacles, combo chain scoring and health pickups.

- **Pass 1** — Large obstacle buildings
- **Pass 2** — Small hazards (birds)
- **Pass 3** — Combo chain scoring
- **Pass 4** — Health pickups and the restore interface

### Milestone: Core systems refactor
Event-driven foundation for spawning: no hand-wired source arrays, an overseeing `GameManager` and `PlayerManager`.

- **Pass 1** — `CombatEvents` bus and `ICrashSource`
- **Pass 2** — Uniform penalty wiring and the mistimed tough-hit fix
- **Pass 3** — Per-resource damage tuning
- **Pass 4** — `GameManager`
- **Pass 5** — `PlayerManager`

### Milestone: Movement & fail-state rework
Cosmetic pitch, simpler crash quality, altitude bounds and a first Game Over screen.

- **Pass 1** — Pitch cosmetic, velocity-based vertical movement
- **Pass 2** — Crash-quality formula rework
- **Pass 3** — Minimal Game Over screen
- **Pass 4** — Altitude bounds
- **Pass 5** — Crash-stutter physics fix
- **Pass 6** — Debug-inspector refresh fix

### Milestone: Difficulty ramp
Density and toughness grow over a run.

- **Pass 1** — Procedural spawner
- **Pass 2** — Difficulty ramp curves
- **Pass 3** — Missing-building combo penalty

### Milestone: Feel polish
Closes MVP: lateral bounds, health HUD, touch controls, main menu, game flow, spawn rework and camera juice.

- **Pass 1** — Lateral run bounds
- **Pass 2** — Health HUD element
- **Pass 3** — Android touch HUD
- **Pass 4** — Main menu scene UI
- **Pass 5** — Game flow (Bootstrap, MainMenu, Prototype)
- **Pass 6** — Spawn algorithm rework (staggered plateaus, chunk spawning)
- **Pass 7** — Camera effects and juice

## Phase: Alpha

### Milestone: MVP bug fix + backlog + engine upgrade
Android bugs found testing the MVP build, the Unity 6.6 upgrade and two QoL items.

- **Pass 1** — Unity 6.6 upgrade
- **Pass 2** — Android bug fixes
  - **2a** — Game Over screen not appearing via Bootstrap
  - **2b** — Menu button press effect
  - **2c** — `UIDocument` to `PanelRenderer` migration
  - **2d** — Unrecoverable plane spin
- **Pass 3** — Touch UI QoL (2400x1080 reference, floating joystick)
- **Pass 4** — Low-altitude warning
- **Pass 5** — Heal building type

### Milestone: Live leaderboard
Unity Leaderboards (UGS) with anonymous sign-in: submit a run's score and show the top scores.

- **Pass 1** — Backend research
- **Pass 2** — UGS spike
- **Pass 3** — `LeaderboardService`, score submit, Game Over rank
- **Pass 4** — Menu Top-10 panel and offline/error states

### Milestone: UI/art pass
The developer's UI mockup (`Docs/Mockups/`) as UI Toolkit UI, placeholder data where the system is a later milestone.

- **Pass 1** — Mockup review (gap analysis)
- **Pass 2** — UI foundation (fonts, theme, components, screen router)
- **Pass 3** — Main menu and its popups
- **Pass 4** — Merged HUD panel and pause popup visuals
  - **4a** — Score, fuel, health, combo, HUD gallery
  - **4b** — Touch controls into the HUD
  - **4c** — Bounds warnings
  - **4d** — Low-fuel / low-health states, damage overlay
  - **4e** — Impact popups
  - **4f** — Pause popup and resume countdown
  - **4g** — Close-out docs
- **Pass 5** — Game Over screen
  - **5a** — Shell, `RunEndCause`, HUD run-end state
  - **5b** — Rank chip states
  - **5c** — Entry cascade, attempt-driven buttons
- **Pass 6** — Settings, Leaderboard and Profile screens
  - **6a** — Settings
  - **6b** — Leaderboard
  - **6c** — Profile
- **Pass 7** — Shop, Credits, Loading, Last week's results
  - **7a** — Shop and Credits
  - **7b** — Loading screen and rotate gate
  - **7c** — Last week's results shell
- **Pass 8** — UI hardening
  - **8a** — Toast, safe area, touch targets, aspect sweep
  - **8b** — Device pass (Android and WebGL)
  - **8c** — Docs close-out

### Milestone: Pause, settings & audio
Functional pause, persisted settings applied in play, sound effects (no music, by design) and haptics.

- **Pass 1** — Settings store
- **Pass 2** — Functional pause
  - **2a** — Core pause
  - **2b** — QUIT TO MENU stub and practice branching
- **Pass 3** — Auto-pause on focus loss and portrait prompt
- **Pass 4** — Audio system
  - **4a** — Foundation
  - **4b** — Gameplay SFX
- **Pass 5** — Haptics and screen shake
- **Pass 6** — Joystick settings and invert steering
- **Pass 7** — Hardening, device checks and close-out
  - **7a** — Editor hardening sweep
  - **7b** — Device checks
  - **7c** — Docs and milestone close

### Milestone: 3D art pass
*(not yet scoped)* Replace the primitive 3D with final art and fill every 3D slot the UI left empty; scoped when the developer starts it.

### Milestone: Daily play structure
A real return to the menu, weekly + all-time boards, 3 attempts a day plus capped practice, server-validated scores and opt-in share. Branch `alpha-ms12/daily-play-structure`.

- **Pass 1** — Real return to the main menu
  - **1a** — Core return-to-menu flow
  - **1b** — Loading-screen cover and hardening
- **Pass 2** — Weekly + all-time boards: dashboard config and SDK spike
- **Pass 3** — `LeaderboardService` rework
  - **3a** — Service data layer
  - **3b** — Submit path
  - **3c** — Wiring and close-out
- **Pass 4** — Archived weeks and Last week's results
  - **4a** — Service archive reads
  - **4b** — Leaderboard screen archived weeks
  - **4c** — Last week's results flow
  - **4d** — Close-out docs
- **Pass 5** — Daily attempts and practice core
  - **5a** — `DailyPlay` service
  - **5b** — Menu
  - **5c** — In run (HUD line, restart confirm, RETRY)
  - **5d** — Close-out docs
- **Pass 6** — Game Over made real
  - **6a** — Rows, notes and rank status from real attempts
  - **6b** — PRACTICE start and the LEADERBOARD deep link
  - **6c** — Close-out docs
- **Pass 7** — Cloud Code validation and server-time enforcement
- **Pass 8** — Share to socials (opt-in)
- **Pass 9** — Hardening, device checks and close-out

### Milestone: Player profile & run stats
*(not yet scoped)* Display name and rename, persisted lifetime stats, per-run stats feeding Game Over, NEW BEST detection and badges.

## Phase: Beta

### Milestone: Shop, currency, economy
Unlockable planes, crash effects and skins funded by an in-game currency; the attempt and practice caps stay un-monetized.

### Milestone: How to Play tutorial
*(not yet scoped)* A separate tutorial replacing the menu's placeholder entry.

## Phase: Full Launch

### Milestone: Polish pass
Polished, complete version of everything above.
