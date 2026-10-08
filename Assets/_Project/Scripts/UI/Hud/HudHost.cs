using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Base for the components that sit next to the HUD's PanelRenderer and own a <see cref="HudView"/>: the in-game
	/// <see cref="HudPresenter"/> and the gallery's <see cref="HudStateDriver"/>. Rebuilds the view when the UI tree
	/// reloads and ignores repeat reload callbacks on the same tree.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public abstract class HudHost : MonoBehaviour
	{
		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;

		protected HudView View { get; private set; }

		protected virtual void Awake()
		{
			m_panelRenderer = GetComponent<PanelRenderer>();
		}

		protected virtual void OnEnable()
		{
			m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
		}

		protected virtual void OnDisable()
		{
			m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
			m_loadedRoot = null;
			View = null;
		}

		/// <summary>Called once per freshly loaded tree; push the current state into <paramref name="view"/> here.</summary>
		protected abstract void OnViewReady(HudView view);

		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			View = new HudView(root.Q<VisualElement>("hud-root"));
			OnViewReady(View);
		}
	}
}
