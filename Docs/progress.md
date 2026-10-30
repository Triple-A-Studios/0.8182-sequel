# Progress

## Backlog
Open items only — once an item is addressed, its row moves to [Resolved Backlog](#resolved-backlog) below. `#` is a stable reference number; it doesn't change when an item resolves or the table is re-sorted.

| # | Item | Origin | Type |
|---|---|---|---|
| 19 | Post-plateau spawn oscillator (Left 4 Dead "AI Director" style: modulate spawn-rate/toughness with a slow sine after the ramp plateaus, instead of holding flat) — explicitly deferred out of Feel polish Pass 6 pending playtest results on the staggered-plateau + chunk-spawning rework. See `design-doc.md`'s "Difficulty ramp — engagement levers" parking lot. | Developer note (2026-09-26) | Design |
| 20 | Chunk-spawning rework — Pass 6's `SpawnChunk`/`SpawnChunkSlot` starter set (`Solo`/`ToughPair`/`GauntletMixed`, fixed Z-offset slots, uniform pick among distance-eligible chunks) is a first cut; flagged post-playtest for better chunk variety/authoring. See [spawning.md](technical-design/spawning.md#chunkpattern-based-spawning-pass-6). | Developer note (2026-09-26) | Design |
| 21 | Multi-lane spawn system (Subway Surfers-style) — lateral spawn position should snap to a fixed lane set with min/max variance instead of `SpawnManager`'s current continuous `Random.Range(-m_lateralRange, m_lateralRange)`; needs a matching height-lane axis too, for `BirdSmall`/`HealthPickup` (currently a per-entry vertical min/max range, not discrete lanes). | Developer note (2026-09-26) | Feature |
| 23 | Cinematic side-view + slow-motion on a perfect Tough-building crash — upgrade to `CameraJuice`'s existing Tough-only hit-stop (`PlayerManager.HandleCrashed`'s `quality >= 1f && BuildingType.Tough` gate): switch to a side-view camera angle and slow time (not just a near-instant freeze) for the crash moment. | Developer note (2026-09-26) | Feature |
| 24 | `PlaneController` movement itself feels like it's missing juice (separate from the camera-side juice `CameraJuice` added in Pass 7) — exact gap not yet identified, deferred until playtest feedback narrows down what's missing. | Developer note (2026-09-26) | Design |
| 32 | Invert steering toggle in the pause popup. The setting exists (Settings screen, applied in `PlaneController`) but the pause popup has no switch for it, so changing it mid-run means leaving the run. Add it next to the pause popup's other settings, bound to `SettingsService`. | Developer note (2026-10-25) | Feature |
| 33 | Idle floating joystick visual: the faded joystick base should be visible even when the player is not touching the screen, while staying a floating joystick (the origin still follows the first touch). Needs a resting position/style for the untouched state (and a rule for where it rests with the left/right-hand setting), then it snaps/moves to the touch point on touch-down. See movement.md and `HudTouchControls`. | Developer note (2026-10-25) | Feature |
| 34 | Attempts / practice chip tap info: tapping the menu's attempts chip (competitive tab) or practice clock chip (practice tab) should tell the player how many attempts (or practice minutes) are left today and when they reset, in whole hours (never minutes), neutral wording per the UX rules. Presentation is a decision to make when it is scoped: a popup or a toast. | Developer note (2026-10-30) | Feature (QoL) |

## Resolved Backlog
Bookkeeping only — items move here once addressed, keeping the table above to open items.

