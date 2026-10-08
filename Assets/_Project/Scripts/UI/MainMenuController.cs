using System.Collections;
using Alchemy.Inspector;
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
	/// Attempts, practice clock, name and coins are static placeholders (see the Placeholder data block): the real
	/// systems arrive with the "Daily play structure" and "Player profile &amp; run stats" milestones.
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

		// ---- Placeholder data (UI shell only, replaced by the real systems in later milestones) ----
		private const string k_PlaceholderPlayerName = "OLIVE";
		private const string k_PlaceholderCoins = "12,480";
		private const int k_PlaceholderAttemptsMax = 3;
		private const int k_PlaceholderAttemptsLeft = 3;
		private const string k_PlaceholderPracticeClock = "28:14";

		// Kept for the whole play session: the menu scene is unloaded when a run starts and loaded again when the
		// player returns, so an instance field would forget it. Deliberately not persisted (Daily play structure
		// decides that, decision 4 of the UI/art pass).
		private static bool m_s_dontAskAttemptConfirm;

		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private float m_baseSafeAreaPadding = 24f;

		[Title("Stub toast")]
		[FoldoutGroup("Stub toast")] [SerializeField] private string m_stubToastMessage = "IMPLEMENTED IN ALPHA";
		[FoldoutGroup("Stub toast")] [SerializeField] private string m_comingLaterMessage = "COMING IN A LATER PASS";
		[FoldoutGroup("Stub toast")] [SerializeField] private float m_stubToastDuration = 3f;
		[FoldoutGroup("Stub toast")] [SerializeField] private float m_rapidClickWindowSeconds = 1.5f;
		[FoldoutGroup("Stub toast")] [SerializeField] private int m_rapidClickThreshold = 3;
		[FoldoutGroup("Stub toast")]
		[SerializeField]
		private string[] m_rapidClickJokeMessages =
		{
			"OK OK, IT'S STILL ALPHA",
			"THE DEV IS ONE PERSON, BE NICE",
			"CLICKING HARDER WON'T HELP",
			"THAT TICKLES",
			"PATIENCE. ALPHA. SOON(ish).",
		};

		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private ScreenRouter m_router;

		private VisualElement m_statusCompetitive;
		private VisualElement m_statusPractice;
		private Label m_playSublabel;
		private VisualElement m_playPips;
		private VisualElement m_popupAttempt;
		private VisualElement m_popupQuit;
		private VisualElement m_popupOverflow;
		private Toggle m_dontAskToggle;
		private TabBar m_modeTabs;
		private VisualElement m_stubToast;
		private Label m_stubToastLabel;

		private PlayMode m_playMode = PlayMode.Competitive;
		private Coroutine m_hideStubToastCoroutine;
		private float m_lastStubClickTime = -999f;
		private int m_rapidClickStreak;
		private int m_jokeMessageIndex;

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
			m_loadedRoot = null;
			m_router = null;
		}

		private void Update()
		{
			if (m_router == null) return;

			// Escape doubles as the Android back button in the Input System. Closes an open popup; nothing at the root.
			var keyboard = Keyboard.current;
			if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) m_router.HandleBackRequested();
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
			m_router.SetRoot(k_MenuScreenId);

			m_statusCompetitive = root.Q<VisualElement>("status-competitive");
			m_statusPractice = root.Q<VisualElement>("status-practice");
			m_playSublabel = root.Q<Label>("play-sublabel");
			m_playPips = root.Q<VisualElement>("play-pips");
			m_popupAttempt = root.Q<VisualElement>("popup-attempt");
			m_popupQuit = root.Q<VisualElement>("popup-quit");
			m_popupOverflow = root.Q<VisualElement>("popup-overflow");
			m_dontAskToggle = root.Q<Toggle>("toggle-dont-ask");
			m_modeTabs = root.Q<TabBar>("mode-tabs");

			// The router only hides the popup it closes; all three start hidden inside the (hidden) layer.
			m_popupAttempt.style.display = DisplayStyle.None;
			m_popupQuit.style.display = DisplayStyle.None;
			m_popupOverflow.style.display = DisplayStyle.None;

			m_stubToast = root.Q<VisualElement>("stub-toast");
			m_stubToastLabel = root.Q<Label>("stub-toast-label");

			PopulateStaticData(root);
			WireHeader(root);
			WireTiles(root);
			WirePlay(root);
			WirePopups(root);

			m_playMode = PlayMode.Competitive;
			m_modeTabs.SetSelectedIndexWithoutNotify(0);
			RefreshMode();
		}

		private void PopulateStaticData(VisualElement root)
		{
			root.Q<Label>("profile-name").text = k_PlaceholderPlayerName;
			root.Q<Label>("currency-value").text = k_PlaceholderCoins;
			root.Q<Label>("status-count").text = $"{k_PlaceholderAttemptsLeft}/{k_PlaceholderAttemptsMax}";
			root.Q<Label>("status-clock").text = k_PlaceholderPracticeClock;

			FillPips(root.Q<VisualElement>("status-pips"));
			FillPips(m_playPips);
			FillPips(root.Q<VisualElement>("attempt-pips"));

			root.Q<Label>("attempt-body").text =
				$"This run counts toward this week's leaderboard. You have {k_PlaceholderAttemptsLeft} of {k_PlaceholderAttemptsMax} attempts left today.";
		}

		private static void FillPips(VisualElement container)
		{
			container.Clear();
			for (var i = 0; i < k_PlaceholderAttemptsMax; i++)
			{
				var pip = new VisualElement { pickingMode = PickingMode.Ignore };
				pip.AddToClassList("pip");
				pip.EnableInClassList("pip--off", i >= k_PlaceholderAttemptsLeft);
				container.Add(pip);
			}
		}

		private void WireHeader(VisualElement root)
		{
			MakePressable(root.Q<VisualElement>("profile-chip"));
			root.Q<VisualElement>("profile-chip").RegisterCallback<ClickEvent>(_ => ShowStubToast(m_stubToastMessage));

			root.Q<LipButton>("currency-add-button").clicked += () => ShowStubToast(m_stubToastMessage);
			root.Q<LipButton>("menu-button").clicked += () => m_router.ShowPopup(m_popupOverflow);
		}

		private void WireTiles(VisualElement root)
		{
			var shopTile = root.Q<VisualElement>("shop-tile");
			MakePressable(shopTile);
			shopTile.RegisterCallback<ClickEvent>(_ => ShowStubToast(m_stubToastMessage));

			// LeaderboardPanelUI owns the click (opens the Top-10 panel); this only adds the press state.
			MakePressable(root.Q<VisualElement>("leaderboard-tile"));
		}

		private void WirePlay(VisualElement root)
		{
			var playButton = root.Q<VisualElement>("play-button");
			MakePressable(playButton);
			playButton.RegisterCallback<ClickEvent>(_ => OnPlayClicked());

			m_modeTabs.changed += index =>
			{
				m_playMode = index == 0 ? PlayMode.Competitive : PlayMode.Practice;
				RefreshMode();
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

			WireOverflowRow(root, "overflow-settings", () => ShowComingLater());
			WireOverflowRow(root, "overflow-howto", () => ShowComingLater());
			WireOverflowRow(root, "overflow-credits", () => ShowComingLater());
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

		private void RefreshMode()
		{
			var competitive = m_playMode == PlayMode.Competitive;
			m_statusCompetitive.style.display = competitive ? DisplayStyle.Flex : DisplayStyle.None;
			m_statusPractice.style.display = competitive ? DisplayStyle.None : DisplayStyle.Flex;
			m_playPips.style.display = competitive ? DisplayStyle.Flex : DisplayStyle.None;
			m_playSublabel.text = competitive
				? $"{k_PlaceholderAttemptsLeft} ATTEMPTS LEFT TODAY"
				: $"{k_PlaceholderPracticeClock} LEFT TODAY";
		}

		private void OnPlayClicked()
		{
			// Practice never confirms; competitive confirms unless the player opted out earlier this session.
			if (m_playMode == PlayMode.Competitive && !m_s_dontAskAttemptConfirm)
			{
				m_dontAskToggle.SetValueWithoutNotify(false);
				m_router.ShowPopup(m_popupAttempt);
				return;
			}

			StartRun();
		}

		private void OnAttemptConfirmed()
		{
			if (m_dontAskToggle.value) m_s_dontAskAttemptConfirm = true;
			m_router.ClosePopup();
			StartRun();
		}

		private void StartRun()
		{
			Debug.Log($"[MainMenu] PLAY pressed ({m_playMode})");

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
			ShowStubToast(m_comingLaterMessage);
		}

		// Placeholder affordance for chips/tiles whose screen lands in a later pass; deleted in Pass 8.
		private void ShowStubToast(string baseMessage)
		{
			var now = Time.unscaledTime;
			m_rapidClickStreak = now - m_lastStubClickTime <= m_rapidClickWindowSeconds ? m_rapidClickStreak + 1 : 1;
			m_lastStubClickTime = now;

			m_stubToastLabel.text = m_rapidClickStreak >= m_rapidClickThreshold ? NextJokeMessage(baseMessage) : baseMessage;
			m_stubToast.style.display = DisplayStyle.Flex;

			if (m_hideStubToastCoroutine != null) StopCoroutine(m_hideStubToastCoroutine);
			m_hideStubToastCoroutine = StartCoroutine(HideStubToastAfterDelay());
		}

		private string NextJokeMessage(string fallback)
		{
			if (m_rapidClickJokeMessages.Length == 0) return fallback;

			var message = m_rapidClickJokeMessages[m_jokeMessageIndex % m_rapidClickJokeMessages.Length];
			m_jokeMessageIndex++;
			return message;
		}

		private IEnumerator HideStubToastAfterDelay()
		{
			yield return new WaitForSecondsRealtime(m_stubToastDuration);
			m_stubToast.style.display = DisplayStyle.None;
			m_hideStubToastCoroutine = null;
		}
	}
}
