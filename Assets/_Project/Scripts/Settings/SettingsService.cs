using System;
using UnityEngine;

namespace Opoint8182
{
	public enum JoystickSide
	{
		Left,
		Right,
	}

	/// <summary>
	/// The player's settings (Settings screen S06 and the pause popup S04), persisted locally in <see cref="PlayerPrefs"/>
	/// (no cloud, per the design doc). Static, so it needs no scene object and works from Bootstrap or a standalone scene.
	/// Values load lazily on first access and are clamped, so a corrupt or old pref cannot produce a bad state. A setter
	/// only updates memory, marks the store dirty and raises <see cref="Changed"/>: sliders fire continuously while
	/// dragging, so the disk write is <see cref="Flush"/>, which the UI schedules after a pointer release and which also
	/// runs when the app loses focus or quits (an Android kill and a closed WebGL tab both pass through one of those).
	/// Applying the values (audio, shake, haptics, joystick) is the job of whoever subscribes or reads them.
	/// </summary>
	public static class SettingsService
	{
		public const float SoundMin = 0f;
		public const float SoundMax = 100f;

		/// <summary>The S06 hint reads "0% for motion-sensitive players, 150% for maximum chaos".</summary>
		public const float ScreenShakeMin = 0f;
		public const float ScreenShakeMax = 150f;

		/// <summary>Joystick size steps: 0 = S, 1 = M, 2 = L.</summary>
		public const int JoystickSizeMin = 0;
		public const int JoystickSizeMax = 2;

		public const float SensitivityMin = 0.5f;
		public const float SensitivityMax = 1.4f;

		private const string k_SoundKey = "settings.sound";
		private const string k_HapticsKey = "settings.haptics";
		private const string k_ShakeKey = "settings.shake";
		private const string k_JoystickSideKey = "settings.joystick_side";
		private const string k_JoystickSizeKey = "settings.joystick_size";
		private const string k_SensitivityKey = "settings.sensitivity";
		private const string k_InvertKey = "settings.invert";

		// Defaults are the mockup's.
		private const float k_DefaultSound = 80f;
		private const bool k_DefaultHaptics = true;
		private const float k_DefaultShake = 120f;
		private const JoystickSide k_DefaultJoystickSide = JoystickSide.Left;
		private const int k_DefaultJoystickSize = 1;
		private const float k_DefaultSensitivity = 0.9f;
		private const bool k_DefaultInvert = false;

		private static bool m_s_loaded;
		private static bool m_s_dirty;

		private static float m_s_sound;
		private static bool m_s_haptics;
		private static float m_s_screenShake;
		private static JoystickSide m_s_joystickSide;
		private static int m_s_joystickSize;
		private static float m_s_sensitivity;
		private static bool m_s_invertSteering;

		/// <summary>Raised after any value actually changed (not for a set to the current value).</summary>
		public static event Action Changed;

		/// <summary>0-100.</summary>
		public static float Sound
		{
			get { EnsureLoaded(); return m_s_sound; }
			set
			{
				EnsureLoaded();
				var clamped = Mathf.Clamp(value, SoundMin, SoundMax);
				if (Mathf.Approximately(clamped, m_s_sound)) return;

				m_s_sound = clamped;
				MarkChanged();
			}
		}

		public static bool Haptics
		{
			get { EnsureLoaded(); return m_s_haptics; }
			set
			{
				EnsureLoaded();
				if (value == m_s_haptics) return;

				m_s_haptics = value;
				MarkChanged();
			}
		}

		/// <summary>0-150 (percent).</summary>
		public static float ScreenShake
		{
			get { EnsureLoaded(); return m_s_screenShake; }
			set
			{
				EnsureLoaded();
				var clamped = Mathf.Clamp(value, ScreenShakeMin, ScreenShakeMax);
				if (Mathf.Approximately(clamped, m_s_screenShake)) return;

				m_s_screenShake = clamped;
				MarkChanged();
			}
		}

		public static JoystickSide JoystickSide
		{
			get { EnsureLoaded(); return m_s_joystickSide; }
			set
			{
				EnsureLoaded();
				if (value == m_s_joystickSide) return;

				m_s_joystickSide = value;
				MarkChanged();
			}
		}

