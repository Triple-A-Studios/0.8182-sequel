using UnityEngine;

namespace Opoint8182
{
	/// <summary>
	/// Puts the equipped plane's model under the plane's <c>Visual</c> object (the one <c>PlaneController</c> banks and
	/// pitches), and swaps it when the selection changes. The model is cosmetic: the root's capsule collider, rigidbody and
	/// flight numbers never change with the plane. <see cref="Spawn"/> is the one way a plane model gets into the world, so
	/// the menu's rooftop slot, the Profile portrait and the shop thumbnails reuse it later.
	/// </summary>
	public class PlaneVisualHost : MonoBehaviour
	{
		[Tooltip("The Visual object under the plane root: the same one PlaneController banks and pitches.")]
		[SerializeField] private Transform m_visualRoot;

		private GameObject m_model;

		private void OnEnable()
		{
			PlaneSelection.Changed += Refresh;
			Refresh();
		}

		private void OnDisable()
		{
			PlaneSelection.Changed -= Refresh;
		}

		private void Refresh()
		{
			if (m_model != null) Destroy(m_model);

			var plane = PlaneSelection.Equipped;
			if (plane == null)
			{
				Debug.LogWarning("[Plane] No plane to show: create Resources/PlaneCatalog with Tools > Planes > Create Or Update Catalog.", this);
				m_model = null;
				return;
			}

			m_model = Spawn(plane, m_visualRoot != null ? m_visualRoot : transform);
		}

		/// <summary>
		/// Instantiates a plane's visual prefab under <paramref name="parent"/> at the origin, with no collider, rigidbody
		/// or animator left on it. Null when the definition has no visual prefab.
		/// </summary>
		public static GameObject Spawn(PlaneDefinition plane, Transform parent)
		{
			if (plane == null || plane.VisualPrefab == null)
			{
				Debug.LogWarning($"[Plane] '{(plane == null ? "(none)" : plane.name)}' has no visual prefab.", plane);
				return null;
			}

			var model = Instantiate(plane.VisualPrefab, parent);
			model.transform.localPosition = Vector3.zero;
			model.transform.localRotation = Quaternion.identity;

			// Stripped right away on the fresh clone so nothing can collide for a frame: only the root capsule is the hit shape.
			foreach (var collider in model.GetComponentsInChildren<Collider>(true)) DestroyImmediate(collider);
			foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(body);
			foreach (var animator in model.GetComponentsInChildren<Animator>(true)) DestroyImmediate(animator);

			return model;
		}
	}
}
