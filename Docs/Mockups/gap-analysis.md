# UI Mockup Gap Analysis (UI/art pass, Pass 1)

Input: `mockup-instructions.md` + 11 screens in `screens/` (S11 and the S07 weekly/all-time rework added 2026-10-07; HTML + PNG, 4800x2160 = 2x of the 2400x1080 reference). Compared against the UITK code in `Assets/_Project/UI` and `Scripts/UI` as of `f8fdb87`. Output of this pass: this document. No Unity changes were made.

## 1. Mockup coverage check

All 10 screens have at least one PNG, and every variant listed in the instructions has one. **No PNGs are missing.** Not mocked at all (described in text only, or not described):

| Item | Where mentioned | Status |
|---|---|---|
| Takeoff transition (UI slides out, plane winds back) | instructions §5 | described, not mocked |
| Rotate-device gate (web portrait) | S01 notes | described, not mocked |
| How to Play overlay | S02 overflow menu | menu entry exists, no screen, offered "on request" |
| Quit confirmation | S02 overflow menu | none; Quit hidden on web |
| Settings as in-game popup (full) | S06 notes | only abbreviated version in S04 |
| Shop tabs Crash FX / Building Skins | S09 | one "coming in Beta" PNG covers both |
| Locked/unlocked badge tap detail | S08 | text note only |
| S03 b/e/f/g/h overlays | S03 | mocked (a to h all present) |

Housekeeping: PNG/HTML name drift (`S05c-...attempt-3of3png.png`, `S08-player-profile.png` vs `S08-profile.html`); the instructions point at `screens/S01-loading.html` but files sit in per-screen subfolders; §6 references `claude-code-prompt-mainmenu-blockout.md`, which does not exist (struck through, harmless).

## 2. Mockup issues to resolve before building

These are defects or ambiguities in the mockup itself, not in our code.

1. **HUD resource bars are unlabelled/ambiguous (S03).** A 10-segment amber pip bar labelled FUEL sits over a thin green bar with no label. Instructions say "fuel (largest), health". Reading: pips = fuel, thin bar = health (green to red lerp, S03.d). Needs confirming. Also: pips imply fuel is quantised to 10 steps, while `FuelSystem` is continuous (fraction to pips needs a partial-fill rule).
2. **Text clipping/wrapping in the comps:** profile chip ("LV 14 · PROFILE" wraps), settings labels ("JOYSTICK SIZE", "SCREEN SHAKE", "INVERT STEERING" overlap their sliders), "Don't ask again today", "or fly it in Practice instead", leaderboard reset timer, profile "Crop Duster Mk.II"/"Confetti Blast". These are HTML layout bugs; fix in UXML (wider containers/min-widths), don't copy.
3. **Stray overlay artefacts** in S05 (a red "HEALTH" sticker and combo sticker bleed through the game-over layer, 3D caption text over the EARNED chip). Ignore; game over sits on a dimmed frozen frame.
4. **Profile name inconsistency:** menu chip says "OLIVE · LV 14", profile/leaderboard say "RustyBiplane47". Menu chip should show the generated name. "LV 14" implies a level/XP system that appears nowhere else in the mockup or design doc. Needs a decision (cut it, or define it).
5. **Mockup contradicts shipped behaviour:** Game Over currently shows red ▼ / green ▲ rank trend (`GameOverUI.TrendGlyph`); mockup rule is "never show red ▼, negative deltas show rank only". Update the rule and fold backlog #26 (glyphs).
6. **Practice-mode Game Over** is described (drops attempt row, share tagged "practice") but not mocked; **practice HUD** has a "PRACTICE" tag under score only.
7. Mockup uses an **HTML-only dashed outline + monospace captions** for 3D slots; both are annotation, not shipped (instructions say so).

## 3. Foundation gaps (apply to every screen)

