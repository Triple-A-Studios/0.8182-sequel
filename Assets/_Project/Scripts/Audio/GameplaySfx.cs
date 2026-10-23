using Opoint8182.Altitude;
using Opoint8182.Common;
using Opoint8182.Fuel;
using Opoint8182.Game;
using Opoint8182.Health;
using Opoint8182.Lateral;
using Opoint8182.Pickup;
using Opoint8182.Player;
using Opoint8182.Score;
using Opoint8182.UI;
using UnityEngine;
using Bldng = Opoint8182.Building.Building;

namespace Opoint8182.Audio
{
	/// <summary>
	/// Plays the in-run sound effects (Pause, settings and audio, Pass 4b): it only listens to events the gameplay systems
	/// already raise and calls <see cref="Sfx"/>, so no gameplay script knows about audio. Lives on the GameManager object in
	/// <c>Prototype</c>, finds the systems itself, and acts only while the run is live and not paused (a paused game is
	/// silent through <c>AudioListener.pause</c>, and a held boost or warning resumes with it).
	/// </summary>
	public class GameplaySfx : MonoBehaviour
	{
		[Header("Crashes and combo")]
		[Tooltip("Pitch added per combo multiplier step above x1 (x2 plays at 1 + step).")]
		[SerializeField] private float m_comboPitchStep = 0.12f;

		[Header("Warning beeps")]
		[Tooltip("Ceiling beep interval at a full grace period (x) and about to fly off (y); it speeds up as time runs out.")]
		[SerializeField] private Vector2 m_ceilingBeepInterval = new(0.9f, 0.25f);
		[SerializeField] private float m_groundBeepInterval = 0.45f;
		[SerializeField] private float m_lateralBeepInterval = 0.6f;
		[SerializeField] private float m_lowFuelBeepInterval = 1.2f;
		[SerializeField] private float m_lowHealthBeepInterval = 1f;
		[Tooltip("Same defaults as the HUD's low-fuel / low-health state; change both together.")]
		[SerializeField] private float m_lowFuelThreshold = HudView.DefaultLowFuelThreshold;
		[SerializeField] private float m_lowHealthThreshold = HudView.DefaultLowHealthThreshold;

		[Header("Run end")]
		[Tooltip("Seconds after the run-end sound before the Game Over sting.")]
		[SerializeField] private float m_gameOverDelay = 0.9f;

		private GameManager m_gameManager;
		private PlaneController m_plane;
		private FuelSystem m_fuel;
		private HealthSystem m_health;
		private ScoreSystem m_score;
		private AltitudeSystem m_altitude;
		private LateralSystem m_lateral;

		private int m_lastMultiplier = 1;
		private bool m_wasBoosting;
		private float m_nextCeilingBeep;
		private float m_nextGroundBeep;
		private float m_nextLateralBeep;
		private float m_nextLowFuelBeep;
		private float m_nextLowHealthBeep;

		private bool Live => m_gameManager != null && m_gameManager.IsPlaying && m_gameManager.IsRunActive;

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
			m_plane = FindAnyObjectByType<PlaneController>();
			m_fuel = FindAnyObjectByType<FuelSystem>();
			m_health = FindAnyObjectByType<HealthSystem>();
			m_score = FindAnyObjectByType<ScoreSystem>();
			m_altitude = FindAnyObjectByType<AltitudeSystem>();
			m_lateral = FindAnyObjectByType<LateralSystem>();

			if (m_gameManager != null)
			{
				m_gameManager.RunEnded += HandleRunEnded;
				m_gameManager.ReturnedToMenu += HandleReturnedToMenu;
			}

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
			if (m_gameManager != null)
			{
				m_gameManager.RunEnded -= HandleRunEnded;
				m_gameManager.ReturnedToMenu -= HandleReturnedToMenu;
			}

			if (m_score != null) m_score.MultiplierValue.RemoveListener(HandleMultiplierChanged);
			Sfx.StopLoop(SfxId.BoostLoop);
		}

		private void Update()
		{
			if (m_gameManager == null || m_gameManager.IsPaused) return;

			var live = Live;
			UpdateBoost(live);
			UpdateWarnings(live);
		}

