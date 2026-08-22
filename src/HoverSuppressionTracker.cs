using System;

namespace HDTHighlightMinionType
{
	internal sealed class HoverSuppressionTracker
	{
		private static readonly TimeSpan RestoreDelay = TimeSpan.FromMilliseconds(120);
		private bool _wasHovered;
		private DateTime _restoreAfterUtc;

		public bool IsSuppressed { get; private set; }

		public bool Update(bool hovered, DateTime nowUtc)
		{
			if(hovered)
			{
				_wasHovered = true;
				IsSuppressed = true;
				return true;
			}
			if(_wasHovered)
			{
				_wasHovered = false;
				_restoreAfterUtc = nowUtc + RestoreDelay;
			}
			if(IsSuppressed && nowUtc >= _restoreAfterUtc)
				IsSuppressed = false;
			return IsSuppressed;
		}
	}
}
