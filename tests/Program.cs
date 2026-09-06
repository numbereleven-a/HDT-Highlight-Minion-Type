using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using HDTHighlightMinionType;

internal static class Program
{
	private static int _failures;

	private static void Main()
	{
		NothingSelectedDoesNotMatch();
		IncludesAndMultiRaceWork();
		ExcludeHasPriority();
		ExcludeOnlyIsReverseMode();
		NeutralWorks();
		AllRaceWorks();
		ToggleIsThreeStateAware();
		VersionComparisonWorks();
		BetaTagParsingIsAnchored();
		TavernSpellHighlightingWorks();
		BattlegroundsSessionsAreDetected();
		ShoppingPhaseSurvivesReconnectReplay();
		CombatPhaseSurvivesReconnectReplay();
		NewGameAfterMenuDoesNotUseReconnectPhase();
		AtomicWritePreservesExistingFileOnFailure();
		SettingsMigrationEnablesSpellIgnoring();
		CurrentSettingsPreserveSpellIgnoringChoice();
		VisibleShopSourceSurvivesReconnectReset();
		HighlightLayerStaysHiddenBetweenHoveredCards();
		HoverHidingDefaultsOff();
		OverlayClicksMapToSelectionModes();
		AllDoesNotMatchNeutralOnly();
		RequiredSnapshotRejectsCachedCards();
		SnapshotReadsInvalidateOldCards();
		RecoveryRequiresConfirmedShopping();

		if(_failures > 0)
			throw new Exception(_failures + " tests failed.");
		Console.WriteLine("All tests passed.");
	}

	private static void NothingSelectedDoesNotMatch()
	{
		var sut = new SelectionLogic();
		Check(!sut.Matches(new[] { "BEAST" }, false, true), nameof(NothingSelectedDoesNotMatch));
	}

	private static void IncludesAndMultiRaceWork()
	{
		var sut = new SelectionLogic();
		sut.Toggle("BEAST", SelectionState.Include);
		Check(sut.Matches(new[] { "DEMON", "BEAST" }, false, true), nameof(IncludesAndMultiRaceWork), "a dual-type card should match an included race");
		Check(!sut.Matches(new[] { "DEMON" }, false, true), nameof(IncludesAndMultiRaceWork), "an unrelated race should not match");
	}

	private static void ExcludeHasPriority()
	{
		var sut = new SelectionLogic();
		sut.Toggle("BEAST", SelectionState.Include);
		sut.Toggle("DEMON", SelectionState.Exclude);
		Check(!sut.Matches(new[] { "BEAST", "DEMON" }, false, true), nameof(ExcludeHasPriority));
	}

	private static void ExcludeOnlyIsReverseMode()
	{
		var sut = new SelectionLogic();
		sut.Toggle("DEMON", SelectionState.Exclude);
		Check(sut.Matches(new[] { "BEAST" }, false, true), nameof(ExcludeOnlyIsReverseMode), "non-excluded races should match in exclude-only mode");
		Check(!sut.Matches(new[] { "DEMON" }, false, true), nameof(ExcludeOnlyIsReverseMode), "the excluded race should not match");
	}

	private static void NeutralWorks()
	{
		var sut = new SelectionLogic();
		sut.Toggle(SelectionLogic.NeutralKey, SelectionState.Include);
		Check(sut.Matches(Array.Empty<string>(), false, true), nameof(NeutralWorks), "a typeless card should match Neutral");
		Check(!sut.Matches(new[] { "BEAST" }, false, true), nameof(NeutralWorks), "a typed card should not match Neutral");
	}

	private static void AllRaceWorks()
	{
		var sut = new SelectionLogic();
		sut.Toggle("BEAST", SelectionState.Include);
		Check(sut.Matches(Array.Empty<string>(), true, true), nameof(AllRaceWorks), "AlwaysHighlightAll should override exclusions");
		sut.Toggle("DEMON", SelectionState.Exclude);
		Check(sut.Matches(Array.Empty<string>(), true, true), nameof(AllRaceWorks), "AlwaysHighlightAll should continue to override exclusions");
		Check(!sut.Matches(Array.Empty<string>(), true, false), nameof(AllRaceWorks), "an All-type card should honor exclusions when the override is disabled");
	}

	private static void ToggleIsThreeStateAware()
	{
		var sut = new SelectionLogic();
		sut.Toggle("BEAST", SelectionState.Include);
		sut.Toggle("BEAST", SelectionState.Include);
		Check(sut.Get("BEAST") == SelectionState.None, nameof(ToggleIsThreeStateAware));
		sut.Toggle("BEAST", SelectionState.Exclude);
		Check(sut.Get("BEAST") == SelectionState.Exclude, nameof(ToggleIsThreeStateAware));
	}

