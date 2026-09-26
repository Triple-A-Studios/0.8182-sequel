using System.Collections;
using Alchemy.Inspector;
using Opoint8182.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Opoint8182.CameraFeel
{
	public class CameraJuice : MonoBehaviour
	{
		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private PlaneController m_planeController;

		[Title("Boost Follow-Tighten")]
		[FoldoutGroup("Boost Follow-Tighten")] [SerializeField] private Vector3 m_boostedPositionDamping = new(0.2f, 0.2f, 0.2f);
		[FoldoutGroup("Boost Follow-Tighten")] [SerializeField] private float m_dampingLerpSpeed = 4f;

		[Title("Boost FOV")]
		// Sustained kick while boosting, plus a one-shot punch on the boost engage edge that decays
		// back to the sustained level - two separate knobs so "boost feels stronger" and "boost just
		// kicked in" can be tuned independently.
		[FoldoutGroup("Boost FOV")] [SerializeField] private float m_boostFovKick = 4f;
		[FoldoutGroup("Boost FOV")] [SerializeField] private float m_boostEngageFovPunch = 6f;
		[FoldoutGroup("Boost FOV")] [SerializeField] private float m_fovPunchDecayPerSec = 20f;
		[FoldoutGroup("Boost FOV")] [SerializeField] private float m_fovLerpSpeed = 8f;

		[Title("Steering Roll")]
		[FoldoutGroup("Steering Roll")] [SerializeField] private float m_maxRollAngle = 6f;
		[FoldoutGroup("Steering Roll")] [SerializeField] private float m_rollSpeedDegPerSec = 40f;

		[Title("Shake")]
		[FoldoutGroup("Shake")] [SerializeField] private float m_crashShakeMagnitude = 0.4f;
		[FoldoutGroup("Shake")] [SerializeField] private float m_crashShakeDuration = 0.25f;
		[FoldoutGroup("Shake")] [SerializeField] private float m_hitShakeMagnitude = 0.2f;
		[FoldoutGroup("Shake")] [SerializeField] private float m_hitShakeDuration = 0.15f;
		[FoldoutGroup("Shake")] [SerializeField] private float m_depletedShakeMagnitude = 0.6f;
		[FoldoutGroup("Shake")] [SerializeField] private float m_depletedShakeDuration = 0.35f;

		[Title("Hit Stop")]
		[FoldoutGroup("Hit Stop")] [SerializeField] private float m_hitStopTimeScale = 0.05f;
		[FoldoutGroup("Hit Stop")] [SerializeField] private float m_hitStopDuration = 0.08f;

		private CinemachineCamera m_vcam;
		private CinemachineFollow m_follow;

		private Vector3 m_baseFollowOffset;
		private Vector3 m_baseDamping;
		private float m_baseFov;

		private bool m_wasBoosting;
		private float m_fovPunch;
		private float m_currentRoll;

		private float m_shakeMagnitude;
		private float m_shakeDurationTotal;
		private float m_shakeTimeRemaining;

		private void Awake()
		{
			m_vcam = GetComponent<CinemachineCamera>();
			m_follow = GetComponent<CinemachineFollow>();

			m_baseFollowOffset = m_follow.FollowOffset;
			m_baseDamping = m_follow.TrackerSettings.PositionDamping;
			m_baseFov = m_vcam.Lens.FieldOfView;
		}

		private void OnDisable()
		{
			// Cheap insurance against a hit-stop coroutine getting cut off mid-freeze (e.g. a scene
			// reload) - astronomically unlikely given the ~0.08s duration, but free to guard.
			Time.timeScale = 1f;
		}

		private void Update()
		{
			var boosting = m_planeController != null && m_planeController.IsBoosting;

			UpdateFollowTighten(boosting);
			UpdateFov(boosting);
			UpdateRoll();
			UpdateShake();

			m_wasBoosting = boosting;
		}

		private void UpdateFollowTighten(bool boosting)
		{
			var target = boosting ? m_boostedPositionDamping : m_baseDamping;
			var tracker = m_follow.TrackerSettings;
			tracker.PositionDamping = Vector3.Lerp(tracker.PositionDamping, target, Time.deltaTime * m_dampingLerpSpeed);
			m_follow.TrackerSettings = tracker;
		}

		private void UpdateFov(bool boosting)
		{
			if (boosting && !m_wasBoosting) m_fovPunch = m_boostEngageFovPunch;
			m_fovPunch = Mathf.MoveTowards(m_fovPunch, 0f, m_fovPunchDecayPerSec * Time.deltaTime);

			var sustained = boosting ? m_baseFov + m_boostFovKick : m_baseFov;
			var target = sustained + m_fovPunch;

			var lens = m_vcam.Lens;
			lens.FieldOfView = Mathf.MoveTowards(lens.FieldOfView, target, m_fovLerpSpeed * Time.deltaTime);
			m_vcam.Lens = lens;
		}

		private void UpdateRoll()
		{
			var steerX = m_planeController != null ? m_planeController.SteerInput.x : 0f;
			var targetRoll = -steerX * m_maxRollAngle;
			m_currentRoll = Mathf.MoveTowards(m_currentRoll, targetRoll, m_rollSpeedDegPerSec * Time.deltaTime);

			var lens = m_vcam.Lens;
			lens.Dutch = m_currentRoll;
			m_vcam.Lens = lens;
		}

		private void UpdateShake()
		{
			var offset = m_baseFollowOffset;

			if (m_shakeTimeRemaining > 0f)
			{
				m_shakeTimeRemaining -= Time.deltaTime;
				var falloff = Mathf.Clamp01(m_shakeTimeRemaining / m_shakeDurationTotal);
				var jitter = Random.insideUnitSphere * (m_shakeMagnitude * falloff);
				jitter.z = 0f; // keep shake off the forward axis - don't push the camera through/away from the plane
				offset += jitter;
			}

			m_follow.FollowOffset = offset;
		}

		public void Shake(float magnitude, float duration)
		{
			m_shakeMagnitude = magnitude;
			m_shakeDurationTotal = duration;
			m_shakeTimeRemaining = duration;
		}

		// Floored at 0.3f so even a mistimed/quality-0 crash still reads as an impact, not silence.
		public void ShakeForCrash(float quality) => Shake(m_crashShakeMagnitude * Mathf.Max(quality, 0.3f), m_crashShakeDuration);

		public void ShakeForHit() => Shake(m_hitShakeMagnitude, m_hitShakeDuration);

		public void ShakeForDepleted() => Shake(m_depletedShakeMagnitude, m_depletedShakeDuration);

		public void HitStop() => StartCoroutine(HitStopRoutine());

		private IEnumerator HitStopRoutine()
		{
			Time.timeScale = m_hitStopTimeScale;
			yield return new WaitForSecondsRealtime(m_hitStopDuration);
			Time.timeScale = 1f;
		}
	}
}
