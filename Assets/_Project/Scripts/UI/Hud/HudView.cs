using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Pure UI for the merged gameplay HUD (mockup S03): takes plain values through setters and knows nothing about the
	/// game systems, so the same view runs in play (driven by <see cref="HudPresenter"/>) and in the HudGallery
	/// (driven by <see cref="HudStateDriver"/>).
	/// </summary>
	public class HudView
	{
		public const int FuelPipCount = 10;

		/// <summary>Fuel fraction at or below which the low-fuel state shows (HudPresenter exposes it as a tunable).</summary>
		public const float DefaultLowFuelThreshold = 0.25f;

		/// <summary>Health fraction at or below which the low-health glow shows (HudPresenter exposes it as a tunable).</summary>
		public const float DefaultLowHealthThreshold = 0.3f;

		private const float k_ComboMinScale = 0.78f;
		private const float k_ComboMinOpacity = 0.7f;
		private const long k_ToastDurationMs = 2500;
		private const long k_GroundFlashPeriodMs = 350;
		private const float k_GroundFlashHigh = 0.85f;
		private const float k_GroundFlashLow = 0.25f;
		private const long k_LowFuelFlashPeriodMs = 500;
		private const long k_DamageDurationMs = 1500;
		private const float k_PopupMarginX = 300f;
		private const float k_PopupMarginTop = 300f;
		private const float k_PopupMarginBottom = 200f;

		// Health bar colour: full green above 75%, full red at 25% and below, lerped in between.
		private const float k_HealthRedAt = 0.25f;
		private const float k_HealthGreenAt = 0.75f;
		private static readonly Color m_S_HealthGreen = new(0.373f, 0.761f, 0.416f);
		private static readonly Color m_S_HealthRed = new(0.878f, 0.271f, 0.184f);
		private static readonly Color m_S_ComboFadedColor = new(0.49f, 0.416f, 0.388f);
		private static readonly Color m_S_ComboActiveColor = new(0.91f, 0.376f, 0.235f);

		private readonly Label m_scoreValue;
		private readonly VisualElement[] m_pipFills = new VisualElement[FuelPipCount];
		private readonly VisualElement m_healthFill;
		private readonly VisualElement m_practiceTag;
		private readonly VisualElement m_comboSticker;
		private readonly VisualElement m_comboFace;
		private readonly Label m_comboLabel;
		private readonly VisualElement m_toast;
		private readonly Label m_toastLabel;
		private readonly HudTouchControls m_touchControls;
		private readonly VisualElement m_topStack;
		private readonly VisualElement m_ceilingBanner;
		private readonly Label m_ceilingCount;
		private readonly VisualElement m_lateralLeft;
		private readonly VisualElement m_lateralRight;
		private readonly VisualElement m_groundVignette;
		private readonly VisualElement m_groundText;
		private IVisualElementScheduledItem m_groundFlash;
		private bool m_groundActive;
		private bool m_groundFlashHigh;
		private readonly VisualElement m_fuelCluster;
		private readonly Label m_fuelLabel;
		private readonly VisualElement m_healthWrap;
		private readonly VisualElement m_damageOverlay;
		private readonly VisualElement m_hitWrap;
		private readonly Label m_hitLabel;
		private IVisualElementScheduledItem m_lowFuelFlash;
		private IVisualElementScheduledItem m_damageHide;
		private bool m_lowFuel;
		private bool m_lowFuelInitialized;
		private bool m_lowFuelFlashHigh;
		private bool m_damageRequested;
		private bool m_runEnded;
		private IVisualElementScheduledItem m_toastHide;
		private readonly HudImpactPopups m_popups;
		private readonly HudPausePopup m_pausePopup;
		private readonly LipButton m_pauseButton;

		private float m_lastFuelFraction = -1f;
		private float m_lastHealthFraction = -1f;

		public VisualElement Root { get; }

		/// <summary>Pause popup + resume countdown (S04). Visuals only until the "Pause, settings and audio" milestone.</summary>
		public HudPausePopup PausePopup => m_pausePopup;

		/// <summary>
		/// When true the pause button opens the pause popup instead of showing the "arrives later" toast. Only the HudGallery
		/// sets it: in play the button stays inert.
		/// </summary>
		public bool PauseOpensPopup { get; set; }

		/// <summary>Raised when the (inert) pause button is tapped.</summary>
		public event Action PauseClicked;

		/// <summary>Floating-joystick steer, -1..1 per axis, up positive. Zero when released.</summary>
		public event Action<Vector2> SteerChanged;

		/// <summary>Boost button held / released.</summary>
		public event Action<bool> BoostChanged;

		public HudView(VisualElement root)
		{
			Root = root;

			m_scoreValue = root.Q<Label>("score-value");
			m_healthFill = root.Q<VisualElement>("health-fill");
			m_practiceTag = root.Q<VisualElement>("practice-tag");
			m_comboSticker = root.Q<VisualElement>("combo-sticker");
			m_comboFace = root.Q<LipBox>("combo-sticker").Face;
			m_comboLabel = root.Q<Label>("combo-label");
			m_toast = root.Q<VisualElement>("hud-toast");
			m_topStack = root.Q<VisualElement>("top-stack");
			m_ceilingBanner = root.Q<VisualElement>("ceiling-banner");
			m_ceilingCount = root.Q<Label>("ceiling-count");
			m_lateralLeft = root.Q<VisualElement>("lateral-tape-left");
			m_lateralRight = root.Q<VisualElement>("lateral-tape-right");
			m_groundVignette = root.Q<VisualElement>("ground-vignette");
			m_groundText = root.Q<VisualElement>("ground-text");
			m_fuelCluster = root.Q<VisualElement>("fuel-cluster");
			m_fuelLabel = root.Q<Label>("fuel-label");
			m_healthWrap = root.Q<VisualElement>("health-wrap");
			m_damageOverlay = root.Q<VisualElement>("damage-overlay");
			m_hitWrap = root.Q<VisualElement>("hit-wrap");
			m_hitLabel = root.Q<Label>("hit-label");
			m_toastLabel = root.Q<Label>("hud-toast-label");

			// No base padding: positions in Hud.uss are measured from the screen edge like the mockup, the helper only adds device insets.
			SafeAreaHelper.Apply(root.Q<VisualElement>("hud-safe"), 0f);

			BuildFuelPips(root.Q<VisualElement>("fuel-pips"));
			m_popups = new HudImpactPopups(root.Q<VisualElement>("hud-popups"));
			m_pausePopup = new HudPausePopup(root);

			m_touchControls = new HudTouchControls(root);
			m_touchControls.SteerChanged += steer => SteerChanged?.Invoke(steer);
			m_touchControls.BoostChanged += boost => BoostChanged?.Invoke(boost);
			m_pauseButton = root.Q<LipButton>("pause-button");
			m_pauseButton.clicked += OnPauseClicked;

			SetFuel(1f);
			SetHealth(1f);
			SetLowFuel(false);
			SetLowHealth(false);
			SetScore(0);
			SetPractice(false);
			SetCombo(1, 0f);
		}

		public void SetScore(int score)
		{
			m_scoreValue.text = score.ToString("N0");
		}

		/// <param name="fraction">0-1 of the full tank.</param>
		public void SetFuel(float fraction)
		{
			fraction = Mathf.Clamp01(fraction);
			if (Mathf.Approximately(fraction, m_lastFuelFraction)) return;
			m_lastFuelFraction = fraction;

			for (var i = 0; i < FuelPipCount; i++)
			{
				m_pipFills[i].style.width = new StyleLength(Length.Percent(PipFill(fraction, i) * 100f));
			}
		}

		/// <param name="fraction">0-1 of full health.</param>
		public void SetHealth(float fraction)
		{
			fraction = Mathf.Clamp01(fraction);
			if (Mathf.Approximately(fraction, m_lastHealthFraction)) return;
			m_lastHealthFraction = fraction;

			m_healthFill.style.width = new StyleLength(Length.Percent(fraction * 100f));

			var green = Mathf.InverseLerp(k_HealthRedAt, k_HealthGreenAt, fraction);
			m_healthFill.style.backgroundColor = Color.Lerp(m_S_HealthRed, m_S_HealthGreen, green);
		}

		/// <summary>Low-fuel state (S03 d): glow ring around the pips, tinted tray, flashing LOW FUEL in place of FUEL.</summary>
		public void SetLowFuel(bool low)
		{
			if (low == m_lowFuel && m_lowFuelInitialized) return;
			m_lowFuelInitialized = true;
			m_lowFuel = low;

			m_fuelCluster.EnableInClassList("hud-fuel-cluster--low", low);
			m_fuelLabel.text = low ? "LOW FUEL" : "FUEL";

			if (!low)
			{
				m_lowFuelFlash?.Pause();
				m_fuelLabel.style.opacity = 1f;
				return;
			}

			m_lowFuelFlashHigh = true;
			m_fuelLabel.style.opacity = k_GroundFlashHigh;
			m_lowFuelFlash ??= Root.schedule.Execute(ToggleLowFuelFlash).Every(k_LowFuelFlashPeriodMs);
			m_lowFuelFlash.Resume();
		}

		/// <summary>Low-health state (S03 d): red glow ring around the health bar.</summary>
		public void SetLowHealth(bool low)
		{
			m_healthWrap.EnableInClassList("hud-health-wrap--low", low);
		}

		/// <summary>
		/// Damage taken (S03 h): cracks + dark vignette over the frame and a health-hit sticker with the HP lost. Restarts the
		/// timer on every hit. The cracks defer to the ground warning (one full-frame effect at a time).
		/// </summary>
		public void ShowDamage(float hpLost)
		{
			m_hitLabel.text = $"-{Mathf.Max(1, Mathf.RoundToInt(hpLost))} HEALTH";
			m_hitWrap.style.display = DisplayStyle.Flex;
			m_damageRequested = true;
			RefreshFrameEffects();

			m_damageHide?.Pause();
			m_damageHide = m_hitWrap.schedule.Execute(HideDamage).StartingIn(k_DamageDurationMs);
		}

		/// <summary>Clears the damage state immediately (gallery reset).</summary>
		public void HideDamage()
		{
			m_damageRequested = false;
			m_hitWrap.style.display = DisplayStyle.None;
			m_damageHide?.Pause();
			RefreshFrameEffects();
		}

		public void SetPractice(bool isPractice)
		{
			m_practiceTag.style.display = isPractice ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// Shows the combo sticker while the chain is live (multiplier above x1 and time left). It shrinks and
		/// desaturates as <paramref name="timerFraction"/> drains and disappears when the chain expires.
		/// </summary>
		public void SetCombo(int multiplier, float timerFraction)
		{
			var active = multiplier > 1 && timerFraction > 0f;
			m_comboSticker.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
			if (!active) return;

			var fraction = Mathf.Clamp01(timerFraction);
			var scale = Mathf.Lerp(k_ComboMinScale, 1f, fraction);

			m_comboLabel.text = $"×{multiplier} COMBO!";
			m_comboSticker.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
			m_comboSticker.style.opacity = Mathf.Lerp(k_ComboMinOpacity, 1f, fraction);
			m_comboFace.style.backgroundColor = Color.Lerp(m_S_ComboFadedColor, m_S_ComboActiveColor, fraction);
		}

		/// <summary>
		/// Ceiling warning (S03 e): the taped banner slides down with the countdown number and the fuel / health stack
		/// moves out of its way. <paramref name="secondsLeft"/> is only read while <paramref name="active"/>.
		/// </summary>
		public void SetCeilingWarning(bool active, int secondsLeft)
		{
			active &= !m_runEnded;
			m_ceilingBanner.EnableInClassList("is-visible", active);
			m_topStack.EnableInClassList("hud-top-stack--shifted", active);
			if (active) m_ceilingCount.text = Mathf.Max(0, secondsLeft).ToString();
		}

		/// <summary>Lateral soft limit (S03 f): slides the tape in on the offending edge. -1 left, 1 right, 0 none.</summary>
		public void SetLateralWarning(int side)
		{
			if (m_runEnded) side = 0;
			m_lateralLeft.EnableInClassList("is-visible", side < 0);
			m_lateralRight.EnableInClassList("is-visible", side > 0);
		}

		/// <summary>Ground proximity (S03 g): flashing red vignette and PULL UP!.</summary>
		public void SetGroundWarning(bool active)
		{
			active &= !m_runEnded;
			if (active == m_groundActive) return;
			m_groundActive = active;

			var display = active ? DisplayStyle.Flex : DisplayStyle.None;
			m_groundVignette.style.display = display;
			m_groundText.style.display = display;
			RefreshFrameEffects();

			if (!active)
			{
				m_groundFlash?.Pause();
				return;
			}

			m_groundFlashHigh = true;
			ApplyGroundFlash();
			m_groundFlash ??= Root.schedule.Execute(ToggleGroundFlash).Every(k_GroundFlashPeriodMs);
			m_groundFlash.Resume();
		}

		/// <summary>
		/// Floating "+points" popup (S03 c) at a panel-space position, with a PERFECT CRASH tag and an optional coin line.
		/// The position is kept inside the frame so a crash at the screen edge still shows a whole popup.
		/// </summary>
		/// <param name="coins">Coin line amount; 0 hides it (no currency yet, see <see cref="HudPlaceholderData"/>).</param>
		public void ShowImpactPopup(Vector2 panelPosition, int points, bool perfect, int coins = 0)
		{
			var size = Root.layout.size;
			if (size.x > 0f && size.y > 0f)
			{
				panelPosition.x = Mathf.Clamp(panelPosition.x, k_PopupMarginX, size.x - k_PopupMarginX);
				panelPosition.y = Mathf.Clamp(panelPosition.y, k_PopupMarginTop, size.y - k_PopupMarginBottom);
			}

			m_popups.Show(panelPosition, points, perfect, coins);
		}

		/// <summary>Removes any popups still on screen (gallery reset).</summary>
		public void ClearPopups() => m_popups.Clear();

		/// <summary>
		/// The run ended: the HUD stays on screen (dimmed by the Game Over backdrop) but is frozen. Live warnings are cleared
		/// and stay off, touch input is released and ignored, and the pause button stops responding. Passing false (gallery
		/// reset) undoes it; a real run never un-ends, the scene reloads.
		/// </summary>
		public void SetRunEnded(bool ended)
		{
			if (m_runEnded == ended) return;
			m_runEnded = ended;

			m_touchControls.SetEnabled(!ended);
			m_pauseButton.SetEnabled(!ended);
			if (!ended) return;

			SetCeilingWarning(false, 0);
			SetLateralWarning(0);
			SetGroundWarning(false);
		}

		public void ShowToast(string message)
		{
			m_toastLabel.text = message;
			m_toast.style.display = DisplayStyle.Flex;

			m_toastHide?.Pause();
			m_toastHide = m_toast.schedule.Execute(() => m_toast.style.display = DisplayStyle.None).StartingIn(k_ToastDurationMs);
		}

		/// <summary>Shows the joystick base at <paramref name="origin"/> (panel px) with the handle offset, without input. Gallery only.</summary>
		public void PreviewJoystick(Vector2 origin, Vector2 handleOffset)
		{
			m_touchControls.ShowPreview(origin, handleOffset);
		}

		public void HideJoystickPreview()
		{
			m_touchControls.HidePreview();
		}

		/// <summary>
		/// How full pip <paramref name="index"/> (0 = leftmost) is for a tank at <paramref name="fraction"/>: whole pips
		/// light up in order and the boundary pip is partially filled, so the continuous fuel value never snaps.
		/// </summary>
		public static float PipFill(float fraction, int index)
		{
			return Mathf.Clamp01(fraction * FuelPipCount - index);
		}

		private void BuildFuelPips(VisualElement container)
		{
			container.Clear();
			for (var i = 0; i < FuelPipCount; i++)
			{
				var pip = new VisualElement { pickingMode = PickingMode.Ignore };
				pip.AddToClassList("hud-pip");
				if (i == FuelPipCount - 1) pip.AddToClassList("hud-pip--last");

				var fill = new VisualElement { pickingMode = PickingMode.Ignore };
				fill.AddToClassList("hud-pip__fill");
				pip.Add(fill);

				container.Add(pip);
				m_pipFills[i] = fill;
			}
		}

		// Full-frame effects: the ground vignette owns the frame, so the damage cracks wait until it clears.
		private void RefreshFrameEffects()
		{
			m_damageOverlay.EnableInClassList("is-visible", m_damageRequested && !m_groundActive);
		}

		private void ToggleLowFuelFlash()
		{
			m_lowFuelFlashHigh = !m_lowFuelFlashHigh;
			m_fuelLabel.style.opacity = m_lowFuelFlashHigh ? k_GroundFlashHigh : k_GroundFlashLow;
		}

		private void ToggleGroundFlash()
		{
			m_groundFlashHigh = !m_groundFlashHigh;
			ApplyGroundFlash();
		}

		private void ApplyGroundFlash()
		{
			var opacity = m_groundFlashHigh ? k_GroundFlashHigh : k_GroundFlashLow;
			m_groundVignette.style.opacity = opacity;
			m_groundText.style.opacity = opacity;
		}

		private void OnPauseClicked()
		{
			if (PauseOpensPopup) m_pausePopup.Show();
			else ShowToast(HudPlaceholderData.PauseToastMessage);
			PauseClicked?.Invoke();
		}
	}
}
