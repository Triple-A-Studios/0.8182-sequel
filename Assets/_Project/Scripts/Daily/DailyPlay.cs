using System;
using UnityEngine;

namespace Opoint8182.Daily
{
	/// <summary>How the current run counts: a leaderboard attempt, practice, or no run started through the menu (a scene opened on its own).</summary>
	public enum RunMode
	{
		None,
		Competitive,
		Practice,
	}

	public enum BeginRunResult
	{
		Started,
		NoAttemptsLeft,
		NoPracticeTimeLeft,
	}

	/// <summary>
	/// The time source for the daily counters. The device clock today; Daily play structure Pass 7 swaps in server time
	/// (the attempt day is a UTC day and a device clock can be changed to regain attempts).
	/// </summary>
	public interface IDailyClock
	{
		DateTime UtcNow { get; }
	}

	public sealed class SystemDailyClock : IDailyClock
	{
		public DateTime UtcNow => DateTime.UtcNow;
	}

	/// <summary>A clock set by hand, for checks and dev tools (<see cref="DailyPlay.Clock"/>).</summary>
	public sealed class ManualDailyClock : IDailyClock
	{
		public DateTime UtcNow { get; set; }

		public ManualDailyClock(DateTime utcNow)
		{
			UtcNow = utcNow;
		}
	}

	/// <summary>
	/// The daily limits: <see cref="ATTEMPTS_MAX"/> leaderboard attempts per UTC day and <see cref="PRACTICE_CAP_SECONDS"/> of practice
	/// per UTC day, plus the mode of the run in progress. Static, like <see cref="SettingsService"/>: it needs no scene object, works from
	/// Bootstrap, the menu, a standalone game scene and the galleries, and survives the game scene reloads (a restart, a return to the menu).
	/// <para>Everything counted lives under one UTC day key and resets when the day changes, so unused attempts and practice time never
	/// bank. The key never goes backwards (the highest day seen is kept), so setting the clock back does not hand attempts back; this is
	/// still a soft cap until server time (Pass 7). Values load lazily, are clamped on load, and are written by <see cref="Flush"/>,
	/// which also runs on focus loss and quit.</para>
	/// <para>An attempt is spent when a run begins (<see cref="BeginRun"/>), not when it ends: a kill, a quit or an abandoned run all
	/// count. The practice cap is soft: <see cref="TickPractice"/> keeps counting past it so the run in progress finishes, and only
	/// the next practice <see cref="BeginRun"/> is refused.</para>
	/// </summary>
	public static class DailyPlay
	{
		public const int ATTEMPTS_MAX = 3;
		public const float PRACTICE_CAP_SECONDS = 30f * 60f;

		private const string k_DayKey = "daily.day";
		private const string k_AttemptsUsedKey = "daily.attempts_used";
		private const string k_PracticeSecondsKey = "daily.practice_seconds";
		private const string k_DontAskDayKey = "daily.dont_ask_day";

		// Practice time is added every frame; the disk write waits for this much of it (or a Flush).
		private const float k_PracticeSaveInterval = 10f;

		private static readonly DateTime m_S_Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static bool m_s_loaded;
		private static bool m_s_dirty;
		private static int m_s_day;
		private static int m_s_attemptsUsed;
		private static float m_s_practiceSeconds;
		private static int m_s_dontAskDay;
		private static float m_s_unsavedPracticeSeconds;

		private static IDailyClock m_s_clock = new SystemDailyClock();

		/// <summary>Raised when the counters or the run mode changed (a run began or ended, a new day started), not on every practice tick.</summary>
		public static event Action Changed;

		/// <summary>The time source. Settable so Pass 7 can use server time (and so a check can fake the day).</summary>
		public static IDailyClock Clock
		{
			get => m_s_clock;
			set
			{
				Flush();
				m_s_clock = value ?? new SystemDailyClock();
				m_s_loaded = false;
			}
		}

		/// <summary>The mode of the run in progress (None until a run is begun through <see cref="BeginRun"/>).</summary>
		public static RunMode Mode { get; private set; }

