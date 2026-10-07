using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Dev-only harness for the UI foundation (Pass 2): shows every theme component and state at the reference
	/// resolution and exercises <see cref="ScreenRouter"/> (push/back/popup) and <see cref="SafeAreaHelper"/>.
	/// Lives in the UIGallery scene only; kept as a regression reference for later passes.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public class UIGalleryController : MonoBehaviour
	{
		private const string k_GalleryScreenId = "gallery";
		private const string k_DetailScreenId = "detail";

		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private ScreenRouter m_router;
		private ScrollView m_scroll;

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
			m_scroll = null;
		}

		private void Update()
		{
			if (m_router == null) return;

			var keyboard = Keyboard.current;
			if (keyboard == null) return;

			if (keyboard.escapeKey.wasPressedThisFrame) m_router.HandleBackRequested();

			// PageUp/PageDown page the gallery (handy when driving it without a mouse wheel).
			if (m_scroll == null) return;
			var page = m_scroll.contentViewport.layout.height * 0.9f;
			if (keyboard.pageDownKey.wasPressedThisFrame) m_scroll.scrollOffset += new Vector2(0f, page);
			if (keyboard.pageUpKey.wasPressedThisFrame) m_scroll.scrollOffset -= new Vector2(0f, page);
		}

		// Can fire more than once per enable (live reload); a repeat call on the same tree must not double-register.
		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			SafeAreaHelper.Apply(root.Q<VisualElement>("safe-area"));
			m_scroll = root.Q<ScrollView>("gallery-scroll");

			m_router = new ScreenRouter(root.Q<VisualElement>("popup-layer"));
			m_router.Register(k_GalleryScreenId, root.Q<VisualElement>("screen-gallery"));
			m_router.Register(k_DetailScreenId, root.Q<VisualElement>("screen-detail"));
			m_router.SetRoot(k_GalleryScreenId);

			var popup = root.Q<VisualElement>("popup-confirm");

			root.Q<LipButton>("gallery-back").clicked += () => m_router.HandleBackRequested();
			root.Q<LipButton>("detail-back").clicked += () => m_router.HandleBackRequested();
			root.Q<LipButton>("open-detail").clicked += () => m_router.Show(k_DetailScreenId);
			root.Q<LipButton>("open-popup").clicked += () => m_router.ShowPopup(popup);
			root.Q<LipButton>("popup-confirm-yes").clicked += () => m_router.ClosePopup();
			root.Q<LipButton>("popup-confirm-no").clicked += () => m_router.ClosePopup();

			root.Q<LipButton>("btn-disabled").SetEnabled(false);

			var readout = root.Q<Label>("tabs-readout");
			var amberTabs = root.Q<TabBar>("tabs-amber");
			amberTabs.changed += index => readout.text = $"selected: {(index == 0 ? "WEEKLY" : "ALL-TIME")}";

			var sliderReadout = root.Q<Label>("slider-readout");
			root.Q<ThemedSlider>("slider-sound").RegisterValueChangedCallback(evt => sliderReadout.text = Mathf.RoundToInt(evt.newValue).ToString());
		}
	}
}
