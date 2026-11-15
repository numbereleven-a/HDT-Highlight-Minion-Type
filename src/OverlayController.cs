using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Utility.Extensions;
using HearthMirror.Objects;
using CardType = HearthDb.Enums.CardType;
using HdtCard = Hearthstone_Deck_Tracker.Hearthstone.Card;

namespace HDTHighlightMinionType
{
	internal sealed class OverlayController
	{
		private readonly PluginSettings _settings;
		private readonly SelectionLogic _selection;
		private readonly Canvas _effects = new Canvas { IsHitTestVisible = false };
		private readonly HoverSuppressionTracker _hoverSuppression = new HoverSuppressionTracker();
		private readonly Border _panel;
		private readonly Border _dragHandle;
		private readonly StackPanel _buttons = new StackPanel { Orientation = Orientation.Horizontal };
		private readonly Dictionary<string, OverlayActionButton> _typeButtons = new Dictionary<string, OverlayActionButton>(StringComparer.OrdinalIgnoreCase);
		private readonly HearthMirrorBoardStateProvider _boardStateProvider = new HearthMirrorBoardStateProvider();
		private readonly ShopSnapshot<ShopCardInfo> _mirrorSnapshot = new ShopSnapshot<ShopCardInfo>();
		private Canvas? _attachedCanvas;
		private Point _dragStart;
		private bool _dragging;
		private string _raceSignature = "";
		private string _effectSignature = "";
		private bool? _lastShowNeutral;
		private DateTime _nextRaceRefreshUtc;
		private DateTime _nextMirrorRefreshUtc;
		private bool _effectsEnabled;
		private bool _shopHovered;
		private bool _highlightLayerSuppressed;
		private static readonly BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly object GeometryMemberSync = new object();
		private static Type? _geometryOwnerType;
		private static FieldInfo? _opponentBoardField;
		private static MethodInfo? _getBoardHoverTargetsMethod;
		private static readonly Brush NormalBackground = CreateBrush(Color.FromRgb(38, 47, 61));
		private static readonly Brush NormalBorder = CreateBrush(Color.FromRgb(82, 96, 116));
		private static readonly Brush IncludeBackground = CreateBrush(Color.FromRgb(18, 97, 126));
		private static readonly Brush IncludeBorder = CreateBrush(Color.FromRgb(82, 220, 255));
		private static readonly Brush ExcludeBackground = CreateBrush(Color.FromRgb(115, 34, 43));
		private static readonly Brush ExcludeBorder = CreateBrush(Color.FromRgb(255, 91, 105));

		public OverlayController(PluginSettings settings, SelectionLogic selection)
		{
			_settings = settings;
			_selection = selection;
			var body = new StackPanel { Orientation = Orientation.Horizontal };
			_dragHandle = new Border
			{
				Background = new SolidColorBrush(Color.FromRgb(35, 43, 55)),
				CornerRadius = new CornerRadius(8, 0, 0, 8),
				Padding = new Thickness(8, 7, 7, 7),
				Child = new TextBlock { Text = "TYPES", Foreground = Brushes.White, FontSize = 9, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center }
			};
			_dragHandle.MouseLeftButtonDown += BeginDrag;
			_dragHandle.MouseMove += Drag;
			_dragHandle.MouseLeftButtonUp += EndDrag;
			_dragHandle.LostMouseCapture += LostMouseCapture;
			body.Children.Add(_dragHandle);
			body.Children.Add(_buttons);
			_panel = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(235, 13, 18, 27)),
				BorderBrush = new SolidColorBrush(Color.FromArgb(210, 94, 112, 137)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(9),
				Padding = new Thickness(3),
				Child = body,
				RenderTransformOrigin = new Point(0, 0)
			};
			OverlayExtensions.SetIsOverlayHitTestVisible(_panel, true);
		}

		public void Attach()
		{
			var canvas = Core.OverlayCanvas;
			_attachedCanvas = canvas;
			canvas.SizeChanged += CanvasSizeChanged;
			if(!canvas.Children.Contains(_effects)) canvas.Children.Add(_effects);
			if(!canvas.Children.Contains(_panel)) canvas.Children.Add(_panel);
			Canvas.SetZIndex(_effects, 900);
			Canvas.SetZIndex(_panel, 1000);
			ApplySettings();
		}

