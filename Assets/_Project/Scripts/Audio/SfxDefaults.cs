using UnityEngine;

namespace Opoint8182.Audio
{
	/// <summary>
	/// The starting tuning for every <see cref="SfxId"/>. The SfxLibrary builder copies it into the asset for ids that have
	/// no entry yet (and for "Reset tuning"); after that the asset is the source of truth and can be tuned in the Inspector.
	/// </summary>
	public static class SfxDefaults
	{
		// Priority bands: ambient loops 10, UI 20-30, small feedback 25-40, hits 50, warnings 55-75, crashes 60-70,
		// run end 90, game over 95.
		public static SfxEntry Create(SfxId id)
		{
			var entry = new SfxEntry { id = id };

			switch (id)
			{
				case SfxId.UiClick:
					entry.priority = 20; entry.cooldown = 0.03f; entry.maxConcurrent = 2; entry.ignorePause = true;
					entry.pitchRange = new Vector2(0.97f, 1.03f);
					break;
				case SfxId.UiToggle:
					entry.priority = 20; entry.cooldown = 0.05f; entry.maxConcurrent = 2; entry.ignorePause = true;
					break;
				case SfxId.UiSliderTick:
					entry.priority = 20; entry.cooldown = 0.05f; entry.maxConcurrent = 1; entry.ignorePause = true;
					break;
				case SfxId.CountdownTick:
					entry.priority = 30; entry.maxConcurrent = 1; entry.ignorePause = true;
					break;
				case SfxId.CountdownGo:
					entry.priority = 30; entry.maxConcurrent = 1; entry.ignorePause = true;
					break;

				case SfxId.CrashPlain:
					entry.priority = 60; entry.cooldown = 0.05f; entry.maxConcurrent = 3;
					entry.pitchRange = new Vector2(0.92f, 1.08f);
					break;
				case SfxId.CrashMistimed:
					entry.priority = 60; entry.cooldown = 0.05f; entry.maxConcurrent = 2;
					break;
				case SfxId.CrashPerfect:
					entry.priority = 70; entry.cooldown = 0.05f; entry.maxConcurrent = 2;
					entry.duckAmount = 0.5f; entry.duckSeconds = 0.4f;
					break;
				case SfxId.Hit:
					entry.priority = 50; entry.cooldown = 0.08f; entry.maxConcurrent = 2;
					entry.pitchRange = new Vector2(0.92f, 1.08f);
					break;
				case SfxId.Heal:
					entry.priority = 40; entry.maxConcurrent = 1;
					break;
				case SfxId.Pickup:
					entry.priority = 40; entry.maxConcurrent = 2;
					break;
				case SfxId.ComboTick:
					entry.priority = 25; entry.cooldown = 0.05f; entry.maxConcurrent = 2;
					entry.volume = 0.7f;
					break;

				case SfxId.BoostStart:
					entry.priority = 35; entry.maxConcurrent = 1;
					break;
				case SfxId.BoostLoop:
					entry.priority = 10; entry.loop = true; entry.volume = 0.5f;
					break;

				case SfxId.WarnCeiling:
					entry.priority = 55; entry.maxConcurrent = 1;
					break;
				case SfxId.WarnLateral:
					entry.priority = 55; entry.maxConcurrent = 1;
					break;
				case SfxId.WarnGround:
					entry.priority = 75; entry.maxConcurrent = 1;
					break;
				case SfxId.WarnLowFuel:
					entry.priority = 55; entry.maxConcurrent = 1;
					break;
				case SfxId.WarnLowHealth:
					entry.priority = 55; entry.maxConcurrent = 1;
					break;

				case SfxId.RunEndWreck:
				case SfxId.RunEndFuel:
				case SfxId.RunEndFlyOff:
				case SfxId.RunEndGround:
					entry.priority = 90; entry.maxConcurrent = 1;
					entry.duckAmount = 0.3f; entry.duckSeconds = 1f;
					break;
				case SfxId.GameOver:
					entry.priority = 95; entry.maxConcurrent = 1;
					entry.duckAmount = 0.5f; entry.duckSeconds = 1f;
					break;
			}

			return entry;
		}
	}
}
