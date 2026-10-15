using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Opoint8182.UI
{
	/// <summary>
	/// Owns the launch panel in the Bootstrap scene (UI/art pass, Pass 7b): the loading screen with its rotate gate.
	/// <see cref="Opoint8182.Bootstrap.BootstrapLoader"/> reports the scene-load progress and then awaits
	/// <see cref="WaitUntilReadyAsync"/>, which returns once the loads are done, the minimum time has passed and the
	/// page is landscape. Values reported before the UI tree has loaded are applied when it does.
	/// Pass 7c: when <see cref="ILastWeekResultsSource"/> has a closed week to show, the last week's results screen
	/// takes the place of the loading content and the flow waits for CONTINUE before the panel hides.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public class LaunchFlowHost : MonoBehaviour
	{
		[Tooltip("Dev / editor builds only: force the last week's results screen on launch.")]
		[SerializeField] private LastWeekResultsDebugMode m_debugResults = LastWeekResultsDebugMode.None;

		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private LoadingScreen m_loading;
		private LastWeekResultsScreen m_results;
		private ILastWeekResultsSource m_source;

		private float m_startTime;
		private float m_target;
		private float m_shown;
		private bool m_loadsDone;
		private bool m_finished;

		public LoadingScreen Loading => m_loading;

		public LastWeekResultsScreen Results => m_results;

		/// <summary>
		/// Where closed weeks come from. Defaults to nothing (the flow goes straight to the menu); the weekly
		/// leaderboard rework (backlog #28) plugs the real source in here.
		/// </summary>
		public ILastWeekResultsSource ResultsSource
		{
			get => m_source;
			set => m_source = value;
		}

		/// <summary>True while the rotate prompt is up (the page is portrait where the gate applies).</summary>
		public bool GateActive { get; private set; }

		private void Awake()
		{
			m_panelRenderer = GetComponent<PanelRenderer>();
			m_startTime = Time.unscaledTime;
		}

		private void OnEnable()
		{
			m_panelRenderer.RegisterUIReloadCallback(OnUIReload);
		}

		private void OnDisable()
		{
			m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
			m_loadedRoot = null;
			m_loading = null;
			m_results = null;
		}

		/// <summary>Raw scene-load progress (<see cref="Opoint8182.Bootstrap.AsyncOperationGroup.Progress"/>).</summary>
		public void ReportProgress(float rawProgress)
		{
			m_target = LaunchFlow.MapProgress(rawProgress, m_loadsDone);
		}

		/// <summary>Completes when the loads are done, the minimum time has passed and no rotate prompt is up; then hides.</summary>
		public async Task WaitUntilReadyAsync()
		{
			m_loadsDone = true;
			m_target = 1f;

			// m_shown only advances while the view exists; without a view there is nothing to wait for.
			while (!LaunchFlow.CanLeave(Time.unscaledTime - m_startTime, GateActive) || (m_loading != null && m_shown < 1f))
			{
				await Task.Yield();
			}

			var pending = ResolveSource().TryGetPending();
			if (pending.HasValue && m_results != null && m_loading != null)
			{
				m_loading.SetContentVisible(false);
				await m_results.ShowAndWaitAsync(pending.Value);
			}

			m_finished = true;
			m_loading?.Hide();
			Debug.Log($"[Launch] loading screen left after {Time.unscaledTime - m_startTime:0.00}s");
		}

		private ILastWeekResultsSource ResolveSource()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (m_debugResults != LastWeekResultsDebugMode.None)
			{
				return new SampleLastWeekResultsSource(m_debugResults == LastWeekResultsDebugMode.WithRank);
			}
#endif
			return m_source ?? new NullLastWeekResultsSource();
		}

		private void Update()
		{
			if (m_finished || m_loading == null) return;

			GateActive = LaunchFlow.GateApplies() && LaunchFlow.IsPortrait(Screen.width, Screen.height);
			if (GateActive != m_loading.RotateGateVisible) m_loading.SetRotateGate(GateActive);

			m_shown = Mathf.MoveTowards(m_shown, m_target, LaunchFlow.BAR_SPEED * Time.unscaledDeltaTime);
			m_loading.SetProgress(m_shown);
		}

		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			SafeAreaHelper.Apply(root.Q<VisualElement>("launch-safe-area"));
			m_loading = new LoadingScreen(root.Q<VisualElement>("launch-root"));
			m_loading.Show();
			m_results = new LastWeekResultsScreen(root.Q<VisualElement>("screen-results"));
		}
	}
}
