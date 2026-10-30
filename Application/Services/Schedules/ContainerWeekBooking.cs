// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One date assign_container_week books the container on.
/// </summary>
/// <param name="Date">The calendar date</param>
/// <param name="ExpectedVisitItems">Number of task visits the template of that date holds; above zero, the booking is
/// only confirmed when the container work has sub-works after the write</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ContainerWeekBooking(DateOnly Date, int ExpectedVisitItems);
