using System;
using System.Collections.Generic;

namespace Opoint8182.Perf
{
	/// <summary>The numbers of one run, from the frames the sampler was told belong to it.</summary>
	public readonly struct PerfSummary
	{
		public readonly float seconds;
		public readonly int frames;
		public readonly float averageFps;
		/// <summary>Average fps of the slowest 1 % of frames (at least one frame).</summary>
		public readonly float lowFps;
		public readonly float worstMs;
		public readonly int hitches33;
		public readonly int hitches50;
		public readonly int hitches100;

		public PerfSummary(float seconds, int frames, float averageFps, float lowFps, float worstMs, int hitches33, int hitches50, int hitches100)
		{
			this.seconds = seconds;
			this.frames = frames;
			this.averageFps = averageFps;
			this.lowFps = lowFps;
			this.worstMs = worstMs;
			this.hitches33 = hitches33;
			this.hitches50 = hitches50;
			this.hitches100 = hitches100;
		}
	}

	/// <summary>
	/// Collects frame times for the perf overlay: a rolling window of the last 300 frames (live fps, average and worst frame
	/// time) and, separately, every frame of the current run (average fps, 1 % low, hitch counts). Plain class, no Unity
	/// calls, so it can be fed by a test.
	/// </summary>
	public sealed class PerfSampler
	{
		public const float HITCH_33_SECONDS = 1f / 30f;
		public const float HITCH_50_SECONDS = 0.05f;
		public const float HITCH_100_SECONDS = 0.1f;

		/// <summary>A frame longer than this is a stall (a scene load, the app in the background), not a frame to judge.</summary>
		public const float STALL_SECONDS = 2f;

		private const int k_WindowFrames = 300;
		private const int k_MaxRunFrames = 200000;

		private readonly float[] m_window = new float[k_WindowFrames];
		private int m_windowCount;
		private int m_windowHead;

		private readonly List<float> m_run = new(8192);
		private float m_runSeconds;
		private float m_runWorst;
		private int m_hitches33;
		private int m_hitches50;
		private int m_hitches100;

		public int Stalls { get; private set; }

		public int RunFrames => m_run.Count;

		public int Hitches33 => m_hitches33;

		public int Hitches50 => m_hitches50;

		public int Hitches100 => m_hitches100;

		public float RunSeconds => m_runSeconds;

		/// <summary>Adds one frame. <paramref name="inRun"/> is true while a run is being flown (not paused, not in a menu).</summary>
		public void Add(float seconds, bool inRun)
		{
			if (seconds <= 0f) return;

			if (seconds > STALL_SECONDS)
			{
				Stalls++;
				return;
			}

			m_window[m_windowHead] = seconds;
			m_windowHead = (m_windowHead + 1) % k_WindowFrames;
			if (m_windowCount < k_WindowFrames) m_windowCount++;

			if (!inRun || m_run.Count >= k_MaxRunFrames) return;

			m_run.Add(seconds);
			m_runSeconds += seconds;
			if (seconds > m_runWorst) m_runWorst = seconds;
			if (seconds > HITCH_33_SECONDS) m_hitches33++;
			if (seconds > HITCH_50_SECONDS) m_hitches50++;
			if (seconds > HITCH_100_SECONDS) m_hitches100++;
		}

		/// <summary>Frames per second over the last second of the window.</summary>
		public float WindowFps()
		{
			var total = 0f;
			var frames = 0;
			for (var i = 0; i < m_windowCount && total < 1f; i++)
			{
				total += m_window[(m_windowHead - 1 - i + k_WindowFrames) % k_WindowFrames];
				frames++;
			}

			return total > 0f ? frames / total : 0f;
		}

		/// <summary>Average and worst frame time, in milliseconds, over the whole window (about 5 s at 60 fps).</summary>
		public void WindowMs(out float averageMs, out float worstMs)
		{
			var total = 0f;
			var worst = 0f;
			for (var i = 0; i < m_windowCount; i++)
			{
				total += m_window[i];
				if (m_window[i] > worst) worst = m_window[i];
			}

			averageMs = m_windowCount > 0 ? total / m_windowCount * 1000f : 0f;
			worstMs = worst * 1000f;
		}

		/// <summary>Starts a new run. The live window is kept.</summary>
		public void ResetRun()
		{
			m_run.Clear();
			m_runSeconds = 0f;
			m_runWorst = 0f;
			m_hitches33 = 0;
			m_hitches50 = 0;
			m_hitches100 = 0;
			Stalls = 0;
		}

		public PerfSummary Summarize()
		{
			var frames = m_run.Count;
			if (frames == 0) return default;

			var slowest = new List<float>(m_run);
			slowest.Sort();
			slowest.Reverse();
			var take = Math.Max(1, (int)Math.Ceiling(frames * 0.01));
			var slowSum = 0f;
			for (var i = 0; i < take; i++) slowSum += slowest[i];

			return new PerfSummary(
				m_runSeconds,
				frames,
				m_runSeconds > 0f ? frames / m_runSeconds : 0f,
				slowSum > 0f ? take / slowSum : 0f,
				m_runWorst * 1000f,
				m_hitches33,
				m_hitches50,
				m_hitches100);
		}
	}
}
