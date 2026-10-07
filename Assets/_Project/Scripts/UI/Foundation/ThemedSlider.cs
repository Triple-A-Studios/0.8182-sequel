using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// <see cref="Slider"/> with the mockup's amber fill. The built-in slider has a tracker and a dragger but no
	/// "filled" portion, so this adds a fill element inside the tracker and keeps it ending at the dragger's
	/// centre. All styling (including the fill colour) is in <c>Theme.uss</c> under <c>.themed-slider</c>.
	/// </summary>
	[UxmlElement]
	public partial class ThemedSlider : Slider
	{
		public const string UssClassName = "themed-slider";
		public const string FillUssClassName = "themed-slider__fill";

		private readonly VisualElement m_fill;
		private VisualElement m_tracker;
		private VisualElement m_dragger;

		public ThemedSlider()
		{
			AddToClassList(UssClassName);

			m_fill = new VisualElement { pickingMode = PickingMode.Ignore };
			m_fill.AddToClassList(FillUssClassName);

			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<GeometryChangedEvent>(_ => UpdateFill());
			this.RegisterValueChangedCallback(_ => UpdateFill());
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			m_tracker = this.Q<VisualElement>("unity-tracker");
			m_dragger = this.Q<VisualElement>("unity-dragger");
			if (m_tracker == null || m_dragger == null) return;

			if (m_fill.parent != m_tracker) m_tracker.Add(m_fill);
			m_dragger.RegisterCallback<GeometryChangedEvent>(_ => UpdateFill());
			UpdateFill();
		}

		private void UpdateFill()
		{
			if (m_tracker == null || m_dragger == null) return;

			var draggerWidth = m_dragger.resolvedStyle.width;
			var trackWidth = m_tracker.resolvedStyle.width;
			if (float.IsNaN(draggerWidth) || float.IsNaN(trackWidth)) return;

			var t = Mathf.InverseLerp(lowValue, highValue, value);
			m_fill.style.width = t * (trackWidth - draggerWidth) + draggerWidth * 0.5f;
		}
	}
}
