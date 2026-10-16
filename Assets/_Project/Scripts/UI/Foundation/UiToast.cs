using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// The one short-lived message toast shared by the menu, HUD, Game Over and launch-flow screens (UI/art pass,
	/// Pass 8a; it replaced four copies and the rapid-click stub machinery). Builds its own element, styled by
	/// <c>.ui-toast</c> in <c>Theme.uss</c>, and adds it as the last child of <paramref name="host"/> so it draws on
	/// top. Showing again restarts the timer instead of stacking. Never takes pointer events.
	/// </summary>
	public class UiToast
	{
		public const long DEFAULT_DURATION_MS = 2500;

		private readonly VisualElement m_root;
		private readonly Label m_label;
		private IVisualElementScheduledItem m_hide;

		/// <param name="host">Full-panel (or full-screen) element to put the toast in.</param>
		/// <param name="topOffset">Distance from the host's top edge, or null to centre the toast on the host.</param>
		public UiToast(VisualElement host, float? topOffset = null)
		{
			m_root = new VisualElement { name = "ui-toast", pickingMode = PickingMode.Ignore };
			m_root.AddToClassList("ui-toast");
			if (topOffset.HasValue)
			{
				m_root.AddToClassList("ui-toast--top");
				m_root.style.top = topOffset.Value;
			}

			m_label = new Label { name = "ui-toast-label", pickingMode = PickingMode.Ignore };
			m_label.AddToClassList("ui-toast__label");
			m_root.Add(m_label);

			host.Add(m_root);
		}

		public bool IsVisible => m_root.resolvedStyle.display != DisplayStyle.None;

		public string Text => m_label.text;

		public void Show(string message, long durationMs = DEFAULT_DURATION_MS)
		{
			m_label.text = message;
			m_root.style.display = DisplayStyle.Flex;

			m_hide?.Pause();
			m_hide = m_root.schedule.Execute(Hide).StartingIn(durationMs);
		}

		public void Hide()
		{
			m_hide?.Pause();
			m_root.style.display = DisplayStyle.None;
		}
	}
}
