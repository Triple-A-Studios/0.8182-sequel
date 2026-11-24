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
Replace the primitive 3D with final art and fill every 3D slot the UI left empty. Art comes from imported asset packs (swapped in per system, with material, scale and mobile/WebGL budget fixups). Branch `alpha-ms14/3d-art`. Passes are scoped incrementally, one system at a time; no backlog items folded in (2026-11-05).

- **Pass 1** — Plane (pack model on a selectable-plane foundation for the Beta shop; collider, pivot and flight numbers unchanged so flight and crash feel are the same). Device performance check (Android and WebGL) is deferred until after the obstacles and pickups pass, so one device round covers the new art together.
- **Pass 2** — Buildings (Normal, Tough, Heal; scoped 2026-11-06, landed 2026-11-13)
  - One pack model (Mellow Fox Building 4, decimated to about 25k triangles) shared by the three types, no spawner change; the type reads by a runtime tint of one material slot (`BuildingTint`: Normal light grey, Tough yellow, Heal green; red now belongs to `Obstacle_Large`). Spare decimated models stay for a later variety pass.
  - The 6 x 6 x 8 footprint was a placeholder: the buildings are about 25 high, the trigger box is 6 x 20 x 8 (the mesh is bigger on purpose: ledges, windows and antennas must not collide), the weak point is at height 10 on the -Z face with hit radius 2, as a flat red disc. Varied weak-point heights per building come in a later pass.
  - Android / WebGL performance check deferred with Pass 1 (shadows on buildings are still on, about 43k vertices and 5 draw calls each).
- **Pass 3** — Obstacles and pickups (scoped 2026-11-13, landed 2026-11-17; one pass, three sub-parts, verified once in the Editor, then the Android / WebGL performance check for Passes 1 to 3 together)
  - **3a Obstacle** — `Obstacle_Large` reuses the decimated building models (spare Building 2 / 3 / 13 / 15, or 4) with `BuildingTint` red, same visual offset trick as the building prefabs. Solid collider grows from 6 x 8 x 6 to 6 x 20 x 8 like the buildings, no weak point; `ObstacleBuilding` damage logic (`OnCollisionEnter`; the prefab sets `m_hitDamage` 10000, so it is an instant kill) unchanged. No pack to import. Done in the prefab (Building 4 model, same offset and scale as the building prefabs, `BuildingTint` red on the body slot) and playtested in the Editor (2026-11-17): spawner placement with the taller collider and the red reading against the buildings are fine. `Mat_Obstacle_Large` is no longer used by the prefab. The spare models (2 / 3 / 13 / 15) can replace Building 4 here once scaled.
  - **3b Bird** — `Bird_Small` gets a low-poly bird pack model (developer imports, ideally with a flap animation; static is acceptable). `BirdHazard` flips the bird to face its patrol direction on each turn; the 10 damage stays. Wired (2026-11-17): the pack's `01BirdPrefab` (Models/Birds, 171 vertices, one skinned mesh, looping `Fly` clip, faces +Z) is nested under `Visual` at scale 10 (about 1.6 wide), Animator culling `CullUpdateTransforms`; `BirdHazard.m_visual` turns `Visual` to +/-90 degrees on each patrol turn; the trigger moved from 1 x 1 x 1.5 to 1.5 x 1 x 1.5 to match the bird's silhouette once it faces along X. Playtested in the Editor.
  - **3c Health pickup** — `HealthPickup` gets a pack model (heart / medkit / wrench style, developer imports) with a small spin and bob script on its `Visual`; trigger box and +25 restore unchanged. It must read clearly against sky and the green Heal building. Wired (2026-11-17): the developer's `HealthPickupVisual` (Models/Misc, a green cross, 136 vertices) nested under `Visual` at scale 0.75 (1.5 wide, matching the trigger); new `PickupMotion` (spin 120 degrees per second, bob 0.3 at 0.8 Hz, random phase) on the root drives `Visual`. Playtested in the Editor.
  - Rules carried from Passes 1 and 2: no pack colliders or Rigidbodies on the visuals, gameplay colliders stay on the roots, few draw calls and low vertices (Mobile / WebGL), materials converted to URP Lit.
  - Out of scope: new obstacle types, spawner or difficulty changes, hit / collect VFX and sounds (the VFX pass), varied weak-point heights.
