using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using Opoint8182.Game;
using Opoint8182.Player;
using TripleA.Utils.Singletons;
using UnityEngine;

namespace Opoint8182.Spawning
{
	public class SpawnManager : GenericSingleton<SpawnManager>
	{
		[Serializable]
		private struct SpawnableEntry
		{
			[SerializeField] private GameObject m_prefab;
			[SerializeField] private SpawnKind m_kind;

			// Per-entry, not a manager-wide range: ground-pivot types (buildings, the large
			// obstacle) want a range pinned near 0 so their base-pivot sits on the ground:
			// a single shared range either floats them in midair or, at the setting that puts
			// them on the ground, buries small centered-pivot types (birds, the pickup) in it.
			[SerializeField] private float m_verticalMin;
			[SerializeField] private float m_verticalMax;

			public GameObject Prefab => m_prefab;
			public SpawnKind Kind => m_kind;
			public float VerticalMin => m_verticalMin;
			public float VerticalMax => m_verticalMax;
		}

		[Title("Plane Reference")]
		[FoldoutGroup("Plane Reference")] [SerializeField] private PlaneController m_planeController;

		[Title("Spawnables")]
		[FoldoutGroup("Spawnables")] [SerializeField] private SpawnableEntry[] m_spawnables;

		[Title("Spawn Pattern")]
		[FoldoutGroup("Spawn Pattern")] [SerializeField] private float m_spawnAheadDistance = 100f;
		[FoldoutGroup("Spawn Pattern")] [SerializeField] private float m_spawnInterval = 18f;
		[FoldoutGroup("Spawn Pattern")] [SerializeField] private float m_spawnIntervalJitter = 6f;
		[FoldoutGroup("Spawn Pattern")] [SerializeField] private float m_lateralRange = 10f;

		[Title("Despawn")]
		[FoldoutGroup("Despawn")] [SerializeField] private float m_despawnBehindDistance = 40f;

		private const int k_MaxSpawnsPerFrame = 8;

		private readonly List<SpawnedEntity> m_activeEntities = new();

		private GameManager m_gameManager;
		private float m_nextSpawnZ;
		private float m_startZ;
		private bool m_spawningActive = true;

		// Pass 2 hook: the difficulty ramp curve can read this instead of adding its own tracker.
		public float DistanceTraveled =>
			m_planeController != null ? m_planeController.transform.position.z - m_startZ : 0f;

		// Deliberately NOT overriding Awake() - GenericSingleton<T>.Awake() must run unmodified
		// to register the singleton instance (see GameManager, which follows the same rule and
		// does its own setup in OnEnable() instead).
		private void OnEnable()
		{
			if (m_planeController == null) m_planeController = FindAnyObjectByType<PlaneController>();
		}

		private void Start()
		{
			// Same Awake-before-Start-only ordering hazard GameOverUI documents: subscribe from
			// Start(), and use TryGetInstance() (not Instance) so a missing GameManager fails
			// quietly instead of GenericSingleton auto-instantiating a stand-in.
			m_gameManager = GameManager.TryGetInstance();
			if (m_gameManager != null) m_gameManager.RunEnded += HandleRunEnded;

			if (m_planeController == null)
			{
				Debug.LogError("[SpawnManager] No PlaneController found in scene - spawner disabled.");
				m_spawningActive = false;
				return;
			}

			m_startZ = m_planeController.transform.position.z;
			m_nextSpawnZ = m_startZ + m_spawnAheadDistance;

			if (m_gameManager != null && !m_gameManager.IsRunActive) m_spawningActive = false;
		}

		private void OnDisable()
		{
			if (m_gameManager != null) m_gameManager.RunEnded -= HandleRunEnded;
		}

		private void Update()
		{
			if (!m_spawningActive || m_planeController == null) return;

			var planeZ = m_planeController.transform.position.z;

			var spawnsThisFrame = 0;
			while (planeZ + m_spawnAheadDistance >= m_nextSpawnZ && spawnsThisFrame < k_MaxSpawnsPerFrame)
			{
				SpawnAt(m_nextSpawnZ);
				m_nextSpawnZ += m_spawnInterval + UnityEngine.Random.Range(-m_spawnIntervalJitter, m_spawnIntervalJitter);
				spawnsThisFrame++;
			}

			CullBehindPlane(planeZ);
		}

		private void SpawnAt(float spawnZ)
		{
			if (m_spawnables == null || m_spawnables.Length == 0) return;

			var entry = m_spawnables[UnityEngine.Random.Range(0, m_spawnables.Length)];
			if (entry.Prefab == null) return;

			var lateral = UnityEngine.Random.Range(-m_lateralRange, m_lateralRange);
			var vertical = UnityEngine.Random.Range(entry.VerticalMin, entry.VerticalMax);
			var position = new Vector3(lateral, vertical, spawnZ);

			var instance = Instantiate(entry.Prefab, position, Quaternion.identity);
			var spawnedEntity = instance.AddComponent<SpawnedEntity>();
			spawnedEntity.Initialize(entry.Kind);
			m_activeEntities.Add(spawnedEntity);
		}

		private void CullBehindPlane(float planeZ)
		{
			var cullThresholdZ = planeZ - m_despawnBehindDistance;

			for (var i = m_activeEntities.Count - 1; i >= 0; i--)
			{
				var entity = m_activeEntities[i];

				if (entity == null)
				{
					// Destroyed elsewhere (e.g. Building's own hit-destroy) - just stop tracking
					// it. Not a cull, so no SpawnEvents.EntityCulled here.
					m_activeEntities.RemoveAt(i);
					continue;
				}

				if (entity.transform.position.z >= cullThresholdZ) continue;

				SpawnEvents.RaiseEntityCulled(entity);
				m_activeEntities.RemoveAt(i);
				Destroy(entity.gameObject);
			}
		}

		private void HandleRunEnded()
		{
			m_spawningActive = false;
		}
	}
}
