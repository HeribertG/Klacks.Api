// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the RouteInfo a container template stores after a route optimization, with the same content the Ui stores
/// from the optimize-route response: TimeSpan values in their JSON wire text ("c" format) and shift ids as text.
/// </summary>
/// <param name="startBase">Start base address the route was optimized from</param>
/// <param name="endBase">End base address the route was optimized to</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Schedules;
using RouteSegmentDirections = Klacks.Api.Domain.Services.RouteOptimization.RouteSegmentDirections;

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public static class ContainerRouteInfoBuilder
{
    public static RouteInfo Build(
        string startBase, string endBase, RouteOptimizationResult result, IReadOnlyList<RouteStep> steps)
    {
        return new RouteInfo
        {
            StartBase = startBase,
            EndBase = endBase,
            TotalDistanceKm = result.TotalDistanceKm,
            EstimatedTravelTime = WireText(result.EstimatedTravelTime),
            TravelTimeFromStartBase = WireText(result.TravelTimeFromStartBase),
            DistanceFromStartBaseKm = result.DistanceFromStartBaseKm,
            DistanceToEndBaseKm = result.DistanceToEndBaseKm,
            TravelTimeToEndBase = WireText(result.TravelTimeToEndBase),
            OptimizedRoute = steps.Select(ToRouteLocation).ToList(),
            SegmentDirections = result.SegmentDirections?.Select(ToSegmentDirections).ToList()
        };
    }

    public static string WireText(TimeSpan value) =>
        value.ToString(ContainerVisitDefaults.TimeSpanWireFormat, CultureInfo.InvariantCulture);

    private static Klacks.Api.Domain.Models.Schedules.RouteSegmentDirections ToSegmentDirections(RouteSegmentDirections segment) => new()
    {
        FromName = segment.FromName,
        ToName = segment.ToName,
        TransportMode = segment.TransportMode,
        DistanceKm = segment.DistanceKm,
        Duration = WireText(segment.Duration),
        Steps = segment.Steps.Select(step => new Klacks.Api.Domain.Models.Schedules.DirectionStep
        {
            Instruction = step.Instruction,
            StreetName = step.StreetName,
            DistanceMeters = step.DistanceMeters,
            DurationSeconds = step.DurationSeconds,
            ManeuverType = step.ManeuverType
        }).ToList()
    };

    private static RouteLocation ToRouteLocation(RouteStep step) => new()
    {
        Name = step.Name,
        Address = step.Address,
        Latitude = step.Latitude,
        Longitude = step.Longitude,
        ShiftId = step.ShiftId.ToString(),
        DistanceToNextKm = step.DistanceToNextKm,
        TravelTimeToNext = WireText(step.TravelTimeToNext),
        Order = step.Order
    };
}
