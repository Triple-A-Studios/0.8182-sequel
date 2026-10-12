using Opoint8182.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the Game Over screen: shows it with sample values and jumps between the mockup states, end
	/// causes and rank chip states with hotkeys so each can be checked without playing to it. Lives in the GameOverGallery
	/// scene only.
	/// Keys: A / S / D / F the mockup states (S05 a placed well, b did not place, c final attempt, practice) with their
	/// score, chips, rank and button row; 1-5 end cause; N toggle NEW BEST; M cycle the rank chip (ranking, ranked + arrow,
	/// provisional, final, unranked + gap, rank only, failed + retry, unavailable, hidden); K replay the entry cascade;
	/// H hide / show the whole screen (show replays the cascade).
	/// </summary>
	public class GameOverStateDriver : GameOverHost
	{
		private const int k_RankStateCount = 9;
		// Under 200 on purpose: the count-down starts at rank + clamp(2 * rank, 25, 400), so a small sample rank keeps the
		// whole count-down short and easy to watch.
		private const int k_SampleRank = 87;

		private bool m_newBest;
		private int m_rankState;

		protected override void OnViewReady(GameOverView view)
		{
			view.ActionClicked += OnActionClicked;
			view.RankClicked += ShowArrivesLaterToast;
			view.RankRetryClicked += () => View?.ShowRanking();

			ApplyMockupState(GameOverAttemptState.PlacedWell);
			view.Show();
		}

		private void Update()
		{
			if (View == null) return;

			var keyboard = Keyboard.current;
			if (keyboard == null) return;

			if (keyboard.aKey.wasPressedThisFrame) ApplyMockupState(GameOverAttemptState.PlacedWell, true);
			if (keyboard.sKey.wasPressedThisFrame) ApplyMockupState(GameOverAttemptState.DidNotPlace, true);
			if (keyboard.dKey.wasPressedThisFrame) ApplyMockupState(GameOverAttemptState.FinalAttempt, true);
			if (keyboard.fKey.wasPressedThisFrame) ApplyMockupState(GameOverAttemptState.Practice, true);

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

			if (keyboard.kKey.wasPressedThisFrame) View.Show();

			if (keyboard.hKey.wasPressedThisFrame)
			{
				if (View.IsVisible) View.Hide();
				else View.Show();
			}
		}

		// Mockup S05: a is the 184,920 run that beat the best, b the 96,310 run that did not place (YOUR BEST chip, unranked
		// with the gap), c the same strong run on the last attempt (rank locked), practice drops the attempt row and the rank.
		private void ApplyMockupState(GameOverAttemptState state, bool replay = false)
		{
			var strong = state != GameOverAttemptState.DidNotPlace;

			View.SetCause(RunEndCause.OutOfFuel);
			View.SetScore(strong ? 184920 : 96310);
			if (strong) View.SetStats("2.4 km", "38", "×7", 286);
			else View.SetStats("1.3 km", "19", "×4", 148);

			var showBest = state == GameOverAttemptState.PlacedWell || state == GameOverAttemptState.FinalAttempt;
			m_newBest = showBest;
			View.SetNewBest(showBest);
			View.SetYourBest(state == GameOverAttemptState.DidNotPlace ? GameOverPlaceholderData.SampleBest : string.Empty);

			View.ApplyAttemptState(state);

			switch (state)
			{
				case GameOverAttemptState.PlacedWell:
					m_rankState = 2;
					break;
				case GameOverAttemptState.DidNotPlace:
					m_rankState = 4;
					break;
				case GameOverAttemptState.FinalAttempt:
					m_rankState = 3;
					break;
				default:
					m_rankState = 8;
					break;
			}

			ApplyRankState();
			if (replay) View.Show();
		}

		// 0 RANKING skeleton, 1 ranked (up arrow, no place count: what play shows), 2 provisional (a), 3 final (c),
		// 4 unranked with the gap (b), 5 rank only (no arrow: same or worse), 6 failed + retry, 7 unavailable, 8 hidden.
		private void ApplyRankState()
		{
			switch (m_rankState)
			{
				case 0:
					View.ShowRanking();
					break;
				case 1:
					View.ShowRankResult(k_SampleRank, true, 0, GameOverView.DefaultRankCaption, "", RankChipStyle.Amber, true);
					break;
				case 2:
					View.ShowRankResult(k_SampleRank, true, GameOverPlaceholderData.SampleRankDelta, "TODAY'S RANK", GameOverPlaceholderData.SampleProvisionalStatus, RankChipStyle.Amber, true);
					break;
				case 3:
					View.ShowRankResult(k_SampleRank, true, GameOverPlaceholderData.SampleRankDelta, "FINAL RANK", GameOverPlaceholderData.SampleFinalStatus, RankChipStyle.Final, true);
					break;
				case 4:
					View.ShowRankUnranked(GameOverPlaceholderData.SampleGapStatus, "TODAY'S RANK");
					break;
				case 5:
					View.ShowRankResult(k_SampleRank, false, 0, GameOverView.DefaultRankCaption, "", RankChipStyle.Amber, true);
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

		private void OnActionClicked(GameOverAction action)
		{
			ShowArrivesLaterToast();
		}

		private void ShowArrivesLaterToast()
		{
			View?.ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
		}
	}
}
