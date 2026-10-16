using System;
using Opoint8182.Game;
using UnityEngine;
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

	/// <summary>What a Game Over button does. The view only reports which one was tapped; the presenter decides what it means.</summary>
	public enum GameOverAction
	{
		BankIt,
		Retry,
		RetryNow,
		MainMenu,
		Leaderboard,
		Practice,
		Share
	}

	/// <summary>
	/// The attempt-driven variants of the button row (mockup S05 a / b / c and practice). Attempts do not exist yet, so play
	/// uses <see cref="GameOverPlaceholderData.DefaultAttemptState"/> and the gallery shows the rest.
	/// </summary>
	public enum GameOverAttemptState
	{
		/// <summary>a: attempt 1 of 3, placed well. BANK IT is primary so the other attempts can be saved.</summary>
		PlacedWell,

		/// <summary>b: attempt 1 of 3, did not place. RETRY stays primary.</summary>
		DidNotPlace,

		/// <summary>c: attempt 3 of 3. The rank locks; LEADERBOARD is primary and PRACTICE is greyed.</summary>
		FinalAttempt,

		/// <summary>Practice: the attempt row is dropped and SHARE keeps a "practice" tag.</summary>
		Practice
	}

	/// <summary>
	/// Pure UI for the Game Over screen (mockup S05): takes plain values through setters and knows nothing about the
	/// game systems, so the same view runs in play (driven by <see cref="GameOverUI"/>) and in the GameOverGallery
	/// (driven by <see cref="GameOverStateDriver"/>).
	/// </summary>
	public class GameOverView
	{
		public const string DefaultRankCaption = "RANK";

		private const long k_SkeletonPulseMs = 520;
		private const long k_CascadeTickMs = 16;
		private const string k_RankFinalClass = "go-rank-chip--final";
		private const string k_RankNeutralClass = "go-rank-chip--neutral";
		private const string k_RankWordClass = "go-rank-value--word";
		private const string k_SkeletonDimClass = "go-rank-skeleton--dim";
		private const string k_PrimaryLongClass = "go-primary--long";
		private const int k_StatChipCount = 4;

		// Cascade timeline in seconds (S05 note: score slams in, stat chips pop left to right 60 ms apart, rank chip
		// fades up, buttons rise on their own schedule). The buttons never wait for the rank chip.
		private const float k_HeadDelay = 0f;
		private const float k_HeadDuration = 0.2f;
		private const float k_ScoreDelay = 0.05f;
		private const float k_ScoreDuration = 0.25f;
		private const float k_ScoreFromScale = 1.3f;
		private const float k_ScoreCountDuration = 0.95f;
		private const float k_StatsDelay = 0.35f;
		private const float k_StatsStagger = 0.06f;
		private const float k_StatDuration = 0.22f;
		private const float k_EarnedCountDuration = 0.65f;
		private const float k_RankDelay = 0.75f;
		private const float k_RankDuration = 0.3f;
		private const float k_RankRise = 24f;
		private const float k_ActionsDelay = 0.6f;
		private const float k_ActionsDuration = 0.35f;
		private const float k_ActionsRise = 150f;
		private const float k_CascadeTotal = 1.15f;

		// After the cascade: the NEW BEST! sticker slams in once the rank has settled (or after a wait), and the rank chip
		// counts down to the player's rank, holds briefly, then widens to show the caption, arrow and status.
		private const float k_SlamDuration = 0.2f;
		private const float k_SlamFromScale = 2.4f;
		private const float k_SlamMaxWait = 2.5f;
		private const float k_RankCountDuration = 1.0f;
		private const float k_RankHoldDuration = 0.12f;
		private const float k_RankRevealDuration = 0.38f;

		private readonly Label m_title;
		private readonly VisualElement m_cause;
		private readonly Label m_causeLabel;
		private readonly Label m_score;
		private readonly VisualElement m_newBest;
		private readonly VisualElement m_yourBest;
		private readonly Label m_yourBestValue;
		private readonly VisualElement[] m_statChips = new VisualElement[k_StatChipCount];
		private readonly Label m_distance;
		private readonly Label m_flattened;
		private readonly Label m_combo;
		private readonly Label m_earned;
		private readonly VisualElement m_rankChip;
		private readonly VisualElement m_rankLead;
		private readonly VisualElement m_rankTail;
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
		private readonly IVisualElementScheduledItem m_rankAnim;
		private readonly VisualElement m_actions;
		private readonly LipButton m_primary;
		private readonly Label m_primaryNote;
		private readonly LipButton m_secondaryA;
		private readonly LipButton m_secondaryB;
		private readonly Label m_shareTag;
		private readonly UiToast m_toast;
		private readonly IVisualElementScheduledItem m_cascade;

		private GameOverAction m_primaryAction = GameOverAction.Retry;
		private GameOverAction m_secondaryAAction = GameOverAction.MainMenu;
		private GameOverAction m_secondaryBAction = GameOverAction.Leaderboard;
		private int m_earnedTarget;
		private int m_scoreTarget;
		private bool m_cascadeActive;
		private float m_cascadeStart;
		private float m_slamStart = float.NaN;
		private bool m_newBestShown;

		private enum RankPhase
		{
			None,
			Waiting,
			Counting,
			Hold,
			Revealing
		}

		private RankPhase m_rankPhase;
		private bool m_rankCountsDown;
		private float m_rankPhaseStart;
		private int m_rankCountFrom;
		private int m_rankCountTo;
		private float m_rankLeadTarget;
		private float m_rankTailTarget;

		public VisualElement Root { get; }

		public bool IsVisible => Root.resolvedStyle.display == DisplayStyle.Flex;

		/// <summary>A button in the row was tapped.</summary>
		public event Action<GameOverAction> ActionClicked;

		/// <summary>The rank chip itself was tapped (opens the leaderboard at the player's row once that screen exists).</summary>
		public event Action RankClicked;

		/// <summary>The rank chip's own retry (a failed score submit): send the score again.</summary>
		public event Action RankRetryClicked;

		public GameOverView(VisualElement root)
		{
			Root = root;

			m_title = root.Q<Label>("go-title");
			m_cause = root.Q<VisualElement>("go-cause");
			m_causeLabel = root.Q<Label>("go-cause-label");
			m_score = root.Q<Label>("go-score");
			m_newBest = root.Q<VisualElement>("go-new-best");
			m_yourBest = root.Q<VisualElement>("go-your-best");
			m_yourBestValue = root.Q<Label>("go-best-value");
			m_distance = root.Q<Label>("go-stat-distance");
			m_flattened = root.Q<Label>("go-stat-flattened");
			m_combo = root.Q<Label>("go-stat-combo");
			m_earned = root.Q<Label>("go-stat-earned");
			m_rankChip = root.Q<VisualElement>("go-rank-chip");
			m_rankLead = root.Q<VisualElement>("go-rank-lead");
			m_rankTail = root.Q<VisualElement>("go-rank-tail");
			m_rankCaption = root.Q<Label>("go-rank-caption");
			m_rankValue = root.Q<Label>("go-rank-value");
			m_rankDelta = root.Q<VisualElement>("go-rank-delta");
			m_rankDeltaText = root.Q<Label>("go-rank-delta-text");
			m_rankSeparator = root.Q<VisualElement>("go-rank-sep");
			m_rankStatus = root.Q<Label>("go-rank-status");
			m_rankSkeleton = root.Q<VisualElement>("go-rank-skeleton");
			m_rankMessage = root.Q<Label>("go-rank-message");
			m_rankRetry = root.Q<LipButton>("go-rank-retry");
			m_actions = root.Q<VisualElement>("go-actions");
			m_primary = root.Q<LipButton>("go-primary");
			m_primaryNote = root.Q<Label>("go-primary-note");
			m_secondaryA = root.Q<LipButton>("go-secondary-a");
			m_secondaryB = root.Q<LipButton>("go-secondary-b");
			m_shareTag = root.Q<Label>("go-share-tag");
			m_toast = new UiToast(root, 24f);

			var chips = root.Query<VisualElement>(className: "go-stat").ToList();
			for (var i = 0; i < k_StatChipCount && i < chips.Count; i++) m_statChips[i] = chips[i];

			// No base padding: positions in GameOver.uss are measured from the screen edge like the mockup; the helper only adds device insets.
			SafeAreaHelper.Apply(root.Q<VisualElement>("go-safe"), 0f);

			m_primary.clicked += () => ActionClicked?.Invoke(m_primaryAction);
			m_secondaryA.clicked += () => ActionClicked?.Invoke(m_secondaryAAction);
			m_secondaryB.clicked += () => ActionClicked?.Invoke(m_secondaryBAction);
			root.Q<LipButton>("go-share").clicked += () => ActionClicked?.Invoke(GameOverAction.Share);
			m_rankRetry.clicked += () => RankRetryClicked?.Invoke();
			m_rankChip.RegisterCallback<ClickEvent>(OnRankChipClicked);

			m_skeletonPulse = root.schedule.Execute(PulseSkeleton).Every(k_SkeletonPulseMs);
			m_skeletonPulse.Pause();

			m_rankAnim = root.schedule.Execute(TickRankAnimation).Every(k_CascadeTickMs);
			m_rankAnim.Pause();

			m_cascade = root.schedule.Execute(TickCascade).Every(k_CascadeTickMs);
			m_cascade.Pause();

			Hide();
			HideRank();
			ApplyAttemptState(GameOverAttemptState.DidNotPlace);
		}

		/// <summary>Shows the screen and, unless <paramref name="animate"/> is false, plays the entry cascade.</summary>
		public void Show(bool animate = true)
		{
			Root.style.display = DisplayStyle.Flex;
			if (animate) PlayCascade();
			else StopCascade();
		}

		public void Hide()
		{
			StopCascade();
			Root.style.display = DisplayStyle.None;
		}

		/// <summary>The cause chip under GAME OVER.</summary>
		public void SetCause(RunEndCause cause)
		{
			m_causeLabel.text = CauseText(cause);
		}

		public void SetScore(int score)
		{
			m_scoreTarget = score;
			m_score.text = score.ToString("N0");
		}

		/// <summary>NEW BEST! sticker beside the score.</summary>
		public void SetNewBest(bool show)
		{
			m_newBestShown = show;
			m_newBest.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>YOUR BEST chip beside the score (a run that did not beat it); null or empty hides it.</summary>
		public void SetYourBest(string value)
		{
			var has = !string.IsNullOrEmpty(value);
			m_yourBestValue.text = value;
			m_yourBest.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>The four stat chips. Values are pre-formatted text (static placeholders until run stats exist).</summary>
		public void SetStats(string distance, string flattened, string bestCombo, int earned)
		{
			m_distance.text = distance;
			m_flattened.text = flattened;
			m_combo.text = bestCombo;
			m_earnedTarget = earned;
			m_earned.text = EarnedText(earned);
		}

		/// <summary>
		/// Dresses the button row for an attempt state: which button is primary, the line under it, the two secondary
		/// buttons, and the "practice" tag on SHARE.
		/// </summary>
		public void ApplyAttemptState(GameOverAttemptState state)
		{
			switch (state)
			{
				case GameOverAttemptState.PlacedWell:
					SetActions(GameOverAction.BankIt, GameOverPlaceholderData.BankItNote, GameOverAction.RetryNow, GameOverAction.Leaderboard);
					break;
				case GameOverAttemptState.FinalAttempt:
					SetActions(GameOverAction.Leaderboard, GameOverPlaceholderData.FinalAttemptNote, GameOverAction.MainMenu, GameOverAction.Practice);
					break;
				case GameOverAttemptState.Practice:
					SetActions(GameOverAction.Retry, string.Empty, GameOverAction.MainMenu, GameOverAction.Leaderboard);
					break;
				default:
					SetActions(GameOverAction.Retry, GameOverPlaceholderData.RetryNote, GameOverAction.MainMenu, GameOverAction.Leaderboard);
					break;
			}

			m_shareTag.style.display = state == GameOverAttemptState.Practice ? DisplayStyle.Flex : DisplayStyle.None;
			m_secondaryB.SetEnabled(state != GameOverAttemptState.FinalAttempt);
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
			m_rankLead.style.display = DisplayStyle.Flex;
			m_rankSkeleton.style.display = DisplayStyle.Flex;
			m_rankSkeleton.RemoveFromClassList(k_SkeletonDimClass);
			m_skeletonPulse.Resume();
		}

		/// <summary>
		/// A rank: "#N", an up arrow image when it improved (with <paramref name="delta"/> places when known, 0 = arrow
		/// only), and an optional status after a divider. A worse or equal rank shows the number alone: no red, no down
		/// arrow (S05 note: a bad day must not read as a punishment).
		/// <para>With <paramref name="animate"/> the chip starts as just the number: an improved rank counts down from a
		/// larger number to <paramref name="rank"/>, then the chip widens to bring in the caption, arrow and status.</para>
		/// </summary>
		public void ShowRankResult(int rank, bool improved, int delta = 0, string caption = DefaultRankCaption, string status = "", RankChipStyle style = RankChipStyle.Amber, bool animate = false)
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

			m_rankLead.style.display = DisplayStyle.Flex;
			m_rankTail.style.display = improved || !string.IsNullOrEmpty(status) ? DisplayStyle.Flex : DisplayStyle.None;
			if (animate) BeginRankAnimation(rank, improved);
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
			m_rankLead.style.display = DisplayStyle.Flex;
			m_rankTail.style.display = DisplayStyle.Flex;
		}

		/// <summary>A message in the chip (submit failed, leaderboard unavailable), optionally with the chip's own RETRY.</summary>
		public void ShowRankMessage(string message, bool showRetry)
		{
			ResetRankChip();
			m_rankChip.style.display = DisplayStyle.Flex;
			ApplyRankStyle(RankChipStyle.Neutral);

			m_rankCaption.text = DefaultRankCaption;
			m_rankLead.style.display = DisplayStyle.Flex;
			m_rankMessage.text = message;
			m_rankMessage.style.display = DisplayStyle.Flex;
			m_rankRetry.style.display = showRetry ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public void ShowToast(string message)
		{
			m_toast.Show(message);
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

		public static string ActionText(GameOverAction action)
		{
			return action switch
			{
				GameOverAction.BankIt => "BANK IT",
				GameOverAction.Retry => "RETRY",
				GameOverAction.RetryNow => "RETRY NOW",
				GameOverAction.MainMenu => "MAIN MENU",
				GameOverAction.Leaderboard => "LEADERBOARD",
				GameOverAction.Practice => "PRACTICE",
				_ => string.Empty
			};
		}

		private void SetActions(GameOverAction primary, string note, GameOverAction secondaryA, GameOverAction secondaryB)
		{
			m_primaryAction = primary;
			m_secondaryAAction = secondaryA;
			m_secondaryBAction = secondaryB;

			m_primary.text = ActionText(primary);
			m_primary.EnableInClassList(k_PrimaryLongClass, primary == GameOverAction.Leaderboard);
			m_secondaryA.text = ActionText(secondaryA);
			m_secondaryB.text = ActionText(secondaryB);

			m_primaryNote.text = note;
			m_primaryNote.style.display = string.IsNullOrEmpty(note) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		// Every state starts from a blank chip so a previous state's parts cannot leak into the next one.
		private void ResetRankChip()
		{
			m_skeletonPulse.Pause();
			m_rankAnim.Pause();
			m_rankPhase = RankPhase.None;
			m_rankLead.style.maxWidth = StyleKeyword.None;
			m_rankTail.style.maxWidth = StyleKeyword.None;
			m_rankLead.style.opacity = StyleKeyword.Null;
			m_rankTail.style.opacity = StyleKeyword.Null;
			m_rankLead.style.display = DisplayStyle.None;
			m_rankTail.style.display = DisplayStyle.None;
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

		// The chip starts as just the number (lead and tail clipped to zero width). An improved rank counts down from an
		// arbitrary larger number, holds, then the lead / tail containers grow to their measured content width.
		private void BeginRankAnimation(int rank, bool count)
		{
			m_rankCountTo = rank;
			m_rankCountFrom = rank + Mathf.Clamp(rank * 2, 25, 400);
			// 1 px, not 0: a zero-width container makes UITK wrap its labels one character per line, which blew the
			// chip up to the height of the whole column. Invisible until the reveal starts.
			m_rankLead.style.maxWidth = 1f;
			m_rankTail.style.maxWidth = 1f;
			m_rankLead.style.opacity = 0f;
			m_rankTail.style.opacity = 0f;
			m_rankValue.text = $"#{(count ? m_rankCountFrom : rank):N0}";
			m_rankCountsDown = count;
			m_rankPhase = RankPhase.Waiting;
			m_rankPhaseStart = float.NaN;
			m_rankAnim.Resume();
		}

		private void TickRankAnimation()
		{
			var now = Time.unscaledTime;
			if (float.IsNaN(m_rankPhaseStart)) m_rankPhaseStart = now;
			var t = now - m_rankPhaseStart;

			switch (m_rankPhase)
			{
				case RankPhase.Waiting:
				{
					// Hold the count-down until the cascade has faded the chip in; otherwise the whole count-down runs
					// while the chip is still invisible and the player only ever sees the final number.
					var chipVisibleAt = k_RankDelay + k_RankDuration;
					if (m_cascadeActive && (float.IsNaN(m_cascadeStart) || now - m_cascadeStart < chipVisibleAt)) break;

					m_rankPhase = m_rankCountsDown ? RankPhase.Counting : RankPhase.Hold;
					m_rankPhaseStart = now;
					break;
				}
				case RankPhase.Counting:
				{
					var p = Mathf.Clamp01(t / k_RankCountDuration);
					var value = Mathf.RoundToInt(Mathf.Lerp(m_rankCountFrom, m_rankCountTo, EaseOut(p)));
					m_rankValue.text = $"#{value:N0}";
					if (p >= 1f)
					{
						m_rankPhase = RankPhase.Hold;
						m_rankPhaseStart = now;
					}

					break;
				}
				case RankPhase.Hold:
				{
					if (t < k_RankHoldDuration) break;

					m_rankLeadTarget = ContentWidth(m_rankLead);
					m_rankTailTarget = ContentWidth(m_rankTail);
					m_rankLead.style.opacity = StyleKeyword.Null;
					m_rankTail.style.opacity = StyleKeyword.Null;
					m_rankPhase = RankPhase.Revealing;
					m_rankPhaseStart = now;
					break;
				}
				case RankPhase.Revealing:
				{
					var p = Mathf.Clamp01(t / k_RankRevealDuration);
					var ease = EaseOut(p);
					m_rankLead.style.maxWidth = Mathf.Lerp(0f, m_rankLeadTarget, ease);
					m_rankTail.style.maxWidth = Mathf.Lerp(0f, m_rankTailTarget, ease);
					if (p >= 1f)
					{
						m_rankLead.style.maxWidth = StyleKeyword.None;
						m_rankTail.style.maxWidth = StyleKeyword.None;
						m_rankPhase = RankPhase.None;
						m_rankAnim.Pause();
					}

					break;
				}
				default:
					m_rankAnim.Pause();
					break;
			}
		}

		// Natural width of a row of children (margins included). The container is clipped to 1 px while this runs, which
		// squeezes the labels' laid-out widths, so text is measured directly instead of read back from the layout: the
		// reveal then ends exactly at the width the chip settles on, with no jump when the clamp is removed.
		private static float ContentWidth(VisualElement container)
		{
			var width = 0f;
			foreach (var child in container.Children())
			{
				if (child.resolvedStyle.display == DisplayStyle.None) continue;
				width += NaturalWidth(child);
			}

			return width;
		}

		private static float NaturalWidth(VisualElement element)
		{
			float own;
			if (element is TextElement text)
			{
				// Over-estimating is safe (the container never grows past its content); under-estimating is the visible
				// jump when the clamp comes off, and the text measure does not fully include letter-spacing.
				var spacing = text.resolvedStyle.letterSpacing;
				if (float.IsNaN(spacing)) spacing = 0f;
				own = text.MeasureTextSize(text.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x
				      + spacing * text.text.Length + 2f;
			}
			else if (element.childCount > 0)
			{
				own = ContentWidth(element);
			}
			else
			{
				own = element.resolvedStyle.width;
				if (float.IsNaN(own)) own = 0f;
			}

			return own + element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight;
		}

		// True once the rank chip has nothing left to do (no submit in flight, no count-down or widening running).
		private bool RankSettled()
		{
			return m_rankPhase == RankPhase.None && m_rankSkeleton.resolvedStyle.display != DisplayStyle.Flex;
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

		private static string EarnedText(int earned) => $"+{earned:N0}";

		// ---- Entry cascade -------------------------------------------------------------------------------------------
		// One scheduled item (created once, paused when idle) drives every piece from elapsed unscaled time: no per-piece
		// tweens or allocations, and the same code path for play and the gallery. Inline styles are cleared at the end so
		// the USS values apply again.

		// The clock starts on the first tick, not here: a long frame between the run ending and the first UI update (a
		// hitch, a hit-stop, a slow editor frame) would otherwise eat the animation and the screen would just appear.
		private void PlayCascade()
		{
			m_cascadeActive = true;
			m_cascadeStart = float.NaN;
			m_slamStart = float.NaN;

			// The score counts up, so pin the label to the width of the final number: otherwise the centred text
			// shifts sideways every time it gains a digit.
			var finalWidth = m_score.MeasureTextSize(m_scoreTarget.ToString("N0"), 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
			m_score.style.width = Mathf.Ceil(finalWidth) + 3f;
			ApplyCascade(0f);
			m_cascade.Resume();
		}

		private void StopCascade()
		{
			m_cascadeActive = false;
			m_cascade.Pause();
			ClearCascadeStyles();
		}

		private void TickCascade()
		{
			var now = Time.unscaledTime;
			if (float.IsNaN(m_cascadeStart)) m_cascadeStart = now;

			var t = now - m_cascadeStart;
			if (t < k_CascadeTotal)
			{
				ApplyCascade(t);
				return;
			}

			// The cascade is over. With nothing to slam in we are done; otherwise hold the finished pose until the rank
			// has settled (or a timeout), then slam NEW BEST! in beside the score, which stays where it is.
			if (!m_newBestShown)
			{
				StopCascade();
				return;
			}

			if (float.IsNaN(m_slamStart))
			{
				if (!RankSettled() && t < k_CascadeTotal + k_SlamMaxWait)
				{
					ApplyCascade(k_CascadeTotal);
					return;
				}

				m_slamStart = now;
			}

			var p = Progress(now, m_slamStart, k_SlamDuration);
			SetPop(m_newBest, Mathf.Lerp(k_SlamFromScale, 1f, EaseOut(p)), Mathf.Min(1f, p * 5f));
			if (p >= 1f) StopCascade();
		}

		private void ApplyCascade(float t)
		{
			var head = Progress(t, k_HeadDelay, k_HeadDuration);
			SetPop(m_title, Mathf.Lerp(1.15f, 1f, EaseOut(head)), Mathf.Min(1f, head * 3f));
			SetPop(m_cause, Mathf.Lerp(1.15f, 1f, EaseOut(head)), Mathf.Min(1f, head * 3f));

			var score = Progress(t, k_ScoreDelay, k_ScoreDuration);
			SetPop(m_score, Mathf.Lerp(k_ScoreFromScale, 1f, EaseOut(score)), Mathf.Min(1f, score * 4f));
			m_yourBest.style.opacity = Mathf.Min(1f, score * 4f);

			var scoreCount = Progress(t, k_ScoreDelay, k_ScoreCountDuration);
			m_score.text = Mathf.RoundToInt(m_scoreTarget * EaseOut(scoreCount)).ToString("N0");

			// NEW BEST! waits for the end of the cascade (see TickCascade); the score is centred without it.
			if (m_newBestShown) m_newBest.style.opacity = 0f;

			for (var i = 0; i < k_StatChipCount; i++)
			{
				if (m_statChips[i] == null) continue;

				var p = Progress(t, k_StatsDelay + k_StatsStagger * i, k_StatDuration);
				var scale = p < 0.6f ? Mathf.Lerp(0.6f, 1.1f, p / 0.6f) : Mathf.Lerp(1.1f, 1f, (p - 0.6f) / 0.4f);
				SetPop(m_statChips[i], scale, Mathf.Min(1f, p * 4f));
			}

			var count = Progress(t, k_StatsDelay, k_EarnedCountDuration);
			m_earned.text = EarnedText(Mathf.RoundToInt(m_earnedTarget * EaseOut(count)));

			var rank = Progress(t, k_RankDelay, k_RankDuration);
			m_rankChip.style.opacity = rank;
			m_rankChip.style.translate = new StyleTranslate(new Translate(0f, Mathf.Lerp(k_RankRise, 0f, EaseOut(rank))));

			var actions = Progress(t, k_ActionsDelay, k_ActionsDuration);
			var ease = EaseOut(actions);
			m_actions.style.opacity = Mathf.Min(1f, actions * 3f);
			m_actions.style.translate = new StyleTranslate(new Translate(0f, Mathf.Lerp(k_ActionsRise, 0f, ease)));
		}

		private void ClearCascadeStyles()
		{
			ClearPop(m_title);
			ClearPop(m_cause);
			ClearPop(m_score);
			m_score.style.width = StyleKeyword.Null;
			m_score.text = m_scoreTarget.ToString("N0");
			ClearPop(m_newBest);
			m_yourBest.style.opacity = StyleKeyword.Null;
			for (var i = 0; i < k_StatChipCount; i++)
			{
				if (m_statChips[i] != null) ClearPop(m_statChips[i]);
			}

			ClearPop(m_rankChip);
			ClearPop(m_actions);
			m_earned.text = EarnedText(m_earnedTarget);
		}

		private static void SetPop(VisualElement element, float scale, float opacity)
		{
			element.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
			element.style.opacity = opacity;
		}

		private static void ClearPop(VisualElement element)
		{
			element.style.scale = StyleKeyword.Null;
			element.style.opacity = StyleKeyword.Null;
			element.style.translate = StyleKeyword.Null;
		}

		private static float Progress(float t, float delay, float duration) => Mathf.Clamp01((t - delay) / duration);

		private static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x);
	}
}
