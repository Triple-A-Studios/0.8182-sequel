# Settings (Pause, settings & audio, Pass 1)

`Scripts/Settings/SettingsService.cs`, namespace `Opoint8182` (not `.UI`: gameplay reads it too). A static class, so it needs no scene object and works from `Bootstrap` or a standalone scene. It replaced the session-only `UiSettingsState`; property names are unchanged, so the UI code only changed type name.

## Values, ranges, defaults (the mockup's)

| Property | Range | Default | PlayerPrefs key |
|---|---|---|---|
| `Sound` | 0-100 | 80 | `settings.sound` |
| `Haptics` | bool | on | `settings.haptics` |
| `ScreenShake` (percent) | 0-150 | 120 | `settings.shake` |
| `JoystickSide` | Left / Right | Left | `settings.joystick_side` |
| `JoystickSize` | 0-2 (S / M / L) | 1 | `settings.joystick_size` |
| `Sensitivity` | 0.5-1.4 | 0.9 | `settings.sensitivity` |
| `InvertSteering` | bool | off | `settings.invert` |

Local only (`PlayerPrefs`), no cloud, per the design doc. Values load lazily on first access and are clamped; an out-of-range enum falls back to the default.

## Behaviour

- Setters clamp, ignore a set to the current value, mark the store dirty and raise `Changed` (a single `event Action`; later passes subscribe instead of polling).
- **Writing to disk is `Flush()`, not the setter**: sliders fire continuously while dragging. `Flush` is a no-op when nothing changed. It runs from `SettingsScreen` and `HudPausePopup` on a scheduled callback after each pointer release (trickle-down sees the release before the slider snaps; the scheduled call runs after the slider's own handler, so the snapped value is what is saved), on `Application.focusChanged(false)` (covers an Android pause / kill) and on `Application.quitting`. On WebGL `PlayerPrefs.Save()` is what pushes to IndexedDB.
- `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` drops the cache and re-hooks the two Application events, so Editor Play with domain reload off does not keep stale values or stack handlers.

## Readers

`SettingsScreen` (MainMenu scene) and `HudPausePopup` (Prototype scene) edit the values. Both scenes are loaded at once, so the popup re-reads everything in `Show()` (`RefreshFromSettings`); reading only in its constructor would show stale values after a change in the menu. Nothing applies the values to gameplay yet: audio (Pass 4), haptics and screen shake (Pass 5), joystick and invert (Pass 6).
