using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using PrimeTween;
using TripleA.Utils.Singletons;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
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

		public TopScoresResult(bool success, IReadOnlyList<LeaderboardRow> rows, LeaderboardRow? self)
		{
			Success = success;
			Rows = rows;
			Self = self;
		}

		public static TopScoresResult Failed => new(false, Array.Empty<LeaderboardRow>(), null);
	}

	/// <summary>
	/// Thin wrapper over Unity Leaderboards (UGS) with anonymous sign-in. Submits a run's score to the
	/// daily Best Score board and reports the player's rank plus its trend vs. their previous attempt.
	/// Never throws to callers; failures come back as <see cref="SubmitResult.Success"/> == false.
	/// Look it up with <c>TryGetInstance()</c> so scenes opened without Bootstrap degrade quietly.
	/// </summary>
	public class LeaderboardService : PersistentSingleton<LeaderboardService>
	{
		private const string k_LogPrefix = "[LeaderboardService] ";
		private const string k_LastRankKey = "lb_last_rank";
		private const string k_LastDateKey = "lb_last_utc_date";
		private const string k_DateFormat = "yyyy-MM-dd";

		[SerializeField] private string m_leaderboardId = "daily_scores";
		[SerializeField] private float m_requestTimeoutSeconds = 10f;

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
				var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(m_leaderboardId, score);
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

		/// <summary>Fetches the top <paramref name="limit"/> entries plus the player's own row when it falls outside them.</summary>
		public Task<TopScoresResult> GetTopScoresAsync(int limit = 10)
		{
			if (IsOffline()) return Task.FromResult(TopScoresResult.Failed);
			return WithTimeout(_ => GetTopScoresCoreAsync(limit), TopScoresResult.Failed);
		}

		private async Task<TopScoresResult> GetTopScoresCoreAsync(int limit)
		{
			try
			{
				await EnsureReadyAsync();
				string selfId = AuthenticationService.Instance.PlayerId;
				var page = await LeaderboardsService.Instance.GetScoresAsync(m_leaderboardId,
					new GetScoresOptions { Limit = limit });

				var rows = new List<LeaderboardRow>(page.Results.Count);
				bool selfInTop = false;
				foreach (var entry in page.Results)
				{
					bool isSelf = entry.PlayerId == selfId;
					selfInTop |= isSelf;
					rows.Add(ToRow(entry, isSelf));
				}

				LeaderboardRow? self = selfInTop ? null : await TryGetOwnRowAsync();
				return new TopScoresResult(true, rows, self);
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Fetch top scores failed: {e.GetType().Name}: {e.Message}");
				return TopScoresResult.Failed;
			}
		}

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

		// GetPlayerScoreAsync throws when the player has no entry yet, which is a normal state here.
		private async Task<LeaderboardRow?> TryGetOwnRowAsync()
		{
			try
			{
				var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(m_leaderboardId);
				return ToRow(entry, true);
			}
			catch (Exception)
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
