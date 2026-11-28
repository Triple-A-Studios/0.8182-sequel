# Effects (3D art pass, Pass 6)

The crash and run-end effects and the heal vignette. The scope is in [plan.md](../plan.md#milestone-3d-art-pass) (Pass 6).

## Pack
Cartoon FX Remaster (CFXR), imported into `Art/VFX`. The game uses three prefab variants in `Prefabs/VFX`; a fourth (`CFXR Explosion Smoke 2 Solo (HDR) Variant`) is not used. `Assets/JMO Assets` is the pack's Editor-only welcome window. The pack's demo assemblies (`Demo Assets`) are not used by the game.

All three use the Built-in Particle System, shaders supported in URP, no lights, no particle collision and no trails; the pack's own camera shake is off (the game's `CameraJuice` shake follows the player's Screen shake setting).

| Effect | Prefab variant | When |
|---|---|---|
| Crash burst | `CFXR Explosion 1 Variant` | A building is destroyed: Normal, Tough (a mistimed hit too) and Heal |
| Run-end blast | `CFXR3 Hit Fire B (Air) Variant` (one shot) | Out of fuel, wrecked, hit the deck |
| Wreck fire | `CFXR2 Firewall A Variant` (loops) | Right after the blast, stays on the plane until the scene reloads |

Flew too high and out of bounds have no blast or fire: the plane keeps flying (design doc, fail states).

**Changed in the variant:** the burst's `Smoke` lifetime went from 0.8 to 1.5 s down to 0.27 to 0.5 s. The chase camera flies through the burst about half a second after the hit, and the longer smoke filled about a third of the screen (also the costliest overdraw on a phone).

## `PlaneVfx` (`Scripts/Vfx`, on `Plane.prefab`)
- Everything is instantiated in `Awake` and reused: 4 bursts and 1 blast under a `PlaneVfxPool` root moved into the plane's scene (scenes load additively), and the fire as an inactive child of the plane. The scene reload on retry or return to menu clears them.
- Replay is deactivate, move, activate. The pack's `CFXR_Effect` destroys a finished effect by default; pooled instances are switched to disable. A one-shot that finishes off screen can stay active (Unity pauses culled particles) until its slot is reused, which is harmless.
- Shadow casting is switched off on every particle renderer at creation.
- Bursts follow `CombatEvents.Crashed` (buildings) and `CombatEvents.Restored` when the source is a `HealBuilding`. The burst sits 1.5 ahead of the plane, scaled 0.7 (quality 0) to 1.2 (quality 1).
- `PlayerManager.HandleDepleted` calls `PlayRunEnd()`. Blast scale 2.5, fire scale 1.
- Particles use scaled time, so they freeze on pause and during hit-stop.

## Game Over pause
`GameOverUI.m_blastDelaySeconds` (1 s, unscaled) holds the panel back for the three endings with a blast. Score, cause, stats, best and attempt state are read at the moment the run ends; the panel, the rank submit and the badge toast start after the delay.

## Heal vignette
`heal-vignette` in `Hud.uxml` / `.uss` (`Tex_UiVignetteGreen.png`, from `Tools/generate_ui_textures.py`). `HudPresenter` calls `HudView.ShowHeal()` on `CombatEvents.Restored` when the amount is above 0 and the run is live: a health pickup or a Heal building hit on its weak point, health full or not. It fades in over 0.1 s to 0.6 opacity and out over 0.9 s, replaces the damage cracks if they are up, and is hidden while the ground warning owns the frame.

## Checked
Editor Play mode (2026-11-28), with buildings placed and damage applied from script: burst on a Normal building, vignette on for a restore and off for a zero restore, blast and fire, Game Over after the pause (wrecked and out of fuel), effects again after a scene reload. The developer then playtested in the Editor and was happy (2026-11-28). Not checked: Android and WebGL (frame rate, and any first-use shader hitch).

## Held back
Plane wear smoke, pickup particles and bird / obstacle hit effects: after the optimisation work (backlog #42).
