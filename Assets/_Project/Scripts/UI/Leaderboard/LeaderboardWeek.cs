using System;
using System.Globalization;

namespace Opoint8182.UI
{
	/// <summary>
	/// Week arithmetic for the leaderboard screen: a week is Monday 00:00 UTC to the next Monday (the same boundary the
	/// weekly board will reset on). Pure functions of "now" so they can be checked without a clock.
	/// </summary>
	public static class LeaderboardWeek
	{
		private const int k_DaysInWeek = 7;

		/// <summary>Monday 00:00 UTC of the week that contains <paramref name="utcNow"/>, <paramref name="weeksBack"/> weeks earlier.</summary>
		public static DateTime StartUtc(DateTime utcNow, int weeksBack = 0)
		{
			var daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % k_DaysInWeek;
			return utcNow.Date.AddDays(-daysSinceMonday - k_DaysInWeek * weeksBack);
		}

		/// <summary>
		/// Neutral countdown for the reset chip: whole days while more than a day is left ("3 DAYS"), hours and minutes on
		/// the last day ("6h 12m"). Never red and never pulsing (gap-analysis 10.3: no fake urgency).
		/// </summary>
		public static string ResetText(DateTime utcNow)
		{
			var remaining = StartUtc(utcNow).AddDays(k_DaysInWeek) - utcNow;
			if (remaining.TotalHours > 24d) return $"{(int)Math.Ceiling(remaining.TotalDays)} DAYS";

			return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
		}

		/// <summary>"Oct 6 – Oct 12" for the week <paramref name="weeksBack"/> weeks ago.</summary>
		public static string RangeText(DateTime utcNow, int weeksBack)
		{
			var start = StartUtc(utcNow, weeksBack);
			var end = start.AddDays(k_DaysInWeek - 1);
			return $"{start.ToString("MMM d", CultureInfo.InvariantCulture)} – {end.ToString("MMM d", CultureInfo.InvariantCulture)}";
		}

		public static string Name(int weeksBack) => weeksBack switch
		{
			0 => "THIS WEEK",
			1 => "LAST WEEK",
			_ => $"{weeksBack} WEEKS AGO",
		};
	}
}
