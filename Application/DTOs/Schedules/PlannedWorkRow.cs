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
public sealed record PlannedWorkRow(
    Guid ClientId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? ShiftId = null)
{
    /// <summary>
    /// The row a Work occupies once it is written: its employee, day, times and shift.
    /// </summary>
    /// <param name="work">The Work about to be written</param>
    public static PlannedWorkRow From(Work work)
        => new(work.ClientId, work.CurrentDate, work.StartTime, work.EndTime, work.ShiftId);
}
