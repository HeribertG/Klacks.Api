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
        var selectedIndices = GreedySelection(distanceMatrix, startBaseIndex, endBaseIndex, candidateCount, timeBudget, fromTime, timeRangeTolerance);
        _logger.LogInformation("Greedy selection: {Count} shifts selected", selectedIndices.Count);

        if (selectedIndices.Count == 0)
        {
            return CreateEmptyResult(transportMode, timeRangeTasks.Count);
        }

        var optimizedRoute = OptimizeSelectedRoute(distanceMatrix, selectedIndices, startBaseIndex, endBaseIndex);
        _logger.LogInformation("ACO optimization complete: route with {Count} stops", optimizedRoute.Count);

        var containerFromTimeSeconds = fromTime.ToTimeSpan().TotalSeconds;
        var containerEndTimeSeconds = untilTime.ToTimeSpan().TotalSeconds;
        var validatedRoute = ValidateRouteTimeRanges(
            distanceMatrix, optimizedRoute, startBaseIndex, endBaseIndex,
            containerFromTimeSeconds, containerEndTimeSeconds, timeRangeTolerance);

        if (validatedRoute.Count < optimizedRoute.Count)
        {
            _logger.LogInformation("Post-ACO validation: removed {Count} stops with TimeRange/container-end violations",
                optimizedRoute.Count - validatedRoute.Count);
            optimizedRoute = validatedRoute;
        }

        var remainingCandidates = Enumerable.Range(0, candidateCount)
            .Where(i => !optimizedRoute.Contains(i))
            .ToList();

        optimizedRoute = PostOptimizationInsertion(distanceMatrix, optimizedRoute, remainingCandidates, startBaseIndex, endBaseIndex, timeBudget, fromTime, timeRangeTolerance);
        _logger.LogInformation("Post-insertion complete: route with {Count} stops", optimizedRoute.Count);

        return BuildResult(distanceMatrix, optimizedRoute, startBaseIndex, endBaseIndex, timeBudget, timeRangeTasks.Count, transportMode);
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
        DistanceMatrix distanceMatrix,
        int startBaseIndex,
        int endBaseIndex,
        int candidateCount,
        TimeSpan timeBudget,
        TimeOnly containerFromTime,
        double timeRangeTolerance)
    {
        var selected = new List<int>();
        var remaining = Enumerable.Range(0, candidateCount).ToList();
        var currentPosition = startBaseIndex;
        var usedTimeSeconds = 0.0;
        var budgetSeconds = timeBudget.TotalSeconds;
        var containerFromTimeSeconds = containerFromTime.ToTimeSpan().TotalSeconds;
        var containerEndTimeSeconds = containerFromTimeSeconds + budgetSeconds;

        while (remaining.Count > 0)
        {
            int bestCandidate = -1;
            double bestCost = double.MaxValue;

            foreach (var candidate in remaining)
            {
                var travelToCandidate = distanceMatrix.DurationMatrix[currentPosition, candidate];
                var onSiteTime = distanceMatrix.Locations[candidate].TotalOnSiteTime.TotalSeconds;
                var travelToEnd = distanceMatrix.DurationMatrix[candidate, endBaseIndex];

                var absoluteArrival = containerFromTimeSeconds + usedTimeSeconds + travelToCandidate;

                if (!IsTimeRangeValid(distanceMatrix.Locations[candidate], absoluteArrival, timeRangeTolerance))
                {
                    continue;
                }

                if (absoluteArrival + onSiteTime + travelToEnd > containerEndTimeSeconds)
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

            var travelToBest = distanceMatrix.DurationMatrix[currentPosition, bestCandidate];
            var bestOnSiteTime = distanceMatrix.Locations[bestCandidate].TotalOnSiteTime.TotalSeconds;

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
        DistanceMatrix distanceMatrix,
        List<int> currentRoute,
        List<int> remainingCandidates,
        int startBaseIndex,
        int endBaseIndex,
        TimeSpan timeBudget,
        TimeOnly containerFromTime,
        double timeRangeTolerance)
    {
        if (remainingCandidates.Count == 0)
        {
            return currentRoute;
        }

        var route = new List<int>(currentRoute);
        var budgetSeconds = timeBudget.TotalSeconds;
        var containerFromTimeSeconds = containerFromTime.ToTimeSpan().TotalSeconds;
        var containerEndTimeSeconds = containerFromTimeSeconds + budgetSeconds;
        var changed = true;

        while (changed && remainingCandidates.Count > 0)
        {
            changed = false;
            int bestCandidate = -1;
            int bestPosition = -1;
            double bestInsertionCost = double.MaxValue;

            var currentTotalTime = CalculateRouteTotalTime(distanceMatrix, route, startBaseIndex, endBaseIndex);
            var currentArrivalTimes = CalculateAbsoluteArrivalTimes(distanceMatrix, route, startBaseIndex, containerFromTimeSeconds);

            foreach (var candidate in remainingCandidates)
            {
                var onSiteTime = distanceMatrix.Locations[candidate].TotalOnSiteTime.TotalSeconds;

                for (int pos = 0; pos <= route.Count; pos++)
                {
                    var prevIndex = pos == 0 ? startBaseIndex : route[pos - 1];
                    var nextIndex = pos == route.Count ? endBaseIndex : route[pos];

                    var currentSegmentDuration = distanceMatrix.DurationMatrix[prevIndex, nextIndex];
                    var newDurationBefore = distanceMatrix.DurationMatrix[prevIndex, candidate];
                    var newDurationAfter = distanceMatrix.DurationMatrix[candidate, nextIndex];
                    var insertionCost = newDurationBefore + onSiteTime + newDurationAfter - currentSegmentDuration;

                    if (currentTotalTime + insertionCost > budgetSeconds || insertionCost >= bestInsertionCost)
                    {
                        continue;
                    }

                    var prevArrivalEnd = pos == 0
                        ? containerFromTimeSeconds
                        : currentArrivalTimes[pos - 1] + distanceMatrix.Locations[route[pos - 1]].TotalOnSiteTime.TotalSeconds;

                    var candidateArrival = prevArrivalEnd + newDurationBefore;
                    if (!IsTimeRangeValid(distanceMatrix.Locations[candidate], candidateArrival, timeRangeTolerance))
                    {
                        continue;
                    }

                    var timeShift = insertionCost;
                    var allValid = true;
                    for (int i = pos; i < route.Count; i++)
                    {
                        var shiftedArrival = currentArrivalTimes[i] + timeShift;
                        if (!IsTimeRangeValid(distanceMatrix.Locations[route[i]], shiftedArrival, timeRangeTolerance))
                        {
                            allValid = false;
                            break;
                        }
                    }

                    if (!allValid)
                    {
                        continue;
                    }

                    var lastIndex = route.Count > 0 ? route[route.Count - 1] : candidate;
                    var lastArrival = route.Count > 0
                        ? currentArrivalTimes[route.Count - 1] + timeShift
                        : candidateArrival;
                    var lastOnSite = distanceMatrix.Locations[lastIndex].TotalOnSiteTime.TotalSeconds;
                    var travelToEnd = distanceMatrix.DurationMatrix[lastIndex, endBaseIndex];

                    if (pos == route.Count)
                    {
                        lastIndex = candidate;
                        lastArrival = candidateArrival;
                        lastOnSite = onSiteTime;
                        travelToEnd = distanceMatrix.DurationMatrix[candidate, endBaseIndex];
                    }

                    if (lastArrival + lastOnSite + travelToEnd > containerEndTimeSeconds)
                    {
                        continue;
                    }

                    bestInsertionCost = insertionCost;
                    bestCandidate = candidate;
                    bestPosition = pos;
                }
            }

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
        DistanceMatrix distanceMatrix,
        List<int> route,
        int startBaseIndex,
        int endBaseIndex,
        double containerFromTimeSeconds,
        double containerEndTimeSeconds,
        double timeRangeTolerance)
    {
        var validRoute = new List<int>(route);
        var changed = true;

        while (changed)
        {
            changed = false;
            var arrivalTimes = CalculateAbsoluteArrivalTimes(distanceMatrix, validRoute, startBaseIndex, containerFromTimeSeconds);

            for (int i = validRoute.Count - 1; i >= 0; i--)
            {
                var location = distanceMatrix.Locations[validRoute[i]];
                var arrival = arrivalTimes[i];
                var onSite = location.TotalOnSiteTime.TotalSeconds;
                var travelToEnd = distanceMatrix.DurationMatrix[validRoute[i], endBaseIndex];
                var isLastStop = i == validRoute.Count - 1;

                var timeRangeViolation = !IsTimeRangeValid(location, arrival, timeRangeTolerance);
                var containerEndViolation = isLastStop && arrival + onSite + travelToEnd > containerEndTimeSeconds;

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
        DistanceMatrix distanceMatrix,
        List<int> optimizedRoute,
        int startBaseIndex,
        int endBaseIndex,
        TimeSpan timeBudget,
        int totalAvailableShifts,
        ContainerTransportMode transportMode)
    {
        var selectedShiftIds = optimizedRoute
            .Select(i => distanceMatrix.Locations[i].ShiftId)
            .Where(id => id != Guid.Empty)
            .ToList();

        var fullRoute = new List<Location>();
        var fullRouteIndices = new List<int>();

        fullRoute.Add(distanceMatrix.Locations[startBaseIndex]);
        fullRouteIndices.Add(startBaseIndex);

        foreach (var idx in optimizedRoute)
        {
            fullRoute.Add(distanceMatrix.Locations[idx]);
            fullRouteIndices.Add(idx);
        }

        if (endBaseIndex != startBaseIndex)
        {
            fullRoute.Add(distanceMatrix.Locations[endBaseIndex]);
            fullRouteIndices.Add(endBaseIndex);
        }
        else
        {
            fullRoute.Add(distanceMatrix.Locations[endBaseIndex]);
            fullRouteIndices.Add(endBaseIndex);
        }

        double totalDistance = 0;
        for (int i = 0; i < fullRouteIndices.Count - 1; i++)
        {
            totalDistance += distanceMatrix.Matrix[fullRouteIndices[i], fullRouteIndices[i + 1]];
        }

        double distanceFromStartBase = optimizedRoute.Count > 0
            ? distanceMatrix.Matrix[startBaseIndex, optimizedRoute[0]]
            : 0;

        double distanceToEndBase = optimizedRoute.Count > 0
            ? distanceMatrix.Matrix[optimizedRoute.Last(), endBaseIndex]
            : 0;

        var travelTimeFromStartBase = optimizedRoute.Count > 0
            ? TimeSpan.FromSeconds(distanceMatrix.DurationMatrix[startBaseIndex, optimizedRoute[0]])
            : TimeSpan.Zero;

        var travelTimeToEndBase = optimizedRoute.Count > 0
            ? TimeSpan.FromSeconds(distanceMatrix.DurationMatrix[optimizedRoute.Last(), endBaseIndex])
            : TimeSpan.Zero;

        double totalTravelSeconds = 0;
        if (optimizedRoute.Count > 0)
        {
            totalTravelSeconds += distanceMatrix.DurationMatrix[startBaseIndex, optimizedRoute[0]];
            for (int i = 0; i < optimizedRoute.Count - 1; i++)
            {
                totalTravelSeconds += distanceMatrix.DurationMatrix[optimizedRoute[i], optimizedRoute[i + 1]];
            }
            totalTravelSeconds += distanceMatrix.DurationMatrix[optimizedRoute.Last(), endBaseIndex];
        }

        var totalWorkTime = TimeSpan.Zero;
        foreach (var idx in optimizedRoute)
        {
            totalWorkTime += distanceMatrix.Locations[idx].TotalOnSiteTime;
        }

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
            distanceMatrix.Matrix,
            distanceMatrix.DurationMatrix,
            travelTimeFromStartBase,
            optimizedRoute,
            distanceFromStartBase,
            distanceToEndBase,
            travelTimeToEndBase,
            fullRouteIndices,
            distanceMatrix.DurationMatricesByProfile,
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