		public void Detach()
		{
			var canvas = _attachedCanvas ?? Core.OverlayCanvas;
			_dragHandle.ReleaseMouseCapture();
			_dragging = false;
			OverlayExtensions.SetIsOverlayHitTestVisible(_panel, false);
			if(canvas != null)
			{
				canvas.SizeChanged -= CanvasSizeChanged;
				canvas.Children.Remove(_panel);
				canvas.Children.Remove(_effects);
			}
			ClearEffects();
			_attachedCanvas = null;
		}

		public void ApplySettings()
		{
			_settings.Normalize();
			_panel.Opacity = _settings.Opacity;
			_panel.RenderTransform = new ScaleTransform(_settings.Scale, _settings.Scale);
			Canvas.SetLeft(_panel, _settings.Left);
			Canvas.SetTop(_panel, _settings.Top);
			ApplyLockState();
			_effectSignature = "";
			UpdateHighlightLayer(DateTime.UtcNow);
			RefreshButtons(force: string.IsNullOrEmpty(_raceSignature) || _lastShowNeutral != _settings.ShowNeutral);
			_panel.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ClampPosition));
		}

		public void ResetPosition()
		{
			_settings.Left = 80;
			_settings.Top = 80;
			Canvas.SetLeft(_panel, _settings.Left);
			Canvas.SetTop(_panel, _settings.Top);
			ClampPosition();
			_settings.Save();
		}

		public void ResetSelection()
		{
			InvalidateShop();
			_selection.Reset();
			_effectSignature = "";
			ClearEffects();
			UpdateButtonStyles();
		}

		public void RefreshInteractivityRegistration()
		{
			OverlayExtensions.SetIsOverlayHitTestVisible(_panel, false);
			OverlayExtensions.SetIsOverlayHitTestVisible(_panel, true);
			InvalidateShop();
		}

		private void InvalidateShop()
		{
			_mirrorSnapshot.Invalidate();
			_shopHovered = false;
			_nextMirrorRefreshUtc = DateTime.MinValue;
			_effectSignature = "";
			ClearEffects();
		}

		public void Update(bool visible, bool effectsEnabled)
		{
			var now = DateTime.UtcNow;
			_effectsEnabled = visible && effectsEnabled;
			_panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
			_buttons.IsEnabled = visible;
			_buttons.Opacity = 1.0;
			_buttons.ToolTip = null;
			if(visible && effectsEnabled) RefreshMirrorShop(force: false);
			else InvalidateShop();
			UpdateHighlightLayer(now);
			_effects.Visibility = visible && effectsEnabled && _selection.HasSelection ? Visibility.Visible : Visibility.Collapsed;
			if(!visible || !effectsEnabled)
			{
				ClearEffects();
				_effectSignature = "";
				return;
			}

			RefreshButtons(force: false);
			RefreshEffects();
		}

		public void RefreshButtons(bool force)
		{
			var now = DateTime.UtcNow;
			if(!force && now < _nextRaceRefreshUtc) return;
			_nextRaceRefreshUtc = now.AddSeconds(1);
			var races = RaceResolver.Available();
			var signature = string.Join("|", races.Select(x => x.Key)) + "|N=" + _settings.ShowNeutral;
			if(!force && signature == _raceSignature) return;
			_raceSignature = signature;
			_lastShowNeutral = _settings.ShowNeutral;
			_buttons.Children.Clear();
			_typeButtons.Clear();
			foreach(var race in races) AddTypeButton(race.Key, race.ShortName, race.FullName);
			if(_settings.ShowNeutral) AddTypeButton(SelectionLogic.NeutralKey, "N", "Neutral (no minion type)");
			var reset = MakeButton("↺", "Reset all selections");
			reset.LabelFontSize = 17;
			reset.LabelFontWeight = FontWeights.Bold;
			reset.Click += (_, e) =>
			{
				if(e.ChangedButton == MouseButton.Left)
					RunUiAction("reset-selection", () => { ResetSelection(); RefreshEffects(); });
			};
			_buttons.Children.Add(reset);
			UpdateButtonStyles();
			_panel.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ClampPosition));
		}

		private void AddTypeButton(string key, string text, string tooltip)
		{
			var button = MakeButton(text, tooltip + "\nLeft-click: include. Right-click: exclude.");
			button.Click += (_, e) =>
			{
				var state = OverlayClickPolicy.SelectionFor(e.ChangedButton);
				if(state.HasValue)
					RunUiAction("type-selection", () => Toggle(key, state.Value));
			};
			_typeButtons[key] = button;
			_buttons.Children.Add(button);
		}

		private void Toggle(string key, SelectionState state)
		{
			_selection.Toggle(key, state);
			_effectSignature = "";
			UpdateButtonStyles();
			RefreshEffects(forceMirrorRefresh: true);
		}

		private static OverlayActionButton MakeButton(string text, string tooltip)
		{
			return new OverlayActionButton(text, tooltip)
			{
				Background = NormalBackground,
				BorderBrush = NormalBorder
			};
		}

		private void UpdateButtonStyles()
		{
			foreach(var pair in _typeButtons)
			{
				var state = _selection.Get(pair.Key);
				pair.Value.Background = state == SelectionState.Include ? IncludeBackground : state == SelectionState.Exclude ? ExcludeBackground : NormalBackground;
				pair.Value.BorderBrush = state == SelectionState.Include ? IncludeBorder : state == SelectionState.Exclude ? ExcludeBorder : NormalBorder;
				pair.Value.LabelFontStyle = state == SelectionState.Exclude ? FontStyles.Italic : FontStyles.Normal;
			}
		}

		private void RefreshEffects(bool forceMirrorRefresh = false)
		{
			if(!_effectsEnabled || !_selection.HasSelection)
			{
				_effectSignature = "";
				ClearEffects();
				return;
			}
			var canvas = Core.OverlayCanvas;
			var width = canvas.ActualWidth;
			var height = canvas.ActualHeight;
			if(width <= 0 || height <= 0) return;
			_effects.Width = width;
			_effects.Height = height;
			RefreshMirrorShop(forceMirrorRefresh);
			var shopRegions = GetBoardRegions("OppBoardItemsControl", canvas);
			var current = _mirrorSnapshot.IsCurrent(DateTime.UtcNow);
			var source = ShopCardSourcePolicy.Select(current, _mirrorSnapshot.Cards.Count, shopRegions.Count);
			var shop = source == ShopCardSource.HearthMirror ? _mirrorSnapshot.Cards : Array.Empty<ShopCardInfo>();
			var geometrySignature = string.Join(";", shopRegions.Select(x => $"{x.X:0.0},{x.Y:0.0},{x.Width:0.0},{x.Height:0.0}"));
			var signature = source + ":" + string.Join(";", shop.Select(x => x.EntityId + ":" + x.CardId + ":" + x.ZonePosition))
				+ "|" + string.Join(";", _selection.States.OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Value))
				+ "|G:" + geometrySignature
				+ $"|{width:0.0}x{height:0.0}|{_settings.HighlightStyle}|{_settings.GlowColor}|{_settings.BorderOpacity:0.000}|{_settings.HighlightOpacity:0.000}|{_settings.DimStrength:0.000}|{_settings.AlwaysHighlightAll}|{_settings.IgnoreSpells}";
			if(signature == _effectSignature) return;
			if(source == ShopCardSource.None)
			{
				_effectSignature = "";
				ClearEffects();
				Diagnostics.WarnThrottled("shop-card-source",
					$"Waiting for the current visible Tavern snapshot (current={current}, mirror={_mirrorSnapshot.Cards.Count}, regions={shopRegions.Count}).",
					TimeSpan.FromSeconds(10));
				return;
			}
			_effectSignature = signature;
			ClearEffects();
			var glowColor = (Color)ColorConverter.ConvertFromString(_settings.GlowColor);

			for(var i = 0; i < shop.Count; i++)
				AddCardEffect(shop[i], shopRegions[i], glowColor);
		}

		private void AddCardEffect(ShopCardInfo shopCard, Rect region, Color glowColor)
		{
			var isMinion = shopCard.IsMinion;
			var match = false;
			if(isMinion && shopCard.Card != null)
			{
				var races = RaceResolver.GetRaceKeys(shopCard.Card, out var all);
				match = _selection.Matches(races, all, _settings.AlwaysHighlightAll);
			}
			var effect = HighlightLogic.Resolve(isMinion, shopCard.IsSpell, match, _settings.IgnoreSpells, _settings.HighlightStyle);
			if(effect == HighlightEffect.Glow) AddGlow(region, glowColor);
			else if(effect == HighlightEffect.Dim) AddDim(region);
		}

		private void RefreshMirrorShop(bool force)
		{
			var now = DateTime.UtcNow;
			if(!force && now < _nextMirrorRefreshUtc) return;
			_nextMirrorRefreshUtc = now.AddMilliseconds(50);
			try
			{
				_shopHovered = false;
				_mirrorSnapshot.Read(() =>
				{
					var opposing = _boardStateProvider.BoardState?.Opposing;
					if(opposing?.BoardCards == null) return null;
					var visibleCards = opposing.BoardCards
						.Where(x => x.ZonePosition > 0 && !string.IsNullOrWhiteSpace(x.CardId))
						.OrderBy(x => x.ZonePosition)
						.ToList();
					_shopHovered = visibleCards.Any(x => x.Hovered);
					return visibleCards.Select(ShopCardInfo.FromMirror).ToList();
				}, now);
			}
			catch(Exception ex)
			{
				Diagnostics.WarnThrottled("mirror-shop", "Could not read the visible Tavern snapshot: " + ex.Message, TimeSpan.FromSeconds(30));
			}
		}

		private void AddGlow(Rect rect, Color color)
		{
			var border = new Border
			{
				Width = rect.Width + 6,
				Height = rect.Height + 6,
				CornerRadius = new CornerRadius(Math.Max(10, rect.Width * 0.45)),
				BorderThickness = new Thickness(Math.Max(2, rect.Width * 0.025)),
				BorderBrush = CreateBrush(color),
				Background = Brushes.Transparent,
				Opacity = _settings.HighlightOpacity * _settings.BorderOpacity,
				Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = color, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.8 * _settings.BorderOpacity },
				IsHitTestVisible = false
			};
			Canvas.SetLeft(border, rect.X - 3);
			Canvas.SetTop(border, rect.Y - 3);
			_effects.Children.Add(border);
		}

		private void AddDim(Rect rect)
		{
			var dim = new Border
			{
				Width = rect.Width,
				Height = rect.Height,
				CornerRadius = new CornerRadius(Math.Max(8, rect.Width * 0.45)),
				Background = CreateBrush(Color.FromArgb((byte)Math.Round(255 * _settings.DimStrength * _settings.HighlightOpacity), 0, 0, 0)),
				IsHitTestVisible = false
			};
			Canvas.SetLeft(dim, rect.X);
			Canvas.SetTop(dim, rect.Y);
			_effects.Children.Add(dim);
		}

		private void UpdateHighlightLayer(DateTime nowUtc)
		{
			var hoverSuppressed = _hoverSuppression.Update(_shopHovered, nowUtc);
			var suppressed = _effectsEnabled && _settings.HideHighlightOnHover && hoverSuppressed;
			if(suppressed == _highlightLayerSuppressed) return;
			_highlightLayerSuppressed = suppressed;
			var target = suppressed ? 0.0 : 1.0;
			var current = _effects.Opacity;
			_effects.BeginAnimation(UIElement.OpacityProperty, null);
			_effects.Opacity = target;
			if(!_effectsEnabled) return;
			_effects.BeginAnimation(UIElement.OpacityProperty,
				new DoubleAnimation(current, target, TimeSpan.FromMilliseconds(suppressed ? 60 : 100)) { FillBehavior = FillBehavior.Stop },
				HandoffBehavior.SnapshotAndReplace);
		}

		private void ClearEffects() => _effects.Children.Clear();

		private static List<Rect> GetBoardRegions(string fieldName, Canvas canvas)
		{
			var result = new List<Rect>();
			try
			{
				var overlay = Core.OverlayWindow;
				ResolveGeometryMembers(overlay.GetType(), fieldName);
				var control = _opponentBoardField?.GetValue(overlay) as ItemsControl;
				var method = _getBoardHoverTargetsMethod;
				if(control == null || method?.Invoke(overlay, new object[] { control }) is not IEnumerable targets) return result;
				foreach(var target in targets.OfType<Ellipse>())
					if(TryGetElementBounds(target, canvas, out var bounds)) result.Add(bounds);
			}
			catch(Exception ex) { Diagnostics.WarnThrottled("board-geometry", "Could not read board card geometry: " + ex.Message, TimeSpan.FromSeconds(30)); }
			return result;
		}

		private static void ResolveGeometryMembers(Type ownerType, string fieldName)
		{
			lock(GeometryMemberSync)
			{
				if(_geometryOwnerType == ownerType) return;
				_geometryOwnerType = ownerType;
				_opponentBoardField = ownerType.GetField(fieldName, InstanceFields);
				_getBoardHoverTargetsMethod = ownerType.GetMethod("GetBoardHoverTargets", InstanceFields);
				if(_opponentBoardField == null || _getBoardHoverTargetsMethod == null)
					Diagnostics.WarnOnce("board-geometry-members", "This HDT version does not expose the expected board geometry members; highlighting is disabled.");
			}
		}

		private static bool TryGetElementBounds(FrameworkElement element, Canvas canvas, out Rect bounds)
		{
			bounds = Rect.Empty;
			if(!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0) return false;
			try
			{
				bounds = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
				return !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0;
			}
			catch(InvalidOperationException) { return false; }
		}

		private sealed class ShopCardInfo
		{
			private ShopCardInfo(int? entityId, string cardId, int zonePosition, HdtCard? card, bool isMinion, bool isSpell)
			{
				EntityId = entityId;
				CardId = cardId;
				ZonePosition = zonePosition;
				Card = card;
				IsMinion = isMinion;
				IsSpell = isSpell;
			}

			public int? EntityId { get; }
			public string CardId { get; }
			public int ZonePosition { get; }
			public HdtCard? Card { get; }
			public bool IsMinion { get; }
			public bool IsSpell { get; }

			public static ShopCardInfo FromMirror(BoardCard boardCard)
			{
				var card = Database.GetCardFromId(boardCard.CardId);
				var type = card?.TypeEnum;
				return new ShopCardInfo(boardCard.EntityId, boardCard.CardId, boardCard.ZonePosition, card,
					type == CardType.MINION,
					type == CardType.SPELL || type == CardType.BATTLEGROUND_SPELL);
			}
		}

		private void BeginDrag(object sender, MouseButtonEventArgs e)
		{
			if(_settings.LockOverlayPosition) return;
			_dragging = true;
			_dragStart = e.GetPosition(Core.OverlayCanvas);
			((UIElement)sender).CaptureMouse();
			e.Handled = true;
		}

		private void Drag(object sender, MouseEventArgs e)
		{
			if(_settings.LockOverlayPosition || !_dragging) return;
			if(e.LeftButton != MouseButtonState.Pressed) { FinishDrag(sender as UIElement, save: true); return; }
			var point = e.GetPosition(Core.OverlayCanvas);
			_settings.Left += point.X - _dragStart.X;
			_settings.Top += point.Y - _dragStart.Y;
			_dragStart = point;
			ClampPosition();
		}

		private void EndDrag(object sender, MouseButtonEventArgs e)
		{
			if(!_dragging) return;
			FinishDrag(sender as UIElement, save: true);
			e.Handled = true;
		}

		private void LostMouseCapture(object sender, MouseEventArgs e)
		{
			if(_dragging) FinishDrag(null, save: true);
		}

		private void FinishDrag(UIElement? captureOwner, bool save)
		{
			if(!_dragging) return;
			_dragging = false;
			captureOwner?.ReleaseMouseCapture();
			if(save) _settings.Save();
		}

		private void CanvasSizeChanged(object sender, SizeChangedEventArgs e) => ClampPosition();

		private void ClampPosition()
		{
			var canvas = _attachedCanvas;
			if(canvas == null || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;
			var maxLeft = Math.Max(0, canvas.ActualWidth - _panel.ActualWidth * _settings.Scale);
			var maxTop = Math.Max(0, canvas.ActualHeight - _panel.ActualHeight * _settings.Scale);
			_settings.Left = Math.Min(maxLeft, Math.Max(0, _settings.Left));
			_settings.Top = Math.Min(maxTop, Math.Max(0, _settings.Top));
			Canvas.SetLeft(_panel, _settings.Left);
			Canvas.SetTop(_panel, _settings.Top);
		}

		private static void RunUiAction(string key, Action action)
		{
			try { action(); }
			catch(Exception ex) { Diagnostics.WarnThrottled(key, "Overlay action failed: " + ex.Message, TimeSpan.FromSeconds(30)); }
		}

		private static SolidColorBrush CreateBrush(Color color)
		{
			var brush = new SolidColorBrush(color);
			brush.Freeze();
			return brush;
		}

		private void ApplyLockState()
		{
			_dragHandle.Cursor = _settings.LockOverlayPosition ? Cursors.Arrow : Cursors.SizeAll;
			_dragHandle.ToolTip = _settings.LockOverlayPosition ? "Overlay position is locked" : "Drag to move the overlay";
		}
	}
}
