// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One further move of a swap chain behind a cover: an employee who takes over the open slot hands one of their own
/// works on to someone else, so the planner sees everybody the cover touches.
/// </summary>
/// <param name="ShiftId">The shift of the work that is handed on (original shift id)</param>
/// <param name="Date">Workday of that work; it can differ from the day of the covered slot</param>
/// <param name="FromClientId">Employee who gives the work up</param>
/// <param name="FromName">Display name of that employee</param>
/// <param name="ToClientId">Employee who takes the work over</param>
/// <param name="ToName">Display name of that employee</param>
/// <param name="StartTime">Start of the work</param>
/// <param name="EndTime">End of the work</param>
/// <param name="Tier">Escalation tier of the move, same values as on the covered slot</param>
public sealed record CoverRelocation(
    Guid ShiftId,
    DateOnly Date,
    Guid FromClientId,
    string FromName,
    Guid ToClientId,
    string ToName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int Tier);
