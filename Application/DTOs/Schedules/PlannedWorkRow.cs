// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single not-yet-committed work assignment to be checked by the pre-commit conflict guardrail.
/// </summary>
/// <param name="ClientId">Employee that would be assigned</param>
/// <param name="Date">Workday</param>
/// <param name="StartTime">Shift start (wraps past midnight when EndTime &lt;= StartTime)</param>
/// <param name="EndTime">Shift end</param>
/// <param name="ShiftId">Optional shift id (not required for conflict detection)</param>
/// <param name="LeadHours">Briefing, start correction and start travel hours directly before the start; a TimeOnly cannot express a start before midnight</param>
/// <param name="TrailHours">Debriefing, end correction and end travel hours directly after the end</param>
/// <param name="ExtraPaidHours">Travel and correction hours on top of the shift's own hours; a briefing adds none</param>
public sealed record PlannedWorkRow(
    Guid ClientId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? ShiftId = null,
    decimal LeadHours = 0m,
    decimal TrailHours = 0m,
    decimal ExtraPaidHours = 0m)
{
    /// <summary>
    /// The row a Work occupies once it is written: its employee, day, times and shift.
    /// </summary>
    /// <param name="work">The Work about to be written</param>
    public static PlannedWorkRow From(Work work)
        => new(work.ClientId, work.CurrentDate, work.StartTime, work.EndTime, work.ShiftId);
}