| Need | Mockup | Available | Gap |
|---|---|---|---|
| Canvas | 2400x1080, Match 0.5, safe area 24px | `FuelGaugePanelSettings.asset` already 2400x1080 / 0.5; menu has safe-area inset code | Safe area exists only in `MainMenuController`; HUD/Game Over don't use it. Need one shared safe-area helper. |
| Fonts | Baloo 2 (600-800), Nunito (400-800), OFL | none in project (`find` found no ttf/otf/font assets) | Import both, create UITK font assets (Unity 6 `FontAsset`/`-unity-font-definition`). **User to supply files (not in repo yet).** Check arrow/▲▼ glyph coverage or use image icons (#26). |
| Palette/tokens | 13 tokens (sky, panel, amber, coral, green...) | USS vars declared only in `MainMenu.uss` `.root`; HUD USS uses ad-hoc flat colours | Extract one shared `Theme.uss` (tokens + component classes) and have every UXML include it. |
| Background | vertical gradient + diagonal stripe overlay | flat mid-tone + stripe element in menu (gradient approximated) | USS has no `linear-gradient`; use a baked gradient texture/9-slice or `generateVisualContent`. Decide once, reuse for bars/buttons. |
| Bevel chip + hard lip | 8-14px lip, gradients on primary buttons | `border-bottom` approximation for small chips; real lip sibling only for logo/PLAY | Need reusable chip/button component (UXML template or custom `VisualElement`) with lip + press state, instead of per-screen copies. |
| Press state | scale 0.97 + lip collapse | PLAY uses translate-down (dev correction), others scale 0.95 | Spec says scale; our shipped press is translate. Keep translate (it fixed a real on-device bug), update the spec note. |
| Icons | final 2D icon set (placeholders now) | CSS-shape placeholders (coin, bars, menu bars) | Icon set + logo needed from user. |
| Animations | cascade, slams, sticker pop/float, takeoff, countdown | PrimeTween installed; used in leaderboard | Available. Needs a shared animation helper per pattern. |
| Panel structure | one composed HUD | 8 separate `PanelRenderer` GameObjects (fuel, health, score, 3 warnings, touch, game over) + menu | Decide: keep per-element panels or consolidate to one HUD tree (S03 is a single composition; stickers/overlays interact, e.g. "no two full-frame effects"). |
| Navigation | menu <-> 6 full screens, back button, popups | menu is one scene with an overlay panel pattern (leaderboard) | Need a small screen-stack/router (show/hide, back, popup layer). Not present. |
| Loading screen | S01 bar + % + quip, doubles as rotate gate | `BootstrapLoader` loads Menu+Prototype additively, no UI, no progress | New screen + progress hook from `AsyncOperationGroup`. |

## 4. Per-screen gap

Legend: **Have** = exists and roughly matches; **Restyle** = exists, visuals only; **Build** = new UI; **Logic** = needs non-UI systems.

### S01 Loading
Build: bar, %, random quip (list of destruction lines), key-art slot, rotate gate (web, portrait detection). Logic: real progress from scene loads. Key art: user to supply.

### S02 Main menu (+ attempt confirm, overflow)
- Have: logo (3-layer), tagline, top chips, shop/leaderboard tiles, mode tabs, PLAY + lip, 3D plane slot, safe area, stub toast.
- Restyle: fonts, gradients, icons, profile text fit, press feedback.
- Build: **attempt-confirm popup** (checkbox "don't ask again today"), **overflow menu** with working items (currently 4 labels, non-functional, picking ignored), mode-tab state swap (competitive shows pips, practice shows daily clock), top-left attempts chip mirroring tab.
- Logic: attempts count/reset, practice clock + 30 min cap, currency, profile name/level. All belong to **Daily play structure** and **Beta** milestones (not this one).
- Change: leaderboard tile currently opens a popup Top-10; mockup opens full screen S07.

### S03 Gameplay HUD (a-h)
- Have (functional, ugly): fuel fill bar, health fill bar, score + combo label + combo timer bar, altitude/lateral/ground warning banners, floating joystick, boost button.
- Restyle/rebuild: fuel pips + health bar layout, score with label, combo as sticker (shrink/desaturate with timer), taped ceiling banner with countdown number, lateral tape on offending edge, ground vignette + flashing text, low-fuel glow state, practice tag, joystick (300px) and boost (186px circle) skins, pause button.
- Build (new): **pause button + pause popup** (see S04), **impact popups** (+score, +coins, PERFECT CRASH, penalty) spawned at world impact point and floating up (needs world-to-screen from crash events), **damage overlay** (cracks/smoke, owns whole frame), **full-frame effect arbiter** (ground vignette suppresses cracks).
- Logic: boost has no meter (nothing needed). Currency popup needs currency. "PERFECT CRASH" needs the crash-quality event (exists via `PlayerManager.HandleCrashed`; verify what is exposed to UI).

### S04 Pause
Build everything: no pause exists (no `timeScale`-based pause, no pause button; only `CameraJuice` hit-stop touches timeScale). Needs pause state in `GameManager`, popup (sliders, toggle, RESUME/RESTART/QUIT), 3-2-1 resume countdown on unscaled time, "quitting still burns attempt" warning (attempts logic). Settings controls shared with S06.

### S05 Game Over (a/b/c)
- Have: title, score, rank label with trend + Retry, Restart button.
- Build: cascade (score slam, stat chips, rank chip, buttons), cause chip (OUT OF FUEL / WRECKED / FLEW TOO HIGH / HIT THE DECK / OUT OF BOUNDS), NEW BEST sticker, stat chips (distance, flattened, best combo, earned), rank chip states (RANKING skeleton / provisional / final+locked / neutral did-not-place), BANK IT / RETRY NOW / LEADERBOARD / MAIN MENU / PRACTICE / SHARE buttons.
- Logic gaps: `GameManager.RunEnded` carries **no cause** (four separate depleted/exceeded sources are all funnelled into one event); no distance, buildings-flattened or best-combo tracking found in `Scripts`; no personal-best store; no currency; attempts (BANK IT/RETRY semantics); share; "yesterday delta" (partly there: `LeaderboardService` trend vs previous attempt same day, not vs yesterday).

### S06 Settings
Build all: sound slider (needs audio mixer/master volume; **no audio system exists**), haptics toggle (no haptics code), screen shake % (`CameraJuice` exists, needs a multiplier hook), joystick side L/R + size S/M/L + sensitivity + invert + live preview (folds **#25**; `TouchControlsUI` has fixed serialized radius/size and a hardcoded left-half zone), persistence (PlayerPrefs; nothing stores settings yet).

### S07 Leaderboard
- Have: `LeaderboardPanelUI` + `LeaderboardService.GetTopScoresAsync` (Top-10 of the old daily board, loading/empty/error/retry states, own row appended if outside top 10).
- Restyle/build: full-screen layout, gold/silver/bronze rows, sticky own row with rank/name/score, SHARE MY RANK, WEEKLY / ALL-TIME tabs, ‹ › buttons that step through the last 4 weekly boards in place (date range label, read-only, FINAL tag on past weeks, › disabled on the current week, ‹ disabled 4 weeks back), reset chip, empty state (FLY NOW). UI only in this milestone, with static data.
- Logic (backlog #28, not this milestone): `weekly_scores` + `alltime_scores` boards, `GetVersionsAsync(Limit 4)` for the browse list and reset time, archived-version fetches for past weeks, cache/no-polling rules. See §10.

### S08 Profile
Build all, plus data: display name (generated adjective+plane+number, 14-char inline edit, dice re-roll), ID, coins, 6 lifetime stats (high score, distance, buildings down, best combo, runs, perfect crashes), equipped plane / crash FX cards, badges 3/12 (needs 12 badge definitions + unlock rules), 3D turntable portrait slot. **Nothing here is persisted today** (no stats store). Display-name field was explicitly deferred into this milestone (UGS Authentication has player-name APIs; verify exact API/limits in 3.8.0 before committing to it).

### S09 Shop
Beta-milestone content in the Alpha mockup. UI shell only: planes grid (equipped / buy / not enough / locked with progress), tabs with BETA badges, coin chip. Logic (currency, unlocks, purchases) is Beta. Needs more than one plane to be non-empty.

### S10 Credits
Build (static): text, links (itch.io, email), support, thanks list, attributions (Baloo 2/Nunito OFL, SFX/music TBD), version/build. Easy; needs real names/links from user. Version string source (`Application.version`).

## 5. Scope observations

1. The mockup is a **complete game UI**, but several screens/features belong to later milestones in `plan.md`: attempts + practice + share (Daily play structure, Alpha), currency + shop + unlocks (Beta). Recommendation: this milestone builds the **UI and a data-driven shell** for them (static/placeholder data behind a small interface), not the systems, as the first main-menu build already did for chips.
2. A few features are **not backed by any plan item** and need a home: pause + resume countdown, settings (audio/haptics/shake/controls), profile + persistence (name, stats, badges), run-end cause + run stats, loading screen/rotate gate, how-to-play, credits.
3. **No audio system exists** (sound slider has nothing to drive), and no haptics.
4. 3D art slots (plane on rooftop, key art, turntable, shop cards) are placeholders until the 3D half of this milestone.

## 6. Proposed pass breakdown (draft; superseded by §8 after developer review)

Incremental per the planning choice; only the next pass would be committed to `plan.md`.

| Pass | Scope |
|---|---|
| 2 | Foundation: fonts, shared `Theme.uss` tokens, reusable chip/button/popup/back-header components, screen router + safe-area helper, bg/gradient approach. Verify on one existing screen (menu) |
| 3 | Main menu restyle to match S02 + overflow menu + attempt-confirm popup shell + mode-tab swap (placeholder data) |
| 4 | Gameplay HUD rebuild (S03 a-h states) + pause button/popup/countdown (S04) |
| 5 | Game Over rebuild (S05) incl. run cause + run stats plumbing; fold #26 |
| 6 | Settings (S06) + persistence + joystick prefs (fold #25) |
| 7 | Leaderboard full screen (S07) + Profile (S08, fold display-name) |
| 8 | Shop shell (S09), Credits (S10), Loading + rotate gate (S01) |

Open questions for the developer: (a) confirm the HUD bar reading in §2.1; (b) keep per-element HUD panels or consolidate; (c) cut or define "LV 14"; (d) monthly/all-time tabs: build boards now or hide until later; (e) provide fonts, icon set, logo, key art when the pass needs them; (f) is the pause/settings/audio work in scope here or its own milestone.

## 7. Developer decisions (2026-10-07)

Answers to the §2/§6 questions, plus the not-mocked items.

| Topic | Decision |
|---|---|
| HUD bars | Reading confirmed: 10-segment pips = fuel, thin bar = health. |
| HUD structure | **Merge into one HUD panel.** The 8 current HUD panels are states of a single panel. |
| "LV 14" | Cut. No player-experience system is planned. Menu chip shows the name (+ "PROFILE" sub-label) only. |
| Leaderboard tabs | Superseded 2026-10-07: the board structure is now **weekly (archived) + all-time** (S07 reworked, S11 added). See §10 and `design-doc.md`. Backlog #27 closed; real work is backlog #28. |
| Placeholder scope | Pause, settings and audio are **separate milestones**; this milestone builds UI placeholders only. Recorded in `plan.md`. |
| Assets | Developer supplies fonts and logo. Key art deferred; logo stands in. Icons listed in §9. |
| Takeoff transition | Entirely 3D, belongs to the 3D half of this milestone, not UI. |
| Rotate-device gate | Screen will be added (UI/art pass, with the loading screen). |
| How to Play | Separate full tutorial, Beta. Menu entry stays a placeholder. |
| Quit confirmation | Screen will be added (UI/art pass). Quit stays hidden on web. |
| In-game settings popup | The abbreviated popup in S04 **is** the full pause popup. The "full list lives in Settings (S06)" caption is stale. S06 is a separate full screen. |

Where the remaining non-UI gaps went (all in `plan.md`):
- Weekly + all-time leaderboard rework (Alpha, backlog #28, unscoped): the boards, archive/results flow, call-budget rules.
- Daily play structure (Alpha, unscoped): attempts, practice clock/cap, attempt-confirm and BANK IT logic, share.
- Pause, settings & audio (Alpha, unscoped, new): functional pause + resume, settings wired/persisted, audio system, haptics, joystick prefs (#25).
- Player profile & run stats (Alpha, unscoped, new): generated/renamable display name, lifetime stats, run-end cause + per-run stats, NEW BEST, badges.
- Shop, currency, economy (Beta): currency, shop logic, equipped plane/FX.
- How to Play tutorial (Beta, unscoped, new).

Consequences for this milestone: Game Over cause chip, stat chips, NEW BEST, attempts text, coin amounts, profile stats/badges, settings values and monthly/all-time rows all show **static placeholder data** until those milestones land.

## 8. Revised UI pass breakdown (draft)

Superseded 2026-10-07 by the detailed Pass 2-8 draft in `../current-state.md` (live status) and `../plan.md` (scope). Kept below for history.

| Pass | Scope |
|---|---|
| 2 | Foundation: import Baloo 2 + Nunito (UITK font assets), shared `Theme.uss` (tokens + chip/button/popup/back-header classes), screen router + popup layer, safe-area helper, background gradient/stripe approach. Prove it on the main menu. |
| 3 | Main menu to S02: restyle, profile chip (no level), mode-tab swap shell, overflow menu (working open/close, Quit hidden on web), attempt-confirm popup, quit-confirmation popup. |
| 4 | Merged HUD panel (S03 a-h states: fuel pips, health bar, score, combo sticker, ceiling/lateral/ground warnings, low-fuel/damage overlays, impact popups, joystick/boost skins, pause button) + pause popup visuals (S04) incl. resume-countdown visual. Replaces the 8 current HUD panels. |
| 5 | Game Over to S05 (a/b/c states, cascade animation, placeholder stats), fold backlog #26 (arrow image instead of ▲/▼ glyph; drop the red ▼ per the rank rule). |
| 6 | Settings (S06), Leaderboard screen (S07: WEEKLY/ALL-TIME tabs, ‹ › past-week browsing, UI only, static data), Profile (S08 shell). |
| 7 | Shop shell (S09), Credits (S10), Loading screen + web rotate-device gate (S01), Last week's results screen (S11, shell, wired into the loading to menu flow). |
| 8+ | 3D half: plane/building/obstacle/environment art, rooftop/turntable slots, takeoff transition. Scoped after the UI passes land. |

## 9. Icon set required

Everything below is a placeholder CSS shape in the mockup/menu build today. Flat, single-colour-friendly, readable at 44px; the HUD ones are sized to the 300px joystick and 186px boost layout.

| Where | Icons |
|---|---|
| Top bar / menu | profile avatar (person silhouette), coin, green "+" add, hamburger (≡), shop (crate/hangar), leaderboard (bars) |
| Overflow menu | settings (gear/ring), how to play, credits, quit (placeholders if text-only is fine; the mockup only shows the gear) |
| Navigation / generic | back chevron (‹), close (×), checkbox (empty + checked), share, pencil (rename), dice (re-roll name), lock, "?" locked tile |
| HUD | pause (two bars), boost glyph (button is text "BOOST", optional), warning-tape pattern (tileable), crack overlay + smoke (full-frame art) |
| Game Over / leaderboard | trend arrow up (replaces ▲ glyph, **#26**), trend arrow down/neutral (recolourable via USS tint), rank medals gold/silver/bronze (rows are tinted in the mockup, medals optional) |
| Profile | 5 badge icons minimum (First Blood, x10 Combo, 1000 Crashes + locked) of 12 total, equipped-plane and crash-FX thumbnails |
| Shop | plane card renders (3D art pass), coin glyph |
| Loading / web | rotate-device (phone turning) icon, logo (supplied) |
| Credits | itch.io, email, heart (support) |

Fonts to supply: Baloo 2 (SemiBold/Bold/ExtraBold) and Nunito (Regular/SemiBold/Bold/ExtraBold), both OFL. Check the final font has the "×" (multiplication) and "·" glyphs used in the mockup.

## 10. Leaderboard rework impact (2026-10-07)

Mockup change: S07 is now WEEKLY / ALL-TIME with ‹ › browsing of the last 4 weeks, and **S11 Last week's results** is new (plus a no-ranked-run variant). Board structure decision and rules live in `design-doc.md` > Leaderboards & daily attempts; the work is backlog #28 (not scoped, no code changes yet).

**New screen S11 (UI shell in this milestone):** left, top 5 winners (top 3 large); right, player card (big rank, "of N pilots · top X%", best score, SHARE MY RESULT, CONTINUE); header chip with the week range and FINAL. Shown once, on the first launch after a weekly reset, between loading (S01) and the main menu, never mid-session; its data is the board that just closed, which then becomes the newest entry reachable via ‹ on S07. Needs a new flow step (loading, then results, then menu) and a share-card export (rank card: rank, score, week range) for the Daily play structure milestone.

**Data S07/S11 need and where it comes from (SDK 2.3.4):**

| UI element | Call |
|---|---|
| Reset chip, live version, 4-week list (labels + date ranges) | `GetVersionsAsync(id, Limit = 4)` returns `NextReset`, live `VersionId`, `Results[]` (`Id`, `Start`, `End`), newest first |
| Top rows (current week / all-time) | `GetScoresAsync` |
| Own row (current) | `GetPlayerScoreAsync` (or the `AddPlayerScoreAsync` response right after a submit) |
| Past-week rows | `GetVersionScoresAsync(versionId)` (top N + `Total`) |
| Past-week own row / S11 rank | `GetVersionPlayerScoreAsync(versionId)`; a player with no entry must render the S11 "no ranked run" variant (error reason for no-entry untested) |
| "of N pilots · top X%" | `Total` from the version scores page, X computed client-side |

**Mockup issues to fix when building (not implementation blockers):**
1. **S11 no-rank PNG still shows the SHARE MY RESULT button**, but the notes say "no share, CONTINUE only". Follow the notes.
2. **S05 Game Over still reads daily:** "TODAY'S RANK", "delta vs yesterday", "next attempts in 6h 12m", and the S02 attempt-confirm says "today's leaderboard". Update copy to the weekly framing ("THIS WEEK'S RANK", change vs your previous attempt this week, no yesterday delta). The attempts-reset timer is still daily, so keep the "next attempts" wording but see the next point.
3. **Countdown copy vs the UX rules:** "WEEKLY RESETS IN 6h 12m" (S07/S11) and the Game Over reset timer are granular countdowns. The design doc allows a neutral "resets in X days" label and forbids pressure countdowns. Use days (hours only on the final day) with neutral styling, no red/pulse.
4. **Sample dates are illustrative:** S07/S11 show "Oct 6 – Oct 12" and "Sep 29 – Oct 5" (Monday to Sunday), consistent with a Monday reset. The shipped labels come from `LeaderboardVersion.Start/End` in UTC; decide whether to show them in UTC or local date to avoid an off-by-one near the boundary.
5. The S11 header says "skipped-week variant", but the only variant mocked is "no ranked run". A player away for several weeks sees only the most recent closed week (design doc), so no separate variant is needed.
6. `mockup-instructions.md` flow diagram and §5 do not list S11. Add it: LOADING, then S11 (once after a reset), then MAIN MENU.
7. "SHARE MY RANK" (S07) and "SHARE MY RESULT" (S11) must open the share sheet only on tap, never automatically.

