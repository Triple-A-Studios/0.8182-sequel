using System.Globalization;
using Opoint8182.UI;
using UnityEngine.UIElements;

namespace Opoint8182.Share
{
	/// <summary>
	/// The share card (<c>UI/ShareCard.uxml</c>): the game's own screen layouts (Game Over, the results screen, Profile)
	/// instanced side by side in one frame with a footer, of which <see cref="Apply"/> fills in and shows the one the data
	/// asks for and drops its buttons. The card is never shown on screen; <see cref="ShareCardRenderer"/> renders it to a
	/// texture. A plain class over the card's root, like the screens it drives.
	/// </summary>
	public class ShareCardView
	{
		private readonly VisualElement m_run;
		private readonly VisualElement m_rank;
		private readonly VisualElement m_profile;
		private readonly VisualElement m_practiceChip;

		public ShareCardView(VisualElement root)
		{
			m_run = root.Q<VisualElement>("sc-run");
			m_rank = root.Q<VisualElement>("sc-rank");
			m_profile = root.Q<VisualElement>("sc-profile");
			m_practiceChip = root.Q<VisualElement>("sc-practice-chip");
		}

		public void Apply(ShareCardData data)
		{
			SetDisplay(m_run, data.Kind == ShareCardKind.Run);
			SetDisplay(m_rank, data.Kind == ShareCardKind.Rank);
			SetDisplay(m_profile, data.Kind == ShareCardKind.Profile);
			SetDisplay(m_practiceChip, data.Kind == ShareCardKind.Run && data.IsPractice);

			switch (data.Kind)
			{
				case ShareCardKind.Run:
					ApplyRun(data);
					break;
				case ShareCardKind.Rank:
					ApplyRank(data);
					break;
				default:
					new ProfileScreen(m_profile.Q<VisualElement>("profile-root")).PrepareForShare();
					break;
			}
		}

		// Game Over exactly as play fills it: score, cause, the four stat chips, NEW BEST! or YOUR BEST, the rank chip (the player's
		// name where the remaining attempts go). No buttons, no cascade.
		private void ApplyRun(ShareCardData data)
		{
			var root = m_run.Q<VisualElement>("game-over-root");
			var view = new GameOverView(root, false);
			root.Q<VisualElement>("go-actions").style.display = DisplayStyle.None;

			view.SetCause(data.Cause);
			view.SetScore(data.Score);
			view.SetStats(data.Distance, data.Flattened, data.BestCombo, data.Earned);
			view.SetNewBest(data.IsNewBest);
			view.SetYourBest(!data.IsNewBest && data.BestScore > 0 ? data.BestScore.ToString("N0", CultureInfo.InvariantCulture) : null);

			if (data.WeeklyRank > 0) view.ShowRankResult(data.WeeklyRank, false, 0, GameOverView.DefaultRankCaption, data.PlayerName);
			else view.HideRank();

			view.Show(false);
		}

		private void ApplyRank(ShareCardData data)
		{
			var screen = new LastWeekResultsScreen(m_rank.Q<VisualElement>("results-root"));
			screen.Show(data.Result);
			screen.ApplyShareLabels(data.RankTitle, data.RankRange, data.RankTag, data.RankKicker, data.RankCaption);
			screen.HideButtons();
		}

		private static void SetDisplay(VisualElement element, bool visible)
		{
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}
