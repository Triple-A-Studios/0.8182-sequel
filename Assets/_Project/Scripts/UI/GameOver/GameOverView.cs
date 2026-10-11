using System;
using Opoint8182.Game;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>How the rank chip is dressed: amber (a rank in play), green and locked (final), or plain (unranked, messages).</summary>
	public enum RankChipStyle
	{
		Amber,
		Final,
		Neutral
	}

	/// <summary>
	/// Pure UI for the Game Over screen (mockup S05): takes plain values through setters and knows nothing about the
	/// game systems, so the same view runs in play (driven by <see cref="GameOverUI"/>) and in the GameOverGallery
	/// (driven by <see cref="GameOverStateDriver"/>).
	/// </summary>
	public class GameOverView
	{
		public const string DefaultRankCaption = "RANK";

		private const long k_ToastDurationMs = 2500;
		private const long k_SkeletonPulseMs = 520;
		private const string k_RankFinalClass = "go-rank-chip--final";
		private const string k_RankNeutralClass = "go-rank-chip--neutral";
		private const string k_RankWordClass = "go-rank-value--word";
		private const string k_SkeletonDimClass = "go-rank-skeleton--dim";

		private readonly Label m_causeLabel;
		private readonly Label m_score;
		private readonly VisualElement m_newBest;
		private readonly Label m_distance;
		private readonly Label m_flattened;
		private readonly Label m_combo;
		private readonly Label m_earned;
		private readonly VisualElement m_rankChip;
		private readonly Label m_rankCaption;
		private readonly Label m_rankValue;
		private readonly VisualElement m_rankDelta;
		private readonly Label m_rankDeltaText;
		private readonly VisualElement m_rankSeparator;
		private readonly Label m_rankStatus;
		private readonly VisualElement m_rankSkeleton;
		private readonly Label m_rankMessage;
		private readonly LipButton m_rankRetry;
		private readonly IVisualElementScheduledItem m_skeletonPulse;
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

		/// <summary>The rank chip itself was tapped (opens the leaderboard at the player's row once that screen exists).</summary>
		public event Action RankClicked;

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
			m_rankCaption = root.Q<Label>("go-rank-caption");
			m_rankValue = root.Q<Label>("go-rank-value");
			m_rankDelta = root.Q<VisualElement>("go-rank-delta");
			m_rankDeltaText = root.Q<Label>("go-rank-delta-text");
			m_rankSeparator = root.Q<VisualElement>("go-rank-sep");
			m_rankStatus = root.Q<Label>("go-rank-status");
			m_rankSkeleton = root.Q<VisualElement>("go-rank-skeleton");
			m_rankMessage = root.Q<Label>("go-rank-message");
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
			m_rankChip.RegisterCallback<ClickEvent>(OnRankChipClicked);

			m_skeletonPulse = root.schedule.Execute(PulseSkeleton).Every(k_SkeletonPulseMs);
			m_skeletonPulse.Pause();

			Hide();
			HideRank();
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

		/// <summary>Hides the whole rank chip (no score to rank).</summary>
		public void HideRank()
		{
			ResetRankChip();
			m_rankChip.style.display = DisplayStyle.None;
		}

		/// <summary>
		/// RANKING state: the same chip with pulsing grey bars while the submit is in flight. It never blocks the buttons,
		/// they are independent of it.
		/// </summary>
		public void ShowRanking()
		{
			ResetRankChip();
			m_rankChip.style.display = DisplayStyle.Flex;
			m_rankCaption.text = DefaultRankCaption;
			m_rankSkeleton.style.display = DisplayStyle.Flex;
			m_rankSkeleton.RemoveFromClassList(k_SkeletonDimClass);
			m_skeletonPulse.Resume();
		}

		/// <summary>
		/// A rank: "#N", an up arrow image when it improved (with <paramref name="delta"/> places when known, 0 = arrow
		/// only), and an optional status after a divider. A worse or equal rank shows the number alone: no red, no down
		/// arrow (S05 note: a bad day must not read as a punishment).
		/// </summary>
		public void ShowRankResult(int rank, bool improved, int delta = 0, string caption = DefaultRankCaption, string status = "", RankChipStyle style = RankChipStyle.Amber)
		{
			ResetRankChip();
			m_rankChip.style.display = DisplayStyle.Flex;
			ApplyRankStyle(style);

			m_rankCaption.text = caption;
			m_rankValue.text = $"#{rank:N0}";
			m_rankValue.style.display = DisplayStyle.Flex;

			if (improved)
			{
				m_rankDelta.style.display = DisplayStyle.Flex;
				m_rankDeltaText.text = delta > 0 ? delta.ToString("N0") : string.Empty;
				m_rankDeltaText.style.display = delta > 0 ? DisplayStyle.Flex : DisplayStyle.None;
			}

			ShowRankStatus(status);
		}

		/// <summary>"UNRANKED" with a status after a divider (the gap to the next threshold), in the plain style.</summary>
		public void ShowRankUnranked(string status, string caption = DefaultRankCaption)
		{
			ResetRankChip();
			m_rankChip.style.display = DisplayStyle.Flex;
			ApplyRankStyle(RankChipStyle.Neutral);

			m_rankCaption.text = caption;
			m_rankValue.text = "UNRANKED";
			m_rankValue.EnableInClassList(k_RankWordClass, true);
			m_rankValue.style.display = DisplayStyle.Flex;

			ShowRankStatus(status);
		}

		/// <summary>A message in the chip (submit failed, leaderboard unavailable), optionally with the chip's own RETRY.</summary>
		public void ShowRankMessage(string message, bool showRetry)
		{
			ResetRankChip();
			m_rankChip.style.display = DisplayStyle.Flex;
			ApplyRankStyle(RankChipStyle.Neutral);

			m_rankCaption.text = DefaultRankCaption;
			m_rankMessage.text = message;
			m_rankMessage.style.display = DisplayStyle.Flex;
			m_rankRetry.style.display = showRetry ? DisplayStyle.Flex : DisplayStyle.None;
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

		// Every state starts from a blank chip so a previous state's parts cannot leak into the next one.
		private void ResetRankChip()
		{
			m_skeletonPulse.Pause();
			m_rankChip.RemoveFromClassList(k_RankFinalClass);
			m_rankChip.RemoveFromClassList(k_RankNeutralClass);
			m_rankValue.EnableInClassList(k_RankWordClass, false);

			m_rankValue.style.display = DisplayStyle.None;
			m_rankDelta.style.display = DisplayStyle.None;
			m_rankSeparator.style.display = DisplayStyle.None;
			m_rankStatus.style.display = DisplayStyle.None;
			m_rankSkeleton.style.display = DisplayStyle.None;
			m_rankMessage.style.display = DisplayStyle.None;
			m_rankRetry.style.display = DisplayStyle.None;
		}

		private void ApplyRankStyle(RankChipStyle style)
		{
			m_rankChip.EnableInClassList(k_RankFinalClass, style == RankChipStyle.Final);
			m_rankChip.EnableInClassList(k_RankNeutralClass, style == RankChipStyle.Neutral);
		}

		private void ShowRankStatus(string status)
		{
			var has = !string.IsNullOrEmpty(status);
			m_rankStatus.text = status;
			m_rankStatus.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
			m_rankSeparator.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void PulseSkeleton()
		{
			m_rankSkeleton.ToggleInClassList(k_SkeletonDimClass);
		}

		// The retry button sits inside the chip, so its click bubbles here too; it must not count as a tap on the chip.
		private void OnRankChipClicked(ClickEvent evt)
		{
			if (evt.target is VisualElement target && (target == m_rankRetry || m_rankRetry.Contains(target))) return;
			RankClicked?.Invoke();
		}
	}
}
