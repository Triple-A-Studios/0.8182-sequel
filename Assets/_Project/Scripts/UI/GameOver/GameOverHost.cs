using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Base for the components that sit next to the Game Over PanelRenderer and own a <see cref="GameOverView"/>: the
	/// in-game <see cref="GameOverUI"/> and the gallery's <see cref="GameOverStateDriver"/>. Rebuilds the view when the UI
	/// tree reloads and ignores repeat reload callbacks on the same tree (same shape as <see cref="HudHost"/>).
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public abstract class GameOverHost : MonoBehaviour
	{
		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;

		protected GameOverView View { get; private set; }

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

		/// <summary>Called once per freshly loaded tree; hook events and push the current state into <paramref name="view"/> here.</summary>
		protected abstract void OnViewReady(GameOverView view);

		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			View = new GameOverView(root.Q<VisualElement>("game-over-root"));
			OnViewReady(View);
		}
	}
}
