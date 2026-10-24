using Opoint8182.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Toggles the <see cref="PressedClassName"/> USS class on its target while a pointer is held on it.
	/// Driven by raw pointer events instead of the <c>:active</c> pseudo-class, which is less predictable
	/// under touch/pointer capture (same reasoning as <c>TouchControlsUI</c>). The visual press (travel,
	/// scale, lip collapse) lives entirely in USS under <c>.is-pressed</c>.
	/// <para>
	/// It is also the one place the UI click (sound and a light haptic) is played, on the press rather than on the release (a
	/// click event only arrives on release, which reads as a delay): every pressable thing in the UI (buttons, tabs, chips,
	/// tiles, overflow rows, badges) carries this manipulator, so new ones get the feedback for free. A disabled target is
	/// silent. One physical press gives one click: a pointer-down while this target is already pressed, shortly after it
	/// was released (an echo some phones deliver at release) or from a mouse on a handheld is ignored.
	/// </para>
	/// </summary>
	public class PressedClassManipulator : PointerManipulator
	{
		public const string PressedClassName = "is-pressed";

		// A second pointer-down this soon after a release is the platform echoing the touch, not a new tap.
		private const float k_ReleaseEchoSeconds = 0.25f;

		private readonly bool m_playsSound;

		private bool m_pressed;
		private float m_lastReleaseTime = float.NegativeInfinity;

		/// <param name="playsSound">False for things with their own feedback (the boost button).</param>
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

			var echo = m_pressed
				|| Time.unscaledTime - m_lastReleaseTime < k_ReleaseEchoSeconds
				|| (Application.isMobilePlatform && evt.pointerType == UnityEngine.UIElements.PointerType.mouse);
			if (Debug.isDebugBuild) Debug.Log($"[UiClick] down '{target.name}' pointer={evt.pointerType}#{evt.pointerId} echo={echo} t={Time.unscaledTime:0.000}");

			m_pressed = true;
			if (echo || !m_playsSound || !target.enabledInHierarchy) return;

			Sfx.Play(SfxId.UiClick);
			Haptics.Play(HapticTier.Light);
		}

		private void OnPointerRelease(EventBase evt)
		{
			target.RemoveFromClassList(PressedClassName);

			if (!m_pressed) return;
			m_pressed = false;
			m_lastReleaseTime = Time.unscaledTime;
		}
	}
}
