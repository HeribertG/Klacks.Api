// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Which tasks the available-task list of a container template slot reads: always real-plan tasks (ShiftType.IsTask,
/// AnalyseToken null) with a status from OriginalShift on that are not sporadic, inside the slot's time frame
/// (ShiftTimeFrameFilter), running on the slot's days (ShiftDayActivityFilter) and, when a period is given, valid
/// during it (ShiftValidityOverlap); soft-deleted shifts are dropped by the query filter.
/// </summary>
/// <param name="Weekday">Stored weekday of the slot (0=Sunday .. 6=Saturday)</param>
/// <param name="FromTime">Start of the slot's time frame</param>
/// <param name="UntilTime">End of the slot's time frame; before FromTime when the frame crosses midnight</param>
public sealed record AvailableTaskCandidateCriteria(int Weekday, TimeOnly FromTime, TimeOnly UntilTime)
{
    /// <summary>True for a holiday slot; false or null for the normal slot.</summary>
    public bool? IsHoliday { get; init; }

    /// <summary>Only read for a holiday slot: true = the holiday slot of the weekday, otherwise the generic one.</summary>
    public bool? IsWeekdayAndHoliday { get; init; }

    /// <summary>Never these shifts (the tasks already used in a container); empty = no exclusion.</summary>
    public IReadOnlyCollection<Guid> ExcludedShiftIds { get; init; } = [];

    /// <summary>Only these shifts (the container's group); null = no restriction, empty = no rows.</summary>
    public IReadOnlyCollection<Guid>? OnlyShiftIds { get; init; }

    /// <summary>First day of the container's validity; a task that ended before it is no candidate. Null = no lower bound.</summary>
    public DateOnly? ValidFrom { get; init; }

    /// <summary>Last day of the container's validity; a task that starts after it is no candidate. Null = no upper bound.</summary>
    public DateOnly? ValidUntil { get; init; }

    /// <summary>Fragment the name, abbreviation or description must contain, compared lower-cased; null or blank = all.</summary>
    public string? SearchFragment { get; init; }
}
