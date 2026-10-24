// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// What a full-day order with a given start and split covers at the edges of its period.
/// </summary>
/// <param name="FirstDayUncoveredUntil">On the first day the time from midnight up to this time is not covered; null
/// when the first day is covered from midnight</param>
/// <param name="LastDayEndsNextDayAt">The last service runs into the day after the last day up to this time; null when
/// the last day ends at midnight</param>
public sealed record FullDayCoverage(TimeOnly? FirstDayUncoveredUntil, TimeOnly? LastDayEndsNextDayAt);
