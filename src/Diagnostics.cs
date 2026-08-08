using System;
using System.Collections.Generic;
using Hearthstone_Deck_Tracker.Utility.Logging;

namespace HDTHighlightMinionType
{
	internal static class Diagnostics
	{
		private static readonly object Sync = new object();
		private static readonly HashSet<string> OnceKeys = new HashSet<string>(StringComparer.Ordinal);
		private static readonly Dictionary<string, DateTime> LastWarnings = new Dictionary<string, DateTime>(StringComparer.Ordinal);

		public static void Info(string message) => Log.Info("[HighlightMinionType] " + message);
		public static void Warn(string message) => Log.Warn("[HighlightMinionType] " + message);

		public static void WarnOnce(string key, string message)
		{
			lock(Sync)
				if(!OnceKeys.Add(key)) return;
			Warn(message);
		}

		public static void WarnThrottled(string key, string message, TimeSpan interval)
		{
			var now = DateTime.UtcNow;
			lock(Sync)
			{
				if(LastWarnings.TryGetValue(key, out var previous) && now - previous < interval) return;
				LastWarnings[key] = now;
			}
			Warn(message);
		}
	}
}
