# Audio (Pause, settings & audio, Pass 4)

Sound effects only: **the game has no music or BGM, by design.** There is also no `AudioMixer`: a mixer asset cannot be created from code, and priority / ducking between a handful of SFX is plain logic. The pooled sources could be routed to a mixer group later (one line each) if effects such as a low-pass on pause are ever wanted.

## Pieces (`Scripts/Audio/`)

- **`SfxId`**: every sound the game plays. The name is the clip file name (minus a trailing `_1` / `_2` variation suffix).
- **`SfxLibrary`** (ScriptableObject, one asset at `Resources/SfxLibrary.asset`): per id, the clips (variations, one picked at random) and how to play them: volume, pitch range, priority, cooldown, max concurrent, loop, ignore-pause, duck amount / seconds. **Never hand-assembled**: Editor menu *Tools > Audio > Rebuild SfxLibrary* (`Scripts/Editor/SfxLibraryBuilder.cs`) maps clip file names under `Assets/_Project/Audio` to ids. An existing entry keeps its tuning and only refreshes its clips; an id with no entry gets `SfxDefaults`. *Reset SfxLibrary Tuning To Defaults* re-applies the code defaults. Run the rebuild after adding, renaming or replacing a clip, or after adding an `SfxId`.
- **`SfxDefaults`**: the starting tuning per id (priority bands: loops 10, UI 20-30, feedback 25-40, hits 50, warnings 55-75, crashes 60-70, run end 90, game over 95).
- **`AudioManager`**: `DontDestroyOnLoad` singleton created before the first scene (`RuntimeInitializeOnLoadMethod`), so it exists from Bootstrap, a standalone scene, the dev galleries and across a Restart. Pool of 8 sources; voice stealing (a new sound takes a free voice, else the lowest-priority voice at or below its own priority, oldest first, else it is dropped); per-id cooldown and concurrency cap; ducking (a sound with `duckAmount` < 1 scales lower-priority voices for `duckSeconds`: fast attack, 3/s recovery, a per-source multiplier); looped sounds (boost) get their own source each.
- **`Sfx`**: the static call site: `Sfx.Play(SfxId)`, `StartLoop` / `StopLoop`, `StopAll`, `SetPaused`. Safe anywhere; a no-op with no manager.

## Volume, pause

- Master volume is `AudioListener.volume = (Sound / 100)^2` from `SettingsService.Sound`, applied at start and live on `SettingsService.Changed`. Sound 0 is silent (the persisted setting carries over between runs, so a test run after setting 0 is silent).
- Pause: `GameManager` calls `Sfx.SetPaused` from `RaisePaused` / `RaiseResumed` (`AudioListener.pause`); `GameManager.Awake` and `RestartRun` clear it, and `RestartRun` also `StopAll()`s. Entries marked ignore-pause (UI and countdown) keep playing while paused, and `StopAll()` leaves them alone (the click that triggered a restart must finish); game over is not ignore-pause (it only plays after the run ended, when pause is unavailable).

## Where sounds are triggered (4a)

`UiClick` is played by `PressedClassManipulator` **on pointer down** (a click event only arrives on release, which read as a delay): every pressable UI thing carries that manipulator (`LipButton`, `TabBar` tabs, the menu chips / tiles / overflow rows / Play, profile badges), so new ones are covered for free; a disabled target is silent, and the boost button opts out (`playsSound: false`). The haptics and invert toggles play `UiToggle` on press too (a `Toggle` is not pressable by that manipulator), a tick on release of the Sound slider in Settings and the pause popup (`UiSliderTick`, after the slider snaps, so the tick is at the saved level), and the resume countdown (`CountdownTick` per number, `CountdownGo` at zero). Gameplay sounds are 4b.

## Placeholder clips

`Tools/generate_placeholder_sfx.py` (stdlib only, deterministic) writes about 26 mono 16-bit 22.05 kHz WAVs (about 0.5 MB total) into `Assets/_Project/Audio/Placeholder/`; they are committed. To use real audio, drop a file with the id's name (for example `CrashPerfect.wav`) anywhere under `Assets/_Project/Audio` **outside** `Placeholder`: it wins over the placeholder of the same id after a library rebuild. No code change.
