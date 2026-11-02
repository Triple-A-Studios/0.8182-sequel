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

		/// <summary>A lifetime distance for the Profile tile: meters, then one decimal of km, then whole km from 100 km ("850 m", "12.4 km", "418 km").</summary>
		public static string TotalDistance(float meters) =>
			meters >= 100000f
				? Count(Mathf.RoundToInt(meters / 1000f)) + " km"
				: Distance(meters);

		public static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
	}
}
