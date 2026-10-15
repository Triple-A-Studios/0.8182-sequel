using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Last week's results (mockup S11): the closed week's top five on the left, the player's finish on the right with
	/// SHARE MY RESULT (only when they have a ranked run) and CONTINUE. Plain class over the instance's root, like the
	/// menu screens. Shown by <see cref="LaunchFlowHost"/> between loading and the menu; all data comes in as a
	/// <see cref="LastWeekResult"/>. SHARE shows the arrives-later toast (share belongs to the Daily play structure
	/// milestone, and it only ever runs on a tap).
	/// </summary>
	public class LastWeekResultsScreen
	{
		private const float k_ToastSeconds = 2.5f;

		private readonly VisualElement m_root;
		private readonly Label m_range;
		private readonly VisualElement m_rows;
		private readonly VisualElement m_card;
		private readonly Label m_rank;
		private readonly Label m_pilots;
		private readonly Label m_score;
		private readonly Label m_name;
		private readonly LipButton m_share;
		private readonly VisualElement m_toast;
		private readonly Label m_toastLabel;

		private IVisualElementScheduledItem m_toastHide;
		private TaskCompletionSource<bool> m_continued;

		/// <summary>CONTINUE was tapped.</summary>
		public event Action ContinueRequested;

		/// <summary>SHARE MY RESULT was tapped (the screen itself shows the toast).</summary>
		public event Action ShareRequested;

		public bool IsVisible => m_root.resolvedStyle.display != DisplayStyle.None;

		public bool ShareVisible => m_share.style.display != DisplayStyle.None;

		public LastWeekResultsScreen(VisualElement root)
		{
			m_root = root;
			m_range = root.Q<Label>("lw-range");
			m_rows = root.Q<VisualElement>("lw-rows");
			m_card = root.Q<VisualElement>("lw-card");
			m_rank = root.Q<Label>("lw-rank");
			m_pilots = root.Q<Label>("lw-pilots");
			m_score = root.Q<Label>("lw-score");
			m_name = root.Q<Label>("lw-name");
			m_share = root.Q<LipButton>("lw-share");
			m_toast = root.Q<VisualElement>("lw-toast");
			m_toastLabel = root.Q<Label>("lw-toast-label");

			m_share.clicked += OnShare;
			root.Q<LipButton>("lw-continue").clicked += OnContinue;

			Hide();
		}

		public void Show(LastWeekResult result)
		{
			m_range.text = result.RangeText.ToUpperInvariant();

			BuildRows(result);

			m_card.EnableInClassList("lw-card--norank", !result.HasRank);
			if (result.HasRank)
			{
				m_rank.text = $"#{result.Rank}";
				m_pilots.text = $"of {result.Pilots.ToString("N0", CultureInfo.InvariantCulture)} pilots · top {result.TopPercent}%";
				m_score.text = result.BestScore.ToString("N0", CultureInfo.InvariantCulture);
				m_name.text = $"{result.PlayerName} · best run of the week";
			}
			else
			{
				m_rank.text = "–";
				m_pilots.text = "no ranked run last week";
				m_score.text = "–";
				m_name.text = $"{result.PlayerName} · fly a ranked run to place";
			}

			m_share.style.display = result.HasRank ? DisplayStyle.Flex : DisplayStyle.None;
			m_toast.style.display = DisplayStyle.None;
			m_root.style.display = DisplayStyle.Flex;
		}

		/// <summary>Shows the result and completes when CONTINUE is tapped.</summary>
		public Task ShowAndWaitAsync(LastWeekResult result)
		{
			m_continued = new TaskCompletionSource<bool>();
			Show(result);
			return m_continued.Task;
		}

		public void Hide()
		{
			m_root.style.display = DisplayStyle.None;
		}

		private void BuildRows(LastWeekResult result)
		{
			m_rows.Clear();

			var winners = result.Winners;
			for (var i = 0; i < winners.Count; i++)
			{
				var row = new VisualElement { pickingMode = PickingMode.Ignore };
				row.AddToClassList("lw-row");
				row.AddToClassList(i switch
				{
					0 => "lw-row--gold",
					1 => "lw-row--silver",
					2 => "lw-row--bronze",
					_ => "lw-row--plain",
				});

				row.Add(MakeLabel(winners[i].Rank.ToString(CultureInfo.InvariantCulture), "lw-row__rank"));
				row.Add(MakeLabel(winners[i].Name, "lw-row__name"));
				row.Add(MakeLabel(winners[i].Score.ToString("N0", CultureInfo.InvariantCulture), "lw-row__score"));
				m_rows.Add(row);
			}
		}

		private void OnShare()
		{
			ShareRequested?.Invoke();

			m_toastLabel.text = GameOverPlaceholderData.ArrivesLaterMessage;
			m_toast.style.display = DisplayStyle.Flex;

			m_toastHide?.Pause();
			m_toastHide = m_toast.schedule.Execute(() => m_toast.style.display = DisplayStyle.None);
			m_toastHide.ExecuteLater((long)(k_ToastSeconds * 1000f));
		}

		private void OnContinue()
		{
			ContinueRequested?.Invoke();
			m_continued?.TrySetResult(true);
			m_continued = null;
		}

		private static Label MakeLabel(string text, string className)
		{
			var label = new Label(text) { pickingMode = PickingMode.Ignore };
			label.AddToClassList(className);
			return label;
		}
	}
}
