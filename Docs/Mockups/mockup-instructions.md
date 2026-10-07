# Opoint8182 — UI Design Instructions (Unity handoff)

Companion to the HTML screens in `screens/`. Each HTML file is a static, pixel-accurate reference of one screen, with that screen's notes in an HTML comment at the top. **Build in Unity UI Toolkit (UXML + USS).** HTML is reference only — don't port the CSS literally.

## 1. Canvas

- Orientation: **landscape only** (Landscape Left, auto-rotate to Landscape Right).
- Reference resolution: **2400 × 1080 (20:9)**. PanelSettings: Scale With Screen Size, Match Width Or Height, Match = 0.5.
- Wrap content in a safe-area container (24px padding, inflate from `Screen.safeArea`). Edge-anchored elements (top bars, PLAY cluster, joystick, boost) sit inside it; centred elements stay centred. Wider/narrower devices change only the gap in the middle.
- Min touch target 44px (reference space). Min text 24px.
- Web build: portrait → rotate-device gate (progress holds on S01).

## 2. Palette

| Token | Hex | Use |
|---|---|---|
| `--sky` | #3D6280 | background mid (gradient #5B86A6 → #3D6280 → #243A4F → #1B2937) |
| `--panel` | #223247 | chips, tiles, popups |
| `--panel-lip` | #101823 | hard bottom shadow under every panel |
| `--panel-inner` | #1A2634 | inset wells, inactive rows |
| `--ink` | #F6EFE2 | cream text and icons on dark |
| `--ink-dark` | #2A1A06 | text on amber buttons |
| `--amber` | #F0A93B | your resource / go: currency, PLAY, primary CTA, pips |
| `--amber-light` | #FFC76A | top of amber gradient |
| `--amber-lip` | #A2620F | bottom lip of amber buttons |
| `--coral` | #E8603C | danger / destruction, active mode tab, logo shadow |
| `--coral-lip` | #A83A20 | lip under coral |
| `--green` | #5FC26A | positive delta, perfect, health full |
| `--health-low` | #E0452F | health empty (health lerps green → this) |

Secondary text = ink at 55–70% alpha. Disabled = ink at 35%.

## 3. Typography

- **Baloo 2** (600–800): titles, numbers, score, stickers, button labels. Logo 128px; PLAY 118px; score 96px; chip numbers 32–38px.
- **Nunito** (400–800): labels, body, tab text. 19–36px; uppercase labels use +0.08–0.24em tracking.
- Monospace is annotation-only (placeholders, handoff notes). Never ship it.
- Nothing under 24px at reference resolution except the 19px sub-label in the profile chip.

## 4. Component rules

- **Chunky bevelled chip:** 20–30px radius, 3–6px `rgba(255,255,255,.1–.28)` border, hard bottom **lip** (8–14px offset, lip colour, no blur). USS has no box-shadow — use a duplicate backing element offset downward.
- **Primary button:** amber-light → amber vertical gradient, 5–6px white-28% border, amber-lip below, ink-dark text.
- **Secondary button:** panel fill, 3px white-12% border, panel-lip below, ink text.
- **Press state:** scale 0.97, lip collapses to ~4px.
- **Top bar:** all four chips are **104px tall**, 26px radius. Left: profile chip, mode-aware attempts chip. Right: currency chip (+ button), ≡ menu. Currency is a row so a second currency chip can be appended.
- **Icons:** flat shapes are placeholders; replace with the final 2D icon set.
- **3D slot:** transparent `picking-mode: ignore` region where the Unity camera render shows through. Remove the dashed outline and caption.

## 5. Flow

```
LOADING → MAIN MENU ─┬─ PLAY → (takeoff) → GAMEPLAY HUD → GAME OVER → RETRY / BANK IT / MENU
                     ├─ Leaderboard   ┐
                     ├─ Shop          │ all return to the main menu
                     ├─ Profile       │ (profile also reachable from the avatar chip)
                     └─ ≡ → Settings · How to Play · Credits · Quit
GAMEPLAY HUD → pause → PAUSE popup (settings inline) → 3-2-1 resume
```

