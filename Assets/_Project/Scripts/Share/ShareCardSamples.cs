using System;
using System.Collections.Generic;
using Opoint8182.Game;
using Opoint8182.UI;

namespace Opoint8182.Share
{
	/// <summary>Sample cards for the dev gallery and the checks: one of every layout variant the game can produce.</summary>
	public static class ShareCardSamples
	{
		public static IReadOnlyList<(string Label, ShareCardData Data)> All()
		{
			var now = DateTime.UtcNow;
			var lastWeek = LastWeekResultsPlaceholderData.Sample(true, now);
			var thisWeek = new LastWeekResult(LeaderboardWeek.RangeText(now, 0), lastWeek.Winners, lastWeek.PlayerName, true, 7, 2418, 1, 184920);
			var allTime = new LastWeekResult(string.Empty, lastWeek.Winners, lastWeek.PlayerName, true, 248, 9120, 3, 412880);
			var earlier = LastWeekResultsPlaceholderData.Sample(true, now.AddDays(-7));

			return new[]
			{
				("run_ranked_new_best", ShareCardData.ForRun(184920, RunEndCause.Wrecked, false, 7, true, 0, "OLIVE")),
				("run_unranked_your_best", ShareCardData.ForRun(96310, RunEndCause.OutOfFuel, false, 0, false, 184920, "OLIVE")),
				("run_practice", ShareCardData.ForRun(51240, RunEndCause.FlewTooHigh, true, 0, false, 184920, "OLIVE")),
				("rank_last_week", ShareCardData.ForRank(ShareBoard.LastWeek, lastWeek)),
				("rank_closed_week", ShareCardData.ForRank(ShareBoard.ClosedWeek, earlier)),
				("rank_this_week", ShareCardData.ForRank(ShareBoard.ThisWeek, thisWeek)),
				("rank_all_time", ShareCardData.ForRank(ShareBoard.AllTime, allTime)),
				("profile", ShareCardData.ForProfile("OLIVE")),
			};
		}
	}
}
