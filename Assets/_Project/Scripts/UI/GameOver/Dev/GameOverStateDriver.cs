using System;
using Opoint8182.Daily;
using Opoint8182.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the Game Over screen: shows it with sample values and jumps between the mockup states, end
	/// causes and rank chip states with hotkeys so each can be checked without playing to it. Lives in the GameOverGallery
	/// scene only.
	/// Keys: A / S / D / F / G the button-row states (A attempt 1 with a strong run, S attempt 2 that did not place, D the
	/// last attempt, F practice, G practice with no time left) with their score, chips, rank and button row; 1-5 end cause;
	/// N toggle NEW BEST; M cycle the rank chip (ranking, ranked + arrow, attempts left, done today, unranked + gap, rank
	/// only, failed + retry, unavailable, hidden); K replay the entry cascade; H hide / show the whole screen (show replays
	/// the cascade). The rows come from GameOverRow.Derive, the same code play uses.
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

			ApplyMockupState(SampleRow(RunMode.Competitive, 2, 1500f), true);
			view.Show();
		}

		private void Update()
		{
			if (View == null) return;

			var keyboard = Keyboard.current;
			if (keyboard == null) return;

			if (keyboard.aKey.wasPressedThisFrame) ApplyMockupState(SampleRow(RunMode.Competitive, 2, 1500f), true, true);
			if (keyboard.sKey.wasPressedThisFrame) ApplyMockupState(SampleRow(RunMode.Competitive, 1, 1500f), false, true);
			if (keyboard.dKey.wasPressedThisFrame) ApplyMockupState(SampleRow(RunMode.Competitive, 0, 1500f), true, true);
			if (keyboard.fKey.wasPressedThisFrame) ApplyMockupState(SampleRow(RunMode.Practice, 0, 1500f), true, true);
			if (keyboard.gKey.wasPressedThisFrame) ApplyMockupState(SampleRow(RunMode.Practice, 0, 0f), true, true);

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

		// The 184,920 run is the strong one (NEW BEST); the 96,310 run did not place (YOUR BEST chip, unranked with the gap).
		// A practice run drops the rank. The row is derived the way play derives it, from a mode, attempts left and practice time.
		private static GameOverRow SampleRow(RunMode mode, int attemptsLeft, float practiceSeconds)
		{
			var now = new DateTime(2026, 10, 28, 17, 0, 0, DateTimeKind.Utc);
			return GameOverRow.Derive(mode, mode == RunMode.Practice, attemptsLeft, practiceSeconds, now.Date.AddDays(1), now);
		}

		private void ApplyMockupState(GameOverRow row, bool strong, bool replay = false)
		{
			View.SetCause(RunEndCause.OutOfFuel);
			View.SetScore(strong ? 184920 : 96310);
			if (strong) View.SetStats("2.4 km", "38", "×7", 286);
			else View.SetStats("1.3 km", "19", "×4", 148);

			m_newBest = strong;
			View.SetNewBest(strong);
			View.SetYourBest(strong ? string.Empty : GameOverPlaceholderData.SampleBest);

			View.ApplyAttemptState(row);

			m_rankState = row.State == GameOverAttemptState.Practice ? 8 : !strong ? 4 : row.State == GameOverAttemptState.FinalAttempt ? 3 : 2;
			ApplyRankState();
			if (replay) View.Show();
		}

		// 0 RANKING skeleton, 1 ranked (up arrow, no place count: what play shows), 2 ranked with attempts left today,
		// 3 ranked with none left, 4 unranked with the gap, 5 rank only (no arrow: same or worse), 6 failed + retry,
		// 7 unavailable, 8 hidden.
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
					View.ShowRankResult(k_SampleRank, true, GameOverPlaceholderData.SampleRankDelta, GameOverView.DefaultRankCaption, DailyPlayText.RankStatus(2), RankChipStyle.Amber, true);
					break;
				case 3:
					View.ShowRankResult(k_SampleRank, true, GameOverPlaceholderData.SampleRankDelta, GameOverView.DefaultRankCaption, DailyPlayText.RankStatus(0), RankChipStyle.Amber, true);
					break;
				case 4:
					View.ShowRankUnranked(GameOverPlaceholderData.SampleGapStatus);
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