		/// <summary>Step index, see <see cref="JoystickSizeMin"/>.</summary>
		public static int JoystickSize
		{
			get { EnsureLoaded(); return m_s_joystickSize; }
			set
			{
				EnsureLoaded();
				var clamped = Mathf.Clamp(value, JoystickSizeMin, JoystickSizeMax);
				if (clamped == m_s_joystickSize) return;

				m_s_joystickSize = clamped;
				MarkChanged();
			}
		}

		public static float Sensitivity
		{
			get { EnsureLoaded(); return m_s_sensitivity; }
			set
			{
				EnsureLoaded();
				var clamped = Mathf.Clamp(value, SensitivityMin, SensitivityMax);
				if (Mathf.Approximately(clamped, m_s_sensitivity)) return;

				m_s_sensitivity = clamped;
				MarkChanged();
			}
		}

		public static bool InvertSteering
		{
			get { EnsureLoaded(); return m_s_invertSteering; }
			set
			{
				EnsureLoaded();
				if (value == m_s_invertSteering) return;

				m_s_invertSteering = value;
				MarkChanged();
			}
		}

		/// <summary>Writes pending changes to <see cref="PlayerPrefs"/> and saves. No-op when nothing changed.</summary>
		public static void Flush()
		{
			if (!m_s_dirty) return;

			PlayerPrefs.SetFloat(k_SoundKey, m_s_sound);
			PlayerPrefs.SetInt(k_HapticsKey, m_s_haptics ? 1 : 0);
			PlayerPrefs.SetFloat(k_ShakeKey, m_s_screenShake);
			PlayerPrefs.SetInt(k_JoystickSideKey, (int)m_s_joystickSide);
			PlayerPrefs.SetInt(k_JoystickSizeKey, m_s_joystickSize);
			PlayerPrefs.SetFloat(k_SensitivityKey, m_s_sensitivity);
			PlayerPrefs.SetInt(k_InvertKey, m_s_invertSteering ? 1 : 0);
			PlayerPrefs.Save();

			m_s_dirty = false;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			// Editor Play with domain reload off keeps statics: drop the cache and re-hook without stacking handlers.
			m_s_loaded = false;
			m_s_dirty = false;
			Changed = null;

			Application.focusChanged -= OnFocusChanged;
			Application.focusChanged += OnFocusChanged;
			Application.quitting -= Flush;
			Application.quitting += Flush;
		}

		private static void OnFocusChanged(bool hasFocus)
		{
			if (!hasFocus) Flush();
		}

		private static void MarkChanged()
		{
			m_s_dirty = true;
			Changed?.Invoke();
		}

		private static void EnsureLoaded()
		{
			if (m_s_loaded) return;
			m_s_loaded = true;

			m_s_sound = Mathf.Clamp(PlayerPrefs.GetFloat(k_SoundKey, k_DefaultSound), SoundMin, SoundMax);
			m_s_haptics = PlayerPrefs.GetInt(k_HapticsKey, k_DefaultHaptics ? 1 : 0) != 0;
			m_s_screenShake = Mathf.Clamp(PlayerPrefs.GetFloat(k_ShakeKey, k_DefaultShake), ScreenShakeMin, ScreenShakeMax);

			var side = PlayerPrefs.GetInt(k_JoystickSideKey, (int)k_DefaultJoystickSide);
			m_s_joystickSide = Enum.IsDefined(typeof(JoystickSide), side) ? (JoystickSide)side : k_DefaultJoystickSide;

			m_s_joystickSize = Mathf.Clamp(PlayerPrefs.GetInt(k_JoystickSizeKey, k_DefaultJoystickSize), JoystickSizeMin, JoystickSizeMax);
			m_s_sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(k_SensitivityKey, k_DefaultSensitivity), SensitivityMin, SensitivityMax);
			m_s_invertSteering = PlayerPrefs.GetInt(k_InvertKey, k_DefaultInvert ? 1 : 0) != 0;
		}
	}
}
