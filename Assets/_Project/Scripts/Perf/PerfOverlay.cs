using System.Collections.Generic;
using Opoint8182.Common;
using Opoint8182.Game;
using Opoint8182.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Opoint8182.Perf
{
	/// <summary>
	/// Development-only perf overlay (3D art pass, Pass 5b): live fps, frame times, hitches, render and memory counters, and a
	/// summary of the last runs, so a phone can be judged without a cable. It lives next to the launch panel in the Bootstrap
	/// scene (which survives every scene load), is off in release builds, and is opened with F9 or a three-finger tap.
	/// The run summary (average fps, 1 % low, worst frame, hitches, memory change) is also logged when a run ends.
	/// </summary>
	[RequireComponent(typeof(PanelRenderer))]
	public class PerfOverlay : MonoBehaviour
	{
		private const float k_RefreshSeconds = 0.25f;
		private const int k_KeptSummaries = 4;

		private static readonly int[] m_S_Targets = { 60, 61, 30, -1 };

		private readonly PerfSampler m_sampler = new();
		private readonly List<string> m_summaries = new();

		private PanelRenderer m_panelRenderer;
		private VisualElement m_loadedRoot;
		private VisualElement m_root;
		private Label m_fpsLabel;
		private Label m_hitchLabel;
		private Label m_gfxLabel;
		private Label m_memLabel;
		private Label m_lastLabel;
		private Button m_targetButton;

		private ProfilerRecorder m_draw;
		private ProfilerRecorder m_setPass;
		private ProfilerRecorder m_triangles;
		private ProfilerRecorder m_vertices;

		private GameManager m_gameManager;
		private long m_runStartMemory;
		private float m_nextRefresh;
		private bool m_open;
		private bool m_threeFingersDown;

		private void Awake()
		{
			m_panelRenderer = GetComponent<PanelRenderer>();

			// A release build never shows or samples anything.
			if (!Debug.isDebugBuild) gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			m_panelRenderer.RegisterUIReloadCallback(OnUIReload);

			m_draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
			m_setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
			m_triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
			m_vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
		}

		private void OnDisable()
		{
			m_panelRenderer.UnregisterUIReloadCallback(OnUIReload);
			m_loadedRoot = null;
			Unbind();

			m_draw.Dispose();
			m_setPass.Dispose();
			m_triangles.Dispose();
			m_vertices.Dispose();
		}

		private void Update()
		{
			BindGameManager();

			var inRun = m_gameManager != null && m_gameManager.IsPlaying && !m_gameManager.IsPaused && m_gameManager.IsRunActive;
			m_sampler.Add(Time.unscaledDeltaTime, inRun);

			PollToggle();

			if (m_open && Time.unscaledTime >= m_nextRefresh)
			{
				m_nextRefresh = Time.unscaledTime + k_RefreshSeconds;
				Refresh();
			}
		}

		// Each scene reload makes a new GameManager: follow it, and start counting a new run with it.
		private void BindGameManager()
		{
			var current = GameManager.TryGetInstance();
			if (current == m_gameManager) return;

			Unbind();
			m_gameManager = current;
			if (m_gameManager == null) return;

			m_gameManager.RunStarted += HandleRunStarted;
			m_gameManager.RunEnded += HandleRunEnded;
			BeginRun();
		}

		private void Unbind()
		{
			if (m_gameManager == null) return;

			m_gameManager.RunStarted -= HandleRunStarted;
			m_gameManager.RunEnded -= HandleRunEnded;
			m_gameManager = null;
		}

		private void HandleRunStarted() => BeginRun();

		private void BeginRun()
		{
			m_sampler.ResetRun();
			m_runStartMemory = Profiler.GetTotalAllocatedMemoryLong();
		}

		private void HandleRunEnded(RunEndCause cause)
		{
			var summary = m_sampler.Summarize();
			if (summary.frames == 0) return;

			var memoryChange = (Profiler.GetTotalAllocatedMemoryLong() - m_runStartMemory) / 1048576f;
			var text = $"{cause} · {summary.seconds:0}s · avg {summary.averageFps:0.0} · 1% low {summary.lowFps:0.0} · worst {summary.worstMs:0} ms · hitches >33: {summary.hitches33} >50: {summary.hitches50} >100: {summary.hitches100} · mem {memoryChange:+0.0;-0.0} MB · cap {Application.targetFrameRate}";
			Debug.Log($"[Perf] run end: {text}");

			m_summaries.Insert(0, text);
			if (m_summaries.Count > k_KeptSummaries) m_summaries.RemoveAt(m_summaries.Count - 1);
			if (m_open) Refresh();
		}

		// F9 in the Editor and on a keyboard; three fingers down together on a touch screen (re-armed when all are lifted).
		private void PollToggle()
		{
			var keyboard = Keyboard.current;
			if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) Toggle();

			var touchscreen = Touchscreen.current;
			if (touchscreen == null) return;

			var down = 0;
			for (var i = 0; i < touchscreen.touches.Count; i++)
			{
				if (touchscreen.touches[i].press.isPressed) down++;
			}

			if (down >= 3 && !m_threeFingersDown)
			{
				m_threeFingersDown = true;
				Toggle();
			}
			else if (down == 0)
			{
				m_threeFingersDown = false;
			}
		}

		private void Toggle() => SetOpen(!m_open);

		private void SetOpen(bool open)
		{
			m_open = open;
			m_root?.EnableInClassList("is-open", open);
			if (open) Refresh();
		}

		private void Refresh()
		{
			if (m_fpsLabel == null) return;

			m_sampler.WindowMs(out var averageMs, out var worstMs);
			m_fpsLabel.text = $"{m_sampler.WindowFps():0} FPS · {averageMs:0.0} ms · max {worstMs:0.0}";
			m_hitchLabel.text = $"RUN {m_sampler.RunSeconds:0}s · HITCH >33 ms: {m_sampler.Hitches33} · >50: {m_sampler.Hitches50} · >100: {m_sampler.Hitches100} · STALLS {m_sampler.Stalls}";

			m_gfxLabel.text = m_draw.Valid && m_draw.LastValue > 0
				? $"DRAW {m_draw.LastValue} · SETPASS {m_setPass.LastValue} · TRI {m_triangles.LastValue / 1000}k · VERT {m_vertices.LastValue / 1000}k"
				: "DRAW / TRI: no render counters on this platform";

			var memory = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
			var managed = System.GC.GetTotalMemory(false) / 1048576f;
			var refresh = Screen.currentResolution.refreshRateRatio.value;
			m_memLabel.text = $"MEM {memory:0} MB · GC {managed:0} MB · CAP {Application.targetFrameRate} · DISPLAY {refresh:0} Hz";

			m_targetButton.text = $"CAP {Application.targetFrameRate}";
			m_lastLabel.text = m_summaries.Count == 0 ? "NO RUN FINISHED YET" : string.Join("\n", m_summaries);
		}

		private void CycleTarget()
		{
			var index = System.Array.IndexOf(m_S_Targets, Application.targetFrameRate);
			FrameRateSetup.SetDevOverride(m_S_Targets[(index + 1) % m_S_Targets.Length]);
			Refresh();
		}

		private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
		{
			if (m_loadedRoot == root) return;
			m_loadedRoot = root;

			m_root = root.Q<VisualElement>("perf-root");
			SafeAreaHelper.Apply(root.Q<VisualElement>("perf-safe-area"));
			m_fpsLabel = root.Q<Label>("perf-fps");
			m_hitchLabel = root.Q<Label>("perf-hitch");
			m_gfxLabel = root.Q<Label>("perf-gfx");
			m_memLabel = root.Q<Label>("perf-mem");
			m_lastLabel = root.Q<Label>("perf-last");
			m_targetButton = root.Q<Button>("perf-target");

			m_targetButton.clicked += CycleTarget;
			root.Q<Button>("perf-reset").clicked += () =>
			{
				m_summaries.Clear();
				BeginRun();
				Refresh();
			};
			root.Q<Button>("perf-hide").clicked += () => SetOpen(false);

			SetOpen(m_open);
		}
	}
}
