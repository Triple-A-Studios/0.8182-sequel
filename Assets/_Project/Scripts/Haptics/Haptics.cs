using System;
using System.Collections.Generic;
using UnityEngine;

namespace Opoint8182
{
	/// <summary>
	/// Phone vibration: <c>Haptics.Play(HapticTier.Heavy)</c>. Safe to call anywhere. Does nothing when the Haptics setting
	/// (<see cref="SettingsService.Haptics"/>) is off, and only vibrates on an Android player (the Editor, WebGL and every
	/// other platform are a no-op). Each <see cref="HapticTier"/> maps to a <see cref="HapticProfile"/> (durations and
	/// amplitudes per segment, from <c>Resources/HapticProfiles</c>, with built-in defaults when the asset is missing) played
	/// as an Android waveform. While a pattern is running, an equal or weaker tier is skipped (a UI tap must not cut off the
	/// crash rhythm); a stronger one replaces it. <see cref="Requested"/> fires for every haptic that passes the setting and
	/// that rule, on every platform, so the wiring can be tested without a phone.
	/// </summary>
	public static class Haptics
	{
		private const string k_ProfilesPath = "HapticProfiles";
		private const float k_MinBusySeconds = 0.04f;

		private static readonly Dictionary<HapticTier, HapticProfile> m_S_Profiles = new();

		private static bool m_s_loaded;
		private static float m_s_busyUntil = float.NegativeInfinity;
		private static HapticTier m_s_busyTier;

		/// <summary>Raised for each haptic that was not suppressed (setting off, a stronger pattern still running).</summary>
		public static event Action<HapticTier> Requested;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics.
			m_s_loaded = false;
			m_s_busyUntil = float.NegativeInfinity;
			m_S_Profiles.Clear();
			Requested = null;
		}

		public static void Play(HapticTier tier)
		{
			if (!SettingsService.Haptics) return;

			var now = Time.unscaledTime;
			if (now < m_s_busyUntil && tier <= m_s_busyTier) return;

			var profile = GetProfile(tier);
			m_s_busyTier = tier;
			m_s_busyUntil = now + Mathf.Max(profile.TotalMs / 1000f, k_MinBusySeconds);

			Requested?.Invoke(tier);
			if (profile.durationsMs.Length > 0) Vibrate(profile);
		}

		/// <summary>The (sanitized) pattern for a tier: from the asset, else the built-in default.</summary>
		public static HapticProfile GetProfile(HapticTier tier)
		{
			EnsureLoaded();
			return m_S_Profiles[tier];
		}

		private static void EnsureLoaded()
		{
			if (m_s_loaded) return;
			m_s_loaded = true;

			var asset = Resources.Load<HapticProfiles>(k_ProfilesPath);
			if (asset != null)
			{
				foreach (var profile in asset.profiles)
				{
					if (profile != null) m_S_Profiles[profile.tier] = profile;
				}
			}

			// A missing asset or a missing tier falls back to the default, so haptics work before the asset exists.
			foreach (HapticTier tier in Enum.GetValues(typeof(HapticTier)))
			{
				if (!m_S_Profiles.ContainsKey(tier)) m_S_Profiles[tier] = HapticProfile.CreateDefault(tier);
				m_S_Profiles[tier].Sanitize();
			}
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		private const int k_DefaultAmplitude = -1; // VibrationEffect.DEFAULT_AMPLITUDE

		private static AndroidJavaObject m_s_vibrator;
		private static AndroidJavaClass m_s_effectClass;
		private static bool m_s_amplitudeControl;
		private static bool m_s_failed;

		private static void Vibrate(HapticProfile profile)
		{
			if (m_s_failed) return;

			try
			{
				if (m_s_vibrator == null && !Connect()) return;

				var count = profile.durationsMs.Length;
				var timings = new long[count];
				var amplitudes = new int[count];
				for (var i = 0; i < count; i++)
				{
					timings[i] = profile.durationsMs[i];

					// Without amplitude control, "on" segments use the default strength; gaps stay gaps.
					amplitudes[i] = m_s_amplitudeControl || profile.amplitudes[i] == 0 ? profile.amplitudes[i] : k_DefaultAmplitude;
				}

				using var effect = m_s_effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
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
		private static void Vibrate(HapticProfile profile)
		{
		}
#endif
	}
}
