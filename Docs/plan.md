# Plan

## Phases

### Phase: Prototype
Milestones:
- **Core loop, bare minimum** — plane movement, joystick steering, one building type (normal only, weak-point crash), fuel gain/loss, run ends when fuel hits zero, minimal UI (fuel gauge only). Purpose: validate that flying and crashing feels good before anything else is built.
- **Building types + scoring** — adds tough buildings, the boost mechanic, health as a second resource (from mistimed tough-building crashes), and a simple visible score. Prototype is considered done here — the full core resource loop (fuel + health + score) is playable.

### Phase: MVP
Milestones:
- **Obstacles, combos, recovery** — adds the two-tier obstacle system (large obstacle buildings, small hazards like birds), combo chain scoring, and health pickups to recover from bad crashes/obstacle hits.
- **Difficulty ramp** — obstacle/building density and toughness increase over the length of a run.
- **Feel polish** — camera effects (shake, follow-tightening on boost, etc.) and general juice. MVP is considered done here.

### Phase: Alpha
Milestones:
- **Live leaderboard** — backend not yet decided; Unity's own leaderboard service vs. a free third-party option is research needed, not resolved.
- **UI/art pass** — replace the prototype's placeholder visuals with real UI/art.
- **Daily play structure** — 3 leaderboard attempts/day, a separate practice mode with a hard 30-minute/day cap (exact enforcement mechanics TBD), and a share-to-socials feature for posting a high-score screenshot.

### Phase: Beta
Milestones:
- **Shop, currency, economy** — unlockable planes, crash effects, building skins, funded by an in-game currency earned from crashes. Guardrail: attempt/time caps from Alpha stay un-monetized.

### Phase: Full Launch
Milestones:
- **Polish pass** — polished, complete version of everything above. No additional major systems currently planned beyond what Beta introduces.
