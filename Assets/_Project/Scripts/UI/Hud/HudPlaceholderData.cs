namespace Opoint8182.UI
{
	/// <summary>
	/// HUD values whose real systems arrive in later milestones (UI shell only, see the UI scope rule in plan.md).
	/// One place so the placeholders are easy to find and replace.
	/// </summary>
	public static class HudPlaceholderData
	{
		/// <summary>Pause popup: mode line in a practice run (no attempt is used).</summary>
		public const string PausePracticeModeLine = "PRACTICE · NO ATTEMPT USED";

		/// <summary>Coin line under crash popups. Off: there is no currency yet (Shop / currency milestone).</summary>
		public const bool ShowCoinPopup = false;

		/// <summary>Amount shown on the coin line while <see cref="ShowCoinPopup"/> is on.</summary>
		public const int CoinPopupAmount = 12;
	}
}
