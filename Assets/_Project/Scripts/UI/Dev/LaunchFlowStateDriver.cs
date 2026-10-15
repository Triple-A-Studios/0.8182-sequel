using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the launch flow screens (Loading + rotate gate; the last-week's-results screen joins it in
	/// Pass 7c): loads <c>Loading.uxml</c> into the LaunchFlowGallery scene so the states that are quick or hard to reach
	/// in a real launch can be looked at. Keys: 1 to 9 progress 10 to 90%, 0 progress 100%, Q a new quip,
	/// R toggles the rotate prompt.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public class LaunchFlowStateDriver : MonoBehaviour
	{
		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private LoadingScreen m_loading;

		private void Awake()
		{
			m_panelRenderer = GetComponent<PanelRenderer>();
		}

		private void OnEnable()
		{
			m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
		}

		private void OnDisable()
		{
			m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
			m_loadedRoot = null;
			m_loading = null;
		}

		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			SafeAreaHelper.Apply(root.Q<VisualElement>("launch-safe-area"));
			m_loading = new LoadingScreen(root.Q<VisualElement>("launch-root"));
			m_loading.Show();
			m_loading.SetProgress(0.62f);
			m_loading.SetQuip(LoadingQuips.Sample);
		}

		private void Update()
		{
			var keyboard = Keyboard.current;
			if (keyboard == null || m_loading == null) return;

			if (keyboard.digit1Key.wasPressedThisFrame) m_loading.SetProgress(0.1f);
			if (keyboard.digit2Key.wasPressedThisFrame) m_loading.SetProgress(0.2f);
			if (keyboard.digit3Key.wasPressedThisFrame) m_loading.SetProgress(0.3f);
			if (keyboard.digit4Key.wasPressedThisFrame) m_loading.SetProgress(0.4f);
			if (keyboard.digit5Key.wasPressedThisFrame) m_loading.SetProgress(0.5f);
			if (keyboard.digit6Key.wasPressedThisFrame) m_loading.SetProgress(0.6f);
			if (keyboard.digit7Key.wasPressedThisFrame) m_loading.SetProgress(0.7f);
			if (keyboard.digit8Key.wasPressedThisFrame) m_loading.SetProgress(0.8f);
			if (keyboard.digit9Key.wasPressedThisFrame) m_loading.SetProgress(0.9f);
			if (keyboard.digit0Key.wasPressedThisFrame) m_loading.SetProgress(1f);

			if (keyboard.qKey.wasPressedThisFrame) m_loading.SetQuip(LoadingQuips.Pick());
			if (keyboard.rKey.wasPressedThisFrame) m_loading.SetRotateGate(!m_loading.RotateGateVisible);
		}
	}
}
