# Opoint8182 (sequel) — Concept Doc
*(Full subtitle/name still TBD — confirmed as a sequel to the old Flappy-Bird-style plane/buildings game, keeping the "Opoint8182" name)*

Living doc: this gets updated as the concept develops. Resolved sections are recaps of what's already been decided; open items are flagged and kept minimal since this is a hobby project.

---

## 1. Core Concept & Hook — ✅ Resolved
The player flies a plane with limited fuel and deliberately crashes into buildings instead of avoiding them. Crash quality (impact speed into a visible weak point) determines how much fuel you recover — a perfect crash fully refuels. The run is endless and ends only when fuel runs out. The hook: risk/reward crash-aiming (Hill Climb Racing's tension) fused with the destruction fantasy of a demolition game, instead of pure dodge-and-survive.

## 2. Genre, Platform & Format — ✅ Resolved
- Casual endless arcade / vehicular destruction
- Mobile & web
- Unity
- Camera: third-person follow (changed from side-scroller)
- Controls: joystick steer (changed from tap-to-fly)

## 3. Core Gameplay Loop & Mechanics — ✅ Resolved
**Resolved:**
- Buildings have a visible weak point to aim for; crash precision (impact speed, gated on hitting the weak point) determines refuel amount, up to a full "perfect crash" refuel.
- Two building types: **normal** (easy, quick refuel, lower value) and **tough** (requires holding boost to break; boost drains fuel faster and makes steering harder — this is the game's built-in risk lever, rather than a fuel penalty on bad crashes).
- Combo chains for crashing through multiple buildings in quick succession.
- Difficulty ramps as the run goes on via **obstacles** — see #7 for detail.
- **Second fail state confirmed:** crashing into a tough building without enough boost/speed is punishing, not a harmless bounce — either instant death or health loss that eventually kills you. Exact behavior (instant vs. gradual) is intentionally left open to decide via playtest feel, not a gap that needs answering now.
- This introduces **health as a resource alongside fuel** — fuel is the endless-run clock (crash-refuel loop), health is the "don't botch a crash / don't hit the wrong thing" penalty.
- Obstacles come in two tiers: **large obstacle buildings** (bigger, visually distinct from crashable normal/tough buildings, higher penalty on impact) and **small hazards** like birds (lower penalty, more of a reflex/dodge challenge).
- **Altitude bounds (new fail states):** the play space has an upper and lower altitude limit. Ceiling: the player is warned first (countdown pauses while actively descending, only fully resets once back in safe bounds); if they don't descend in time (or keep climbing), the run ends — the plane keeps flying in whatever direction it was already moving (no forced launch skyward), camera stops following, Game Over screen appears. Ground: hitting it ends the run instantly, with the same cosmetic "blast" treatment as a building crash or a health-zero death, Game Over screen appears. Game Over screen: title, final score, and a Restart button that resets the run.
- **Crash-quality formula simplified (Movement & fail-state rework, Pass 2):** now speed-only, gated on hitting the weak point — the angle-into-weak-point term was dropped since pitch became purely cosmetic (Pass 1) and no longer reliably reflects real impact angle.

## 4. Player Experience, Emotion & Fantasy — ✅ Resolved
Cartoonish, over-the-top destruction — chunky debris, screen shake, silly rather than gritty. The feeling should be satisfying/comedic impact, not tense realism.

## 5. Target Audience & Market Context — *not discussed, left open*
No questions asked here given the hobby-project scope — flag if you want to define this later.

## 6. Narrative, Setting & Tone — ✅ Resolved
Mechanics-first, no story required.

## 7. Progression, Difficulty & Retention — ✅ Resolved
Retention hook beyond the leaderboard = meta-progression (unlockable planes, crash effects, building skins) funded by an in-game currency earned from crashes. Currency exists from the start, but the shop/unlocks are explicitly **deferred past alpha/beta/prototype**.

Difficulty ramp: the run introduces **obstacles that must be avoided** rather than crashed into — hitting one costs fuel/health and gives no reward (two tiers: large obstacle buildings, small hazards like birds — see #3).

**New retention mechanic (Alpha scope):** instead of relying on dark patterns to drive daily return, the plan is a deliberately healthy-usage structure — **3 daily attempts** that can be submitted to the leaderboards (so at most **21 leaderboard attempts per week**), plus a separate practice mode with a **hard 30-minute/day cap** (exact enforcement mechanics TBD), and a **share-to-socials** feature (always opt-in) for posting a high-score or weekly-result card. Explicit design goal: build a daily habit without exploiting the player. Guardrail to hold onto: never sell a way to bypass the attempt/time caps once the Beta shop exists — that's the one move that would turn this into the dark pattern it's designed to avoid. The leaderboard structure, the weekly results flow and the UX "don'ts" this implies are in [Leaderboards & daily attempts](#leaderboards--daily-attempts-decided-2026-10-07) below.

### Leaderboards & daily attempts (decided 2026-10-07)

**Decision:** the daily leaderboard is replaced by a **weekly leaderboard** plus an **all-time leaderboard**, both Unity Leaderboards (UGS) boards. (There is no daily board, no monthly board and no weekly-vs-monthly question any more — backlog #27 is closed by this decision.)

*Why:*
1. The 3-attempts/day hook fits a weekly board: a player who hasn't placed can come back tomorrow, and a player who has placed has to return to hold their spot. Total attempts are capped at 21 a week.
2. A daily board has a dead end: a player who placed #1 has no way to find out after it expires. Weekly resets with a small number of archives fix that with far less data — about 7x fewer archived versions per year than daily (~52 vs ~365).
3. An all-time board alone gets unreachable for new players. The weekly board keeps competition fresh; the all-time board is the long-term record.

**Boards**

| Board | Reset | Update type | Archive | IDs (proposed) |
|---|---|---|---|---|
| Weekly | Fixed weekly schedule: **Monday 00:00 UTC** (chosen and documented here; confirm in the dashboard, see open items) | Keep best (High to Low, Best Score) | **Yes**, on every reset | `weekly_scores` |
| All-time | Never | Keep best | n/a | `alltime_scores` |

The old `daily_scores` board is retired when this ships.

**Submission**
- On run end, submit to **both boards**, but only when the score beats the player's locally stored best *for that board*. The weekly best resets when the weekly version changes. The server's keep-best already ignores lower scores, so the local check exists purely to save API calls (the client-side store is soft: clearing storage only costs a redundant submit).
- Only runs that count as one of the day's 3 leaderboard attempts are submitted. **Practice runs never submit.**
- Unused attempts do **not** bank or carry over.
- Scores are submitted client-side via the SDK until the Daily play structure milestone moves submission behind Cloud Code validation (see Alpha scope).
- Game Over shows the player's rank for the weekly board, taken from the `AddPlayerScoreAsync` response (no extra call). A negative change shows the rank only — never a red ▼ and never "you dropped N places".

**Daily attempts: day boundary and time source (TBD).** The attempt day is a UTC day (00:00 UTC, matching the weekly reset). Device clocks cannot be trusted (change the clock, regain attempts), so enforcement should use a server time source. The likely answer is the Cloud Code script that already has to validate submissions: it holds the attempt counter and returns/uses server time. Until then the cap is soft, consistent with the anonymous-identity limitation below. Decide in the Daily play structure milestone.

**"You placed" results flow (the archive use)**
- On game launch, once the player is authenticated, the client fetches the weekly board's versions (see the SDK note). It compares the newest archived version's ID with the last version ID it has already shown a result for (stored locally).
- If they differ, the client queries that archived version for the player's own entry, plus the top scores and total entry count, and shows the **Last week's results** screen (mockup S11) between the splash/loading screen and the main menu, never mid-session. A player with a ranked run sees their rank, "of N pilots", their best score and an **optional** share button. A player without one sees the same layout, neutrally, with no share. Either way the new version ID is recorded so the screen shows once. A player who was away for several weeks sees only the most recent closed week.
- A fresh install (no stored version ID yet) records the newest archived ID silently and shows nothing, so a brand-new player doesn't get a "no ranked run" screen on day one. (The anonymous identity limitation applies: a reinstall looks like a new player.)
- The player can also view last week's top scores/winner from the leaderboard screen, which doubles as the entry point to past weeks.
- **Past-weeks browsing is limited to the last 4 archived weekly versions**, using the SDK's `Limit` option on the versions query. This is a client-side display limit: stored archives keep accumulating and no way to prune them has been found. The leaderboard screen (mockup S07) steps back through those 4 weeks in place, read-only, with the date range and a FINAL tag. Archived versions never change, so they can be cached for the whole session (or persisted).
- The per-leaderboard archive cap is 10,000 versions, deleted oldest-first, irrelevant at weekly cadence (~52 a year).

**SDK notes** (verified against the installed `com.unity.services.leaderboards` **2.3.4**; method names have changed between versions, so re-check on upgrade). Live board: `AddPlayerScoreAsync`, `GetScoresAsync`, `GetPlayerScoreAsync`, `GetPlayerRangeAsync`. Archive/version calls: `GetVersionsAsync(leaderboardId, GetVersionsOptions { Limit })` returns `LeaderboardVersions` with the **live `VersionId`**, **`NextReset`** (for the reset label), `TotalArchivedVersions` and `Results` (a list of `LeaderboardVersion` with `Id`, `Start`, `End`), newest first — so one call covers the "did a week reset?" check, the reset label and the 4-week browse list. Archived entries: `GetVersionPlayerScoreAsync` (own entry; a player with no entry is expected to surface as a `LeaderboardsException` with reason `EntryNotFound` — the reason exists in the enum but this is untested, confirm in the spike), `GetVersionPlayerRangeAsync`, `GetVersionScoresAsync` (top N; the page also carries the total count used for "of N pilots · top X%"), `GetVersionScoresByPlayerIdsAsync`. Ranks are zero-based (add 1 for display).

**Call budget and polling rules.** Pricing and call allowances are unconfirmed (see open items), and the per-user call allowance is the constraint most worth designing around:
- **No polling.** No auto-refresh timers and no background fetches.
- Fetch leaderboard data only when the leaderboard screen opens, and right after a successful submission.
- Cache results client-side with a short TTL (a minute or two) and reuse the cache when the player re-enters the screen. Archived weeks are immutable, so cache them for longer.
- A manual refresh button, if one exists, gets a cooldown.
- Skip submissions that can't improve the player's best (see Submission).
- Rough estimate per session (one `GetVersionsAsync` at launch; one-time-per-week results = 2 more; viewing the weekly tab = 2 calls, the all-time tab = 2, each past week stepped to = 2 once; submissions only on a new best, at most 2 per run): **about 3 calls on a launch with nothing new, 5–10 on a typical session**, worst case on the order of a dozen. Sign-in is a separate Authentication call. At several sessions a week this is in the low hundreds of calls per user per month, which is why the rules above matter if the 500-calls-per-user figure is real.

**UX rules (design constraints).** This game deliberately avoids dark patterns. The following must **not** be built:
- Push notifications about rank changes ("someone passed you!").
- Urgency copy or countdowns designed to pressure play. A neutral "resets in X days" label is fine; styling must stay neutral (no red, pulsing or last-chance treatment).
- Streak counters, or any penalty for missing a day.
- Banked or carried-over attempts.
- Loss framing on the weekly results screen or anywhere else: show rank and score neutrally, no "you dropped N places".
- Auto-opening the share sheet. Sharing is always opt-in.
- Any future shop item that bypasses or extends the attempt or practice-time caps (the existing guardrail, restated).

**Anonymous identity (limitation).** UGS anonymous sign-in stores its token in local storage (PlayerPrefs / IndexedDB on WebGL). Clearing site data or reinstalling creates a brand-new player: scores, archive placements and the locally stored "last shown results" version don't carry over, and the 3-attempts/day cap is soft, not hard-enforced. This is accepted (consistent with "no accounts, as anonymous as possible") and is why the local best-score checks are treated as a soft optimisation only.

**Open items**
1. **Weekly reset time:** Monday 00:00 UTC is the working choice. Confirm the cron/schedule and that archive-on-reset is enabled in the UGS dashboard/config (not verifiable from the package).
2. **Day-boundary / time-source enforcement** for the 3 attempts/day (server time via Cloud Code; see above).
3. **Pricing and call allowances are unconfirmed.** Unity's pricing pages disagree: one snapshot lists a free tier of 50,000 MAU with 500 calls per user and 50 leaderboards stored per user and a small per-user fee beyond that; a newer snapshot says Leaderboards is "free for a limited time". One version of the archive docs also says each archived version counts toward the leaderboards-stored-per-user number (which could matter at ~52 versions a year per board). Verify in the current Unity dashboard before relying on any number here. LootLocker remains the fallback backend.
4. **Archive pruning:** no way to prune stored archives has been found; the 4-week limit is display-only.
5. **Results screen for players who never ranked:** the flow shows a neutral screen "either way". Consider skipping it for a player who has never submitted a ranked run (needs a local flag); decide when scoping.
6. Reset-label and results copy to be checked against the UX rules (the mockup's "WEEKLY RESETS IN 6h 12m" chip is a granular countdown; prefer days, with hours only on the final day, in neutral styling).

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
- Live leaderboard — **backend decided (2026-10-05): Unity Leaderboards (UGS)** with anonymous sign-in, over LootLocker. Why: first-party for Unity 6, anonymous auth supported, Cloud Code can validate scores server-side (client submit disabled) and later hold the daily-attempt counter, Authentication is free, REST API is an exit path. Board shape: **a weekly (Monday 00:00 UTC, archived) board plus an all-time board**, both keep-best — see [Leaderboards & daily attempts](#leaderboards--daily-attempts-decided-2026-10-07) (this replaced the original daily-reset board on 2026-10-07; the Live leaderboard milestone shipped with the daily `daily_scores` board, and the rework is tracked as backlog #28). Game Over shows the player's weekly rank with a trend vs. the player's previous attempt that week (client-side `PlayerPrefs`, soft); negative changes show the rank only. Display names deferred to the UI/art pass (2026-10-07: the profile/display-name UI lands as a shell in the UI/art pass; the real generated-name + rename logic is the unscoped "Player profile & run stats" milestone). Score submission: **client-side via the Leaderboards SDK for the Live leaderboard milestone; Cloud Code server-side validation (client submit disabled) is deferred to the Daily play structure milestone**, where the 3-attempts/day counter needs it anyway and a sanity check (reject impossible scores) can ride in the same script. Cloud Code is Unity-hosted serverless — no own server — and effectively free at hobby scale (free tier 1M invocations/20 compute-hours per month; $1.50 per extra million invocations). It cannot detect a plausible fake score, only impossible ones and direct-API abuse; until then, bad entries are removed manually via the dashboard/Admin API. Keep submission behind one thin `LeaderboardService` class so the switch is a one-file change. Accepted risks: Leaderboards pricing and call allowances are unconfirmed — Unity's pricing pages disagree and one snapshot says "free for a limited time" (LootLocker is the fallback; see the open items in the Leaderboards section); anonymous player ID is tied to local storage, so clearing PlayerPrefs/browser data creates a new identity — scores and weekly placements don't carry over and the 3-attempts/day cap is soft, not hard-enforced (consistent with "no accounts, as anonymous as possible"); WebGL support for Authentication/Leaderboards unconfirmed in docs, to be verified by a spike before building UI.
- Better UI/art, replacing the prototype's placeholder visuals.
- Weekly + all-time leaderboard rework (weekly archived board, all-time board, "last week's results" screen, call-budget rules) — see [Leaderboards & daily attempts](#leaderboards--daily-attempts-decided-2026-10-07); tracked as backlog #28.
- The daily play structure (3 leaderboard attempts/day = at most 21 a week, 30-min practice cap, opt-in share-to-socials) — see #7, since it depends on the leaderboard existing.

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

### Difficulty ramp — engagement levers (2026-09-22)
Flagged while reviewing Pass 2 (difficulty ramp curve): "more/harder" via density + tough/normal ratio is not on its own a sustainable engagement design — it plateaus into "this is just how it is now" and a skilled player eventually pattern-matches the steady state. Concrete levers to draw from when Pass 2's spawn algorithm gets revisited (backlog item — see progress.md):

- ~~**Stagger plateaus instead of one global max-distance.**~~ Implemented, Feel polish Pass 6 (resolves backlog #17 in part). Density, tough/normal ratio, and pickup rarity currently share one ramp cap. Spreading them out (ratio caps earliest, density a bit later, pickup rarity latest) gives the player a sequence of distinct phases instead of hitting the ceiling all at once — cheap, meaningfully extends the feeling of progression.
- **Modulate post-plateau intensity with a slow oscillator, don't leave it flat.** Left 4 Dead's "AI Director" trick: even at max difficulty, alternate tension spikes with brief relief rather than holding a flat ceiling. Cheap fake: multiply spawn-rate/toughness by `1 + amplitude * sin(slowFrequency * distance)` after the plateau. **Deferred (developer decision, 2026-09-26): explicitly held back out of Pass 6, revisit once playtest results on the staggered plateaus + chunk spawning are in — see backlog #19.**
- ~~**Add pattern/chunk-based spawning, not just independent per-item probability.**~~ Implemented, Feel polish Pass 6 (resolves backlog #17 in part). Independent weighted rolls eventually get pattern-matched by a skilled player and stop demanding attention. Pre-authored or semi-random chunks (clusters with internal structure, pool grows with distance) create arrangement variety a flat probability table can't — Subway Surfers / Mario Maker-style segment generation.
- **Use the "golden building" parking-lot idea (above) as the actual answer to "becomes easy at plateau."** Inject rare high-risk/high-reward spawns at a flat, non-escalating rate once past the ramp, instead of trying to keep scaling the baseline. Doesn't violate the "don't scale to infinity" constraint — the rate stays flat — but keeps forcing fresh decisions in a fully mastered steady state.
- **Consider a second axis orthogonal to density/toughness: base flight/scroll speed, slowly increasing and separately capped.** What Temple Run/Subway Surfers lean on — raises the reaction-time bar independent of building composition, composes with everything else here.
- **Make pickup recovery a risk decision, not just a probability gate.** Instead of (or alongside) making pickups rarer, place them in progressively riskier positions late-run (closer to obstacles, requiring a tighter line) so recovery itself becomes a skill test — consistent with the risk/reward identity the boost mechanic and perfect-crash refuel already establish.
