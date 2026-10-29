// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A visit placed by the container visit scheduler.
/// </summary>
/// <param name="Item">The scheduled visit item</param>
/// <param name="StartMinutes">Start of the work in minutes since the container day's midnight; may exceed 1440</param>
/// <param name="EndMinutes">End of the work in minutes; may exceed 1440</param>
/// <param name="TravelBeforeMinutes">Travel time before the visit; after a break only the part after the break</param>
/// <param name="TravelAfterMinutes">New travel time after the visit, or null when the item keeps its stored value</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public record ScheduledVisit(
    VisitItem Item,
    int StartMinutes,
    int EndMinutes,
    int TravelBeforeMinutes,
    int? TravelAfterMinutes);