- **Pass 4** — Environment and lighting (scoped 2026-11-17, 4a to 4c landed 2026-11-23, playtested in the Editor; 4e dressing scoped 2026-11-23 and landed 2026-11-24, playtested in the Editor)
  - Starting point: default Unity skybox, one white directional light (intensity 2, soft shadows), fog off, and one `Ground` plane (about 100 x 100, at the origin) that nothing moves, so as far as the scene and scripts show the run leaves it behind after roughly 55 units. The developer imports a sky and ground pack first.
  - **4a Ground that never ends** — pack city-block / road tiles recycled along Z ahead of the plane by a small new component (reads the plane position, moves tiles from behind to the front, same idea as the spawner's culling). Visual only, no colliders: the `Ground` mesh collider and the altitude `HitTheDeck` logic stay the gameplay floor. Tiles must cover the playable width and the view to the horizon.
  - **4b Sky and sun** — pack skybox (or gradient) for a bright cartoon day; light colour, angle and shadow strength, ambient and shadow distance tuned; both URP asset pairs checked (the mobile one keeps one shadow cascade).
  - **4c Fog and pop-in** — light distance fog matched to the horizon so spawned buildings, obstacles, birds and pickups (spawned 100 ahead) do not pop in.
  - **4d Menu** — `MainMenu` and the Bootstrap loading flow checked against the new skybox and lighting (lighting is per scene); dev galleries untouched.
  - Built (2026-11-23): the developer imported the QMS Cartoon Skybox (`Sky0008_Day`) and the SimplePoly City pack, with `Prefabs/Misc/Ground.prefab` (a 300 x 60 strip of 45 road and lot pieces, 4,190 triangles, one material) and `Prefabs/Misc/City.prefab` (a hand-built 300 x 260 city: 702 renderers, 140k triangles, 44 materials, buildings up to 25 high). **4a** `Tools > World > Build Ground Tile` merges `Ground.prefab` into one mesh (`Prefabs/World/GroundTile.prefab`, one draw call, no shadow casting); `GroundTiler` (`Scripts/World`) keeps 8 tiles (2 behind, 5 ahead) at a 56.6 pitch so the kerbs of neighbours overlap, and a fog-coloured `GroundSkirt` quad that follows the plane hides the skybox below the horizon. The old 100 x 100 `Ground` (mesh collider included) is gone from `Prototype.unity`. **4b** the skybox material is set on `Prototype.unity`, ambient from the skybox, sun warm white at intensity 1.0 with shadow strength 0.8 (was white at 2). **4c** linear fog, start 80, end 250, colour (0.45, 0.80, 0.98) = the sky's horizon. **4d** the game scene is the active scene in the real flow (`BootstrapLoader`), so its skybox and fog apply; `MainMenu` run on its own keeps its own settings, not changed. Checked 2026-11-23 in Play from `Bootstrap`: every menu-side screen (loading cover, last-week results, `MainMenu`) draws the opaque `.screen-bg` over the camera, so the new world is not visible behind the menu and nothing changed; the `plane-slot` is still a captioned placeholder. The world will show through the menu only once the menu 3D slot (the plane on a rooftop, a later pass) exists, and that pass should check its backdrop against the new sky and fog. No code change. Details: [world-environment.md](technical-design/world-environment.md).
  - Decisions: the City prefab is not used as is (too heavy to recycle, and tall decorative buildings in the lane would look crashable); the hard floor `AltitudeSystem.m_groundHardY` moves from -1 to 0.5 so a run ends when the plane touches the visible ground (the only gameplay change in the pass).
  - **4e Side dressing** (scoped 2026-11-23; after 4d) — the ground is plain roads and grey lots, so the sides get trees, bushes, low props and low buildings taken from the `City.prefab` pieces (`Prefabs/Misc`), baked by an Editor tool (a new `Tools > World` menu item, next to Build Ground Tile) into merged meshes, never as 700 loose renderers.
    - Placement: only outside the flight lane. The plane's hard lateral bound is x 16 (`LateralSystem`), buildings spawn within x +/-10 and are about 7 wide, so nothing decorative closer than |x| 24 (the tool refuses to place a piece inside it). Pieces stand on the ground tile's grey lots (the 10 `Road Concrete Tile` pieces, whose positions the tool reads), seeded so a rebuild gives the same result.
    - Piece palette: houses, shops and other low buildings up to about 12 high (no `Sky_*` high-rises, stadium or factory: they read as crashable and are taller than the lane's buildings), `Natures_*` (trees, bushes, rocks) and small `Props_*` (street lights, bus stops, signs, cones). A small material subset (about 8 of the 44) so a tile stays near 8 to 10 draw calls.
    - Variants: 2 or 3 dressed tile variants of the same footprint as `GroundTile.prefab`; `GroundTiler` picks a variant per tile index (a hash, so the order looks random and a recycled tile is re-dressed), switching a child on and off, no instantiate during the run. The base ground mesh and the 56.6 pitch stay as they are.
    - Budget (to be confirmed on the Android / WebGL check): at most about 10 draw calls and 6k triangles per dressed tile, no shadow casting on dressing, shadow receive only. The tool logs the triangles, vertices and materials of each variant.
    - Out of scope: anything inside the lane, animated props, traffic, people, street lighting at night, a second skyline layer.
    - Built (2026-11-24): `Tools > World > Build Ground Dressing` (`GroundDressingBuilder`; `Build Ground Tile` now ends by running it, because it rewrites the tile prefab). Every pack piece has its own 512 px texture, so the 16 palette pieces (10 low buildings 6.5 to 11.6 high: Bakery, Fast Food, Coffee Shop, Gift Shop, Pizza, Chicken Shop, Books Shop, Music Store, Super Market, House 01; trees, bush, rock, street light) are packed into one 512 px atlas (128 px each, compressed) and every variant is one mesh with one material. The 8 grey lots of the tile with |x| >= 34 (nothing within 24 of the centre line) get one building each (turned a quarter at random) and 1 to 3 trees, bushes, rocks or lights; seeded. 3 variants, 3.7k, 3.7k and 4.6k triangles (7.5k to 9.2k vertices). They are inactive children of `GroundTile.prefab` next to a `GroundTileView`; `GroundTiler` activates one per tile from a hash of the tile index each time a tile is placed. Cost: one extra draw call per tile, 8 in all, no shadow casting. Details: [world-environment.md](technical-design/world-environment.md).
  - Budgets: SRP Batcher, few shared materials, low-triangle tiles that receive shadows but do not cast them. Out of scope: crash / smoke / wear VFX and pickup sparkle (a later VFX pass), menu 3D slots and takeoff, weather or a day/night cycle, any gameplay or spawn change. The Android / WebGL performance check covers Passes 1 to 4 together.

Later passes are scoped as they are reached. Candidates, in rough order: crash, smoke and plane-wear VFX; menu 3D slots (plane on a rooftop) and the takeoff transition; Profile portrait / thumbnails, loading key art and the share card; hardening, device checks and close-out.

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
  - **7a** — Spike in a separate `dev` environment: package, minimal scripts, Cloud Save lock, board writes from a script, revoking client writes
  - **7b** — Server scripts: `StartRun`, `SubmitScore`, `GetDailyState`, `DevSetState`
  - **7c** — Client server layer: `DailyServer`, server clock, server-held attempts in `DailyPlay`
  - **7d** — Menu and run starts: offline state, async PLAY, restart / RETRY through `StartRun`
  - **7e** — Ticketed score submit and the Game Over messages
  - **7f** — Close-out docs
- **Pass 8** — Share to socials (opt-in)
  - **8a** — Share card rendering (score card and rank card to a PNG)
  - **8b** — Android share sheet
  - **8c** — WebGL download
  - **8d** — Wiring: `ShareService` and the three SHARE buttons
  - **8e** — Close-out docs
- **Pass 9** — Hardening, device checks and close-out
  - **9a** — Editor hardening sweep (and the cold-start retry, #37)
  - **9b** — Server go-live preparation (environment by build type)
  - **9c** — Device checks and the score bound (#36)
  - **9d** — Docs refresh and milestone close

### Milestone: Player profile & run stats
Display name and rename, persisted lifetime stats, per-run stats feeding Game Over, NEW BEST detection and badges. Branch `alpha-ms13/player-profile-and-run-stats`.

- **Pass 1** — Local stats store (lifetime stats persisted)
- **Pass 2** — Per-run stats and NEW BEST on Game Over (replaces `GameOverPlaceholderData`)
- **Pass 3** — Profile screen real stats (replaces `ProfilePlaceholderData`)
- **Pass 4** — Display name persistence and moderation
- **Pass 5** — Badges (11 tiered badges, a hidden one, tap popup, Game Over toast, hidden-badge pip)
- **Pass 6** — Badge UI changes: an Achievements screen, picked badges on the Profile ([plan in badges.md](technical-design/badges.md#pass-6-plan-badge-ui-changes))
  - **6a** — Plan the new badge UI
  - **6b** — Badge picks store (picked set, default, persistence)
  - **6c** — Profile badge card (picked tiles, bigger tiles, two-line ellipsis, chevron header)
  - **6d** — Achievements screen (route, list, rows, pips, popup)
  - **6e** — Pick mode
  - **6f** — Hardening, share card, galleries, docs
- **Pass 7** — Hardening, device checks and close-out
  - **7a** — Editor hardening sweep (edge cases of stats, name, badges, Achievements)
  - **7b** — Device checks (Android and WebGL)
  - **7c** — Docs and milestone close

### Milestone: Backlog and Alpha close-out
*(not yet scoped)* Works through the open backlog items worth doing before Beta (checked with the developer, item by item) and closes the Alpha phase: docs refresh, device checks on Android and WebGL, and a go/no-go for starting Beta. Scoped when the developer starts it.

## Phase: Beta

### Milestone: Shop, currency, economy
Unlockable planes, crash effects and skins funded by an in-game currency; the attempt and practice caps stay un-monetized. When coins exist, pass the run's coins into `RunStats.Coins` so the COIN STASH badge can be earned (and with it ALL STAR).

### Milestone: Optional sign-in
*(not yet scoped)* Optional account linking for the anonymous UGS player (provider choice per platform, a conflict flow when a second device already has progress, practice time moved to Cloud Save) so progress and the attempt counter follow the player across devices. Needs a design-doc update to "no accounts".

### Milestone: Android paid build
*(not yet scoped)* Android APK sold as a paid app, WebGL stays free; the paid build unlocks extra cosmetics only, no gameplay, attempt or leaderboard difference. See design-doc section 11.

### Milestone: How to Play tutorial
*(not yet scoped)* A separate tutorial replacing the menu's placeholder entry.

### Milestone: Easter eggs
*(not yet scoped)* Secrets hidden in the game that the player finds by playing, each unlocking a hidden badge (the hidden-badge system lands in Player profile & run stats Pass 5, so an egg only needs to call it).

## Phase: Full Launch

### Milestone: Polish pass
Polished, complete version of everything above.

- Hidden badge unlock animation (until then a pip on the badge tile and the menu's PROFILE chip is the cue; see [badges.md](technical-design/badges.md))
