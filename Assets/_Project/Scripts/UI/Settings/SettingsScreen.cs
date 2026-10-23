using System;
using System.Globalization;
using Opoint8182.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Settings screen (mockup S06): AUDIO, FEEL and CONTROLS cards plus a live joystick preview. Pure UI over
	/// <see cref="SettingsService"/>: it reads the values when built and writes every change back, and the service
	/// persists them (flushed after each pointer release, so a slider's snapped final value is what is saved). Applying
	/// the values to gameplay is up to the subscribers added by the rest of the "Pause, settings and audio" milestone.
	/// The preview ring follows SIZE (S / M / L) and SIDE, and the handle you drag inside it travels
	/// <see cref="SettingsService.Sensitivity"/> times the finger distance, so it shows what the sliders would do.
	/// </summary>
	public class SettingsScreen
	{
		private const string k_RightClass = "settings-preview--right";
		private const string k_DraggingClass = "is-dragging";
		private const float k_HandleRatio = 0.45f;

		// Ring diameters for S / M / L in reference px (M is the mockup's 190).
		private static readonly float[] m_S_RingSizes = { 150f, 190f, 230f };
		private static readonly string[] m_S_SizeNames = { "S", "M", "L" };

		private readonly VisualElement m_root;
		private readonly ThemedSlider m_sound;
		private readonly Label m_soundValue;
		private readonly Toggle m_haptics;
		private readonly ThemedSlider m_shake;
		private readonly Label m_shakeValue;
		private readonly TabBar m_sideTabs;
		private readonly ThemedSlider m_size;
		private readonly Label m_sizeValue;
		private readonly ThemedSlider m_sensitivity;
		private readonly Label m_sensitivityValue;
		private readonly Toggle m_invert;
		private readonly VisualElement m_preview;
		private readonly VisualElement m_ring;
		private readonly VisualElement m_handle;

		private int m_dragPointerId = -1;
		private bool m_soundDragging;

		/// <summary>The back chevron was tapped (the owner pops the router).</summary>
		public event Action BackRequested;

		public SettingsScreen(VisualElement root)
		{
			m_root = root;

			m_sound = root.Q<ThemedSlider>("settings-sound");
			m_soundValue = root.Q<Label>("settings-sound-value");
			m_haptics = root.Q<Toggle>("settings-haptics");
			m_shake = root.Q<ThemedSlider>("settings-shake");
			m_shakeValue = root.Q<Label>("settings-shake-value");
			m_sideTabs = root.Q<TabBar>("settings-side");
			m_size = root.Q<ThemedSlider>("settings-size");
			m_sizeValue = root.Q<Label>("settings-size-value");
			m_sensitivity = root.Q<ThemedSlider>("settings-sensitivity");
			m_sensitivityValue = root.Q<Label>("settings-sensitivity-value");
			m_invert = root.Q<Toggle>("settings-invert");
			m_preview = root.Q<VisualElement>("settings-preview");
			m_ring = root.Q<VisualElement>("settings-stick-ring");
			m_handle = root.Q<VisualElement>("settings-stick-handle");

			ConfigureRanges();
			Refresh();

			root.Q<LipButton>("settings-back").clicked += () => BackRequested?.Invoke();

			m_sound.RegisterValueChangedCallback(evt => OnSoundChanged(evt.newValue));
			m_haptics.RegisterValueChangedCallback(evt => SettingsService.Haptics = evt.newValue);
			m_haptics.RegisterCallback<PointerDownEvent>(_ => Sfx.Play(SfxId.UiToggle), TrickleDown.TrickleDown);
			m_shake.RegisterValueChangedCallback(evt => OnShakeChanged(evt.newValue));
			m_sideTabs.changed += OnSideChanged;
			m_size.RegisterValueChangedCallback(evt => OnSizeChanged(evt.newValue));
			m_sensitivity.RegisterValueChangedCallback(evt => OnSensitivityChanged(evt.newValue));
			m_invert.RegisterValueChangedCallback(evt => SettingsService.InvertSteering = evt.newValue);
			m_invert.RegisterCallback<PointerDownEvent>(_ => Sfx.Play(SfxId.UiToggle), TrickleDown.TrickleDown);

			// Trickle-down sees the press and release before the slider handles them; the scheduled step runs after its
			// handler, so the saved value and the preview tick are the snapped one.
			m_sound.RegisterCallback<PointerDownEvent>(_ => m_soundDragging = true, TrickleDown.TrickleDown);
			root.RegisterCallback<PointerUpEvent>(_ => root.schedule.Execute(FlushAndPreview), TrickleDown.TrickleDown);

			m_ring.RegisterCallback<PointerDownEvent>(OnStickPointerDown);
			m_ring.RegisterCallback<PointerMoveEvent>(OnStickPointerMove);
			m_ring.RegisterCallback<PointerUpEvent>(OnStickPointerUp);
			m_ring.RegisterCallback<PointerCancelEvent>(OnStickPointerCancel);
		}

		private void FlushAndPreview()
		{
			SettingsService.Flush();
			if (!m_soundDragging) return;

			m_soundDragging = false;
			Sfx.Play(SfxId.UiSliderTick);
		}

		/// <summary>Reloads every control from <see cref="SettingsService"/> without raising change events.</summary>
		public void Refresh()
		{
			m_sound.SetValueWithoutNotify(SettingsService.Sound);
			m_soundValue.text = Mathf.RoundToInt(SettingsService.Sound).ToString(CultureInfo.InvariantCulture);

			m_haptics.SetValueWithoutNotify(SettingsService.Haptics);

			m_shake.SetValueWithoutNotify(SettingsService.ScreenShake);
			m_shakeValue.text = FormatShake(SettingsService.ScreenShake);

			m_sideTabs.SetSelectedIndexWithoutNotify((int)SettingsService.JoystickSide);

			m_size.SetValueWithoutNotify(SettingsService.JoystickSize);
			m_sizeValue.text = m_S_SizeNames[SettingsService.JoystickSize];

			m_sensitivity.SetValueWithoutNotify(SettingsService.Sensitivity);
			m_sensitivityValue.text = FormatSensitivity(SettingsService.Sensitivity);

			m_invert.SetValueWithoutNotify(SettingsService.InvertSteering);

			ApplyPreviewSide();
			ApplyPreviewSize();
		}

		private void ConfigureRanges()
		{
			m_sound.lowValue = SettingsService.SoundMin;
			m_sound.highValue = SettingsService.SoundMax;
			m_shake.lowValue = SettingsService.ScreenShakeMin;
			m_shake.highValue = SettingsService.ScreenShakeMax;
			m_size.lowValue = SettingsService.JoystickSizeMin;
			m_size.highValue = SettingsService.JoystickSizeMax;
			m_sensitivity.lowValue = SettingsService.SensitivityMin;
			m_sensitivity.highValue = SettingsService.SensitivityMax;

			// Knob and fill follow the pointer freely; on release they settle on the step. The readouts below show the
			// snapped value, i.e. what a release at that moment would set.
			m_sound.snapStep = 1f;
			m_shake.snapStep = 1f;
			m_size.snapStep = 1f;
			m_sensitivity.snapStep = 0.1f;
		}

		private void OnSoundChanged(float value)
		{
			var snapped = m_sound.Snap(value);
			SettingsService.Sound = snapped;
			m_soundValue.text = snapped.ToString("0", CultureInfo.InvariantCulture);
		}

		private void OnShakeChanged(float value)
		{
			var snapped = m_shake.Snap(value);
			SettingsService.ScreenShake = snapped;
			m_shakeValue.text = FormatShake(snapped);
		}

		private void OnSideChanged(int index)
		{
			SettingsService.JoystickSide = index == 0 ? JoystickSide.Left : JoystickSide.Right;
			ApplyPreviewSide();
		}

		// The size slider has three stops. While dragging the knob is free; the readout and preview show the nearest stop.
		private void OnSizeChanged(float value)
		{
			var step = Mathf.RoundToInt(m_size.Snap(value));

			SettingsService.JoystickSize = step;
			m_sizeValue.text = m_S_SizeNames[step];
			ApplyPreviewSize();
		}

		private void OnSensitivityChanged(float value)
		{
			var stepped = m_sensitivity.Snap(value);

			SettingsService.Sensitivity = stepped;
			m_sensitivityValue.text = FormatSensitivity(stepped);
		}

		private static string FormatShake(float value) => $"{Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)}%";

		private static string FormatSensitivity(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

		// The ring sits at the chosen end of the preview card, like the real joystick snaps to its corner.
		private void ApplyPreviewSide()
		{
			m_preview.EnableInClassList(k_RightClass, SettingsService.JoystickSide == JoystickSide.Right);
		}

		private void ApplyPreviewSize()
		{
			var ring = m_S_RingSizes[SettingsService.JoystickSize];
			var handle = ring * k_HandleRatio;

			m_ring.style.width = ring;
			m_ring.style.height = ring;
			m_handle.style.width = handle;
			m_handle.style.height = handle;
			ResetHandle();
		}

		private void OnStickPointerDown(PointerDownEvent evt)
		{
			if (m_dragPointerId >= 0) return;

			m_dragPointerId = evt.pointerId;
			m_ring.CapturePointer(evt.pointerId);
			m_handle.AddToClassList(k_DraggingClass);
			MoveHandle(evt.localPosition);
			evt.StopPropagation();
		}

		private void OnStickPointerMove(PointerMoveEvent evt)
		{
			if (evt.pointerId != m_dragPointerId || !m_ring.HasPointerCapture(evt.pointerId)) return;
			MoveHandle(evt.localPosition);
		}

		private void OnStickPointerUp(PointerUpEvent evt)
		{
			if (evt.pointerId != m_dragPointerId) return;
			EndDrag();
		}

		private void OnStickPointerCancel(PointerCancelEvent evt)
		{
			if (evt.pointerId != m_dragPointerId) return;
			EndDrag();
		}

		private void EndDrag()
		{
			if (m_ring.HasPointerCapture(m_dragPointerId)) m_ring.ReleasePointer(m_dragPointerId);
			m_dragPointerId = -1;
			m_handle.RemoveFromClassList(k_DraggingClass);
			ResetHandle();
		}

		// Handle offset = finger offset from the ring centre x sensitivity, kept inside the ring.
		private void MoveHandle(Vector2 localPosition)
		{
			var radius = m_ring.layout.width * 0.5f;
			var handleRadius = m_handle.layout.width * 0.5f;
			var centre = new Vector2(radius, radius);

			var offset = (localPosition - centre) * SettingsService.Sensitivity;
			var max = Mathf.Max(0f, radius - handleRadius - m_ring.resolvedStyle.borderLeftWidth);
			if (offset.magnitude > max) offset = offset.normalized * max;

			m_handle.style.translate = new StyleTranslate(new Translate(offset.x, offset.y));
		}

		private void ResetHandle()
		{
			m_handle.style.translate = StyleKeyword.Null;
		}
	}
}
