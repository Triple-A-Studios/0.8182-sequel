# Haptics and screen shake (Pause, settings & audio, Pass 5)

## Screen shake slider

`CameraJuice.Shake(magnitude, duration)` multiplies the magnitude by `SettingsService.ScreenShake / 100` (the S06 slider, 0-150%), read at call time, so a change applies from the next shake and there is no subscription. `ShakeForCrash`, `ShakeForHit` and `ShakeForDepleted` all go through `Shake`. 0% is no jitter at all, 100% is exactly the tuned magnitudes in `camera-feel.md`, and the default is the mockup's 120% (a little stronger than the tuning). Hit-stop is a time effect, not camera motion, so the slider does not scale it.

## Haptics

`Scripts/Haptics/Haptics.cs` (namespace `Opoint8182`, static): `Haptics.Play(HapticTier)`.

**Profiles.** Each `HapticTier` maps to a `HapticProfile` (`HapticProfile.cs`): two parallel arrays, `durationsMs[]` (how long each segment plays) and `amplitudes[]` (its strength, 0 = off / a gap, 1-255). One segment is a plain buzz; a rhythm alternates strong segments with 0-amplitude gaps. This is the shape of Android's `VibrationEffect.createWaveform(timings, amplitudes, -1)`, which is what plays it. The patterns live in `Resources/HapticProfiles.asset` (a `HapticProfiles` ScriptableObject, tune the arrays in the Inspector); built-in defaults are used for any tier the asset lacks, so haptics work before the asset exists. *Tools > Haptics > Create HapticProfiles (keep tuning)* adds missing tiers; *Reset HapticProfiles To Defaults* overwrites them. `Sanitize()` repairs bad data at load (the shorter array wins, durations at least 1 ms, amplitudes 0-255, total capped at 1.5 s).

| Tier | Used for | Default pattern (ms @ amplitude) |
|---|---|---|
| `Light` | every UI press (buttons, tabs, chips, rows, toggles), a heal / pickup, a combo step up | 12 @ 70 |
| `Medium` | a plain crash, a bird / obstacle hit | 30 @ 160 |
| `Heavy` | a perfect crash | 55 @ 255 |
| `CrashRhythm` | the run ending | 60@255, 50 off, 40@190, 50 off, 30@130, 60 off, 160@240 (450 ms: impact, two fading aftershocks, a rumble) |

- **Gate and busy rule:** nothing when `SettingsService.Haptics` is off. While a pattern is running (its total length, at least 40 ms) an equal or weaker tier is skipped, so a UI tap cannot cut the crash rhythm and a crash plus its hit does not stack; a stronger tier replaces it.
- **Platform:** only an Android player vibrates (`#if UNITY_ANDROID && !UNITY_EDITOR`); the Editor, WebGL and everything else are a no-op. The vibrator is fetched once; without amplitude control the non-zero segments use the default strength. Any failure logs one warning and disables vibration, so it can never reach gameplay.
- **`Haptics.Requested`** (static event) fires for every haptic that passes the gate, on every platform: the way to test the wiring in the Editor (subscribe and log).
- **Permission:** `Assets/Plugins/Android/AndroidManifest.xml` is a copy of Unity's `UnityManifest.xml` template plus `VIBRATE` (and `INTERNET`, stated explicitly so the custom manifest cannot lose Unity Gaming Services; Unity's build log notes it was already declared, which is expected), with `PlayerSettings` "Custom Main Manifest" on. When Unity's template changes in a newer editor, re-copy it and re-add the two permissions.

`Scripts/Haptics/GameplayHaptics.cs` sits on the `GameManager` object in `Prototype` next to `GameplaySfx` and listens to the same gameplay events, only while the run is live and not paused: a perfect crash Heavy, other crashes and bird / obstacle hits Medium (a mistimed Tough `Building` raises both `DamageDealt` and `Crashed`, so `Building` sources skip the hit buzz), heal / pickup / combo step up Light, the run ending `CrashRhythm`.

Switching Haptics **on** in Settings or the pause popup buzzes once (Light) so the player feels what they enabled; switching it off does nothing.

**UI presses.** `PressedClassManipulator` plays the click sound and a Light haptic together on pointer-down, so every pressable UI thing gets both; the haptics and invert toggles do the same from their own press handlers.
