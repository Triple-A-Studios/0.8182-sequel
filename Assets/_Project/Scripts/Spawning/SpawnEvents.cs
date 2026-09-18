using System;

namespace Opoint8182.Spawning
{
	// Static event bus for the spawner, mirroring Opoint8182.Common.CombatEvents' pattern.
	// EntityCulled fires only from SpawnManager's distance-cull loop - an entity destroyed by
	// its own hit logic (e.g. Building.OnTriggerEnter -> Destroy(gameObject)) never reaches this
	// event, so by construction EntityCulled already means "this instance was never hit."
	public static class SpawnEvents
	{
		public static event Action<SpawnedEntity> EntityCulled;

		public static void RaiseEntityCulled(SpawnedEntity entity)
		{
			EntityCulled?.Invoke(entity);
		}
	}
}
