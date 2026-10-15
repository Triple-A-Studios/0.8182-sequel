using System;
using Opoint8182.Leaderboard;

namespace Opoint8182.UI
{
	/// <summary>
	/// Static sample for the "Last week's results" screen (UI/art pass, Pass 7c): five winners and the player's finish.
	/// None of it is real; the real data (archived weekly board, own entry, total pilots) arrives with backlog #28.
	/// Only the date range is computed (the week before <c>utcNow</c>, Monday to Sunday UTC).
	/// </summary>
	public static class LastWeekResultsPlaceholderData
	{
		public const int RANK = 31;
		public const int PILOTS = 2418;
		public const int TOP_PERCENT = 2;
		public const int BEST_SCORE = 412880;

		public static LastWeekResult Sample(bool withRank, DateTime utcNow)
		{
			var winners = new[]
			{
				new LeaderboardRow(1, "TurboMoose", 612480, false),
				new LeaderboardRow(2, "CementDust", 588105, false),
				new LeaderboardRow(3, "WingedIdiot", 540332, false),
				new LeaderboardRow(4, "RebarRhonda", 498770, false),
				new LeaderboardRow(5, "HangarQueen", 455190, false),
			};

			var range = LeaderboardWeek.RangeText(utcNow, 1);
			var name = LeaderboardPlaceholderData.SAMPLE_SELF_NAME;

			return withRank
				? new LastWeekResult(range, winners, name, true, RANK, PILOTS, TOP_PERCENT, BEST_SCORE)
				: new LastWeekResult(range, winners, name, false);
		}
	}
}
