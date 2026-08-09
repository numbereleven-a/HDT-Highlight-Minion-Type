using System;
using System.IO;
using System.Xml.Serialization;
using Hearthstone_Deck_Tracker;

namespace HDTHighlightMinionType
{
	public enum HighlightStyle
	{
		GlowBorder,
		DimOthers,
		Combo
	}

	public sealed class PluginSettings
	{
		private const int CurrentSettingsVersion = 1600;
		[XmlIgnore]
		private bool IsFutureVersion { get; set; }
		public int SettingsVersion { get; set; }
		public double Left { get; set; } = 80;
		public double Top { get; set; } = 80;
		public double Opacity { get; set; } = 0.8;
		public double Scale { get; set; } = 0.8;
		public HighlightStyle HighlightStyle { get; set; } = HighlightStyle.Combo;
		public string GlowColor { get; set; } = "#FF58D8FF";
		public double BorderOpacity { get; set; } = 0.8;
		public double HighlightOpacity { get; set; } = 0.8;
		public double DimStrength { get; set; } = 0.8;
		public bool AlwaysHighlightAll { get; set; }
		public bool ShowNeutral { get; set; } = true;
		public bool IgnoreSpells { get; set; } = true;
		public bool LockOverlayPosition { get; set; }
		public bool HideOverlayDuringCombat { get; set; } = true;

		public void Normalize()
		{
			if(!Finite(Left)) Left = 80;
			if(!Finite(Top)) Top = 80;
			if(!Finite(Opacity) || Opacity < 0.25 || Opacity > 1) Opacity = 0.8;
			if(!Finite(Scale) || Scale < 0.6 || Scale > 2) Scale = 0.8;
			if(!Finite(DimStrength) || DimStrength < 0 || DimStrength > 1) DimStrength = 0.8;
			if(!Finite(BorderOpacity) || BorderOpacity < 0 || BorderOpacity > 1) BorderOpacity = 0.8;
			if(!Finite(HighlightOpacity) || HighlightOpacity < 0 || HighlightOpacity > 1) HighlightOpacity = 0.8;
			if(!Enum.IsDefined(typeof(HighlightStyle), HighlightStyle)) HighlightStyle = HighlightStyle.Combo;
			try { System.Windows.Media.ColorConverter.ConvertFromString(GlowColor); }
			catch { GlowColor = "#FF58D8FF"; }
		}

		public static PluginSettings Load()
		{
			var path = GetPath();
			if(!File.Exists(path)) return new PluginSettings();
			try
			{
				using(var stream = File.OpenRead(path))
				{
					var value = (PluginSettings?)new XmlSerializer(typeof(PluginSettings)).Deserialize(stream) ?? new PluginSettings();
					value.IsFutureVersion = value.SettingsVersion > CurrentSettingsVersion;
					var migrated = value.Migrate();
					value.Normalize();
					if(migrated) value.Save();
					return value;
				}
			}
			catch(Exception ex)
			{
				Diagnostics.Warn("Could not load settings: " + ex.Message);
				return new PluginSettings();
			}
		}

		public void Save()
		{
			Normalize();
			if(IsFutureVersion)
			{
				Diagnostics.WarnOnce("future-settings-save", "Settings were created by a newer plugin version and will not be overwritten.");
				return;
			}
			SettingsVersion = CurrentSettingsVersion;
			try
			{
				var path = GetPath();
				var directory = Path.GetDirectoryName(path);
				if(string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("The settings directory could not be determined.");
				Directory.CreateDirectory(directory);
				using(var stream = File.Create(path))
					new XmlSerializer(typeof(PluginSettings)).Serialize(stream, this);
			}
			catch(Exception ex) { Diagnostics.Warn("Could not save settings: " + ex.Message); }
		}

		private static string GetPath()
		{
			var root = Config.Instance.ConfigDir;
			if(string.IsNullOrWhiteSpace(root))
				root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HearthstoneDeckTracker");
			return Path.Combine(root, "HighlightMinionType", "settings.xml");
		}

		private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

		private bool Migrate()
		{
			if(SettingsVersion >= CurrentSettingsVersion) return false;
			if(SettingsVersion < 1200) AlwaysHighlightAll = false;
			if(SettingsVersion < 1300) HideOverlayDuringCombat = true;
			if(SettingsVersion < 1400 && !Finite(BorderOpacity)) BorderOpacity = 0.8;
			if(SettingsVersion < 1600) IgnoreSpells = true;
			SettingsVersion = CurrentSettingsVersion;
			return true;
		}
	}
}