		// Perfect and mistimed are the two ends of crash quality; everything between is a plain crash, softer when weaker.
		// A heal building raises Restored instead of Crashed, so it never lands here.
		private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo)
		{
			if (!Live) return;

			if (quality >= 1f) Sfx.Play(SfxId.CrashPerfect);
			else if (quality <= 0f) Sfx.Play(SfxId.CrashMistimed);
			else Sfx.Play(SfxId.CrashPlain, 0.6f + 0.4f * quality);
		}

		// A mistimed Tough building raises DamageDealt and Crashed(0) from the same hit and already plays CrashMistimed.
		private void HandleDamageDealt(IDamageDealer source, float damage)
		{
			if (!Live || source is Bldng) return;

			Sfx.Play(SfxId.Hit);
		}

		private void HandleRestored(IRestorer source, float amount)
		{
			if (!Live) return;

			if (source is HealthPickup) Sfx.Play(SfxId.Pickup);
			else if (amount > 0f) Sfx.Play(SfxId.Heal);
			else Sfx.Play(SfxId.CrashMistimed);
		}

		// Up a tier plays a tick that rises with the multiplier; decay is silent.
		private void HandleMultiplierChanged(int multiplier)
		{
			var rose = multiplier > m_lastMultiplier;
			m_lastMultiplier = multiplier;

			if (rose && Live) Sfx.Play(SfxId.ComboTick, 1f, 1f + m_comboPitchStep * (multiplier - 1));
		}

		private void UpdateBoost(bool live)
		{
			var boosting = live && m_plane != null && m_plane.enabled && m_plane.IsBoosting;
			if (boosting == m_wasBoosting) return;

			m_wasBoosting = boosting;
			if (boosting)
			{
				Sfx.Play(SfxId.BoostStart);
				Sfx.StartLoop(SfxId.BoostLoop);
			}
			else
			{
				Sfx.StopLoop(SfxId.BoostLoop);
			}
		}

		private void UpdateWarnings(bool live)
		{
			var ceiling = live && m_altitude != null && m_altitude.IsWarningValue.Value;
			var ceilingInterval = m_altitude == null ? 0f : Mathf.Lerp(m_ceilingBeepInterval.y, m_ceilingBeepInterval.x, m_altitude.WarningFractionValue.Value);

			Beep(ref m_nextCeilingBeep, ceiling, SfxId.WarnCeiling, ceilingInterval);
			Beep(ref m_nextGroundBeep, live && m_altitude != null && m_altitude.IsGroundWarningValue.Value, SfxId.WarnGround, m_groundBeepInterval);
			Beep(ref m_nextLateralBeep, live && m_lateral != null && m_lateral.IsWarningValue.Value, SfxId.WarnLateral, m_lateralBeepInterval);
			Beep(ref m_nextLowFuelBeep, live && m_fuel != null && m_fuel.FuelFraction <= m_lowFuelThreshold, SfxId.WarnLowFuel, m_lowFuelBeepInterval);
			Beep(ref m_nextLowHealthBeep, live && m_health != null && m_health.HealthFraction <= m_lowHealthThreshold, SfxId.WarnLowHealth, m_lowHealthBeepInterval);
		}

		// Beeps right away when a warning starts, then every interval while it holds; clearing it resets the timer.
		private static void Beep(ref float nextTime, bool active, SfxId id, float interval)
		{
			if (!active)
			{
				nextTime = 0f;
				return;
			}

			if (Time.time < nextTime) return;

			Sfx.Play(id);
			nextTime = Time.time + interval;
		}

		private void HandleRunEnded(RunEndCause cause)
		{
			StopBoost();

			switch (cause)
			{
				case RunEndCause.OutOfFuel: Sfx.Play(SfxId.RunEndFuel); break;
				case RunEndCause.Wrecked: Sfx.Play(SfxId.RunEndWreck); break;
				case RunEndCause.HitTheDeck: Sfx.Play(SfxId.RunEndGround); break;
				case RunEndCause.FlewTooHigh:
				case RunEndCause.OutOfBounds: Sfx.Play(SfxId.RunEndFlyOff); break;
			}

			Invoke(nameof(PlayGameOver), m_gameOverDelay);
		}

		private void HandleReturnedToMenu()
		{
			CancelInvoke(nameof(PlayGameOver));
			StopBoost();
		}

		private void PlayGameOver() => Sfx.Play(SfxId.GameOver);

		private void StopBoost()
		{
			m_wasBoosting = false;
			Sfx.StopLoop(SfxId.BoostLoop);
		}
	}
}
