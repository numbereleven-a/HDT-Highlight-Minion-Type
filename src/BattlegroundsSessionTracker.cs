using System;

namespace HDTHighlightMinionType
{
	internal enum BattlegroundsSessionChange
	{
		None,
		Started,
		Reconnected
	}

	internal sealed class BattlegroundsSessionTracker
	{
		private string _stableMatchId = "";
		private Guid? _gameId;
		private bool _wasActive;
		private bool _hasSession;
		private bool _sawMenu;

		public BattlegroundsSessionChange Update(bool active, bool inMenu, string stableMatchId, Guid? gameId)
		{
			if(!active)
			{
				if(inMenu) _sawMenu = true;
				_wasActive = false;
				return BattlegroundsSessionChange.None;
			}

			stableMatchId = stableMatchId ?? "";
			var stableMatchChanged = !string.IsNullOrEmpty(_stableMatchId) && !string.IsNullOrEmpty(stableMatchId)
				&& !_stableMatchId.Equals(stableMatchId, StringComparison.Ordinal);
			var stableMatchUnchanged = !string.IsNullOrEmpty(_stableMatchId) && !string.IsNullOrEmpty(stableMatchId)
				&& _stableMatchId.Equals(stableMatchId, StringComparison.Ordinal);
			var gameIdChanged = _gameId.HasValue && gameId.HasValue && _gameId.Value != gameId.Value;
			BattlegroundsSessionChange change;
			if(!_hasSession || stableMatchChanged)
				change = BattlegroundsSessionChange.Started;
			else if(gameIdChanged && stableMatchUnchanged)
				change = BattlegroundsSessionChange.Reconnected;
			else if(gameIdChanged)
				change = _sawMenu ? BattlegroundsSessionChange.Started : BattlegroundsSessionChange.Reconnected;
			else if(!_wasActive && _sawMenu && string.IsNullOrEmpty(stableMatchId))
				change = BattlegroundsSessionChange.Started;
			else
				change = BattlegroundsSessionChange.None;

			_hasSession = true;
			_wasActive = true;
			_sawMenu = false;
			if(!string.IsNullOrEmpty(stableMatchId)) _stableMatchId = stableMatchId;
			if(gameId.HasValue) _gameId = gameId;
			return change;
		}

		public void Reset()
		{
			_stableMatchId = "";
			_gameId = null;
			_wasActive = false;
			_hasSession = false;
			_sawMenu = false;
		}
	}
}