- **Mode tabs** sit on PLAY. Competitive shows attempt pips ("3 ATTEMPTS LEFT TODAY"), Practice shows the daily clock. The top-left attempts chip mirrors the active tab. Practice never asks for confirmation.
- **Attempt confirm:** competitive PLAY opens a one-tap "SPEND AN ATTEMPT?" popup with "Don't ask again today".
- **Takeoff transition (described, not mocked):** tap PLAY → every UI element slides off its nearest edge (~0.25s, staggered) while the plane winds back like a pull-back toy, squashes, and launches; camera snaps behind it and HUD stickers drop in.
- **Game-over cascade:** ① score slams (~1.3× → 1×, 0.25s) ② stat chips pop left→right, 60ms apart ③ rank chip fades in ④ buttons rise. Rank submits on death and shows a "RANKING…" skeleton; it never blocks ④.
- **Rank rule:** after attempts 1–2 the rank is *provisional*; if the player placed well, **BANK IT** is the primary CTA (save remaining attempts), otherwise **RETRY**. After 3/3 the rank is final, the delta vs yesterday shows, and the attempt row becomes a reset timer. Never show a red ▼ — negative deltas show rank only.
- **HUD permanent set:** joystick (300px), boost (186px, secondary), fuel (largest), health, score (top-left), pause. Everything else is a transient sticker. No two full-frame effects at once (ground vignette suppresses crack overlay). Attempts / practice clock live on pause only. Boost has no meter; a future limit = circular fill around the button.

## 6. Scope for the first build

Only the **main menu** needs to be functional: PLAY loads the gameplay scene. Every other element is a visual placeholder. See `../claude-code-prompt-mainmenu-blockout.md` for the build prompt.

## 7. Original brief & assumptions

BRIEF Opoint8182 — UI mockups 2400×1080 (20:9) landscape · Android + web · UI only, no gameplay art

WHAT I ASSUMED Landscape-only, as the design doc says — the "portrait" line in your brief is treated as the rotate-prompt case, not a second layout. Every screen except gameplay returns to the main menu with one back control. All 3D content is a labelled placeholder: Unity owns the plane, city and VFX. HOW THE LOOK WORKS Comic-toy, not neon: dusk-teal sky, chunky bevelled chips with a hard bottom lip, cream ink. Amber = your resource / go, coral = danger and destruction, green→red lerp = health. Structural cues from your reference (flanking icon columns, top counter strip, oversized PLAY, overflow ≡, split profile) — none of its art, colour or branding. YOUR OPEN QUESTION Confirm before spending a competitive attempt? Yes — once. Three a day makes each one expensive, and an accidental tap is a ruined day. Keep it to a single tap-to-confirm with the pip count visible (S02), plus "don't ask again today". Practice never confirms.

PALETTE sky panel ink amber coral health TYPE Baloo 2 — titles, numbers, stickers Nunito — labels, body, buttons. Nothing under 24px on a 1080p frame. monospace — placeholder + handoff notes only

FLOW LOADING → MAIN MENU → LEADERBOARD SHOP PROFILE SETTINGS CREDITS ↩ all back to menu → GAMEPLAY HUD → GAME OVER → RETRY / MENU Takeoff transition (described, not mocked): tap PLAY → every UI element slides off its nearest edge (~0.25s, staggered) while the plane pull-back-toy-winds backwards with a rising click, squashes, then launches forward out of frame as the camera snaps behind it and the HUD stickers drop in.

## 8. Per-screen comments

### S01 · loading  →  `screens/S01-loading.html`
S01 Loading progress bar · percentage · random destruction line

**Variants / labels:** KEY ART — plane mid-crash through a skyline "Structural engineers hate this one weak point." 62% OPOINT8182

