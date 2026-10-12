namespace Opoint8182.UI
{
	public enum JoystickSide
	{
		Left,
		Right,
	}

	/// <summary>
	/// The player's settings as the UI shows them (Settings screen S06 and the pause popup S04). Session-only static
	/// state: nothing persists it and nothing in gameplay reads it. Applying the values (audio mixer, haptics,
	/// <c>CameraJuice</c> shake multiplier, touch controls) and saving them is the later "Pause, settings and audio"
	/// milestone (backlog #25), which replaces this class's backing store. The defaults are the mockup's.
	/// </summary>
	public static class UiSettingsState
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

		/// <summary>0-100.</summary>
		public static float Sound { get; set; } = 80f;

		public static bool Haptics { get; set; } = true;

		/// <summary>0-150 (percent).</summary>
		public static float ScreenShake { get; set; } = 120f;

		public static JoystickSide JoystickSide { get; set; } = JoystickSide.Left;

		/// <summary>Step index, see <see cref="JoystickSizeMin"/>.</summary>
		public static int JoystickSize { get; set; } = 1;

		public static float Sensitivity { get; set; } = 0.9f;

		public static bool InvertSteering { get; set; }
	}
}
