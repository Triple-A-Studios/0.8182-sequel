namespace Opoint8182.UI
{
	public enum ShopCardState
	{
		Equipped,
		Buy,
		NotEnough,
		Locked,
	}

	/// <summary>One plane card on the shop's PLANES tab. Prices and progress are display strings (static for now).</summary>
	public readonly struct ShopPlaneCard
	{
		public readonly string Name;
		public readonly string Description;
		public readonly ShopCardState State;

		/// <summary>Price text for Buy / NotEnough, progress text ("2.4 / 5.0 km") for Locked, unused for Equipped.</summary>
		public readonly string Detail;

		public ShopPlaneCard(string name, string description, ShopCardState state, string detail = "")
		{
			Name = name;
			Description = description;
			State = state;
			Detail = detail;
		}
	}

	/// <summary>
	/// Static stand-ins for the shop (UI/art pass, Pass 7a): the four plane cards and the copy. There is no currency,
	/// ownership or unlock system yet, so nothing here can be bought or equipped; the real thing is the Beta
	/// shop / currency milestone. The coin total is shared with the menu chip (<see cref="ProfilePlaceholderData.COINS"/>).
	/// </summary>
	public static class ShopPlaceholderData
	{
		public const string FOOTNOTE =
			"Earn coins by crashing well: perfect crashes and long combos pay the most. Nothing here is buyable with real money.";

		public const string BETA_TITLE = "COMING IN BETA";
		public const string BETA_BODY = "Keep stacking coins, you'll need them";

		public static readonly ShopPlaneCard[] Planes =
		{
			new("Crop Duster Mk.II", "Slow, forgiving, huge nose", ShopCardState.Equipped),
			new("Jet Lunchbox", "Fast, twitchy, thirsty", ShopCardState.Buy, "8,000"),
			new("Flying Bathtub", "Heavy. Tough buildings fear it", ShopCardState.NotEnough, "18,000"),
			new("LOCKED", "Unlocks at 5 km in a single run", ShopCardState.Locked, "2.4 / 5.0 km"),
		};
	}
}
