using System;
using UnityEngine;

namespace Opoint8182.Stats
{
	/// <summary>What one finished run adds to the lifetime stats. <see cref="IsPractice"/> runs count toward the totals but not the high score.</summary>
	public readonly struct RunStats
	{
		public readonly int Score;
		public readonly float DistanceMeters;
		public readonly int BuildingsDestroyed;

		/// <summary>The longest combo streak of the run: buildings crashed in a row without the combo being disrupted.</summary>
		public readonly int BestCombo;

		public readonly int PerfectCrashes;
		public readonly bool IsPractice;

		public RunStats(int score, float distanceMeters, int buildingsDestroyed, int bestCombo, int perfectCrashes, bool isPractice)
		{
			Score = Mathf.Max(0, score);
			DistanceMeters = Mathf.Max(0f, distanceMeters);
			BuildingsDestroyed = Mathf.Max(0, buildingsDestroyed);
			BestCombo = Mathf.Max(0, bestCombo);
			PerfectCrashes = Mathf.Max(0, perfectCrashes);
			IsPractice = isPractice;
		}
	}

	/// <summary>
	/// The player's lifetime stats (the Profile screen's six tiles), persisted locally in <see cref="PlayerPrefs"/> like
	/// <see cref="SettingsService"/>: no cloud, soft (clearing storage resets them). Static, so it needs no scene object.
	/// Values load lazily and are clamped, so a corrupt pref cannot produce a bad state. <see cref="RecordRun"/> is the only
	/// writer and saves at once: a run ends rarely, so there is nothing to batch and nothing left dirty for an app kill to lose.
	/// Every run counts toward the totals, practice included; only <see cref="HighScore"/> is competitive-only, matching the boards.
	/// </summary>
	public static class PlayerStats
	{
		private const string k_HighScoreKey = "stats.high_score";
		private const string k_DistanceKey = "stats.distance_m";
		private const string k_BuildingsKey = "stats.buildings";
		private const string k_BestComboKey = "stats.best_combo";
		private const string k_RunsKey = "stats.runs";
		private const string k_PerfectKey = "stats.perfect";

		private static bool m_s_loaded;

		private static int m_s_highScore;
		private static float m_s_distanceMeters;
		private static int m_s_buildingsDestroyed;
		private static int m_s_bestCombo;
		private static int m_s_runsFlown;
		private static int m_s_perfectCrashes;

		/// <summary>Raised after a run was recorded or the stats were reset.</summary>
		public static event Action Changed;

		/// <summary>Best score of a competitive run, 0 when there is none.</summary>
		public static int HighScore { get { EnsureLoaded(); return m_s_highScore; } }

		/// <summary>Total distance flown, in meters.</summary>
		public static float DistanceMeters { get { EnsureLoaded(); return m_s_distanceMeters; } }

		/// <summary>Total buildings brought down.</summary>
		public static int BuildingsDestroyed { get { EnsureLoaded(); return m_s_buildingsDestroyed; } }

		/// <summary>Longest combo streak ever: buildings crashed in a row without the combo being disrupted.</summary>
		public static int BestCombo { get { EnsureLoaded(); return m_s_bestCombo; } }

		public static int RunsFlown { get { EnsureLoaded(); return m_s_runsFlown; } }

		public static int PerfectCrashes { get { EnsureLoaded(); return m_s_perfectCrashes; } }

		/// <summary>
		/// Adds a finished run to the lifetime stats and saves. Returns true when it set a new <see cref="HighScore"/>
		/// (a competitive run above the previous best; a first scoring run counts).
		/// </summary>
		public static bool RecordRun(RunStats run)
		{
			EnsureLoaded();

			var newHighScore = !run.IsPractice && run.Score > m_s_highScore;
			if (newHighScore) m_s_highScore = run.Score;

			m_s_distanceMeters += run.DistanceMeters;
			m_s_buildingsDestroyed = SaturatingAdd(m_s_buildingsDestroyed, run.BuildingsDestroyed);
			m_s_bestCombo = Mathf.Max(m_s_bestCombo, run.BestCombo);
			m_s_runsFlown = SaturatingAdd(m_s_runsFlown, 1);
			m_s_perfectCrashes = SaturatingAdd(m_s_perfectCrashes, run.PerfectCrashes);

			Save();
			Changed?.Invoke();
			return newHighScore;
		}

		/// <summary>Clears every stat (Editor test menu, and the future "delete my data" path).</summary>
		public static void Reset()
		{
			m_s_loaded = true;
			m_s_highScore = 0;
			m_s_distanceMeters = 0f;
			m_s_buildingsDestroyed = 0;
			m_s_bestCombo = 0;
			m_s_runsFlown = 0;
			m_s_perfectCrashes = 0;

			Save();
			Changed?.Invoke();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the cache and the handlers.
			m_s_loaded = false;
			Changed = null;
		}

		private static void Save()
		{
			PlayerPrefs.SetInt(k_HighScoreKey, m_s_highScore);
			PlayerPrefs.SetFloat(k_DistanceKey, m_s_distanceMeters);
			PlayerPrefs.SetInt(k_BuildingsKey, m_s_buildingsDestroyed);
			PlayerPrefs.SetInt(k_BestComboKey, m_s_bestCombo);
			PlayerPrefs.SetInt(k_RunsKey, m_s_runsFlown);
			PlayerPrefs.SetInt(k_PerfectKey, m_s_perfectCrashes);
			PlayerPrefs.Save();
		}

		private static void EnsureLoaded()
		{
			if (m_s_loaded) return;
			m_s_loaded = true;

			m_s_highScore = Mathf.Max(0, PlayerPrefs.GetInt(k_HighScoreKey, 0));
			m_s_distanceMeters = Mathf.Max(0f, PlayerPrefs.GetFloat(k_DistanceKey, 0f));
			m_s_buildingsDestroyed = Mathf.Max(0, PlayerPrefs.GetInt(k_BuildingsKey, 0));
			m_s_bestCombo = Mathf.Max(0, PlayerPrefs.GetInt(k_BestComboKey, 0));
			m_s_runsFlown = Mathf.Max(0, PlayerPrefs.GetInt(k_RunsKey, 0));
			m_s_perfectCrashes = Mathf.Max(0, PlayerPrefs.GetInt(k_PerfectKey, 0));
		}

		private static int SaturatingAdd(int total, int amount) => amount > int.MaxValue - total ? int.MaxValue : total + amount;
	}
}