		/// <summary>Which attempt of the day the current run is (1 to <see cref="ATTEMPTS_MAX"/>); 0 for a practice run or no run.</summary>
		public static int CurrentAttemptNumber { get; private set; }

		/// <summary>
		/// The mode of the last run begun this session, kept after the run ends so the menu can come back on the same tab (None
		/// before the first run).
		/// </summary>
		public static RunMode LastMode { get; private set; }

		public static bool IsPracticeRun => Mode == RunMode.Practice;

		/// <summary>
		/// Whether the run in progress is a leaderboard attempt. True for a run with no recorded start (a scene opened on its own),
		/// so standalone testing still submits; false for practice.
		/// </summary>
		public static bool RunCountsAsAttempt => Mode != RunMode.Practice;

		public static int AttemptsUsed
		{
			get { RollDay(); return m_s_attemptsUsed; }
		}

		public static int AttemptsLeft => Mathf.Max(0, ATTEMPTS_MAX - AttemptsUsed);

		public static float PracticeUsedSeconds
		{
			get { RollDay(); return m_s_practiceSeconds; }
		}

		public static float PracticeRemainingSeconds => Mathf.Max(0f, PRACTICE_CAP_SECONDS - PracticeUsedSeconds);

		/// <summary>When the counters reset: the next UTC midnight (by <see cref="Clock"/>).</summary>
		public static DateTime NextResetUtc
		{
			get { RollDay(); return m_S_Epoch.AddDays(m_s_day + 1); }
		}

		/// <summary>The attempt-confirm popup's "Don't ask again today": true until the UTC day changes.</summary>
		public static bool DontAskAgainToday
		{
			get { RollDay(); return m_s_dontAskDay == m_s_day; }
			set
			{
				RollDay();
				var day = value ? m_s_day : 0;
				if (day == m_s_dontAskDay) return;

				m_s_dontAskDay = day;
				m_s_dirty = true;
				Flush();
			}
		}

		/// <summary>Whether a run of this mode could begin now (so the menu can disable PLAY without spending anything).</summary>
		public static BeginRunResult CanBeginRun(RunMode mode)
		{
			switch (mode)
			{
				case RunMode.Competitive: return AttemptsLeft > 0 ? BeginRunResult.Started : BeginRunResult.NoAttemptsLeft;
				case RunMode.Practice: return PracticeRemainingSeconds > 0f ? BeginRunResult.Started : BeginRunResult.NoPracticeTimeLeft;
				default: return BeginRunResult.Started;
			}
		}

		/// <summary>
		/// Starts a run of the given mode. A competitive run spends an attempt and is refused when none are left; a practice run is
		/// refused when the day's practice time is used up. Callers start the run only on <see cref="BeginRunResult.Started"/>.
		/// </summary>
		public static BeginRunResult BeginRun(RunMode mode)
		{
			var result = CanBeginRun(mode);
			if (result != BeginRunResult.Started) return result;

			// The previous run's practice time is committed before the mode changes.
			Flush();

			Mode = mode;
			LastMode = mode;
			CurrentAttemptNumber = 0;
			if (mode == RunMode.Competitive)
			{
				m_s_attemptsUsed++;
				CurrentAttemptNumber = m_s_attemptsUsed;
				m_s_dirty = true;
				Flush();
			}

			Changed?.Invoke();
			return BeginRunResult.Started;
		}

		/// <summary>The run is over (ended, restarted away from, or left for the menu): commits practice time and clears the mode.</summary>
		public static void EndRun()
		{
			var hadRun = Mode != RunMode.None;
			Mode = RunMode.None;
			CurrentAttemptNumber = 0;
			Flush();
			if (hadRun) Changed?.Invoke();
		}

