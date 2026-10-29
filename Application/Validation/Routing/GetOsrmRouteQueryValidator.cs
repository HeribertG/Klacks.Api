// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates an OSRM route geometry request with the waypoint rules shared with the OpenRouteService directions proxy.
/// </summary>
/// <param name="Coordinates">Waypoints in visiting order (2 to 50, latitude and longitude within range)</param>
/// <param name="TransportMode">Must be a defined container transport mode</param>

using FluentValidation;
using Klacks.Api.Application.Queries.Routing;

namespace Klacks.Api.Application.Validation.Routing;

public class GetOsrmRouteQueryValidator : AbstractValidator<GetOsrmRouteQuery>
{
    public GetOsrmRouteQueryValidator()
    {
        this.AddRouteWaypointRules(query => query.Coordinates);
        RuleFor(query => query.TransportMode).IsInEnum();
    }
}
