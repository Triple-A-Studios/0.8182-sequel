# World environment (3D art pass, Pass 4)

The ground, sky, light and fog of the game scene. Built in the "3D art pass" milestone; the plane, building, obstacle and pickup art is in [planes.md](planes.md), [building-crash-system.md](building-crash-system.md) and [obstacle-system.md](obstacle-system.md).

## What is in the scene

| Piece | Where | Notes |
|---|---|---|
| Endless ground | `GroundTiler` (`Scripts/World`) on the `GroundTiler` object in `Prototype.unity` | 8 tiles recycled along Z: 2 behind the plane, 5 ahead, the one under it |
| Ground tile | `Prefabs/World/GroundTile.prefab` + `GroundTile_Mesh.asset` | One mesh, one renderer, one material (`Simple Lit`), 7,440 vertices, 4,190 triangles, 300 x 60 x 0.2, no collider, no shadow casting (receives shadows) |
| Skirt | `GroundSkirt` quad under `GroundTiler`, material `Materials/Mat_GroundSkirt` | 4000 x 4000, y -0.05, unlit in the fog colour; follows the plane in X and Z; hides the skybox below the horizon and the far edge of the last tile |
| Sky | `Art/QMS - Cartoon Skybox Pack FREE/Materials/Sky0008_Day.mat`, set as the scene skybox | 2048 x 1024 panorama, no mips |
| Sun | `Directional Light` | Colour (1, 0.96, 0.88), intensity 1.0, soft shadows, strength 0.8, set as the scene sun; ambient from the skybox |
| Fog | Scene render settings | Linear, start 80, end 250, colour (0.45, 0.80, 0.98), matched to the sky horizon |

## Decisions of record
- **The ground is visual only.** The tiles have no colliders. The old 100 x 100 `Ground` (a mesh collider that the run left behind after about 55 units) was removed. A run ends on the deck through `AltitudeSystem`: `m_groundHardY` is now `0.5` (was `-1`), so it ends when the plane touches the visible ground (tile top at y 0.2, capsule radius 0.5). This is the one gameplay change of the pass. See [altitude-system.md](altitude-system.md).
- **The pack's `City.prefab` is not used as is.** It is 300 x 260, 702 renderers, 140k triangles and 44 materials, with buildings up to 25 high: too heavy to recycle on a phone, and tall decorative buildings in the flight lane (spawner x range is +/-10) would look crashable but be ghosts. Decorative buildings may only appear outside the lane; that is the planned side dressing (not scoped).
- **Tile pitch is 56.6, not 60.** The tile has a white kerb along both Z edges; at a pitch of 60 two neighbours make a double-width kerb stripe, at 56.6 their kerbs overlap and a seam reads as the kerb between a road and a lot. The overlapped faces are the same colour and height, so there is no visible z-fight.
- **Tiles are placed by slot, not shuffled.** Tile k always lives in slot `k mod count`, so each frame only the tiles that left the window move. `GroundTiler` starts with tile 0 centred under the plane and instantiates all tiles once (no instantiate or destroy during a run).
- **Fog hides the end of the world.** Fog end (250) is inside the tile window (5 x 56.6 = 283 ahead), and the skirt covers the part of the skybox below the horizon. Fog is in the scene's render settings, so it only applies while the game scene is the active scene: `BootstrapLoader` sets it active in the real flow; `MainMenu` run on its own keeps its own (default) settings.

## Rebuilding the tile
`Tools > World > Build Ground Tile` merges `Prefabs/Misc/Ground.prefab` (the pack's 45 road and lot pieces) into `GroundTile_Mesh.asset` and rewrites `GroundTile.prefab`, in place so references stay valid. Run it again after changing `Ground.prefab`.

## Budget
Per frame: 8 tiles = 8 draw calls, about 33k triangles (most off screen), one material, shadow receive only. No per-frame allocation. Not yet measured on a phone: the Android / WebGL performance check for Passes 1 to 4 is still to do.
