// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    Guid? ShiftId = null);
