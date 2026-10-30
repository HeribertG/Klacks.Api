// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A date of the range assign_container_week does not book, with the reason.
/// </summary>
/// <param name="Date">The calendar date</param>
/// <param name="Reason">Why nothing is booked on that date</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ContainerWeekSkip(DateOnly Date, string Reason);
