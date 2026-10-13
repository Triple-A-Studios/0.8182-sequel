using System.Collections.Generic;
using System.Threading.Tasks;
using Opoint8182.Leaderboard;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the main menu's full screens (Leaderboard now, Profile in 6c): loads <c>MainMenu.uxml</c>
	/// into the MenuScreensGallery scene, builds a router and the screens exactly like <see cref="MainMenuController"/>
	/// does, but backs the leaderboard with sample data so every state is reachable without a backend or a run.
	/// Keys (leaderboard): 1 sample board with the player at #248 (pinned row), 2 player inside the top 3, 3 no score
	/// yet, 4 only two rows, 5 empty board; L loading skeleton, R load error, O offline; A ALL-TIME tab, W WEEKLY tab;
	/// P enable / disable the past-week buttons (they are disabled in play until backlog #28); M open the leaderboard
	/// again after Escape / the back chevron; Escape back.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public class MenuScreensStateDriver : MonoBehaviour
	{
		private const string k_MenuScreenId = "menu";
		private const string k_LeaderboardScreenId = "leaderboard";
		private const float k_ToastSeconds = 2.5f;

		private enum BoardScenario
		{
			SampleBoard,
			SelfInTopThree,
			NoScore,
			TwoRows,
			Empty,
		}

		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private ScreenRouter m_router;
		private LeaderboardScreen m_leaderboard;
		private VisualElement m_toast;
		private Label m_toastLabel;
		private IVisualElementScheduledItem m_toastHide;
		private BoardScenario m_scenario = BoardScenario.SampleBoard;
		private string m_selfName = LeaderboardPlaceholderData.SAMPLE_SELF_NAME;

		private void Awake()
		{
			m_panelRenderer = GetComponent<PanelRenderer>();
		}

		private void OnEnable()
		{
			m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
		}

		private void OnDisable()
		{
			m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
			m_leaderboard?.Close();
			m_loadedRoot = null;
			m_router = null;
			m_leaderboard = null;
		}

		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			SafeAreaHelper.Apply(root.Q<VisualElement>("safe-area"));

			m_router = new ScreenRouter(root.Q<VisualElement>("popup-layer"));
			m_router.Register(k_MenuScreenId, root.Q<VisualElement>("screen-menu"));
			// Registered only so the router hides it: an unregistered screen would stay visible under the others.
			m_router.Register("settings", root.Q<VisualElement>("screen-settings"));
			m_router.Register(k_LeaderboardScreenId, root.Q<VisualElement>("screen-leaderboard"));
			m_router.SetRoot(k_MenuScreenId);

			m_toast = root.Q<VisualElement>("stub-toast");
			m_toastLabel = root.Q<Label>("stub-toast-label");
			m_toast.style.display = DisplayStyle.None;

			m_leaderboard = new LeaderboardScreen(root.Q<VisualElement>("screen-leaderboard"));
			m_leaderboard.Provider = LoadScenario;
			m_leaderboard.BackRequested += () => m_router.Back();
			m_leaderboard.ShareRequested += () => ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
			m_leaderboard.FlyNowRequested += () => ShowToast("FLY NOW: the menu starts a run here");
			m_router.ScreenChanged += OnScreenChanged;

			m_router.Show(k_LeaderboardScreenId);
		}

		private void Update()
		{
			if (m_leaderboard == null) return;

			var keyboard = Keyboard.current;
			if (keyboard == null) return;

			if (keyboard.escapeKey.wasPressedThisFrame) m_router.HandleBackRequested();
			if (keyboard.mKey.wasPressedThisFrame) m_router.Show(k_LeaderboardScreenId);
			if (m_router.CurrentScreenId != k_LeaderboardScreenId) return;

			if (keyboard.digit1Key.wasPressedThisFrame) SetScenario(BoardScenario.SampleBoard);
			if (keyboard.digit2Key.wasPressedThisFrame) SetScenario(BoardScenario.SelfInTopThree);
			if (keyboard.digit3Key.wasPressedThisFrame) SetScenario(BoardScenario.NoScore);
			if (keyboard.digit4Key.wasPressedThisFrame) SetScenario(BoardScenario.TwoRows);
			if (keyboard.digit5Key.wasPressedThisFrame) SetScenario(BoardScenario.Empty);

			if (keyboard.lKey.wasPressedThisFrame) m_leaderboard.ShowLoading();
			if (keyboard.rKey.wasPressedThisFrame) m_leaderboard.ShowError(false);
			if (keyboard.oKey.wasPressedThisFrame) m_leaderboard.ShowError(true);

			if (keyboard.aKey.wasPressedThisFrame) m_leaderboard.SelectTab(LeaderboardTab.AllTime);
			if (keyboard.wKey.wasPressedThisFrame) m_leaderboard.SelectTab(LeaderboardTab.Weekly);

			if (keyboard.pKey.wasPressedThisFrame)
			{
				LeaderboardPlaceholderData.PastWeeksEnabled = !LeaderboardPlaceholderData.PastWeeksEnabled;
				m_leaderboard.Reload();
			}
		}

		private void OnScreenChanged(string screenId)
		{
			if (screenId == k_LeaderboardScreenId) m_leaderboard.Open();
			else m_leaderboard.Close();
		}

		private void SetScenario(BoardScenario scenario)
		{
			m_scenario = scenario;
			m_leaderboard.SelectTab(LeaderboardTab.Weekly);
			m_leaderboard.Reload();
		}

		private Task<TopScoresResult> LoadScenario(LeaderboardTab tab, int weeksBack)
		{
			if (tab == LeaderboardTab.AllTime) return Task.FromResult(LeaderboardPlaceholderData.AllTime(m_selfName));
			if (weeksBack > 0) return Task.FromResult(LeaderboardPlaceholderData.PastWeek(weeksBack, m_selfName));

			var board = LeaderboardPlaceholderData.AllTime(m_selfName).Rows;
			switch (m_scenario)
			{
				case BoardScenario.SelfInTopThree:
					var rows = new List<LeaderboardRow>(board);
					rows[1] = new LeaderboardRow(2, m_selfName, board[1].Score, true);
					return Task.FromResult(new TopScoresResult(true, rows, null));
				case BoardScenario.NoScore:
					return Task.FromResult(new TopScoresResult(true, board, null));
				case BoardScenario.TwoRows:
					return Task.FromResult(new TopScoresResult(true, new[] { board[0], board[1] },
						new LeaderboardRow(3, m_selfName, 184920, true)));
				case BoardScenario.Empty:
					return Task.FromResult(new TopScoresResult(true, new List<LeaderboardRow>(), null));
				default:
					return Task.FromResult(new TopScoresResult(true, board, new LeaderboardRow(248, m_selfName, 184920, true)));
			}
		}

		private void ShowToast(string message)
		{
			m_toastLabel.text = message;
			m_toast.style.display = DisplayStyle.Flex;

			m_toastHide?.Pause();
			m_toastHide = m_toast.schedule.Execute(() => m_toast.style.display = DisplayStyle.None);
			m_toastHide.ExecuteLater((long)(k_ToastSeconds * 1000f));
		}
	}
}
