using Opoint8182.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the Game Over screen: shows it with a fixed sample score and jumps between the end causes
	/// and rank chip states with hotkeys so each can be checked without playing to it. Lives in the GameOverGallery
	/// scene only. Each Game Over sub-pass adds its states here.
	/// Keys: 1-5 cause (out of fuel, wrecked, flew too high, hit the deck, out of bounds), N toggle NEW BEST,
	/// M cycle the rank chip (ranking, ranked + arrow, provisional, final, unranked + gap, rank only, failed + retry,
	/// unavailable, hidden), H hide / show the whole screen.
	/// </summary>
	public class GameOverStateDriver : GameOverHost
	{
		private const int k_SampleScore = 184920;
		private const int k_SampleRank = 412;
		private const int k_RankStateCount = 9;

		private bool m_newBest;
		private int m_rankState = -1;

		protected override void OnViewReady(GameOverView view)
		{
			view.SetScore(k_SampleScore);
			view.SetCause(RunEndCause.OutOfFuel);
			view.SetStats(GameOverPlaceholderData.Distance, GameOverPlaceholderData.Flattened, GameOverPlaceholderData.BestCombo, GameOverPlaceholderData.Earned);
			view.SetRetryNote(GameOverPlaceholderData.RetryNote);
			view.SetNewBest(false);

			view.MainMenuClicked += ShowArrivesLaterToast;
			view.LeaderboardClicked += ShowArrivesLaterToast;
			view.ShareClicked += ShowArrivesLaterToast;
			view.RetryClicked += ShowArrivesLaterToast;
			view.RankClicked += ShowArrivesLaterToast;
			view.RankRetryClicked += () => View?.ShowRanking();

			m_rankState = 0;
			ApplyRankState();
			view.Show();
		}

		private void Update()
		{
			if (View == null) return;

			var keyboard = Keyboard.current;
			if (keyboard == null) return;

			if (keyboard.digit1Key.wasPressedThisFrame) View.SetCause(RunEndCause.OutOfFuel);
			if (keyboard.digit2Key.wasPressedThisFrame) View.SetCause(RunEndCause.Wrecked);
			if (keyboard.digit3Key.wasPressedThisFrame) View.SetCause(RunEndCause.FlewTooHigh);
			if (keyboard.digit4Key.wasPressedThisFrame) View.SetCause(RunEndCause.HitTheDeck);
			if (keyboard.digit5Key.wasPressedThisFrame) View.SetCause(RunEndCause.OutOfBounds);

			if (keyboard.nKey.wasPressedThisFrame)
			{
				m_newBest = !m_newBest;
				View.SetNewBest(m_newBest);
			}

			if (keyboard.mKey.wasPressedThisFrame)
			{
				m_rankState = (m_rankState + 1) % k_RankStateCount;
				ApplyRankState();
			}

			if (keyboard.hKey.wasPressedThisFrame)
			{
				if (View.IsVisible) View.Hide();
				else View.Show();
			}
		}

		// Mockup S05: 0 RANKING skeleton, 1 ranked (up arrow, no place count: what play shows), 2 provisional (a),
		// 3 final (c), 4 unranked with the gap (b), 5 rank only (no arrow: same or worse), 6 failed + retry, 7 unavailable, 8 hidden.
		private void ApplyRankState()
		{
			switch (m_rankState)
			{
				case 0:
					View.ShowRanking();
					break;
				case 1:
					View.ShowRankResult(k_SampleRank, true);
					break;
				case 2:
					View.ShowRankResult(k_SampleRank, true, GameOverPlaceholderData.SampleRankDelta, "TODAY'S RANK", GameOverPlaceholderData.SampleProvisionalStatus);
					break;
				case 3:
					View.ShowRankResult(k_SampleRank, true, GameOverPlaceholderData.SampleRankDelta, "FINAL RANK", GameOverPlaceholderData.SampleFinalStatus, RankChipStyle.Final);
					break;
				case 4:
					View.ShowRankUnranked(GameOverPlaceholderData.SampleGapStatus, "TODAY'S RANK");
					break;
				case 5:
					View.ShowRankResult(k_SampleRank, false);
					break;
				case 6:
					View.ShowRankMessage("Couldn't submit score", true);
					break;
				case 7:
					View.ShowRankMessage("Leaderboard unavailable", false);
					break;
				default:
					View.HideRank();
					break;
			}
		}

		private void ShowArrivesLaterToast()
		{
			View?.ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
		}
	}
}
