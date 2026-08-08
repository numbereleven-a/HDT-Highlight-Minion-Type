using System;
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

		if(_failures > 0)
			throw new Exception(_failures + " selection tests failed.");
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

	private static void Check(bool value, string test, string? caseName = null)
	{
		if(value)
			return;
		_failures++;
		Console.Error.WriteLine("FAILED: " + test + (caseName == null ? "" : " — " + caseName));
	}
}
