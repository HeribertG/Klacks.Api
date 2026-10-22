// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// What to read from the materialised schedule (get_schedule_entries). The range selects by work day: a night
/// shift's next-day correction comes with its work day, not with the day it lands on. Visible groups never hide a
/// row, they only flag works on a shift of a foreign group as group-restricted.
/// </summary>
/// <param name="From">First work day, inclusive</param>
/// <param name="Until">Last work day, inclusive</param>
/// <param name="VisibleGroupIds">Groups of the caller (incl. subgroups); null or empty = no group restriction flags</param>
/// <param name="AnalyseToken">Scenario token; null = the real plan</param>
public sealed record ScheduleEntriesCriteria(
    DateOnly From,
    DateOnly Until,
    IReadOnlyCollection<Guid>? VisibleGroupIds = null,
    Guid? AnalyseToken = null)
{
    private const int RefreshWindowDaysAroundDay = 1;

    /// <summary>Only rows of these clients; null = every client, empty = no rows.</summary>
    public IReadOnlyCollection<Guid>? ClientIds { get; init; }

    /// <summary>Only rows of this entry type; null = every type.</summary>
    public ScheduleEntryType? EntryType { get; init; }

    /// <summary>Sorts the rows by start time in the database instead of keeping the function's order.</summary>
    public bool OrderByStartTime { get; init; }

    /// <summary>
    /// The three-day window a client's grid row repaints from after a write: the day before, the day and the day after.
    /// </summary>
    /// <param name="day">Work day of the written entry</param>
    /// <param name="clientIds">Clients whose rows are repainted</param>
    /// <param name="visibleGroupIds">Groups of the caller, so cross-group works keep their seal</param>
    /// <param name="analyseToken">Scenario token of the written entry</param>
    public static ScheduleEntriesCriteria RefreshWindowAround(
        DateOnly day,
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<Guid>? visibleGroupIds,
        Guid? analyseToken)
        => new(
            day.AddDays(-RefreshWindowDaysAroundDay),
            day.AddDays(RefreshWindowDaysAroundDay),
            visibleGroupIds,
            analyseToken)
        {
            ClientIds = clientIds,
        };
}
