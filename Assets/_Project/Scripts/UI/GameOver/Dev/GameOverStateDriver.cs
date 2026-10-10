using Opoint8182.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the Game Over screen: shows it with a fixed sample score and jumps between the end causes
	/// with hotkeys so each can be checked without playing to it. Lives in the GameOverGallery scene only. Each Game
	/// Over sub-pass adds its states here.
	/// Keys: 1-5 cause (out of fuel, wrecked, flew too high, hit the deck, out of bounds), N toggle NEW BEST,
	/// M toggle a rank text sample, F toggle the failed-submit retry button, H hide / show the whole screen.
	/// </summary>
	public class GameOverStateDriver : GameOverHost
	{
		private const int k_SampleScore = 184920;

		private bool m_newBest;
		private bool m_rankSample;
		private bool m_rankRetry;

		protected override void OnViewReady(GameOverView view)
		{
			view.SetScore(k_SampleScore);
			view.SetCause(RunEndCause.OutOfFuel);
			view.SetStats(GameOverPlaceholderData.Distance, GameOverPlaceholderData.Flattened, GameOverPlaceholderData.BestCombo, GameOverPlaceholderData.Earned);
			view.SetRetryNote(GameOverPlaceholderData.RetryNote);
			view.SetNewBest(false);
			view.SetRank("#412 ▲", true);
			m_rankSample = true;

			view.MainMenuClicked += ShowArrivesLaterToast;
			view.LeaderboardClicked += ShowArrivesLaterToast;
			view.ShareClicked += ShowArrivesLaterToast;
			view.RetryClicked += ShowArrivesLaterToast;
			view.RankRetryClicked += ShowArrivesLaterToast;

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
				m_rankSample = !m_rankSample;
				View.SetRank(m_rankSample ? "#412 ▲" : string.Empty, m_rankSample);
			}

			if (keyboard.fKey.wasPressedThisFrame)
			{
				m_rankRetry = !m_rankRetry;
				View.SetRank(m_rankRetry ? "Couldn't submit score" : "#412", false);
				View.SetRankRetryVisible(m_rankRetry);
			}

			if (keyboard.hKey.wasPressedThisFrame)
			{
				if (View.IsVisible) View.Hide();
				else View.Show();
			}
		}

		private void ShowArrivesLaterToast()
		{
			View?.ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
		}
	}
}
