using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;

namespace HDTHighlightMinionType
{
	internal sealed class RaceDescriptor
	{
		private static readonly Dictionary<string, string> Abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["BEAST"] = "Be", ["DEMON"] = "De", ["DRAGON"] = "Dr", ["ELEMENTAL"] = "El",
			["MECHANICAL"] = "Me", ["MECH"] = "Me", ["MURLOC"] = "Mu", ["NAGA"] = "Na",
			["PIRATE"] = "Pi", ["QUILBOAR"] = "Qu", ["UNDEAD"] = "Un"
		};

		public RaceDescriptor(Race race)
		{
			Race = race;
			Key = race.ToString();
			FullName = Humanize(Key);
			ShortName = Abbreviations.TryGetValue(Key, out var value) ? value : MakeShort(FullName);
		}

		public Race Race { get; }
		public string Key { get; }
		public string FullName { get; }
		public string ShortName { get; }

		private static string Humanize(string value)
		{
			if(value.Equals("MECHANICAL", StringComparison.OrdinalIgnoreCase)) return "Mech";
			if(value.Equals("QUILBOAR", StringComparison.OrdinalIgnoreCase)) return "Quilboar";
			return string.Join(" ", value.Split('_').Where(x => x.Length > 0).Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1).ToLowerInvariant()));
		}

		private static string MakeShort(string value)
		{
			var letters = new string(value.Where(char.IsLetter).Take(2).ToArray());
			return letters.Length == 0 ? "??" : letters.Length == 1 ? letters.ToUpperInvariant() : char.ToUpperInvariant(letters[0]) + letters.Substring(1).ToLowerInvariant();
		}
	}

	internal static class RaceResolver
	{
		private static readonly object RacePropertySync = new object();
		private static readonly Dictionary<System.Type, PropertyInfo?> RaceProperties = new Dictionary<System.Type, PropertyInfo?>();

		public static IReadOnlyList<RaceDescriptor> Available()
		{
			var races = BattlegroundsUtils.GetAvailableRaces();
			if(races == null) return Array.Empty<RaceDescriptor>();
			return races.Where(Usable).Distinct().Select(x => new RaceDescriptor(x)).OrderBy(x => x.FullName).ToList();
		}

		public static IReadOnlyList<string> GetRaceKeys(Card card, out bool isAll)
		{
			isAll = card.IsAllRace();
			var races = new HashSet<Race>();
			TryReadFutureRaceList(card, races);
			if(card.RaceEnum.HasValue) races.Add(card.RaceEnum.Value);
			if(card.SecondaryRaceEnum.HasValue) races.Add(card.SecondaryRaceEnum.Value);
			if(races.Contains(Race.ALL)) isAll = true;
			return races.Where(Usable).Select(x => x.ToString()).ToList();
		}

		private static void TryReadFutureRaceList(Card card, ISet<Race> output)
		{
			try
			{
				var type = card.GetType();
				PropertyInfo? property;
				lock(RacePropertySync)
				{
					if(!RaceProperties.TryGetValue(type, out property))
					{
						property = type.GetProperty("Races", BindingFlags.Instance | BindingFlags.Public);
						RaceProperties[type] = property;
					}
				}
				if(property?.GetValue(card) is IEnumerable values)
					foreach(var value in values)
						if(value is Race race) output.Add(race);
			}
			catch(Exception ex) { Diagnostics.WarnOnce("future-race-list", "Could not read the optional multi-race card property: " + ex.Message); }
		}

		private static bool Usable(Race race) => race != Race.INVALID && race != Race.ALL && race != Race.BLANK;
	}
}
