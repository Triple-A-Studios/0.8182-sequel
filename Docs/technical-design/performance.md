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
- **Open and close:** F9, or three fingers down at once on a touch screen (re-armed when all are lifted); HIDE closes it. It samples while hidden, so a run's summary is never lost.
- **Live block:** fps over the last second; average and worst frame ms over the last 300 frames; for the current run, hitch counts (frames over 33, 50 and 100 ms) and stalls (frames over 2 s, ignored as scene loads or the app in the background); draw calls, SetPass calls, triangles and vertices; total and GC memory; the frame-rate cap and the display's refresh rate. A run counts only frames flown (playing, not paused).
- **Run summary:** when a run ends it logs `[Perf] run end: <cause> · <seconds> · avg <fps> · 1% low <fps> · worst <ms> · hitches ... · mem <change> MB · cap <n>` (logcat on Android, the browser console on WebGL) and keeps the last 4 on screen, so a phone can be read without a cable. The 1 % low is the average fps of the slowest 1 % of frames.
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

## Results
To be filled in from the device runs (5e).

| Target | Cap | Avg fps | 1 % low | Hitches over 50 ms | Draw calls / triangles | Memory | Notes |
|---|---|---|---|---|---|---|---|
| Android | 60 | | | | | | |
| Android | 61 | | | | | | |
| WebGL (phone browser) | -1 | | | | | | |

## Levers, if the numbers miss
Ranked by expected gain per effort; each is scoped as its own pass from the data, not applied in advance.
1. No shadow casting on buildings and obstacles (they are most of the shadow-pass triangles).
2. Shadow distance 50 to about 30.
3. HDR off on the `Mobile` asset; render scale 0.8 to 0.7.
4. Building triangles 25k to about 10k with `Tools/decimate_building_mesh.py` (the pack's windows tear below about 25k, so this needs a look), and 5 material slots to 1 through an atlas, the method of the ground dressing.
5. Fewer ground tiles and a shorter fog end; dressing off on weak phones.
6. Bird animation cost (3 animated birds were visible in the dense frames).
