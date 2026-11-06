using UnityEngine;

namespace Opoint8182
{
	/// <summary>
	/// One selectable plane: a visual only. Every plane shares the root collider and the <c>PlaneController</c> numbers
	/// on <c>Plane.prefab</c>, so scores stay comparable and a shop plane is never an advantage (see planes.md).
	/// In its own file because Unity binds a ScriptableObject asset to its script by file name.
	/// </summary>
	[CreateAssetMenu(fileName = "Plane_", menuName = "Opoint8182/Plane Definition")]
	public class PlaneDefinition : ScriptableObject
	{
		// The art spec a visual prefab should match (the root capsule is 4 long and 1 wide, the primitive wings spanned 6).
		private const float k_MinLength = 2f;
		private const float k_MaxLength = 8f;
		private const float k_MinSpan = 3f;
		private const float k_MaxSpan = 10f;
		private const float k_MaxPivotOffset = 0.5f;

		[Tooltip("Stable key saved on the device and, later, on the server. Never shown, never renamed once shipped.")]
		[SerializeField] private string m_id;

		[SerializeField] private string m_displayName;

		[Tooltip("Meshes and materials only: pivot at the centre, nose along +Z, no colliders, rigidbody or animator.")]
		[SerializeField] private GameObject m_visualPrefab;

		[Tooltip("Optional card image for the shop and the Profile.")]
		[SerializeField] private Sprite m_thumbnail;

		public string Id => m_id;
		public string DisplayName => string.IsNullOrEmpty(m_displayName) ? m_id : m_displayName;
		public GameObject VisualPrefab => m_visualPrefab;
		public Sprite Thumbnail => m_thumbnail;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrWhiteSpace(m_id)) m_id = name.StartsWith("Plane_") ? name.Substring("Plane_".Length).ToLowerInvariant() : name.ToLowerInvariant();
			if (m_visualPrefab == null) return;

			if (m_visualPrefab.GetComponentInChildren<Collider>(true) != null)
				Debug.LogWarning($"[Plane] '{name}': the visual prefab has a collider. Remove it: the plane's hit shape is the capsule on Plane.prefab (colliders are stripped at runtime anyway).", this);
			if (m_visualPrefab.GetComponentInChildren<Rigidbody>(true) != null)
				Debug.LogWarning($"[Plane] '{name}': the visual prefab has a Rigidbody. Remove it.", this);

			var renderers = m_visualPrefab.GetComponentsInChildren<Renderer>(true);
			if (renderers.Length == 0)
			{
				Debug.LogWarning($"[Plane] '{name}': the visual prefab has no renderer.", this);
				return;
			}

			var bounds = renderers[0].bounds;
			for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

			if (bounds.size.z < k_MinLength || bounds.size.z > k_MaxLength)
				Debug.LogWarning($"[Plane] '{name}': length (Z) is {bounds.size.z:0.0}, expected {k_MinLength:0}-{k_MaxLength:0} (the capsule is 4). Check scale and that the nose points along +Z.", this);
			if (bounds.size.x < k_MinSpan || bounds.size.x > k_MaxSpan)
				Debug.LogWarning($"[Plane] '{name}': wing span (X) is {bounds.size.x:0.0}, expected {k_MinSpan:0}-{k_MaxSpan:0} (the primitive plane was 6). Check scale and orientation.", this);
			if (bounds.center.magnitude > k_MaxPivotOffset)
				Debug.LogWarning($"[Plane] '{name}': the model centre is {bounds.center.magnitude:0.0} from the prefab origin. Move the pivot to the centre: banking and pitching rotate around it.", this);
		}
#endif
	}
}
