using Alchemy.Inspector;
using Opoint8182.Fuel;
using Opoint8182.Health;
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

			base.OnDisable();
		}

		protected override void OnViewReady(HudView view)
		{
			view.SetPractice(HudPlaceholderData.IsPractice);

			if (m_fuelSystem != null) view.SetFuel(m_fuelSystem.FuelFraction);
			if (m_healthSystem != null) view.SetHealth(m_healthSystem.HealthFraction);
			if (m_scoreSystem != null)
			{
				view.SetScore(m_scoreSystem.CurrentScore);
				view.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);
			}
		}

		private void OnFuelChanged(float _) => View?.SetFuel(m_fuelSystem.FuelFraction);

		private void OnHealthChanged(float _) => View?.SetHealth(m_healthSystem.HealthFraction);

		private void OnScoreChanged(int score) => View?.SetScore(score);

		private void OnMultiplierChanged(int _) => View?.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);

		private void OnComboTimerChanged(float _) => View?.SetCombo(m_scoreSystem.CurrentMultiplier, m_scoreSystem.ComboTimerFraction);
	}
}
