// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Routing;

/// <summary>
/// Road route geometry through OpenRouteService.
/// </summary>
/// <param name="Coordinates">Waypoints in visiting order</param>
/// <param name="TransportMode">Transport mode whose routing profile is used; car when omitted</param>
public record GetRouteQuery(
    List<RoutePointResource> Coordinates,
    ContainerTransportMode TransportMode = ContainerTransportMode.ByCar) : IRequest<List<RoutePointResource>?>;
