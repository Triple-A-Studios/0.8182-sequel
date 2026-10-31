# Share to socials (Daily play structure, Pass 8)

Opt-in sharing of a run, a leaderboard result or the player's profile as an image. A tap on a SHARE button renders the matching game screen to a PNG (buttons dropped) and hands it to the platform: the Android share sheet, a browser download on WebGL, a file in `ShareCards/` in the Editor. Sharing is never automatic (design-doc UX rules: no auto-opened share sheet).

Code: `Assets/_Project/Scripts/Share/`, layout `UI/ShareCard.uxml` + `ShareCard.uss`, Android plugin `Assets/Plugins/Android/ShareHelper.java` + `ShareFileProvider.java` (+ the provider entry in `AndroidManifest.xml`), WebGL plugin `Assets/Plugins/WebGL/ShareDownload.jslib`.

## Flow

```
SHARE tap -> ShareCardData (what to show + the one-line text)
          -> ShareService.ShareAsync(data)             one at a time; Busy for a repeat tap
               -> ShareCardRenderer.RenderPngAsync     card layout -> RenderTexture -> PNG bytes
               -> platform backend                     Editor / Android / WebGL
          -> ShareService.ToastFor(result)             "SAVED IMAGE" / "COULDN'T SHARE", nothing for the Android sheet
```

`ShareResult`: `Shared` (the Android sheet opened), `Saved` (a file was written or downloaded), `Failed`, `Busy`. A failure is neutral wording, never an error dialog.

## The card is the game's own screen

A card is not a separate design: `ShareCard.uxml` instances `GameOver.uxml`, `LastWeekResults.uxml` and `Profile.uxml` side by side in one 2400 x 1152 frame (the 2400 x 1080 reference layouts plus a 72 px footer with the game name, a hazard-tape strip and, for a practice run, a PRACTICE RUN tag). `ShareCardView.Apply(data)` shows one of them, fills it through the real view class (`GameOverView`, `LastWeekResultsScreen`, `ProfileScreen`, so the layout, fonts and states are exactly the in-game ones) and drops the buttons. A change to one of those screens therefore changes its card.

| Kind | Screen used | Shows | Buttons / hints dropped |
|---|---|---|---|
| `Run` | Game Over | GAME OVER, the end-cause chip, FINAL SCORE, the four stat chips (the same stand-in values as on screen), NEW BEST! sticker or YOUR BEST chip, the rank chip with the **player's name where the remaining attempts go** | the whole button row (RETRY, MAIN MENU, LEADERBOARD, PRACTICE, SHARE) |
| `Rank` | Last week's results (S11) | top five on the left, the player's finish on the right (rank, "of N pilots · top X%", best score, name line), re-labelled per board | SHARE MY RESULT, CONTINUE |
| `Profile` | Profile (S08) | name, stats, equipped plane / FX, badges | back, SHARE, rename, the dev caption in the portrait slot, the "tap a badge" line, the badge scroll bar |

**Run card rules.** The rank chip appears only for a ranked competitive run (the weekly rank the submit returned); an unranked or practice run has no chip, like the screen. A practice run never carries a rank. NEW BEST! / YOUR BEST: see [Best score](#best-score-new-best--your-best).

**Rank card boards** (`ShareBoard`), same layout, different words (`ShareCardData.RankTitle`, `RankRange`, `RankTag`, `RankKicker`, `RankCaption`):

| Board | Title | Header chip | Card kicker | Caption |
|---|---|---|---|---|
| `LastWeek` (launch results) | LAST WEEK'S RESULTS | `SEP 29 – OCT 5 · FINAL` | YOU FINISHED | name · best run of the week |
| `ClosedWeek` (an earlier week browsed on the leaderboard) | WEEKLY RESULTS | range · FINAL | YOU FINISHED | name · best run of the week |
| `ThisWeek` | THIS WEEK'S LEADERBOARD | range · LIVE | YOUR RANK | name · best run this week |
| `AllTime` | ALL-TIME LEADERBOARD | ALL-TIME | YOUR RANK | name · all-time best |

The leaderboard's data comes from the board on show: `LeaderboardScreen.TryBuildShareCard` keeps the last successful `TopScoresResult` and turns it into a `LastWeekResult` with `LeaderboardLastWeekResultsSource.Build(range, read)` (top five rows, the player's own row, pilots from the board total, top percent rounded up). No card when there is no read or the player has no row on it (the SHARE MY RANK button is hidden in that case anyway).

## Share text

One line per card, no link (a store page can be added later in one place, `ShareCardData.Text`). Only glyphs the fonts bake in.

| Card | Text |
|---|---|
| Run, ranked | I scored 184,920 in Opoint8182 and I'm #7 this week! |
| Run, unranked | I scored 96,310 in Opoint8182! |
| Run, practice | I scored 51,240 in a practice run on Opoint8182! |
| Rank, this week / all-time | I'm #7 on this week's Opoint8182 board! / I'm #248 on the all-time Opoint8182 board! |
| Rank, last week | I finished #31 on last week's Opoint8182 board! |
| Rank, earlier week | I finished #31 on the Opoint8182 weekly board (Sep 29 – Oct 5)! |
| Rank, no row | Check out the Opoint8182 leaderboard! |
| Profile | My pilot profile in Opoint8182! |

The text goes to the Android share sheet only. The WebGL download and the Editor file carry the image alone; the file names are `opoint8182-score.png`, `-rank.png`, `-profile.png` (the browser numbers repeats).

