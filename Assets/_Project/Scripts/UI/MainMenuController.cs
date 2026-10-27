using Alchemy.Inspector;
using Opoint8182.Daily;
using Opoint8182.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Opoint8182.UI
{
	/// <summary>
	/// Main menu (S02): chips, mode tabs, PLAY, overflow menu, attempt-confirm and quit-confirm popups, built on the
	/// UI foundation (<see cref="ScreenRouter"/>, <see cref="LipBox"/>, <see cref="TabBar"/>).
	/// The attempts, the practice clock and the confirm popup read <see cref="DailyPlay"/>; PLAY begins the run through it.
	/// Name and coins are still placeholders until the "Player profile &amp; run stats" milestone.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public class MainMenuController : MonoBehaviour
	{
		private enum PlayMode
		{
			Competitive,
			Practice,
		}

		private const string k_MenuScreenId = "menu";
		private const string k_SettingsScreenId = "settings";
		private const string k_LeaderboardScreenId = "leaderboard";
		private const string k_ProfileScreenId = "profile";
		private const string k_ShopScreenId = "shop";
		private const string k_CreditsScreenId = "credits";
		private const string k_ComingSoonMessage = "COMING SOON";

		// The counters can roll over at UTC midnight while the menu is open: reading them applies the new day, and this
		// refresh makes sure something reads them now and then.
		private const long k_RefreshMilliseconds = 30000;

		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private float m_baseSafeAreaPadding = 24f;

		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private ScreenRouter m_router;
		private SettingsScreen m_settingsScreen;
		private LeaderboardScreen m_leaderboardScreen;
		private ProfileScreen m_profileScreen;
		private ShopScreen m_shopScreen;
		private CreditsScreen m_creditsScreen;
		private Label m_profileNameLabel;

		private VisualElement m_statusCompetitive;
		private VisualElement m_statusPractice;
		private Label m_playSublabel;
		private VisualElement m_playPips;
		private VisualElement m_playButton;
		private Label m_statusCount;
		private Label m_statusClock;
		private Label m_attemptBody;
		private VisualElement m_statusPips;
		private VisualElement m_attemptPips;
		private bool m_refreshing;
		private VisualElement m_popupAttempt;
		private VisualElement m_popupQuit;
		private VisualElement m_popupOverflow;
		private Toggle m_dontAskToggle;
		private TabBar m_modeTabs;
		private UiToast m_toast;

		private PlayMode m_playMode = PlayMode.Competitive;

		private void Awake()
		{
			m_panelRenderer = GetComponent<PanelRenderer>();
		}

		private void OnEnable()
		{
			m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
			DailyPlay.Changed += OnDailyChanged;
		}

		private void OnDisable()
		{
			DailyPlay.Changed -= OnDailyChanged;
			m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
			m_loadedRoot = null;
			m_leaderboardScreen?.Close();
			m_profileScreen?.Close();
			m_router = null;
			m_settingsScreen = null;
			m_leaderboardScreen = null;
			m_profileScreen = null;
			m_shopScreen = null;
			m_creditsScreen = null;
			m_profileNameLabel = null;
		}

		private void Update()
		{
			if (m_router == null) return;

			// Escape doubles as the Android back button in the Input System. Closes an open popup; nothing at the root.
			var keyboard = Keyboard.current;
			if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

			// Escape while renaming only leaves the text field; the screen stays.
			if (m_router.CurrentScreenId == k_ProfileScreenId && m_profileScreen.CancelEdit()) return;

			m_router.HandleBackRequested();
		}

		// Can fire more than once per enable (live asset reload); a repeat call on the same tree must not double-register.
		// A new tree replaces the old elements, so nothing needs unregistering.
		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			SafeAreaHelper.Apply(root.Q<VisualElement>("safe-area"), m_baseSafeAreaPadding);

			m_router = new ScreenRouter(root.Q<VisualElement>("popup-layer"));
			m_router.Register(k_MenuScreenId, root.Q<VisualElement>("screen-menu"));
			m_router.Register(k_SettingsScreenId, root.Q<VisualElement>("screen-settings"));
			m_router.Register(k_LeaderboardScreenId, root.Q<VisualElement>("screen-leaderboard"));
			m_router.Register(k_ProfileScreenId, root.Q<VisualElement>("screen-profile"));
			m_router.Register(k_ShopScreenId, root.Q<VisualElement>("screen-shop"));
			m_router.Register(k_CreditsScreenId, root.Q<VisualElement>("screen-credits"));
			m_router.SetRoot(k_MenuScreenId);

			m_settingsScreen = new SettingsScreen(root.Q<VisualElement>("screen-settings"));
			m_settingsScreen.BackRequested += () => m_router.Back();

			m_leaderboardScreen = new LeaderboardScreen(root.Q<VisualElement>("screen-leaderboard"));
			m_leaderboardScreen.BackRequested += () => m_router.Back();
			m_leaderboardScreen.ShareRequested += () => m_toast.Show(GameOverPlaceholderData.ArrivesLaterMessage);
			m_leaderboardScreen.FlyNowRequested += OnLeaderboardFlyNow;

			m_profileScreen = new ProfileScreen(root.Q<VisualElement>("screen-profile"));
			m_profileScreen.BackRequested += () => m_router.Back();

			m_shopScreen = new ShopScreen(root.Q<VisualElement>("screen-shop"));
			m_shopScreen.BackRequested += () => m_router.Back();
			m_shopScreen.ActionRequested += () => m_toast.Show(GameOverPlaceholderData.ArrivesLaterMessage);

			m_creditsScreen = new CreditsScreen(root.Q<VisualElement>("screen-credits"));
			m_creditsScreen.BackRequested += () => m_router.Back();
			m_creditsScreen.LinkRequested += OnCreditsLink;
			m_router.ScreenChanged += OnScreenChanged;

			m_statusCompetitive = root.Q<VisualElement>("status-competitive");
			m_statusPractice = root.Q<VisualElement>("status-practice");
			m_playSublabel = root.Q<Label>("play-sublabel");
			m_playPips = root.Q<VisualElement>("play-pips");
			m_playButton = root.Q<VisualElement>("play-button");
			m_statusCount = root.Q<Label>("status-count");
			m_statusClock = root.Q<Label>("status-clock");
			m_attemptBody = root.Q<Label>("attempt-body");
			m_statusPips = root.Q<VisualElement>("status-pips");
			m_attemptPips = root.Q<VisualElement>("attempt-pips");
			m_popupAttempt = root.Q<VisualElement>("popup-attempt");
			m_popupQuit = root.Q<VisualElement>("popup-quit");
			m_popupOverflow = root.Q<VisualElement>("popup-overflow");
			m_dontAskToggle = root.Q<Toggle>("toggle-dont-ask");
			m_modeTabs = root.Q<TabBar>("mode-tabs");

			// The router only hides the popup it closes; all three start hidden inside the (hidden) layer.
			m_popupAttempt.style.display = DisplayStyle.None;
			m_popupQuit.style.display = DisplayStyle.None;
			m_popupOverflow.style.display = DisplayStyle.None;

			m_toast = new UiToast(root.Q<VisualElement>("root"));

			PopulateStaticData(root);
			WireHeader(root);
			WireTiles(root);
			WirePlay(root);
			WirePopups(root);

			// Back from a practice run, the menu comes back on the Practice tab.
			var practice = DailyPlay.LastMode == RunMode.Practice;
			m_playMode = practice ? PlayMode.Practice : PlayMode.Competitive;
			m_modeTabs.SetSelectedIndexWithoutNotify(practice ? 1 : 0);
			RefreshDaily();
			root.schedule.Execute(RefreshDaily).Every(k_RefreshMilliseconds);
		}

		private void PopulateStaticData(VisualElement root)
		{
			m_profileNameLabel = root.Q<Label>("profile-name");
			m_profileNameLabel.text = UiProfileState.DisplayName;
			root.Q<Label>("currency-value").text = ProfilePlaceholderData.COINS;
		}

		private static void FillPips(VisualElement container, int max, int left)
		{
			container.Clear();
			for (var i = 0; i < max; i++)
			{
				var pip = new VisualElement { pickingMode = PickingMode.Ignore };
				pip.AddToClassList("pip");
				pip.EnableInClassList("pip--off", i >= left);
				container.Add(pip);
			}
		}

		private void OnDailyChanged()
		{
			if (m_loadedRoot != null) RefreshDaily();
		}

		private void WireHeader(VisualElement root)
		{
			var profileChip = root.Q<VisualElement>("profile-chip");
			MakePressable(profileChip);
			profileChip.RegisterCallback<ClickEvent>(_ => m_router.Show(k_ProfileScreenId));

			// Coins are earned, never bought, so the chip has no "+": it opens the shop instead.
			var currencyChip = root.Q<VisualElement>("currency-chip");
			MakePressable(currencyChip);
			currencyChip.RegisterCallback<ClickEvent>(_ => m_router.Show(k_ShopScreenId));

			root.Q<LipButton>("menu-button").clicked += () => m_router.ShowPopup(m_popupOverflow);
		}

		private void WireTiles(VisualElement root)
		{
			var shopTile = root.Q<VisualElement>("shop-tile");
			MakePressable(shopTile);
			shopTile.RegisterCallback<ClickEvent>(_ => m_router.Show(k_ShopScreenId));

			var leaderboardTile = root.Q<VisualElement>("leaderboard-tile");
			MakePressable(leaderboardTile);
			leaderboardTile.RegisterCallback<ClickEvent>(_ => m_router.Show(k_LeaderboardScreenId));
		}

		private void WirePlay(VisualElement root)
		{
			var playButton = root.Q<VisualElement>("play-button");
			MakePressable(playButton);
			playButton.RegisterCallback<ClickEvent>(_ => OnPlayClicked());

			m_modeTabs.changed += index =>
			{
				m_playMode = index == 0 ? PlayMode.Competitive : PlayMode.Practice;
				RefreshDaily();
			};
		}

		private void WirePopups(VisualElement root)
		{
			root.Q<LipButton>("attempt-yes").clicked += OnAttemptConfirmed;
			root.Q<LipButton>("attempt-no").clicked += () => m_router.ClosePopup();
			root.Q<Label>("attempt-practice-link").RegisterCallback<ClickEvent>(_ =>
			{
				m_router.ClosePopup();
				m_modeTabs.selectedIndex = 1;
			});

			root.Q<LipButton>("quit-yes").clicked += QuitGame;
			root.Q<LipButton>("quit-no").clicked += () => m_router.ClosePopup();

			WireOverflowRow(root, "overflow-settings", () => m_router.Show(k_SettingsScreenId));
			WireOverflowRow(root, "overflow-howto", () => ShowComingLater());
			WireOverflowRow(root, "overflow-credits", () => m_router.Show(k_CreditsScreenId));
			WireOverflowRow(root, "overflow-quit", () => m_router.ShowPopup(m_popupQuit));

			// No quitting a browser tab: the Quit entry does not exist on WebGL.
			if (Application.platform == RuntimePlatform.WebGLPlayer) root.Q<VisualElement>("overflow-quit").style.display = DisplayStyle.None;
		}

		private void WireOverflowRow(VisualElement root, string rowName, System.Action onClick)
		{
			var row = root.Q<VisualElement>(rowName);
			MakePressable(row);
			row.RegisterCallback<ClickEvent>(_ => onClick());
		}

		private static void MakePressable(VisualElement element)
		{
			element.AddManipulator(new PressedClassManipulator());
		}

		// The leaderboard loads when it becomes the visible screen and drops its request when it is left.
		private void OnScreenChanged(string screenId)
		{
			if (screenId == k_LeaderboardScreenId) m_leaderboardScreen.Open();
			else m_leaderboardScreen.Close();

			if (screenId == k_ProfileScreenId) m_profileScreen.Open();
			else m_profileScreen.Close();

			if (screenId == k_ShopScreenId) m_shopScreen.Open();

			// The profile screen may have renamed the player; the chip follows when the menu is shown again.
			if (screenId == k_MenuScreenId) m_profileNameLabel.text = UiProfileState.DisplayName;
		}

		// An empty URL is a placeholder chip that is not filled in yet: nothing to open.
		private static void OnCreditsLink(CreditsLink link)
		{
			if (string.IsNullOrEmpty(link.Url)) return;

			Application.OpenURL(link.Url);
		}

		// FLY NOW on an empty board is the same path as PLAY, in competitive mode (the board only counts those runs).
		private void OnLeaderboardFlyNow()
		{
			m_router.Back();
			m_modeTabs.selectedIndex = 0;
			OnPlayClicked();
		}

		private RunMode CurrentRunMode => m_playMode == PlayMode.Competitive ? RunMode.Competitive : RunMode.Practice;

		// Fills every chip, pip, label and the PLAY state from the daily counters and the selected mode. Reading the counters
		// applies a new UTC day first, which raises Changed and comes back here, hence the guard.
		private void RefreshDaily()
		{
			if (m_refreshing || m_loadedRoot == null) return;

			m_refreshing = true;
			try
			{
				var max = DailyPlay.ATTEMPTS_MAX;
				var left = DailyPlay.AttemptsLeft;
				var practiceLeft = DailyPlay.PracticeRemainingSeconds;
				var nextReset = DailyPlay.NextResetUtc;
				var now = DailyPlay.Clock.UtcNow;

				m_statusCount.text = DailyPlayText.AttemptsCount(left, max);
				m_statusClock.text = DailyPlayText.PracticeClock(practiceLeft);
				FillPips(m_statusPips, max, left);
				FillPips(m_playPips, max, left);
				FillPips(m_attemptPips, max, left);
				m_attemptBody.text = DailyPlayText.ConfirmBody(left, max);

				var competitive = m_playMode == PlayMode.Competitive;
				m_statusCompetitive.style.display = competitive ? DisplayStyle.Flex : DisplayStyle.None;
				m_statusPractice.style.display = competitive ? DisplayStyle.None : DisplayStyle.Flex;
				m_playPips.style.display = competitive ? DisplayStyle.Flex : DisplayStyle.None;

				// Nothing left: PLAY is greyed and inert and says when it comes back, in plain hours (no red, no countdown).
				var locked = DailyPlay.CanBeginRun(CurrentRunMode) != BeginRunResult.Started;
				m_playButton.EnableInClassList("menu-play--locked", locked);
				if (competitive)
				{
					m_playSublabel.text = left > 0 ? DailyPlayText.AttemptsLeft(left) : DailyPlayText.NewAttemptsIn(nextReset, now);
				}
				else
				{
					m_playSublabel.text = practiceLeft > 0f ? DailyPlayText.PracticeLeft(practiceLeft) : DailyPlayText.PracticeReturnsIn(nextReset, now);
				}
			}
			finally
			{
				m_refreshing = false;
			}
		}

		private void OnPlayClicked()
		{
			// A locked PLAY (no attempts or practice time left) does nothing; the sub-label says when it returns.
			if (DailyPlay.CanBeginRun(CurrentRunMode) != BeginRunResult.Started)
			{
				RefreshDaily();
				return;
			}

			// Practice never confirms; competitive confirms unless the player opted out earlier today.
			if (m_playMode == PlayMode.Competitive && !DailyPlay.DontAskAgainToday)
			{
				m_dontAskToggle.SetValueWithoutNotify(false);
				m_router.ShowPopup(m_popupAttempt);
				return;
			}

			StartRun();
		}

		private void OnAttemptConfirmed()
		{
			if (m_dontAskToggle.value) DailyPlay.DontAskAgainToday = true;
			m_router.ClosePopup();
			StartRun();
		}

		private void StartRun()
		{
			Debug.Log($"[MainMenu] PLAY pressed ({m_playMode})");

			// The attempt is spent here, before the run starts, so the HUD builds with the right mode. The state can have
			// changed since the button was drawn (UTC midnight, another instance): then the menu just refreshes.
			if (DailyPlay.BeginRun(CurrentRunMode) != BeginRunResult.Started)
			{
				RefreshDaily();
				return;
			}

			// Hide immediately so there's no chance of a leftover-menu flash once gameplay
			// resumes - GameManager.EnterPlayingState only takes effect next frame (see
			// GameManager's own comment on why), unloading MainMenu is fire-and-forget.
			m_loadedRoot.style.display = DisplayStyle.None;
			GameManager.TryGetInstance()?.EnterPlayingState();
			SceneManager.UnloadSceneAsync(gameObject.scene);
		}

		private static void QuitGame()
		{
#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		private void ShowComingLater()
		{
			m_router.ClosePopup();
			m_toast.Show(k_ComingSoonMessage);
		}
	}
}
