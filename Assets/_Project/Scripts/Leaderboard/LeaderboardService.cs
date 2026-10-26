using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using PrimeTween;
using TripleA.Utils.Singletons;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace Opoint8182.Leaderboard
{
	public enum ServiceState
	{
		Idle,
		Initializing,
		Ready,
		Failed
	}

	/// <summary>The two live boards: the weekly one (resets Monday 00:00 UTC, archived) and the all-time one.</summary>
	public enum LeaderboardBoard
	{
		Weekly,
		AllTime
	}

	public enum RankTrend
	{
		Up,
		Down,
		Same
	}

	public readonly struct SubmitResult
	{
		public readonly bool Success;
		/// <summary>1-based rank for display (UGS ranks are zero-based).</summary>
		public readonly int Rank;
		public readonly int Score;
		public readonly RankTrend Trend;

		public SubmitResult(bool success, int rank, int score, RankTrend trend)
		{
			Success = success;
			Rank = rank;
			Score = score;
			Trend = trend;
		}

		public static SubmitResult Failed => new(false, 0, 0, RankTrend.Same);
	}

	public readonly struct LeaderboardRow
	{
		/// <summary>1-based rank for display.</summary>
		public readonly int Rank;
		public readonly string Name;
		public readonly int Score;
		public readonly bool IsSelf;

		public LeaderboardRow(int rank, string name, int score, bool isSelf)
		{
			Rank = rank;
			Name = name;
			Score = score;
			IsSelf = isSelf;
		}
	}

	public readonly struct TopScoresResult
	{
		public readonly bool Success;
		public readonly IReadOnlyList<LeaderboardRow> Rows;
		/// <summary>The player's own row, only when it is not already inside <see cref="Rows"/>; null if they have no entry yet.</summary>
		public readonly LeaderboardRow? Self;
		/// <summary>How many players have an entry on the board (the "of N pilots" number); 0 when the board is empty or the count is unknown.</summary>
		public readonly int Total;

		public TopScoresResult(bool success, IReadOnlyList<LeaderboardRow> rows, LeaderboardRow? self, int total = 0)
		{
			Success = success;
			Rows = rows;
			Self = self;
			Total = total;
		}

		public static TopScoresResult Failed => new(false, Array.Empty<LeaderboardRow>(), null);
	}

	/// <summary>One closed week of the weekly board.</summary>
	public readonly struct ArchivedWeek
	{
		/// <summary>The version ID to pass to the archive calls. Only ever use IDs that came from <see cref="WeeklyVersions"/>.</summary>
		public readonly string Id;
		public readonly DateTime StartUtc;
		public readonly DateTime EndUtc;

		public ArchivedWeek(string id, DateTime startUtc, DateTime endUtc)
		{
			Id = id;
			StartUtc = startUtc;
			EndUtc = endUtc;
		}
	}

	/// <summary>The weekly board's version info from one <c>GetVersionsAsync</c> call: the live version, the next reset and the newest closed weeks.</summary>
	public readonly struct WeeklyVersions
	{
		public readonly bool Success;
		public readonly string LiveVersionId;
		/// <summary>When the live version closes (the Monday 00:00 UTC reset); null if the board reports none.</summary>
		public readonly DateTime? NextResetUtc;
		public readonly int TotalArchived;
		/// <summary>Newest first, at most <see cref="LeaderboardService.ArchivedWeeksLimit"/>.</summary>
		public readonly IReadOnlyList<ArchivedWeek> Archived;

		public WeeklyVersions(bool success, string liveVersionId, DateTime? nextResetUtc, int totalArchived, IReadOnlyList<ArchivedWeek> archived)
		{
			Success = success;
			LiveVersionId = liveVersionId;
			NextResetUtc = nextResetUtc;
			TotalArchived = totalArchived;
			Archived = archived;
		}

		public static WeeklyVersions Failed => new(false, null, null, 0, Array.Empty<ArchivedWeek>());
	}

	/// <summary>
	/// Thin wrapper over Unity Leaderboards (UGS) with anonymous sign-in. Two boards: weekly (Best Score, resets
	/// Monday 00:00 UTC, archived) and all-time. Submits a run's score and reports the player's rank plus its trend
	/// vs. their previous attempt (still the weekly board only until the submit rework, Pass 3b), reads the live
	/// boards and the weekly version info, with a short in-memory cache so screens never cost a call per open.
	/// Never throws to callers; failures come back as <see cref="SubmitResult.Success"/> == false.
	/// Look it up with <c>TryGetInstance()</c> so scenes opened without Bootstrap degrade quietly.
	/// </summary>
	public class LeaderboardService : PersistentSingleton<LeaderboardService>
	{
		private const string k_LogPrefix = "[LeaderboardService] ";
		private const string k_LastRankKey = "lb_last_rank";
		private const string k_LastDateKey = "lb_last_utc_date";
		private const string k_DateFormat = "yyyy-MM-dd";

		/// <summary>How many closed weeks the leaderboard screen can step back through (a display limit: the archives keep accumulating).</summary>
		public const int ArchivedWeeksLimit = 4;

		// Live boards are re-read at most this often; archives never change, so the versions result lives until the next reset.
		private const float k_LiveCacheSeconds = 90f;

		[SerializeField] private string m_weeklyBoardId = "weekly_scores";
		[SerializeField] private string m_allTimeBoardId = "alltime_scores";
		[SerializeField] private float m_requestTimeoutSeconds = 10f;

		private sealed class TopCacheEntry
		{
			public Task<TopScoresResult> Task;
			public float StoredAt;
		}

		private readonly Dictionary<(LeaderboardBoard board, int limit), TopCacheEntry> m_topCache = new();
		private Task<WeeklyVersions> m_versionsTask;
		private Task m_readyTask;

		public ServiceState State { get; private set; } = ServiceState.Idle;
		public event Action<ServiceState> StateChanged;

		private async void Start()
		{
			try
			{
				await EnsureReadyAsync();
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Startup sign-in failed, will retry on next use: {e.GetType().Name}: {e.Message}");
			}
		}

		/// <summary>Idempotent: initializes UGS and signs in anonymously. Concurrent callers share one in-flight attempt; a failed attempt is retried on the next call.</summary>
		public Task EnsureReadyAsync()
		{
			if (State == ServiceState.Ready) return Task.CompletedTask;
			if (m_readyTask == null || State == ServiceState.Failed) m_readyTask = InitializeAsync();
			return m_readyTask;
		}

		/// <summary>Submits a finished run's score. Scores of zero or less are not submitted.</summary>
		public Task<SubmitResult> SubmitScoreAsync(int score)
		{
			if (score <= 0 || IsOffline()) return Task.FromResult(SubmitResult.Failed);
			return WithTimeout(attempt => SubmitCoreAsync(score, attempt), SubmitResult.Failed);
		}

		private async Task<SubmitResult> SubmitCoreAsync(int score, Attempt attempt)
		{
			try
			{
				await EnsureReadyAsync();
				var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(m_weeklyBoardId, score);
				// A request that finished after its timeout must not record a rank: the player's retry
				// would then compare against it and show Same instead of the real trend.
				if (attempt.TimedOut) return SubmitResult.Failed;
				int rank = entry.Rank + 1;
				return new SubmitResult(true, rank, (int)entry.Score, ResolveTrend(rank));
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Submit failed: {e.GetType().Name}: {e.Message}");
				return SubmitResult.Failed;
			}
		}

		/// <summary>The weekly board's top scores; the same as <c>GetTopScoresAsync(LeaderboardBoard.Weekly, limit)</c>.</summary>
		public Task<TopScoresResult> GetTopScoresAsync(int limit = 10) => GetTopScoresAsync(LeaderboardBoard.Weekly, limit);

		/// <summary>
		/// Fetches the top <paramref name="limit"/> entries of a live board plus the player's own row when it falls outside
		/// them, and the board's entry count. A successful result is reused for <see cref="k_LiveCacheSeconds"/> and
		/// concurrent callers share one request; a failure is never cached, so a retry asks again.
		/// </summary>
		public Task<TopScoresResult> GetTopScoresAsync(LeaderboardBoard board, int limit = 10)
		{
			if (IsOffline()) return Task.FromResult(TopScoresResult.Failed);

			var key = (board, limit);
			if (m_topCache.TryGetValue(key, out var cached)
			    && (!cached.Task.IsCompleted || Time.realtimeSinceStartup - cached.StoredAt < k_LiveCacheSeconds))
			{
				return cached.Task;
			}

			var entry = new TopCacheEntry { StoredAt = Time.realtimeSinceStartup };
			entry.Task = FetchTopScoresAsync(key, entry);
			m_topCache[key] = entry;
			return entry.Task;
		}

		/// <summary>Drops the cached live-board reads, so the next open fetches fresh (called after a successful submit).</summary>
		public void InvalidateLiveBoards() => m_topCache.Clear();

		private async Task<TopScoresResult> FetchTopScoresAsync((LeaderboardBoard board, int limit) key, TopCacheEntry entry)
		{
			var result = await WithTimeout(_ => GetTopScoresCoreAsync(BoardId(key.board), key.limit), TopScoresResult.Failed);
			entry.StoredAt = Time.realtimeSinceStartup;
			if (!result.Success && m_topCache.TryGetValue(key, out var current) && current == entry) m_topCache.Remove(key);
			return result;
		}

		private async Task<TopScoresResult> GetTopScoresCoreAsync(string boardId, int limit)
		{
			try
			{
				await EnsureReadyAsync();
				string selfId = AuthenticationService.Instance.PlayerId;
				var page = await LeaderboardsService.Instance.GetScoresAsync(boardId, new GetScoresOptions { Limit = limit });

				var rows = new List<LeaderboardRow>(page.Results.Count);
				bool selfInTop = false;
				foreach (var entry in page.Results)
				{
					bool isSelf = entry.PlayerId == selfId;
					selfInTop |= isSelf;
					rows.Add(ToRow(entry, isSelf));
				}

				LeaderboardRow? self = selfInTop ? null : await TryGetOwnRowAsync(boardId);
				return new TopScoresResult(true, rows, self, page.Total);
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Fetch top scores ({boardId}) failed: {e.GetType().Name}: {e.Message}");
				return TopScoresResult.Failed;
			}
		}

		/// <summary>
		/// The weekly board's version info: the live version ID (what the stored weekly best is keyed to), the next reset
		/// and the newest <see cref="ArchivedWeeksLimit"/> closed weeks. One call, reused until the reset time passes (a
		/// closed week is then fetched again). A failure is not cached.
		/// </summary>
		public Task<WeeklyVersions> GetWeeklyVersionsAsync()
		{
			if (IsOffline()) return Task.FromResult(WeeklyVersions.Failed);

			if (m_versionsTask != null && (!m_versionsTask.IsCompleted || IsVersionsCurrent(m_versionsTask.Result)))
			{
				return m_versionsTask;
			}

			m_versionsTask = FetchVersionsAsync();
			return m_versionsTask;
		}

		private static bool IsVersionsCurrent(WeeklyVersions versions)
		{
			return versions.Success && (!versions.NextResetUtc.HasValue || DateTime.UtcNow < versions.NextResetUtc.Value);
		}

		private Task<WeeklyVersions> FetchVersionsAsync()
		{
			return WithTimeout(_ => GetVersionsCoreAsync(), WeeklyVersions.Failed);
		}

		private async Task<WeeklyVersions> GetVersionsCoreAsync()
		{
			try
			{
				await EnsureReadyAsync();
				var versions = await LeaderboardsService.Instance.GetVersionsAsync(m_weeklyBoardId,
					new GetVersionsOptions { Limit = ArchivedWeeksLimit });

				var archived = new List<ArchivedWeek>(versions.Results.Count);
				foreach (var version in versions.Results)
				{
					archived.Add(new ArchivedWeek(version.Id, version.Start.ToUniversalTime(), version.End.ToUniversalTime()));
				}

				// The model's NextReset is a plain DateTime: unset (a board with no schedule) reads as its default.
				DateTime? nextReset = versions.NextReset == default ? null : versions.NextReset.ToUniversalTime();
				return new WeeklyVersions(true, versions.VersionId, nextReset, versions.TotalArchivedVersions, archived);
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Fetch weekly versions failed: {e.GetType().Name}: {e.Message}");
				return WeeklyVersions.Failed;
			}
		}

		private string BoardId(LeaderboardBoard board) => board == LeaderboardBoard.AllTime ? m_allTimeBoardId : m_weeklyBoardId;

		/// <summary>First attempt of the UTC day is always Up (new placement); otherwise a lower rank number is Up.</summary>
		public static RankTrend Compare(bool hasPreviousToday, int previousRank, int newRank)
		{
			if (!hasPreviousToday) return RankTrend.Up;
			if (newRank < previousRank) return RankTrend.Up;
			if (newRank > previousRank) return RankTrend.Down;
			return RankTrend.Same;
		}

		private async Task InitializeAsync()
		{
			SetState(ServiceState.Initializing);
			try
			{
				if (UnityServices.State != ServicesInitializationState.Initialized)
					await UnityServices.InitializeAsync();
				if (!AuthenticationService.Instance.IsSignedIn)
					await AuthenticationService.Instance.SignInAnonymouslyAsync();
				SetState(ServiceState.Ready);
			}
			catch
			{
				SetState(ServiceState.Failed);
				throw;
			}
		}

		// Skips the request entirely when the device reports no connectivity, so the UI fails fast
		// instead of waiting on a hanging request.
		private static bool IsOffline()
		{
			return Application.internetReachability == NetworkReachability.NotReachable;
		}

		private sealed class Attempt
		{
			public bool TimedOut;
		}

		/// <summary>
		/// Races <paramref name="operation"/> against <see cref="m_requestTimeoutSeconds"/>, returning
		/// <paramref name="onTimeout"/> if the timer wins. The timer is PrimeTween's <c>Tween.Delay</c>, which runs
		/// off PrimeTween's update loop (no threads), so it works on WebGL where <c>Task.Delay</c> does not.
		/// The abandoned operation keeps running; it is told it timed out via <see cref="Attempt"/> so it skips side effects.
		/// </summary>
		private async Task<T> WithTimeout<T>(Func<Attempt, Task<T>> operation, T onTimeout)
		{
			var attempt = new Attempt();
			var timeoutSource = new TaskCompletionSource<T>();
			Tween timer = Tween.Delay(m_requestTimeoutSeconds, () =>
			{
				attempt.TimedOut = true;
				timeoutSource.TrySetResult(onTimeout);
			}, useUnscaledTime: true);

			Task<T> operationTask = operation(attempt);
			Task<T> winner = await Task.WhenAny(operationTask, timeoutSource.Task);
			timer.Stop();

			if (attempt.TimedOut)
			{
				Debug.LogWarning(k_LogPrefix + $"Request timed out after {m_requestTimeoutSeconds:0.#}s");
				// A hung UGS init would otherwise leave every retry waiting on the same dead task.
				if (State == ServiceState.Initializing)
				{
					m_readyTask = null;
					SetState(ServiceState.Failed);
				}
			}
			return await winner;
		}

		// GetPlayerScoreAsync throws EntryNotFound (code 27009) when the player has no entry yet, which is a normal state
		// here. Any other failure is a real error and goes to the caller, so it is not mistaken for "no entry".
		private async Task<LeaderboardRow?> TryGetOwnRowAsync(string boardId)
		{
			try
			{
				var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(boardId);
				return ToRow(entry, true);
			}
			catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
			{
				return null;
			}
		}

		private static LeaderboardRow ToRow(LeaderboardEntry entry, bool isSelf)
		{
			return new LeaderboardRow(entry.Rank + 1, entry.PlayerName, (int)entry.Score, isSelf);
		}

		private RankTrend ResolveTrend(int rank)
		{
			string today = DateTime.UtcNow.ToString(k_DateFormat, CultureInfo.InvariantCulture);
			bool hasPreviousToday = PlayerPrefs.GetString(k_LastDateKey, string.Empty) == today && PlayerPrefs.HasKey(k_LastRankKey);
			RankTrend trend = Compare(hasPreviousToday, PlayerPrefs.GetInt(k_LastRankKey, 0), rank);

			PlayerPrefs.SetInt(k_LastRankKey, rank);
			PlayerPrefs.SetString(k_LastDateKey, today);
			PlayerPrefs.Save();
			return trend;
		}

		private void SetState(ServiceState state)
		{
			if (State == state) return;
			State = state;
			StateChanged?.Invoke(state);
		}
	}
}
