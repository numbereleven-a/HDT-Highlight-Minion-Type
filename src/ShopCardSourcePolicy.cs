namespace HDTHighlightMinionType
{
	internal enum ShopCardSource
	{
		None,
		HearthMirror,
		TrackedEntities
	}

	internal static class ShopCardSourcePolicy
	{
		public static ShopCardSource Select(bool mirrorRequired, bool mirrorAvailable,
			int mirrorCardCount, int trackedCardCount, int regionCount)
		{
			if(mirrorRequired) return ShopCardSource.None;
			if(mirrorAvailable && mirrorCardCount == regionCount)
				return ShopCardSource.HearthMirror;
			if(mirrorRequired || mirrorAvailable)
				return ShopCardSource.None;
			return trackedCardCount == regionCount
				? ShopCardSource.TrackedEntities
				: ShopCardSource.None;
		}
	}
}
