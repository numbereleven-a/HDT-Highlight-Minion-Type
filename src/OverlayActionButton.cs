using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HDTHighlightMinionType
{
	internal sealed class OverlayActionButton : Border
	{
		private readonly TextBlock _label;

		public OverlayActionButton(string text, string tooltip)
		{
			ToolTip = tooltip;
			MinWidth = 31;
			Height = 30;
			Margin = new Thickness(2);
			Padding = new Thickness(5, 1, 5, 1);
			BorderThickness = new Thickness(1);
			CornerRadius = new CornerRadius(4);
			Cursor = Cursors.Hand;
			Focusable = false;

			_label = new TextBlock
			{
				Text = text,
				Foreground = Brushes.White,
				FontSize = 12,
				FontWeight = FontWeights.SemiBold,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};
			Child = _label;
		}

		public event MouseButtonEventHandler? Click;

		public double LabelFontSize
		{
			get => _label.FontSize;
			set => _label.FontSize = value;
		}

		public FontWeight LabelFontWeight
		{
			get => _label.FontWeight;
			set => _label.FontWeight = value;
		}

		public FontStyle LabelFontStyle
		{
			get => _label.FontStyle;
			set => _label.FontStyle = value;
		}

		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			if(IsEnabled) Opacity = 0.88;
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			Opacity = 1.0;
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if(!IsEnabled) return;
			Opacity = 0.68;
			e.Handled = true;
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			if(!IsEnabled) return;
			Opacity = IsMouseOver ? 0.88 : 1.0;
			Click?.Invoke(this, e);
			e.Handled = true;
		}
	}
}
