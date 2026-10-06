using System;
using System.Globalization;
using System.Threading.Tasks;
using TripleA.Utils.Singletons;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
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
		public async Task<SubmitResult> SubmitScoreAsync(int score)
		{
			if (score <= 0) return SubmitResult.Failed;

			try
			{
				await EnsureReadyAsync();
				var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(m_leaderboardId, score);
				int rank = entry.Rank + 1;
				return new SubmitResult(true, rank, (int)entry.Score, ResolveTrend(rank));
			}
			catch (Exception e)
			{
				Debug.LogWarning(k_LogPrefix + $"Submit failed: {e.GetType().Name}: {e.Message}");
				return SubmitResult.Failed;
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
