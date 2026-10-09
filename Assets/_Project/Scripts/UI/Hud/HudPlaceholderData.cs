namespace Opoint8182.UI
{
	/// <summary>
	/// HUD values whose real systems arrive in later milestones (UI shell only, see the UI scope rule in plan.md).
	/// One place so the placeholders are easy to find and replace.
	/// </summary>
	public static class HudPlaceholderData
	{
		/// <summary>PRACTICE tag under the score. Wired to the real mode by the "Daily play structure" milestone.</summary>
		public const bool IsPractice = false;

		/// <summary>Functional pause is the later "Pause, settings and audio" milestone; the button only shows this.</summary>
		public const string PauseToastMessage = "PAUSE ARRIVES LATER";

		/// <summary>Coin line under crash popups. Off: there is no currency yet (Shop / currency milestone).</summary>
		public const bool ShowCoinPopup = false;

		/// <summary>Amount shown on the coin line while <see cref="ShowCoinPopup"/> is on.</summary>
		public const int CoinPopupAmount = 12;
	}
}
