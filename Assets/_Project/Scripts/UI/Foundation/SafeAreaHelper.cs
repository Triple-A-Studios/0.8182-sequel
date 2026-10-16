using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Insets a full-screen "safe area" container by <c>Screen.safeArea</c> (notches, rounded corners, gesture bars)
	/// on top of a base padding, in the panel's reference-resolution space. Extracted from the main menu so every
	/// screen uses the same rule: edge-anchored content (top bars, joystick, boost, PLAY) sits inside the container,
	/// centred content ignores it. On desktop and in the Editor there is no inset, so it settles at the base padding.
	/// </summary>
	public static class SafeAreaHelper
	{
		public const float DefaultBasePadding = 24f;

		/// <summary>How often the safe area is compared with what is applied (a 180 degree rotation changes it without resizing anything).</summary>
		private const long k_PollMs = 250;

		/// <summary>Where the device safe area comes from. <see cref="Screen.safeArea"/> by default; a test can swap it.</summary>
		public static System.Func<Rect> SafeAreaProvider { get; set; } = () => Screen.safeArea;

		/// <summary>
		/// Applies the insets now and whenever the container's geometry changes (e.g. device rotation).
		/// The container is expected to be absolutely positioned to fill the panel, so setting its padding never
		/// changes its own rect and cannot feed back into the geometry callback.
		/// </summary>
		public static void Apply(VisualElement safeArea, float basePadding = DefaultBasePadding)
		{
			// Safe to call again on the same element (UI reload callbacks can fire more than once): only the
			// first call registers the geometry callback, later calls just update the base padding.
			if (safeArea.userData is Binding existing)
			{
				existing.BasePadding = basePadding;
			}
			else
			{
				var binding = new Binding { BasePadding = basePadding };
				safeArea.userData = binding;
				safeArea.RegisterCallback<GeometryChangedEvent>(evt => ApplyInsets(safeArea, evt.newRect.size, binding.BasePadding));

				// Turning a phone from landscape-left to landscape-right moves the notch to the other side but keeps the
				// screen size, so no geometry event fires: poll for it.
				safeArea.schedule.Execute(() =>
				{
					if (safeArea.layout.width <= 0f || safeArea.layout.height <= 0f) return;
					if (SafeAreaProvider() == binding.AppliedSafeArea && Screen.width == binding.AppliedWidth && Screen.height == binding.AppliedHeight) return;

					ApplyInsets(safeArea, safeArea.layout.size, binding.BasePadding);
				}).Every(k_PollMs);
			}

			if (safeArea.layout.width > 0f && safeArea.layout.height > 0f)
			{
				ApplyInsets(safeArea, safeArea.layout.size, basePadding);
			}
		}

		private sealed class Binding
		{
			public float BasePadding;
			public Rect AppliedSafeArea;
			public int AppliedWidth;
			public int AppliedHeight;
		}

		/// <summary>Pure conversion from a device-space safe area to panel-space padding (left, top, right, bottom).</summary>
		public static Vector4 ComputePadding(Rect deviceSafeArea, Vector2 deviceSize, Vector2 panelSize, float basePadding)
		{
			if (deviceSize.x <= 0f || deviceSize.y <= 0f) return new Vector4(basePadding, basePadding, basePadding, basePadding);

			var left = deviceSafeArea.xMin / deviceSize.x * panelSize.x;
			var right = (deviceSize.x - deviceSafeArea.xMax) / deviceSize.x * panelSize.x;
			var top = (deviceSize.y - deviceSafeArea.yMax) / deviceSize.y * panelSize.y;
			var bottom = deviceSafeArea.yMin / deviceSize.y * panelSize.y;

			return new Vector4(basePadding + left, basePadding + top, basePadding + right, basePadding + bottom);
		}

		private static void ApplyInsets(VisualElement safeArea, Vector2 panelSize, float basePadding)
		{
			if (panelSize.x <= 0f || panelSize.y <= 0f) return;

			var deviceSafeArea = SafeAreaProvider();
			if (safeArea.userData is Binding binding)
			{
				binding.AppliedSafeArea = deviceSafeArea;
				binding.AppliedWidth = Screen.width;
				binding.AppliedHeight = Screen.height;
			}

			var padding = ComputePadding(deviceSafeArea, new Vector2(Screen.width, Screen.height), panelSize, basePadding);
			safeArea.style.paddingLeft = padding.x;
			safeArea.style.paddingTop = padding.y;
			safeArea.style.paddingRight = padding.z;
			safeArea.style.paddingBottom = padding.w;
		}
	}
}
