// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One employee/day cell that lies outside the employee's planning period (membership or group period), as the
/// schedule grid would lock it.
/// </summary>
/// <param name="ClientId">Employee the cell belongs to</param>
/// <param name="Date">Day of the cell</param>
/// <param name="Message">User-facing reason naming the employee, the day and the violated period</param>
public sealed record PlanningPeriodViolation(Guid ClientId, DateOnly Date, string Message);
