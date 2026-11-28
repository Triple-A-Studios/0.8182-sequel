namespace Opoint8182.UI
{
	/// <summary>
	/// The shop's fixed copy. There is no currency, ownership or unlock system yet, so nothing can be bought or equipped: the
	/// PLANES tab shows the equipped plane (real, from its <see cref="PlaneDefinition"/>) and teaser cards for the planes
	/// the Beta shop / currency milestone will bring. The coin total is shared with the menu chip
	/// (<see cref="ProfilePlaceholderData.COINS"/>).
	/// </summary>
	public static class ShopPlaceholderData
	{
		public const string FOOTNOTE =
			"Earn coins by crashing well: perfect crashes and long combos pay the most. Nothing here is buyable with real money.";

		public const string BETA_TITLE = "COMING IN BETA";
		public const string BETA_BODY = "Keep stacking coins, you'll need them";

		/// <summary>Teaser cards next to the equipped one.</summary>
		public const int TEASER_COUNT = 3;
		public const string TEASER_NAME = "MYSTERY PLANE";
		public const string TEASER_LINE = "Still in the hangar";
		public const string TEASER_PILL = "COMING IN BETA";
	}
}
