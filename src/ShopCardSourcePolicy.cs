namespace HDTHighlightMinionType
{
	internal enum ShopCardSource
	{
		None,
		HearthMirror
	}

	internal static class ShopCardSourcePolicy
	{
		public static ShopCardSource Select(bool mirrorAvailable, int mirrorCardCount, int regionCount)
			=> mirrorAvailable && mirrorCardCount == regionCount
				? ShopCardSource.HearthMirror
				: ShopCardSource.None;
	}
}
