// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The result of WeeklyTourPlanner: the tours, the resolved time budget and validity, and the customers that could not be
/// planned.
/// </summary>
/// <param name="Tours">The tours, west to east</param>
/// <param name="FromTime">Start of the day's time budget</param>
/// <param name="UntilTime">End of the day's time budget</param>
/// <param name="FromDate">First day of validity of the tours</param>
/// <param name="UntilDate">Last day of validity of the tours; null = open-ended</param>
/// <param name="Unplanned">Customers and services left out, each with its reason</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WeeklyTourPlan(
    IReadOnlyList<PlannedWeeklyTour> Tours,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    DateOnly FromDate,
    DateOnly? UntilDate,
    IReadOnlyList<WeeklyTourCustomer> Unplanned);
