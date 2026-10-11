namespace Opoint8182.UI
{
	/// <summary>
	/// Game Over values whose real systems arrive in later milestones (UI shell only, see the UI scope rule in plan.md).
	/// One place so the placeholders are easy to find and replace. Shown on every run until then: they are not real.
	/// </summary>
	public static class GameOverPlaceholderData
	{
		/// <summary>DISTANCE chip. Real value: "Player profile and run stats".</summary>
		public const string Distance = "2.4 km";

		/// <summary>FLATTENED chip (buildings destroyed). Real value: "Player profile and run stats".</summary>
		public const string Flattened = "38";

		/// <summary>BEST COMBO chip (highest multiplier this run). Real value: "Player profile and run stats".</summary>
		public const string BestCombo = "×7";

		/// <summary>EARNED chip amount. Real value: the currency / shop milestone.</summary>
		public const int Earned = 286;

		/// <summary>NEW BEST! sticker beside the score. Off: there is no stored personal best yet.</summary>
		public const bool ShowNewBest = false;

		/// <summary>Line under the primary button. Real value: "Daily play structure" (attempts).</summary>
		public const string RetryNote = "2 attempts left today";

		/// <summary>Gallery only: places gained shown next to the up arrow ("38"). The score service reports no such number, so play shows the arrow alone.</summary>
		public const int SampleRankDelta = 38;

		/// <summary>Gallery only: status after the divider on a provisional rank. Real value: "Daily play structure" (attempts).</summary>
		public const string SampleProvisionalStatus = "PROVISIONAL · 2 LEFT";

		/// <summary>Gallery only: status on a locked, final rank (attempt 3 of 3).</summary>
		public const string SampleFinalStatus = "LOCKED FOR TODAY";

		/// <summary>Gallery only: gap shown beside UNRANKED (a run that did not place).</summary>
		public const string SampleGapStatus = "143,690 TO TOP 500";

		/// <summary>Shown by the buttons whose flow does not exist yet (MAIN MENU, LEADERBOARD, SHARE).</summary>
		public const string ArrivesLaterMessage = "ARRIVES IN A LATER UPDATE";
	}
}
