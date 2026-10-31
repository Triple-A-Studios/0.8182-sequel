using System.Globalization;
using Opoint8182.Game;
using Opoint8182.UI;

namespace Opoint8182.Share
{
	public enum ShareCardKind
	{
		/// <summary>A finished run: the Game Over screen without its buttons.</summary>
		Run,

		/// <summary>A leaderboard result: the "Last week's results" layout without its buttons.</summary>
		Rank,

		/// <summary>The player's profile screen without its buttons.</summary>
		Profile,
	}

	/// <summary>Which board a rank card is about; it picks the title, the header chip and the share text.</summary>
	public enum ShareBoard
	{
		ThisWeek,

		/// <summary>The week that just closed (the launch results screen).</summary>
		LastWeek,

		/// <summary>An earlier closed week, browsed on the leaderboard screen.</summary>
		ClosedWeek,

		AllTime,
	}

	/// <summary>
	/// Everything one share card shows, plus the one-line text that goes with it. Built by the screens that have a SHARE
	/// button (<see cref="ForRun"/>, <see cref="ForRank"/>, <see cref="ForProfile"/>), drawn by <see cref="ShareCardView"/> and
	/// <see cref="ShareCardRenderer"/>. A card is the real screen's layout rendered to an image: the numbers shown are whatever
	/// the screen shows (the run's stat chips and the profile's stats are the same static stand-ins as on screen until their
	/// milestone). Wording uses only glyphs the fonts bake in.
	/// </summary>
	public sealed class ShareCardData
	{
		public ShareCardKind Kind { get; private set; }

		// Run
		public int Score { get; private set; }
		public RunEndCause Cause { get; private set; }
		public bool IsPractice { get; private set; }

		/// <summary>Rank on this week's board, 0 for none (no rank chip; a practice run never has one).</summary>
		public int WeeklyRank { get; private set; }

		/// <summary>NEW BEST! sticker; otherwise YOUR BEST shows <see cref="BestScore"/> when there is one.</summary>
		public bool IsNewBest { get; private set; }

		public int BestScore { get; private set; }
		public string PlayerName { get; private set; }
		public string Distance { get; private set; }
		public string Flattened { get; private set; }
		public string BestCombo { get; private set; }
		public int Earned { get; private set; }

		// Rank
		public ShareBoard Board { get; private set; }
		public LastWeekResult Result { get; private set; }

		private ShareCardData()
		{
		}

		/// <param name="weeklyRank">The run's rank on this week's board, or 0 when it has none.</param>
		/// <param name="isNewBest">The run beat the player's best (NEW BEST! sticker). Otherwise YOUR BEST shows <paramref name="bestScore"/>.</param>
		/// <param name="playerName">Shown beside the rank; null reads the session display name.</param>
		public static ShareCardData ForRun(int score, RunEndCause cause, bool isPractice, int weeklyRank = 0, bool isNewBest = false,
			int bestScore = 0, string playerName = null) =>
			new ShareCardData
			{
				Kind = ShareCardKind.Run,
				Score = score,
				Cause = cause,
				IsPractice = isPractice,
				WeeklyRank = isPractice ? 0 : weeklyRank,
				IsNewBest = isNewBest,
				BestScore = bestScore,
				PlayerName = playerName ?? UiProfileState.DisplayName,
				Distance = GameOverPlaceholderData.Distance,
				Flattened = GameOverPlaceholderData.Flattened,
				BestCombo = GameOverPlaceholderData.BestCombo,
				Earned = GameOverPlaceholderData.Earned,
			};

		/// <param name="result">The rows, the player's finish and the range text of the board being shared.</param>
		public static ShareCardData ForRank(ShareBoard board, LastWeekResult result) =>
			new ShareCardData { Kind = ShareCardKind.Rank, Board = board, Result = result };

		public static ShareCardData ForProfile(string playerName = null) =>
			new ShareCardData { Kind = ShareCardKind.Profile, PlayerName = playerName ?? UiProfileState.DisplayName };

		// ---- Rank card wording (the S11 layout re-labelled per board) ----

		/// <summary>Screen title of a rank card.</summary>
		public string RankTitle
		{
			get
			{
				switch (Board)
				{
					case ShareBoard.ThisWeek: return "THIS WEEK'S LEADERBOARD";
					case ShareBoard.AllTime: return "ALL-TIME LEADERBOARD";
					case ShareBoard.ClosedWeek: return "WEEKLY RESULTS";
					default: return "LAST WEEK'S RESULTS";
				}
			}
		}

		/// <summary>Range text in the header chip ("SEP 29 – OCT 5", or "ALL-TIME").</summary>
		public string RankRange => Board == ShareBoard.AllTime ? "ALL-TIME" : Result.RangeText.ToUpperInvariant();

		/// <summary>Tag after the range in the header chip, empty for none.</summary>
		public string RankTag
		{
			get
			{
				switch (Board)
				{
					case ShareBoard.ThisWeek: return "LIVE";
					case ShareBoard.AllTime: return string.Empty;
					default: return "FINAL";
				}
			}
		}

		/// <summary>Small line above the big rank on the player card.</summary>
		public string RankKicker => Board == ShareBoard.LastWeek || Board == ShareBoard.ClosedWeek ? "YOU FINISHED" : "YOUR RANK";

		/// <summary>Line under the score on the player card.</summary>
		public string RankCaption
		{
			get
			{
				var suffix = Board == ShareBoard.AllTime ? "all-time best" : Board == ShareBoard.ThisWeek ? "best run this week" : "best run of the week";
				return Result.PlayerName + " · " + suffix;
			}
		}

		/// <summary>The one-line message shared with the card. No link; a store page can be added later in one place.</summary>
		public string Text
		{
			get
			{
				switch (Kind)
				{
					case ShareCardKind.Run: return RunText();
					case ShareCardKind.Profile: return "My pilot profile in Opoint8182!";
					default: return RankText();
				}
			}
		}

		private string RunText()
		{
			if (IsPractice) return "I scored " + Format(Score) + " in a practice run on Opoint8182!";
			return WeeklyRank > 0
				? "I scored " + Format(Score) + " in Opoint8182 and I'm #" + Format(WeeklyRank) + " this week!"
				: "I scored " + Format(Score) + " in Opoint8182!";
		}

		private string RankText()
		{
			if (!Result.HasRank) return "Check out the Opoint8182 leaderboard!";

			var rank = Format(Result.Rank);
			switch (Board)
			{
				case ShareBoard.AllTime: return "I'm #" + rank + " on the all-time Opoint8182 board!";
				case ShareBoard.ThisWeek: return "I'm #" + rank + " on this week's Opoint8182 board!";
				case ShareBoard.LastWeek: return "I finished #" + rank + " on last week's Opoint8182 board!";
				default:
					return Result.RangeText.Length > 0
						? "I finished #" + rank + " on the Opoint8182 weekly board (" + Result.RangeText + ")!"
						: "I finished #" + rank + " on the Opoint8182 weekly board!";
			}
		}

		private static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
	}
}
