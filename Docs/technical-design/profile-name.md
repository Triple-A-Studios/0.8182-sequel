# Display name (Player profile & run stats, Pass 4)

`Scripts/UI/Profile/UiProfileState.cs` (the name, its storage and the sync) and `DisplayNameRules.cs` (what a name may be), namespace `Opoint8182.UI`. The Profile screen edits it ([ui.md](ui.md#profile-s08)); the menu chip, the share cards and, once synced, the leaderboards show it.

## Rules (`DisplayNameRules`)

- ASCII letters, digits, `_` and `-` only: Unity Authentication refuses spaces, and the UI fonts bake a known glyph set. `Sanitize` drops every other character (spaces included) and cuts to 16.
- 3 to 16 characters (`MIN_LENGTH` 3 is our own floor; `MAX_LENGTH` 16 is the old length cap). Nothing typed at all means "keep the old name", not an error.
- Not on the blocklist (`m_S_Blocked` in `DisplayNameRules.cs`, lowercase letters only; to add a word, append it to that array): a substring check on the normalized name (lowercase, `0 1 3 4 5 7` read as `o i/l e a s t`, digits, `_` and `-` dropped), so padding a word does not dodge it. It is a **starting list** of about 30 words (`m_S_Blocked`); words that sit inside innocent ones ("spic", "coon") are left out, and the odd false positive (the classic "Scunthorpe") is accepted. It is client-side only: cheap and offline, but a modified client can skip it. A server-side check (a Cloud Code script that sets the name) is the later option if the boards need it.
- A refused name keeps the Profile edit open and shows a neutral line on the ID line ("Use at least 3 letters or numbers", "That name isn't available, try another"). The dice always produces a valid name (checked against the list for every adjective / plane / number combination).

## Storage (`UiProfileState`)

`PlayerPrefs` `profile.name`, saved at once on a change. The first launch (or a stored name that no longer passes the rules) generates a name, so nobody is ever blocked by a naming step. `NameChanged` fires after a change. `ResetName()` is for the Editor menu.

## Sync to Unity Services

The boards show the Unity Authentication player name, so a changed name is pushed with `LeaderboardService.SetPlayerNameAsync` (`UpdatePlayerNameAsync`, with the usual sign-in, offline fast-fail and timeout; the live-board cache is dropped on success). `UiProfileState.SyncToServiceAsync` is the one entry point: it does nothing when `profile.name_synced` (the name the service last accepted) equals the current name, so it costs no call once synced. It runs on a rename (Profile confirm), when the menu UI builds and whenever the menu is shown again, so a name changed offline, or the first launch's generated one, goes out at the next opportunity. `NameSyncResult.Rejected` (the service refused the name for good) is logged and not retried; `Failed` is retried. A second call while one runs is ignored; a name changed mid-push is picked up by a second round.

**The `#1234` tag:** Unity appends `#1234` to every player name to keep them unique. It is shown, but muted, so a player can tell two identical names apart without it competing with the name. `LeaderboardRow.SplitName` splits a raw board name into `Name` and `Tag` (and drops `<` and `>` from the name, since names are drawn as rich text and come from other players); `PlayerNameText.Apply` (`Scripts/UI/Foundation`) draws `name` + `<color=#F6EFE28C>#1234</color>` (the theme's `--ink-55`, hard-coded because rich text cannot read USS variables) on a label. Used by the leaderboard screen (rows and the pinned own row), the last-week results rows (so the rank share card too) and the Profile name. The Profile's own tag is stored in `profile.name_tag` (`UiProfileState.NameTag`): taken from the reply of `UpdatePlayerNameAsync`, cleared when the name changes (the tag belonged to the old name, so it appears once the sync of the new one finishes), and read once per session with `FetchPlayerNameTagAsync` when a synced name has none stored. The menu chip, Game Over's rank chip and the results screen's own-name caption show the name without the tag.

## Testing

Editor menu `Tools > Profile Name` (`Scripts/Editor/ProfileNameMenu.cs`): Log Name (name and whether it is synced), Reset Name, Sync Now (play from `Bootstrap` for the service).

## Open

- Whether the board entry's name follows a rename immediately or only from the player's next score is a service behaviour to confirm on the `dev` environment.
- The `3` minimum is not from the service's documentation; if `UpdatePlayerNameAsync` accepts shorter or demands longer names, adjust `MIN_LENGTH`.
