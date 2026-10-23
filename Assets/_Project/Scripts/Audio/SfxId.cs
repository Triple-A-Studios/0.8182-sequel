namespace Opoint8182.Audio
{
	/// <summary>
	/// Every sound effect the game plays. The name is also the clip file name (minus a trailing _1 / _2 variation suffix)
	/// the SfxLibrary builder looks for, so adding an id means adding a clip with that name.
	/// </summary>
	public enum SfxId
	{
		UiClick,
		UiToggle,
		UiSliderTick,
		CountdownTick,
		CountdownGo,

		CrashPlain,
		CrashMistimed,
		CrashPerfect,
		Hit,
		Heal,
		Pickup,
		ComboTick,

		BoostStart,
		BoostLoop,

		WarnCeiling,
		WarnLateral,
		WarnGround,
		WarnLowFuel,
		WarnLowHealth,

		RunEndWreck,
		RunEndFuel,
		RunEndFlyOff,
		RunEndGround,
		GameOver,
	}
}
