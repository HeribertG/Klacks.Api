// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Narrows a container autofill result to a subset of its picked visits without optimizing again: the dropped visits
/// leave the route, the kept ones stay in their order, and every leg, distance and travel time is read again from the
/// result's own distance and duration matrices, so no figure still runs through a dropped stop. The route keeps its
/// start and end base. The remaining budget moves by the change of the estimated travel and on-site time; when the
/// builder had clamped an over-budget remainder to zero, the narrowed remainder is too large by that overrun (the
/// autofill selects within the budget, so this does not occur for its own results).
/// Segment directions and placed time blocks belong to the old route and are dropped (null).
/// </summary>
/// <param name="result">Autofill result whose OptimizedRoute and FullRouteIndices run from the start base over the
/// visits to the end base</param>
/// <param name="keptShiftIds">Shift ids of the visits to keep; the bases (Guid.Empty) are always kept</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public static class ContainerAutofillRouteFilter
{
    public static ContainerAutofillResult KeepShifts(ContainerAutofillResult result, IReadOnlySet<Guid> keptShiftIds)
    {
        if (result.OptimizedRoute.Count != result.FullRouteIndices.Count)
        {
            throw new ArgumentException(
                "The optimized route and its matrix indices must have the same length.", nameof(result));
        }

        var keptPositions = Enumerable.Range(0, result.OptimizedRoute.Count)
            .Where(position => IsKept(result.OptimizedRoute[position], keptShiftIds))
            .ToList();

        var route = keptPositions.Select(position => result.OptimizedRoute[position]).ToList();
        var fullRouteIndices = keptPositions.Select(position => result.FullRouteIndices[position]).ToList();
        var visits = route.Where(location => location.ShiftId != Guid.Empty).ToList();

        if (visits.Count == 0)
        {
            return ContainerAutofillResultBuilder.CreateEmptyResult(result.TransportMode, result.TotalAvailableShifts);
        }

        var distanceKm = 0d;
        var travelSeconds = 0d;
        for (var i = 0; i < fullRouteIndices.Count - 1; i++)
        {
            distanceKm += result.DistanceMatrix[fullRouteIndices[i], fullRouteIndices[i + 1]];
            travelSeconds += result.DurationMatrix[fullRouteIndices[i], fullRouteIndices[i + 1]];
        }

        var first = fullRouteIndices[0];
        var second = fullRouteIndices[1];
        var secondToLast = fullRouteIndices.SkipLast(1).Last();
        var last = fullRouteIndices[^1];

        var totalWorkTime = visits.Aggregate(TimeSpan.Zero, (sum, location) => sum + location.TotalOnSiteTime);
        var estimatedTravelTime = TimeSpan.FromSeconds(travelSeconds) + totalWorkTime;
        var remainingTime = result.RemainingTime + result.EstimatedTravelTime - estimatedTravelTime;

        return result with
        {
            OptimizedRoute = route,
            SelectedShiftIds = visits.Select(location => location.ShiftId).ToList(),
            SelectedShiftCount = visits.Count,
            TotalDistanceKm = Math.Round(distanceKm, ContainerAutofillDefaults.TotalDistanceDecimals),
            EstimatedTravelTime = estimatedTravelTime,
            TotalWorkTime = totalWorkTime,
            TotalBriefingDebriefingTime = totalWorkTime,
            RemainingTime = remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero,
            TravelTimeFromStartBase = TimeSpan.FromSeconds(result.DurationMatrix[first, second]),
            DistanceFromStartBaseKm = result.DistanceMatrix[first, second],
            TravelTimeToEndBase = TimeSpan.FromSeconds(result.DurationMatrix[secondToLast, last]),
            DistanceToEndBaseKm = result.DistanceMatrix[secondToLast, last],
            RouteIndices = fullRouteIndices.Skip(1).SkipLast(1).ToList(),
            FullRouteIndices = fullRouteIndices,
            SegmentDirections = null,
            PlacedTimeBlocks = null
        };
    }

    private static bool IsKept(Location location, IReadOnlySet<Guid> keptShiftIds) =>
        location.ShiftId == Guid.Empty || keptShiftIds.Contains(location.ShiftId);
}
