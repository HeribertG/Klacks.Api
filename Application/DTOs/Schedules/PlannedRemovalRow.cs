// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A work interval a not-yet-committed plan vacates from a client, so the guardrail evaluates the
/// resulting state instead of a double-booked intermediate. Without this, one half of a swap looks
/// like an added shift on an agent who is in truth handing another one away.
/// </summary>
/// <param name="ClientId">Agent the interval is taken away from.</param>
/// <param name="Date">Day of the assignment.</param>
/// <param name="StartTime">Start of the vacated interval.</param>
/// <param name="EndTime">End of the vacated interval.</param>
/// <param name="WorkId">Id of the work being vacated; matched by interval when null.</param>
public sealed record PlannedRemovalRow(
    Guid ClientId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? WorkId = null);
