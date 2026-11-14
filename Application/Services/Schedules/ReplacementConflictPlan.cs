// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the conflict check of a replacement write has to look at. A new replacement adds its whole window. An update
/// that keeps person and Work and only shortens the window, or leaves it as it is, adds nothing and is not checked. An
/// update that goes beyond the stored window adds the whole new window and removes the whole stored one: the stored
/// replacement is not in the substitute's timeline, so the day and week limits need the new window in one piece, and it
/// already counts in the substitute's period hours, so the stored window has to come off them. The removal carries the id
/// of the stored change, which no Work of the substitute has, so it only nets the hours and never takes a real Work out
/// of the timeline.
/// </summary>
/// <param name="Added">Windows the substitute newly occupies</param>
/// <param name="Removed">Windows the substitute no longer occupies after the update</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ReplacementConflictPlan(
    IReadOnlyList<PlannedWorkRow> Added,
    IReadOnlyList<PlannedRemovalRow> Removed)
{
    /// <summary>
    /// Builds the conflict plan for a replacement write: the whole window is added for a new change or one that goes beyond the stored window (which is then removed), nothing for an update inside it.
    /// </summary>
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
        var whole = ToAddedRow(person, parentWork, windowStart, windowEnd);

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
            return new ReplacementConflictPlan([whole], []);
        }

        if (windowStart >= storedStart && windowEnd <= storedEnd)
        {
            return new ReplacementConflictPlan([], []);
        }

        return new ReplacementConflictPlan([whole], [ToRemovedRow(person, storedWorkChange.Id, storedStart, storedEnd)]);
    }

    private static PlannedWorkRow ToAddedRow(Guid person, Work parentWork, DateTime start, DateTime end)
        => new(person, DateOnly.FromDateTime(start), TimeOnly.FromDateTime(start), TimeOnly.FromDateTime(end), parentWork.ShiftId);

    private static PlannedRemovalRow ToRemovedRow(Guid person, Guid storedChangeId, DateTime start, DateTime end)
        => new(person, DateOnly.FromDateTime(start), TimeOnly.FromDateTime(start), TimeOnly.FromDateTime(end), storedChangeId);
}
