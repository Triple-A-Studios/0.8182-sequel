using UnityEngine;
using UnityEngine.InputSystem;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the merged HUD: jumps the view to each mockup state (S03 a-h) with hotkeys so states that are
	/// hard to reach in play can be checked. Lives in the HudGallery scene only. Each HUD sub-pass adds its states here.
	/// Keys: A clean early run, B practice mode, C combo, V drain the combo timer, F cycle fuel, H cycle health.
	/// </summary>
	public class HudStateDriver : HudHost
	{
		private static readonly float[] m_S_FuelLevels = { 1f, 0.76f, 0.5f, 0.25f, 0.1f, 0f };
		private static readonly float[] m_S_HealthLevels = { 1f, 0.76f, 0.5f, 0.22f, 0f };

		private const float k_ComboDrainSeconds = 3f;
		private const float k_ComboStaticFraction = 0.8f;

		private int m_fuelIndex = 1;
		private int m_healthIndex = 1;
		private int m_comboMultiplier = 1;
		private float m_comboTimer;

		protected override void OnViewReady(HudView view)
		{
			ApplyCleanRun();
		}

		private void Update()
		{
			if (View == null) return;

			var keyboard = Keyboard.current;
			if (keyboard != null)
			{
				if (keyboard.aKey.wasPressedThisFrame) ApplyCleanRun();
				if (keyboard.bKey.wasPressedThisFrame) ApplyPractice();
				if (keyboard.cKey.wasPressedThisFrame) ApplyCombo();
				if (keyboard.vKey.wasPressedThisFrame) StartComboDrain();
				if (keyboard.fKey.wasPressedThisFrame) CycleFuel();
				if (keyboard.hKey.wasPressedThisFrame) CycleHealth();
			}

			if (m_comboTimer > 0f)
			{
				m_comboTimer = Mathf.Max(0f, m_comboTimer - Time.unscaledDeltaTime);
				View.SetCombo(m_comboMultiplier, m_comboTimer / k_ComboDrainSeconds);
			}
		}

		private void ApplyCleanRun()
		{
			m_fuelIndex = 1;
			m_healthIndex = 1;
			m_comboMultiplier = 1;
			m_comboTimer = 0f;
			View.SetScore(184920);
			View.SetFuel(m_S_FuelLevels[m_fuelIndex]);
			View.SetHealth(m_S_HealthLevels[m_healthIndex]);
			View.SetPractice(false);
			View.SetCombo(1, 0f);
		}

		private void ApplyPractice()
		{
			ApplyCleanRun();
			View.SetPractice(true);
		}

		private void ApplyCombo()
		{
			ApplyCleanRun();
			m_comboMultiplier = 3;
			View.SetCombo(m_comboMultiplier, k_ComboStaticFraction);
		}

		private void StartComboDrain()
		{
			if (m_comboMultiplier < 2) m_comboMultiplier = 3;
			m_comboTimer = k_ComboDrainSeconds;
		}

		private void CycleFuel()
		{
			m_fuelIndex = (m_fuelIndex + 1) % m_S_FuelLevels.Length;
			View.SetFuel(m_S_FuelLevels[m_fuelIndex]);
		}

		private void CycleHealth()
		{
			m_healthIndex = (m_healthIndex + 1) % m_S_HealthLevels.Length;
			View.SetHealth(m_S_HealthLevels[m_healthIndex]);
		}
	}
}
