using System;
using UnityEngine;

namespace Opoint8182
{
	public enum HapticTier
	{
		Light,
		Medium,
		Heavy,
	}

	/// <summary>
	/// Phone vibration: <c>Haptics.Play(HapticTier.Heavy)</c>. Safe to call anywhere. Does nothing when the Haptics setting
	/// (<see cref="SettingsService.Haptics"/>) is off, and only vibrates on an Android player (the Editor, WebGL and every
	/// other platform are a no-op), through <c>VibrationEffect.createOneShot</c> (duration and amplitude, so the three tiers
	/// feel different). A short cooldown stops a burst of events (a crash plus its hit) from stacking buzzes; a stronger tier
	/// still cuts through it. <see cref="Requested"/> fires for every haptic that passes the setting and the cooldown, on
	/// every platform, so the wiring can be tested without a phone.
	/// </summary>
	public static class Haptics
	{
		private const float k_CooldownSeconds = 0.04f;

		private static float m_s_lastTime = float.NegativeInfinity;
		private static HapticTier m_s_lastTier;

		/// <summary>Raised for each haptic that was not suppressed (setting off, cooldown).</summary>
		public static event Action<HapticTier> Requested;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics.
			m_s_lastTime = float.NegativeInfinity;
			Requested = null;
		}

		public static void Play(HapticTier tier)
		{
			if (!SettingsService.Haptics) return;

			var now = Time.unscaledTime;
			if (now - m_s_lastTime < k_CooldownSeconds && tier <= m_s_lastTier) return;

			m_s_lastTime = now;
			m_s_lastTier = tier;

			Requested?.Invoke(tier);
			Vibrate(tier);
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		// Duration in ms and amplitude 1-255 per tier (index = HapticTier).
		private static readonly long[] m_S_Durations = { 15L, 30L, 55L };
		private static readonly int[] m_S_Amplitudes = { 80, 160, 255 };

		private static AndroidJavaObject m_s_vibrator;
		private static AndroidJavaClass m_s_effectClass;
		private static bool m_s_amplitudeControl;
		private static bool m_s_failed;

		private static void Vibrate(HapticTier tier)
		{
			if (m_s_failed) return;

			try
			{
				if (m_s_vibrator == null && !Connect()) return;

				var amplitude = m_s_amplitudeControl ? m_S_Amplitudes[(int)tier] : -1; // -1 = VibrationEffect.DEFAULT_AMPLITUDE
				using var effect = m_s_effectClass.CallStatic<AndroidJavaObject>("createOneShot", m_S_Durations[(int)tier], amplitude);
				m_s_vibrator.Call("vibrate", effect);
			}
			catch (Exception exception)
			{
				// Never let a vibration problem reach gameplay: log once and stay quiet.
				m_s_failed = true;
				Debug.LogWarning($"[Haptics] Vibration disabled: {exception.Message}");
			}
		}

		private static bool Connect()
		{
			using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");

			m_s_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
			if (m_s_vibrator == null || !m_s_vibrator.Call<bool>("hasVibrator"))
			{
				m_s_failed = true;
				return false;
			}

			m_s_amplitudeControl = m_s_vibrator.Call<bool>("hasAmplitudeControl");
			m_s_effectClass = new AndroidJavaClass("android.os.VibrationEffect");
			return true;
		}
#else
		// Editor, WebGL, desktop: nothing to vibrate.
		private static void Vibrate(HapticTier tier)
		{
		}
#endif
	}
}
