# Performance (3D art pass, Pass 5)

Budgets, how the game is measured, and the results. The scope of the check is in [plan.md](../plan.md#milestone-3d-art-pass) (Pass 5).

## Targets and settings
- **Devices:** the developer's Android phone (a development APK) and WebGL in the phone browser. Both use the `Mobile` quality tier.
- **Target frame rate:** a steady 60. Proposed pass line (to confirm): on the phone an average of at least 55 fps and a 1 % low of at least 45; in the phone browser an average of at least 50; no hitch over 100 ms outside scene loads; memory flat across retries; no visible slowdown over 10 minutes.
- **`Mobile` tier (`Mobile_RPAsset`):** HDR on, render scale 0.8, MSAA off, SRP Batcher on, dynamic batching off, one shadow cascade at 1024, shadow distance 50, no soft shadows. The `PC` tier (Editor) has 4 cascades at 2048, render scale 1 and soft shadows.
- **Frame rate setup** (`Scripts/Common/FrameRateSetup`): Android only. `Application.targetFrameRate` is set to 60 before the first scene loads (nothing set it before, so Android ran at its default of 30). WebGL is left at -1 on purpose: the browser's `requestAnimationFrame` paces it, and any other value switches WebGL to a timer loop. The Editor and desktop builds are untouched.
  - Vsync is 0 on both tiers, so a software limiter paces the frame, and a limiter at exactly 60 can race the display's vblank. Development builds therefore read an override from `PlayerPrefs` (`FrameRateSetup.SetDevOverride`, to be wired into the perf overlay) to try 61 on the phone without a rebuild. Release builds always use 60.
  - **Optimized Frame Pacing** (Player Settings > Android) is currently **off**. It is part of the experiment: measure 60 and 61 with it on and off, and keep whichever is smoothest on the phone.

## Perf overlay (5b)
Development builds and the Editor only (`Debug.isDebugBuild`; a release build deactivates it). Code in `Scripts/Perf` (`PerfSampler`: frame-time maths; `PerfOverlay`: the panel), layout in `UI/Dev/PerfOverlay.uxml / .uss`, placed as a child of the `Bootstrap` object so it survives scene reloads.
- **Open and close:** F9 in the Editor; on a device type `perf_on` into the name field (Menu > Profile > pencil, then confirm). The name does not change and the line under it says `DEV: PERF OVERLAY ON`. It stays open across scenes until HIDE or `perf_off`, or the app closes. (Three fingers was the first idea but Android takes it as the screenshot gesture.) It samples while hidden, so a run's summary is never lost.
- **Live block:** fps over the last second; average and worst frame ms over the last 300 frames; for the current run, hitch counts (frames over 33, 50 and 100 ms) and stalls (frames over 2 s, ignored as scene loads or the app in the background); draw calls, SetPass calls, triangles and vertices; total and GC memory; the frame-rate cap and the display's refresh rate. A run counts only frames flown (playing, not paused).
- **Run summary:** when a run ends it logs `[Perf] run end: <cause> · <seconds> · avg <fps> · 1% low <fps> · worst <ms> · hitches ... · mem <change> MB · cap <n>` (logcat on Android, the browser console on WebGL) and keeps the last 4 on screen, so a phone can be read without a cable. The 1 % low is the average fps of the slowest 1 % of frames.
- **Hidden commands** (`Scripts/Perf/DevCommands`, run from `ProfileScreen.ConfirmEdit` before the name is checked, development builds only; in a release build they are ordinary names): `perf_on`, `perf_off`, `cap_60`, `cap_61`, `cap_30`, `cap_free` (-1, the platform default) and `cap_reset` (the project default, 60 on Android). Case does not matter. A cap command also shows `DEV: CAP <n>` on the ID line and is kept for the next launch.
- **CAP button:** cycles 60, 61, 30 and -1 through `FrameRateSetup.SetDevOverride`, kept for the next launch (development builds). On WebGL any value other than -1 switches the loop to a timer, so use it there only to compare.
- **Render counters** come from `ProfilerRecorder` (`Draw Calls Count`, `SetPass Calls Count`, `Triangles Count`, `Vertices Count`) and read empty in the Editor; if they are also empty on a device the overlay says so and the Unity Profiler over USB is the fallback (Android).

## Editor baseline (5c)
Taken 2026-11-24 in the Editor, Play mode in `Prototype`, quality level `Mobile`, Game view 833 x 441 (the counts do not depend on resolution; the fill cost does, and the phone renders about 2400 x 1080 at scale 0.8). `UnityEditor.UnityStats`, last rendered frame. The "dense" rows come from teleporting the plane to a distance of about 3,400 (the difficulty ramp is at full density past 3,000) at height 24, above every collider, and reading the stats over the next 2 seconds as the plane flew on.

| Moment | Draw calls | SRP Batcher | SetPass | Triangles | Vertices | Shadow casters |
|---|---|---|---|---|---|---|
| Run start (distance about 140, 3 buildings and 1 obstacle in view) | 87 | 61 | 28 | 230k | 472k | 16 |
| Dense, start | 92 | 61 | 29 | 234k | 482k | 15 |
| Dense, 3 to 5 buildings in view | 94 to 98 | 61 to 63 | 27 to 29 | 235k to 252k | 482k to 520k | 14 |
| **Dense, worst frame seen** (6 buildings, 2 birds, 1 obstacle) | **113** | 76 | 28 | **303k** | **626k** | 20 |
| Dense, light frame | 95 | 52 | 27 | 200k | 414k | 14 |

What the numbers say:
- **Buildings dominate.** One building or obstacle is 25.8k triangles and 42.8k vertices in 5 material slots (5 draw calls, plus 1 for the weak-point disc), and it is drawn again in the shadow pass while inside the shadow distance. That is the 30k to 50k triangles per building in view; a frame with 6 of them is 300k triangles and 600k vertices.
- **The environment is cheap:** 8 ground tiles are 8 draw calls and about 4k triangles each, the dressing one more draw call and about 4k triangles each, the skirt one quad. The plane is 4 renderers, birds 134 triangles, pickups 76.
- Draw calls stay between about 87 and 113; SetPass calls stay near 28 (few materials). Both are modest. The risk is vertex and triangle load on a weak GPU, not draw calls.
- Not measured here: GPU time, fill rate, memory and thermals. Those need the phone.

## Device protocol (5d builds, 5e runs)

**Builds (5d)**
- Android: Development Build on, Optimized Frame Pacing **off** for the first round (it needs its own build to compare).
- WebGL: Development Build on, Brotli, no decompression fallback (as for the itch.io draft page). Restore `Data/Plugins/lib_burst_generated.wasm` with `git checkout` afterwards. Note the build folder size.
- Both: use **PRACTICE** runs (competitive attempts are limited and practice needs no server). Practice time is a local soft cap of **30 min per day per device / browser**, so plan the session: the full protocol (about 20 min) once per target, and only short 3 minute runs for the extra cap trials.
- Before starting: note the device model, OS and browser; battery over 50 %, power saver off, fixed brightness, phone at room temperature, other apps closed.

**Per target (Android, then WebGL)**
1. **Cold start.** Stopwatch from the tap on the icon (or the page load) to the menu showing. Do it twice: first launch, then relaunch. Note the WebGL page load too.
2. **Overlay check on the menu.** Open it: Profile > pencil > type `perf_on` > confirm. Read DISPLAY Hz and CAP, and note whether the DRAW / SETPASS / TRI / VERT line shows numbers or the "no render counters" message.
3. **Three runs.** Practice, flown the way you normally play (crashes into buildings are part of the load); at least one run past **3,000 m** on the Game Over DISTANCE chip. After each run, screenshot the overlay (it keeps the last 4 summaries). In one dense moment press pause and screenshot the overlay for DRAW / TRI.
4. **Five retries.** From Game Over press RETRY five times in a row (short runs are fine). Note any freeze on a reload and the overlay MEM and GC values before the first and after the fifth.
5. **Ten minutes.** Chain runs back to back for 10 minutes. At 0, 5 and 10 minutes note the live FPS, MEM, how warm the phone is, and the battery drop.
6. **Cap trial (Android only).** With the CAP button, one 3 minute run each at 60 and at 61, then, if you want, a build with Optimized Frame Pacing on at 60 and 61. Keep the one with the best 1 % low and fewest hitches. On WebGL leave CAP at -1.

**While playing, look for** (a quick eye test, not a number): stutter at the moment of a crash, a stall when a building first appears, shadow flicker, seams between ground tiles, fog colour against the sky, bird animation, the red obstacle against the buildings.

**Report back** (copy the overlay text or a screenshot per run):
- Device, OS / browser, refresh Hz, build type, Optimized Frame Pacing on or off, CAP.
- Cold start seconds (first and second); WebGL load time and build size.
- Render counters: shown or not; DRAW / SETPASS / TRI / VERT at the densest moment seen.
- Each run's summary line: average fps, 1 % low, worst ms, hitches over 33 / 50 / 100 ms, memory change.
- Retries: MEM and GC before and after; any freeze.
- 10 minutes: FPS, MEM, warmth and battery at 0 / 5 / 10 minutes.
- Cap trial: average and 1 % low at 60 and 61.
- Anything you saw that looked wrong.

## Results
Run by the developer on 2026-11-28, development builds (Android APK and WebGL), transcribed from overlay screenshots. Optimized Frame Pacing off. A -1 cap on Android was not run. The 61 trial was skipped in the first session and run afterwards (below); the totals and per-run tables are from the first session.

**Setup:** realme 8s 5G; the overlay reads a 60 Hz display. WebGL ran on the same phone in Brave with about 6 tabs open.

**Totals** (averages weighted by run length):

| Target | Cap | Runs / time | Avg fps | 1 % low (range) | Worst frame | Frames over 50 ms | Frames over 100 ms |
|---|---|---|---|---|---|---|---|
| Android | 60 | 4 / 210 s | 49.4 | 25.6 to 29.9 | 67 ms | 11 (about 3 a minute) | 0 |
| Android | 30 | 3 / 232 s | 29.9 | 27.8 to 29.8 | 67 ms | 3 (under 1 a minute) | 0 |
| WebGL | -1 | 5 / 316 s | 52.1 | 13.3 to 22.3 | 206 ms | 88 (about 17 a minute) | 4 |

**Per run** (hitch columns are frames over 33 / 50 / 100 ms; memory is the change over the run):

| Target, cap | Seconds | Avg | 1 % low | Worst ms | Hitches | Memory MB |
|---|---|---|---|---|---|---|
| Android 60 | 80 | 49.2 | 27.5 | 67 | 822 / 6 / 0 | +3.2 |
| Android 60 | 69 | 50.1 | 28.6 | 67 | 664 / 2 / 0 | +2.2 |
| Android 60 | 24 | 49.4 | 25.6 | 67 | 240 / 3 / 0 | +0.6 |
| Android 60 | 37 | 48.4 | 29.9 | 34 | 410 / 0 / 0 | 0.0 |
| Android 30 | 89 | 29.9 | 28.2 | 67 | 2653 / 2 / 0 | +0.1 |
| Android 30 | 97 | 29.9 | 29.8 | 34 | 2891 / 0 / 0 | +0.2 |
| Android 30 | 46 | 29.9 | 27.8 | 50 | 1358 / 1 / 0 | +0.1 |
| WebGL -1 (first run) | 42 | 47.3 | 13.3 | 206 | 148 / 27 / 2 | +1.1 |
| WebGL -1 | 77 | 52.7 | 17.9 | 105 | 227 / 23 / 1 | -0.5 |
| WebGL -1 (competitive) | 11 | 53.6 | 22.3 | 51 | 34 / 2 / 0 | -8.7 |
| WebGL -1 | 86 | 52.8 | 21.0 | 63 | 221 / 11 / 0 | +0.1 |
| WebGL -1 | 100 | 52.9 | 19.3 | 114 | 301 / 25 / 1 | +0.2 |

**61 cap trial** (Android, Optimized Frame Pacing off; a 60 and a 61 run done for the comparison, both past 3,000 m, both `Wrecked`):

| Cap | Seconds | Distance | Avg | 1 % low | Worst ms | Hitches (33 / 50 / 100) | Share over 33 ms | Over 50 ms per minute | Memory MB |
|---|---|---|---|---|---|---|---|---|---|
| 60 | 224 | 6.6 km | 50.1 | 24.8 | 100 | 2105 / 32 / 1 | about 19 % | 8.6 | -0.1 |
| 61 | 156 | 4.6 km | 50.1 | 26.7 | 67 | 1489 / 16 / 0 | about 19 % | 6.2 | +0.1 |

**61 makes no difference:** same average and same share of two-refresh frames. The small gaps in 1 % low, worst frame and 50 ms hitches are not evidence either way, since the 60 run was longer and flew further. The project stays at 60.

**Other readings**

| | Android | WebGL |
|---|---|---|
| Cold start to menu | 6 s | 20 s (a new build's first launch: includes itch serving the whole game and Unity caching it, so it is not a pure start-up time) |
| Restart | 5 s | 8 s |
| Memory in the overlay (profiler total / GC) | 150 to 153 MB / 14 to 15 MB | 121 to 122 MB / 7 to 8 MB |
| 15 minutes non-stop | played through; Android's battery page showed about 0.86 % | played through; battery use could not be separated from earlier browsing |
| Heat | less | more than the APK |

**Against the proposed pass line**

| Criterion | Android (cap 60) | WebGL |
|---|---|---|
| Average (55 phone, 50 browser) | **Missed** (49.4) | Met (52.1; the first run, 47.3, was under) |
| 1 % low at least 45 (phone) | **Missed** (25.6 to 29.9) | not a criterion (13.3 to 22.3) |
| No frame over 100 ms outside loads | Met | **Missed** (4 frames, worst 206 ms) |
| Memory flat | Met (at most +3.2 MB in a run, no upward trend) | Met |
| Retries and 10 minute slowdown | not recorded as numbers | not recorded as numbers |

**What the numbers show**
- **Android at 60 is mostly a one-refresh miss, not long stalls.** About 1 frame in 5 took over 33 ms (19 to 23 % in all four runs), while only 11 frames passed 50 ms. The averages fit that exactly: 4 frames in 5 at 16.7 ms and 1 in 5 at 33.3 ms give about 49 to 50 fps, and the live "max" reads 33.4 to 33.9 ms.
- **Android at 30 is steady**: 29.9 average, 1 % low 27.8 to 29.8. A 30 fps cap is a working fallback.
- **WebGL averages higher but is spikier**: about 5 to 7 % of frames over 33 ms against about 20 % on Android, but far more frames over 50 ms and the only ones over 100 ms. The first run was the worst (206 ms). A warm-up cost (shaders or assets on first use) is a likely reason, not verified; the open tabs may add to it.
- **Not decided by this data:** whether Android misses 60 because frames are really over budget or because of frame pacing (a software limiter at 60 with vsync off and Optimized Frame Pacing off). WebGL beating the native build on average on the same phone makes pacing worth testing, but the two may render at different resolutions (not measured). The 61 trial changed nothing, so the exact-60 limiter value is not the fix; a build with Optimized Frame Pacing on is the pacing test still open.
- **Not measured:** CPU against GPU time, draw calls and triangles (the render counters were empty on both the Android and the WebGL development build), and the browser's own memory.
- **Overlay flaw:** the "over 33 ms" counter uses 33.3 ms, which is also a normal frame at a cap of 30 (and a normal two-refresh frame at 60), so at cap 30 it counts nearly every frame. Read the 50 and 100 ms counters; move the first threshold to about 35 ms if the overlay is touched again.

**Verdict (developer):** the game is playable. A few more people will play test for general feel and hiccups (no numbers). Optimisation is deferred to the Beta phase or Full Launch: backlog #40 in [progress.md](../progress.md#backlog).

## Levers, if the numbers miss
Ranked by expected gain per effort; each is scoped as its own pass from the data, not applied in advance.
1. No shadow casting on buildings and obstacles (they are most of the shadow-pass triangles).
2. Shadow distance 50 to about 30.
3. HDR off on the `Mobile` asset; render scale 0.8 to 0.7.
4. Building triangles 25k to about 10k with `Tools/decimate_building_mesh.py` (the pack's windows tear below about 25k, so this needs a look), and 5 material slots to 1 through an atlas, the method of the ground dressing.
5. Fewer ground tiles and a shorter fog end; dressing off on weak phones.
6. Bird animation cost (3 animated birds were visible in the dense frames).
