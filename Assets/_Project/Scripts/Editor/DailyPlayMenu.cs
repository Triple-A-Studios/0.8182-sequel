using Opoint8182.Daily;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.DailyEditor
{
	/// <summary>Editor shortcuts for testing the daily limits without wiping every other saved value.</summary>
	public static class DailyPlayMenu
	{
		[MenuItem("Tools/Daily Play/Reset Today's Attempts And Practice Time")]
		private static void ResetToday()
		{
			DailyPlay.ResetToday();
			Debug.Log($"[DailyPlay] reset: {DailyPlay.AttemptsLeft} attempts and {DailyPlay.PracticeRemainingSeconds / 60f:0} min of practice left today.");
		}
	}
}
