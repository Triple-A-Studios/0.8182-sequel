using Opoint8182.Audio;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Toggles the <see cref="PressedClassName"/> USS class on its target while a pointer is held on it.
	/// Driven by raw pointer events instead of the <c>:active</c> pseudo-class, which is less predictable
	/// under touch/pointer capture (same reasoning as <c>TouchControlsUI</c>). The visual press (travel,
	/// scale, lip collapse) lives entirely in USS under <c>.is-pressed</c>.
	/// <para>
	/// It is also the one place the UI click sound is played, on the press rather than on the release (a click event only
	/// arrives on release, which reads as a delay): every pressable thing in the UI (buttons, tabs, chips, tiles, overflow
	/// rows, badges) carries this manipulator, so new ones get the sound for free. A disabled target is silent.
	/// </para>
	/// </summary>
	public class PressedClassManipulator : PointerManipulator
	{
		public const string PressedClassName = "is-pressed";

		private readonly bool m_playsSound;

		/// <param name="playsSound">False for things with their own sound (the boost button).</param>
		public PressedClassManipulator(bool playsSound = true)
		{
			m_playsSound = playsSound;
		}

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
			if (m_playsSound && target.enabledInHierarchy) Sfx.Play(SfxId.UiClick);
		}

		private void OnPointerRelease(EventBase evt)
		{
			target.RemoveFromClassList(PressedClassName);
		}
	}
}
