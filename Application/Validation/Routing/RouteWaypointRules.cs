// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Waypoint rules shared by every route proxy request (OpenRouteService directions, OSRM geometry): 2 to 50
/// waypoints, latitude within -90..90 and longitude within -180..180.
/// </summary>
/// <param name="validator">Validator of the route request the rules are added to</param>
/// <param name="waypoints">Selects the waypoint list of the request</param>

using System.Linq.Expressions;
using FluentValidation;
using Klacks.Api.Application.DTOs.Routing;

namespace Klacks.Api.Application.Validation.Routing;

public static class RouteWaypointRules
{
    public const int MinimumWaypoints = 2;
    public const int MaximumWaypoints = 50;
    private const double MinimumLatitude = -90;
    private const double MaximumLatitude = 90;
    private const double MinimumLongitude = -180;
    private const double MaximumLongitude = 180;

    public static void AddRouteWaypointRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, List<RoutePointResource>>> waypoints)
    {
        validator.RuleFor(waypoints)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(coordinates => coordinates.Count >= MinimumWaypoints)
            .WithMessage($"A route needs at least {MinimumWaypoints} waypoints.")
            .Must(coordinates => coordinates.Count <= MaximumWaypoints)
            .WithMessage($"A route accepts at most {MaximumWaypoints} waypoints.");

        var eachWaypoint = Expression.Lambda<Func<T, IEnumerable<RoutePointResource>>>(waypoints.Body, waypoints.Parameters);

        validator.RuleForEach(eachWaypoint).ChildRules(coordinate =>
        {
            coordinate.RuleFor(c => c.Lat)
                .InclusiveBetween(MinimumLatitude, MaximumLatitude)
                .WithMessage("Latitude is out of range.");

            coordinate.RuleFor(c => c.Lon)
                .InclusiveBetween(MinimumLongitude, MaximumLongitude)
                .WithMessage("Longitude is out of range.");
        });
    }
}
