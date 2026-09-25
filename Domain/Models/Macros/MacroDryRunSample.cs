// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// One real entry evaluated by the macro dry-run: the value stored today and the values the current and the new macro
/// of its holder compute for exactly the same inputs, without persisting anything. Changes compares what a recalculation
/// would leave behind: a side without a value (no macro, a deleted one, one that does not compile or fails on this entry,
/// or an undo that removes the reference) keeps the stored value in production, so it counts as the stored value.
/// </summary>
/// <param name="EntryId">Id of the work or break the sample was taken from</param>
/// <param name="Date">Calendar date of the entry</param>
/// <param name="StoredValue">Value persisted today (work: surcharges, break: credited hours)</param>
/// <param name="CurrentValue">Value the currently assigned macro computes; null when there is none or it fails</param>
/// <param name="NewValue">Value the new macro computes; null when there is none or it fails</param>
/// <param name="KeepsRecordedValue">True for a break whose duration was recorded directly: no macro ever changes it</param>
public record MacroDryRunSample(
    Guid EntryId,
    DateOnly Date,
    decimal StoredValue,
    decimal? CurrentValue,
    decimal? NewValue,
    bool KeepsRecordedValue)
{
    public bool Changes => !KeepsRecordedValue && (CurrentValue ?? StoredValue) != (NewValue ?? StoredValue);
}
