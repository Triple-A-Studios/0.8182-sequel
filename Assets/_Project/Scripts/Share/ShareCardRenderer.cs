using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.Share
{
	/// <summary>
	/// Draws a share card to a PNG. Builds the card layout on a throw-away <see cref="PanelRenderer"/> whose panel settings
	/// (a copy of the shared ones) draw into a <see cref="RenderTexture"/> instead of the screen, waits for the panel to
	/// draw, reads the pixels back and encodes them, then destroys everything it made. Nothing is ever shown to the player.
	/// Runs in play mode only (a panel needs the player loop); one render at a time.
	/// </summary>
	public static class ShareCardRenderer
	{
		/// <summary>
		/// Layout size in reference px: the game's 2400 x 1080 screen layouts plus a 72 px footer. The texture is
		/// <see cref="RESOLUTION_SCALE"/> times larger (1: the layouts are already authored at full size).
		/// </summary>
		public const int CARD_WIDTH = 2400;
		public const int CARD_HEIGHT = 1152;
		public const int RESOLUTION_SCALE = 1;

		private const int k_MaxWaitFrames = 30;
		private const int k_SettleFrames = 3;

		private static bool m_s_busy;

		/// <summary>The PNG bytes of the card, or null when the assets are missing, a render is already running or it failed.</summary>
		public static async Awaitable<byte[]> RenderPngAsync(ShareCardData data)
		{
			if (m_s_busy) return null;

			var assets = Resources.Load<ShareCardAssets>(ShareCardAssets.RESOURCE_NAME);
			if (assets == null || assets.CardLayout == null || assets.PanelSettingsTemplate == null)
			{
				Debug.LogWarning("[Share] ShareCardAssets is missing or incomplete; no card rendered.");
				return null;
			}

			m_s_busy = true;
			GameObject host = null;
			PanelSettings settings = null;
			RenderTexture target = null;
			Texture2D readback = null;
			var previousActive = RenderTexture.active;
			try
			{
				var width = CARD_WIDTH * RESOLUTION_SCALE;
				var height = CARD_HEIGHT * RESOLUTION_SCALE;
				target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

				settings = Object.Instantiate(assets.PanelSettingsTemplate);
				settings.targetTexture = target;
				settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
				settings.referenceResolution = new Vector2Int(CARD_WIDTH, CARD_HEIGHT);
				settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
				settings.match = 0.5f;
				settings.clearColor = true;
				settings.colorClearValue = new Color(0.106f, 0.161f, 0.216f, 1f);

				host = new GameObject("ShareCardPanel") { hideFlags = HideFlags.HideAndDontSave };
				host.SetActive(false);
				var panel = host.AddComponent<PanelRenderer>();
				panel.panelSettings = settings;
				panel.visualTreeAsset = assets.CardLayout;

				VisualElement root = null;
				panel.RegisterUIReloadCallback((renderer, element, version) => root = element);
				host.SetActive(true);

				for (var frame = 0; root == null && frame < k_MaxWaitFrames; frame++) await Awaitable.NextFrameAsync();
				var card = root?.Q<VisualElement>("share-card");
				if (card == null)
				{
					Debug.LogWarning("[Share] The card layout did not build; no card rendered.");
					return null;
				}

				new ShareCardView(card).Apply(data);

				// Let the layout resolve and the panel draw into the texture before reading it back.
				for (var frame = 0; frame < k_SettleFrames; frame++) await Awaitable.NextFrameAsync();
				await Awaitable.EndOfFrameAsync();

				RenderTexture.active = target;
				readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
				readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				readback.Apply(false);
				return readback.EncodeToPNG();
			}
			finally
			{
				RenderTexture.active = previousActive;
				if (host != null) Object.Destroy(host);
				if (settings != null) Object.Destroy(settings);
				if (target != null)
				{
					target.Release();
					Object.Destroy(target);
				}

				if (readback != null) Object.Destroy(readback);
				m_s_busy = false;
			}
		}
	}
}
