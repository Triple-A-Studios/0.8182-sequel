using Alchemy.Inspector;
using Opoint8182.Fuel;
using Opoint8182.Health;
using Opoint8182.Player;
using Opoint8182.Score;
using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>
	/// Binds the gameplay systems (fuel, health, score/combo) to the merged HUD view. Replaces the per-element
	/// FuelGaugeUI, HealthGaugeUI and ScoreUI scripts.
	/// </summary>
	public class HudPresenter : HudHost
	{
		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private FuelSystem m_fuelSystem;
		[FoldoutGroup("References")] [SerializeField] private HealthSystem m_healthSystem;
		[FoldoutGroup("References")] [SerializeField] private ScoreSystem m_scoreSystem;
		[FoldoutGroup("References")] [SerializeField] private PlaneController m_planeController;

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
			}
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
			}

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
			view.SetPractice(HudPlaceholderData.IsPractice);
			view.SteerChanged += OnSteerChanged;
			view.BoostChanged += OnBoostChanged;

			if (m_fuelSystem != null) view.SetFuel(m_fuelSystem.FuelFraction);
			if (m_healthSystem != null) view.SetHealth(m_healthSystem.HealthFraction);
			if (m_scoreSystem != null)
			{
				view.SetScore(m_scoreSystem.CurrentScore);
				view.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);
			}
		}

		private void OnSteerChanged(Vector2 steer)
		{
			if (m_planeController != null) m_planeController.TouchSteer = steer;
		}

		private void OnBoostChanged(bool isBoosting)
		{
			if (m_planeController != null) m_planeController.TouchBoost = isBoosting;
		}

		private void OnFuelChanged(float _) => View?.SetFuel(m_fuelSystem.FuelFraction);

		private void OnHealthChanged(float _) => View?.SetHealth(m_healthSystem.HealthFraction);

		private void OnScoreChanged(int score) => View?.SetScore(score);

		private void OnMultiplierChanged(int _) => View?.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);

		private void OnComboTimerChanged(float _) => View?.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);
	}
}
