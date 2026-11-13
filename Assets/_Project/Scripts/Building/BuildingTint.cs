using System.Collections.Generic;
using UnityEngine;

namespace Opoint8182.Building
{
	/// <summary>
	/// Tints one material slot of a building's mesh, so Normal, Tough and Heal can share one model and one set of materials
	/// and differ only by this colour (chosen in the Inspector). At run time the slot gets a tinted copy of the slot's
	/// material that is made once per base material and colour and then shared by every building with that tint: no
	/// per-building material instance and nothing to clean up, and the SRP Batcher still batches it. In the Editor a
	/// <see cref="MaterialPropertyBlock"/> shows the colour for preview only: nothing is saved into the prefab or a material.
	/// </summary>
	public class BuildingTint : MonoBehaviour
	{
		private static readonly int m_S_BaseColorId = Shader.PropertyToID("_BaseColor");
		private static readonly Dictionary<(Material, Color), Material> m_S_Cache = new();

		[Tooltip("The mesh renderer that holds the material to tint.")]
		[SerializeField] private Renderer m_renderer;

		[Tooltip("Index of the material slot on the renderer that takes the colour.")]
		[SerializeField] private int m_materialIndex;

		[SerializeField] private Color m_color = Color.white;

		private void Awake()
		{
			Apply();
		}

		/// <summary>Swaps the slot for the shared tinted copy of its material.</summary>
		private void Apply()
		{
			if (m_renderer == null) return;

			var materials = m_renderer.sharedMaterials;
			if (m_materialIndex < 0 || m_materialIndex >= materials.Length || materials[m_materialIndex] == null)
			{
				Debug.LogWarning($"[BuildingTint] '{name}': material index {m_materialIndex} is not a material on the renderer.", this);
				return;
			}

			m_renderer.SetPropertyBlock(null, m_materialIndex);
			materials[m_materialIndex] = GetTinted(materials[m_materialIndex], m_color);
			m_renderer.sharedMaterials = materials;
		}

		private static Material GetTinted(Material source, Color color)
		{
			var key = (source, color);
			if (m_S_Cache.TryGetValue(key, out var cached) && cached != null) return cached;

			// Not saved into a scene or a build: it only lives for the session.
			var tinted = new Material(source) { name = $"{source.name} (tint)", hideFlags = HideFlags.DontSave };
			tinted.SetColor(m_S_BaseColorId, color);
			m_S_Cache[key] = tinted;
			return tinted;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the cache so it never holds a destroyed material.
			m_S_Cache.Clear();
		}

#if UNITY_EDITOR
		private void Reset()
		{
			foreach (var candidate in GetComponentsInChildren<Renderer>(true))
			{
				if (candidate.sharedMaterials.Length <= 1) continue;

				m_renderer = candidate;
				m_materialIndex = candidate.sharedMaterials.Length - 1;
				break;
			}
		}

		private void OnValidate()
		{
			if (Application.isPlaying || m_renderer == null) return;
			if (m_materialIndex < 0 || m_materialIndex >= m_renderer.sharedMaterials.Length) return;

			var block = new MaterialPropertyBlock();
			m_renderer.GetPropertyBlock(block, m_materialIndex);
			block.SetColor(m_S_BaseColorId, m_color);
			m_renderer.SetPropertyBlock(block, m_materialIndex);
		}
#endif
	}
}
