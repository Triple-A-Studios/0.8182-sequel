namespace Opoint8182.UI
{
	/// <summary>
	/// Static stand-ins for what the profile screen cannot get yet (UI/art pass, Pass 6c): the coins and the id.
	/// Nothing here is saved or earned; the real values arrive with the "Player profile and run stats" milestone (id) and the
	/// currency (coins). The equipped plane and the crash effect are real since the 3D art pass.
	/// The six stat tiles are real since that milestone's Pass 3 (<see cref="Opoint8182.Stats.PlayerStats"/>) and the badges since Pass 5 (<see cref="Opoint8182.Badges.BadgeCatalog"/>).
	/// The display name is the one live part, see <see cref="UiProfileState"/>.
	/// </summary>
	public static class ProfilePlaceholderData
	{
		public const string ID_LINE = "ID #2VULYCQUJ · no account";
		public const string COINS = "12,480";
	}
}
