using Alchemy.Inspector;
using CartoonFX;
using Opoint8182.Building;
using Opoint8182.Common;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Opoint8182.Vfx
{
	/// <summary>
	/// The effects around the plane (3D art pass, Pass 6): a burst where a building is destroyed (bigger for a better hit),
	/// and, when the run ends with the plane stopped, a blast followed by a fire that stays on the plane. Every instance is
	/// created up front and reused, so nothing is instantiated during a run. The fire and the blast are cleared by the scene
	/// reload that starts the next run.
	/// </summary>
	public class PlaneVfx : MonoBehaviour
	{
		[Title("Prefabs")]
		[FoldoutGroup("Prefabs")] [SerializeField] private GameObject m_crashBurstPrefab;
		[FoldoutGroup("Prefabs")] [SerializeField] private GameObject m_runEndBlastPrefab;
		[FoldoutGroup("Prefabs")] [SerializeField] private GameObject m_wreckFirePrefab;

		[Title("Crash Burst")]
		// A burst lasts about 1.6 s and crashes come seconds apart, so a few instances are plenty; the oldest is reused.
		[FoldoutGroup("Crash Burst")] [SerializeField] [Min(1)] private int m_burstPoolSize = 4;
		[FoldoutGroup("Crash Burst")] [SerializeField] private float m_minBurstScale = 0.7f;
		[FoldoutGroup("Crash Burst")] [SerializeField] private float m_maxBurstScale = 1.2f;
		[FoldoutGroup("Crash Burst")] [SerializeField] private Vector3 m_burstOffset = new(0f, 0f, 1.5f);

		[Title("Run End")]
		[FoldoutGroup("Run End")] [SerializeField] private float m_blastScale = 2.5f;
		[FoldoutGroup("Run End")] [SerializeField] private Vector3 m_blastOffset = Vector3.zero;
		[FoldoutGroup("Run End")] [SerializeField] private float m_fireScale = 1f;
		[FoldoutGroup("Run End")] [SerializeField] private Vector3 m_fireOffset = Vector3.zero;

		private GameObject[] m_bursts;
		private int m_nextBurst;
		private GameObject m_blast;
		private GameObject m_fire;
		private GameObject m_poolRoot;

		private void Awake()
		{
			// Scenes load additively, so the active scene is not necessarily the plane's: keep the pool with the plane,
			// or it would outlive the scene reload.
			m_poolRoot = new GameObject("PlaneVfxPool");
			SceneManager.MoveGameObjectToScene(m_poolRoot, gameObject.scene);

			m_bursts = new GameObject[m_crashBurstPrefab != null ? m_burstPoolSize : 0];
			for (var i = 0; i < m_bursts.Length; i++) m_bursts[i] = CreateOneShot(m_crashBurstPrefab, m_poolRoot.transform);

			if (m_runEndBlastPrefab != null) m_blast = CreateOneShot(m_runEndBlastPrefab, m_poolRoot.transform);
			if (m_wreckFirePrefab != null) m_fire = Create(m_wreckFirePrefab, transform);
		}

		private void OnEnable()
		{
			CombatEvents.Crashed += HandleCrashed;
			CombatEvents.Restored += HandleRestored;
		}

		private void OnDisable()
		{
			CombatEvents.Crashed -= HandleCrashed;
			CombatEvents.Restored -= HandleRestored;
		}

		private void OnDestroy()
		{
			if (m_poolRoot != null) Destroy(m_poolRoot);
		}

		/// <summary>The plane is stopped for good: blast, and leave it burning.</summary>
		public void PlayRunEnd()
		{
			if (m_blast != null) Play(m_blast, transform.position + m_blastOffset, m_blastScale);

			if (m_fire == null) return;

			m_fire.transform.localPosition = m_fireOffset;
			m_fire.transform.localScale = Vector3.one * m_fireScale;
			m_fire.SetActive(true);
		}

		private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo) => PlayBurst(quality);

		// A Heal building is destroyed too, but reports a restore instead of a crash. A health pickup is not a building.
		private void HandleRestored(IRestorer source, float amount)
		{
			if (source is HealBuilding healBuilding) PlayBurst(healBuilding.Restore > 0f ? amount / healBuilding.Restore : 0f);
		}

		private void PlayBurst(float quality)
		{
			if (m_bursts.Length == 0) return;

			var burst = m_bursts[m_nextBurst];
			m_nextBurst = (m_nextBurst + 1) % m_bursts.Length;

			Play(burst, transform.position + m_burstOffset, Mathf.Lerp(m_minBurstScale, m_maxBurstScale, Mathf.Clamp01(quality)));
		}

		private static void Play(GameObject effect, Vector3 position, float scale)
		{
			// Off and on again restarts the particles (they play on awake) and the pack script's own timers.
			effect.SetActive(false);
			effect.transform.position = position;
			effect.transform.localScale = Vector3.one * scale;
			effect.SetActive(true);
		}

		private static GameObject CreateOneShot(GameObject prefab, Transform parent)
		{
			var instance = Create(prefab, parent);

			// The pack's default destroys a finished effect; these are reused.
			if (instance.TryGetComponent<CFXR_Effect>(out var effect)) effect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
			return instance;
		}

		private static GameObject Create(GameObject prefab, Transform parent)
		{
			// The variants were saved with a position from the scene they were made in; every use sets its own.
			var instance = Instantiate(prefab, parent);
			instance.SetActive(false);
			instance.transform.localPosition = Vector3.zero;

			foreach (var particleRenderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
			{
				particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}

			return instance;
		}
	}
}
