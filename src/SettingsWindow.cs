using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HDTHighlightMinionType
{
	internal sealed class SettingsWindow : Window
	{
		private readonly PluginSettings _settings;
		private readonly Action _apply;
		private readonly Action _reset;
		private readonly DispatcherTimer _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
		private readonly TextBlock _opacityValue = new TextBlock();
		private readonly TextBlock _scaleValue = new TextBlock();
		private readonly TextBlock _borderOpacityValue = new TextBlock();
		private readonly TextBlock _highlightOpacityValue = new TextBlock();
		private readonly TextBlock _dimValue = new TextBlock();
		private readonly System.Windows.Controls.TextBox _colorText = new System.Windows.Controls.TextBox { MinWidth = 105, Margin = new Thickness(4, 0, 4, 0) };
		private readonly Border _colorPreview = new Border { Width = 28, Height = 22, CornerRadius = new CornerRadius(4), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
		private readonly TextBlock _updateStatus = new TextBlock { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
		private readonly Border _previewMatch = CreatePreviewCard("MATCH");
		private readonly Border _previewOther = CreatePreviewCard("OTHER");

		public SettingsWindow(PluginSettings settings, Action apply, Action reset)
		{
			_settings = settings;
			_apply = apply;
			_reset = reset;
			Title = "Highlight Minion Type — Options";
			Width = 540;
			Height = 790;
			ResizeMode = ResizeMode.NoResize;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
			_saveTimer.Tick += (_, __) => FlushSave();
			Closed += (_, __) => FlushSave();
			Content = BuildContent();
			UpdateLabels();
		}

		private UIElement BuildContent()
		{
			var content = new StackPanel { Margin = new Thickness(8, 8, 8, 4) };
			content.Children.Add(SliderRow("Overlay opacity", 25, 100, _settings.Opacity * 100, _opacityValue, value => _settings.Opacity = value / 100));
			content.Children.Add(SliderRow("Overlay scale", 60, 200, _settings.Scale * 100, _scaleValue, value => _settings.Scale = value / 100));
			var reset = new System.Windows.Controls.Button { Content = "Reset position", HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 4, 0, 16) };
			reset.Click += (_, __) => _reset();
			content.Children.Add(reset);
			var locked = new System.Windows.Controls.CheckBox { Content = "Lock overlay position", IsChecked = _settings.LockOverlayPosition, Margin = new Thickness(0, -8, 0, 16) };
			locked.Click += (_, __) => { _settings.LockOverlayPosition = locked.IsChecked == true; Changed(); };
			content.Children.Add(locked);
			var hideInCombat = new System.Windows.Controls.CheckBox { Content = "Hide overlay during combat", IsChecked = _settings.HideOverlayDuringCombat, Margin = new Thickness(0, -8, 0, 16) };
			hideInCombat.Click += (_, __) => { _settings.HideOverlayDuringCombat = hideInCombat.IsChecked == true; Changed(); };
			content.Children.Add(hideInCombat);

			content.Children.Add(new TextBlock { Text = "Highlight", FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
			var styleNames = new[] { "Glow/Border", "Dim others", "Combo" };
			var style = new System.Windows.Controls.ComboBox { ItemsSource = styleNames, SelectedIndex = (int)_settings.HighlightStyle, MinWidth = 180 };
			style.SelectionChanged += (_, __) => { if(style.SelectedIndex >= 0) { _settings.HighlightStyle = (HighlightStyle)style.SelectedIndex; Changed(); UpdateLabels(); } };
			content.Children.Add(Labeled("Highlight style", style));

			_colorText.Text = _settings.GlowColor;
			_colorText.LostFocus += (_, __) => ApplyTypedColor();
			var choose = new System.Windows.Controls.Button { Content = "Choose…", Padding = new Thickness(8, 3, 8, 3) };
			choose.Click += ChooseColor;
			var colorPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
			colorPanel.Children.Add(_colorPreview); colorPanel.Children.Add(_colorText); colorPanel.Children.Add(choose);
			content.Children.Add(Labeled("Glow color", colorPanel));
			content.Children.Add(SliderRow("Border opacity", 0, 100, _settings.BorderOpacity * 100, _borderOpacityValue, value => _settings.BorderOpacity = value / 100));
			content.Children.Add(SliderRow("Highlight opacity", 0, 100, _settings.HighlightOpacity * 100, _highlightOpacityValue, value => _settings.HighlightOpacity = value / 100));
			content.Children.Add(SliderRow("Dim strength", 0, 100, _settings.DimStrength * 100, _dimValue, value => _settings.DimStrength = value / 100));

			var previewPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 12) };
			previewPanel.Children.Add(_previewMatch);
			previewPanel.Children.Add(_previewOther);
			var preview = new StackPanel();
			preview.Children.Add(new TextBlock { Text = "Highlight preview", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 2) });
			preview.Children.Add(previewPanel);
			content.Children.Add(preview);

			var all = new System.Windows.Controls.CheckBox { Content = "Always highlight All-type cards", IsChecked = _settings.AlwaysHighlightAll, Margin = new Thickness(0, 8, 0, 4) };
			all.Click += (_, __) => { _settings.AlwaysHighlightAll = all.IsChecked == true; Changed(); };
			content.Children.Add(all);
			var neutral = new System.Windows.Controls.CheckBox { Content = "Show Neutral button", IsChecked = _settings.ShowNeutral, Margin = new Thickness(0, 4, 0, 16) };
			neutral.Click += (_, __) => { _settings.ShowNeutral = neutral.IsChecked == true; Changed(); };
			content.Children.Add(neutral);
			var ignoreSpells = new System.Windows.Controls.CheckBox { Content = "Ignore Tavern spells", IsChecked = _settings.IgnoreSpells, Margin = new Thickness(0, -8, 0, 16) };
			ignoreSpells.Click += (_, __) => { _settings.IgnoreSpells = ignoreSpells.IsChecked == true; Changed(); };
			content.Children.Add(ignoreSpells);

			var version = new TextBlock { Text = "Version " + DisplayVersion, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)), Margin = new Thickness(9, 0, 0, 0) };
			var check = new System.Windows.Controls.Button { Content = "↻", ToolTip = "Check for updates", Width = 24, Height = 24, FontSize = 14, Padding = new Thickness(0) };
			check.Click += async (_, __) =>
			{
				check.IsEnabled = false;
				_updateStatus.Text = "Checking GitHub Releases…";
				try
				{
					var result = await UpdateChecker.CheckAsync(DisplayVersion);
					_updateStatus.Text = result.Message;
					if(result.IsNewer)
					{
						var open = System.Windows.MessageBox.Show(result.Message + "\n\nOpen the Releases page?", "Update available", MessageBoxButton.YesNo, MessageBoxImage.Information);
						if(open == MessageBoxResult.Yes)
						{
							try { Process.Start(new ProcessStartInfo(UpdateChecker.ReleasesUrl) { UseShellExecute = true }); }
							catch(Exception ex)
							{
								Diagnostics.Warn("Could not open the Releases page: " + ex.Message);
								System.Windows.MessageBox.Show("Could not open the Releases page.\n\n" + ex.Message, "Update check", MessageBoxButton.OK, MessageBoxImage.Warning);
							}
						}
					}
				}
				catch(Exception ex)
				{
					_updateStatus.Text = "Update check failed: " + ex.Message;
					Diagnostics.WarnThrottled("update-ui", "Update check failed: " + ex.Message, TimeSpan.FromSeconds(30));
				}
				finally { check.IsEnabled = true; }
			};
			var versionRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
			versionRow.Children.Add(check); versionRow.Children.Add(version);
			var close = new System.Windows.Controls.Button { Content = "Close", IsDefault = true, Width = 80, Height = 27, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
			close.Click += (_, __) => Close();

			var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
			footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			footer.Children.Add(versionRow); Grid.SetColumn(close, 1); footer.Children.Add(close);

			var root = new Grid { Margin = new Thickness(14) };
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			var scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			root.Children.Add(scroll);
			Grid.SetRow(_updateStatus, 1); root.Children.Add(_updateStatus);
			Grid.SetRow(footer, 2); root.Children.Add(footer);
			return root;
		}

		private FrameworkElement SliderRow(string label, double min, double max, double value, TextBlock valueText, Action<double> setter)
		{
			var slider = new System.Windows.Controls.Slider { Minimum = min, Maximum = max, Value = value, Width = 220, TickFrequency = 1, IsSnapToTickEnabled = false };
			slider.ValueChanged += (_, __) => { setter(slider.Value); Changed(); UpdateLabels(); };
			var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
			panel.Children.Add(slider); valueText.Width = 55; valueText.TextAlignment = TextAlignment.Right; panel.Children.Add(valueText);
			return Labeled(label, panel);
		}

		private static FrameworkElement Labeled(string label, UIElement value)
		{
			var grid = new Grid { Margin = new Thickness(0, 4, 0, 7) };
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(value, 1); grid.Children.Add(text); grid.Children.Add(value); return grid;
		}

		private void ChooseColor(object sender, RoutedEventArgs e)
		{
			using(var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true })
			{
				var current = (Color)ColorConverter.ConvertFromString(_settings.GlowColor);
				dialog.Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B);
				if(dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
				_settings.GlowColor = $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
				_colorText.Text = _settings.GlowColor; Changed(); UpdateLabels();
			}
		}

		private void ApplyTypedColor()
		{
			try { ColorConverter.ConvertFromString(_colorText.Text); _settings.GlowColor = _colorText.Text; Changed(); }
			catch { _colorText.Text = _settings.GlowColor; }
			UpdateLabels();
		}

		private void Changed()
		{
			_settings.Normalize();
			try { _apply(); }
			catch(Exception ex) { Diagnostics.WarnThrottled("apply-settings", "Could not apply settings: " + ex.Message, TimeSpan.FromSeconds(30)); }
			_saveTimer.Stop();
			_saveTimer.Start();
		}

		private void FlushSave()
		{
			_saveTimer.Stop();
			_settings.Save();
		}

		private void UpdateLabels()
		{
			_opacityValue.Text = $"{_settings.Opacity * 100:0}%";
			_scaleValue.Text = $"{_settings.Scale * 100:0}%";
			_borderOpacityValue.Text = $"{_settings.BorderOpacity * 100:0}%";
			_highlightOpacityValue.Text = $"{_settings.HighlightOpacity * 100:0}%";
			_dimValue.Text = $"{_settings.DimStrength * 100:0}%";
			_colorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.GlowColor));
			UpdatePreview();
		}

		private void UpdatePreview()
		{
			var color = (Color)ColorConverter.ConvertFromString(_settings.GlowColor);
			var glow = _settings.HighlightStyle == HighlightStyle.GlowBorder || _settings.HighlightStyle == HighlightStyle.Combo;
			var dim = _settings.HighlightStyle == HighlightStyle.DimOthers || _settings.HighlightStyle == HighlightStyle.Combo;
			var effectiveBorderOpacity = _settings.HighlightOpacity * _settings.BorderOpacity;
			var previewColor = Color.FromArgb((byte)Math.Round(color.A * effectiveBorderOpacity), color.R, color.G, color.B);
			_previewMatch.BorderBrush = glow ? new SolidColorBrush(previewColor) : Brushes.Transparent;
			_previewMatch.BorderThickness = glow ? new Thickness(3) : new Thickness(1);
			_previewMatch.Opacity = 1;
			_previewMatch.Effect = glow ? new System.Windows.Media.Effects.DropShadowEffect { Color = color, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.8 * effectiveBorderOpacity } : null;
			_previewOther.Background = new SolidColorBrush(dim
				? Color.FromArgb((byte)Math.Round(255 * _settings.DimStrength * _settings.HighlightOpacity), 13, 16, 21)
				: Color.FromRgb(62, 76, 96));
			_previewOther.BorderBrush = new SolidColorBrush(Color.FromRgb(118, 133, 153));
			_previewOther.Opacity = 1;
		}

		private static Border CreatePreviewCard(string text)
		{
			return new Border
			{
				Width = 74,
				Height = 102,
				Margin = new Thickness(12, 5, 12, 5),
				CornerRadius = new CornerRadius(30),
				Background = new SolidColorBrush(Color.FromRgb(62, 76, 96)),
				BorderBrush = new SolidColorBrush(Color.FromRgb(118, 133, 153)),
				BorderThickness = new Thickness(1),
				Child = new TextBlock { Text = text, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 10, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
			};
		}

		private static string DisplayVersion => PluginMetadata.DisplayVersion;
	}
}
