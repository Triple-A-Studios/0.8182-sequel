using System;
using Opoint8182.Daily;
using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>
	/// The menu's wording for the daily counters (<see cref="Opoint8182.Daily.DailyPlay"/>). Pure functions so the copy can be
	/// checked without a UI. Neutral by rule: no urgency, no minutes in a reset time (design-doc UX rules).
	/// </summary>
	public static class DailyPlayText
	{
		/// <summary>"3 ATTEMPTS LEFT TODAY", "1 ATTEMPT LEFT TODAY".</summary>
		public static string AttemptsLeft(int left)
		{
			return left == 1 ? "1 ATTEMPT LEFT TODAY" : $"{left} ATTEMPTS LEFT TODAY";
		}

		/// <summary>"2/3".</summary>
		public static string AttemptsCount(int left, int max) => $"{left}/{max}";

		/// <summary>The attempt-confirm popup body.</summary>
		public static string ConfirmBody(int left, int max)
		{
			return $"This run counts toward this week's leaderboard. You have {left} of {max} attempts left today.";
		}

		/// <summary>"28:14" from seconds (rounded down).</summary>
		public static string PracticeClock(float seconds)
		{
			var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
			return $"{total / 60}:{total % 60:00}";
		}

		/// <summary>"28:14 LEFT TODAY".</summary>
		public static string PracticeLeft(float seconds) => $"{PracticeClock(seconds)} LEFT TODAY";

		/// <summary>Whole hours until the reset, at least 1 ("7h"); never minutes.</summary>
		public static string ResetHours(DateTime nextResetUtc, DateTime utcNow)
		{
			var hours = Math.Max(1, (int)Math.Ceiling((nextResetUtc - utcNow).TotalHours));
			return $"{hours}h";
		}

		/// <summary>Pause popup mode line: "COMPETITIVE · ATTEMPT 2/3".</summary>
		public static string PauseModeLine(int attemptNumber, int max) => $"COMPETITIVE · ATTEMPT {attemptNumber}/{max}";

		/// <summary>Pause popup warning under the buttons.</summary>
		public static string PauseQuitNote(int attemptNumber, int max) => $"Quitting still burns attempt {attemptNumber} of {max}.";

		/// <summary>The restart confirm body: the current attempt is given up, the next one is used.</summary>
		public static string RestartBody(int nextAttemptNumber, int max)
		{
			return $"This attempt is lost without submitting your score. The new run uses attempt {nextAttemptNumber} of {max}.";
		}

		/// <summary>What a refused restart or RETRY says: when the daily limit for that mode comes back (whole hours).</summary>
		public static string RefusedLine(RunMode mode)
		{
			var nextReset = DailyPlay.NextResetUtc;
			var now = DailyPlay.Clock.UtcNow;
			return mode == RunMode.Practice ? PracticeReturnsIn(nextReset, now) : NewAttemptsIn(nextReset, now);
		}

		/// <summary>The PLAY sub-label with no attempts left.</summary>
		public static string NewAttemptsIn(DateTime nextResetUtc, DateTime utcNow) => $"NEW ATTEMPTS IN {ResetHours(nextResetUtc, utcNow)}";

		/// <summary>The PLAY sub-label with no practice time left.</summary>
		public static string PracticeReturnsIn(DateTime nextResetUtc, DateTime utcNow) => $"PRACTICE RETURNS IN {ResetHours(nextResetUtc, utcNow)}";
	}
}
