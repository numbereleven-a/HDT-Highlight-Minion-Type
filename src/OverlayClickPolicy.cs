using System.Windows.Input;

namespace HDTHighlightMinionType
{
	internal static class OverlayClickPolicy
	{
		public static SelectionState? SelectionFor(MouseButton button)
		{
			if(button == MouseButton.Left) return SelectionState.Include;
			if(button == MouseButton.Right) return SelectionState.Exclude;
			return null;
		}
	}
}
