using System.Collections.Generic;
using Opoint8182.Leaderboard;

namespace Opoint8182.UI
{
	/// <summary>
	/// What the "Last week's results" screen (mockup S11) shows: the week that just closed, its top five and the
	/// player's own finish. <see cref="HasRank"/> false is the "no ranked run last week" variant (rank and score are
	/// not used then, and there is no share).
	/// </summary>
	public readonly struct LastWeekResult
	{
		/// <summary>Display range of the closed week, "Sep 29 – Oct 5".</summary>
		public readonly string RangeText;
		public readonly IReadOnlyList<LeaderboardRow> Winners;
		public readonly string PlayerName;
		public readonly bool HasRank;
		public readonly int Rank;
		public readonly int Pilots;
		public readonly int TopPercent;
		public readonly int BestScore;

		public LastWeekResult(string rangeText, IReadOnlyList<LeaderboardRow> winners, string playerName, bool hasRank,
			int rank = 0, int pilots = 0, int topPercent = 0, int bestScore = 0)
		{
			RangeText = rangeText;
			Winners = winners;
			PlayerName = playerName;
			HasRank = hasRank;
			Rank = rank;
			Pilots = pilots;
			TopPercent = topPercent;
			BestScore = bestScore;
		}
	}

	/// <summary>
	/// Where the launch flow asks whether a closed week still has to be shown. The shipped implementation is
	/// <see cref="NullLastWeekResultsSource"/> (nothing, until the weekly leaderboard rework, backlog #28, supplies a
	/// real one that compares the newest archived version id with the one last shown and records it afterwards).
	/// </summary>
	public interface ILastWeekResultsSource
	{
		/// <summary>The results to show once now, or null when there is nothing new to show.</summary>
		LastWeekResult? TryGetPending();
	}

	public sealed class NullLastWeekResultsSource : ILastWeekResultsSource
	{
		public LastWeekResult? TryGetPending() => null;
	}

	/// <summary>Forces the screen in dev: set on the Bootstrap launch flow object (ignored in release builds).</summary>
	public enum LastWeekResultsDebugMode
	{
		None,
		WithRank,
		NoRank,
	}

	/// <summary>Always returns the static sample, for the debug mode and the gallery.</summary>
	public sealed class SampleLastWeekResultsSource : ILastWeekResultsSource
	{
		private readonly bool m_withRank;

		public SampleLastWeekResultsSource(bool withRank)
		{
			m_withRank = withRank;
		}

		public LastWeekResult? TryGetPending() => LastWeekResultsPlaceholderData.Sample(m_withRank, System.DateTime.UtcNow);
	}
}
