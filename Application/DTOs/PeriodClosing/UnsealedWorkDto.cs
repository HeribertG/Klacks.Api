// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// One work of a period that is not sealed yet, as the refusal of a period close lists it.
/// </summary>
/// <param name="WorkId">Id of the work</param>
/// <param name="ClientId">Id of the person the work belongs to</param>
/// <param name="ClientName">Display name of that person</param>
/// <param name="Date">Day of the work</param>
/// <param name="StartTime">Start of the service</param>
/// <param name="EndTime">End of the service</param>
/// <param name="ShiftName">Name of the shift</param>
public sealed record UnsealedWorkDto(
    Guid WorkId,
    Guid ClientId,
    string ClientName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ShiftName);