		/// <summary>
		/// Adds practice time for a frame of a practice run that is playing (the caller gates on Playing, run active and not paused,
		/// and passes unscaled time). It keeps counting past the cap so the run in progress is not cut off.
		/// </summary>
		public static void TickPractice(float unscaledDeltaTime)
		{
			if (Mode != RunMode.Practice || unscaledDeltaTime <= 0f) return;

			RollDay();
			m_s_practiceSeconds += unscaledDeltaTime;
			m_s_unsavedPracticeSeconds += unscaledDeltaTime;
			m_s_dirty = true;
			if (m_s_unsavedPracticeSeconds >= k_PracticeSaveInterval) Flush();
		}

		/// <summary>Writes pending changes to <see cref="PlayerPrefs"/> and saves. No-op when nothing changed.</summary>
		public static void Flush()
		{
			if (!m_s_dirty) return;

			PlayerPrefs.SetInt(k_DayKey, m_s_day);
			PlayerPrefs.SetInt(k_AttemptsUsedKey, m_s_attemptsUsed);
			PlayerPrefs.SetFloat(k_PracticeSecondsKey, m_s_practiceSeconds);
			PlayerPrefs.SetInt(k_DontAskDayKey, m_s_dontAskDay);
			PlayerPrefs.Save();

			m_s_dirty = false;
			m_s_unsavedPracticeSeconds = 0f;
		}

		/// <summary>
		/// Gives today's attempts and practice time back (and forgets "don't ask again today"). For testing and dev tools only; it
		/// touches no other saved value.
		/// </summary>
		public static void ResetToday()
		{
			PlayerPrefs.DeleteKey(k_DayKey);
			PlayerPrefs.DeleteKey(k_AttemptsUsedKey);
			PlayerPrefs.DeleteKey(k_PracticeSecondsKey);
			PlayerPrefs.DeleteKey(k_DontAskDayKey);
			PlayerPrefs.Save();
			ReloadFromPrefs();
			Changed?.Invoke();
		}

		/// <summary>Drops the in-memory copy so the next read loads from <see cref="PlayerPrefs"/> again (for checks and dev tools).</summary>
		public static void ReloadFromPrefs()
		{
			m_s_loaded = false;
			m_s_dirty = false;
			m_s_unsavedPracticeSeconds = 0f;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the cache and the run, re-hook without stacking handlers.
			m_s_loaded = false;
			m_s_dirty = false;
			m_s_unsavedPracticeSeconds = 0f;
			m_s_clock = new SystemDailyClock();
			Mode = RunMode.None;
			LastMode = RunMode.None;
			CurrentAttemptNumber = 0;
			Changed = null;

			Application.focusChanged -= OnFocusChanged;
			Application.focusChanged += OnFocusChanged;
			Application.quitting -= Flush;
			Application.quitting += Flush;
		}

		private static void OnFocusChanged(bool hasFocus)
		{
			if (!hasFocus) Flush();
		}

		private static int TodayKey()
		{
			return (int)Math.Floor((m_s_clock.UtcNow - m_S_Epoch).TotalDays);
		}

		// Loads on first use and rolls to the current day: a new day zeroes everything counted. The day key is the later of
		// "today by the clock" and the last one seen, so a clock set back does not return to an earlier day's fresh counters.
		private static void RollDay()
		{
			if (!m_s_loaded)
			{
				m_s_day = PlayerPrefs.GetInt(k_DayKey, 0);
				m_s_attemptsUsed = Mathf.Clamp(PlayerPrefs.GetInt(k_AttemptsUsedKey, 0), 0, ATTEMPTS_MAX);
				m_s_practiceSeconds = Mathf.Max(0f, PlayerPrefs.GetFloat(k_PracticeSecondsKey, 0f));
				m_s_dontAskDay = PlayerPrefs.GetInt(k_DontAskDayKey, 0);
				m_s_loaded = true;
			}

			var today = Math.Max(TodayKey(), m_s_day);
			if (today == m_s_day) return;

			m_s_day = today;
			m_s_attemptsUsed = 0;
			m_s_practiceSeconds = 0f;
			m_s_unsavedPracticeSeconds = 0f;
			m_s_dirty = true;
			Flush();
			Changed?.Invoke();
		}
	}
}
