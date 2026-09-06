using System;
using System.Collections.Generic;
using System.Linq;

namespace HDTHighlightMinionType
{
	internal sealed class ShopSnapshot<T>
	{
		private static readonly TimeSpan MaximumAge = TimeSpan.FromMilliseconds(250);
		private DateTime? _readAt;
		public IReadOnlyList<T> Cards { get; private set; } = Array.Empty<T>();

		public bool IsCurrent(DateTime now)
			=> _readAt.HasValue && now >= _readAt.Value && now - _readAt.Value <= MaximumAge;

		public void Invalidate()
		{
			_readAt = null;
			Cards = Array.Empty<T>();
		}

		public void Read(Func<IEnumerable<T>?> provider, DateTime now)
		{
			// Failed reads must never retain cards from the previous Tavern.
			Invalidate();
			var incoming = provider();
			if(incoming == null) return;
			Cards = incoming.ToArray();
			_readAt = now;
		}
	}
}
