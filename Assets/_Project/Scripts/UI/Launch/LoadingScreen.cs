using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// The loading screen's view (mockup S01): wordmark, key-art slot with the studio logo, a quip, the progress bar
	/// with its percentage, and the rotate-device prompt that replaces the art while the page is portrait. Plain class
	/// over the launch panel's root; <see cref="LaunchFlowHost"/> drives it.
	/// </summary>
	public class LoadingScreen
	{
		private readonly VisualElement m_root;
		private readonly VisualElement m_content;
		private readonly VisualElement m_art;
		private readonly VisualElement m_rotate;
		private readonly VisualElement m_barFill;
		private readonly Label m_percent;
		private readonly Label m_quip;

		private float m_shown = -1f;

		public VisualElement Root => m_root;

		/// <summary>The bar's current fill, 0 to 1 (what is on screen, after the easing).</summary>
		public float ShownProgress => Mathf.Max(0f, m_shown);

		public bool RotateGateVisible { get; private set; }

		public LoadingScreen(VisualElement root)
		{
			m_root = root;
			m_content = root.Q<VisualElement>("loading-content");
			m_art = root.Q<VisualElement>("loading-art");
			m_rotate = root.Q<VisualElement>("loading-rotate");
			m_barFill = root.Q<VisualElement>("loading-bar-fill");
			m_percent = root.Q<Label>("loading-percent");
			m_quip = root.Q<Label>("loading-quip");

			SetRotateGate(false);
			SetProgress(0f);
			SetQuip(LoadingQuips.Pick());
		}

		public void SetQuip(string quip)
		{
			m_quip.text = $"\"{quip}\"";
		}

		/// <summary>Sets the bar and the percentage label. 0 to 1.</summary>
		public void SetProgress(float progress)
		{
			progress = Mathf.Clamp01(progress);
			if (Mathf.Approximately(progress, m_shown)) return;

			m_shown = progress;
			m_barFill.style.width = Length.Percent(progress * 100f);
			m_percent.text = $"{Mathf.RoundToInt(progress * 100f)}%";
		}

		/// <summary>Swaps the key art for the rotate prompt (the bar stays, so progress visibly holds).</summary>
		public void SetRotateGate(bool visible)
		{
			RotateGateVisible = visible;
			m_art.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
			m_rotate.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>Hides or shows the loading content itself (the launch panel shows the results screen instead).</summary>
		public void SetContentVisible(bool visible)
		{
			m_content.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public void Show()
		{
			m_root.style.display = DisplayStyle.Flex;
		}

		public void Hide()
		{
			m_root.style.display = DisplayStyle.None;
		}
	}
}
