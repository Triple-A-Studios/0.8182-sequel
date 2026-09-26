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

			// Weight over normalized ramp progress (0 = run start, 1 = this entry's effective ramp
			// distance reached). ClampProgressAtRampCap = true holds the curve's t=1 value forever
			// once the ramp caps (every hazard/building type); false keeps sampling past t=1 on the
			// curve's own raw, ever-growing progress - used only by HealthPickup, whose curve is
			// authored flat through the ramp then decaying to a floor well past it.
			[SerializeField] private AnimationCurve m_weightCurve;
			[SerializeField] private bool m_clampProgressAtRampCap;

			// Pass 6: per-entry plateau distance, staggered instead of every stat sharing one
			// global cap. 0 = fall back to the manager-wide m_difficultyRampDistance - the correct
			// default for a struct-array field added after entries already existed, since Unity
			// zero-inits new serialized fields on existing elements rather than re-running C#
			// field initializers.
			[SerializeField] private float m_rampDistanceOverride;

			public GameObject Prefab => m_prefab;
			public SpawnKind Kind => m_kind;
			public float VerticalMin => m_verticalMin;
			public float VerticalMax => m_verticalMax;
			public AnimationCurve WeightCurve => m_weightCurve;
			public bool ClampProgressAtRampCap => m_clampProgressAtRampCap;
			public float RampDistanceOverride => m_rampDistanceOverride;
		}

		[Serializable]
		private struct SpawnChunkSlot
		{
			[SerializeField] private float m_zOffset;
			// -1 = weighted roll (semi-random slot); >= 0 = fixed index into m_spawnables
			// (authored slot) - lets a chunk mix pre-authored structure with random fill.
			[SerializeField] private int m_spawnableIndex;

			public float ZOffset => m_zOffset;
			public int SpawnableIndex => m_spawnableIndex;
		}

		[Serializable]
		private struct SpawnChunk
		{
			[SerializeField] private string m_name; // editor label only, not read at runtime
			// Chunk only enters the eligible pool once DistanceTraveled reaches this - the "pool
			// grows with distance" lever from design-doc.md's parking lot.
			[SerializeField] private float m_minDistance;
			[SerializeField] private SpawnChunkSlot[] m_slots;

			public float MinDistance => m_minDistance;
			public SpawnChunkSlot[] Slots => m_slots;
		}

		[Title("Plane Reference")]
		[FoldoutGroup("Plane Reference")] [SerializeField] private PlaneController m_planeController;

		[Title("Spawnables")]
		[FoldoutGroup("Spawnables")] [SerializeField] private SpawnableEntry[] m_spawnables;

		[Title("Spawn Chunks")]
		[FoldoutGroup("Spawn Chunks")] [SerializeField] private SpawnChunk[] m_chunks;

		[Title("Difficulty Ramp")]
		[FoldoutGroup("Difficulty Ramp")] [SerializeField] private float m_difficultyRampDistance = 3000f;

		[Title("Spawn Pattern")]
		[FoldoutGroup("Spawn Pattern")] [SerializeField] private float m_spawnAheadDistance = 100f;
		[FoldoutGroup("Spawn Pattern")] [SerializeField] private AnimationCurve m_spawnIntervalCurve = AnimationCurve.Linear(0f, 18f, 1f, 8f);
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

		// Clamped ramp progress (0 at run start, 1 once m_difficultyRampDistance is reached, held
		// there after). Entries that don't clamp (HealthPickup) sample their curve on raw,
		// ever-growing progress instead - see ComputeWeight.
		public float DifficultyProgress01 =>
			m_difficultyRampDistance > 0f ? Mathf.Clamp01(DistanceTraveled / m_difficultyRampDistance) : 1f;

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
				var chunkLength = SpawnChunkAt(m_nextSpawnZ);
				var advance = chunkLength + ComputeSpawnInterval() + UnityEngine.Random.Range(-m_spawnIntervalJitter, m_spawnIntervalJitter);
				// A shrinking interval curve combined with jitter can otherwise go non-positive,
				// walking m_nextSpawnZ backward and spamming spawns at the same spot every frame.
				// chunkLength is added on top so a multi-slot chunk's own internal length doesn't
				// overlap the next chunk's items.
				m_nextSpawnZ += Mathf.Max(1f, advance);
				spawnsThisFrame++;
			}

			CullBehindPlane(planeZ);
		}

		private float ComputeSpawnInterval()
		{
			return Mathf.Max(0.5f, m_spawnIntervalCurve.Evaluate(DifficultyProgress01));
		}

		private float ComputeWeight(SpawnableEntry entry)
		{
			// Pass 6: sample against this entry's own effective ramp distance, not the shared
			// DifficultyProgress01 - lets tough/normal ratio, density (still keyed to the global
			// distance via ComputeSpawnInterval/DifficultyProgress01), and pickup rarity plateau at
			// different distances instead of all freezing into one steady state together.
			var rampDistance = entry.RampDistanceOverride > 0f ? entry.RampDistanceOverride : m_difficultyRampDistance;
			var rawProgress = rampDistance > 0f ? DistanceTraveled / rampDistance : 1f;
			var sampleT = entry.ClampProgressAtRampCap ? Mathf.Clamp01(rawProgress) : rawProgress;
			return Mathf.Max(0f, entry.WeightCurve.Evaluate(sampleT));
		}

		private bool TryPickChunk(out SpawnChunk chunk)
		{
			if (m_chunks == null || m_chunks.Length == 0)
			{
				chunk = default;
				return false;
			}

			var distance = DistanceTraveled;
			var eligibleCount = 0;
			for (var i = 0; i < m_chunks.Length; i++) if (distance >= m_chunks[i].MinDistance) eligibleCount++;

			if (eligibleCount == 0)
			{
				chunk = default;
				return false;
			}

			var pick = UnityEngine.Random.Range(0, eligibleCount);
			for (var i = 0; i < m_chunks.Length; i++)
			{
				if (distance < m_chunks[i].MinDistance) continue;
				if (pick == 0)
				{
					chunk = m_chunks[i];
					return true;
				}

				pick--;
			}

			chunk = default;
			return false;
		}

		private bool TryPickWeightedEntry(out SpawnableEntry picked)
		{
			var totalWeight = 0f;
			for (var i = 0; i < m_spawnables.Length; i++) totalWeight += ComputeWeight(m_spawnables[i]);

			if (totalWeight <= 0.0001f)
			{
				picked = default;
				return false;
			}

			var roll = UnityEngine.Random.Range(0f, totalWeight);
			var cumulative = 0f;
			for (var i = 0; i < m_spawnables.Length; i++)
			{
				cumulative += ComputeWeight(m_spawnables[i]);
				if (roll <= cumulative)
				{
					picked = m_spawnables[i];
					return true;
				}
			}

			picked = m_spawnables[^1]; // float-rounding fallback
			return true;
		}

		private void PlaceEntry(SpawnableEntry entry, float spawnZ)
		{
			if (entry.Prefab == null) return;

			var lateral = UnityEngine.Random.Range(-m_lateralRange, m_lateralRange);
			var vertical = UnityEngine.Random.Range(entry.VerticalMin, entry.VerticalMax);
			var position = new Vector3(lateral, vertical, spawnZ);

			var instance = Instantiate(entry.Prefab, position, Quaternion.identity);
			var spawnedEntity = instance.AddComponent<SpawnedEntity>();
			spawnedEntity.Initialize(entry.Kind);
			m_activeEntities.Add(spawnedEntity);
		}

		// Pass 6: replaces the old one-entity-per-tick SpawnAt. Returns the chunk's max slot
		// offset so the caller's schedule can add it on top of the normal interval, keeping a
		// multi-slot chunk's own length from overlapping the next spawn event.
		private float SpawnChunkAt(float anchorZ)
		{
			if (m_spawnables == null || m_spawnables.Length == 0) return 0f;

			if (!TryPickChunk(out var chunk))
			{
				// No chunk eligible (e.g. m_chunks unauthored) - fall back to a single independent
				// roll rather than spawning nothing. Not the normal path: the always-eligible
				// "Solo" chunk (m_minDistance 0) covers this in authored data.
				if (TryPickWeightedEntry(out var fallbackEntry)) PlaceEntry(fallbackEntry, anchorZ);
				return 0f;
			}

			var maxOffset = 0f;
			foreach (var slot in chunk.Slots)
			{
				var hasEntry = true;
				var entry = default(SpawnableEntry);

				if (slot.SpawnableIndex >= 0 && slot.SpawnableIndex < m_spawnables.Length)
				{
					entry = m_spawnables[slot.SpawnableIndex];
				}
				else
				{
					hasEntry = TryPickWeightedEntry(out entry);
				}

				if (hasEntry) PlaceEntry(entry, anchorZ + slot.ZOffset);
				maxOffset = Mathf.Max(maxOffset, slot.ZOffset);
			}

			return maxOffset;
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
