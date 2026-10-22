using Alchemy.Inspector;
using Opoint8182.Altitude;
using Opoint8182.Fuel;
using Opoint8182.Game;
using Opoint8182.Health;
using Opoint8182.Lateral;
using Opoint8182.Player;
using Opoint8182.Score;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Binds the gameplay systems (fuel, health, score/combo) to the merged HUD view. Replaces the per-element
	/// FuelGaugeUI, HealthGaugeUI and ScoreUI scripts. Also the bridge to <see cref="GameManager"/>'s pause: the pause
	/// button and the back key (Escape, Android back) ask it to pause, its Paused / Resumed events drive the popup, and
	/// the popup's RESUME countdown, RESTART RUN and QUIT TO MENU call back into it.
	/// </summary>
	public class HudPresenter : HudHost
	{
		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private FuelSystem m_fuelSystem;
		[FoldoutGroup("References")] [SerializeField] private HealthSystem m_healthSystem;
		[FoldoutGroup("References")] [SerializeField] private ScoreSystem m_scoreSystem;
		[FoldoutGroup("References")] [SerializeField] private PlaneController m_planeController;
		[FoldoutGroup("References")] [SerializeField] private AltitudeSystem m_altitudeSystem;
		[FoldoutGroup("References")] [SerializeField] private LateralSystem m_lateralSystem;

		[Title("Tunables")]
		[FoldoutGroup("Tunables")] [SerializeField] [Range(0f, 1f)] private float m_lowFuelThreshold = HudView.DefaultLowFuelThreshold;
		[FoldoutGroup("Tunables")] [SerializeField] [Range(0f, 1f)] private float m_lowHealthThreshold = HudView.DefaultLowHealthThreshold;

		private float m_lastHealth = -1f;
		private GameManager m_gameManager;

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_fuelSystem != null) m_fuelSystem.FuelValue.AddListener(OnFuelChanged);
			if (m_healthSystem != null) m_healthSystem.HealthValue.AddListener(OnHealthChanged);
			if (m_scoreSystem != null)
			{
				m_scoreSystem.ScoreValue.AddListener(OnScoreChanged);
				m_scoreSystem.MultiplierValue.AddListener(OnMultiplierChanged);
				m_scoreSystem.ComboTimerValue.AddListener(OnComboTimerChanged);
				m_scoreSystem.PointsAwarded += OnPointsAwarded;
			}
			if (m_altitudeSystem != null)
			{
				m_altitudeSystem.IsWarningValue.AddListener(OnCeilingWarningChanged);
				m_altitudeSystem.WarningFractionValue.AddListener(OnCeilingFractionChanged);
				m_altitudeSystem.IsGroundWarningValue.AddListener(OnGroundWarningChanged);
			}
			if (m_lateralSystem != null) m_lateralSystem.IsWarningValue.AddListener(OnLateralWarningChanged);

			// The HUD object is toggled across the menu <-> play transition, so OnEnable runs again later; subscribing
			// here too (once Start has cached the manager) keeps RunEnded from being dropped, same as GameOverUI.
			if (m_gameManager != null) SubscribeToGameManager();
		}

		// GameManager is a GenericSingleton set up in Awake; Start is the first point where it is guaranteed to exist.
		private void Start()
		{
			m_gameManager = GameManager.TryGetInstance();
			if (m_gameManager != null) SubscribeToGameManager();
		}

		protected override void OnDisable()
		{
			if (m_fuelSystem != null) m_fuelSystem.FuelValue.RemoveListener(OnFuelChanged);
			if (m_healthSystem != null) m_healthSystem.HealthValue.RemoveListener(OnHealthChanged);
			if (m_scoreSystem != null)
			{
				m_scoreSystem.ScoreValue.RemoveListener(OnScoreChanged);
				m_scoreSystem.MultiplierValue.RemoveListener(OnMultiplierChanged);
				m_scoreSystem.ComboTimerValue.RemoveListener(OnComboTimerChanged);
				m_scoreSystem.PointsAwarded -= OnPointsAwarded;
			}
			if (m_altitudeSystem != null)
			{
				m_altitudeSystem.IsWarningValue.RemoveListener(OnCeilingWarningChanged);
				m_altitudeSystem.WarningFractionValue.RemoveListener(OnCeilingFractionChanged);
				m_altitudeSystem.IsGroundWarningValue.RemoveListener(OnGroundWarningChanged);
			}
			if (m_lateralSystem != null) m_lateralSystem.IsWarningValue.RemoveListener(OnLateralWarningChanged);
			if (m_gameManager != null) UnsubscribeFromGameManager();

			// A held finger must not keep steering or boosting once the HUD is gone (run ended, back to menu).
			if (m_planeController != null)
			{
				m_planeController.TouchSteer = Vector2.zero;
				m_planeController.TouchBoost = false;
			}

			base.OnDisable();
		}

		protected override void OnViewReady(HudView view)
		{
			var gameManager = m_gameManager != null ? m_gameManager : GameManager.TryGetInstance();
			view.SetPractice(gameManager != null && gameManager.IsPracticeRun);
			view.SteerChanged += OnSteerChanged;
			view.BoostChanged += OnBoostChanged;
			view.PauseClicked += OnPauseClicked;
			view.PausePopup.CountdownFinished += OnCountdownFinished;
			view.PausePopup.RestartRequested += OnRestartRequested;
			view.PausePopup.QuitRequested += OnQuitRequested;

			if (m_fuelSystem != null)
			{
				view.SetFuel(m_fuelSystem.FuelFraction);
				view.SetLowFuel(m_fuelSystem.FuelFraction <= m_lowFuelThreshold);
			}
			if (m_healthSystem != null)
			{
				view.SetHealth(m_healthSystem.HealthFraction);
				view.SetLowHealth(m_healthSystem.HealthFraction <= m_lowHealthThreshold);
				m_lastHealth = m_healthSystem.HealthValue.Value;
			}
			if (m_scoreSystem != null)
			{
				view.SetScore(m_scoreSystem.CurrentScore);
				view.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);
			}
			if (m_altitudeSystem != null)
			{
				SyncCeilingWarning(view);
				view.SetGroundWarning(m_altitudeSystem.IsGroundWarningValue.Value);
			}
			if (m_lateralSystem != null) view.SetLateralWarning(m_lateralSystem.IsWarningValue.Value ? LateralSide() : 0);
		}

		// The back key: pauses a running game, steps back out of the leave confirm, and resumes (with the countdown) from
		// the pause popup. Ignored during the
		// countdown and once the run has ended (GameManager.Pause refuses).
		private void Update()
		{
			if (m_gameManager == null || View == null) return;

			UpdateRotateGate();

			var keyboard = Keyboard.current;
			if (View.RotateGateVisible || keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

			if (!m_gameManager.IsPaused) m_gameManager.Pause();
			else if (View.PausePopup.IsLeaveConfirmOpen) View.PausePopup.CancelLeaveConfirm();
			else if (View.PausePopup.IsOpen) View.PausePopup.RequestResume();
		}

		// Portrait during a run (a phone browser page turned sideways): freeze at once and cover everything with the prompt.
		// Landscape again hides it and leaves the pause popup, so the player resumes with the usual countdown.
		private void UpdateRotateGate()
		{
			// Only while the run is live: Game Over has its own panel, and the menu is not gated (ui.md decision 11).
			var portrait = m_gameManager.IsRunActive && LaunchFlow.GateApplies() && LaunchFlow.IsPortrait(Screen.width, Screen.height);
			if (portrait == View.RotateGateVisible) return;

			if (portrait)
			{
				m_gameManager.PauseNow();
				if (View.PausePopup.IsCountingDown) View.PausePopup.Show();
			}

			View.SetRotateGate(portrait);
		}

		// Backgrounded during the 3-2-1: the countdown runs on unscaled time and would finish unseen on return. GameManager
		// pauses on focus loss itself; this only puts the popup back.
		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus || View == null || !View.PausePopup.IsCountingDown) return;

			View.PausePopup.Show();
		}

		private void SubscribeToGameManager()
		{
			m_gameManager.RunEnded += OnRunEnded;
			m_gameManager.Paused += OnPaused;
			m_gameManager.Resumed += OnResumed;
		}

		private void UnsubscribeFromGameManager()
		{
			m_gameManager.RunEnded -= OnRunEnded;
			m_gameManager.Paused -= OnPaused;
			m_gameManager.Resumed -= OnResumed;
		}

		private void OnRunEnded(RunEndCause _) => View?.SetRunEnded(true);

		private void OnPauseClicked() => m_gameManager?.Pause();

		private void OnPaused()
		{
			if (View == null) return;

			View.SetPaused(true);
			View.PausePopup.Show();
		}

		private void OnResumed()
		{
			if (View == null) return;

			View.PausePopup.Hide();
			View.SetPaused(false);
		}

		// The game stays frozen through the 3-2-1; it unfreezes only when the countdown ends.
		private void OnCountdownFinished() => m_gameManager?.Resume();

		private void OnRestartRequested() => m_gameManager?.RestartRun();

		// Stub until the run / menu flow exists (Daily play structure): say so and stay paused on the popup.
		private void OnQuitRequested()
		{
			if (View == null) return;

			View.ShowToast(GameOverPlaceholderData.ArrivesLaterMessage);
			View.PausePopup.Show();
		}

		private void OnSteerChanged(Vector2 steer)
		{
			if (m_planeController != null) m_planeController.TouchSteer = steer;
		}

		private void OnBoostChanged(bool isBoosting)
		{
			if (m_planeController != null) m_planeController.TouchBoost = isBoosting;
		}

		private void OnCeilingWarningChanged(bool _) => SyncCeilingWarning(View);

		private void OnCeilingFractionChanged(float _) => SyncCeilingWarning(View);

		private void OnGroundWarningChanged(bool isWarning) => View?.SetGroundWarning(isWarning);

		private void OnLateralWarningChanged(bool isWarning) => View?.SetLateralWarning(isWarning ? LateralSide() : 0);

		// Countdown number = whole seconds left of the ceiling grace period (rounded up so it never shows 0 while still alive).
		private void SyncCeilingWarning(HudView view)
		{
			if (view == null || m_altitudeSystem == null) return;

			var seconds = Mathf.CeilToInt(m_altitudeSystem.WarningFractionValue.Value * m_altitudeSystem.CeilingWarningSeconds);
			view.SetCeilingWarning(m_altitudeSystem.IsWarningValue.Value, seconds);
		}

		// The lateral system only says "soft limit crossed"; the offending edge is the side of the plane's x position.
		private int LateralSide()
		{
			return m_planeController != null && m_planeController.transform.position.x < 0f ? -1 : 1;
		}

		private void OnFuelChanged(float _)
		{
			if (View == null) return;
			View.SetFuel(m_fuelSystem.FuelFraction);
			View.SetLowFuel(m_fuelSystem.FuelFraction <= m_lowFuelThreshold);
		}

		// A drop in health is damage taken (any source, whatever its multiplier): show cracks and a sticker with the HP lost.
		private void OnHealthChanged(float health)
		{
			if (View == null) return;
			View.SetHealth(m_healthSystem.HealthFraction);
			View.SetLowHealth(m_healthSystem.HealthFraction <= m_lowHealthThreshold);

			if (m_lastHealth >= 0f && health < m_lastHealth) View.ShowDamage(m_lastHealth - health);
			m_lastHealth = health;
		}

		private void OnScoreChanged(int score) => View?.SetScore(score);

		// Anchors the popup where the crash happened on screen. The point is converted once, at the impact: the plane flies
		// through it, so tracking the world point would slide the popup away instead of reading as a hit marker.
		private void OnPointsAwarded(int points, Vector3 worldPosition, bool perfect)
		{
			if (View == null || View.Root.panel == null) return;

			var coins = HudPlaceholderData.ShowCoinPopup ? HudPlaceholderData.CoinPopupAmount : 0;
			var camera = Camera.main;
			if (camera == null || camera.WorldToViewportPoint(worldPosition).z <= 0f)
			{
				// Impact behind the camera: fall back to the middle of the frame.
				var size = View.Root.layout.size;
				View.ShowImpactPopup(new Vector2(size.x * 0.5f, size.y * 0.4f), points, perfect, coins);
				return;
			}

			var panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(View.Root.panel, worldPosition, camera);
			View.ShowImpactPopup(panelPosition, points, perfect, coins);
		}

		private void OnMultiplierChanged(int _) => View?.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);

		private void OnComboTimerChanged(float _) => View?.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);
	}
}