	private static void VersionComparisonWorks()
	{
		Check(UpdateChecker.Compare("1.8", "1.7b") > 0, nameof(VersionComparisonWorks), "a newer stable version should compare greater");
		Check(UpdateChecker.Compare("1.7", "1.7b") > 0, nameof(VersionComparisonWorks), "stable should compare greater than beta with the same core version");
		Check(UpdateChecker.Compare("1.7b2", "1.7b1") > 0, nameof(VersionComparisonWorks), "beta sequence numbers should be ordered");
		Check(UpdateChecker.Compare("v1.7.0", "1.7") == 0, nameof(VersionComparisonWorks), "prefixes and trailing zero components should compare equally");
	}

	private static void BetaTagParsingIsAnchored()
	{
		Check(UpdateChecker.Compare("2.0-bugfix", "2.0") == 0, nameof(BetaTagParsingIsAnchored), "the word bugfix must not be treated as a beta marker");
		Check(UpdateChecker.Compare("2.0-beta.1", "2.0") < 0, nameof(BetaTagParsingIsAnchored), "an explicit beta suffix should compare below stable");
	}

	private static void TavernSpellHighlightingWorks()
	{
		Check(HighlightLogic.Resolve(false, true, false, true, HighlightStyle.Combo) == HighlightEffect.None,
			nameof(TavernSpellHighlightingWorks), "ignored Tavern spells should receive no effect");
		Check(HighlightLogic.Resolve(false, true, false, false, HighlightStyle.Combo) == HighlightEffect.Dim,
			nameof(TavernSpellHighlightingWorks), "non-ignored Tavern spells should be dimmed in Combo mode");
		Check(HighlightLogic.Resolve(true, false, true, true, HighlightStyle.Combo) == HighlightEffect.Glow,
			nameof(TavernSpellHighlightingWorks), "matching minions should glow in Combo mode");
		Check(HighlightLogic.Resolve(true, false, false, true, HighlightStyle.Combo) == HighlightEffect.Dim,
			nameof(TavernSpellHighlightingWorks), "non-matching minions should be dimmed in Combo mode");
	}

