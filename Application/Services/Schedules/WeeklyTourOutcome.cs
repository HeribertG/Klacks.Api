// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One tour as it was written: its container and what each weekday came to.
/// </summary>
/// <param name="Number">1-based number of the tour</param>
/// <param name="ContainerId">Id of the container shift</param>
/// <param name="Name">Name of the container</param>
/// <param name="Customers">Number of customers assigned to the tour</param>
/// <param name="Days">One entry per weekday with services, in weekday order</param>
/// <param name="PlacedShiftIds">Ids of all services placed in the templates of the tour</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WeeklyTourOutcome(
    int Number,
    Guid ContainerId,
    string Name,
    int Customers,
    IReadOnlyList<WeeklyTourDayOutcome> Days,
    IReadOnlyList<Guid> PlacedShiftIds);
