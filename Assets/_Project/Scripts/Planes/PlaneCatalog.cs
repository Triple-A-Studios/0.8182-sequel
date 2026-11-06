using System;
using UnityEngine;

namespace Opoint8182
{
	/// <summary>
	/// Every plane the game knows, in shop order, loaded from <c>Resources/PlaneCatalog</c>. Built and refreshed by
	/// <c>Tools > Planes > Create Or Update Catalog</c>. In its own file because Unity binds a ScriptableObject asset
	/// to its script by file name.
	/// </summary>
	[CreateAssetMenu(fileName = "PlaneCatalog", menuName = "Opoint8182/Plane Catalog")]
	public class PlaneCatalog : ScriptableObject
	{
		public const string RESOURCE_NAME = "PlaneCatalog";

		[SerializeField] private PlaneDefinition[] m_planes = Array.Empty<PlaneDefinition>();

		[Tooltip("Id of the plane a new player starts with, and the fallback for a saved id that no longer exists.")]
		[SerializeField] private string m_defaultId;

		public PlaneDefinition[] Planes => m_planes;
		public string DefaultId => m_defaultId;

		/// <summary>The default plane, or the first valid one when the default id is unset or unknown. Null for an empty catalog.</summary>
		public PlaneDefinition Default
		{
			get
			{
				if (TryGet(m_defaultId, out var plane)) return plane;

				foreach (var candidate in m_planes)
					if (candidate != null) return candidate;

				return null;
			}
		}

		public bool TryGet(string id, out PlaneDefinition plane)
		{
			plane = null;
			if (string.IsNullOrEmpty(id)) return false;

			foreach (var candidate in m_planes)
			{
				if (candidate == null || candidate.Id != id) continue;

				plane = candidate;
				return true;
			}

			return false;
		}

#if UNITY_EDITOR
		/// <summary>Editor builder hook: replaces the list, keeping the default id when it is still in it.</summary>
		public void SetPlanes(PlaneDefinition[] planes, string defaultId)
		{
			m_planes = planes;
			m_defaultId = defaultId;
		}
#endif
	}
}