**Notes:**
- Web build: this screen doubles as the rotate-your-device gate — if portrait is detected, art swaps for a rotate prompt and progress holds.

### S02 · main-menu  →  `screens/S02-main-menu.html`
S02 Main menu logo · flanking icon columns · top counter strip · mode tabs over oversized PLAY · overflow ≡

**Variants / labels:** overflow ≡ open SETTINGS HOW TO PLAY CREDITS QUIT GAME quit hidden on web build competitive attempt confirm — my recommendation SPEND AN ATTEMPT? This run counts toward today's leaderboard. You have 3 of 3 left — they reset at midnight. or fly it in Practice instead LET'S FLY CANCEL Don't ask again today

**Notes:**
- Mode tabs swap the PLAY sub-label: competitive shows attempt pips, practice shows "28:14 left today" and never confirms. Icon glyphs are placeholders — swap for final 2D icon set.

### S03 · gameplay-hud  →  `screens/S03-gameplay-hud.html`
S03 In-game HUD permanent: joystick · boost · fuel (biggest) · health · score · pause. Everything else arrives as a sticker and leaves. Attempts / practice clock surface on pause, not in-run. Boost is unlimited for now — no meter; if a limit ships it reads as a circular fill around the button (or the button body filling), never a separate bar.

**Variants / labels:** a · clean, early run (competitive) b · practice mode (small tag under score — session clock moved to pause) c · combo active + impact popups d · low fuel glow + health lerped to red e · ceiling warning — taped banner slides down, countdown f · lateral soft limit — same tape on the offending edge g · ground proximity — red vignette + flashing text h · damage taken — cracks, smoke, health hit

**Notes:**
- Sticker rules: combo sits screen-left and shrinks + desaturates as the chain timer drains; score / currency / penalty popups spawn at the impact point and float up; bounds warnings own the edges; damage owns the whole frame. No two full-frame effects play at once — ground vignette suppresses the crack overlay.

### S04 · pause  →  `screens/S04-pause.html`
S04 Pause popup over frozen gameplay · inline audio/feel · attempt warning · 3-2-1 resume

**Variants / labels:** resume countdown 3 GET READY

### S05 · game-over  →  `screens/S05-game-over.html`
S05 Game over score → stats → rank → buttons, cascade order marked ①②③④

**Three states:**
- a · attempt 1 of 3 — placed well. Rank is provisional; BANK IT is primary so the player can save the other two attempts.
- b · attempt 1 of 3 — did not place. Same chip, neutral styling with the gap to beat; RETRY stays primary.
- c · attempt 3 of 3 — final. Rank locks, delta vs yesterday appears, attempt count is replaced by the reset timer.

**Notes:**
- ① score slams in at ~1.3× and settles (0.25s) ② stat chips pop in left→right, 60ms apart, currency counts up ③ rank chip fades up — it submits on death, so it holds a "RANKING…" skeleton (same chip, pulsing grey bars) until the server answers; it never blocks ④, which rises from the bottom edge on its own schedule. Rank is tappable and opens the leaderboard scrolled to the player's row. A negative delta shows the rank only — no red ▼ — so a bad day never reads as a punishment. Cause chip text swaps: OUT OF FUEL / WRECKED / FLEW TOO HIGH / HIT THE DECK / OUT OF BOUNDS. Practice mode drops the attempt row and the share button keeps a "practice" tag.

### S06 · settings  →  `screens/S06-settings.html`
S06 Settings full screen from the menu · same controls as a popup in-game

**Variants / labels:** ‹ SETTINGS AUDIO SOUND80 FEEL HAPTICS SCREEN SHAKE120%0% for motion-sensitive players · 150% for maximum chaos CONTROLS JOYSTICK SIDE LEFTRIGHT JOYSTICK SIZEM SENSITIVITY0.9 INVERT STEERING live preview — drag it to setsize and side, snaps to thechosen corner

