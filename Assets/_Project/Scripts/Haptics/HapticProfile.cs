using System;
using UnityEngine;

namespace Opoint8182
{
	public enum HapticTier
	{
		/// <summary>A faint tick: UI presses.</summary>
		Light,

		/// <summary>A normal thud: plain crashes, hits.</summary>
		Medium,

		/// <summary>A hard thump: a perfect crash.</summary>
		Heavy,

		/// <summary>A multi-pulse crash rhythm: the run ending.</summary>
		CrashRhythm,
	}

	/// <summary>
	/// One vibration pattern, as a pair of parallel arrays: segment <c>i</c> lasts <c>durationsMs[i]</c> milliseconds at
	/// <c>amplitudes[i]</c> (0 = off, 1-255 = strength). A plain buzz is one segment; a rhythm alternates strong segments
	/// with 0-amplitude gaps. This is exactly the shape of Android's <c>VibrationEffect.createWaveform(timings, amplitudes)</c>.
	/// Tune them in the <c>Resources/HapticProfiles</c> asset; <see cref="Sanitize"/> repairs mismatched or out-of-range data.
	/// </summary>
	[Serializable]
	public class HapticProfile
	{
		public const int MaxAmplitude = 255;

		/// <summary>No pattern may run longer than this in total, so a typo cannot lock the motor on.</summary>
		public const int MaxTotalMs = 1500;

		public HapticTier tier;

		[Tooltip("Milliseconds each segment plays (or pauses, where its amplitude is 0).")]
		public int[] durationsMs = Array.Empty<int>();

		[Tooltip("Strength of each segment, 0 (off) to 255. Same length as the durations.")]
		public int[] amplitudes = Array.Empty<int>();

		/// <summary>Total length of the pattern in milliseconds (after <see cref="Sanitize"/>).</summary>
		public int TotalMs
		{
			get
			{
				var total = 0;
				foreach (var duration in durationsMs) total += duration;
				return total;
			}
		}

		/// <summary>
		/// Makes the arrays usable: equal length (the shorter wins), durations at least 1 ms, amplitudes 0-255, total capped at
		/// <see cref="MaxTotalMs"/>. An empty profile stays empty (it plays nothing).
		/// </summary>
		public void Sanitize()
		{
			var count = Mathf.Min(durationsMs?.Length ?? 0, amplitudes?.Length ?? 0);
			var durations = new int[count];
			var strengths = new int[count];
			var total = 0;

			for (var i = 0; i < count; i++)
			{
				var duration = Mathf.Max(1, durationsMs[i]);
				if (total + duration > MaxTotalMs)
				{
					count = i;
					break;
				}

				durations[i] = duration;
				strengths[i] = Mathf.Clamp(amplitudes[i], 0, MaxAmplitude);
				total += duration;
			}

			Array.Resize(ref durations, count);
			Array.Resize(ref strengths, count);
			durationsMs = durations;
			amplitudes = strengths;
		}

		/// <summary>The starting patterns; also what is used when the asset is missing.</summary>
		public static HapticProfile CreateDefault(HapticTier tier)
		{
			var profile = new HapticProfile { tier = tier };

			switch (tier)
			{
				case HapticTier.Light:
					profile.durationsMs = new[] { 12 };
					profile.amplitudes = new[] { 70 };
					break;
				case HapticTier.Medium:
					profile.durationsMs = new[] { 30 };
					profile.amplitudes = new[] { 160 };
					break;
				case HapticTier.Heavy:
					profile.durationsMs = new[] { 55 };
					profile.amplitudes = new[] { 255 };
					break;
				case HapticTier.CrashRhythm:
					// Impact, two fading aftershocks, then a long rumble.
					profile.durationsMs = new[] { 60, 50, 40, 50, 30, 60, 160 };
					profile.amplitudes = new[] { 255, 0, 190, 0, 130, 0, 240 };
					break;
			}

			return profile;
		}
	}

	/// <summary>
	/// The haptic patterns, one asset at <c>Resources/HapticProfiles</c>. Created and reset by the editor menu
	/// Tools / Haptics / Create Or Reset HapticProfiles; tune the arrays in the Inspector afterwards.
	/// </summary>
	[CreateAssetMenu(fileName = "HapticProfiles", menuName = "Opoint8182/Haptic Profiles")]
	public class HapticProfiles : ScriptableObject
	{
		public HapticProfile[] profiles = Array.Empty<HapticProfile>();
	}
}
