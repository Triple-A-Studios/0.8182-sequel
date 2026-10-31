using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.Share
{
	/// <summary>
	/// What <see cref="ShareCardRenderer"/> needs to build the card: the share card layout and a panel settings asset to
	/// copy (the shared one, so the theme and shaders match). Loaded from <c>Resources/ShareCardAssets</c> so it works from
	/// every scene, including the ones played on their own.
	/// </summary>
	[CreateAssetMenu(fileName = "ShareCardAssets", menuName = "Opoint8182/Share Card Assets")]
	public class ShareCardAssets : ScriptableObject
	{
		public const string RESOURCE_NAME = "ShareCardAssets";

		[SerializeField] private VisualTreeAsset m_cardLayout;
		[SerializeField] private PanelSettings m_panelSettingsTemplate;

		public VisualTreeAsset CardLayout => m_cardLayout;
		public PanelSettings PanelSettingsTemplate => m_panelSettingsTemplate;
	}
}
