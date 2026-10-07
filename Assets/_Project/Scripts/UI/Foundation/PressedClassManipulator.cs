using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Toggles the <see cref="PressedClassName"/> USS class on its target while a pointer is held on it.
	/// Driven by raw pointer events instead of the <c>:active</c> pseudo-class, which is less predictable
	/// under touch/pointer capture (same reasoning as <c>TouchControlsUI</c>). The visual press (travel,
	/// scale, lip collapse) lives entirely in USS under <c>.is-pressed</c>.
	/// </summary>
	public class PressedClassManipulator : PointerManipulator
	{
		public const string PressedClassName = "is-pressed";

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<PointerDownEvent>(OnPointerDown);
			target.RegisterCallback<PointerUpEvent>(OnPointerRelease);
			target.RegisterCallback<PointerLeaveEvent>(OnPointerRelease);
			target.RegisterCallback<PointerCancelEvent>(OnPointerRelease);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			target.UnregisterCallback<PointerUpEvent>(OnPointerRelease);
			target.UnregisterCallback<PointerLeaveEvent>(OnPointerRelease);
			target.UnregisterCallback<PointerCancelEvent>(OnPointerRelease);
			target.RemoveFromClassList(PressedClassName);
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			target.AddToClassList(PressedClassName);
		}

		private void OnPointerRelease(EventBase evt)
		{
			target.RemoveFromClassList(PressedClassName);
		}
	}
}
