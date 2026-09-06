using System;

namespace HDTHighlightMinionType
{
	internal sealed class BattlegroundsPhaseTracker
	{
		private static readonly TimeSpan ReconnectGap = TimeSpan.FromSeconds(30);
		private Guid? _gameId;
		private DateTime _lastActiveUtc;
		private bool _wasActive;
		private bool _lastCombat;

		public bool IsRecovering { get; private set; }
		public bool CanHighlight { get; private set; }

		public bool Resolve(bool active, Guid? gameId, bool reportedCombat, bool shoppingTurnRestored, DateTime nowUtc)
		{
			CanHighlight = false;
			if(!active)
			{
				_wasActive = false;
				if(_lastActiveUtc != default && nowUtc - _lastActiveUtc > ReconnectGap)
				{
					_gameId = null;
					IsRecovering = false;
				}
				return reportedCombat;
			}

			var shortGap = _lastActiveUtc != default && nowUtc - _lastActiveUtc <= ReconnectGap;
			var gameChanged = _gameId.HasValue && gameId.HasValue && _gameId.Value != gameId.Value;
			if(gameChanged && (_wasActive || shortGap)) IsRecovering = true;
			else if(!_wasActive && !shortGap) IsRecovering = false;

			_wasActive = true;
			_lastActiveUtc = nowUtc;
			if(gameId.HasValue) _gameId = gameId;

			if(IsRecovering && !shoppingTurnRestored) return _lastCombat;
			IsRecovering = false;
			_lastCombat = reportedCombat;
			CanHighlight = !reportedCombat && shoppingTurnRestored;
			return reportedCombat;
		}

		public void Reset()
		{
			CanHighlight = false;
			_gameId = null;
			_lastActiveUtc = default;
			_wasActive = false;
			_lastCombat = false;
			IsRecovering = false;
		}
	}
}
