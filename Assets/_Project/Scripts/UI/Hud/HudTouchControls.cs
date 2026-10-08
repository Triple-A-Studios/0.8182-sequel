using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Pointer logic for the HUD's on-screen controls: a floating joystick (appears under the first touch in the left
	/// half of the panel) and the boost button. Ported from the old per-panel TouchControlsUI; it only raises events, the
	/// <see cref="HudPresenter"/> writes them to the plane. Positions are in panel space, so the joystick base must be a
	/// child of a full-panel, non-inset element (<c>hud-input</c>).
	/// </summary>
	public class HudTouchControls
	{
		/// <summary>Finger travel (panel px) that gives full steer deflection. Unchanged from the old joystick, so the feel is the same.</summary>
		public const float SteerFullRadius = 50f;

		/// <summary>How far the handle may travel from the base centre: (base 300 - handle 132) / 2, matches <c>.hud-joy-base</c> / <c>.hud-joy-handle</c>.</summary>
		public const float HandleMaxTravel = 84f;

		/// <summary>Matches <c>.hud-joy-base</c> width/height in Hud.uss; used to centre the base on the touch without a layout query.</summary>
		public const float BaseSize = 300f;

		private readonly VisualElement m_zone;
		private readonly VisualElement m_base;
		private readonly VisualElement m_handle;
		private readonly VisualElement m_boostButton;
		private Vector2 m_origin;

		/// <summary>Steer in -1..1 per axis, up positive (matches PlaneController.TouchSteer).</summary>
		public event Action<Vector2> SteerChanged;

		public event Action<bool> BoostChanged;

		public HudTouchControls(VisualElement root)
		{
			m_zone = root.Q<VisualElement>("touch-joystick-zone");
			m_base = root.Q<VisualElement>("touch-joystick-background");
			m_handle = root.Q<VisualElement>("touch-joystick-handle");
			m_boostButton = root.Q<VisualElement>("touch-boost-button");

			m_zone.RegisterCallback<PointerDownEvent>(OnJoystickPointerDown);
			m_zone.RegisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
			m_zone.RegisterCallback<PointerUpEvent>(OnJoystickPointerUp);
			m_zone.RegisterCallback<PointerCancelEvent>(OnJoystickPointerCancel);

			m_boostButton.AddManipulator(new PressedClassManipulator());
			m_boostButton.RegisterCallback<PointerDownEvent>(OnBoostPointerDown);
			m_boostButton.RegisterCallback<PointerUpEvent>(OnBoostPointerUp);
			m_boostButton.RegisterCallback<PointerCancelEvent>(OnBoostPointerCancel);
		}

		/// <summary>Shows the joystick base at <paramref name="origin"/> (panel px) with the handle offset; used by the gallery preview.</summary>
		public void ShowPreview(Vector2 origin, Vector2 handleOffset)
		{
			PlaceBase(origin);
			SetHandle(handleOffset);
		}

		public void HidePreview()
		{
			HideBase();
		}

		private void OnJoystickPointerDown(PointerDownEvent evt)
		{
			m_zone.CapturePointer(evt.pointerId);

			m_origin = evt.position;
			PlaceBase(m_origin);
			UpdateJoystick(evt.position);
		}

		private void OnJoystickPointerMove(PointerMoveEvent evt)
		{
			if (!m_zone.HasPointerCapture(evt.pointerId)) return;
			UpdateJoystick(evt.position);
		}

		private void OnJoystickPointerUp(PointerUpEvent evt)
		{
			if (!m_zone.HasPointerCapture(evt.pointerId)) return;
			m_zone.ReleasePointer(evt.pointerId);
			ResetJoystick();
		}

		private void OnJoystickPointerCancel(PointerCancelEvent evt)
		{
			if (!m_zone.HasPointerCapture(evt.pointerId)) return;
			m_zone.ReleasePointer(evt.pointerId);
			ResetJoystick();
		}

		private void PlaceBase(Vector2 origin)
		{
			var halfSize = BaseSize * 0.5f;
			m_base.style.left = origin.x - halfSize;
			m_base.style.top = origin.y - halfSize;
			m_base.style.display = DisplayStyle.Flex;
		}

		private void HideBase()
		{
			m_base.style.display = DisplayStyle.None;
			SetHandle(Vector2.zero);
		}

		private void SetHandle(Vector2 offset)
		{
			m_handle.style.translate = new StyleTranslate(new Translate(offset.x, offset.y, 0f));
		}

		private void UpdateJoystick(Vector2 pointerPosition)
		{
			var delta = pointerPosition - m_origin;

			SetHandle(Vector2.ClampMagnitude(delta, HandleMaxTravel));

			// UI-space Y grows downward; gameplay steer Y is "up positive" (matches PlaneController.Velocity.y).
			var steer = Vector2.ClampMagnitude(delta, SteerFullRadius) / SteerFullRadius;
			SteerChanged?.Invoke(new Vector2(steer.x, -steer.y));
		}

		private void ResetJoystick()
		{
			HideBase();
			SteerChanged?.Invoke(Vector2.zero);
		}

		private void OnBoostPointerDown(PointerDownEvent evt)
		{
			m_boostButton.CapturePointer(evt.pointerId);
			BoostChanged?.Invoke(true);
		}

		private void OnBoostPointerUp(PointerUpEvent evt)
		{
			if (!m_boostButton.HasPointerCapture(evt.pointerId)) return;
			m_boostButton.ReleasePointer(evt.pointerId);
			BoostChanged?.Invoke(false);
		}

		private void OnBoostPointerCancel(PointerCancelEvent evt)
		{
			if (!m_boostButton.HasPointerCapture(evt.pointerId)) return;
			m_boostButton.ReleasePointer(evt.pointerId);
			BoostChanged?.Invoke(false);
		}
	}
}
