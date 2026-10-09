using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Pooled floating popups for crashes (mockup S03 c): "+4,500" with an optional PERFECT CRASH tag and an optional coin
	/// line, spawned at a panel position, popping in, floating up and fading out. The pool is built once; showing a popup
	/// allocates nothing but its text. Animation runs on unscaled time so hit-stop does not freeze it.
	/// </summary>
	public class HudImpactPopups
	{
		private const int k_PoolSize = 6;
		private const long k_TickMs = 16;
		private const float k_Duration = 1.15f;
		private const float k_PopInSeconds = 0.14f;
		private const float k_PopSettleSeconds = 0.1f;
		private const float k_PopFromScale = 0.5f;
		private const float k_PopPeakScale = 1.12f;
		private const float k_FloatDistance = 130f;
		private const float k_FadeStart = 0.62f;

		private readonly Popup[] m_pool = new Popup[k_PoolSize];
		private int m_next;

		public HudImpactPopups(VisualElement layer)
		{
			for (var i = 0; i < k_PoolSize; i++) m_pool[i] = new Popup(layer);
		}

		/// <param name="coins">Coin line under the score; 0 hides it.</param>
		public void Show(Vector2 panelPosition, int points, bool perfect, int coins)
		{
			// Round robin: a burst of crashes reuses the oldest popup instead of growing the pool.
			var popup = m_pool[m_next];
			m_next = (m_next + 1) % k_PoolSize;
			popup.Show(panelPosition, points, perfect, coins);
		}

		/// <summary>Hides every popup immediately (gallery reset, run end).</summary>
		public void Clear()
		{
			foreach (var popup in m_pool) popup.Stop();
		}

		private class Popup
		{
			private readonly VisualElement m_anchor;
			private readonly VisualElement m_stack;
			private readonly Label m_perfectLabel;
			private readonly Label m_scoreLabel;
			private readonly VisualElement m_coinRow;
			private readonly Label m_coinLabel;
			private readonly IVisualElementScheduledItem m_tick;
			private float m_startTime;

			public Popup(VisualElement layer)
			{
				m_anchor = new VisualElement { pickingMode = PickingMode.Ignore };
				m_anchor.AddToClassList("hud-popup");

				m_stack = new VisualElement { pickingMode = PickingMode.Ignore };
				m_stack.AddToClassList("hud-popup__stack");

				m_perfectLabel = MakeLabel("PERFECT CRASH", "hud-popup__perfect");
				m_scoreLabel = MakeLabel(string.Empty, "hud-popup__score");

				m_coinRow = new VisualElement { pickingMode = PickingMode.Ignore };
				m_coinRow.AddToClassList("hud-popup__coin-row");
				m_coinLabel = MakeLabel(string.Empty, "hud-popup__coin");
				var coinDot = new VisualElement { pickingMode = PickingMode.Ignore };
				coinDot.AddToClassList("hud-popup__coin-dot");
				m_coinRow.Add(m_coinLabel);
				m_coinRow.Add(coinDot);

				m_stack.Add(m_perfectLabel);
				m_stack.Add(m_scoreLabel);
				m_stack.Add(m_coinRow);
				m_anchor.Add(m_stack);
				layer.Add(m_anchor);

				m_tick = m_anchor.schedule.Execute(Tick).Every(k_TickMs);
				m_tick.Pause();
			}

			public void Show(Vector2 panelPosition, int points, bool perfect, int coins)
			{
				m_anchor.style.left = panelPosition.x;
				m_anchor.style.top = panelPosition.y;

				m_scoreLabel.text = $"+{points:N0}";
				m_perfectLabel.style.display = perfect ? DisplayStyle.Flex : DisplayStyle.None;
				m_coinRow.style.display = coins > 0 ? DisplayStyle.Flex : DisplayStyle.None;
				if (coins > 0) m_coinLabel.text = $"+{coins:N0}";

				m_startTime = Time.unscaledTime;
				Apply(0f);
				m_anchor.style.display = DisplayStyle.Flex;
				m_tick.Resume();
			}

			public void Stop()
			{
				m_tick.Pause();
				m_anchor.style.display = DisplayStyle.None;
			}

			private void Tick(TimerState _)
			{
				var elapsed = Time.unscaledTime - m_startTime;
				if (elapsed >= k_Duration)
				{
					Stop();
					return;
				}

				Apply(elapsed);
			}

			private void Apply(float elapsed)
			{
				var t = Mathf.Clamp01(elapsed / k_Duration);

				float scale;
				if (elapsed < k_PopInSeconds) scale = Mathf.Lerp(k_PopFromScale, k_PopPeakScale, elapsed / k_PopInSeconds);
				else scale = Mathf.Lerp(k_PopPeakScale, 1f, Mathf.Clamp01((elapsed - k_PopInSeconds) / k_PopSettleSeconds));

				var eased = 1f - (1f - t) * (1f - t);
				var opacity = t < k_FadeStart ? 1f : 1f - (t - k_FadeStart) / (1f - k_FadeStart);

				m_stack.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
				m_stack.style.translate = new StyleTranslate(new Translate(0f, -eased * k_FloatDistance));
				m_stack.style.opacity = opacity;
			}

			private static Label MakeLabel(string text, string className)
			{
				var label = new Label(text) { pickingMode = PickingMode.Ignore };
				label.AddToClassList("t-baloo-extrabold");
				label.AddToClassList(className);
				return label;
			}
		}
	}
}
