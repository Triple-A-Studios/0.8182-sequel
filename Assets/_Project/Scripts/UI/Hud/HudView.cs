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

		private const float k_ComboMinScale = 0.78f;
		private const float k_ComboMinOpacity = 0.7f;
		private const long k_ToastDurationMs = 2500;
		private static readonly Color m_S_ComboFadedColor = new(0.49f, 0.416f, 0.388f);
		private static readonly Color m_S_ComboActiveColor = new(0.91f, 0.376f, 0.235f);

		private readonly Label m_scoreValue;
		private readonly VisualElement[] m_pipFills = new VisualElement[FuelPipCount];
		private readonly VisualElement m_healthFill;
		private readonly VisualElement m_practiceTag;
		private readonly VisualElement m_comboSticker;
		private readonly Label m_comboLabel;
		private readonly VisualElement m_toast;
		private readonly Label m_toastLabel;
		private IVisualElementScheduledItem m_toastHide;

		private float m_lastFuelFraction = -1f;
		private float m_lastHealthFraction = -1f;

		public VisualElement Root { get; }

		/// <summary>Raised when the (inert) pause button is tapped.</summary>
		public event Action PauseClicked;

		public HudView(VisualElement root)
		{
			Root = root;

			m_scoreValue = root.Q<Label>("score-value");
			m_healthFill = root.Q<VisualElement>("health-fill");
			m_practiceTag = root.Q<VisualElement>("practice-tag");
			m_comboSticker = root.Q<VisualElement>("combo-sticker");
			m_comboLabel = root.Q<Label>("combo-label");
			m_toast = root.Q<VisualElement>("hud-toast");
			m_toastLabel = root.Q<Label>("hud-toast-label");

			// No base padding: positions in Hud.uss are measured from the screen edge like the mockup, the helper only adds device insets.
			SafeAreaHelper.Apply(root.Q<VisualElement>("hud-safe"), 0f);

			BuildFuelPips(root.Q<VisualElement>("fuel-pips"));
			root.Q<LipButton>("pause-button").clicked += OnPauseClicked;

			SetFuel(1f);
			SetHealth(1f);
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
			m_comboSticker.style.backgroundColor = Color.Lerp(m_S_ComboFadedColor, m_S_ComboActiveColor, fraction);
		}

		public void ShowToast(string message)
		{
			m_toastLabel.text = message;
			m_toast.style.display = DisplayStyle.Flex;

			m_toastHide?.Pause();
			m_toastHide = m_toast.schedule.Execute(() => m_toast.style.display = DisplayStyle.None).StartingIn(k_ToastDurationMs);
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

		private void OnPauseClicked()
		{
			ShowToast(HudPlaceholderData.PauseToastMessage);
			PauseClicked?.Invoke();
		}
	}
}
