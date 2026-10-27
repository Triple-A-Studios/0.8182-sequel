using System;
using Opoint8182.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Pause popup (mockup S04) and the 3-2-1 resume countdown, built on <see cref="ScreenRouter"/>'s popup layer. The
	/// three sliders / toggle are bound to the persisted <see cref="SettingsService"/>; the buttons just raise events.
	/// Functional pause (timeScale, restart / quit flow) is a later pass of the "Pause, settings and audio" milestone,
	/// which can hook these events. The countdown runs on UITK's scheduler (unscaled time), so it keeps
	/// ticking while the game would be paused.
	/// </summary>
	public class HudPausePopup
	{
		private const int k_CountdownFrom = 3;
		private const long k_CountdownStepMs = 1000;
		private const long k_AnimTickMs = 16;
		private const float k_NumberPopSeconds = 0.28f;
		private const float k_NumberPopFromScale = 1.4f;

		private readonly ScreenRouter m_router;
		private readonly VisualElement m_popup;
		private readonly Label m_modeLine;
		private readonly Label m_quitNote;
		private readonly VisualElement m_leavePopup;
		private readonly VisualElement m_restartPopup;
		private readonly Label m_restartBody;
		private readonly LipButton m_restartButton;
		private bool m_isPractice;
		private int m_attemptNumber = 1;
		private int m_attemptsMax = 3;
		private bool m_soundDragging;
		private readonly ThemedSlider m_sound;
		private readonly Label m_soundValue;
		private readonly ThemedSlider m_shake;
		private readonly Label m_shakeValue;
		private readonly Toggle m_haptics;
		private readonly VisualElement m_countdown;
		private readonly Label m_countdownNumber;
		private readonly IVisualElementScheduledItem m_countdownTick;
		private int m_countdownValue;
		private float m_countdownStart;
		private float m_numberStartTime;

		/// <summary>RESUME tapped (the popup closes and the countdown starts).</summary>
		public event Action ResumeRequested;

		/// <summary>RESTART RUN tapped (the popup closes).</summary>
		public event Action RestartRequested;

		/// <summary>QUIT TO MENU tapped (the popup closes).</summary>
		public event Action QuitRequested;

		/// <summary>The resume countdown reached zero: this is where real pause hands control back to the player.</summary>
		public event Action CountdownFinished;

		public bool IsOpen => m_router.OpenPopup == m_popup;

		/// <summary>The leave-run confirm is showing (competitive runs only; it replaces the pause popup).</summary>
		public bool IsLeaveConfirmOpen => m_router.OpenPopup == m_leavePopup;

		/// <summary>The restart confirm is showing (competitive runs only; it replaces the pause popup).</summary>
		public bool IsRestartConfirmOpen => m_router.OpenPopup == m_restartPopup;

		public bool IsCountingDown => m_countdown.resolvedStyle.display == DisplayStyle.Flex;

		public HudPausePopup(VisualElement root)
		{
			m_router = new ScreenRouter(root.Q<VisualElement>("hud-popup-layer"));
			m_popup = root.Q<VisualElement>("hud-pause-popup");
			m_modeLine = root.Q<Label>("pause-mode-line");
			m_quitNote = root.Q<Label>("pause-quit-note");
			m_leavePopup = root.Q<VisualElement>("hud-leave-popup");
			m_restartPopup = root.Q<VisualElement>("hud-restart-popup");
			m_restartBody = root.Q<Label>("restart-body");
			m_restartButton = root.Q<LipButton>("pause-restart");

			// Both cards share one popup layer and nothing else hides them: whichever the router is not showing must start
			// (and stay) display:none, or opening the layer for one shows the other under it.
			m_popup.style.display = DisplayStyle.None;
			m_leavePopup.style.display = DisplayStyle.None;
			m_restartPopup.style.display = DisplayStyle.None;
			m_countdown = root.Q<VisualElement>("hud-countdown");
			m_countdownNumber = root.Q<Label>("hud-countdown-number");

			// The same persisted values the Settings screen edits, so the two never disagree.
			m_sound = root.Q<ThemedSlider>("pause-sound");
			m_soundValue = root.Q<Label>("pause-sound-value");
			m_shake = root.Q<ThemedSlider>("pause-shake");
			m_shakeValue = root.Q<Label>("pause-shake-value");
			m_haptics = root.Q<Toggle>("pause-haptics");

			m_sound.snapStep = 1f;
			m_shake.snapStep = 1f;
			RefreshFromSettings();

			m_sound.RegisterValueChangedCallback(evt => OnSliderChanged(m_sound, m_soundValue, evt.newValue, value => SettingsService.Sound = value));
			m_shake.RegisterValueChangedCallback(evt => OnSliderChanged(m_shake, m_shakeValue, evt.newValue, value => SettingsService.ScreenShake = value));
			m_haptics.RegisterValueChangedCallback(OnHapticsChanged);
			m_haptics.RegisterCallback<PointerDownEvent>(_ => OnTogglePressed(), TrickleDown.TrickleDown);

			// Trickle-down sees the press and release before the slider handles them; the scheduled step runs after its
			// handler, so the saved value and the preview tick are the snapped one.
			m_sound.RegisterCallback<PointerDownEvent>(_ => m_soundDragging = true, TrickleDown.TrickleDown);
			m_popup.RegisterCallback<PointerUpEvent>(_ => m_popup.schedule.Execute(FlushAndPreview), TrickleDown.TrickleDown);

			SetPractice(false);

			root.Q<LipButton>("pause-resume").clicked += OnResumeClicked;
			root.Q<LipButton>("pause-restart").clicked += OnRestartClicked;
			root.Q<LipButton>("pause-quit").clicked += OnQuitClicked;
			root.Q<LipButton>("leave-yes").clicked += OnLeaveConfirmed;
			root.Q<LipButton>("leave-no").clicked += CancelLeaveConfirm;
			root.Q<LipButton>("restart-yes").clicked += OnRestartConfirmed;
			root.Q<LipButton>("restart-no").clicked += CancelRestartConfirm;

			m_countdown.style.display = DisplayStyle.None;
			m_countdownTick = m_countdown.schedule.Execute(OnCountdownTick).Every(k_AnimTickMs);
			m_countdownTick.Pause();
		}

		/// <summary>Opens the popup. A backdrop tap does not dismiss it: only the three buttons leave the pause state.</summary>
		public void Show()
		{
			CancelCountdown();
			RefreshFromSettings();
			m_router.ShowPopup(m_popup, dismissOnBackdrop: false);
		}

		/// <summary>
		/// The run's mode: a practice run has no attempt to lose, so QUIT TO MENU goes straight through and the quit warning
		/// is hidden; a competitive run asks first (<see cref="ShowLeaveConfirm"/>). Also picks the mode line.
		/// </summary>
		public void SetPractice(bool isPractice) => SetRun(isPractice, 2, 3);

		/// <summary>
		/// <see cref="SetPractice"/> with the real attempt: "COMPETITIVE · ATTEMPT n/3", the quit warning for attempt n, and
		/// the restart confirm (which names attempt n + 1).
		/// </summary>
		public void SetRun(bool isPractice, int attemptNumber, int attemptsMax)
		{
			m_isPractice = isPractice;
			m_attemptNumber = Mathf.Max(1, attemptNumber);
			m_attemptsMax = attemptsMax;
			m_modeLine.text = isPractice ? HudPlaceholderData.PausePracticeModeLine : DailyPlayText.PauseModeLine(m_attemptNumber, attemptsMax);
			m_quitNote.text = DailyPlayText.PauseQuitNote(m_attemptNumber, attemptsMax);
			m_quitNote.style.display = isPractice ? DisplayStyle.None : DisplayStyle.Flex;
			m_restartBody.text = DailyPlayText.RestartBody(m_attemptNumber + 1, attemptsMax);
		}

		/// <summary>RESTART RUN is greyed when starting over is not possible (no attempts left, or no practice time left).</summary>
		public void SetRestartEnabled(bool enabled)
		{
			m_restartButton.SetEnabled(enabled);
		}

		/// <summary>Opens the leave-run confirm in place of the pause popup.</summary>
		public void ShowLeaveConfirm()
		{
			CancelCountdown();
			m_router.ShowPopup(m_leavePopup, dismissOnBackdrop: false);
		}

		/// <summary>Opens the restart confirm in place of the pause popup.</summary>
		public void ShowRestartConfirm()
		{
			CancelCountdown();
			m_router.ShowPopup(m_restartPopup, dismissOnBackdrop: false);
		}

		/// <summary>BACK on the restart confirm (or the back key): returns to the pause popup.</summary>
		public void CancelRestartConfirm()
		{
			if (IsRestartConfirmOpen) Show();
		}

		/// <summary>BACK on the leave-run confirm (or the back key): returns to the pause popup.</summary>
		public void CancelLeaveConfirm()
		{
			if (IsLeaveConfirmOpen) Show();
		}

		/// <summary>Same as tapping RESUME (popup closes, countdown starts): the back key uses it.</summary>
		public void RequestResume() => OnResumeClicked();

		public void Hide()
		{
			m_router.ClosePopup();
		}

		/// <summary>Runs the 3-2-1 overlay, then raises <see cref="CountdownFinished"/>. Restarts if already running.</summary>
		public void StartCountdown()
		{
			Hide();
			m_countdownValue = 0;
			m_countdownStart = Time.unscaledTime;
			m_countdown.style.display = DisplayStyle.Flex;
			m_countdownTick.Pause();
			m_countdownTick.Resume();
			OnCountdownTick();
		}

		/// <summary>Stops the countdown and hides its overlay without raising <see cref="CountdownFinished"/>.</summary>
		public void CancelCountdown()
		{
			m_countdownTick.Pause();
			m_countdown.style.display = DisplayStyle.None;
		}

		private void OnResumeClicked()
		{
			StartCountdown();
			ResumeRequested?.Invoke();
		}

		// A competitive restart gives the current attempt up and uses the next one, so it asks first; practice restarts straight away.
		private void OnRestartClicked()
		{
			if (!m_isPractice)
			{
				ShowRestartConfirm();
				return;
			}

			Hide();
			RestartRequested?.Invoke();
		}

		private void OnRestartConfirmed()
		{
			Hide();
			RestartRequested?.Invoke();
		}

		private void OnQuitClicked()
		{
			if (!m_isPractice)
			{
				ShowLeaveConfirm();
				return;
			}

			Hide();
			QuitRequested?.Invoke();
		}

		private void OnLeaveConfirmed()
		{
			Hide();
			QuitRequested?.Invoke();
		}

		// One scheduled item drives both the 3-2-1 and the pop animation from elapsed unscaled time. The number is derived
		// from the clock rather than counted by a 1 s repeating item: a repeating item that was created paused fires at once
		// the first time it is resumed (zero first delay), which dropped the first countdown of a run straight to 2.
		private void OnCountdownTick()
		{
			var secondsElapsed = Mathf.FloorToInt((Time.unscaledTime - m_countdownStart) * 1000f / k_CountdownStepMs);
			var remaining = k_CountdownFrom - secondsElapsed;
			if (remaining <= 0)
			{
				CancelCountdown();
				Sfx.Play(SfxId.CountdownGo);
				CountdownFinished?.Invoke();
				return;
			}

			if (remaining != m_countdownValue)
			{
				m_countdownValue = remaining;
				m_countdownNumber.text = remaining.ToString();
				Sfx.Play(SfxId.CountdownTick);
				m_numberStartTime = Time.unscaledTime;
			}

			AnimateNumber();
		}

		// Each number pops in from 1.4x with a quick fade-up, then rests.
		private void AnimateNumber()
		{
			var t = Mathf.Clamp01((Time.unscaledTime - m_numberStartTime) / k_NumberPopSeconds);
			var eased = 1f - (1f - t) * (1f - t);
			var scale = Mathf.Lerp(k_NumberPopFromScale, 1f, eased);

			m_countdownNumber.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
			m_countdownNumber.style.opacity = Mathf.Lerp(0.2f, 1f, eased);
		}

		/// <summary>Reloads the three controls from <see cref="SettingsService"/>: the Settings screen lives in another scene and may have changed them since this popup was built.</summary>
		private void RefreshFromSettings()
		{
			m_sound.SetValueWithoutNotify(SettingsService.Sound);
			m_soundValue.text = Mathf.RoundToInt(SettingsService.Sound).ToString();
			m_shake.SetValueWithoutNotify(SettingsService.ScreenShake);
			m_shakeValue.text = Mathf.RoundToInt(SettingsService.ScreenShake).ToString();
			m_haptics.SetValueWithoutNotify(SettingsService.Haptics);
		}

		// Switching haptics on buzzes once so the player feels what they just enabled (the setting is already stored, so it passes the gate).
		private void OnHapticsChanged(ChangeEvent<bool> evt)
		{
			SettingsService.Haptics = evt.newValue;
			if (evt.newValue) Haptics.Play(HapticTier.Light);
		}

		private static void OnTogglePressed()
		{
			Sfx.Play(SfxId.UiToggle);
			Haptics.Play(HapticTier.Light);
		}

		private void FlushAndPreview()
		{
			SettingsService.Flush();
			if (!m_soundDragging) return;

			m_soundDragging = false;
			Sfx.Play(SfxId.UiSliderTick);
		}

		private static void OnSliderChanged(ThemedSlider slider, Label valueLabel, float newValue, Action<float> store)
		{
			// The knob follows the pointer freely and settles on release; the readout is the value that would be set.
			var snapped = slider.Snap(newValue);
			valueLabel.text = snapped.ToString("0");
			store(snapped);
		}
	}
}
