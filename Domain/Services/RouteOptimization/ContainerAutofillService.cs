// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Automatische Befuellung von Container-Templates mit passenden TimeRange-Shifts.
/// Loest ein Orienteering Problem in 3 Phasen: Greedy Selection, ACO+2-Opt, Post-Insertion.
/// </summary>
/// <param name="containerRepository">Repository fuer Container-Templates (Zeitbudget)</param>
/// <param name="availableTasksService">Service fuer verfuegbare Shifts</param>
/// <param name="routeOptimizationService">Service fuer Distance-Matrix-Berechnung</param>
/// <param name="geocodingService">Service fuer Adress-Geocodierung</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
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
    private readonly IAddressCoordinateWriter _coordinateWriter;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<ContainerAutofillService> _logger;

    public ContainerAutofillService(
        IContainerAvailableTasksService availableTasksService,
        IRouteOptimizationService routeOptimizationService,
        IGeocodingService geocodingService,
        IAddressCoordinateWriter coordinateWriter,
        ISettingsReader settingsReader,
        ILogger<ContainerAutofillService> logger)
    {
        _availableTasksService = availableTasksService;
        _routeOptimizationService = routeOptimizationService;
        _geocodingService = geocodingService;
        _coordinateWriter = coordinateWriter;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<ContainerAutofillResult> AutofillAsync(ContainerAutofillRequest request)
    {
        _logger.LogInformation(
            "Autofill started for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, FromTime: {FromTime}, UntilTime: {UntilTime}",
            request.ContainerId, request.Weekday, request.IsHoliday, request.FromTime, request.UntilTime);

        var timeBudget = request.UntilTime - request.FromTime;
        _logger.LogInformation("Time budget: {TimeBudget} ({From} - {Until})", timeBudget, request.FromTime, request.UntilTime);

        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            request.ContainerId, request.Weekday, request.FromTime, request.UntilTime,
            cancellationToken: request.CancellationToken);

        var timeRangeTasks = availableTasks.Where(s => s.IsTimeRange).ToList();
        _logger.LogInformation("Available tasks: {Total}, TimeRange tasks: {TimeRange}", availableTasks.Count, timeRangeTasks.Count);

        if (timeRangeTasks.Count == 0)
        {
            _logger.LogWarning("No TimeRange tasks available for autofill");
            return CreateEmptyResult(request.TransportMode, availableTasks.Count);
        }

        var candidateLocations = await ExtractLocationsFromShiftsAsync(timeRangeTasks, request.TransportMode, request.CancellationToken);
        if (candidateLocations.Count == 0)
        {
            _logger.LogWarning("No geocodable locations found");
            return CreateEmptyResult(request.TransportMode, timeRangeTasks.Count);
        }

        var startBaseLocation = await GeocodeBaseAddressAsync(request.StartBase, "StartBase", request.CancellationToken);
        var endBaseLocation = await GeocodeBaseAddressAsync(request.EndBase, "EndBase", request.CancellationToken);

        if (startBaseLocation == null || endBaseLocation == null)
        {
            _logger.LogWarning("Could not geocode base addresses. StartBase={StartBase}, EndBase={EndBase}", request.StartBase, request.EndBase);
            return CreateEmptyResult(request.TransportMode, timeRangeTasks.Count);
        }

        var allLocations = new List<Location>(candidateLocations);
        var startBaseIndex = allLocations.Count;
        allLocations.Add(startBaseLocation);

        int endBaseIndex;
        if (request.StartBase == request.EndBase)
        {
            endBaseIndex = startBaseIndex;
        }
        else
        {
            endBaseIndex = allLocations.Count;
            allLocations.Add(endBaseLocation);
        }

        _logger.LogInformation("Building distance matrix for {Count} locations (including bases)", allLocations.Count);
        var distanceMatrix = await _routeOptimizationService.CalculateDistanceMatrixForLocationsAsync(allLocations, request.TransportMode);
        var minTravelTimeSeconds = await GetMinTravelTimeSecondsAsync(request.TransportMode);
        ClampDurationMatrix(distanceMatrix.DurationMatrix, minTravelTimeSeconds);

        var candidateCount = candidateLocations.Count;
        var containerFromTimeSeconds = request.FromTime.ToTimeSpan().TotalSeconds;
        var containerEndTimeSeconds = request.UntilTime.ToTimeSpan().TotalSeconds;
        var budgetSeconds = timeBudget.TotalSeconds;

        var context = new RouteEvaluationContext(
            distanceMatrix, startBaseIndex, endBaseIndex,
            containerFromTimeSeconds, containerEndTimeSeconds,
            budgetSeconds, request.TimeRangeTolerance);

        var selectedIndices = GreedySelection(context, candidateCount);
        _logger.LogInformation("Greedy selection: {Count} shifts selected", selectedIndices.Count);

        if (selectedIndices.Count == 0)
        {
            return CreateEmptyResult(request.TransportMode, timeRangeTasks.Count);
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

        return BuildResult(context, optimizedRoute, timeRangeTasks.Count, request.TransportMode);
    }

    private async Task<List<Location>> ExtractLocationsFromShiftsAsync(List<Shift> shifts, ContainerTransportMode containerTransportMode, CancellationToken cancellationToken)
    {
        var locations = new List<Location>();
        var uniqueAddresses = new HashSet<string>();
        var needsGeocoding = new List<(Shift shift, Klacks.Api.Domain.Models.Staffs.Address address, string fullAddress)>();

        foreach (var shift in shifts)
        {
            if (shift.Client?.Addresses == null || !shift.Client.Addresses.Any())
            {
                continue;
            }

            var address = shift.Client.Addresses.First();
            var addressKey = $"{address.City}_{address.Zip}_{address.Street}";

            if (string.IsNullOrEmpty(address.City) || uniqueAddresses.Contains(addressKey))
            {
                continue;
            }

            uniqueAddresses.Add(addressKey);
            var fullAddress = $"{address.Street}, {address.Zip} {address.City}";

            if (address.Latitude.HasValue && address.Longitude.HasValue)
            {
                locations.Add(CreateLocationFromShift(shift, fullAddress, address.Latitude.Value, address.Longitude.Value, containerTransportMode));
            }
            else
            {
                needsGeocoding.Add((shift, address, fullAddress));
            }
        }

        _logger.LogInformation("ExtractLocations: {Stored} from DB, {NeedsGeocoding} need geocoding, from {Total} shifts",
            locations.Count, needsGeocoding.Count, shifts.Count);

        if (needsGeocoding.Count > 0)
        {
            var geocodeTasks = needsGeocoding.Select(async req =>
            {
                var coords = await _geocodingService.GeocodeAddressAsync(req.fullAddress, req.address.Country ?? "Switzerland", cancellationToken);
                return (req.shift, req.address, req.fullAddress, coords);
            });

            var results = await Task.WhenAll(geocodeTasks);

            foreach (var (shift, address, fullAddress, coords) in results)
            {
                if (coords.Latitude.HasValue && coords.Longitude.HasValue)
                {
                    locations.Add(CreateLocationFromShift(shift, fullAddress, coords.Latitude.Value, coords.Longitude.Value, containerTransportMode));

                    _ = _coordinateWriter.UpdateCoordinatesAsync(address.Id, coords.Latitude.Value, coords.Longitude.Value, CancellationToken.None);
                }
            }
        }

        _logger.LogInformation("Extracted {Count} candidate locations from {Total} shifts", locations.Count, shifts.Count);
        return locations;
    }

    private static Location CreateLocationFromShift(Shift shift, string fullAddress, double latitude, double longitude, ContainerTransportMode containerTransportMode)
    {
        return new Location
        {
            Name = shift.Client?.Name ?? "Unknown",
            Address = fullAddress,
            Latitude = latitude,
            Longitude = longitude,
            ShiftId = shift.Id,
            TransportMode = containerTransportMode == ContainerTransportMode.Mix
                ? TransportMode.ByCar
                : (TransportMode)(int)containerTransportMode,
            BriefingTime = shift.BriefingTime.ToTimeSpan(),
            DebriefingTime = shift.DebriefingTime.ToTimeSpan(),
            WorkTime = TimeSpan.FromHours((double)shift.WorkTime),
            TimeRangeStart = shift.StartShift,
            TimeRangeEnd = shift.EndShift
        };
    }

    private async Task<Location?> GeocodeBaseAddressAsync(string address, string label, CancellationToken cancellationToken)
    {
        var coords = await _geocodingService.GeocodeAddressAsync(address, "Switzerland", cancellationToken);
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

        var setting = await _settingsReader.GetSetting(settingKey);
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

        fullRoute.Add(context.DistanceMatrix.Locations[context.EndBaseIndex]);
        fullRouteIndices.Add(context.EndBaseIndex);

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