	private static void BattlegroundsSessionsAreDetected()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var sut = new BattlegroundsSessionTracker();
		Check(sut.Update(true, false, "match-1", first) == BattlegroundsSessionChange.Started, nameof(BattlegroundsSessionsAreDetected), "the first active update should start a session");
		Check(sut.Update(true, false, "match-1", first) == BattlegroundsSessionChange.None, nameof(BattlegroundsSessionsAreDetected), "the same game should not start twice");
		Check(sut.Update(false, true, "match-1", null) == BattlegroundsSessionChange.None, nameof(BattlegroundsSessionsAreDetected), "a transient menu state should not immediately discard the stable match id");
		Check(sut.Update(true, false, "match-1", second) == BattlegroundsSessionChange.Reconnected, nameof(BattlegroundsSessionsAreDetected), "a changed transient game id with the same server match should be treated as reconnect");
		Check(sut.Update(true, false, "match-1", second) == BattlegroundsSessionChange.None, nameof(BattlegroundsSessionsAreDetected), "the reconnected game should stabilize without another reset");
		Check(sut.Update(false, true, "match-1", null) == BattlegroundsSessionChange.None, nameof(BattlegroundsSessionsAreDetected), "entering the menu should not reset selections by itself");
		Check(sut.Update(true, false, "match-2", Guid.NewGuid()) == BattlegroundsSessionChange.Started, nameof(BattlegroundsSessionsAreDetected), "a changed server match should start a new session");
	}

	private static void ShoppingPhaseSurvivesReconnectReplay()
	{
		var first = Guid.NewGuid();
		var reconnect = Guid.NewGuid();
		var now = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
		var sut = new BattlegroundsPhaseTracker();
		Check(!sut.Resolve(true, first, false, true, now), nameof(ShoppingPhaseSurvivesReconnectReplay), "shopping should be detected before reconnect");
		Check(!sut.Resolve(false, null, false, false, now.AddSeconds(1)), nameof(ShoppingPhaseSurvivesReconnectReplay), "the brief disconnect should not invent combat");
		Check(!sut.Resolve(true, reconnect, true, false, now.AddSeconds(2)), nameof(ShoppingPhaseSurvivesReconnectReplay), "a replayed combat flag should not disable the restored shop");
		Check(sut.IsRecovering, nameof(ShoppingPhaseSurvivesReconnectReplay), "phase recovery should remain active while the shopping turn is missing");
		Check(!sut.Resolve(true, reconnect, false, true, now.AddSeconds(45)), nameof(ShoppingPhaseSurvivesReconnectReplay), "the restored shopping marker should complete recovery");
		Check(!sut.IsRecovering, nameof(ShoppingPhaseSurvivesReconnectReplay), "phase recovery should complete after the current shopping turn is restored");
		Check(sut.Resolve(true, reconnect, true, true, now.AddSeconds(50)), nameof(ShoppingPhaseSurvivesReconnectReplay), "a real later combat should still be detected");
	}

	private static void CombatPhaseSurvivesReconnectReplay()
	{
		var first = Guid.NewGuid();
		var reconnect = Guid.NewGuid();
		var now = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
		var sut = new BattlegroundsPhaseTracker();
		Check(sut.Resolve(true, first, true, true, now), nameof(CombatPhaseSurvivesReconnectReplay), "combat should be detected before reconnect");
		sut.Resolve(false, null, false, false, now.AddSeconds(1));
		Check(sut.Resolve(true, reconnect, false, false, now.AddSeconds(2)), nameof(CombatPhaseSurvivesReconnectReplay), "the reset combat flag should not enable effects during a combat reconnect");
		Check(!sut.Resolve(true, reconnect, false, true, now.AddSeconds(15)), nameof(CombatPhaseSurvivesReconnectReplay), "the next restored shopping turn should enable selection again");
	}

	private static void NewGameAfterMenuDoesNotUseReconnectPhase()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var now = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
		var sut = new BattlegroundsPhaseTracker();
		sut.Resolve(true, first, true, true, now);
		sut.Resolve(false, null, false, false, now.AddSeconds(1));
		sut.Resolve(false, null, false, false, now.AddSeconds(31));
		Check(!sut.Resolve(true, second, false, false, now.AddSeconds(32)), nameof(NewGameAfterMenuDoesNotUseReconnectPhase), "a later new game should use its own reported phase");
		Check(!sut.IsRecovering, nameof(NewGameAfterMenuDoesNotUseReconnectPhase), "a normal new game should not enter reconnect recovery");
	}

	private static void AtomicWritePreservesExistingFileOnFailure()
	{
		WithTemporaryDirectory(directory =>
		{
			var path = Path.Combine(directory, "settings.xml");
			File.WriteAllText(path, "original", Encoding.UTF8);
			try
			{
				AtomicFile.Write(path, stream =>
				{
					var partial = Encoding.UTF8.GetBytes("partial");
					stream.Write(partial, 0, partial.Length);
					throw new InvalidOperationException("simulated write failure");
				});
				Check(false, nameof(AtomicWritePreservesExistingFileOnFailure), "the simulated failure should escape the atomic writer");
			}
			catch(InvalidOperationException)
			{
				Check(File.ReadAllText(path, Encoding.UTF8) == "original", nameof(AtomicWritePreservesExistingFileOnFailure), "the previous file should remain intact");
				Check(!Directory.EnumerateFiles(directory, "*.tmp").Any(), nameof(AtomicWritePreservesExistingFileOnFailure), "temporary files should be cleaned up");
			}
		});
	}

	private static void SettingsMigrationEnablesSpellIgnoring()
	{
		WithTemporaryDirectory(directory =>
		{
			var path = Path.Combine(directory, "settings.xml");
			var legacy = new PluginSettings { SettingsVersion = 1500, IgnoreSpells = false };
			using(var stream = File.Create(path))
				new XmlSerializer(typeof(PluginSettings)).Serialize(stream, legacy);
			var migrated = PluginSettings.LoadFrom(path);
			Check(migrated.SettingsVersion == 1700, nameof(SettingsMigrationEnablesSpellIgnoring), "the settings version should be migrated");
			Check(migrated.IgnoreSpells, nameof(SettingsMigrationEnablesSpellIgnoring), "legacy settings should receive the enabled default");
			Check(!migrated.HideHighlightOnHover, nameof(SettingsMigrationEnablesSpellIgnoring), "hover hiding should remain disabled for existing users");
			using(var stream = File.OpenRead(path))
			{
				var persisted = (PluginSettings)new XmlSerializer(typeof(PluginSettings)).Deserialize(stream);
				Check(persisted.SettingsVersion == 1700 && persisted.IgnoreSpells && !persisted.HideHighlightOnHover, nameof(SettingsMigrationEnablesSpellIgnoring), "the migrated values should be persisted to disk");
			}
		});
	}

	private static void CurrentSettingsPreserveSpellIgnoringChoice()
	{
		WithTemporaryDirectory(directory =>
		{
			var path = Path.Combine(directory, "settings.xml");
			new PluginSettings { SettingsVersion = 1700, IgnoreSpells = false, HideHighlightOnHover = true }.SaveTo(path);
			var loaded = PluginSettings.LoadFrom(path);
			Check(!loaded.IgnoreSpells, nameof(CurrentSettingsPreserveSpellIgnoringChoice), "a current user's disabled choice should be preserved");
			Check(loaded.HideHighlightOnHover, nameof(CurrentSettingsPreserveSpellIgnoringChoice), "a current user's hover choice should be preserved");
		});
	}

	private static void VisibleShopSourceSurvivesReconnectReset()
	{
		Check(ShopCardSourcePolicy.Select(true, true, 4, 4, 4) == ShopCardSource.None,
			nameof(VisibleShopSourceSurvivesReconnectReset), "recovery must invalidate cached cards independently of selection");
		Check(ShopCardSourcePolicy.Select(true, false, 0, 7, 7) == ShopCardSource.None,
			nameof(VisibleShopSourceSurvivesReconnectReset), "stale tracked entities must not be used while the visible board is being restored");
		Check(ShopCardSourcePolicy.Select(false, true, 7, 7, 7) == ShopCardSource.HearthMirror,
			nameof(VisibleShopSourceSurvivesReconnectReset), "the current visible board should replace replayed entities after reconnect");
		Check(ShopCardSourcePolicy.Select(false, false, 0, 7, 7) == ShopCardSource.TrackedEntities,
			nameof(VisibleShopSourceSurvivesReconnectReset), "tracked entities should remain a startup fallback when HearthMirror is unavailable");
		Check(ShopCardSourcePolicy.Select(false, true, 6, 7, 7) == ShopCardSource.None,
			nameof(VisibleShopSourceSurvivesReconnectReset), "a mismatched live snapshot must wait instead of falling back to stale replay data");
	}

	private static void HighlightLayerStaysHiddenBetweenHoveredCards()
	{
		var now = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
		var sut = new HoverSuppressionTracker();
		Check(sut.Update(true, now), nameof(HighlightLayerStaysHiddenBetweenHoveredCards), "hovering a Tavern card should suppress highlighting");
		Check(sut.Update(false, now.AddMilliseconds(50)), nameof(HighlightLayerStaysHiddenBetweenHoveredCards), "a short gap between cards should not flash highlighting");
		Check(sut.Update(true, now.AddMilliseconds(80)), nameof(HighlightLayerStaysHiddenBetweenHoveredCards), "hovering the next card should keep highlighting suppressed");
		Check(sut.Update(false, now.AddMilliseconds(200)), nameof(HighlightLayerStaysHiddenBetweenHoveredCards), "the restore delay should begin after leaving the shop");
		Check(!sut.Update(false, now.AddMilliseconds(321)), nameof(HighlightLayerStaysHiddenBetweenHoveredCards), "highlighting should return after the hover animation has ended");
	}

	private static void HoverHidingDefaultsOff()
	{
		Check(!new PluginSettings().HideHighlightOnHover, nameof(HoverHidingDefaultsOff));
	}

	private static void OverlayClicksMapToSelectionModes()
	{
		Check(OverlayClickPolicy.SelectionFor(MouseButton.Left) == SelectionState.Include,
			test: nameof(OverlayClicksMapToSelectionModes), caseName: "left-click should include a type");
		Check(OverlayClickPolicy.SelectionFor(MouseButton.Right) == SelectionState.Exclude,
			test: nameof(OverlayClicksMapToSelectionModes), caseName: "right-click should exclude a type");
		Check(OverlayClickPolicy.SelectionFor(MouseButton.Middle) == null,
			test: nameof(OverlayClicksMapToSelectionModes), caseName: "other mouse buttons should not change a type");
	}

	private static void AllDoesNotMatchNeutralOnly()
	{
		var selection = new SelectionLogic();
		selection.Toggle(SelectionLogic.NeutralKey, SelectionState.Include);
		Check(!selection.Matches(Array.Empty<string>(), true, false), nameof(AllDoesNotMatchNeutralOnly));
		Check(selection.Matches(Array.Empty<string>(), true, true), nameof(AllDoesNotMatchNeutralOnly), "explicit All override");
		selection.Toggle("BEAST", SelectionState.Include);
		Check(selection.Matches(Array.Empty<string>(), true, false), nameof(AllDoesNotMatchNeutralOnly), "real type included");
		selection.Toggle("DEMON", SelectionState.Exclude);
		Check(!selection.Matches(Array.Empty<string>(), true, false), nameof(AllDoesNotMatchNeutralOnly), "real exclusion wins");
		selection.Reset();
		selection.Toggle(SelectionLogic.NeutralKey, SelectionState.Exclude);
		Check(selection.Matches(Array.Empty<string>(), true, false), nameof(AllDoesNotMatchNeutralOnly), "Neutral exclusion does not exclude All");
	}

	private static void RequiredSnapshotRejectsCachedCards()
	{
		Check(ShopCardSourcePolicy.Select(true, true, 2, 2, 2) == ShopCardSource.None,
			nameof(RequiredSnapshotRejectsCachedCards), "old same-size snapshot must not survive invalidation");
	}

	private static void SnapshotReadsInvalidateOldCards()
	{
		var now = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
		var snapshot = new ShopSnapshot<string>();
		foreach(var failure in new[] { "null", "throw", "empty" })
		{
			snapshot.Read(() => new[] { "BEAST", "DEMON" }, now);
			Check(snapshot.IsCurrent(now), nameof(SnapshotReadsInvalidateOldCards));
			snapshot.Invalidate();
			try
			{
				snapshot.Read(() => failure == "throw" ? throw new InvalidOperationException("synthetic")
					: failure == "null" ? null : Array.Empty<string>(), now);
			}
			catch(InvalidOperationException) { }
			Check(snapshot.Cards.Count == 0, nameof(SnapshotReadsInvalidateOldCards), failure);
			Check(snapshot.IsCurrent(now) == (failure == "empty"), nameof(SnapshotReadsInvalidateOldCards), "valid empty differs from failed read");
			Check(ShopCardSourcePolicy.Select(!snapshot.IsCurrent(now), snapshot.IsCurrent(now), snapshot.Cards.Count, 2, 2) == ShopCardSource.None,
				nameof(SnapshotReadsInvalidateOldCards), "same-size old geometry cannot revive old cards");
			snapshot.Read(() => new[] { "DRAGON", "BEAST" }, now);
			Check(snapshot.Cards[1] == "BEAST", nameof(SnapshotReadsInvalidateOldCards), "new same-size list replaces old order");
			Check(!snapshot.IsCurrent(now.AddSeconds(1)), nameof(SnapshotReadsInvalidateOldCards), "expired read");
			// Also exercise failures without an explicit reconnect invalidation.
			snapshot.Read(() => null, now);
			Check(!snapshot.IsCurrent(now) && snapshot.Cards.Count == 0, nameof(SnapshotReadsInvalidateOldCards), "ordinary failed refresh");
		}
	}

	private static void RecoveryRequiresConfirmedShopping()
	{
		var now = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
		var phase = new BattlegroundsPhaseTracker();
		var first = Guid.NewGuid();
		var reconnect = Guid.NewGuid();
		phase.Resolve(true, first, false, true, now);
		Check(phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping));
		phase.Resolve(false, null, false, false, now.AddSeconds(1));
		Check(!phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping));
		phase.Resolve(true, reconnect, true, false, now.AddSeconds(2));
		Check(!phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping), "replay or real combat must not enable effects");
		phase.Resolve(true, reconnect, true, false, now.AddMinutes(2));
		Check(!phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping), "prolonged recovery");
		phase.Resolve(true, reconnect, false, true, now.AddMinutes(3));
		Check(phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping), "confirmed shopping restores effects");
		phase.Resolve(true, reconnect, true, true, now.AddMinutes(4));
		Check(!phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping), "normal combat");
		phase.Reset();
		phase.Resolve(true, Guid.NewGuid(), false, false, now.AddMinutes(5));
		Check(!phase.IsRecovering && !phase.CanHighlight, nameof(RecoveryRequiresConfirmedShopping), "new game does not inherit phase");
	}

	private static void WithTemporaryDirectory(Action<string> test)
	{
		var directory = Path.Combine(Path.GetTempPath(), "HDT-Highlight-Minion-Type.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try { test(directory); }
		finally { if(Directory.Exists(directory)) Directory.Delete(directory, true); }
	}

	private static void Check(bool value, string test, string? caseName = null)
	{
		if(value)
			return;
		_failures++;
		Console.Error.WriteLine("FAILED: " + test + (caseName == null ? "" : " — " + caseName));
	}
}
