using Alchemy.Inspector;
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
			view.ApplyAttemptState(GameOverPlaceholderData.DefaultAttemptState);

			view.ActionClicked += HandleActionClicked;
			view.RankRetryClicked += HandleRankRetryClicked;
			view.RankClicked += ShowArrivesLaterToast;
		}

		private void HandleRunEnded(RunEndCause cause)
		{
			var score = 0;
			if (m_scoreSystem != null) score = m_scoreSystem.CurrentScore;
			m_lastScore = score;

			if (View != null)
			{
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

			// The chip starts as just the number. A better rank counts down to it, then the chip widens to bring in the
			// arrow (no place count, the service does not report one) and the attempt status (static until attempts
			// exist); same or worse: the rank alone (no red, no loss framing).
			var state = GameOverPlaceholderData.DefaultAttemptState;
			var style = state == GameOverAttemptState.FinalAttempt ? RankChipStyle.Final : RankChipStyle.Amber;
			View.ShowRankResult(result.Rank, result.Trend == RankTrend.Up, 0, GameOverView.DefaultRankCaption, GameOverPlaceholderData.RankStatusFor(state), style, true);
		}

		// RETRY (and RETRY NOW, the same thing in the placed-well state) is the only real action: the scene reload.
		// BANK IT, PRACTICE, MAIN MENU, LEADERBOARD and SHARE wait for attempts, the run / menu flow and Pass 6.
		private void HandleActionClicked(GameOverAction action)
		{
			if (action == GameOverAction.Retry || action == GameOverAction.RetryNow)
			{
				SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
				return;
			}

			ShowArrivesLaterToast();
		}

		private void ShowArrivesLaterToast()
		{
			View?.ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
		}
	}
}
