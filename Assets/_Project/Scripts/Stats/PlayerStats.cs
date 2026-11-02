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

		/// <summary>Tough buildings broken properly (a mistimed hit does not count).</summary>
		public readonly int ToughBroken;

		/// <summary>Heal buildings that restored health.</summary>
		public readonly int HealsUsed;

		/// <summary>The most perfect crashes in a row in the run.</summary>
		public readonly int BestPerfectStreak;

		/// <summary>Time spent flying (paused time excluded).</summary>
		public readonly float DurationSeconds;

		/// <summary>Coins collected in the run. Nothing collects coins yet (the currency arrives with the Beta shop milestone), so it is 0.</summary>
		public readonly int Coins;

		public RunStats(int score, float distanceMeters, int buildingsDestroyed, int bestCombo, int perfectCrashes, bool isPractice,
			int toughBroken = 0, int healsUsed = 0, int bestPerfectStreak = 0, float durationSeconds = 0f, int coins = 0)
		{
			Coins = Mathf.Max(0, coins);
			ToughBroken = Mathf.Max(0, toughBroken);
			HealsUsed = Mathf.Max(0, healsUsed);
			BestPerfectStreak = Mathf.Max(0, bestPerfectStreak);
			DurationSeconds = Mathf.Max(0f, durationSeconds);
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
		private const string k_ToughKey = "stats.tough";
		private const string k_HealsKey = "stats.heals";
		private const string k_PerfectStreakKey = "stats.perfect_streak";
		private const string k_LongestRunKey = "stats.longest_run_s";
		private const string k_TotalScoreKey = "stats.total_score";
		private const string k_CoinsKey = "stats.coins";

		private static bool m_s_loaded;

		private static int m_s_highScore;
		private static float m_s_distanceMeters;
		private static int m_s_buildingsDestroyed;
		private static int m_s_bestCombo;
		private static int m_s_runsFlown;
		private static int m_s_perfectCrashes;
		private static int m_s_toughBroken;
		private static int m_s_healsUsed;
		private static int m_s_bestPerfectStreak;
		private static float m_s_longestRunSeconds;
		private static int m_s_totalScore;
		private static int m_s_coinsCollected;

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

		/// <summary>Tough buildings broken properly, over all runs.</summary>
		public static int ToughBroken { get { EnsureLoaded(); return m_s_toughBroken; } }

		/// <summary>Heal buildings that restored health, over all runs.</summary>
		public static int HealsUsed { get { EnsureLoaded(); return m_s_healsUsed; } }

		/// <summary>The most perfect crashes in a row in any one run.</summary>
		public static int BestPerfectStreak { get { EnsureLoaded(); return m_s_bestPerfectStreak; } }

		/// <summary>The longest time spent flying in any one run.</summary>
		public static float LongestRunSeconds { get { EnsureLoaded(); return m_s_longestRunSeconds; } }

		/// <summary>Points scored over all runs, practice included.</summary>
		public static int TotalScore { get { EnsureLoaded(); return m_s_totalScore; } }

		/// <summary>Coins collected over all runs (earned, not the balance). Stays 0 until the currency exists.</summary>
		public static int CoinsCollected { get { EnsureLoaded(); return m_s_coinsCollected; } }

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
			m_s_toughBroken = SaturatingAdd(m_s_toughBroken, run.ToughBroken);
			m_s_healsUsed = SaturatingAdd(m_s_healsUsed, run.HealsUsed);
			m_s_bestPerfectStreak = Mathf.Max(m_s_bestPerfectStreak, run.BestPerfectStreak);
			m_s_longestRunSeconds = Mathf.Max(m_s_longestRunSeconds, run.DurationSeconds);
			m_s_totalScore = SaturatingAdd(m_s_totalScore, run.Score);
			m_s_coinsCollected = SaturatingAdd(m_s_coinsCollected, run.Coins);

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
			m_s_toughBroken = 0;
			m_s_healsUsed = 0;
			m_s_bestPerfectStreak = 0;
			m_s_longestRunSeconds = 0f;
			m_s_totalScore = 0;
			m_s_coinsCollected = 0;

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
			PlayerPrefs.SetInt(k_ToughKey, m_s_toughBroken);
			PlayerPrefs.SetInt(k_HealsKey, m_s_healsUsed);
			PlayerPrefs.SetInt(k_PerfectStreakKey, m_s_bestPerfectStreak);
			PlayerPrefs.SetFloat(k_LongestRunKey, m_s_longestRunSeconds);
			PlayerPrefs.SetInt(k_TotalScoreKey, m_s_totalScore);
			PlayerPrefs.SetInt(k_CoinsKey, m_s_coinsCollected);
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
			m_s_toughBroken = Mathf.Max(0, PlayerPrefs.GetInt(k_ToughKey, 0));
			m_s_healsUsed = Mathf.Max(0, PlayerPrefs.GetInt(k_HealsKey, 0));
			m_s_bestPerfectStreak = Mathf.Max(0, PlayerPrefs.GetInt(k_PerfectStreakKey, 0));
			m_s_longestRunSeconds = Mathf.Max(0f, PlayerPrefs.GetFloat(k_LongestRunKey, 0f));
			m_s_totalScore = Mathf.Max(0, PlayerPrefs.GetInt(k_TotalScoreKey, 0));
			m_s_coinsCollected = Mathf.Max(0, PlayerPrefs.GetInt(k_CoinsKey, 0));
		}

		private static int SaturatingAdd(int total, int amount) => amount > int.MaxValue - total ? int.MaxValue : total + amount;
	}
}
