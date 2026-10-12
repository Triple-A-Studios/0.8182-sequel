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
		private VisualElement m_dragContainer;
		private IVisualElementScheduledItem m_snapWatch;

		/// <summary>
		/// Step the value settles on when the pointer is released (0 = continuous). While the pointer is down the
		/// knob and fill follow it freely and <see cref="Slider.value"/> is the raw position; use <see cref="Snap"/> to
		/// get the value a release at that moment would give (for a live readout).
		/// </summary>
		public float snapStep { get; set; }

		public ThemedSlider()
		{
			AddToClassList(UssClassName);

			m_fill = new VisualElement { pickingMode = PickingMode.Ignore };
			m_fill.AddToClassList(FillUssClassName);

			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<GeometryChangedEvent>(_ => UpdateFill());
			this.RegisterValueChangedCallback(_ =>
			{
				UpdateFill();
				WatchForRelease();
			});

			// Trickle: the capturing child (the drag container) is the target on release, so listen on the way down.
			RegisterCallback<PointerUpEvent>(_ => SnapToStep(), TrickleDown.TrickleDown);
			RegisterCallback<PointerCancelEvent>(_ => SnapToStep(), TrickleDown.TrickleDown);
		}

		/// <summary>The value on the nearest <see cref="snapStep"/> to <paramref name="rawValue"/>, clamped to the range.</summary>
		public float Snap(float rawValue)
		{
			if (snapStep <= 0f) return rawValue;

			var steps = Mathf.Round((rawValue - lowValue) / snapStep);
			var snapped = Mathf.Clamp(lowValue + steps * snapStep, lowValue, highValue);
			return Mathf.Round(snapped * 10000f) / 10000f;
		}

		public override void SetValueWithoutNotify(float newValue)
		{
			base.SetValueWithoutNotify(newValue);
			UpdateFill();
		}

		// Settles knob and fill on the step the live readout was already showing; silent, the readout is up to date.
		private void SnapToStep()
		{
			if (snapStep <= 0f) return;

			var snapped = Snap(value);
			if (!Mathf.Approximately(snapped, value)) SetValueWithoutNotify(snapped);
		}

		// The pointer-up listeners above are not enough on their own (the developer saw the knob stay put after a real
		// drag), so every value change also starts a short poll that snaps as soon as nothing inside the slider holds
		// the pointer any more. It pauses itself once the value sits on a step.
		private void WatchForRelease()
		{
			if (snapStep <= 0f) return;

			m_snapWatch ??= schedule.Execute(CheckRelease).Every(40);
			m_snapWatch.Resume();
		}

		private void CheckRelease()
		{
			if (IsPointerHeld()) return;

			SnapToStep();
			m_snapWatch.Pause();
		}

		private bool IsPointerHeld()
		{
			for (var pointerId = 0; pointerId < PointerId.maxPointers; pointerId++)
			{
				if (this.HasPointerCapture(pointerId)) return true;
				if (m_tracker != null && m_tracker.HasPointerCapture(pointerId)) return true;
				if (m_dragger != null && m_dragger.HasPointerCapture(pointerId)) return true;
				if (m_dragContainer != null && m_dragContainer.HasPointerCapture(pointerId)) return true;
			}

			return false;
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			m_tracker = this.Q<VisualElement>("unity-tracker");
			m_dragger = this.Q<VisualElement>("unity-dragger");
			m_dragContainer = this.Q<VisualElement>("unity-drag-container");
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
