// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One tour as WeeklyTourPlanner plans it: the customers assigned to it and, per ISO weekday, the single-weekday services of
/// those customers that run on that day. All services of a customer are in the same tour.
/// </summary>
/// <param name="Number">1-based number of the tour, counted from west to east</param>
/// <param name="ClientIds">The customers assigned to the tour</param>
/// <param name="ShiftsByWeekday">The services of the tour per ISO weekday; only weekdays with at least one service</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record PlannedWeeklyTour(
    int Number,
    IReadOnlyList<Guid> ClientIds,
    IReadOnlyDictionary<int, List<Shift>> ShiftsByWeekday);
