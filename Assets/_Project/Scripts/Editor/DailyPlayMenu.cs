using Opoint8182.Daily;
using Opoint8182.Server;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.DailyEditor
{
	/// <summary>Editor shortcuts for testing the daily limits without wiping every other saved value.</summary>
	public static class DailyPlayMenu
	{
		[MenuItem("Tools/Daily Play/Reset Today's Attempts And Practice Time")]
		private static async void ResetToday()
		{
			DailyPlay.ResetToday();
			Debug.Log($"[DailyPlay] reset: {DailyPlay.PracticeRemainingSeconds / 60f:0} min of practice left today.");

			// The attempts live on the server: reset them too (the DevSetState script refuses in the production environment).
			// Needs play mode from Bootstrap, where the LeaderboardService is signed in.
			if (!Application.isPlaying)
			{
				Debug.Log("[DailyPlay] server attempts are not reset: play from Bootstrap and run this again.");
				return;
			}

			var ok = await DailyServer.ResetDevStateAsync() && await DailyPlay.RefreshFromServerAsync();
			Debug.Log(ok
				? $"[DailyPlay] server attempts reset: {DailyPlay.AttemptsLeft} left today."
				: "[DailyPlay] server attempts were not reset (no Bootstrap service, offline, or the production environment).");
		}
	}
}
