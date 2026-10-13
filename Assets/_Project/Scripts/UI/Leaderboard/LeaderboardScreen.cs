using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Opoint8182.Leaderboard;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	public enum LeaderboardTab
	{
		Weekly,
		AllTime,
	}

	/// <summary>Loads the board for a tab and a number of weeks back (0 = the current week).</summary>
	public delegate Task<TopScoresResult> LeaderboardProvider(LeaderboardTab tab, int weeksBack);

	/// <summary>
	/// Leaderboard screen (mockup S07): WEEKLY / ALL-TIME tabs, a stepper through past weeks, gold / silver / bronze
	/// rows, the player's own row pinned below the list, and loading / empty / error / offline states. Plain class over
	/// the instance's root, like <see cref="SettingsScreen"/>: the owner (main menu, or the dev gallery) calls
	/// <see cref="Open"/> / <see cref="Close"/> when the router shows and hides it and reacts to the events.
	/// <para>Data comes from a <see cref="LeaderboardProvider"/>. The default one reads the real Top-10 of the existing
	/// daily board for the current week, and falls back to <see cref="LeaderboardPlaceholderData"/> for ALL-TIME and past
	/// weeks, which have no backend yet. Every load is token-guarded: a result that lands after a close, a tab change or
	/// a retry is dropped.</para>
	/// </summary>
	public class LeaderboardScreen
	{
		private const int k_TopLimit = 10;
		private const long k_PulseIntervalMs = 700;
		private const int k_SkeletonRowCount = 7;
		private const int k_SkeletonTopRowCount = 3;

		private const string k_RowClass = "lb-row";
		private const string k_TopRowClass = "lb-row--top";
		private const string k_SkeletonDimClass = "lb-skeleton--dim";

		private enum Panel
		{
			Board,
			Skeleton,
			State,
		}

		private enum StateAction
		{
			None,
			FlyNow,
			Retry,
		}

		private readonly LipButton m_back;
		private readonly VisualElement m_resetChip;
		private readonly Label m_resetPrefix;
		private readonly Label m_resetValue;
		private readonly TabBar m_tabs;
		private readonly VisualElement m_stepper;
		private readonly LipButton m_prev;
		private readonly LipButton m_next;
		private readonly Label m_weekTitle;
		private readonly Label m_weekRange;
		private readonly VisualElement m_sampleTag;
		private readonly VisualElement m_board;
		private readonly ScrollView m_scroll;
		private readonly VisualElement m_own;
		private readonly Label m_ownRank;
		private readonly Label m_ownName;
		private readonly Label m_ownHint;
		private readonly Label m_ownScore;
		private readonly LipButton m_share;
		private readonly VisualElement m_skeleton;
		private readonly VisualElement m_state;
		private readonly Label m_stateGlyph;
		private readonly Label m_stateTitle;
		private readonly Label m_stateBody;
		private readonly LipButton m_stateAction;
		private readonly IVisualElementScheduledItem m_pulse;

		private LeaderboardTab m_tab = LeaderboardTab.Weekly;
		private int m_weeksBack;
		private int m_loadToken;
		private StateAction m_action = StateAction.None;
		private VisualElement m_selfRow;
		private string m_selfName = LeaderboardPlaceholderData.SAMPLE_SELF_NAME;

		/// <summary>Replaces the default data source (the dev gallery uses this to force states).</summary>
		public LeaderboardProvider Provider { get; set; }

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		/// <summary>SHARE MY RANK was tapped. Sharing is not built yet, so the owner shows an arrives-later toast.</summary>
		public event Action ShareRequested;

		/// <summary>FLY NOW on the empty state was tapped (the owner leaves the screen and starts the run flow).</summary>
		public event Action FlyNowRequested;

		public LeaderboardScreen(VisualElement root)
		{
			m_back = root.Q<LipButton>("lb-back");
			m_resetChip = root.Q<VisualElement>("lb-reset-chip");
			m_resetPrefix = root.Q<Label>("lb-reset-prefix");
			m_resetValue = root.Q<Label>("lb-reset-value");
			m_tabs = root.Q<TabBar>("lb-tabs");
			m_stepper = root.Q<VisualElement>("lb-stepper");
			m_prev = root.Q<LipButton>("lb-prev");
			m_next = root.Q<LipButton>("lb-next");
			m_weekTitle = root.Q<Label>("lb-week-title");
			m_weekRange = root.Q<Label>("lb-week-range");
			m_sampleTag = root.Q<VisualElement>("lb-sample-tag");
			m_board = root.Q<VisualElement>("lb-board");
			m_scroll = root.Q<ScrollView>("lb-scroll");
			m_own = root.Q<VisualElement>("lb-own");
			m_ownRank = root.Q<Label>("lb-own-rank");
			m_ownName = root.Q<Label>("lb-own-name");
			m_ownHint = root.Q<Label>("lb-own-hint");
			m_ownScore = root.Q<Label>("lb-own-score");
			m_share = root.Q<LipButton>("lb-share");
			m_skeleton = root.Q<VisualElement>("lb-skeleton");
			m_state = root.Q<VisualElement>("lb-state");
			m_stateGlyph = root.Q<Label>("lb-state-glyph");
			m_stateTitle = root.Q<Label>("lb-state-title");
			m_stateBody = root.Q<Label>("lb-state-body");
			m_stateAction = root.Q<LipButton>("lb-state-action");

			BuildSkeleton();

			m_pulse = root.schedule.Execute(() => m_skeleton.ToggleInClassList(k_SkeletonDimClass)).Every(k_PulseIntervalMs);
			m_pulse.Pause();

			m_back.clicked += () => BackRequested?.Invoke();
			m_tabs.changed += OnTabChanged;
			m_prev.clicked += () => StepWeek(1);
			m_next.clicked += () => StepWeek(-1);
			m_share.clicked += () => ShareRequested?.Invoke();
			// The share button sits inside the pinned row; its tap must not also count as a tap on the row.
			m_share.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
			m_own.RegisterCallback<ClickEvent>(_ => ScrollToSelf());
			m_stateAction.clicked += OnStateActionClicked;
		}

		/// <summary>Resets to this week's WEEKLY board and loads it. Call when the router shows the screen.</summary>
		public void Open()
		{
			m_tab = LeaderboardTab.Weekly;
			m_weeksBack = 0;
			m_tabs.SetSelectedIndexWithoutNotify(0);
			Load();
		}

		/// <summary>Drops any request still in flight. Call when the router hides the screen.</summary>
		public void Close()
		{
			m_loadToken++;
			m_pulse.Pause();
		}

		/// <summary>Loads the current tab / week again (also refreshes the header, e.g. after the gallery flips the stepper).</summary>
		public void Reload()
		{
			Load();
		}

		public void SelectTab(LeaderboardTab tab)
		{
			m_tabs.selectedIndex = (int)tab;
		}

		// ---- States (public so the dev gallery can force the ones that are hard to reach) ----

		public void ShowLoading()
		{
			SetPanel(Panel.Skeleton);
		}

		public void ShowEmpty()
		{
			var current = m_tab == LeaderboardTab.Weekly && m_weeksBack == 0;
			if (m_tab == LeaderboardTab.Weekly && m_weeksBack > 0)
			{
				ShowState(string.Empty, "NO RUNS THAT WEEK", "Nobody posted a score on this board.", StateAction.None);
				return;
			}

			ShowState(string.Empty, "NOBODY HAS CRASHED YET",
				current ? "This week's board is wide open. First submitted run takes the top spot." : "The all-time board is wide open. First submitted run takes the top spot.",
				StateAction.FlyNow);
		}

		public void ShowError(bool offline)
		{
			if (offline)
			{
				ShowState("!", "YOU'RE OFFLINE", "Connect to the internet to see the leaderboard.", StateAction.Retry);
				return;
			}

			ShowState("!", "COULDN'T LOAD THE BOARD", "Something went wrong. Check your connection and try again.", StateAction.Retry);
		}

		/// <param name="rows">Ranked rows, best first.</param>
		/// <param name="self">The player's own row when it is not inside <paramref name="rows"/>, otherwise null.</param>
		public void ShowRows(IReadOnlyList<LeaderboardRow> rows, LeaderboardRow? self)
		{
			var host = m_scroll.contentContainer;
			host.Clear();
			m_selfRow = null;

			var own = self;
			foreach (var row in rows)
			{
				var element = CreateRow(row);
				host.Add(element);
				if (!row.IsSelf) continue;

				m_selfRow = element;
				own ??= row;
			}

			m_scroll.scrollOffset = Vector2.zero;
			ApplyOwnRow(own);
			SetPanel(Panel.Board);
		}

		private async void Load()
		{
			var token = ++m_loadToken;

			RefreshHeader();
			ShowLoading();

			TopScoresResult result;
			try
			{
				result = await (Provider ?? DefaultProvider)(m_tab, m_weeksBack);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[LeaderboardScreen] Load failed: {e.GetType().Name}: {e.Message}");
				result = TopScoresResult.Failed;
			}

			if (token != m_loadToken) return;

			if (!result.Success)
			{
				ShowError(Application.internetReachability == NetworkReachability.NotReachable);
				return;
			}

			if (result.Rows.Count == 0 && !result.Self.HasValue)
			{
				ShowEmpty();
				return;
			}

			CacheSelfName(result);
			ShowRows(result.Rows, result.Self);
		}

		// Weekly, this week: the real Top-10 (of the daily board until backlog #28). Everything else is sample data.
		private Task<TopScoresResult> DefaultProvider(LeaderboardTab tab, int weeksBack)
		{
			if (tab == LeaderboardTab.AllTime) return Task.FromResult(LeaderboardPlaceholderData.AllTime(m_selfName));
			if (weeksBack > 0) return Task.FromResult(LeaderboardPlaceholderData.PastWeek(weeksBack, m_selfName));

			var service = LeaderboardService.TryGetInstance();
			return service != null ? service.GetTopScoresAsync(k_TopLimit) : Task.FromResult(TopScoresResult.Failed);
		}

		// The sample boards borrow the player's real name once the real board has shown it.
		private void CacheSelfName(TopScoresResult result)
		{
			if (m_tab != LeaderboardTab.Weekly || m_weeksBack != 0) return;

			if (result.Self.HasValue)
			{
				m_selfName = result.Self.Value.Name;
				return;
			}

			foreach (var row in result.Rows)
			{
				if (!row.IsSelf) continue;
				m_selfName = row.Name;
				return;
			}
		}

		private void OnTabChanged(int index)
		{
			m_tab = index == 0 ? LeaderboardTab.Weekly : LeaderboardTab.AllTime;
			m_weeksBack = 0;
			Load();
		}

		private void StepWeek(int delta)
		{
			var target = Mathf.Clamp(m_weeksBack + delta, 0, LeaderboardPlaceholderData.MAX_WEEKS_BACK);
			if (target == m_weeksBack) return;

			m_weeksBack = target;
			Load();
		}

		private void OnStateActionClicked()
		{
			switch (m_action)
			{
				case StateAction.FlyNow:
					FlyNowRequested?.Invoke();
					break;
				case StateAction.Retry:
					Load();
					break;
			}
		}

		private void RefreshHeader()
		{
			var weekly = m_tab == LeaderboardTab.Weekly;
			var now = DateTime.UtcNow;

			SetDisplay(m_stepper, weekly);
			SetDisplay(m_sampleTag, !weekly && LeaderboardPlaceholderData.ALL_TIME_IS_SAMPLE);
			SetDisplay(m_resetChip, weekly);

			m_weekTitle.text = LeaderboardWeek.Name(m_weeksBack);
			m_weekRange.text = LeaderboardWeek.RangeText(now, m_weeksBack);
			m_prev.SetEnabled(LeaderboardPlaceholderData.PastWeeksEnabled && m_weeksBack < LeaderboardPlaceholderData.MAX_WEEKS_BACK);
			m_next.SetEnabled(m_weeksBack > 0);

			// A finished week shows FINAL where the current one counts down.
			var current = m_weeksBack == 0;
			SetDisplay(m_resetPrefix, current);
			m_resetValue.text = current ? LeaderboardWeek.ResetText(now) : "FINAL";
		}

		private void ShowState(string glyph, string title, string body, StateAction action)
		{
			m_action = action;
			m_stateGlyph.text = glyph;
			m_stateTitle.text = title;
			m_stateBody.text = body;

			SetDisplay(m_stateAction, action != StateAction.None);
			m_stateAction.text = action == StateAction.Retry ? "RETRY" : "FLY NOW";
			SetPanel(Panel.State);
		}

		private void SetPanel(Panel panel)
		{
			SetDisplay(m_board, panel == Panel.Board);
			SetDisplay(m_skeleton, panel == Panel.Skeleton);
			SetDisplay(m_state, panel == Panel.State);

			if (panel == Panel.Skeleton) m_pulse.Resume();
			else m_pulse.Pause();
		}

		private void ApplyOwnRow(LeaderboardRow? own)
		{
			var has = own.HasValue;
			SetDisplay(m_ownScore, has);
			SetDisplay(m_share, has);

			if (has)
			{
				var row = own.Value;
				m_ownRank.text = $"#{row.Rank.ToString("N0", CultureInfo.InvariantCulture)}";
				m_ownName.text = row.Name;
				m_ownHint.text = "· you";
				m_ownScore.text = FormatScore(row.Score);
				return;
			}

			m_ownRank.text = "–";
			m_ownName.text = "No score yet";
			m_ownHint.text = m_weeksBack == 0 ? "· fly a run to get on the board" : "· you didn't fly that week";
		}

		private void ScrollToSelf()
		{
			if (m_selfRow != null) m_scroll.ScrollTo(m_selfRow);
		}

		private VisualElement CreateRow(LeaderboardRow row)
		{
			var element = new VisualElement { pickingMode = PickingMode.Ignore };
			element.AddToClassList(k_RowClass);
			element.AddToClassList(row.Rank switch
			{
				1 => "lb-row--gold",
				2 => "lb-row--silver",
				3 => "lb-row--bronze",
				_ => "lb-row--plain",
			});
			if (row.Rank <= 3) element.AddToClassList(k_TopRowClass);
			if (row.IsSelf) element.AddToClassList("lb-row--self");

			element.Add(CreateLabel(row.Rank.ToString("N0", CultureInfo.InvariantCulture), "lb-row__rank"));
			element.Add(CreateLabel(row.Name, "lb-row__name"));
			element.Add(CreateLabel(FormatScore(row.Score), "lb-row__score"));
			return element;
		}

		private void BuildSkeleton()
		{
			for (var i = 0; i < k_SkeletonRowCount; i++)
			{
				var row = new VisualElement { pickingMode = PickingMode.Ignore };
				row.AddToClassList(k_RowClass);
				row.AddToClassList("lb-row--skeleton");
				if (i < k_SkeletonTopRowCount) row.AddToClassList(k_TopRowClass);
				m_skeleton.Add(row);
			}
		}

		private static Label CreateLabel(string text, string className)
		{
			var label = new Label(text) { pickingMode = PickingMode.Ignore };
			label.AddToClassList(className);
			return label;
		}

		private static string FormatScore(int score) => score.ToString("N0", CultureInfo.InvariantCulture);

		private static void SetDisplay(VisualElement element, bool visible)
		{
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}
