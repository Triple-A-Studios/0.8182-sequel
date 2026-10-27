using System.Collections.Generic;
using System.Threading.Tasks;
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
	/// <see cref="LeaderboardLastWeekResultsSource"/>; <see cref="NullLastWeekResultsSource"/> stands in when there is no
	/// leaderboard service. The lookup is async (it reads the leaderboard), starts at boot so it overlaps the loading
	/// screen, and the flow only waits a few seconds for it.
	/// </summary>
	public interface ILastWeekResultsSource
	{
		/// <summary>The results to show once now, or null when there is nothing new to show (or the lookup failed).</summary>
		Task<LastWeekResult?> GetPendingAsync();

		/// <summary>
		/// Called by the launch flow the moment it actually shows what <see cref="GetPendingAsync"/> returned, so that week is
		/// not shown again. A result the flow dropped (the lookup came too late) is never marked and comes back next launch.
		/// </summary>
		void MarkShown();
	}

	public sealed class NullLastWeekResultsSource : ILastWeekResultsSource
	{
		public Task<LastWeekResult?> GetPendingAsync() => Task.FromResult<LastWeekResult?>(null);

		public void MarkShown()
		{
		}
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

		public Task<LastWeekResult?> GetPendingAsync() =>
			Task.FromResult<LastWeekResult?>(LastWeekResultsPlaceholderData.Sample(m_withRank, System.DateTime.UtcNow));

		public void MarkShown()
		{
		}
	}
}
