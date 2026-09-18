using UnityEngine;

namespace Opoint8182.Spawning
{
	public enum SpawnKind
	{
		BuildingNormal,
		BuildingTough,
		ObstacleLarge,
		BirdSmall,
		HealthPickup
	}

	// Marker component added at runtime (AddComponent) to every instance SpawnManager spawns.
	// Lets the manager track/cull spawned instances without GetComponent<Building>/
	// <ObstacleBuilding>/<BirdHazard> type-switching, and gives later passes (Pass 3's
	// missed-building combo signal) a single well-known component + Kind to filter on.
	public class SpawnedEntity : MonoBehaviour
	{
		public SpawnKind Kind { get; private set; }

		public void Initialize(SpawnKind kind)
		{
			Kind = kind;
		}
	}
}
