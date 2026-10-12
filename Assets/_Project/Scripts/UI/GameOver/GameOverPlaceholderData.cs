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

		/// <summary>
		/// Which attempt-driven button row play shows: RETRY primary with the note below (S05 b). Attempts do not exist yet,
		/// so every run shows this; the gallery shows the other states. Real value: "Daily play structure".
		/// </summary>
		public const GameOverAttemptState DefaultAttemptState = GameOverAttemptState.DidNotPlace;

		/// <summary>Line under RETRY (S05 b). Real value: "Daily play structure" (attempts).</summary>
		public const string RetryNote = "2 attempts left today";

		/// <summary>Line under BANK IT (S05 a). Real value: "Daily play structure" (attempts).</summary>
		public const string BankItNote = "keep 2 attempts for later today";

		/// <summary>Line under LEADERBOARD on the final attempt (S05 c). Real value: "Daily play structure" (reset timer).</summary>
		public const string FinalAttemptNote = "next attempts in 6h 12m";

		/// <summary>Gallery only: the YOUR BEST chip value (S05 b). Real value: "Player profile and run stats".</summary>
		public const string SampleBest = "184,920";

		/// <summary>
		/// Status after the divider on a ranked chip in play, from the attempt state: PROVISIONAL while attempts remain,
		/// LOCKED once the last one is used, nothing in practice. Static until attempts exist ("Daily play structure").
		/// </summary>
		public static string RankStatusFor(GameOverAttemptState state)
		{
			return state switch
			{
				GameOverAttemptState.FinalAttempt => SampleFinalStatus,
				GameOverAttemptState.Practice => string.Empty,
				_ => SampleProvisionalStatus
			};
		}

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