**Notes:**
- Same three panels render as a centred popup in-game (S04 shows the abbreviated version). Language, graphics quality, reset progress and FPS were cut per your picks — the panels have room if you add them back.

### S07 · leaderboard  →  `screens/S07-leaderboard.html`
S07 Leaderboard weekly / all-time · ‹ › browse last 4 weeks · reset timer · sticky own row · share · empty state

**Variants / labels:** empty state (new week, no scores yet) WEEKLY ALL-TIME NOBODY HAS CRASHED YET This week's board is wide open. First submitted run takes the top spot. FLY NOW

**Notes:**
- Tabs: WEEKLY / ALL-TIME. On WEEKLY, the ‹ › buttons (top-right) step back through the previous 4 weekly boards in place — same screen, read-only, list + pinned own row + date range swap to that week; › is disabled on the current week, ‹ disabled at 4 weeks back. Reset chip reads WEEKLY RESETS IN …; it shows FINAL on past weeks. No separate archive screen.
- No accounts, so a row is just name + score. Your row stays pinned to the bottom regardless of scroll; tapping it jumps to your position in the list.

### S08 · profile  →  `screens/S08-profile.html`
S08 Player profile identity left · stats right · auto-generated name, rename anytime

**Variants / labels:** ‹ PROFILE 3D portrait — equipped plane,slow turntable DISPLAY NAME RustyBiplane47 ✎ ID #2VULYCQUJ · no account 12,480 HIGH SCORE184,920 DISTANCE FLOWN418 km BUILDINGS DOWN6,204 BEST COMBO×11 RUNS FLOWN312 PERFECT CRASHES1,088 EQUIPPED PLANECrop Duster Mk.II CRASH FXConfetti Blast BADGES 3 / 12 FIRSTBLOOD ×10COMBO 1000CRASHES ? ? locked badges show theircondition on tap

**Notes:**
- Name is generated on first launch (adjective + plane + number) so nobody is ever blocked by a naming step; ✎ turns the field into an inline text input with a 14-character cap and a dice button to re-roll.

### S09 · shop  →  `screens/S09-shop.html`
S09 Shop planes live now · crash FX + skins land in Beta

**Variants / labels:** crash FX / skins tab — Beta placeholder PLANES CRASH FX BUILDING SKINS COMING IN BETA Keep stacking ◉ — you'll need it

### S11 · last week's results  →  `screens/S11-last-week-results.html`
S11 Last week's results shown on launch after a weekly reset · winners · your rank · share · skipped-week variant

**Variants / labels:** no ranked run last week (no share) — same layout, rank "–", score "–", no SHARE button, CONTINUE only

**Notes:**
- Shown once, on the first launch after a weekly reset (never mid-session), between splash and main menu. Data = the board that just closed, locked as FINAL; it then becomes the newest entry reachable via ‹ on S07.
- Left: top 5 winners (top 3 large). Right: player card — big rank, "of N pilots · top X%", best score. SHARE MY RESULT exports a rank card (rank, score, week range). CONTINUE → S01.
- No-ranked-run variant: identical layout, rank shown as "–", no share.

### S10 · credits  →  `screens/S10-credits.html`
S10 Credits solo dev · thanks · attributions · links · support

**Variants / labels:** ‹ CREDITS MADE BY ONE PERSON YOUR NAMEHERE Design, code, art, sound and every questionable physics decision. itch.io @handle email SUPPORT THE DEV ♥ THANKS TO THE CRASH TEST CREW playtester one · playtester twoplaytester three · playtester fourand everyone who asked "why can't I dodge?" BUILT WITH Unity · Baloo 2 + Nunito (OFL)SFX pack attribution linemusic attribution lineleaderboard service TBD BACK TO MENU v0.1.0 · build 182

**Notes:**
- Next steps: (1) drop your real icon set and logo in place of the placeholders, (2) confirm whether share exports a framed screenshot or a score card, (3) I can add the rotate-device gate and a first-run how-to-play overlay on request.