## Rendering (`ShareCardRenderer`)

Play mode only (a panel needs the player loop), one render at a time (a second call returns null).

1. Load `Resources/ShareCardAssets` (`ShareCardAssets`: the card layout and a `PanelSettings` template, the shared `FuelGaugePanelSettings`).
2. Copy the settings (`Object.Instantiate`), point `targetTexture` at a new 2400 x 1152 `RenderTexture` (ARGB32, sRGB), scale mode Scale With Screen Size at 2400 x 1152 (so scale 1), opaque clear colour.
3. A hidden `GameObject` with a `PanelRenderer` on those settings and the card layout; wait for the UI reload callback (up to 30 frames), build `ShareCardView`, `Apply`, wait 3 frames and the end of frame.
4. `ReadPixels` into a `Texture2D`, `EncodeToPNG`, destroy everything (object, settings copy, texture, render texture).

Things this depends on, learnt while building it:
- **Safe area.** `GameOverView` insets by the device safe area; a notch would shift the card, so its constructor takes `applySafeArea` (false for the card). The flow screens sit in a 24 px padded stage like the menu's safe area.
- **No scene camera.** An offscreen panel has no 3D behind it: the Profile portrait and thumbnail slots are empty rectangles in the image (revisit in the 3D art pass: the real portrait is a camera render in the slot). The portrait dev caption is hidden in the card.
- **Panel sharing.** The card panel is its own runtime panel (its own `PanelSettings` instance), so it does not pick pointer events or draw over the game.
- **Cost.** A render is a few frames; on WebGL the readback and PNG encode of a 2400 x 1152 texture is the heaviest part (a short hitch on tap, accepted; it worked on desktop and Android browsers).

## Platform backends

| Platform | Backend | Result |
|---|---|---|
| Editor | `ShareCardEditorSave`: writes `ShareCards/share_<kind>.png` next to the project (git-ignored) | `Saved` |
| Android | `AndroidShareSheet` writes the PNG into `<cache>/share/` and calls `ShareHelper.share` (an `ACTION_SEND` chooser with the image and the text) | `Shared` |
| WebGL | `WebGlShareDownload` -> `ShareDownload.jslib`: copies the PNG out of the wasm heap, makes a blob URL, clicks a temporary `<a download>` | `Saved` |

**Android needs no permission.** The image is a file in the app's cache folder served through `ShareFileProvider`, a small read-only `ContentProvider` of our own (declared in the custom `AndroidManifest.xml` as `${applicationId}.share`, `exported="false"`, `grantUriPermissions="true"`). Access is the URI grant the share intent carries, and the provider only serves files directly inside `<cache>/share/`. It is our own class instead of androidx `FileProvider` so the build needs no extra library. Nothing is saved to the gallery on purpose: saving would need a storage permission on Android 8-9 (min SDK 26), and the share sheet offers Save itself. The Java is compile-checked with Unity's JDK against `android.jar`; the manifest placeholder and the plugin import were proved on a real build.

**WebGL downloads, it does not open a share sheet.** The Web Share API is uneven across browsers (file sharing especially), so the player gets the image and posts it themselves. The download starts from the tap's activation (a render takes well under the browser's activation window).

## Best score (NEW BEST! / YOUR BEST)

Decided with the developer in 8d: the run card and the real Game Over screen use the same data. At run end `GameOverUI` compares the score with `LeaderboardService.StoredAllTimeBest` (the all-time best from the last submit reply, in PlayerPrefs, read **before** the submit updates it). A competitive run above it shows NEW BEST!; otherwise YOUR BEST shows that best (hidden when there is none). A practice run never counts as a new best. It is as soft as the stored best: a cleared device shows NEW BEST! once. The old placeholder constant is gone; the full "Player profile & run stats" milestone will own personal-best tracking.

## Entry points

| Button | Owner | Card |
|---|---|---|
| Game Over SHARE (`go-share`; practice keeps its PRACTICE tag on the button) | `GameOverUI.ShareRun` | `Run` |
| Leaderboard SHARE MY RANK (`lb-share`) | `MainMenuController.OnLeaderboardShare` via `LeaderboardScreen.TryBuildShareCard` | `Rank` (this week, earlier week, all-time) |
| Last week's results SHARE MY RESULT (`lw-share`) | `LastWeekResultsScreen.OnShare` | `Rank` (`LastWeek`) |
| Profile SHARE (`profile-share`, top right of the header; added in Pass 8) | `MainMenuController.OnProfileShare` | `Profile` |

## Testing

- `X` in the GameOverGallery renders all eight sample cards (`ShareCardSamples.All()`) into `ShareCards/`; the SHARE buttons in the Game Over, menu (leaderboard, Profile) and launch galleries share a sample card for real.
- The pure parts (text, board labels, file names, the board-read to result conversion) are checked by eval in the Editor; the render by looking at the PNGs.
- Device checks (Android sheet, WebGL download on desktop and phone) were done by the developer. The Last week's results button itself was not exercised on a device (it runs the same code as the others).

## Open / later

- A game link in the share text (one place, `ShareCardData.Text`).
- 3D portrait and thumbnails in the Profile card: the 3D art pass.
- Real stat chips on the run card follow the Game Over chips (Player profile & run stats); the player name is the session display name until the profile is persisted.
- Optional: save to the gallery on Android (needs a storage permission on Android 8-9), a Web Share API path on WebGL.
