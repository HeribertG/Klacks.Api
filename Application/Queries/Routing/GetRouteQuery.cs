// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Routing;

public record GetRouteQuery(List<RoutePointResource> Coordinates) : IRequest<List<RoutePointResource>?>;
