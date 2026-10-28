using System;
using Opoint8182.Daily;

namespace Opoint8182.UI
{
	/// <summary>
	/// What the Game Over button row and rank-chip status look like for one run end: which attempt state, the note under the
	/// primary button, whether the primary and PRACTICE buttons can be used, and the status after the rank chip's divider.
	/// Pure (plain values in, plain values out) so every mode / attempts-left combination can be checked without a scene.
	/// </summary>
	public readonly struct GameOverRow
	{
		public readonly GameOverAttemptState State;
		public readonly string Note;
		public readonly bool PrimaryEnabled;
		public readonly bool SecondaryBEnabled;
		public readonly string RankStatus;

		public GameOverRow(GameOverAttemptState state, string note, bool primaryEnabled, bool secondaryBEnabled, string rankStatus)
		{
			State = state;
			Note = note;
			PrimaryEnabled = primaryEnabled;
			SecondaryBEnabled = secondaryBEnabled;
			RankStatus = rankStatus;
		}

		/// <summary>
		/// The row for a run that just ended. <paramref name="mode"/> is the run's <see cref="DailyPlay.Mode"/> (None in a
		/// standalone game scene), <paramref name="practiceRun"/> the combined practice flag (it also covers the Inspector
		/// override on a standalone scene). Attempts are already spent, so <paramref name="attemptsLeft"/> counts what is left
		/// after this run.
		/// </summary>
		public static GameOverRow Derive(RunMode mode, bool practiceRun, int attemptsLeft, float practiceRemainingSeconds, DateTime nextResetUtc, DateTime utcNow)
		{
			if (mode == RunMode.Practice)
			{
				var hasTime = practiceRemainingSeconds > 0f;
				var note = hasTime ? DailyPlayText.PracticeLeftNote(practiceRemainingSeconds) : DailyPlayText.PracticeReturnsNote(nextResetUtc, utcNow);
				return new GameOverRow(GameOverAttemptState.Practice, note, hasTime, true, string.Empty);
			}

			if (practiceRun) return new GameOverRow(GameOverAttemptState.Practice, string.Empty, true, true, string.Empty);

			if (mode == RunMode.Competitive)
			{
				if (attemptsLeft > 0)
				{
					return new GameOverRow(GameOverAttemptState.Competitive, DailyPlayText.RetryNote(attemptsLeft), true, true, DailyPlayText.RankStatus(attemptsLeft));
				}

				return new GameOverRow(GameOverAttemptState.FinalAttempt, DailyPlayText.NewAttemptsNote(nextResetUtc, utcNow), true, practiceRemainingSeconds > 0f, DailyPlayText.RankStatus(0));
			}

			// No recorded run mode (a game scene opened on its own): the plain row, nothing to say about attempts.
			return new GameOverRow(GameOverAttemptState.Competitive, string.Empty, true, true, string.Empty);
		}
	}
}
