// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Automatische Befüllung von Container-Templates mit passenden TimeRange-Shifts.
/// Löst ein Orienteering Problem in 3 Phasen: Greedy Selection, ACO+2-Opt, Post-Insertion.
/// </summary>
/// <param name="containerRepository">Repository für Container-Templates (Zeitbudget)</param>
/// <param name="availableTasksService">Service für verfügbare Shifts</param>
/// <param name="routeOptimizationService">Service für Distance-Matrix-Berechnung</param>
/// <param name="geocodingService">Service für Adress-Geocodierung</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class ContainerAutofillService : IContainerAutofillService
{
    private const double SecondsPerMinute = 60;

    private record RouteEvaluationContext(
        DistanceMatrix DistanceMatrix, int StartBaseIndex, int EndBaseIndex,
        double ContainerFromTimeSeconds, double ContainerEndTimeSeconds,
        double BudgetSeconds, double TimeRangeTolerance);

    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly IGeocodingService _geocodingService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<ContainerAutofillService> _logger;

    public ContainerAutofillService(
        IContainerAvailableTasksService availableTasksService,
        IRouteOptimizationService routeOptimizationService,
        IGeocodingService geocodingService,
        ISettingsRepository settingsRepository,
        ILogger<ContainerAutofillService> logger)
    {
        _availableTasksService = availableTasksService;
        _routeOptimizationService = routeOptimizationService;
        _geocodingService = geocodingService;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<ContainerAutofillResult> AutofillAsync(
        Guid containerId,
        int weekday,
        bool isHoliday,
        string startBase,
        string endBase,
        TimeOnly fromTime,
        TimeOnly untilTime,
        ContainerTransportMode transportMode = ContainerTransportMode.ByCar,
        double timeRangeTolerance = 0.5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Autofill started for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, FromTime: {FromTime}, UntilTime: {UntilTime}",
            containerId, weekday, isHoliday, fromTime, untilTime);

        var timeBudget = untilTime - fromTime;
        _logger.LogInformation("Time budget: {TimeBudget} ({From} - {Until})", timeBudget, fromTime, untilTime);

        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            containerId, weekday, fromTime, untilTime,
            cancellationToken: cancellationToken);

        var timeRangeTasks = availableTasks.Where(s => s.IsTimeRange).ToList();
        _logger.LogInformation("Available tasks: {Total}, TimeRange tasks: {TimeRange}", availableTasks.Count, timeRangeTasks.Count);

        if (timeRangeTasks.Count == 0)
        {
            _logger.LogWarning("No TimeRange tasks available for autofill");
            return CreateEmptyResult(transportMode, availableTasks.Count);
        }

        var candidateLocations = await ExtractLocationsFromShiftsAsync(timeRangeTasks);
        if (candidateLocations.Count == 0)
        {
            _logger.LogWarning("No geocodable locations found");
            return CreateEmptyResult(transportMode, timeRangeTasks.Count);
        }

        var startBaseLocation = await GeocodeBaseAddressAsync(startBase, "StartBase");
        var endBaseLocation = await GeocodeBaseAddressAsync(endBase, "EndBase");

        if (startBaseLocation == null || endBaseLocation == null)
        {
            _logger.LogWarning("Could not geocode base addresses");
            return CreateEmptyResult(transportMode, timeRangeTasks.Count);
        }

        var allLocations = new List<Location>(candidateLocations);
        var startBaseIndex = allLocations.Count;
        allLocations.Add(startBaseLocation);

        int endBaseIndex;
        if (startBase == endBase)
        {
            endBaseIndex = startBaseIndex;
        }
        else
        {
            endBaseIndex = allLocations.Count;
            allLocations.Add(endBaseLocation);
        }

        _logger.LogInformation("Building distance matrix for {Count} locations (including bases)", allLocations.Count);
        var distanceMatrix = await _routeOptimizationService.CalculateDistanceMatrixForLocationsAsync(allLocations, transportMode);
        var minTravelTimeSeconds = await GetMinTravelTimeSecondsAsync(transportMode);
        ClampDurationMatrix(distanceMatrix.DurationMatrix, minTravelTimeSeconds);

        var candidateCount = candidateLocations.Count;
        var containerFromTimeSeconds = fromTime.ToTimeSpan().TotalSeconds;
        var containerEndTimeSeconds = untilTime.ToTimeSpan().TotalSeconds;
        var budgetSeconds = timeBudget.TotalSeconds;

        var context = new RouteEvaluationContext(
            distanceMatrix, startBaseIndex, endBaseIndex,
            containerFromTimeSeconds, containerEndTimeSeconds,
            budgetSeconds, timeRangeTolerance);

        var selectedIndices = GreedySelection(context, candidateCount);
        _logger.LogInformation("Greedy selection: {Count} shifts selected", selectedIndices.Count);

        if (selectedIndices.Count == 0)
        {
            return CreateEmptyResult(transportMode, timeRangeTasks.Count);
        }

        var optimizedRoute = OptimizeSelectedRoute(distanceMatrix, selectedIndices, startBaseIndex, endBaseIndex);
        _logger.LogInformation("ACO optimization complete: route with {Count} stops", optimizedRoute.Count);

        var validatedRoute = ValidateRouteTimeRanges(context, optimizedRoute);

        if (validatedRoute.Count < optimizedRoute.Count)
        {
            _logger.LogInformation("Post-ACO validation: removed {Count} stops with TimeRange/container-end violations",
                optimizedRoute.Count - validatedRoute.Count);
            optimizedRoute = validatedRoute;
        }

        var remainingCandidates = Enumerable.Range(0, candidateCount)
            .Where(i => !optimizedRoute.Contains(i))
            .ToList();

        optimizedRoute = PostOptimizationInsertion(context, optimizedRoute, remainingCandidates);
        _logger.LogInformation("Post-insertion complete: route with {Count} stops", optimizedRoute.Count);

        return BuildResult(context, optimizedRoute, timeRangeTasks.Count, transportMode);
    }

    private async Task<List<Location>> ExtractLocationsFromShiftsAsync(List<Shift> shifts)
    {
        var locations = new List<Location>();
        var uniqueAddresses = new HashSet<string>();

        foreach (var shift in shifts)
        {
            if (shift.Client?.Addresses == null || !shift.Client.Addresses.Any())
            {
                _logger.LogDebug("Shift {ShiftId} has no client addresses, skipping", shift.Id);
                continue;
            }

            var address = shift.Client.Addresses.First();
            var addressKey = $"{address.City}_{address.Zip}_{address.Street}";

            if (uniqueAddresses.Contains(addressKey) || string.IsNullOrEmpty(address.City))
            {
                continue;
            }

            var fullAddress = $"{address.Street}, {address.Zip} {address.City}";
            var coords = await _geocodingService.GeocodeAddressAsync(fullAddress, address.Country ?? "Switzerland");

            if (coords.Latitude.HasValue && coords.Longitude.HasValue)
            {
                locations.Add(new Location
                {
                    Name = shift.Client.Name ?? "Unknown",
                    Address = fullAddress,
                    Latitude = coords.Latitude.Value,
                    Longitude = coords.Longitude.Value,
                    ShiftId = shift.Id,
                    TransportMode = TransportMode.ByCar,
                    BriefingTime = shift.BriefingTime.ToTimeSpan(),
                    DebriefingTime = shift.DebriefingTime.ToTimeSpan(),
                    WorkTime = TimeSpan.FromHours((double)shift.WorkTime),
                    TimeRangeStart = shift.StartShift,
                    TimeRangeEnd = shift.EndShift
                });
                uniqueAddresses.Add(addressKey);
            }
        }

        _logger.LogInformation("Extracted {Count} candidate locations from {Total} shifts", locations.Count, shifts.Count);
        return locations;
    }

    private async Task<Location?> GeocodeBaseAddressAsync(string address, string label)
    {
        var coords = await _geocodingService.GeocodeAddressAsync(address, "Switzerland");
        if (!coords.Latitude.HasValue || !coords.Longitude.HasValue)
        {
            _logger.LogWarning("Could not geocode {Label} address: {Address}", label, address);
            return null;
        }

        return new Location
        {
            Name = label,
            Address = address,
            Latitude = coords.Latitude.Value,
            Longitude = coords.Longitude.Value,
            ShiftId = Guid.Empty
        };
    }

    private List<int> GreedySelection(
        RouteEvaluationContext context,
        int candidateCount)
    {
        var selected = new List<int>();
        var remaining = Enumerable.Range(0, candidateCount).ToList();
        var currentPosition = context.StartBaseIndex;
        var usedTimeSeconds = 0.0;

        while (remaining.Count > 0)
        {
            int bestCandidate = -1;
            double bestCost = double.MaxValue;

            foreach (var candidate in remaining)
            {
                var travelToCandidate = context.DistanceMatrix.DurationMatrix[currentPosition, candidate];
                var onSiteTime = context.DistanceMatrix.Locations[candidate].TotalOnSiteTime.TotalSeconds;
                var travelToEnd = context.DistanceMatrix.DurationMatrix[candidate, context.EndBaseIndex];

                var absoluteArrival = context.ContainerFromTimeSeconds + usedTimeSeconds + travelToCandidate;

                if (!IsTimeRangeValid(context.DistanceMatrix.Locations[candidate], absoluteArrival, context.TimeRangeTolerance))
                {
                    continue;
                }

                if (absoluteArrival + onSiteTime + travelToEnd > context.ContainerEndTimeSeconds)
                {
                    continue;
                }

                var totalCost = travelToCandidate + onSiteTime + travelToEnd;
                if (totalCost < bestCost)
                {
                    bestCost = totalCost;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate == -1)
            {
                break;
            }

            var travelToBest = context.DistanceMatrix.DurationMatrix[currentPosition, bestCandidate];
            var bestOnSiteTime = context.DistanceMatrix.Locations[bestCandidate].TotalOnSiteTime.TotalSeconds;

            selected.Add(bestCandidate);
            usedTimeSeconds += travelToBest + bestOnSiteTime;
            currentPosition = bestCandidate;
            remaining.Remove(bestCandidate);
        }

        return selected;
    }

    private List<int> OptimizeSelectedRoute(
        DistanceMatrix distanceMatrix,
        List<int> selectedIndices,
        int startBaseIndex,
        int endBaseIndex)
    {
        if (selectedIndices.Count <= 2)
        {
            return selectedIndices;
        }

        var subLocations = new List<Location>();
        subLocations.Add(distanceMatrix.Locations[startBaseIndex]);
        foreach (var idx in selectedIndices)
        {
            subLocations.Add(distanceMatrix.Locations[idx]);
        }
        if (endBaseIndex != startBaseIndex)
        {
            subLocations.Add(distanceMatrix.Locations[endBaseIndex]);
        }

        var subSize = subLocations.Count;
        var subDistanceMatrix = new double[subSize, subSize];
        var subDurationMatrix = new double[subSize, subSize];

        var indexMap = new List<int> { startBaseIndex };
        indexMap.AddRange(selectedIndices);
        if (endBaseIndex != startBaseIndex)
        {
            indexMap.Add(endBaseIndex);
        }

        for (int i = 0; i < subSize; i++)
        {
            for (int j = 0; j < subSize; j++)
            {
                subDistanceMatrix[i, j] = distanceMatrix.Matrix[indexMap[i], indexMap[j]];
                subDurationMatrix[i, j] = distanceMatrix.DurationMatrix[indexMap[i], indexMap[j]];
            }
        }

        var subMatrix = new DistanceMatrix(subLocations, subDistanceMatrix, subDurationMatrix, distanceMatrix.DurationMatricesByProfile);
        var optimizer = new AntColonyOptimizer(subMatrix, _logger);

        int subStartIndex = 0;
        int? subEndIndex = endBaseIndex != startBaseIndex ? subSize - 1 : 0;
        var optimizedSubRoute = optimizer.FindOptimalRoute(subStartIndex, subEndIndex);

        var optimizedOriginalIndices = new List<int>();
        foreach (var subIdx in optimizedSubRoute)
        {
            var originalIdx = indexMap[subIdx];
            if (originalIdx != startBaseIndex && originalIdx != endBaseIndex)
            {
                optimizedOriginalIndices.Add(originalIdx);
            }
        }

        return optimizedOriginalIndices;
    }

    private List<int> PostOptimizationInsertion(
        RouteEvaluationContext context,
        List<int> currentRoute,
        List<int> remainingCandidates)
    {
        if (remainingCandidates.Count == 0)
        {
            return currentRoute;
        }

        var route = new List<int>(currentRoute);
        var changed = true;

        while (changed && remainingCandidates.Count > 0)
        {
            changed = false;
            var currentTotalTime = CalculateRouteTotalTime(context.DistanceMatrix, route, context.StartBaseIndex, context.EndBaseIndex);
            var currentArrivalTimes = CalculateAbsoluteArrivalTimes(context.DistanceMatrix, route, context.StartBaseIndex, context.ContainerFromTimeSeconds);

            var (bestCandidate, bestPosition, _) = EvaluateInsertionCandidates(
                context, route, remainingCandidates, currentTotalTime, currentArrivalTimes);

            if (bestCandidate != -1)
            {
                route.Insert(bestPosition, bestCandidate);
                remainingCandidates.Remove(bestCandidate);
                changed = true;
                _logger.LogInformation("Post-insertion: added shift at index {Index} to position {Position}", bestCandidate, bestPosition);
            }
        }

        return route;
    }

    private (int candidate, int position, double cost) EvaluateInsertionCandidates(
        RouteEvaluationContext context,
        List<int> route,
        List<int> remainingCandidates,
        double currentTotalTime,
        List<double> currentArrivalTimes)
    {
        int bestCandidate = -1;
        int bestPosition = -1;
        double bestInsertionCost = double.MaxValue;

        foreach (var candidate in remainingCandidates)
        {
            for (int pos = 0; pos <= route.Count; pos++)
            {
                var (isValid, insertionCost) = EvaluateInsertionPosition(
                    context, route, candidate, pos, currentTotalTime, currentArrivalTimes);

                if (isValid && insertionCost < bestInsertionCost)
                {
                    bestInsertionCost = insertionCost;
                    bestCandidate = candidate;
                    bestPosition = pos;
                }
            }
        }

        return (bestCandidate, bestPosition, bestInsertionCost);
    }

    private static (bool isValid, double insertionCost) EvaluateInsertionPosition(
        RouteEvaluationContext context,
        List<int> route,
        int candidate,
        int pos,
        double currentTotalTime,
        List<double> currentArrivalTimes)
    {
        var onSiteTime = context.DistanceMatrix.Locations[candidate].TotalOnSiteTime.TotalSeconds;
        var prevIndex = pos == 0 ? context.StartBaseIndex : route[pos - 1];
        var nextIndex = pos == route.Count ? context.EndBaseIndex : route[pos];

        var currentSegmentDuration = context.DistanceMatrix.DurationMatrix[prevIndex, nextIndex];
        var newDurationBefore = context.DistanceMatrix.DurationMatrix[prevIndex, candidate];
        var newDurationAfter = context.DistanceMatrix.DurationMatrix[candidate, nextIndex];
        var insertionCost = newDurationBefore + onSiteTime + newDurationAfter - currentSegmentDuration;

        if (currentTotalTime + insertionCost > context.BudgetSeconds)
        {
            return (false, insertionCost);
        }

        var prevArrivalEnd = pos == 0
            ? context.ContainerFromTimeSeconds
            : currentArrivalTimes[pos - 1] + context.DistanceMatrix.Locations[route[pos - 1]].TotalOnSiteTime.TotalSeconds;

        var candidateArrival = prevArrivalEnd + newDurationBefore;
        if (!IsTimeRangeValid(context.DistanceMatrix.Locations[candidate], candidateArrival, context.TimeRangeTolerance))
        {
            return (false, insertionCost);
        }

        var timeShift = insertionCost;
        for (int i = pos; i < route.Count; i++)
        {
            var shiftedArrival = currentArrivalTimes[i] + timeShift;
            if (!IsTimeRangeValid(context.DistanceMatrix.Locations[route[i]], shiftedArrival, context.TimeRangeTolerance))
            {
                return (false, insertionCost);
            }
        }

        var lastIndex = route.Count > 0 ? route[route.Count - 1] : candidate;
        var lastArrival = route.Count > 0
            ? currentArrivalTimes[route.Count - 1] + timeShift
            : candidateArrival;
        var lastOnSite = context.DistanceMatrix.Locations[lastIndex].TotalOnSiteTime.TotalSeconds;
        var travelToEnd = context.DistanceMatrix.DurationMatrix[lastIndex, context.EndBaseIndex];

        if (pos == route.Count)
        {
            lastIndex = candidate;
            lastArrival = candidateArrival;
            lastOnSite = onSiteTime;
            travelToEnd = context.DistanceMatrix.DurationMatrix[candidate, context.EndBaseIndex];
        }

        if (lastArrival + lastOnSite + travelToEnd > context.ContainerEndTimeSeconds)
        {
            return (false, insertionCost);
        }

        return (true, insertionCost);
    }

    private double CalculateRouteTotalTime(
        DistanceMatrix distanceMatrix,
        List<int> route,
        int startBaseIndex,
        int endBaseIndex)
    {
        if (route.Count == 0)
        {
            return 0;
        }

        double totalSeconds = 0;

        totalSeconds += distanceMatrix.DurationMatrix[startBaseIndex, route[0]];

        for (int i = 0; i < route.Count; i++)
        {
            totalSeconds += distanceMatrix.Locations[route[i]].TotalOnSiteTime.TotalSeconds;

            if (i < route.Count - 1)
            {
                totalSeconds += distanceMatrix.DurationMatrix[route[i], route[i + 1]];
            }
        }

        totalSeconds += distanceMatrix.DurationMatrix[route.Last(), endBaseIndex];

        return totalSeconds;
    }

    private static bool IsTimeRangeValid(Location location, double arrivalTimeSeconds, double timeRangeTolerance)
    {
        if (location.TimeRangeStart == null || location.TimeRangeEnd == null || timeRangeTolerance <= 0)
        {
            return true;
        }

        var windowStart = location.TimeRangeStart.Value.ToTimeSpan().TotalSeconds;
        var windowEnd = location.TimeRangeEnd.Value.ToTimeSpan().TotalSeconds;
        var windowDuration = windowEnd - windowStart;
        var relaxation = 1.0 - timeRangeTolerance;
        var buffer = windowDuration * relaxation;

        return arrivalTimeSeconds >= windowStart - buffer && arrivalTimeSeconds <= windowEnd + buffer;
    }

    private List<int> ValidateRouteTimeRanges(
        RouteEvaluationContext context,
        List<int> route)
    {
        var validRoute = new List<int>(route);
        var changed = true;

        while (changed)
        {
            changed = false;
            var arrivalTimes = CalculateAbsoluteArrivalTimes(context.DistanceMatrix, validRoute, context.StartBaseIndex, context.ContainerFromTimeSeconds);

            for (int i = validRoute.Count - 1; i >= 0; i--)
            {
                var location = context.DistanceMatrix.Locations[validRoute[i]];
                var arrival = arrivalTimes[i];
                var onSite = location.TotalOnSiteTime.TotalSeconds;
                var travelToEnd = context.DistanceMatrix.DurationMatrix[validRoute[i], context.EndBaseIndex];
                var isLastStop = i == validRoute.Count - 1;

                var timeRangeViolation = !IsTimeRangeValid(location, arrival, context.TimeRangeTolerance);
                var containerEndViolation = isLastStop && arrival + onSite + travelToEnd > context.ContainerEndTimeSeconds;

                if (timeRangeViolation || containerEndViolation)
                {
                    validRoute.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }

        return validRoute;
    }

    private List<double> CalculateAbsoluteArrivalTimes(
        DistanceMatrix distanceMatrix, List<int> route, int startBaseIndex, double containerFromTimeSeconds)
    {
        var arrivalTimes = new List<double>();
        var currentTime = containerFromTimeSeconds;
        var prevIndex = startBaseIndex;

        foreach (var stopIndex in route)
        {
            currentTime += distanceMatrix.DurationMatrix[prevIndex, stopIndex];
            arrivalTimes.Add(currentTime);
            currentTime += distanceMatrix.Locations[stopIndex].TotalOnSiteTime.TotalSeconds;
            prevIndex = stopIndex;
        }

        return arrivalTimes;
    }

    private static void ClampDurationMatrix(double[,] durationMatrix, double minTravelTimeSeconds)
    {
        var rows = durationMatrix.GetLength(0);
        var cols = durationMatrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (i != j && durationMatrix[i, j] > 0 && durationMatrix[i, j] < minTravelTimeSeconds)
                {
                    durationMatrix[i, j] = minTravelTimeSeconds;
                }
            }
        }
    }

    private async Task<double> GetMinTravelTimeSecondsAsync(ContainerTransportMode transportMode)
    {
        var settingKey = transportMode switch
        {
            ContainerTransportMode.ByBicycle => Application.Constants.Settings.ROUTE_MIN_TRAVEL_TIME_BY_BICYCLE,
            ContainerTransportMode.ByFoot => Application.Constants.Settings.ROUTE_MIN_TRAVEL_TIME_BY_FOOT,
            _ => Application.Constants.Settings.ROUTE_MIN_TRAVEL_TIME_BY_CAR
        };

        var setting = await _settingsRepository.GetSetting(settingKey);
        if (setting != null && double.TryParse(setting.Value, out var minutes))
        {
            return minutes * SecondsPerMinute;
        }

        return 0;
    }

    private ContainerAutofillResult BuildResult(
        RouteEvaluationContext context,
        List<int> optimizedRoute,
        int totalAvailableShifts,
        ContainerTransportMode transportMode)
    {
        var selectedShiftIds = optimizedRoute
            .Select(i => context.DistanceMatrix.Locations[i].ShiftId)
            .Where(id => id != Guid.Empty)
            .ToList();

        var fullRoute = new List<Location>();
        var fullRouteIndices = new List<int>();

        fullRoute.Add(context.DistanceMatrix.Locations[context.StartBaseIndex]);
        fullRouteIndices.Add(context.StartBaseIndex);

        foreach (var idx in optimizedRoute)
        {
            fullRoute.Add(context.DistanceMatrix.Locations[idx]);
            fullRouteIndices.Add(idx);
        }

        if (context.EndBaseIndex != context.StartBaseIndex)
        {
            fullRoute.Add(context.DistanceMatrix.Locations[context.EndBaseIndex]);
            fullRouteIndices.Add(context.EndBaseIndex);
        }
        else
        {
            fullRoute.Add(context.DistanceMatrix.Locations[context.EndBaseIndex]);
            fullRouteIndices.Add(context.EndBaseIndex);
        }

        double totalDistance = 0;
        for (int i = 0; i < fullRouteIndices.Count - 1; i++)
        {
            totalDistance += context.DistanceMatrix.Matrix[fullRouteIndices[i], fullRouteIndices[i + 1]];
        }

        double distanceFromStartBase = optimizedRoute.Count > 0
            ? context.DistanceMatrix.Matrix[context.StartBaseIndex, optimizedRoute[0]]
            : 0;

        double distanceToEndBase = optimizedRoute.Count > 0
            ? context.DistanceMatrix.Matrix[optimizedRoute.Last(), context.EndBaseIndex]
            : 0;

        var travelTimeFromStartBase = optimizedRoute.Count > 0
            ? TimeSpan.FromSeconds(context.DistanceMatrix.DurationMatrix[context.StartBaseIndex, optimizedRoute[0]])
            : TimeSpan.Zero;

        var travelTimeToEndBase = optimizedRoute.Count > 0
            ? TimeSpan.FromSeconds(context.DistanceMatrix.DurationMatrix[optimizedRoute.Last(), context.EndBaseIndex])
            : TimeSpan.Zero;

        double totalTravelSeconds = 0;
        if (optimizedRoute.Count > 0)
        {
            totalTravelSeconds += context.DistanceMatrix.DurationMatrix[context.StartBaseIndex, optimizedRoute[0]];
            for (int i = 0; i < optimizedRoute.Count - 1; i++)
            {
                totalTravelSeconds += context.DistanceMatrix.DurationMatrix[optimizedRoute[i], optimizedRoute[i + 1]];
            }
            totalTravelSeconds += context.DistanceMatrix.DurationMatrix[optimizedRoute.Last(), context.EndBaseIndex];
        }

        var totalWorkTime = TimeSpan.Zero;
        foreach (var idx in optimizedRoute)
        {
            totalWorkTime += context.DistanceMatrix.Locations[idx].TotalOnSiteTime;
        }

        var timeBudget = TimeSpan.FromSeconds(context.BudgetSeconds);
        var estimatedTravelTime = TimeSpan.FromSeconds(totalTravelSeconds) + totalWorkTime;
        var remainingTime = timeBudget - estimatedTravelTime;

        return new ContainerAutofillResult(
            fullRoute,
            selectedShiftIds,
            Math.Round(totalDistance, 2),
            estimatedTravelTime,
            totalWorkTime,
            remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero,
            totalAvailableShifts,
            selectedShiftIds.Count,
            context.DistanceMatrix.Matrix,
            context.DistanceMatrix.DurationMatrix,
            travelTimeFromStartBase,
            optimizedRoute,
            distanceFromStartBase,
            distanceToEndBase,
            travelTimeToEndBase,
            fullRouteIndices,
            context.DistanceMatrix.DurationMatricesByProfile,
            transportMode,
            TotalBriefingDebriefingTime: totalWorkTime);
    }

    private static ContainerAutofillResult CreateEmptyResult(
        ContainerTransportMode transportMode,
        int totalAvailableShifts = 0)
    {
        return new ContainerAutofillResult(
            new List<Location>(),
            new List<Guid>(),
            0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero,
            totalAvailableShifts, 0,
            new double[0, 0], new double[0, 0],
            TimeSpan.Zero, new List<int>(), 0, 0, TimeSpan.Zero,
            new List<int>(),
            TransportMode: transportMode);
    }
}
