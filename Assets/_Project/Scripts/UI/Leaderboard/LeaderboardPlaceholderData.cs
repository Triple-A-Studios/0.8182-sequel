using System.Collections.Generic;
using Opoint8182.Leaderboard;

namespace Opoint8182.UI
{
	/// <summary>
	/// Static stand-ins for what the leaderboard screen cannot get yet (UI/art pass, Pass 6b). One block, so the later
	/// milestones know exactly what to replace:
	/// <list type="bullet">
	/// <item>ALL-TIME has no board behind it: <see cref="AllTime"/> is sample data, tagged "SAMPLE DATA" on screen.</item>
	/// <item>Past weeks need weekly boards (backlog #28): the buttons are built but <see cref="PastWeeksEnabled"/> stays
	/// false in play; the dev gallery flips it and steps through the sample weeks below.</item>
	/// <item>WEEKLY itself is the real Top-10 of the existing <b>daily</b> board until #28, so its label and the reset
	/// chip (computed locally from the UTC Monday boundary) are ahead of the data.</item>
	/// </list>
	/// </summary>
	public static class LeaderboardPlaceholderData
	{
		public const int MAX_WEEKS_BACK = 4;
		public const bool ALL_TIME_IS_SAMPLE = true;
		public const string SAMPLE_SELF_NAME = "RustyBiplane47";

		// ‹ is live only once weekly boards exist (backlog #28). The gallery flips this.
		public static bool PastWeeksEnabled { get; set; }

		private static readonly string[] m_S_Names =
		{
			"TurboMoose", "CementDust", "WingedIdiot", "RebarRhonda", "HangarQueen",
			"glassparade", "PropWashPete", "SkylineSally", "CrashTestKarl", "NoseDiveNora",
		};

		private static readonly int[] m_S_AllTimeScores =
		{
			1204880, 1150320, 1098774, 982410, 940105, 901662, 871030, 835590, 802115, 779940,
		};

		private static readonly int[][] m_S_WeekScores =
		{
			new[] { 588120, 561900, 530445, 489020, 450335, 422810, 401270, 388005, 371660, 352940 },
			new[] { 640210, 604775, 577330, 521880, 489990, 455105, 430620, 412300, 395875, 377450 },
			new[] { 575060, 549210, 512740, 470115, 441880, 418395, 399040, 380615, 361290, 340775 },
		};

		/// <summary>Sample all-time board with the player at #1,204 (outside the list, so the pinned row shows).</summary>
		public static TopScoresResult AllTime(string selfName)
		{
			return Build(m_S_AllTimeScores, 0, new LeaderboardRow(1204, selfName, 184920, true));
		}

		/// <summary>
		/// Sample past week: 1 has the player at #312, 2 at #96, 3 has no score of theirs, 4 is an empty board (so the
		/// FINAL chip, the pinned-row "no score" variant and the empty state all have a sample).
		/// </summary>
		public static TopScoresResult PastWeek(int weeksBack, string selfName)
		{
			switch (weeksBack)
			{
				case 1: return Build(m_S_WeekScores[0], 3, new LeaderboardRow(312, selfName, 171440, true));
				case 2: return Build(m_S_WeekScores[1], 6, new LeaderboardRow(96, selfName, 201330, true));
				case 3: return Build(m_S_WeekScores[2], 9, null);
				default: return new TopScoresResult(true, new List<LeaderboardRow>(), null);
			}
		}

		private static TopScoresResult Build(int[] scores, int nameOffset, LeaderboardRow? self)
		{
			var rows = new List<LeaderboardRow>(scores.Length);
			for (var i = 0; i < scores.Length; i++)
			{
				rows.Add(new LeaderboardRow(i + 1, m_S_Names[(i + nameOffset) % m_S_Names.Length], scores[i], false));
			}

			return new TopScoresResult(true, rows, self);
		}
	}
}
