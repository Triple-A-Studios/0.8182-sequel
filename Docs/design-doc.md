# Opoint8182 (sequel) — Concept Doc
*(Full subtitle/name still TBD — confirmed as a sequel to the old Flappy-Bird-style plane/buildings game, keeping the "Opoint8182" name)*

Living doc: this gets updated as the concept develops. Resolved sections are recaps of what's already been decided; open items are flagged and kept minimal since this is a hobby project.

---

## 1. Core Concept & Hook — ✅ Resolved
The player flies a plane with limited fuel and deliberately crashes into buildings instead of avoiding them. Crash quality (angle/speed into a visible weak point) determines how much fuel you recover — a perfect crash fully refuels. The run is endless and ends only when fuel runs out. The hook: risk/reward crash-aiming (Hill Climb Racing's tension) fused with the destruction fantasy of a demolition game, instead of pure dodge-and-survive.

## 2. Genre, Platform & Format — ✅ Resolved
- Casual endless arcade / vehicular destruction
- Mobile & web
- Unity
- Camera: third-person follow (changed from side-scroller)
- Controls: joystick steer (changed from tap-to-fly)

## 3. Core Gameplay Loop & Mechanics — ✅ Resolved
**Resolved:**
- Buildings have a visible weak point to aim for; crash precision (speed × angle into it) determines refuel amount, up to a full "perfect crash" refuel.
- Two building types: **normal** (easy, quick refuel, lower value) and **tough** (requires holding boost to break; boost drains fuel faster and makes steering harder — this is the game's built-in risk lever, rather than a fuel penalty on bad crashes).
- Combo chains for crashing through multiple buildings in quick succession.
- Difficulty ramps as the run goes on via **obstacles** — see #7 for detail.
- **Second fail state confirmed:** crashing into a tough building without enough boost/speed is punishing, not a harmless bounce — either instant death or health loss that eventually kills you. Exact behavior (instant vs. gradual) is intentionally left open to decide via playtest feel, not a gap that needs answering now.
- This introduces **health as a resource alongside fuel** — fuel is the endless-run clock (crash-refuel loop), health is the "don't botch a crash / don't hit the wrong thing" penalty.
- Obstacles come in two tiers: **large obstacle buildings** (bigger, visually distinct from crashable normal/tough buildings, higher penalty on impact) and **small hazards** like birds (lower penalty, more of a reflex/dodge challenge).

## 4. Player Experience, Emotion & Fantasy — ✅ Resolved
Cartoonish, over-the-top destruction — chunky debris, screen shake, silly rather than gritty. The feeling should be satisfying/comedic impact, not tense realism.

## 5. Target Audience & Market Context — *not discussed, left open*
No questions asked here given the hobby-project scope — flag if you want to define this later.

## 6. Narrative, Setting & Tone — ✅ Resolved
Mechanics-first, no story required.

## 7. Progression, Difficulty & Retention — ✅ Resolved
Retention hook beyond the leaderboard = meta-progression (unlockable planes, crash effects, building skins) funded by an in-game currency earned from crashes. Currency exists from the start, but the shop/unlocks are explicitly **deferred past alpha/beta/prototype**.

Difficulty ramp: the run introduces **obstacles that must be avoided** rather than crashed into — hitting one costs fuel/health and gives no reward (two tiers: large obstacle buildings, small hazards like birds — see #3).

**New retention mechanic (Alpha scope):** instead of relying on dark patterns to drive daily return, the plan is a deliberately healthy-usage structure — **3 daily attempts** that can be submitted to the leaderboard, plus a separate practice mode with a **hard 30-minute/day cap** (exact enforcement mechanics TBD), and a **share-to-socials** feature for posting a high-score screenshot. Explicit design goal: build a daily habit without exploiting the player. Guardrail to hold onto: never sell a way to bypass the attempt/time caps once the Beta shop exists — that's the one move that would turn this into the dark pattern it's designed to avoid.

## 8. Art & Audio Direction — *lightly touched, left open*
Cartoonish crash feel is set (see #4); everything else (plane/building visual style, music, SFX direction) is open — probably easier to nail down once there's a rough prototype to look at.

## 9. Multiplayer & Social Design — N/A
Skipped — single-player with an asynchronous leaderboard, not multiplayer/social.

## 10. Scope, Technical Constraints & Team — ✅ Resolved
Solo project.

## 11. Business Model & Monetization — non-commercial for now, tentative direction if that changes
Currently a free, non-commercial hobby project — no ads, ever, no account creation, no cloud saves, and as anonymous a leaderboard as possible.

If monetization is added later (before full launch), current thinking — **not yet committed**: keep the base game free, and add a single optional one-time **"Supporter Pack"** (bonus skins/crash effects + a small leaderboard nod, e.g. a display-name effect) rather than paywalling the base game or selling individual cosmetics à la carte. Web build (itch.io) is the more flexible venue for this since it avoids mobile app store in-app-purchase requirements; mobile would need the pack implemented as a real IAP item if pursued there. Guardrail already established in #7 still applies: never sell a way to bypass the attempt/time caps.

## 12. Success Criteria & Definition of Done — ✅ Resolved
"Done" is defined per milestone rather than a single vibe check — see the Design Spec section below. Prototype is considered done after Milestone 2; MVP is considered done after Milestone 5.

---

## Design Spec — Development Phases & Milestones

### Prototype / MVP (five milestones — each a testable checkpoint on its own)

1. **Core loop, bare minimum.** Plane movement, joystick steering, one building type (normal only, weak-point crash), fuel gain/loss, run ends when fuel hits zero. Minimal UI (fuel gauge only). *Purpose: validate that flying and crashing feels good before anything else is built.*
2. **Building types + scoring.** Adds tough buildings, the boost mechanic, health as a second resource (from mistimed tough-building crashes), and a simple visible score. **Prototype is considered done here** — the full core resource loop (fuel + health + score) is playable.
3. **Obstacles, combos, recovery.** Adds the two-tier obstacle system (large obstacle buildings, small hazards like birds), combo chain scoring, and health pickups to recover from bad crashes/obstacle hits.
4. **Difficulty ramp.** Obstacle/building density and toughness increase over the length of a run.
5. **Feel polish.** Camera effects (shake, follow-tightening on boost, etc.) and general juice. **MVP is considered done here.**

### Alpha (after MVP)
- Live leaderboard — backend not yet decided; Unity's own leaderboard service vs. a free third-party option is flagged as **research needed**, not resolved.
- Better UI/art, replacing the prototype's placeholder visuals.
- The daily play structure (3 leaderboard attempts/day, 30-min practice cap, share-to-socials) — see #7, since it depends on the leaderboard existing.

### Beta (after Alpha)
- Shop, currency, economy — unlockable planes, crash effects, building skins (see #7). Guardrail: attempt/time caps from Alpha stay un-monetized.

### Full Launch (after Beta)
- Polished, complete version of everything above. No additional major systems currently planned beyond what Beta introduces.

### Build & deployment order
- Iterate in the Unity Editor first.
- Test on Android once a substantial set of milestones is built.
- Deploy to web (Vercel or itch.io) only after the full MVP (all five milestones) is complete.

### Platform note: orientation
The game is landscape-only. Not an issue on desktop, but the web build needs to explicitly detect portrait on Android browsers and prompt/force the player to rotate their device.

---

## Ideas raised but not yet decided (parking lot)
- Daily seeded run (same building layout for everyone, separate leaderboard) — strongest candidate for a hook beyond raw high-score chasing
- Rare "golden building" spawns for a disproportionate score/fuel bonus
- Building weight classes beyond normal/tough, if the two-tier system ends up feeling thin
