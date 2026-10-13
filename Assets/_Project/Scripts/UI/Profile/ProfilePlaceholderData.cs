namespace Opoint8182.UI
{
	/// <summary>One badge on the profile screen. <see cref="Title"/> uses a line break so it fits the tile.</summary>
	public readonly struct BadgeDefinition
	{
		public readonly string Title;
		public readonly string Condition;
		public readonly bool Unlocked;

		public BadgeDefinition(string title, string condition, bool unlocked)
		{
			Title = title;
			Condition = condition;
			Unlocked = unlocked;
		}

		public string SingleLineTitle => Title.Replace('\n', ' ');
	}

	/// <summary>
	/// Static stand-ins for what the profile screen cannot get yet (UI/art pass, Pass 6c): the stats, coins, id,
	/// equipped plane / crash FX and the badge list. Nothing here is saved or earned; the real values arrive with the
	/// "Player profile and run stats" milestone (stats, id, coins) and the shop / cosmetics milestone (equipped items).
	/// The display name is the one live part, see <see cref="UiProfileState"/>.
	/// </summary>
	public static class ProfilePlaceholderData
	{
		public const string ID_LINE = "ID #2VULYCQUJ · no account";
		public const string COINS = "12,480";

		public const string HIGH_SCORE = "184,920";
		public const string DISTANCE_FLOWN = "418 km";
		public const string BUILDINGS_DOWN = "6,204";
		public const string BEST_COMBO = "×11";
		public const string RUNS_FLOWN = "312";
		public const string PERFECT_CRASHES = "1,088";

		public const string EQUIPPED_PLANE = "Crop Duster Mk.II";
		public const string CRASH_FX = "Confetti Blast";

		/// <summary>Three unlocked and nine locked, as in the mockup (BADGES 3 / 12). Conditions are placeholders.</summary>
		public static readonly BadgeDefinition[] Badges =
		{
			new("FIRST\nBLOOD", "Crash into your first building", true),
			new("×10\nCOMBO", "Reach a ×10 combo", true),
			new("1000\nCRASHES", "Crash 1,000 buildings in total", true),
			new("PERFECT\n100", "Land 100 perfect crashes", false),
			new("250K\nRUN", "Score 250,000 in a single run", false),
			new("ROAD\nTRIP", "Fly 500 km in total", false),
			new("100\nRUNS", "Fly 100 runs", false),
			new("TOP\nTEN", "Reach the top 10 of a weekly board", false),
			new("WEEKLY\nCHAMP", "Finish a week in 1st place", false),
			new("DAILY\nGRIND", "Play on 7 days in a row", false),
			new("TOUGH\nLANDING", "Survive a crash with 1 HP left", false),
			new("DEMO\nCREW", "Bring down 10,000 buildings", false),
		};
	}
}
