using Alchemy.Inspector;
using Opoint8182.Bootstrap;
using Opoint8182.Daily;
using Opoint8182.Game;
using Opoint8182.Leaderboard;
using Opoint8182.Score;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opoint8182.UI
{
	/// <summary>
	/// Presenter for the Game Over screen: shows it when the run ends (with the real end cause and score), submits the
	/// score to the leaderboard and reports the rank, and handles the buttons. Layout and styling live in
	/// <see cref="GameOverView"/> / <c>GameOver.uxml</c>.
	/// </summary>
	public class GameOverUI : GameOverHost
	{
		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private ScoreSystem m_scoreSystem;

		private const string k_UnavailableText = "Leaderboard unavailable";
		private const string k_SubmitFailedText = "Couldn't submit score";

		private GameManager m_gameManager;
		private int m_submitToken;
		private int m_lastScore;
		private GameOverRow m_row;
		private bool m_retrying;

		protected override void OnEnable()
		{
			base.OnEnable();

			// The HUD object (this object's parent) gets SetActive(false)/(true) across the Bootstrap menu <-> play
			// transition, which re-fires OnEnable/OnDisable on every toggle but only ever fires Start() once. Subscribing
			// here too (once m_gameManager is cached, see Start() below) is what makes the second and later re-enables
			// still receive RunEnded; Start() alone missed a real bug where the menu round-trip silently dropped the
			// subscription and Game Over never showed.
			if (m_gameManager != null) m_gameManager.RunEnded += HandleRunEnded;
		}

		private void Start()
		{
			// Unity only guarantees every object's Awake() runs before any object's Start(), NOT before every other
			// object's OnEnable(). GenericSingleton<T> sets its instance in Awake, so subscribing from OnEnable risked
			// running before GameManager's own Awake and silently never subscribing. TryGetInstance() (not Instance) so a
			// missing GameManager fails quietly instead of auto-instantiating a stand-in that never receives RunEnded.
			m_gameManager = GameManager.TryGetInstance();
			if (m_gameManager != null) m_gameManager.RunEnded += HandleRunEnded;
		}

		protected override void OnDisable()
		{
			if (m_gameManager != null) m_gameManager.RunEnded -= HandleRunEnded;
			base.OnDisable();
		}

		protected override void OnViewReady(GameOverView view)
		{
			view.SetNewBest(GameOverPlaceholderData.ShowNewBest);
			view.SetStats(GameOverPlaceholderData.Distance, GameOverPlaceholderData.Flattened, GameOverPlaceholderData.BestCombo, GameOverPlaceholderData.Earned);
			view.ApplyAttemptState(GameOverAttemptState.Competitive);

			view.ActionClicked += HandleActionClicked;
			view.RankRetryClicked += HandleRankRetryClicked;
			view.RankClicked += OpenLeaderboard;
		}

		private void HandleRunEnded(RunEndCause cause)
		{
			var score = 0;
			if (m_scoreSystem != null) score = m_scoreSystem.CurrentScore;
			m_lastScore = score;

			// Attempts are spent at run start, so what is left now is what the row and the rank status report.
			m_row = GameOverRow.Derive(DailyPlay.Mode, m_gameManager != null && m_gameManager.IsPracticeRun, DailyPlay.AttemptsLeft,
				DailyPlay.PracticeRemainingSeconds, DailyPlay.NextResetUtc, DailyPlay.Clock.UtcNow);

			if (View != null)
			{
				View.ApplyAttemptState(m_row);
				View.SetCause(cause);
				View.SetScore(score);
				View.Show();
			}

			SubmitAndShowRank(score);
		}

		private void HandleRankRetryClicked()
		{
			SubmitAndShowRank(m_lastScore);
		}

		private async void SubmitAndShowRank(int score)
		{
			// Each run end (or retry) bumps the token so a result that lands after a newer attempt (or after the scene
			// reloaded under it) is dropped instead of overwriting the chip.
			var token = ++m_submitToken;
			View?.HideRank();

			if (score <= 0) return;

			// A practice run never reaches a board and has no rank to show (IsPracticeRun follows the run's DailyPlay mode).
			if (m_gameManager != null && m_gameManager.IsPracticeRun) return;

			var service = LeaderboardService.TryGetInstance();
			if (service == null)
			{
				View?.ShowRankMessage(k_UnavailableText, false);
				return;
			}

			// RANKING skeleton: the chip holds this state until the server answers; the buttons never wait for it.
			View?.ShowRanking();
			var result = await service.SubmitScoreAsync(score);
			if (this == null || token != m_submitToken || View == null) return;

			if (!result.Success)
			{
				View.ShowRankMessage(k_SubmitFailedText, true);
				return;
			}

			// A run that did not beat the weekly best was not sent: it shows the last known weekly rank, or nothing when
			// there is none (a cleared device).
			if (result.Skipped && result.Rank <= 0) return;

			// The chip starts as just the number. A better rank counts down to it, then the chip widens to bring in the
			// arrow (no place count, the service does not report one) and the attempts status ("THIS WEEK · 2 LEFT TODAY");
			// same or worse: the rank alone (no red, no loss framing).
			View.ShowRankResult(result.Rank, result.Trend == RankTrend.Up, 0, GameOverView.DefaultRankCaption, m_row.RankStatus, RankChipStyle.Amber, true);
		}

		// RETRY reloads the scene; MAIN MENU reloads it into the menu. PRACTICE starts a practice run, LEADERBOARD (and the rank chip) reload into the menu on the leaderboard screen; SHARE waits for Pass 8.
		private void HandleActionClicked(GameOverAction action)
		{
			if (action == GameOverAction.Retry)
			{
				if (m_gameManager == null) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
				else Retry();
				return;
			}

			// PRACTICE starts a practice run at once, no confirm; refused (the day's practice time is gone) it says when that returns.
			if (action == GameOverAction.Practice && m_gameManager != null)
			{
				if (!m_gameManager.StartPracticeRun()) View?.ShowToast(DailyPlayText.RefusedLine(RunMode.Practice));
				return;
			}

			// Without a Bootstrap loader (the game scene opened on its own) there is no menu: fall through to the toast.
			if (BootstrapLoader.Instance != null)
			{
				if (action == GameOverAction.MainMenu)
				{
					BootstrapLoader.Instance.ReturnToMenu();
					return;
				}

				if (action == GameOverAction.Leaderboard)
				{
					OpenLeaderboard();
					return;
				}
			}

			ShowArrivesLaterToast();
		}

		// A competitive RETRY spends the next attempt on the server (so it takes a moment; taps are ignored meanwhile), a practice one
		// needs practice time; refused or unreachable, nothing is spent or reloaded and a neutral line says why.
		private async void Retry()
		{
			if (m_retrying) return;

			m_retrying = true;
			try
			{
				var mode = DailyPlay.Mode;
				var result = await m_gameManager.RestartRunAsync();
				if (result == BeginRunResult.Started || this == null || View == null) return;

				View.ShowToast(DailyPlayText.StartRefusedLine(result, mode));
			}
			finally
			{
				m_retrying = false;
			}
		}

		// The leaderboard screen lives in the menu scene: go back to the menu and land on it.
		private void OpenLeaderboard()
		{
			if (BootstrapLoader.Instance != null) BootstrapLoader.Instance.ReturnToLeaderboard();
			else ShowArrivesLaterToast();
		}

		private void ShowArrivesLaterToast()
		{
			View?.ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
		}
	}
}
