// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The outcome of planning a week for assign_container_week: the dates to book, the dates left out with their reasons
/// and the non-blocking findings of the booked dates. Every date of the requested range is in exactly one of the first two.
/// </summary>
/// <param name="Bookings">Dates to book, earliest first</param>
/// <param name="Skipped">Dates left out, earliest first</param>
/// <param name="Warnings">Non-blocking findings of the dates to book</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ContainerWeekPlan(
    IReadOnlyList<ContainerWeekBooking> Bookings,
    IReadOnlyList<ContainerWeekSkip> Skipped,
    IReadOnlyList<ContainerWeekWarning> Warnings);
