using System;
using System.Collections.Generic;
using System.Linq;

namespace HDTHighlightMinionType
{
	public enum SelectionState
	{
		None,
		Include,
		Exclude
	}

	public sealed class SelectionLogic
	{
		public const string NeutralKey = "__neutral__";
		private readonly Dictionary<string, SelectionState> _states = new Dictionary<string, SelectionState>(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyDictionary<string, SelectionState> States => _states;
		public bool HasSelection => _states.Values.Any(x => x != SelectionState.None);
		public bool HasIncludes => _states.Values.Any(x => x == SelectionState.Include);

		public SelectionState Get(string key)
			=> _states.TryGetValue(key, out var state) ? state : SelectionState.None;

		public void Toggle(string key, SelectionState requested)
		{
			if(string.IsNullOrWhiteSpace(key) || requested == SelectionState.None)
				return;
			_states[key] = Get(key) == requested ? SelectionState.None : requested;
		}

		public void Reset() => _states.Clear();

		public bool Matches(IEnumerable<string> races, bool isAllRace, bool alwaysHighlightAll)
		{
			if(!HasSelection)
				return false;
			if(isAllRace && alwaysHighlightAll)
				return true;

			var keys = new HashSet<string>(races.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
			if(keys.Count == 0 && !isAllRace)
				keys.Add(NeutralKey);

			if(keys.Any(x => Get(x) == SelectionState.Exclude))
				return false;
			if(isAllRace && HasExcludedRaceKey())
				return false;
			if(HasIncludes)
				return isAllRace || keys.Any(x => Get(x) == SelectionState.Include);
			return true;
		}

		private bool HasExcludedRaceKey()
			=> _states.Any(x => x.Value == SelectionState.Exclude && !x.Key.Equals(NeutralKey, StringComparison.OrdinalIgnoreCase));
	}
}
