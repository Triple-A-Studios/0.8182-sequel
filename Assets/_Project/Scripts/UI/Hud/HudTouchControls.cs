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
		// The values below are the M size (Settings JOYSTICK SIZE); S / M / L multiply all of them by m_S_SizeScales, applied
		// inline over the Hud.uss defaults when a touch starts.

		/// <summary>Effective finger travel (panel px) that gives full steer deflection. Unchanged from the old joystick, so the feel at M is the same.</summary>
		public const float SteerFullRadius = 50f;

		/// <summary>How far the handle may travel from the base centre: (base 300 - handle 132) / 2, matches <c>.hud-joy-base</c> / <c>.hud-joy-handle</c>.</summary>
		public const float HandleMaxTravel = 84f;

		/// <summary>Matches <c>.hud-joy-base</c> width/height in Hud.uss; used to centre the base on the touch without a layout query.</summary>
		public const float BaseSize = 300f;

		/// <summary>Matches <c>.hud-joy-handle</c> width/height in Hud.uss.</summary>
		public const float HandleSize = 132f;

		// Indexed by SettingsService.JoystickSize (S / M / L): the same ratios as the Settings preview's 150 / 190 / 230 rings.
		private static readonly float[] m_S_SizeScales = { 0.8f, 1f, 1.2f };

		private readonly VisualElement m_zone;
		private readonly VisualElement m_base;
		private readonly VisualElement m_handle;
		private readonly VisualElement m_boostButton;
		private Vector2 m_origin;
		private bool m_enabled = true;
		private float m_scale = 1f;
		private float m_sensitivity = 1f;

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

			m_boostButton.AddManipulator(new PressedClassManipulator(playsSound: false));
			m_boostButton.RegisterCallback<PointerDownEvent>(OnBoostPointerDown);
			m_boostButton.RegisterCallback<PointerUpEvent>(OnBoostPointerUp);
			m_boostButton.RegisterCallback<PointerCancelEvent>(OnBoostPointerCancel);
		}

		/// <summary>
		/// Turns the controls off (run ended) or back on. Disabling releases any held finger, so steer and boost fall back
		/// to zero and the joystick base disappears; while off, new touches are ignored.
		/// </summary>
		public void SetEnabled(bool enabled)
		{
			if (m_enabled == enabled) return;
			m_enabled = enabled;
			if (enabled) return;

			for (var i = 0; i < PointerId.maxPointers; i++)
			{
				if (m_zone.HasPointerCapture(i)) m_zone.ReleasePointer(i);
				if (m_boostButton.HasPointerCapture(i)) m_boostButton.ReleasePointer(i);
			}

			ResetJoystick();
			BoostChanged?.Invoke(false);
		}

		/// <summary>Shows the joystick base at <paramref name="origin"/> (panel px) with the handle offset; used by the gallery preview.</summary>
		public void ShowPreview(Vector2 origin, Vector2 handleOffset)
		{
			ApplySettings();
			PlaceBase(origin);
			SetHandle(handleOffset);
		}

		public void HidePreview()
		{
			HideBase();
		}

		private void OnJoystickPointerDown(PointerDownEvent evt)
		{
			if (!m_enabled) return;
			m_zone.CapturePointer(evt.pointerId);

			ApplySettings();
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

		// Size and sensitivity are read from the settings at every touch-down (cheap), so they are never stale: the player
		// changes them in the menu's Settings screen, long after this view was built.
		private void ApplySettings()
		{
			m_scale = m_S_SizeScales[SettingsService.JoystickSize];
			m_sensitivity = SettingsService.Sensitivity;

			SetSize(m_base, BaseSize * m_scale);
			SetSize(m_handle, HandleSize * m_scale);
		}

		private static void SetSize(VisualElement element, float size)
		{
			element.style.width = size;
			element.style.height = size;
			element.style.borderTopLeftRadius = size * 0.5f;
			element.style.borderTopRightRadius = size * 0.5f;
			element.style.borderBottomLeftRadius = size * 0.5f;
			element.style.borderBottomRightRadius = size * 0.5f;
		}

		private void PlaceBase(Vector2 origin)
		{
			var halfSize = BaseSize * m_scale * 0.5f;
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
			// Sensitivity scales the finger offset, like the Settings preview: the handle and the steer both follow it.
			var delta = (pointerPosition - m_origin) * m_sensitivity;

			SetHandle(Vector2.ClampMagnitude(delta, HandleMaxTravel * m_scale));

			// UI-space Y grows downward; gameplay steer Y is "up positive" (matches PlaneController.Velocity.y).
			var fullRadius = SteerFullRadius * m_scale;
			var steer = Vector2.ClampMagnitude(delta, fullRadius) / fullRadius;
			SteerChanged?.Invoke(new Vector2(steer.x, -steer.y));
		}

		private void ResetJoystick()
		{
			HideBase();
			SteerChanged?.Invoke(Vector2.zero);
		}

		private void OnBoostPointerDown(PointerDownEvent evt)
		{
			if (!m_enabled) return;
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
