using System;
using System.Reflection;

namespace HDTHighlightMinionType
{
	internal static class PluginMetadata
	{
		public static Version Version
		{
			get
			{
				var version = typeof(Plugin).Assembly.GetName().Version ?? new Version(1, 0);
				if(version.Revision > 0) return version;
				if(version.Build > 0) return new Version(version.Major, version.Minor, version.Build);
				return new Version(version.Major, version.Minor);
			}
		}

		public static string DisplayVersion
			=> typeof(Plugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
				?? Version.ToString();
	}
}
