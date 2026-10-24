using Opoint8182.Common;
using Opoint8182.Game;
using Opoint8182.Pickup;
using Opoint8182.Score;
using UnityEngine;
using Bldng = Opoint8182.Building.Building;

namespace Opoint8182
{
	/// <summary>
	/// Phone haptics for the run (Pause, settings and audio, Pass 5): the same event-listening shape as
	/// <see cref="Audio.GameplaySfx"/> (no gameplay script knows about haptics), on the same GameManager object in
	/// <c>Prototype</c>. Acts only while the run is live and not paused. Perfect crash is Heavy, plain crashes and
	/// hits Medium, heals, pickups and a combo step up Light, and the run ending plays the crash rhythm.
	/// </summary>
	public class GameplayHaptics : MonoBehaviour
	{
		private GameManager m_gameManager;
		private ScoreSystem m_score;
		private int m_lastMultiplier = 1;

		private bool Live => m_gameManager != null && m_gameManager.IsPlaying && m_gameManager.IsRunActive && !m_gameManager.IsPaused;

		private void OnEnable()
		{
			CombatEvents.Crashed += HandleCrashed;
			CombatEvents.DamageDealt += HandleDamageDealt;
			CombatEvents.Restored += HandleRestored;
		}

		// GameManager is a GenericSingleton set up in Awake; Start is the first point where it is guaranteed to exist.
		private void Start()
		{
			m_gameManager = GameManager.TryGetInstance();
			m_score = FindAnyObjectByType<ScoreSystem>();

			if (m_gameManager != null) m_gameManager.RunEnded += HandleRunEnded;

			if (m_score != null)
			{
				m_lastMultiplier = m_score.CurrentMultiplier;
				m_score.MultiplierValue.AddListener(HandleMultiplierChanged);
			}
		}

		private void OnDisable()
		{
			CombatEvents.Crashed -= HandleCrashed;
			CombatEvents.DamageDealt -= HandleDamageDealt;
			CombatEvents.Restored -= HandleRestored;
		}

		private void OnDestroy()
		{
			if (m_gameManager != null) m_gameManager.RunEnded -= HandleRunEnded;
			if (m_score != null) m_score.MultiplierValue.RemoveListener(HandleMultiplierChanged);
		}

		private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo)
		{
			if (Live) Haptics.Play(quality >= 1f ? HapticTier.Heavy : HapticTier.Medium);
		}

		// A mistimed Tough building raises DamageDealt and Crashed(0) from the same hit; the crash already buzzes.
		private void HandleDamageDealt(IDamageDealer source, float damage)
		{
			if (Live && source is not Bldng) Haptics.Play(HapticTier.Medium);
		}

		private void HandleRestored(IRestorer source, float amount)
		{
			if (Live && (source is HealthPickup || amount > 0f)) Haptics.Play(HapticTier.Light);
		}

		private void HandleMultiplierChanged(int multiplier)
		{
			var rose = multiplier > m_lastMultiplier;
			m_lastMultiplier = multiplier;

			if (rose && Live) Haptics.Play(HapticTier.Light);
		}

		private void HandleRunEnded(RunEndCause cause) => Haptics.Play(HapticTier.CrashRhythm);
	}
}
