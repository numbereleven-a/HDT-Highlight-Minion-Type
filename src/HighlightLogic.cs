namespace HDTHighlightMinionType
{
	public enum HighlightStyle
	{
		GlowBorder,
		DimOthers,
		Combo
	}

	internal enum HighlightEffect
	{
		None,
		Glow,
		Dim
	}

	internal static class HighlightLogic
	{
		public static HighlightEffect Resolve(bool isMinion, bool isSpell, bool matches, bool ignoreSpells, HighlightStyle style)
		{
			var supportsGlow = style == HighlightStyle.GlowBorder || style == HighlightStyle.Combo;
			var supportsDim = style == HighlightStyle.DimOthers || style == HighlightStyle.Combo;
			if(!isMinion)
				return isSpell && ignoreSpells ? HighlightEffect.None : supportsDim ? HighlightEffect.Dim : HighlightEffect.None;
			if(matches && supportsGlow) return HighlightEffect.Glow;
			if(!matches && supportsDim) return HighlightEffect.Dim;
			return HighlightEffect.None;
		}
	}
}
