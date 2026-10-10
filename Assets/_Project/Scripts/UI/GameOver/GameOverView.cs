using System;
using Opoint8182.Game;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Pure UI for the Game Over screen (mockup S05): takes plain values through setters and knows nothing about the
	/// game systems, so the same view runs in play (driven by <see cref="GameOverUI"/>) and in the GameOverGallery
	/// (driven by <see cref="GameOverStateDriver"/>).
	/// </summary>
	public class GameOverView
	{
		private const long k_ToastDurationMs = 2500;
		private const string k_RankUpClass = "go-rank-text--up";

		private readonly Label m_causeLabel;
		private readonly Label m_score;
		private readonly VisualElement m_newBest;
		private readonly Label m_distance;
		private readonly Label m_flattened;
		private readonly Label m_combo;
		private readonly Label m_earned;
		private readonly VisualElement m_rankChip;
		private readonly Label m_rankText;
		private readonly LipButton m_rankRetry;
		private readonly Label m_retryNote;
		private readonly VisualElement m_toast;
		private readonly Label m_toastLabel;
		private IVisualElementScheduledItem m_toastHide;

		public VisualElement Root { get; }

		public bool IsVisible => Root.resolvedStyle.display == DisplayStyle.Flex;

		/// <summary>RETRY tapped: start a new run.</summary>
		public event Action RetryClicked;

		/// <summary>Leave for the main menu (stub until the run / menu session flow exists).</summary>
		public event Action MainMenuClicked;

		/// <summary>Open the leaderboard (stub until Pass 6).</summary>
		public event Action LeaderboardClicked;

		/// <summary>Share the result (stub).</summary>
		public event Action ShareClicked;

		/// <summary>The rank chip's own retry (a failed score submit): send the score again.</summary>
		public event Action RankRetryClicked;

		public GameOverView(VisualElement root)
		{
			Root = root;

			m_causeLabel = root.Q<Label>("go-cause-label");
			m_score = root.Q<Label>("go-score");
			m_newBest = root.Q<VisualElement>("go-new-best");
			m_distance = root.Q<Label>("go-stat-distance");
			m_flattened = root.Q<Label>("go-stat-flattened");
			m_combo = root.Q<Label>("go-stat-combo");
			m_earned = root.Q<Label>("go-stat-earned");
			m_rankChip = root.Q<VisualElement>("go-rank-chip");
			m_rankText = root.Q<Label>("go-rank-text");
			m_rankRetry = root.Q<LipButton>("go-rank-retry");
			m_retryNote = root.Q<Label>("go-retry-note");
			m_toast = root.Q<VisualElement>("go-toast");
			m_toastLabel = root.Q<Label>("go-toast-label");

			// No base padding: positions in GameOver.uss are measured from the screen edge like the mockup; the helper only adds device insets.
			SafeAreaHelper.Apply(root.Q<VisualElement>("go-safe"), 0f);

			root.Q<LipButton>("go-retry").clicked += () => RetryClicked?.Invoke();
			root.Q<LipButton>("go-main-menu").clicked += () => MainMenuClicked?.Invoke();
			root.Q<LipButton>("go-leaderboard").clicked += () => LeaderboardClicked?.Invoke();
			root.Q<LipButton>("go-share").clicked += () => ShareClicked?.Invoke();
			m_rankRetry.clicked += () => RankRetryClicked?.Invoke();

			Hide();
			SetRank(string.Empty, false);
		}

		public void Show()
		{
			Root.style.display = DisplayStyle.Flex;
		}

		public void Hide()
		{
			Root.style.display = DisplayStyle.None;
		}

		/// <summary>The cause chip under GAME OVER.</summary>
		public void SetCause(RunEndCause cause)
		{
			m_causeLabel.text = CauseText(cause);
		}

		public void SetScore(int score)
		{
			m_score.text = score.ToString("N0");
		}

		/// <summary>NEW BEST! sticker beside the score.</summary>
		public void SetNewBest(bool show)
		{
			m_newBest.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>The four stat chips. Values are pre-formatted text (static placeholders until run stats exist).</summary>
		public void SetStats(string distance, string flattened, string bestCombo, int earned)
		{
			m_distance.text = distance;
			m_flattened.text = flattened;
			m_combo.text = bestCombo;
			m_earned.text = $"+{earned:N0}";
		}

		/// <summary>Line under the primary button ("2 attempts left today"); empty hides it.</summary>
		public void SetRetryNote(string note)
		{
			m_retryNote.text = note;
			m_retryNote.style.display = string.IsNullOrEmpty(note) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		/// <summary>
		/// Rank chip text (empty hides the chip). <paramref name="improved"/> tints the text green for a better rank than
		/// last time; a worse or equal rank is shown plain, nothing red (S05 note: a bad day must not read as a punishment).
		/// </summary>
		public void SetRank(string text, bool improved)
		{
			m_rankText.text = text;
			m_rankText.EnableInClassList(k_RankUpClass, improved);
			m_rankChip.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		/// <summary>Shows the failed-submit retry button inside the rank chip.</summary>
		public void SetRankRetryVisible(bool visible)
		{
			m_rankRetry.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public void ShowToast(string message)
		{
			m_toastLabel.text = message;
			m_toast.style.display = DisplayStyle.Flex;

			m_toastHide?.Pause();
			m_toastHide = m_toast.schedule.Execute(() => m_toast.style.display = DisplayStyle.None).StartingIn(k_ToastDurationMs);
		}

		public static string CauseText(RunEndCause cause)
		{
			return cause switch
			{
				RunEndCause.OutOfFuel => "OUT OF FUEL",
				RunEndCause.Wrecked => "WRECKED",
				RunEndCause.FlewTooHigh => "FLEW TOO HIGH",
				RunEndCause.HitTheDeck => "HIT THE DECK",
				RunEndCause.OutOfBounds => "OUT OF BOUNDS",
				_ => string.Empty
			};
		}
	}
}
