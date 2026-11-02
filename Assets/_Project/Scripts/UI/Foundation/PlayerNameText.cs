using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// A player name followed by Unity's "#1234" tag in a muted tone, so two players with the same name can be told apart without the tag
	/// competing with the name. The result is rich text: the label showing it keeps <c>enableRichText</c> on (the default).
	/// </summary>
	public static class PlayerNameText
	{
		/// <summary>The theme's <c>--ink-55</c> (cream at 55 %), the muted text tone used for hints. Rich text cannot read USS variables.</summary>
		private const string k_MutedColor = "#F6EFE28C";

		/// <summary>Name plus muted tag; the name alone when there is no tag. Angle brackets are expected to be gone already (<c>LeaderboardRow.SplitName</c>).</summary>
		public static string WithTag(string name, string tag)
		{
			return string.IsNullOrEmpty(tag) ? name : $"{name}<color={k_MutedColor}>{tag}</color>";
		}

		/// <summary>Sets <paramref name="label"/> to <paramref name="name"/> plus the muted <paramref name="tag"/>.</summary>
		public static void Apply(Label label, string name, string tag)
		{
			label.enableRichText = true;
			label.text = WithTag(name, tag);
		}
	}
}
