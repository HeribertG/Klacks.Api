// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the conflict check of a replacement write has to look at. A new replacement adds its whole window. An update
/// that keeps person and Work already has the stored window in the substitute's period hours, so only the time it
/// adds is checked and the time it gives up is taken off the hours; the time it keeps is neither checked nor counted
/// again. The removals carry the id of the stored change, which no Work of the substitute has, so they only net the
/// hours and never take a real Work out of the timeline.
/// </summary>
/// <param name="Added">Windows the substitute newly occupies</param>
/// <param name="Removed">Windows the substitute no longer occupies after the update</param>
/// <param name="NetsAgainstStored">True when the plan belongs to an update of a stored replacement of the same person and Work</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ReplacementConflictPlan(
    IReadOnlyList<PlannedWorkRow> Added,
    IReadOnlyList<PlannedRemovalRow> Removed,
    bool NetsAgainstStored)
{
    /// <param name="workChange">The change about to be written; names the substitute</param>
    /// <param name="storedWorkChange">The change as stored before an update; null on create</param>
    /// <param name="parentWork">The Work the change is attached to</param>
    /// <param name="windowStart">Start of the window the change occupies</param>
    /// <param name="windowEnd">End of the window the change occupies</param>
    public static ReplacementConflictPlan For(
        WorkChange workChange,
        WorkChange? storedWorkChange,
        Work parentWork,
        DateTime windowStart,
        DateTime windowEnd)
    {
        var person = workChange.ReplaceClientId!.Value;

        if (storedWorkChange == null
            || storedWorkChange.WorkId != workChange.WorkId
            || storedWorkChange.ReplaceClientId != workChange.ReplaceClientId
            || !ReplacementWindow.TryGetInterval(
                storedWorkChange.Type,
                parentWork.StartTime,
                parentWork.EndTime,
                parentWork.CurrentDate,
                storedWorkChange.StartTime,
                storedWorkChange.EndTime,
                storedWorkChange.ChangeTime,
                out var storedStart,
                out var storedEnd))
        {
            return new ReplacementConflictPlan([ToAddedRow(person, parentWork, windowStart, windowEnd)], [], false);
        }

        var added = new List<PlannedWorkRow>();
        var removed = new List<PlannedRemovalRow>();
        if (windowEnd <= storedStart || windowStart >= storedEnd)
        {
            added.Add(ToAddedRow(person, parentWork, windowStart, windowEnd));
            removed.Add(ToRemovedRow(person, storedWorkChange.Id, storedStart, storedEnd));
            return new ReplacementConflictPlan(added, removed, true);
        }

        if (windowStart < storedStart)
        {
            added.Add(ToAddedRow(person, parentWork, windowStart, storedStart));
        }

        if (windowEnd > storedEnd)
        {
            added.Add(ToAddedRow(person, parentWork, storedEnd, windowEnd));
        }

        if (storedStart < windowStart)
        {
            removed.Add(ToRemovedRow(person, storedWorkChange.Id, storedStart, windowStart));
        }

        if (windowEnd < storedEnd)
        {
            removed.Add(ToRemovedRow(person, storedWorkChange.Id, windowEnd, storedEnd));
        }

        return new ReplacementConflictPlan(added, removed, true);
    }

    private static PlannedWorkRow ToAddedRow(Guid person, Work parentWork, DateTime start, DateTime end)
        => new(person, DateOnly.FromDateTime(start), TimeOnly.FromDateTime(start), TimeOnly.FromDateTime(end), parentWork.ShiftId);

    private static PlannedRemovalRow ToRemovedRow(Guid person, Guid storedChangeId, DateTime start, DateTime end)
        => new(person, DateOnly.FromDateTime(start), TimeOnly.FromDateTime(start), TimeOnly.FromDateTime(end), storedChangeId);
}
