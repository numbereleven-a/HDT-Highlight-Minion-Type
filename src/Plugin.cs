using System;
using System.Windows;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.Plugins;

namespace HDTHighlightMinionType
{
	public sealed class Plugin : IPlugin
	{
		private readonly SelectionLogic _selection = new SelectionLogic();
		private readonly BattlegroundsSessionTracker _session = new BattlegroundsSessionTracker();
		private readonly BattlegroundsPhaseTracker _phase = new BattlegroundsPhaseTracker();
		private PluginSettings _settings = new PluginSettings();
		private OverlayController? _overlay;
		private SettingsWindow? _settingsWindow;
		private MenuItem? _menuItem;
		private bool _loaded;

		public string Name => "Highlight Minion Type";
		public string Description => "Highlights Battlegrounds minions shown in Bob's Tavern by type during shopping.";
		public string ButtonText => "Options";
		public string Author => "numbereleven-a";
		public Version Version => PluginMetadata.Version;
		public MenuItem MenuItem => _menuItem ??= BuildMenu();

		public void OnLoad()
		{
			_settings = PluginSettings.Load();
			_session.Reset();
			_phase.Reset();
			_selection.Reset();
			InvokeUi(() => { _overlay = new OverlayController(_settings, _selection); _overlay.Attach(); });
			_loaded = true;
			Diagnostics.Info("Loaded version " + PluginMetadata.DisplayVersion + ".");
		}

		public void OnUnload()
		{
			_loaded = false;
			_session.Reset();
			_phase.Reset();
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
			var now = DateTime.UtcNow;
			if(game == null)
			{
				_session.Update(false, false, "", null);
				_phase.Resolve(false, null, false, false, now);
				_overlay.Update(false, false);
				return;
			}
			var inBattlegrounds = game.IsBattlegroundsMatch && !game.IsInMenu;
			var gameHandle = game.MetaData.ServerInfo?.GameHandle ?? 0;
			var stableMatchId = gameHandle > 0 ? gameHandle.ToString() : "";
			var sessionChange = _session.Update(inBattlegrounds, game.IsInMenu, stableMatchId, game.CurrentGameStats?.GameId);
			if(sessionChange == BattlegroundsSessionChange.Started)
			{
				_overlay.ResetSelection();
				Diagnostics.Info("New Battlegrounds match detected; type selections were reset.");
			}
			else if(sessionChange == BattlegroundsSessionChange.Reconnected)
			{
				_overlay.RefreshInteractivityRegistration();
				Diagnostics.Info("Battlegrounds reconnect detected; preserving type selections.");
			}
			var wasRecovering = _phase.IsRecovering;
			var resolvedCombat = _phase.Resolve(inBattlegrounds, game.CurrentGameStats?.GameId,
				game.IsBattlegroundsCombatPhase, game.GameEntityTurnAtShoppingStart >= 0, now);
			var inCombat = inBattlegrounds && resolvedCombat;
			if(!wasRecovering && _phase.IsRecovering)
				Diagnostics.Info("Reconnect detected; preserving the last known " + (inCombat ? "combat" : "shopping") + " phase until HDT restores the current turn.");
			else if(wasRecovering && !_phase.IsRecovering)
				Diagnostics.Info("Battlegrounds phase recovery completed.");
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

		private void ApplyOverlaySettings() => InvokeUi(() => _overlay?.ApplySettings());
		private void ResetOverlayPosition() => InvokeUi(() => _overlay?.ResetPosition());

		private static void InvokeUi(Action action)
		{
			var dispatcher = Application.Current?.Dispatcher;
			if(dispatcher == null || dispatcher.CheckAccess()) action(); else dispatcher.Invoke(action);
		}
	}
}