| # | Item | Origin | Type | Status |
|---|---|---|---|---|
| 28 | ~~Weekly + all-time leaderboard rework (replaces the daily board shipped by the Live leaderboard milestone): `weekly_scores` (Monday 00:00 UTC, keep-best, archived on every reset) and `alltime_scores`, submission gated on the local best per board, weekly rank on Game Over, cached reads, the last 4 archived weeks on the leaderboard screen and the Last week's results screen.~~ Done across Daily play structure Passes 2-4: boards created and spiked (Pass 2), `LeaderboardService` rework (Pass 3: both boards, stored bests keyed to the live version, `Skipped` result with the last known weekly rank, 90 s live cache, `daily_scores` retired), archived weeks and the results flow (Pass 4: `GetArchivedWeekAsync`, versions-driven stepper, `LeaderboardLastWeekResultsSource`). Spec and results: `leaderboard.md`, `design-doc.md` > Leaderboards & daily attempts. Still open from its list, owned elsewhere: day-boundary / time source for the 3 attempts/day (Pass 7), pricing / call allowances (unconfirmed), archive pruning (display limit only). | Developer decision (2026-10-07) | Feature | Resolved (Passes 2-4, Milestone: Daily play structure) |
| 31 | ~~Real return to the main menu: the pause popup's QUIT TO MENU and Game Over's MAIN MENU were toast stubs.~~ Done: `BootstrapLoader.ReturnToMenu` reloads `Prototype` into the menu state and re-loads `MainMenu` additively behind a 1 s loading-screen cover; RESTART RUN / RETRY use the same cover (Daily play structure, Pass 1, commits `ae4005e`, `73381a9`). Left for Pass 9: refresh `game-flow.md` / `ui.md`. | Developer note (2026-10-24) | Feature | Resolved (Pass 1, Milestone: Daily play structure) |
| 26 | ~~Game Over rank trend glyphs (▲/▼/–, `GameOverUI.TrendGlyph`) don't render on the WebGL build — the default UITK font lacks them.~~ Done: the Game Over rank chip draws the up arrow as an image (`Tex_UiArrowUp.png`, tinted in USS) and no longer uses any text trend glyph; a worse or equal rank shows the number alone (UI/art pass, Pass 5b). Left to confirm: a render check on the WebGL build in the Pass 8 sweep. | Developer note (2026-10-06), Live leaderboard Pass 3/4 device test | Bug | Resolved (Pass 5b, Milestone: UI/art pass) |
| 25 | ~~Joystick sensitivity and size settings — player-facing options for `TouchControlsUI`'s `m_joystickMaxRadius`/`m_joystickBackgroundSize` (currently fixed serialized values). Retargeted (2026-10-07) to the unscoped Alpha "Pause, settings & audio" milestone, alongside the already-deferred screen-half setting from Pass 3's floating joystick rework and the mockup's invert-steering option. The UI/art pass only builds the Settings screen UI with placeholder values.~~ Done: joystick side / size / sensitivity and invert steering are applied to the real controls (Pause, settings & audio Pass 6, commit `d1d3bff`; see movement.md). | Developer note (2026-09-28) | Feature | Resolved (Pass 6, Milestone: Pause, settings & audio) |
| 29 | ~~WebGL rotate-device gate could not be exercised on the itch.io draft: the itch player page rotates / sizes the embed so the game is never portrait there. The gate itself was verified in a local WebGL build with a portrait mobile viewport (375x812). Retest on a plain WebGL page or the custom site the game is later deployed on, holding the phone in portrait.~~ Done: verified 2026-10-17 by forcing the itch.io player to portrait on an Android phone; the rotate prompt showed. | Developer report (UI/art pass, Pass 8b device pass, 2026-10-16) | Verification | Resolved (2026-10-17, Pause, settings & audio branch) |
| 30 | ~~Loading screen can flash by on Android: `LaunchFlowHost` starts its 1.5 s minimum (`m_startTime`) at `Awake`, not when the loading view first appears, so heavy first frames on a phone can eat most of the minimum before anything is drawn. Likely fix: start the timer in `OnUIReload` when `LoadingScreen` is first shown. Check first with the existing `[Launch] loading screen left after X s` log in adb logcat (about 1.5 s or less supports the theory).~~ Done: the minimum now counts frames the loading view is up, each capped at 0.1 s (`LaunchFlowHost.m_visibleSeconds`), so a first-frame hitch cannot eat it; the log line shows wall time and counted time. Confirm on the phone in Pass 7b. | Developer report (2026-10-17, Android) | Bug | Resolved (Pass 7a, Milestone: Pause, settings & audio) |
| 15 | ~~New building type: heals the plane on crash, same precision-matters feel as `Building`'s damage buildings.~~ Done: `HealBuilding` (`IRestorer`, fires `RaiseRestored(quality * 35)`, weak-point miss heals zero) + `Building_Heal.prefab` + `SpawnKind.BuildingHeal` (Pass 5, Milestone: MVP bug fix + backlog + engine upgrade). | Developer note (2026-09-18) | Feature | Resolved (Pass 5, Milestone: MVP bug fix + backlog + engine upgrade) |
| 22 | ~~Low-altitude warning (ground proximity) — mirror `LateralSystem`'s soft/hard bound pattern instead of `AltitudeSystem`'s current ground behavior (instant run-end, no warning).~~ Done: `AltitudeSystem.IsGroundWarningValue` + `m_groundSoftY`/`m_groundHardY` (Pass 4, Milestone: MVP bug fix + backlog + engine upgrade). | Developer note (2026-09-26) | Feature | Resolved (Pass 4, Milestone: MVP bug fix + backlog + engine upgrade) |
| 2 | ~~Extend the `IDamageDealer` pattern to other resource-affecting sources — a restore interface for future health/fuel pickups~~ Done: `IRestorer` (Pass 4, Milestone 3), consumed by `HealthSystem`, implemented by `HealthPickup`. | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Resolved (Pass 4, Milestone: Obstacles, combos, recovery) |
| 3 | ~~A symmetric refuel-source interface for `Building.Crashed` (so `FuelSystem` doesn't hardcode `Building` as the crash-source type) is still open — only the pickup/restore half of the original idea was folded into Pass 4.~~ Done: `ICrashSource` (Pass 1, Milestone: Core systems refactor). | [Pass 3, Milestone: Building types + scoring](current-state.md) | Enhancement | Resolved (Pass 1, Milestone: Core systems refactor) |
| 4 | ~~Replace per-instance `IDamageDealer`/`Crashed` array wiring (manual Inspector drag-drop + cast-at-Awake on `FuelSystem`/`HealthSystem`) with a static/global event any damage dealer or crash source fires, subscribed to once — the array approach doesn't scale to procedurally spawned obstacles, which can't be hand-wired in the Inspector.~~ Done: `CombatEvents` static event bus (Pass 1, Milestone: Core systems refactor). | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 1, Milestone: Core systems refactor) |
| 5 | ~~No overseeing system (`GameManager` or similar) exists yet — nothing owns run/game state above the individual resource systems.~~ Done: `GameManager` (Pass 4, Milestone: Core systems refactor). | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 4, Milestone: Core systems refactor) |
| 6 | ~~`FuelSystem`/`HealthSystem` each independently subscribe to crash/damage sources and independently decide run-end (disabling `PlaneController`, zeroing velocity) — they should be pure resource managers (apply increments/decrements, raise a "depleted" event) with a `PlayerManager` (or equivalent) owning crash/damage/run-end orchestration.~~ Done: `PlayerManager` (Pass 5, Milestone: Core systems refactor); `FuelSystem`/`HealthSystem` are now pure resource managers. | [Pass 1, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 5, Milestone: Core systems refactor) |
| 9 | ~~Mistimed tough-building hit (`!canBreak` branch) behaves inconsistently with `Building_Normal`'s always-destroys-and-scores pattern: building survived, scored nothing, only reached `HealthSystem`.~~ Done: mistimed hits now destroy with base score (quality=0) and skip the combo reward (Pass 2, Milestone: Core systems refactor) — confirmed design: "requires enough boost" is a scoring-quality gate, not a hard-resistance gate. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug/Design | Resolved (Pass 2, Milestone: Core systems refactor) |
| 10 | ~~More generally: every `IDamageDealer` source should reach all three penalty systems (`HealthSystem`, `FuelSystem`, `ScoreSystem`'s combo timer) uniformly, but today it's manual opt-in per source per array.~~ Done: the `CombatEvents` global bus (Pass 1) makes every source reach every listener uniformly by construction, no more per-source opt-in arrays. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Refactor | Resolved (Pass 1, Milestone: Core systems refactor) |
| 11 | ~~`FuelSystem`/`HealthSystem` both drain by the exact same raw `damage` value from any wired `IDamageDealer` source — no independent per-resource tuning.~~ Done: `HealthDamageMultiplier`/`FuelDamageMultiplier` on `IDamageDealer` (Pass 3, Milestone: Core systems refactor), both defaulting to `1f`. | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement | Resolved (Pass 3, Milestone: Core systems refactor) |
| 13 | ~~Crash-quality calculation (speed × angle-into-weak-point) simplified so players don't need to reason about precise crash angles.~~ Done: dropped angle term, quality now speed-only gated on weak-point hit (`Building.cs`); `WeakPoint.OutwardNormal` removed as dead code (Pass 2, Milestone: Movement & fail-state rework). | Developer note (2026-09-14) | Design | Resolved (Pass 2, Milestone: Movement & fail-state rework) |
| 1 | ~~Plane momentarily stops on crash — both plane and building are colliders, physics resolves a full stop before `Destroy`~~ Done: `Building`'s `BoxCollider` is now a trigger, `OnTriggerEnter` replaces `OnCollisionEnter` — trigger overlaps skip collision resolution entirely, so there's no stopping impulse left to cause the stutter (Pass 5, Milestone: Movement & fail-state rework). | [Pass 1, Milestone: Building types + scoring](current-state.md) | Bug | Resolved (Pass 5, Milestone: Movement & fail-state rework) |
| 12 | ~~W/S pitch input becomes purely cosmetic (visual rotation only, mirroring how sideways/yaw movement already works) — actual vertical movement uses `rb.linearVelocity` directly, same approach as horizontal strafing.~~ Done: `m_verticalSpeed` drives real velocity, cosmetic pitch mirrors the existing bank `MoveTowards` pattern on `m_visualRoot` (Pass 1, Milestone: Movement & fail-state rework). | Developer note (2026-09-14) | Design/Refactor | Resolved (Pass 1, Milestone: Movement & fail-state rework) |
| 14 | ~~Add upper/lower altitude limits — crossing either ends the run.~~ Done: `AltitudeSystem` — ceiling warns then ends the run (plane keeps its last velocity, camera stops following); ground ends the run instantly, same freeze as a building crash or health-zero death (Pass 4, Milestone: Movement & fail-state rework). | Developer note (2026-09-14) | Feature | Resolved (Pass 4, Milestone: Movement & fail-state rework) |
| 8 | ~~Alchemy `[ShowInInspector]` debug fields (`CurrentFuel`, `CurrentScore`, `CurrentMultiplier`, `CurrentComboTimer`, etc.) don't visibly update in the Inspector during Play Mode.~~ Done: root cause known but out of scope (Alchemy repaint bug) — resolved instead by dropping `[ShowInInspector]`/`[ReadOnly]` for debug-only variables project-wide; use the Inspector's Debug mode instead (Pass 6, Milestone: Movement & fail-state rework). | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Bug | Resolved (Pass 6, Milestone: Movement & fail-state rework) |
| 7 | ~~Combo timer should also take a reduction for *missing* a building (flying past without crashing), not just for crashing hazards — needs a way to know a building existed and wasn't hit, which requires building spawning/tracking (not built yet, buildings are hand-placed). Implement once that spawner exists.~~ Done: `ScoreSystem` subscribes to `SpawnEvents.EntityCulled` (fires only for unhit buildings), drains `m_missedBuildingTimerPenalty` off the combo timer (Pass 3, Milestone: Difficulty ramp). | [Pass 3, Milestone: Obstacles, combos, recovery](current-state.md) | Enhancement | Resolved (Pass 3, Milestone: Difficulty ramp) |
| 18 | ~~Lateral (sideward) bounds — mirrors `AltitudeSystem`'s ceiling/ground pattern but for the horizontal axis: a soft limit displays a warning, a hard limit past it ends the run.~~ Done: `LateralSystem` (Pass 1, Milestone: Feel polish) — hard bound flies the plane off sideways with the camera left behind, same as `AltitudeSystem.CeilingExceeded`, on both sides. | Developer note (2026-09-23) | Feature | Resolved (Pass 1, Milestone: Feel polish) |
| 16 | ~~Plane health HUD element (MVP phase) — no on-screen health readout exists today, only `FuelGaugeUI`/`ScoreUI`/`AltitudeWarningUI`/`GameOverUI`. Mirror `FuelGaugeUI`'s pattern for `HealthSystem`.~~ Done: `HealthGaugeUI` (Pass 2, Milestone: Feel polish), structural copy of `FuelGaugeUI`. Still open: once an art pass (Alpha or later in Feel polish) adds visual damage feedback on the plane itself (smoke, sparks, etc.), revisit whether this HUD element gets removed in favor of the visual cue or kept alongside it. | Developer note (2026-09-18) | Feature | Resolved (Pass 2, Milestone: Feel polish) |
| 17 | ~~Spawn algorithm (`SpawnManager`) needs revisiting — Pass 2's independent-per-spawn weighted roll + single global ramp plateau is a known-incomplete design, not a finished one (it plateaus into a flat, pattern-matchable steady state).~~ Done: staggered per-entry ramp distances (`RampDistanceOverride`) + chunk/pattern-based spawning (`SpawnChunk`/`SpawnChunkSlot`) (Pass 6, Milestone: Feel polish). Remaining candidate levers (golden building, second speed axis, risk-positioned pickups) stay parking-lot-only; post-plateau oscillator explicitly split out as backlog #19. | Developer note (2026-09-22) | Design | Resolved (Pass 6, Milestone: Feel polish) |
| 27 | ~~Leaderboard period decision — daily vs weekly vs monthly/all-time boards.~~ Decided 2026-10-07: **weekly (archived) + all-time**; no daily, no monthly. Recommendation in the original row (daily primary + monthly + all-time) was overridden by the developer. Work tracked in #28. | Developer, UI mockup review (2026-10-07) | Design | Resolved (design-doc.md, Leaderboards & daily attempts) |

## Phase: Prototype — Completed

### Milestone: Core loop, bare minimum — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `b482138` | Plane movement + joystick steering |
| 2 | Verified — Complete | `01fc0ad` (crash-quality fix: `4f463f8`) | Single crashable building + weak-point detection |
| 3 | Verified — Complete | `ad1cfde` | Fuel system (drain + crash-precision refuel + run-end at zero) |
| 4 | Verified — Complete | `ad1cfde` | Minimal UI (fuel gauge) |

### Milestone: Building types + scoring — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `9145687` | Tough building type (variant requiring hold-boost-to-break, distinct visual, own weak point) |
| 2 | Verified — Complete | `2692147` | Boost mechanic (input, faster fuel drain, harder steering while active) |
| 3 | Verified — Complete | `968025f` | Health resource (second stat; lost on mistimed tough-building crash; death behavior — instant vs. gradual — TBD by playtest feel) |
| 4 | Verified — Complete | `1d93f12` | Score (simple visible score, increments per crash, shown in UI) |

Prototype phase complete — design doc: "Prototype is considered done here."

## Phase: MVP — Completed

### Milestone: Obstacles, combos, recovery — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `560c21a` | Large obstacle buildings (avoid-not-crash, fuel/health penalty, distinct visual) + generalize Fuel/Health source wiring to multi-source |
| 2 | Verified — Complete | `d25d86a` | Small hazards (birds) — lower-penalty reflex/dodge obstacle |
| 3 | Verified — Complete | `4101335` | Combo chain scoring — crash multiple buildings in quick succession for bonus/multiplier |
| 4 | Verified — Complete | `e9fce2d` | Health pickups + restore interface — generalize `IDamageDealer` into a matching restore-interface for fuel/health pickups (folds backlog item) |

### Milestone: Core systems refactor — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `0118f74` | Event-driven damage/restore wiring — `CombatEvents` static event bus + `ICrashSource` interface replace per-instance Inspector array wiring (folds backlog items: array wiring, symmetric refuel-source interface) |
| 2 | Verified — Complete | `2c22213` | Uniform penalty wiring + tough-building mistimed-hit fix — mistimed hits now destroy with base score, skip combo reward (folds backlog items: uniform 3-system wiring, mistimed tough-hit design) |
| 3 | Verified — Complete | `56a9ab7` | Per-resource damage tuning — `HealthDamageMultiplier`/`FuelDamageMultiplier` on `IDamageDealer` (folds backlog item: per-resource damage tuning) |
| 4 | Verified — Complete | `6173841` | `GameManager` — single source of truth for run-active state (folds backlog item: no overseeing system) |
| 5 | Verified — Complete | `ee01e77` | `PlayerManager` — owns crash/damage/run-end orchestration; `FuelSystem`/`HealthSystem` become pure resource managers (folds backlog item: pure resource managers) |

### Milestone: Movement & fail-state rework — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `a5f58c0` | Pitch-cosmetic / velocity-based vertical movement |
| 2 | Verified — Complete | `cd40dcd` | Crash-quality formula rework — speed-only, dropped angle-alignment term |
| 3 | Verified — Complete | `2f2e1b4` | Minimal Game Over screen |
| 4 | Verified — Complete | `d76fd2e` | Altitude bounds — ceiling warning + fly-away, ground instant freeze |
| 5 | Verified — Complete | `875f6d9` | Crash-stutter physics fix — trigger collider replaces solid collision (folds backlog item: crash stutter) |
| 6 | Verified — Complete | `b7a8725` | Alchemy debug-inspector refresh fix — dropped `[ShowInInspector]`/`[ReadOnly]` reliance for debug vars project-wide (folds backlog item: Alchemy inspector refresh) |

### Milestone: Difficulty ramp — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `93874e1` | Procedural spawner — `SpawnManager` replaces hand-placed buildings/obstacles/hazards/pickups, distance-based spawn ahead + cull behind |
| 2 | Verified — Complete | `c5e89b5` | Difficulty ramp curve — per-entry `AnimationCurve` weights + spawn-interval curve, capped at a tunable ramp distance (`HealthPickup` decays to a floor instead) |
| 3 | Verified — Complete | `1542362` | Combo-timer missing-building penalty — `ScoreSystem` subscribes to `SpawnEvents.EntityCulled` (folds backlog item: missed-building combo penalty) |

### Milestone: Feel polish — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `dd01b18` | Lateral (sideward) run bounds — soft warning + hard fly-away, mirrors `AltitudeSystem` (folds backlog item: lateral bounds) |
| 2 | Verified — Complete | `a78fc3b` | Health HUD element — structural copy of `FuelGaugeUI` (folds backlog item: health HUD) |
| 3 | Verified — Complete | `2e1ead9` | Android touch HUD — on-screen joystick + boost button, hand-built UI Toolkit |
| 4 | Verified — Complete | `119f0fe` | Main menu scene UI — UI Toolkit flat-color blockout from mockup, PLAY button + stub-clickable feedback |
| 5 | Verified — Complete | `edf0ef6` | Game flow — `Bootstrap` scene additively loads `MainMenu`+`Prototype`, `GameManager` FSM gates Play/Menu state |
| 6 | Verified — Complete | `bbc0216` | Spawn algorithm rework — staggered per-stat ramp plateaus + chunk/pattern-based spawning (folds backlog item: spawn algorithm) |
| 7 | Verified — Complete | `1ce5f3d` | Camera effects/juice — shake, boost follow-tighten/FOV kick, steering roll, Tough-only perfect-crash hit-stop |

Feel polish milestone complete — design-doc.md: "MVP is considered done here."

## Phase: Alpha

### Milestone: MVP bug fix + backlog + engine upgrade — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `5e8eaa3` | Unity 6.6 upgrade (6000.0.79f1 → 6000.6.3f1; tech stream over 6.3 LTS, developer's call) |
| 2.a | Verified — Complete | `68473fc` | Game Over screen not appearing via Bootstrap — `GameOverUI` re-subscribes to `RunEnded` on every `OnEnable` |
| 2.b | Verified — Complete | — | Main-menu button press effect not visible on Android — resolved incidentally by Pass 2.c, no separate commit |
| 2.c | Verified — Complete | `9c43143` | `UIDocument` → `PanelRenderer` migration across all UI scripts (not originally scoped) |
| 2.d | Verified — Complete | `a521b23` | Unrecoverable plane spin — froze Plane Rigidbody rotation (not Android-specific) |
| 3 | Verified — Complete | `c22fcf1` | Touch UI QoL: reference resolution 1920x1080 → 2400x1080 + floating joystick |
| 4 | Verified — Complete | `fb5291f` | Low-altitude warning (folds backlog #22) |
| 5 | Verified — Complete | `2f1a1ac` | Heal building type (folds backlog #15) |

Milestone complete — plan.md's definition of done met; the Android on-device recheck of Pass 3's resolution/joystick change is still the developer's to confirm opportunistically.

### Milestone: Live leaderboard — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `91d5dca` | Backend research — Unity Leaderboards (UGS) with anonymous sign-in chosen over LootLocker; client-side submit now, Cloud Code validation deferred to Daily play structure |
| 2 | Verified — Complete | `f890bab` | UGS spike (throwaway `LeaderboardSpike`) — anonymous sign-in, submit, fetch confirmed on Android + WebGL |
| 3 | Verified — Complete | `2b03eca` | `LeaderboardService` (daily Best Score board `daily_scores`, reset 00:00 UTC), score submit on run end, Game Over rank + up/down trend. Spike deleted |
| 4 | Verified — Complete | `005e412` | Menu Top-10 panel (`LeaderboardPanelUI`), Game Over submit Retry, offline fast-fail + 10s request timeout via PrimeTween `Tween.Delay` |

Verified in Editor, Android and WebGL. Known gap: ▲/▼ glyphs don't render on WebGL (backlog #26, UI/art pass). Display-name field deferred to the UI/art pass.

### Milestone: UI/art pass — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `e23c93e` | Mockup review: `Docs/Mockups/gap-analysis.md` (docs only) |
| 2 | Verified — Complete | `7602582` | UI foundation: fonts, `Theme.uss`, `LipBox` / `LipButton` / `TabBar` / `ThemedSlider`, `ScreenRouter`, `SafeAreaHelper`, `UIGallery` |
| 3 | Verified — Complete | `1fcf74e` | Main menu (S02) + overflow, attempt-confirm and quit-confirm popups |
| 4a | Verified — Complete | `654410b` | Merged HUD panel: score, 10-pip fuel, health bar, combo sticker, `HudGallery` |
| 4b | Verified — Complete | `33a1703` | Touch controls re-homed into the HUD |
| 4c | Verified — Complete | `da3bf97` | Bounds warnings: ceiling banner, lateral tape, ground vignette |
| 4d | Verified — Complete | `b51b719` | Low-fuel / low-health states, damage overlay, effect arbiter |
| 4e | Verified — Complete | `c142969` | Impact popups at the crash point |
| 4f | Verified — Complete | `54a2973` | Pause popup + resume countdown (visuals only) |
| 4g | Verified — Complete | `55d01be` | Pass 4 close-out (docs only) |
| 5a | Verified — Complete | `3680050` | Game Over shell, `RunEndCause`, HUD run-end state |
| 5b | Verified — Complete | `3deea98` | Rank chip states + arrow image (folds backlog #26) |
| 5c | Verified — Complete | `b2d7880` | Entry cascade, attempt-driven buttons, rank count-down |
| 6a | Verified — Complete | `36f029d` | Settings (S06) + live joystick preview |
| 6b | Verified — Complete | `f77923f` | Leaderboard (S07) replacing the Top-10 popup |
| 6c | Verified — Complete | `fa2aece` | Profile (S08) with in-session rename |
| 7a | Verified — Complete | `a715626` | Shop (S09), Credits (S10), coin chip opens the Shop |
| 7b | Verified — Complete | `edda167` | Loading (S01) with real progress + rotate-device gate |
| 7c | Verified — Complete | `5a0dfe1` | Last week's results (S11) + source hook, Pass 7 close-out |
| 8a | Verified — Complete | `db5de83` | Shared toast, menu safe area, 80 px touch targets, aspect sweep |
| 8b | Verified — Complete | `67dacac` | Device pass (Realme 8s 5G, Android + phone-browser WebGL): safe-area flip, Profile keyboard |
| 8c | Complete | — | Docs close-out (docs only): `ui.md` rewritten, 3D art split into its own milestone |

Milestone complete — every mockup screen exists as UITK UI with static placeholder data where the system is a later milestone, verified in the Editor, on Android and in WebGL (phone browser). Known gap: the rotate-device gate was not exercised on itch.io (backlog #29). The 3D half of the original scope was moved into its own milestone, **3D art pass** (below), on 2026-10-17. UI structure and decisions: [technical-design/ui.md](technical-design/ui.md).

### Milestone: Pause, settings & audio — Completed

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1 | Verified — Complete | `3c7c54a` | Settings store: PlayerPrefs-backed `SettingsService` replaces the session-only `UiSettingsState` |
| 2a | Verified — Complete | `75394ff` | Core pause: `GameManager` `PausedState` (owns `timeScale`), `Pause()` / `Resume()`, hit-stop guard, shared `RestartRun()`, HUD wiring (button, popup, 3-2-1 countdown, RESTART, Escape / back) |
| 2b | Verified — Complete | `b6ef40d` | QUIT TO MENU stub with practice / competitive branching: `IsPracticeRun`, leave-run confirm popup |
| 3 | Verified — Complete (Editor, then Android + WebGL in 7b) | `af09727` | Auto-pause on focus loss, countdown guard, in-session portrait rotate overlay |
| 4a | Verified — Complete | `a3da19e` | Audio foundation: generated placeholder WAVs, `SfxLibrary`, `AudioManager` (pool, priority, ducking), `Sfx` facade, Sound slider, pause integration, UI / countdown / slider sounds. No mixer, no music |
| 4b | Verified — Complete | `8e16de0` | Gameplay SFX: `GameplaySfx` (crashes, hits, heal, pickup, combo, boost loop, warnings, run end, game over) |
| 5 | Verified — Complete (Editor, then Android in 7b) | `90cb47a` | Haptics service + `GameplayHaptics`, screen-shake slider into `CameraJuice`, Android VIBRATE manifest |
| 6 | Verified — Complete (Editor, then phone in 7b) | `d1d3bff` | Joystick side / size / sensitivity + invert steering applied (folds backlog #25) |
| 7a | Verified — Complete | `9ec785d` | Editor hardening sweep, fallback `AudioListener`, loading screen minimum counts visible frames (folds backlog #30) |
| 7b | Verified — Complete | `b03c7bc` | Device checks on Android + WebGL; feedback folded in: haptic profiles (light / medium / heavy / crash rhythm as ms + amplitude arrays), UI press haptics, one UI click per press |
| 7c | Verified — Complete | `8cb6a4c` | Docs refresh and milestone close |

Milestone complete — pause, every setting, sound effects (no music, by design) and haptics are real, verified in the Editor, on an Android phone and in WebGL (phone browser). Backlog #25, #29 and #30 resolved. Decisions: pause is state-based through `GameManager` but only the Paused state touches `Time.timeScale`; no audio mixer (priority, voice stealing and ducking are code); settings are local PlayerPrefs; QUIT TO MENU stays a toast stub until Daily play structure; generated stand-in clips and default haptic patterns are replaceable with no code change. System docs: [game-flow.md](technical-design/game-flow.md), [settings.md](technical-design/settings.md), [audio.md](technical-design/audio.md), [haptics.md](technical-design/haptics.md).

### Milestone: 3D art pass — Upcoming
See [plan.md](plan.md#phase-alpha) for scope. Not yet scoped.

### Milestone: Daily play structure — In Progress

Branch `alpha-ms12/daily-play-structure`; scoped 2026-10-25 (one-shot, sub-passes decided when a pass is reached). Folds backlog #31 (real return to the main menu) and #28 (weekly + all-time leaderboard rework). Live status: [current-state.md](current-state.md). System docs: [daily-play.md](technical-design/daily-play.md), [leaderboard.md](technical-design/leaderboard.md), [game-flow.md](technical-design/game-flow.md), [ui.md](technical-design/ui.md).

| Pass | Status | Commit | Target |
|---|---|---|---|
| 1a | Verified — Complete | `ae4005e` | Real return to the main menu (#31), core flow. No gameplay system has a reset and `GameManager` has no Paused to MainMenu transition, so the run is reset the way `RestartRun` does it: `BootstrapLoader.ReturnToMenu` resets `timeScale` / audio / `Sfx.StopAll()`, sets the consume-once `GameManager.ManagedByBootstrap`, single-mode loads `Prototype`, then additively loads `MainMenu`; flag cleared on failure, re-entry guarded, no state machine change. QUIT TO MENU and MAIN MENU wired; the toast stays as the standalone fallback |
| 1b | Verified — Complete | `73381a9` | Loading-screen cover for the reload (`LaunchFlowHost` re-show / hide, serialized `Min Cover Seconds` default 1 s, bar filling over it, old scene frozen under it); RESTART RUN / RETRY routed through the same cover (`GameManager.RestartRun` hands off to `BootstrapLoader.RestartRun`); Editor sweep (every end cause then MAIN MENU, pause quit practice / competitive, repeated cycles, RETRY then quit, quit during a countdown) |
| 2 | Verified — Complete | `c3c8ad2` | Weekly + all-time boards: dashboard config and SDK spike (#28). `weekly_scores` (Monday 00:00 UTC, keep-best, archive on reset) and `alltime_scores` created; `GetVersionsAsync`, `GetVersionPlayerScoreAsync`, `GetVersionScoresAsync` and the no-entry case (`EntryNotFound`) verified; results in `leaderboard.md` |
| 3a | Verified — Complete | `6ed872b` | `LeaderboardService` data layer: board ids `m_weeklyBoardId` / `m_allTimeBoardId` (Bootstrap scene serialized), versions wrapper (one cached `GetVersionsAsync` per session is the week signal), live fetch with `Total`, in-memory cache (about 90 s, shared in-flight tasks) |
| 3b | Verified — Complete | `85cfc3b` | Submit path: each board gated on beating its stored best (weekly keyed to the live version id), weekly then all-time under their own timeouts, `SubmitResult` is the weekly one plus `Skipped`; a skipped run shows the last known weekly rank (`lb_weekly_rank`), rank alone, no trend; trend is `Up` for a first entry or a better rank, else `Same`, never `Down`; a board's local best updates only after its own submit succeeds; old `lb_last_rank` / `lb_last_utc_date` keys deleted |
| 3c | Verified — Complete | `f9534da` | Wiring and close-out: `GameOverUI` practice gate and skipped result, leaderboard screen WEEKLY / ALL-TIME tabs on the real boards, `leaderboard.md` rewrite, `daily_scores` retired (the dashboard board is left for the developer to delete) |
| 4a | Verified — Complete | `cbe6082` | `GetArchivedWeekAsync(versionId, limit)` (rows, `Total`, own entry, `EntryNotFound` = no entry, cached by version id for the session, never cleared by a submit) and a versions-cache clock-skew guard |
| 4b | Verified — Complete | `41f35ae` | Leaderboard screen archived weeks: stepper driven by the cached versions (limit = archived count, hidden with none or on a failed call), server dates, reset chip in days with hours only on the last day (never minutes), dev gallery stub |
| 4c | Verified — Complete | `26bfcae` | Last week's results (S11): async `ILastWeekResultsSource` with `MarkShown`, real `LeaderboardLastWeekResultsSource` (stored `lb_last_shown_version`, silent fresh install, a player who never ranked gets no screen), lookup starts at boot and the launch waits at most 3 s more (frame-polled; the cap itself is only checkable on a device). SHARE stays a toast |
| 4d | Verified — Complete | `1934c52` | Close-out docs; backlog #28 resolved |
| 5a | Verified — Complete | `4e4385e` | Static `DailyPlay` service (`Scripts/Daily/`, `daily.` prefs): UTC day key that never goes back, 3 attempts and 30 min practice per day, nothing banks, an attempt is spent at run start, soft practice cap, `IDailyClock` seam for Pass 7; `GameManager.IsPracticeRun` follows it and ticks practice time only while playing (frame capped at 0.5 s); `ReturnToMenu` ends the run |
| 5b | Verified — Complete | `151ec2d` | Menu reads the service: attempts chip and pips, practice clock, attempt-confirm body, PLAY spends the attempt through `BeginRun`, PLAY greyed with a neutral "NEW ATTEMPTS IN Nh" (whole hours), per-day "don't ask again", menu returns on the tab of the last run, Tools menu reset item |
| 5c | Verified — Complete | `cd3c44c` | In run: pause popup "COMPETITIVE · ATTEMPT n/3" and quit note, restart confirm popup, `RestartRun` / RETRY spend the next attempt and are refused neutrally when none are left (practice restarts free until the cap), `CanRestartRun` greys RESTART RUN |
| 5d | Verified — Complete | `a4544e1` | Close-out docs: `daily-play.md`, `game-flow.md`, `ui.md` registry |
| 6a | Verified — Complete | `d7ffa8c` | Game Over rows from real attempts (`GameOverRow.Derive`): RETRY + "N attempts left today"; last attempt used = LEADERBOARD primary + "new attempts in Nh" + PRACTICE; practice = RETRY with the clock, greyed with "practice returns in Nh". Rank status "THIS WEEK · N LEFT TODAY" / "THIS WEEK · DONE TODAY" (amber, the weekly rank is never "locked"). **BANK IT and RETRY NOW dropped** (a daily-board idea; the weekly keep-best board already saves the score and the UX rules forbid banked attempts) |
| 6b | Verified — Complete | `b7cbcaf` | PRACTICE starts a practice run at once (`GameManager.StartPracticeRun`, no confirm, no attempt spent); LEADERBOARD and the rank-chip tap reload into the menu on the leaderboard screen (`BootstrapLoader.ReturnToLeaderboard`, consume-once `MainMenuController.OpenLeaderboardOnLoad`) |
| 6c | Complete — docs committed, not yet verified | `6193686` | Close-out docs: `design-doc.md` (BANK IT dropped), `ui.md`, `daily-play.md`, `game-flow.md`, mockup notes, `CLAUDE.md` |
| 7a | Verified — Complete | `8a217df` | Cloud Code spike in the `dev` environment: Cloud Code 2.10.4 + Deployment + Tooling packages, scripts deployed from the Editor, parameters set through the script `.meta`, Cloud Save write lock (409 on a stale lock), service-token board write with an empty response body (read the entry back), client board writes denied by a broad `urn:ugs:leaderboards:*` policy while reads and the script write still work; results in `leaderboard.md`. The `LeaderboardService` environment setting landed with it |
| 7b | Verified — Complete | `3c38ab8` | Server scripts in `Assets/_Project/CloudCode/` (JavaScript, one self-contained file each, deployed from the Editor). Per-player state is ONE Cloud Save private item `daily` = `{ day, used, ticket }` (`day` = server UTC day number, `ticket` = `{ id, startedAtMs, used }` or null), so a single write lock covers the counter and the ticket together. `GetDailyState`: rolls the day on read (not persisted), returns `{ attemptsUsed, attemptsLeft, attemptsMax, nowMs, nextResetMs }`. `StartRun`: roll the day; at 3 used returns `{ ok: false, code: "NO_ATTEMPTS" }`; else spends one and writes a fresh ticket (an unused earlier ticket is replaced, so an abandoned run still counts) in one lock-guarded write, retried once on a 409, `BUSY` if it still conflicts; returns `{ ok, ticketId, attemptNumber, attemptsLeft, nowMs, nextResetMs }`. `SubmitScore(ticketId, score)` (params declared in the `.meta`): rejects `NO_TICKET` / `TICKET_USED` / `TICKET_EXPIRED` (older than a max run age) / `IMPLAUSIBLE` (not a positive whole number, or above `MAX_SCORE_PER_SECOND` times SERVER elapsed seconds since the ticket plus a slack; constants at the top, start generous and tune from the logged `{ score, elapsedSec }` pairs); marks the ticket used with the lock first, then writes both boards with the service token and reads each entry back (the add call returns an empty body); on a board error the ticket is restored so the client's retry works; returns `{ ok, weekly: { rank (1-based), score }, allTime: { score } }`. `DevSetState` (optional params `used`, `dayOffset`, `ticketStartedAgoMs`; refuses when `context.environmentName` is `production`): sets the state for checks and backs the Tools > Daily Play reset item in 7c. Checked by eval in `dev` after a deploy: reset, 3 starts then `NO_ATTEMPTS`, state matches, valid submit lands on both boards, same ticket again `TICKET_USED`, wrong ticket, backdated ticket `TICKET_EXPIRED`, huge score `IMPLAUSIBLE`, day roll through `dayOffset`, two concurrent starts spend exactly the remaining attempts, state survives `PlayerPrefs.DeleteAll`, a direct client write is still denied. Developer steps: reimport the scripts, then deploy them from the Deployment window with `dev` selected |
| 7c | Verified — Complete | `6e7826f` | Client server layer (no UI yet). `Scripts/Server/DailyServer`: waits for `LeaderboardService.EnsureReadyAsync`, calls `GetDailyState` / `StartRun` / `SubmitScore` through `CloudCodeService.Instance.CallEndpointAsync<JToken>` with a short timeout and an offline fast-fail, typed results, nothing keyed to the device (the server reads the player id from the token). `ServerDailyClock : IDailyClock`: last server `nowMs` plus elapsed realtime, resynced on every reply, device clock only until the first reply (it feeds the practice day key and the "returns in Nh" copy). `DailyPlay`: competitive `AttemptsUsed` / `AttemptsLeft` / `NextResetUtc` come from the last server reply (`ApplyServerState`, raises `Changed`), `IsServerStateKnown`, `CurrentTicketId`, async `BeginRunAsync(RunMode)` (competitive calls `StartRun`; practice stays synchronous and local); the server count lives beside the local one (`ServerAttemptsUsed` / `ServerAttemptsLeft` / `IsServerStateKnown`, stale once the UTC day rolls): 7c is additive so the game keeps running, and 7d switches the readers and callers over and drops the local competitive counter (`daily.attempts_used`); practice seconds and "don't ask again" stay local. The Cloud Code call itself is `LeaderboardService.CallServerAsync` (sign-in, offline fast-fail, 10 s timeout). Checked by eval in `dev`: state sync, `BeginRunAsync` spends an attempt and keeps a ticket, refusal at 0, clock follows the server, offline reports unknown without spending anything |
| 7d | Verified — Complete | `1c3845c` | Menu and run starts: the server count replaces the local competitive counter. `DailyPlay`: `AttemptsUsed` / `AttemptsLeft` read the server state (0 left while unknown), `CanBeginRun(Competitive)` is `ServerUnavailable` while unknown, `NoAttemptsLeft` at 0, else `Started`; the sync `BeginRun(Competitive)` goes away (competitive starts only through `BeginRunAsync`), the local `daily.attempts_used` pref and counter are dropped (practice seconds and "don't ask again" stay local); `ResetToday` also forgets the server cache. Menu: `RefreshDaily` shows the known server count at once and a fetch (`DailyPlay.RefreshFromServerAsync`) runs at every menu build (about one `GetDailyState` per visit), when the state is unknown or stale (auto-retry at most every 15 s; pressing PLAY retries at once) and one at a time; while unknown the competitive chip reads "-/3" with empty pips, PLAY is greyed (`menu-play--locked`) and the sub-label says "CAN'T REACH THE SERVER" (or "CHECKING" while a fetch runs); practice is unaffected. Starting: LET'S FLY / practice PLAY await `BeginRunAsync` (taps ignored meanwhile, sub-label "STARTING"); `Started` hides the menu as before, `NoAttemptsLeft` just refreshes, `ServerUnavailable` shows a neutral toast and nothing is spent. Restart / RETRY: `GameManager.RestartRun` becomes `RestartRunAsync` returning `BeginRunResult` (competitive spends the next attempt through `StartRun` before the reload; mode `None` reloads at once); `CanRestartRun` reads the cached state; the pause popup's restart confirm and Game Over RETRY ignore taps while it runs and show `DailyPlayText.StartRefusedLine` on a refusal ("NEW ATTEMPTS IN Nh", "PRACTICE RETURNS IN Nh" or "CAN'T REACH THE SERVER"). Editor: Tools > Daily Play reset also calls `DevSetState` and refreshes when played from Bootstrap in a non-production environment. Wording is ASCII-safe (no ellipsis or dash glyphs outside the baked set). Checked by eval in `dev` (state gates, `RestartRunAsync` results for every mode, ticket per restart, refusal at 0, wording) and by the developer by eye (menu chips and PLAY states known / unknown / offline, STARTING, restart confirm and RETRY refusals; network off simulates the offline state) |
| 7e | Scoped (2026-10-30), not started | | Ticketed submit. `DailyServer.SubmitScoreAsync(ticketId, score)` wraps the `SubmitScore` script and classifies the reply: ok, rejected (`NO_TICKET`, `TICKET_USED`, `TICKET_EXPIRED`, `IMPLAUSIBLE`: retrying cannot help) or failed (null, `BUSY`, `BOARD_ERROR`, `ERROR`: retryable, the ticket is still valid). `LeaderboardService.SubmitScoreAsync(score)` sends it with `DailyPlay.CurrentTicketId` (no ticket = unavailable, nothing is called); the stored weekly-best gate stays and is now the only gate (the server writes both boards and keeps the best on each, and an all-time best is never below a week's), an unsent run leaves its ticket unused; on ok the reply's weekly and all-time scores update the stored bests (so `HasEverRanked` and a wiped local best self-correct), the live boards are invalidated, and rank / trend logic is unchanged. `SubmitResult` gains a `Failure` kind (`None`, `Retryable`, `NotAccepted`, `Unavailable`). Game Over: retryable shows "Couldn't submit score" with the chip's RETRY; `NotAccepted` shows a neutral "Score not accepted" with no retry; `Unavailable` (a standalone game scene has no ticket) shows "Leaderboard unavailable" with no retry. The client-side direct writes (`SubmitBoardAsync`, `SubmitAllTimeAsync`, `BoardSubmit`) are removed; the access policy denies them anyway. Checked by eval in `dev` (valid submit updates stored bests and returns rank, same ticket again `NotAccepted`, a gated run leaves the ticket usable, no ticket `Unavailable`, an implausible score `NotAccepted` and burns the ticket, the reply classification, no `AddPlayerScoreAsync` left in the scripts) and by the developer by eye (a real run from Bootstrap: Game Over rank chip with the ticket, and the offline message) |
| 7f | Scoped (2026-10-30), not started | | Close-out docs: `design-doc.md` (submission lines and open item 2 resolved: server-held attempts, tickets, plausibility), `leaderboard.md` and `daily-play.md` (server flow, client layer, offline policy, environment switch to production in Pass 9), `game-flow.md`, `ui.md`, backlog (tune `MAX_SCORE_PER_SECOND` from logged runs in Pass 9; delete leftover spike scripts and test entries) |
| 8 | Not started | | Share to socials, opt-in only: high-score card from Game Over and the weekly-result card from S11 |
| 9 | Not started | | Hardening, device checks and close-out: Editor sweep, Android and WebGL checks (including the 3 s launch cap), docs refresh, freeze this table |

Done when return to menu is real, the weekly and all-time boards, results screen, 3 attempts/day and capped practice work, submission is validated server-side and share works, verified on Android and WebGL.

## Phase: Beta — Upcoming
See [plan.md](plan.md#phase-beta) for scope.

## Phase: Full Launch — Upcoming
See [plan.md](plan.md#phase-full-launch) for scope.
