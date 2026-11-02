using Opoint8182.Leaderboard;
using UnityEngine;

namespace Opoint8182.Stats
{
	/// <summary>
	/// The player's best score as the game shows it (Game Over's NEW BEST / YOUR BEST, the Profile's HIGH SCORE tile): the larger of the
	/// local <see cref="PlayerStats.HighScore"/> and the server-confirmed all-time best stored by the <see cref="LeaderboardService"/>.
	/// Both are soft <c>PlayerPrefs</c>; the server one covers ranked runs from before the local store existed, the local one covers
	/// runs the server never confirmed (offline, refused).
	/// </summary>
	public static class PlayerBest
	{
		/// <summary>The best as of now.</summary>
		public static int Current => Of(PlayerStats.HighScore);

		/// <summary>The best given a local high score (Game Over passes the one from before the run it is showing).</summary>
		public static int Of(int localHighScore) => Mathf.Max(localHighScore, LeaderboardService.StoredAllTimeBest);
	}
}
