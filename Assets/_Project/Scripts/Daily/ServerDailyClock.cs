using System;
using UnityEngine;

namespace Opoint8182.Daily
{
	/// <summary>
	/// The daily clock once the server has told the time: the last server time plus the realtime that passed since, so changing the
	/// device clock afterwards does not move it. Until the first server reply it falls back to the device clock (practice only
	/// needs it; competitive attempts come from the server, see <see cref="DailyPlay"/>). Every server reply calls <see cref="Sync"/>,
	/// which also corrects the lag realtime picks up while the app is suspended.
	/// </summary>
	public sealed class ServerDailyClock : IDailyClock
	{
		private static readonly DateTime m_S_Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private double m_offsetSeconds;

		/// <summary>Whether a server time has been received.</summary>
		public bool IsSynced { get; private set; }

		public DateTime UtcNow => IsSynced ? m_S_Epoch.AddSeconds(Time.realtimeSinceStartupAsDouble + m_offsetSeconds) : DateTime.UtcNow;

		/// <summary>Anchors the clock to a server time (Unix milliseconds) received just now.</summary>
		public void Sync(long serverNowMs)
		{
			m_offsetSeconds = serverNowMs / 1000.0 - Time.realtimeSinceStartupAsDouble;
			IsSynced = true;
		}
	}
}
