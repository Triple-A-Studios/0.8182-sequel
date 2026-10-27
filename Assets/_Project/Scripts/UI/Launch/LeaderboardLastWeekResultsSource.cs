using System.Collections.Generic;
using System.Threading.Tasks;
using Opoint8182.Leaderboard;
using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>
	/// The real "Last week's results" source: compares the newest closed weekly version with the one last shown (stored in
	/// PlayerPrefs) and, for a week the player has not seen, reads it from the leaderboard.
	/// <list type="bullet">
	/// <item>A fresh install (nothing stored) records the newest closed week silently and shows nothing; when there is no
	/// closed week yet it records a marker instead, so the first week ever closed is not mistaken for a fresh install.</item>
	/// <item>A player who has never submitted a ranked run, or a week nobody played, shows nothing but is recorded.</item>
	/// <item>A failed lookup shows nothing and records nothing, so the next launch tries again.</item>
	/// <item>A shown week is recorded by <see cref="MarkShown"/>, which the launch flow calls only when it displays it.</item>
	/// </list>
	/// Someone away for several weeks only sees the most recent closed week.
	/// </summary>
	public sealed class LeaderboardLastWeekResultsSource : ILastWeekResultsSource
	{
		public const string SHOWN_VERSION_KEY = "lb_last_shown_version";
		private const string k_NoArchiveMarker = "none";
		private const int k_WinnerCount = 5;

		private readonly LeaderboardService m_service;
		private string m_pendingVersionId;

		public LeaderboardLastWeekResultsSource(LeaderboardService service)
		{
			m_service = service;
		}

		public async Task<LastWeekResult?> GetPendingAsync()
		{
			m_pendingVersionId = null;

			var versions = await m_service.GetWeeklyVersionsAsync();
			if (!versions.Success) return null;

			var stored = PlayerPrefs.GetString(SHOWN_VERSION_KEY, string.Empty);
			if (versions.Archived.Count == 0)
			{
				if (stored.Length == 0) Record(k_NoArchiveMarker);
				return null;
			}

			var newest = versions.Archived[0];
			if (stored == newest.Id) return null;

			if (stored.Length == 0)
			{
				Record(newest.Id);
				return null;
			}

			if (!LeaderboardService.HasEverRanked())
			{
				Record(newest.Id);
				return null;
			}

			var week = await m_service.GetArchivedWeekAsync(newest.Id);
			if (!week.Success) return null;

			var result = Build(newest, week);
			if (!result.HasValue)
			{
				Record(newest.Id);
				return null;
			}

			m_pendingVersionId = newest.Id;
			return result;
		}

		public void MarkShown()
		{
			if (m_pendingVersionId == null) return;

			Record(m_pendingVersionId);
			m_pendingVersionId = null;
		}

		/// <summary>
		/// What the screen shows for a closed week, or null when nobody played it. The player's own row is the one marked
		/// <c>IsSelf</c> among the rows or the pinned <c>Self</c>; without one it is the neutral no-rank variant. The top
		/// percent is the rank over the entry count, rounded up, 1 to 100.
		/// </summary>
		public static LastWeekResult? Build(ArchivedWeek week, TopScoresResult read)
		{
			if (read.Rows.Count == 0 && !read.Self.HasValue) return null;

			var range = LeaderboardWeek.RangeText(week.StartUtc, week.EndUtc);
			var winners = new List<LeaderboardRow>(k_WinnerCount);
			LeaderboardRow? own = null;
			for (var i = 0; i < read.Rows.Count; i++)
			{
				if (i < k_WinnerCount) winners.Add(read.Rows[i]);
				if (read.Rows[i].IsSelf) own = read.Rows[i];
			}

			if (!own.HasValue) own = read.Self;
			if (!own.HasValue) return new LastWeekResult(range, winners, string.Empty, false);

			var rank = own.Value.Rank;
			var pilots = Mathf.Max(read.Total, rank);
			var topPercent = Mathf.Clamp(Mathf.CeilToInt(rank * 100f / pilots), 1, 100);
			return new LastWeekResult(range, winners, own.Value.Name, true, rank, pilots, topPercent, own.Value.Score);
		}

		private static void Record(string versionId)
		{
			PlayerPrefs.SetString(SHOWN_VERSION_KEY, versionId);
			PlayerPrefs.Save();
		}
	}
}
