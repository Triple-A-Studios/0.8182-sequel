using Opoint8182.Common;
using UnityEngine;

namespace Opoint8182.Perf
{
	/// <summary>
	/// Hidden commands for development builds, typed into the Profile screen's name field (a touch screen has no spare
	/// gesture: three fingers is the system screenshot on Android). A recognised command is run instead of being taken as a
	/// name, so the name does not change. In release builds nothing is recognised and every entry is an ordinary name.
	/// <list type="bullet">
	/// <item><c>perf_on</c> / <c>perf_off</c>: shows or hides the perf overlay (it stays open until the app closes or it is hidden).</item>
	/// <item><c>cap_60</c>, <c>cap_61</c>, <c>cap_30</c>: sets the target frame rate (kept for the next launch).</item>
	/// <item><c>cap_free</c>: target -1, the platform default. <c>cap_reset</c>: back to the project default (60 on Android).</item>
	/// </list>
	/// Every command is letters, digits and underscores, so it survives the name field's own filtering.
	/// </summary>
	public static class DevCommands
	{
		/// <summary>Runs the command in <paramref name="entry"/> if it is one. <paramref name="feedback"/> says what happened.</summary>
		public static bool TryRun(string entry, out string feedback)
		{
			feedback = null;
			if (!Debug.isDebugBuild || string.IsNullOrWhiteSpace(entry)) return false;

			switch (entry.Trim().ToLowerInvariant())
			{
				case "perf_on":
					feedback = PerfOverlay.Open() ? "DEV: PERF OVERLAY ON" : "DEV: NO PERF OVERLAY IN THIS SCENE SETUP";
					return true;
				case "perf_off":
					PerfOverlay.Close();
					feedback = "DEV: PERF OVERLAY OFF";
					return true;
				case "cap_60": return SetCap(60, out feedback);
				case "cap_61": return SetCap(61, out feedback);
				case "cap_30": return SetCap(30, out feedback);
				case "cap_free": return SetCap(-1, out feedback);
				case "cap_reset":
					FrameRateSetup.SetDevOverride(0);
					feedback = $"DEV: CAP RESET ({Application.targetFrameRate})";
					return true;
				default:
					return false;
			}
		}

		private static bool SetCap(int fps, out string feedback)
		{
			FrameRateSetup.SetDevOverride(fps);
			feedback = $"DEV: CAP {Application.targetFrameRate}";
			return true;
		}
	}
}
