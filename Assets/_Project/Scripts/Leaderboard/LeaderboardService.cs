using System;
using System.Collections.Generic;
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

	/// <summary>Up is a new placement or a better rank. A worse rank is never reported as Down: it shows the rank alone.</summary>
	public enum RankTrend
	{
		Up,
		Same
	}

	public readonly struct SubmitResult
	{
		public readonly bool Success;
		/// <summary>1-based rank for display (UGS ranks are zero-based).</summary>
		public readonly int Rank;
		public readonly int Score;
		public readonly RankTrend Trend;
		/// <summary>
		/// True when the run did not beat the player's stored weekly best, so nothing was sent for the weekly board:
		/// <see cref="Rank"/> and <see cref="Score"/> are then the last known weekly ones (rank 0 if there are none) and the
		/// trend is Same.
		/// </summary>
		public readonly bool Skipped;

		public SubmitResult(bool success, int rank, int score, RankTrend trend, bool skipped = false)
		{
			Success = success;
			Rank = rank;
			Score = score;
			Trend = trend;
			Skipped = skipped;
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
	/// Monday 00:00 UTC, archived) and all-time. Submits a run's score (skipping boards it cannot improve) and reports
	/// the player's weekly rank plus its trend vs. their previous entry this week, reads the live
	/// boards and the weekly version info, with a short in-memory cache so screens never cost a call per open.
	/// Never throws to callers; failures come back as <see cref="SubmitResult.Success"/> == false.
	/// Look it up with <c>TryGetInstance()</c> so scenes opened without Bootstrap degrade quietly.
	/// </summary>
	public class LeaderboardService : PersistentSingleton<LeaderboardService>
	{
		private const string k_LogPrefix = "[LeaderboardService] ";

		// The local, soft copy of the player's best per board (clearing storage only costs a redundant submit), used to skip
		// submits that cannot improve it. The weekly best is only valid for the version it was set in.
		private const string k_WeeklyBestKey = "lb_weekly_best";
		private const string k_WeeklyVersionKey = "lb_weekly_version";
		private const string k_WeeklyRankKey = "lb_weekly_rank";
		private const string k_AllTimeBestKey = "lb_alltime_best";

		// Keys of the old daily board (rank + UTC date), removed on startup.
		private const string k_OldRankKey = "lb_last_rank";
		private const string k_OldDateKey = "lb_last_utc_date";

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
			PlayerPrefs.DeleteKey(k_OldRankKey);
			PlayerPrefs.DeleteKey(k_OldDateKey);

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

		/// <summary>
		/// Submits a finished run's score. Each board is only sent the score when it beats the player's stored best for it
		/// (the weekly best is keyed to the live weekly version, so a new week starts from none); the weekly board goes first
		/// and its result is the one returned, the all-time board is sent best-effort in the background. A run that does
		/// not beat the weekly best comes back as <see cref="SubmitResult.Skipped"/> with the last known weekly rank and no
		/// call is made. Scores of zero or less are not submitted; a failed weekly submit changes nothing stored, so a retry
		/// sends it again.
		/// </summary>
		public async Task<SubmitResult> SubmitScoreAsync(int score)
		{
			if (score <= 0 || IsOffline()) return SubmitResult.Failed;

			try
			{
				// One call per session (and per reset); a failure only means the week check is skipped and the stored values are trusted.
				var versions = await GetWeeklyVersionsAsync();
				string liveVersion = versions.Success ? versions.LiveVersionId : null;
				bool sameWeek = liveVersion == null || PlayerPrefs.GetString(k_WeeklyVersionKey, string.Empty) == liveVersion;
				int weeklyBest = sameWeek ? PlayerPrefs.GetInt(k_WeeklyBestKey, 0) : 0;
				int weeklyRank = sameWeek ? PlayerPrefs.GetInt(k_WeeklyRankKey, 0) : 0;

				SubmitResult result;
				if (ShouldSubmit(score, weeklyBest))
				{
					var weekly = await WithTimeout(_ => SubmitBoardAsync(m_weeklyBoardId, score), BoardSubmit.Failed);
					if (!weekly.Success) return SubmitResult.Failed;

					var trend = Compare(weeklyRank > 0, weeklyRank, weekly.Rank);
					PlayerPrefs.SetInt(k_WeeklyBestKey, weekly.Score);
					PlayerPrefs.SetInt(k_WeeklyRankKey, weekly.Rank);
					if (liveVersion != null) PlayerPrefs.SetString(k_WeeklyVersionKey, liveVersion);
					PlayerPrefs.Save();
					InvalidateLiveBoards();
					result = new SubmitResult(true, weekly.Rank, weekly.Score, trend);
				}
				else
				{
					result = new SubmitResult(true, weeklyRank, weeklyBest, RankTrend.Same, true);
				}

				if (ShouldSubmit(score, PlayerPrefs.GetInt(k_AllTimeBestKey, 0))) _ = SubmitAllTimeAsync(score);
				return result;
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Submit failed: {e.GetType().Name}: {e.Message}");
				return SubmitResult.Failed;
			}
		}

		/// <summary>True when a score can improve the stored best, so a submit is worth a call.</summary>
		public static bool ShouldSubmit(int score, int storedBest) => score > storedBest;

		private readonly struct BoardSubmit
		{
			public readonly bool Success;
			/// <summary>1-based rank.</summary>
			public readonly int Rank;
			/// <summary>The player's best on the board after the submit (the server keeps the best), not necessarily the score sent.</summary>
			public readonly int Score;

			public BoardSubmit(bool success, int rank, int score)
			{
				Success = success;
				Rank = rank;
				Score = score;
			}

			public static BoardSubmit Failed => new(false, 0, 0);
		}

		private async Task<BoardSubmit> SubmitBoardAsync(string boardId, int score)
		{
			try
			{
				await EnsureReadyAsync();
				var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(boardId, score);
				return new BoardSubmit(true, entry.Rank + 1, (int)entry.Score);
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Submit to {boardId} failed: {e.GetType().Name}: {e.Message}");
				return BoardSubmit.Failed;
			}
		}

		// Best-effort: the run's result does not wait for it and a failure only means the next qualifying run sends it again.
		private async Task SubmitAllTimeAsync(int score)
		{
			var allTime = await WithTimeout(_ => SubmitBoardAsync(m_allTimeBoardId, score), BoardSubmit.Failed);
			if (!allTime.Success) return;

			PlayerPrefs.SetInt(k_AllTimeBestKey, allTime.Score);
			PlayerPrefs.Save();
			InvalidateLiveBoards();
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

		/// <summary>A first entry in the week is Up (new placement), a lower rank number is Up; anything else, a worse rank included, is Same (never shown as a drop).</summary>
		public static RankTrend Compare(bool hasPrevious, int previousRank, int newRank)
		{
			return !hasPrevious || newRank < previousRank ? RankTrend.Up : RankTrend.Same;
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

		private void SetState(ServiceState state)
		{
			if (State == state) return;
			State = state;
			StateChanged?.Invoke(state);
		}
	}
}
