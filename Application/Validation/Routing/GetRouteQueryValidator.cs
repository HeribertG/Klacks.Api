// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Queries.Routing;

namespace Klacks.Api.Application.Validation.Routing;

public class GetRouteQueryValidator : AbstractValidator<GetRouteQuery>
{
    private const int MinimumWaypoints = 2;
    private const int MaximumWaypoints = 50;
    private const double MinimumLatitude = -90;
    private const double MaximumLatitude = 90;
    private const double MinimumLongitude = -180;
    private const double MaximumLongitude = 180;

    public GetRouteQueryValidator()
    {
        RuleFor(query => query.Coordinates)
            .NotNull()
            .Must(coordinates => coordinates.Count >= MinimumWaypoints)
            .WithMessage($"A route needs at least {MinimumWaypoints} waypoints.")
            .Must(coordinates => coordinates.Count <= MaximumWaypoints)
            .WithMessage($"A route accepts at most {MaximumWaypoints} waypoints.");

        RuleForEach(query => query.Coordinates).ChildRules(coordinate =>
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
