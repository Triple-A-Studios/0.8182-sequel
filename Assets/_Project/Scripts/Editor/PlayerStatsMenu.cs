using Opoint8182.Stats;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.StatsEditor
{
	/// <summary>Editor shortcuts for checking the lifetime stats store without playing a run.</summary>
	public static class PlayerStatsMenu
	{
		[MenuItem("Tools/Player Stats/Log Stats")]
		private static void LogStats()
		{
			Debug.Log($"[PlayerStats] high score {PlayerStats.HighScore}, distance {PlayerStats.DistanceMeters:0.#} m, buildings {PlayerStats.BuildingsDestroyed}, " +
			          $"best combo x{PlayerStats.BestCombo}, runs {PlayerStats.RunsFlown}, perfect crashes {PlayerStats.PerfectCrashes}");
		}

		[MenuItem("Tools/Player Stats/Record Sample Competitive Run")]
		private static void RecordCompetitive() => Record(false);

		[MenuItem("Tools/Player Stats/Record Sample Practice Run")]
		private static void RecordPractice() => Record(true);

		[MenuItem("Tools/Player Stats/Reset Stats")]
		private static void ResetStats()
		{
			PlayerStats.Reset();
			Debug.Log("[PlayerStats] reset.");
		}

		private static void Record(bool practice)
		{
			var newHighScore = PlayerStats.RecordRun(new RunStats(12000, 1500f, 20, 5, 3, practice));
			Debug.Log($"[PlayerStats] recorded a sample {(practice ? "practice" : "competitive")} run (12000 points, 1500 m, 20 buildings, x5, 3 perfect). New high score: {newHighScore}.");
			LogStats();
		}
	}
}
