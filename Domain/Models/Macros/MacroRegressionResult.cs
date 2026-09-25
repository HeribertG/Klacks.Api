// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of comparing an original macro with an extended copy on the regression test grid.
/// </summary>
/// <param name="ComparedSamples">Test inputs on which both scripts ran and were compared</param>
/// <param name="SkippedSamples">Test inputs skipped because the original itself failed on them</param>
/// <param name="Deviations">The first deviations found, capped for readability</param>
/// <param name="TotalDeviationCount">Number of deviations found in total</param>
/// <param name="FailureMessage">Why the check could not be carried out (compile error, copy runtime error, time budget); null when it ran</param>
/// <param name="FailureKind">What aborted the check, so a caller can tell a fault of the copy from a fault of the original or of the check itself; None when it ran</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Macros;

public record MacroRegressionResult(
    int ComparedSamples,
    int SkippedSamples,
    IReadOnlyList<MacroRegressionDeviation> Deviations,
    int TotalDeviationCount,
    string? FailureMessage,
    MacroRegressionFailureKind FailureKind = MacroRegressionFailureKind.None)
{
    public bool Passed => FailureMessage == null && TotalDeviationCount == 0;

    public static MacroRegressionResult Failure(MacroRegressionFailureKind kind, string message) =>
        new(0, 0, Array.Empty<MacroRegressionDeviation>(), 0, message, kind);
}
