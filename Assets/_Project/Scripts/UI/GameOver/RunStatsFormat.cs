using System.Globalization;
using Opoint8182.Stats;
using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>Text for the Game Over stat chips (and the share card that copies them) from a finished run's <see cref="RunStats"/>.</summary>
	public static class RunStatsFormat
	{
		public static string Distance(float meters) =>
			meters >= 1000f
				? (meters / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km"
				: Mathf.RoundToInt(meters).ToString(CultureInfo.InvariantCulture) + " m";

		public static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
	}
}
