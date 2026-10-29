// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One planned visit of an optimized container route as the optimize_container_route skill reports it.
/// </summary>
/// <param name="Order">1-based position among the visits</param>
/// <param name="Name">Client name of the stop as the route optimizer read it</param>
/// <param name="Address">Address of the stop</param>
/// <param name="VisitStart">Planned start of the work (HH:mm)</param>
/// <param name="VisitEnd">Planned end of the work (HH:mm)</param>
/// <param name="TravelBefore">Travel time before the visit (HH:mm)</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public record ContainerRouteStop(
    int Order,
    Guid ShiftId,
    string Name,
    string Address,
    string VisitStart,
    string VisitEnd,
    string TravelBefore);
