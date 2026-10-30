// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Says why the located candidates that are not on a planned route stayed out, with the arrival rule of the route refiner
/// (a stop leaves when the previous one ends, there is no waiting for a window to open): too long for the day alone, no
/// position on the tour whose arrival hits its TimeRange window, or a matching position exists but the tour has no room.
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public static class ContainerAutofillOmissionClassifier
{
    /// <param name="context">Matrix and limits of the plan</param>
    /// <param name="route">Matrix indices of the planned stops</param>
    /// <param name="candidateLocations">The candidates, in matrix order</param>
    public static List<ContainerAutofillOmission> Classify(
        ContainerAutofillRouteContext context, List<int> route, List<Location> candidateLocations)
    {
        var arrivals = ContainerAutofillRouteTiming.CalculateAbsoluteArrivalTimes(
            context.DistanceMatrix, route, context.StartBaseIndex, context.ContainerFromTimeSeconds, context.UnmovableBlocks);

        return Enumerable.Range(0, candidateLocations.Count)
            .Where(candidate => !route.Contains(candidate))
            .Select(candidate => new ContainerAutofillOmission(
                candidateLocations[candidate].ShiftId, ReasonFor(context, route, arrivals, candidate)))
            .ToList();
    }

    private static ContainerAutofillOmissionReason ReasonFor(
        ContainerAutofillRouteContext context, List<int> route, List<double> arrivals, int candidate)
    {
        var durations = context.DistanceMatrix.DurationMatrix;
        var location = context.DistanceMatrix.Locations[candidate];
        var aloneSeconds = durations[context.StartBaseIndex, candidate] + location.TotalOnSiteTime.TotalSeconds
            + durations[candidate, context.EndBaseIndex];

        if (aloneSeconds > context.BudgetSeconds)
        {
            return ContainerAutofillOmissionReason.TooLongForTheDay;
        }

        return Enumerable.Range(0, route.Count + 1).Any(position => ArrivalHitsWindow(context, route, arrivals, candidate, position))
            ? ContainerAutofillOmissionReason.NoRoomOnTour
            : ContainerAutofillOmissionReason.TimeWindowConflict;
    }

    private static bool ArrivalHitsWindow(
        ContainerAutofillRouteContext context, List<int> route, List<double> arrivals, int candidate, int position)
    {
        var matrix = context.DistanceMatrix;
        var previousIndex = position == 0 ? context.StartBaseIndex : route[position - 1];
        var previousEnd = position == 0
            ? context.ContainerFromTimeSeconds
            : arrivals[position - 1] + matrix.Locations[route[position - 1]].TotalOnSiteTime.TotalSeconds;

        return ContainerAutofillRouteTiming.IsTimeRangeValid(
            matrix.Locations[candidate], previousEnd + matrix.DurationMatrix[previousIndex, candidate], context.TimeRangeTolerance);
    }
}
