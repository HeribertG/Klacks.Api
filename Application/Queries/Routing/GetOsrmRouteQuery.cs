// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Routing;

public record GetOsrmRouteQuery(List<RoutePointResource> Coordinates) : IRequest<RouteGeometryResource>;
