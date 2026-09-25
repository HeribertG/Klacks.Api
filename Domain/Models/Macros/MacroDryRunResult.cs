// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Result of a macro dry-run on the real entries of every holder in scope (the shifts of a cut group or one absence
/// type): how many live, non-scenario entries they have, how many of them are sealed (and therefore never recalculated),
/// and a comparison on the most recent open entries.
/// </summary>
/// <param name="TotalEntries">Live, non-scenario works of the shifts or breaks of the absence type in scope</param>
/// <param name="SealedEntries">Entries whose lock level is not None</param>
/// <param name="Samples">Most recent open entries, evaluated with the current and the new macro of their holder</param>
/// <param name="NewMacroError">Why a new macro cannot run (missing or does not compile); null when all can</param>
/// <param name="BudgetExceeded">True when the time budget stopped the sampling early</param>
public record MacroDryRunResult(
    int TotalEntries,
    int SealedEntries,
    IReadOnlyList<MacroDryRunSample> Samples,
    string? NewMacroError,
    bool BudgetExceeded)
{
    public int OpenEntries => TotalEntries - SealedEntries;

    public int ChangedSamples => Samples.Count(sample => sample.Changes);

    public static MacroDryRunResult NewMacroFailed(int totalEntries, int sealedEntries, string error) =>
        new(totalEntries, sealedEntries, Array.Empty<MacroDryRunSample>(), error, false);
}
