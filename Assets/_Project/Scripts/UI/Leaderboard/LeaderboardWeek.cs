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
		/// Neutral countdown for the reset chip: whole days while more than a day is left ("3 DAYS"), whole hours on the
		/// last day ("6h"), never minutes. Never red and never pulsing (no fake urgency, design-doc UX rules). The reset
		/// instant is the board's <c>nextReset</c> when known, otherwise the coming Monday 00:00 UTC.
		/// </summary>
		public static string ResetText(DateTime utcNow, DateTime? nextResetUtc = null)
		{
			var remaining = (nextResetUtc ?? StartUtc(utcNow).AddDays(k_DaysInWeek)) - utcNow;
			if (remaining.TotalHours > 24d) return $"{(int)Math.Ceiling(remaining.TotalDays)} DAYS";

			return $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalHours))}h";
		}

		/// <summary>
		/// "Oct 6 – Oct 12" from a version's server start and end. <paramref name="endUtc"/> is the instant the next version
		/// began, so the last day shown is one second earlier; a version that starts and ends on one day prints that day once.
		/// </summary>
		public static string RangeText(DateTime startUtc, DateTime endUtc)
		{
			var last = endUtc.AddSeconds(-1);
			if (last < startUtc) last = startUtc;

			var from = startUtc.ToString("MMM d", CultureInfo.InvariantCulture);
			var to = last.ToString("MMM d", CultureInfo.InvariantCulture);
			return from == to ? from : $"{from} – {to}";
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
