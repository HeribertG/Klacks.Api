// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What WeeklyTourPlanner needs to plan the tours of a group.
/// </summary>
/// <param name="GroupIds">The group whose direct services are planned</param>
/// <param name="GroupShiftIds">Ids of the services linked directly to the group</param>
/// <param name="TourCount">Number of tours, at least 2</param>
/// <param name="FromTime">Start of the day's time budget; null = the earliest start of the candidate services</param>
/// <param name="UntilTime">End of the day's time budget; null = the latest end of the candidate services</param>
/// <param name="FromDate">First day of validity of the tours; null = the earliest first day of the candidate services</param>
/// <param name="UntilDate">Last day of validity of the tours; null = open-ended</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WeeklyTourRequest(
    IReadOnlyCollection<Guid> GroupIds,
    IReadOnlyCollection<Guid> GroupShiftIds,
    int TourCount,
    TimeOnly? FromTime,
    TimeOnly? UntilTime,
    DateOnly? FromDate,
    DateOnly? UntilDate);
