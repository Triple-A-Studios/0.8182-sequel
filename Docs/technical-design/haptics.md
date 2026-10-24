# Haptics and screen shake (Pause, settings & audio, Pass 5)

## Screen shake slider

`CameraJuice.Shake(magnitude, duration)` multiplies the magnitude by `SettingsService.ScreenShake / 100` (the S06 slider, 0-150%), read at call time, so a change applies from the next shake and there is no subscription. `ShakeForCrash`, `ShakeForHit` and `ShakeForDepleted` all go through `Shake`. 0% is no jitter at all, 100% is exactly the tuned magnitudes in `camera-feel.md`, and the default is the mockup's 120% (a little stronger than the tuning). Hit-stop is a time effect, not camera motion, so the slider does not scale it.

## Haptics

`Scripts/Haptics/Haptics.cs` (namespace `Opoint8182`, static): `Haptics.Play(HapticTier.Light | Medium | Heavy)`.

- **Gate:** nothing when `SettingsService.Haptics` is off. A 0.04 s cooldown stops a crash plus its hit from stacking buzzes; a stronger tier than the last one still cuts through it.
- **Platform:** only an Android player vibrates (`#if UNITY_ANDROID && !UNITY_EDITOR`). The Editor, WebGL and everything else are a no-op. The Android path is `VibrationEffect.createOneShot(duration, amplitude)` through `AndroidJavaObject` (min SDK 26): Light 15 ms / 80, Medium 30 / 160, Heavy 55 / 255; without amplitude control it uses the default amplitude. The vibrator is fetched once; any failure logs one warning and disables vibration, so it can never reach gameplay.
- **`Haptics.Requested`** (static event) fires for every haptic that passes the setting and the cooldown, on every platform: the way to test the wiring in the Editor (subscribe and log).
- **Permission:** `Assets/Plugins/Android/AndroidManifest.xml` is a copy of Unity's `UnityManifest.xml` template plus `VIBRATE` (and `INTERNET`, stated explicitly so the custom manifest cannot lose Unity Gaming Services), with `PlayerSettings` "Custom Main Manifest" on. When Unity's template changes in a newer editor, re-copy it and re-add the two permissions. Not yet built or checked on a device (Pass 7).

`Scripts/Haptics/GameplayHaptics.cs` sits on the `GameManager` object in `Prototype` next to `GameplaySfx` and listens to the same gameplay events, only while the run is live and not paused: perfect crash and run end Heavy, other crashes and bird / obstacle hits Medium (a mistimed Tough `Building` raises both `DamageDealt` and `Crashed`, so `Building` sources skip the hit buzz), heal / pickup / combo step up Light.

Switching Haptics **on** in Settings or the pause popup buzzes once (Light) so the player feels what they enabled; switching it off does nothing.
