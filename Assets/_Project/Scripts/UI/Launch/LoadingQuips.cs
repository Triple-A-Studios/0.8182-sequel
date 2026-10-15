namespace Opoint8182.UI
{
	/// <summary>
	/// The destruction one-liners on the loading screen (mockup S01). One is picked at random per launch. Yours to
	/// rewrite: keep them short enough for one line at the loading screen's quip size (about 70 characters).
	/// </summary>
	public static class LoadingQuips
	{
		public static readonly string[] All =
		{
			"Structural engineers hate this one weak point.",
			"Buildings are just tall suggestions.",
			"Fuel is a state of mind. Mostly it is a number.",
			"Every skyline is a refuel station if you are brave enough.",
			"Aim for the shiny bit.",
			"Insurance does not cover this. Fly anyway.",
			"The ground is optional. The crash is not.",
			"Reminder: brakes are for other games.",
			"Gravity called. It wants a word.",
			"Perfect crashes pay the most. Allegedly.",
			"Somewhere, a demolition crew is jealous.",
			"Please keep your arms inside the plane.",
		};

		/// <summary>The first quip is the mockup's sample line; used when no randomness is wanted.</summary>
		public static string Sample => All[0];

		public static string Pick()
		{
			return All[UnityEngine.Random.Range(0, All.Length)];
		}
	}
}
