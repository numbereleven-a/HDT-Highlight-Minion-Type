using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;

namespace HDTHighlightMinionType
{
	public sealed class Plugin : IPlugin
	{
		private readonly SelectionLogic _selection = new SelectionLogic();
		private PluginSettings _settings = new PluginSettings();
		private OverlayController? _overlay;
		private SettingsWindow? _settingsWindow;
		private MenuItem? _menuItem;
		private bool _loaded;
		private bool _eventsHooked;

		public string Name => "Highlight Minion Type";
		public string Description => "Highlights Battlegrounds minions shown in Bob's Tavern by type during shopping.";
		public string ButtonText => "Options";
		public string Author => "numbereleven-a";
		public Version Version => PluginMetadata.Version;
		public MenuItem MenuItem => _menuItem ??= BuildMenu();

		public void OnLoad()
		{
			_settings = PluginSettings.Load();
			InvokeUi(() => { _overlay = new OverlayController(_settings, _selection); _overlay.Attach(); });
			HookEvents();
			_loaded = true;
			Diagnostics.Info("Loaded version " + PluginMetadata.DisplayVersion + ".");
		}

		public void OnUnload()
		{
			_loaded = false;
			UnhookEvents();
			InvokeUi(() =>
			{
				_settingsWindow?.Close(); _settingsWindow = null;
				_overlay?.Detach(); _overlay = null;
			});
			_settings.Save();
			Diagnostics.Info("Unloaded.");
		}

		public void OnButtonPress() => ShowOptions();

		public void OnUpdate()
		{
			if(!_loaded || _overlay == null) return;
			try { InvokeUi(UpdateOverlay); }
			catch(Exception ex) { Diagnostics.WarnThrottled("overlay-update", "Overlay update failed: " + ex.Message, TimeSpan.FromSeconds(30)); }
		}

		private void UpdateOverlay()
		{
			if(!_loaded || _overlay == null) return;
			var game = Hearthstone_Deck_Tracker.API.Core.Game;
			if(game == null)
			{
				_overlay.Update(false, false);
				return;
			}
			var inBattlegrounds = game.IsBattlegroundsMatch && !game.IsInMenu;
			var inCombat = inBattlegrounds && game.IsBattlegroundsCombatPhase;
			var visible = inBattlegrounds && (!inCombat || !_settings.HideOverlayDuringCombat);
			_overlay.Update(visible, inBattlegrounds && !inCombat);
		}

		private MenuItem BuildMenu()
		{
			var root = new MenuItem { Header = "Highlight Minion Type" };
			var options = new MenuItem { Header = "Options" }; options.Click += (_, __) => ShowOptions(); root.Items.Add(options);
			return root;
		}

		private void ShowOptions()
		{
			InvokeUi(() =>
			{
				if(_settingsWindow != null) { _settingsWindow.Activate(); return; }
				_settingsWindow = new SettingsWindow(_settings, ApplyOverlaySettings, ResetOverlayPosition);
				var owner = Application.Current?.MainWindow;
				if(owner != null && owner.IsVisible) _settingsWindow.Owner = owner;
				_settingsWindow.Closed += (_, __) => _settingsWindow = null;
				_settingsWindow.Show();
			});
		}

		private void HookEvents()
		{
			if(_eventsHooked) return;
			GameEvents.OnGameStart.Add(OnNewGame);
			GameEvents.OnGameEnd.Add(Hide);
			GameEvents.OnInMenu.Add(Hide);
			_eventsHooked = true;
		}

		private void OnNewGame() => InvokeUi(() => { _selection.Reset(); _overlay?.Update(false, false); });
		private void Hide() => InvokeUi(() => _overlay?.Update(false, false));

		private void ApplyOverlaySettings() => InvokeUi(() => _overlay?.ApplySettings());
		private void ResetOverlayPosition() => InvokeUi(() => _overlay?.ResetPosition());

		private void UnhookEvents()
		{
			if(!_eventsHooked) return;
			Remove(GameEvents.OnGameStart, (Action)OnNewGame, "OnGameStart");
			Remove(GameEvents.OnGameEnd, (Action)Hide, "OnGameEnd");
			Remove(GameEvents.OnInMenu, (Action)Hide, "OnInMenu");
			_eventsHooked = false;
		}

		private static void Remove(object actionList, Action action, string eventName)
		{
			var field = actionList.GetType().GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic);
			if(!(field?.GetValue(actionList) is IList actions))
			{
				Diagnostics.WarnOnce("unhook-field-" + eventName, "Could not detach " + eventName + ": this HDT version uses an unknown event implementation.");
				return;
			}
			var removed = 0;
			for(var i = actions.Count - 1; i >= 0; i--)
				if(actions[i] is Tuple<object, Action> tuple && tuple.Item2 == action) { actions.RemoveAt(i); removed++; }
			if(removed == 0) Diagnostics.WarnOnce("unhook-empty-" + eventName, "No registered handler was found while detaching " + eventName + ".");
		}

		private static void InvokeUi(Action action)
		{
			var dispatcher = Application.Current?.Dispatcher;
			if(dispatcher == null || dispatcher.CheckAccess()) action(); else dispatcher.Invoke(action);
		}
	}
}
