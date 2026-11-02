using Bldng = Opoint8182.Building.Building;
using Opoint8182.Building;
using Opoint8182.Common;
using Opoint8182.Game;
using Opoint8182.Score;
using Opoint8182.Spawning;
using UnityEngine;

namespace Opoint8182.Stats
{
	/// <summary>
	/// Collects the stats of the run in progress: buildings brought down (every crash), the longest combo streak (buildings
	/// crashed in a row without the combo being disrupted, see <c>ScoreSystem.ComboStreak</c>), perfect
	/// crashes and, at the end, the distance flown (the spawner's own distance, in world units = meters) and the score.
	/// Added to the <c>GameManager</c> object by <c>GameManager.Awake</c>, so it needs no scene edit; the game scene reloads for
	/// every run, so a tracker always starts from zero. <see cref="Snapshot"/> freezes it and hands the result to the
	/// <c>GameManager</c>, which records it in <see cref="PlayerStats"/> and keeps it for Game Over.
	/// </summary>
	public class RunStatsTracker : MonoBehaviour
	{
		private ScoreSystem m_scoreSystem;
		private SpawnManager m_spawnManager;

		private int m_buildingsDestroyed;
		private int m_bestCombo;
		private int m_perfectCrashes;
		private int m_toughBroken;
		private int m_healsUsed;
		private int m_perfectStreak;
		private int m_bestPerfectStreak;
		private float m_seconds;
		private bool m_frozen;

		// A frame counts at most this long, so a stall (a background hitch, a debugger) cannot inflate the run time.
		private const float k_MaxFrameSeconds = 0.5f;
		private GameManager m_gameManager;

		private void OnEnable()
		{
			if (m_scoreSystem == null) m_scoreSystem = FindAnyObjectByType<ScoreSystem>();
			if (m_spawnManager == null) m_spawnManager = FindAnyObjectByType<SpawnManager>();

			if (m_gameManager == null) m_gameManager = GetComponent<GameManager>();

			CombatEvents.Crashed += HandleCrashed;
			CombatEvents.Restored += HandleRestored;
			if (m_scoreSystem == null) return;

			m_scoreSystem.PointsAwarded += HandlePointsAwarded;
			m_scoreSystem.ComboStreakChanged += HandleComboStreakChanged;
			m_bestCombo = Mathf.Max(m_bestCombo, m_scoreSystem.ComboStreak);
		}

		private void OnDisable()
		{
			CombatEvents.Crashed -= HandleCrashed;
			CombatEvents.Restored -= HandleRestored;
			if (m_scoreSystem == null) return;

			m_scoreSystem.PointsAwarded -= HandlePointsAwarded;
			m_scoreSystem.ComboStreakChanged -= HandleComboStreakChanged;
		}

		// Time spent flying: GameManager.IsPlaying is false in the menu, while paused and through the resume countdown.
		private void Update()
		{
			if (m_frozen || m_gameManager == null || !m_gameManager.IsPlaying) return;

			m_seconds += Mathf.Min(Time.unscaledDeltaTime, k_MaxFrameSeconds);
		}

		/// <summary>Stops counting and returns the run's stats. Later crashes and combo changes (the plane flying off) no longer count.</summary>
		public RunStats Snapshot(bool isPractice)
		{
			m_frozen = true;

			var score = m_scoreSystem != null ? m_scoreSystem.CurrentScore : 0;
			var distance = m_spawnManager != null ? m_spawnManager.DistanceTraveled : 0f;
			return new RunStats(score, distance, m_buildingsDestroyed, m_bestCombo, m_perfectCrashes, isPractice,
				m_toughBroken, m_healsUsed, m_bestPerfectStreak, m_seconds);
		}

		// Only buildings raise Crashed (a mistimed tough hit included: it still brings the building down).
		private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo)
		{
			if (m_frozen) return;

			m_buildingsDestroyed++;

			// A tough building counts once it was really broken (a mistimed hit still flattens it but does not feed the combo).
			if (countsForCombo && source is Bldng building && building.BuildingType == BuildingType.Tough) m_toughBroken++;
		}

		private void HandleRestored(IRestorer source, float amount)
		{
			if (!m_frozen && amount > 0f && source is HealBuilding) m_healsUsed++;
		}

		private void HandlePointsAwarded(int points, Vector3 position, bool perfect)
		{
			if (m_frozen) return;

			// Every scored crash passes here, so a non-perfect one (a mistimed hit included) ends the run of perfect crashes.
			if (!perfect)
			{
				m_perfectStreak = 0;
				return;
			}

			m_perfectCrashes++;
			m_perfectStreak++;
			if (m_perfectStreak > m_bestPerfectStreak) m_bestPerfectStreak = m_perfectStreak;
		}

		private void HandleComboStreakChanged(int streak)
		{
			if (!m_frozen) m_bestCombo = Mathf.Max(m_bestCombo, streak);
		}
	}
}
