using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.PlanesEditor
{
	/// <summary>
	/// Builds <c>Resources/PlaneCatalog.asset</c> from every <see cref="PlaneDefinition"/> in the project: planes already in
	/// the catalog keep their order, new ones are appended, deleted ones drop out, and the default id stays when it is still
	/// listed (else the first plane). Also holds the test shortcuts for the equipped plane.
	/// </summary>
	public static class PlaneCatalogBuilder
	{
		private const string k_CatalogPath = "Assets/_Project/Resources/PlaneCatalog.asset";

		[MenuItem("Tools/Planes/Create Or Update Catalog")]
		private static void BuildMenu() => Debug.Log(Build());

		[MenuItem("Tools/Planes/Log Equipped")]
		private static void LogEquipped()
		{
			var catalog = PlaneSelection.Catalog;
			var count = catalog == null ? 0 : catalog.Planes.Length;
			Debug.Log($"[Plane] equipped '{PlaneSelection.EquippedId}' (catalog default '{(catalog == null ? "-" : catalog.DefaultId)}', {count} planes).");
		}

		[MenuItem("Tools/Planes/Equip Next Plane")]
		private static void EquipNext()
		{
			var catalog = PlaneSelection.Catalog;
			if (catalog == null || catalog.Planes.Length == 0)
			{
				Debug.LogWarning("[Plane] No catalog or no planes: run Create Or Update Catalog first.");
				return;
			}

			var planes = catalog.Planes.Where(p => p != null).ToList();
			var index = planes.FindIndex(p => p.Id == PlaneSelection.EquippedId);
			PlaneSelection.Equip(planes[(index + 1) % planes.Count].Id);
			LogEquipped();
		}

		[MenuItem("Tools/Planes/Reset Equipped (back to the default)")]
		private static void ResetEquipped()
		{
			PlaneSelection.Reset();
			LogEquipped();
		}

		/// <summary>Rebuilds the asset and returns a one-line summary (also usable from the Unity CLI).</summary>
		public static string Build()
		{
			var catalog = AssetDatabase.LoadAssetAtPath<PlaneCatalog>(k_CatalogPath);
			if (catalog == null)
			{
				catalog = ScriptableObject.CreateInstance<PlaneCatalog>();
				AssetDatabase.CreateAsset(catalog, k_CatalogPath);
			}

			var found = AssetDatabase.FindAssets("t:PlaneDefinition")
				.Select(guid => AssetDatabase.LoadAssetAtPath<PlaneDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
				.Where(p => p != null)
				.ToList();

			var ordered = new List<PlaneDefinition>(catalog.Planes.Where(found.Contains));
			ordered.AddRange(found.Where(p => !ordered.Contains(p)).OrderBy(p => p.name, System.StringComparer.Ordinal));

			var duplicates = ordered.GroupBy(p => p.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

			var defaultId = ordered.Any(p => p.Id == catalog.DefaultId) ? catalog.DefaultId : ordered.Count > 0 ? ordered[0].Id : string.Empty;
			catalog.SetPlanes(ordered.ToArray(), defaultId);
			EditorUtility.SetDirty(catalog);
			AssetDatabase.SaveAssets();

			var summary = $"[PlaneCatalog] {ordered.Count} planes, default '{defaultId}'";
			return duplicates.Count == 0 ? summary : $"{summary}. DUPLICATE ids (fix them, only the first of each is reachable): {string.Join(", ", duplicates)}";
		}
	}
}
