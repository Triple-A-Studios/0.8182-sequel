using System;
using System.Threading.Tasks;
using Opoint8182.Server;
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

		/// <summary>A competitive run needs the server (it spends the attempt and issues the ticket) and it could not be reached.</summary>
		ServerUnavailable,
	}

	/// <summary>
	/// The time source for the daily counters (the attempt day is a UTC day and a device clock can be changed to regain attempts).
	/// <see cref="ServerDailyClock"/> by default: server time once the server has answered, the device clock before that.
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

		private static IDailyClock m_s_clock = new ServerDailyClock();

		// Competitive attempts as the server last reported them (Pass 7c). -1 = not known yet. The server day is the UTC day number the
		// count belongs to, so a count from yesterday is treated as unknown once the day rolls.
		private static int m_s_serverUsed = -1;
		private static long m_s_serverDay = -1;
		private static long m_s_serverNextResetMs;
		private const long k_DayMs = 86400000L;

		/// <summary>Raised when the counters or the run mode changed (a run began or ended, a new day started), not on every practice tick.</summary>
		public static event Action Changed;

		/// <summary>The time source. Settable so a check can fake the day.</summary>
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

		/// <summary>
		/// The server's ticket for the competitive run in progress (<see cref="BeginRunAsync"/>), which its score is submitted with; null
		/// for practice or no run. Kept until the run ends.
		/// </summary>
		public static string CurrentTicketId { get; private set; }

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

		/// <summary>
		/// Whether the server's attempt count for today is known (a server reply arrived and the UTC day has not rolled since). Competitive
		/// play needs it; practice does not.
		/// </summary>
		public static bool IsServerStateKnown => m_s_serverUsed >= 0 && TodayKey() <= m_s_serverDay;

		/// <summary>Attempts spent today as the server last reported them; -1 while <see cref="IsServerStateKnown"/> is false.</summary>
		public static int ServerAttemptsUsed => IsServerStateKnown ? m_s_serverUsed : -1;

		/// <summary>Attempts left today by the server's count; 0 while it is not known.</summary>
		public static int ServerAttemptsLeft => IsServerStateKnown ? Mathf.Max(0, ATTEMPTS_MAX - m_s_serverUsed) : 0;

		/// <summary>The next reset by the server's clock (UTC midnight); the local one while the server state is not known.</summary>
		public static DateTime ServerNextResetUtc => IsServerStateKnown ? m_S_Epoch.AddMilliseconds(m_s_serverNextResetMs) : NextResetUtc;

		/// <summary>
		/// Takes the server's word for today: attempts spent, the server time (which also anchors <see cref="Clock"/> when it is a
		/// <see cref="ServerDailyClock"/>) and the next reset. Raises <see cref="Changed"/>.
		/// </summary>
		public static void ApplyServerState(int attemptsUsed, long nowMs, long nextResetMs)
		{
			if (m_s_clock is ServerDailyClock serverClock) serverClock.Sync(nowMs);

			m_s_serverUsed = Mathf.Clamp(attemptsUsed, 0, ATTEMPTS_MAX);
			m_s_serverDay = nowMs / k_DayMs;
			m_s_serverNextResetMs = nextResetMs;
			Changed?.Invoke();
		}

		/// <summary>Asks the server for today's attempts and applies them. False when it could not be reached (the known state is left as it was).</summary>
		public static async Task<bool> RefreshFromServerAsync()
		{
			var reply = await DailyServer.GetStateAsync();
			if (!reply.Success) return false;

			ApplyServerState(reply.AttemptsUsed, reply.NowMs, reply.NextResetMs);
			return true;
		}

		/// <summary>
		/// Starts a run of the given mode, asking the server for a competitive one: <c>StartRun</c> spends the attempt and issues the
		/// ticket (<see cref="CurrentTicketId"/>), <see cref="BeginRunResult.NoAttemptsLeft"/> comes back when the server says none are
		/// left and <see cref="BeginRunResult.ServerUnavailable"/> when it could not be reached (nothing is spent). Practice is the
		/// synchronous <see cref="BeginRun"/>.
		/// </summary>
		public static async Task<BeginRunResult> BeginRunAsync(RunMode mode)
		{
			if (mode != RunMode.Competitive) return BeginRun(mode);

			var reply = await DailyServer.StartRunAsync();
			if (reply.Status == StartRunStatus.Unavailable) return BeginRunResult.ServerUnavailable;

			ApplyServerState(reply.AttemptsUsed, reply.NowMs, reply.NextResetMs);
			if (reply.Status == StartRunStatus.NoAttempts) return BeginRunResult.NoAttemptsLeft;

			// The previous run's practice time is committed before the mode changes.
			Flush();
			Mode = RunMode.Competitive;
			LastMode = RunMode.Competitive;
			CurrentAttemptNumber = reply.AttemptNumber;
			CurrentTicketId = reply.TicketId;
			Changed?.Invoke();
			return BeginRunResult.Started;
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
			CurrentTicketId = null;
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
			m_s_serverUsed = -1;
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
			m_s_clock = new ServerDailyClock();
			m_s_serverUsed = -1;
			m_s_serverDay = -1;
			CurrentTicketId = null;
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
