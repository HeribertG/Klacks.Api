// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates route optimization by coordinating distance matrix building, route finding and directions retrieval.
/// </summary>
/// <param name="_containerRepository">Repository for loading container templates</param>
/// <param name="_branchRepository">Repository for loading branch addresses</param>
/// <param name="_geocodingService">Service for geocoding addresses to coordinates</param>
/// <param name="_distanceMatrixBuilder">Builds distance/duration matrices from locations</param>
/// <param name="_routeDirectionsBuilder">Retrieves turn-by-turn directions for route segments</param>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class RouteOptimizationService : IRouteOptimizationService
{
    private readonly IContainerTemplateRepository _containerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IDistanceMatrixBuilder _distanceMatrixBuilder;
    private readonly IRouteDirectionsBuilder _routeDirectionsBuilder;
    private readonly ILogger<RouteOptimizationService> _logger;

    public RouteOptimizationService(
        IContainerTemplateRepository containerRepository,
        IBranchRepository branchRepository,
        IShiftRepository shiftRepository,
        IGeocodingService geocodingService,
        IDistanceMatrixBuilder distanceMatrixBuilder,
        IRouteDirectionsBuilder routeDirectionsBuilder,
        ILogger<RouteOptimizationService> logger)
    {
        _containerRepository = containerRepository;
        _branchRepository = branchRepository;
        _shiftRepository = shiftRepository;
        _geocodingService = geocodingService;
        _distanceMatrixBuilder = distanceMatrixBuilder;
        _routeDirectionsBuilder = routeDirectionsBuilder;
        _logger = logger;
    }

    public async Task<DistanceMatrix> CalculateDistanceMatrixAsync(
        Guid containerId,
        int weekday,
        bool isHoliday,
        ContainerTransportMode transportMode = ContainerTransportMode.ByCar)
    {
        _logger.LogInformation(
            "Calculating distance matrix for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, TransportMode: {TransportMode}",
            containerId, weekday, isHoliday, transportMode);

        var templates = await _containerRepository.GetTemplatesForContainer(containerId);
        var template = templates.FirstOrDefault(t => t.Weekday == weekday && t.IsHoliday == isHoliday);

        if (template == null)
        {
            _logger.LogWarning("No template found for given criteria");
            return new DistanceMatrix(new List<Location>(), new double[0, 0], new double[0, 0]);
        }

        var locations = await ExtractLocationsFromTemplateAsync(template);

        if (locations.Count < 2)
        {
            _logger.LogWarning("Not enough locations found ({Count}). Need at least 2.", locations.Count);
            return new DistanceMatrix(locations, new double[0, 0], new double[0, 0]);
        }

        var (distanceMatrix, durationMatrix, durationMatricesByProfile) = await _distanceMatrixBuilder.BuildDistanceMatrixAsync(locations, transportMode);

        _logger.LogInformation("Distance matrix calculated: {Size}x{Size}", locations.Count, locations.Count);
        return new DistanceMatrix(locations, distanceMatrix, durationMatrix, durationMatricesByProfile);
    }

    public async Task<RouteOptimizationResult> OptimizeRouteAsync(
        Guid containerId,
        int weekday,
        bool isHoliday,
        string? startBase = null,
        string? endBase = null,
        ContainerTransportMode transportMode = ContainerTransportMode.ByCar)
    {
        _logger.LogInformation(
            "OptimizeRouteAsync called with containerId={ContainerId}, weekday={Weekday}, isHoliday={IsHoliday}, startBase={StartBase}, endBase={EndBase}, transportMode={TransportMode}",
            containerId, weekday, isHoliday, startBase, endBase, transportMode);

        var distanceMatrix = await CalculateDistanceMatrixAsync(containerId, weekday, isHoliday, transportMode);
        _logger.LogInformation("After CalculateDistanceMatrixAsync: {Count} locations", distanceMatrix.Locations.Count);

        distanceMatrix = await AddBranchesToDistanceMatrixAsync(distanceMatrix, startBase, endBase, transportMode);
        _logger.LogInformation("After AddBranchesToDistanceMatrixAsync: {Count} locations", distanceMatrix.Locations.Count);

        _logger.LogInformation("=== ALL LOCATIONS IN DISTANCE MATRIX ===");
        for (int i = 0; i < distanceMatrix.Locations.Count; i++)
        {
            var loc = distanceMatrix.Locations[i];
            _logger.LogInformation("  [{Index}] Name={Name}, Address={Address}, Lat={Lat}, Lon={Lon}, ShiftId={ShiftId}",
                i, loc.Name, loc.Address, loc.Latitude, loc.Longitude, loc.ShiftId);
        }

        _logger.LogInformation("=== DISTANCE MATRIX VALUES ===");
        for (int i = 0; i < distanceMatrix.Locations.Count; i++)
        {
            for (int j = 0; j < distanceMatrix.Locations.Count; j++)
            {
                if (i != j)
                {
                    _logger.LogInformation("  Distance[{I}][{J}] ({FromName} -> {ToName}) = {Distance:F2} km",
                        i, j,
                        distanceMatrix.Locations[i].Name,
                        distanceMatrix.Locations[j].Name,
                        distanceMatrix.Matrix[i, j]);
                }
            }
        }

        if (distanceMatrix.Locations.Count < 2)
        {
            _logger.LogWarning("Cannot optimize route with less than 2 locations (after adding branches)");
            return new RouteOptimizationResult(new List<Location>(), 0.0, TimeSpan.Zero, new double[0, 0], new double[0, 0], TimeSpan.Zero, new List<int>(), 0.0, 0.0, TimeSpan.Zero, new List<int>());
        }

        var optimizer = new AntColonyOptimizer(distanceMatrix, _logger);

        var startIndex = FindLocationIndex(distanceMatrix, startBase, "startBase");
        var endIndex = FindLocationIndex(distanceMatrix, endBase, "endBase");

        var route = optimizer.FindOptimalRoute(startIndex, endIndex);

        _logger.LogInformation("=== OPTIMIZED ROUTE (indices) ===");
        _logger.LogInformation("Route indices: [{Indices}]", string.Join(", ", route));

        var totalDistance = TravelTimeCalculator.CalculateTotalDistance(route, distanceMatrix.Matrix);

        var (distanceFromStartBase, distanceToEndBase) = CalculateBaseDistances(
            distanceMatrix, route, startIndex, endIndex);
        totalDistance += distanceFromStartBase;
        if (endIndex.HasValue && route.Count > 0 && route.Last() != endIndex.Value)
        {
            totalDistance += distanceToEndBase;
        }

        _logger.LogInformation("Total calculated distance (including start and return): {Distance:F2} km", totalDistance);

        var estimatedTime = TravelTimeCalculator.CalculateMixedTravelTime(route, distanceMatrix, transportMode);

        var (fullRoute, fullRouteIndices) = BuildFullRoute(distanceMatrix, route, startIndex, endIndex);

        var (travelTimeFromStartBase, travelTimeToEndBase) = TravelTimeCalculator.CalculateBaseTravelTimes(
            distanceMatrix, route, startIndex, endIndex, distanceFromStartBase, distanceToEndBase, transportMode);

        var totalTravelTime = estimatedTime + travelTimeFromStartBase + travelTimeToEndBase;

        _logger.LogInformation(
            "Route optimized: {LocationCount} locations (full route), {Distance:F2} km, {Time} (real travel time from OSRM)",
            fullRoute.Count, totalDistance, totalTravelTime);

        var segmentDirections = await _routeDirectionsBuilder.GetRouteDirectionsAsync(fullRoute, distanceMatrix, transportMode);
        _logger.LogInformation("Retrieved directions for {Count} segments", segmentDirections.Count);

        var totalBriefingDebriefingTime = CalculateTotalBriefingDebriefingTime(fullRoute);
        _logger.LogInformation("Total on-site time (briefing + work + debriefing): {Time}", totalBriefingDebriefingTime);

        var totalTimeWithBriefing = totalTravelTime + totalBriefingDebriefingTime;
        _logger.LogInformation("Total time (travel + on-site): {Time}", totalTimeWithBriefing);

        return new RouteOptimizationResult(
            fullRoute,
            totalDistance,
            totalTimeWithBriefing,
            distanceMatrix.Matrix,
            distanceMatrix.DurationMatrix,
            travelTimeFromStartBase,
            route,
            distanceFromStartBase,
            distanceToEndBase,
            travelTimeToEndBase,
            fullRouteIndices,
            distanceMatrix.DurationMatricesByProfile,
            transportMode,
            segmentDirections,
            totalBriefingDebriefingTime);
    }

    public async Task<RouteOptimizationResult> OptimizeRouteByShiftIdsAsync(
        List<Guid> shiftIds,
        string? startBase = null,
        string? endBase = null,
        ContainerTransportMode transportMode = ContainerTransportMode.ByCar,
        List<TimeBlock>? timeBlocks = null,
        TimeOnly? containerFromTime = null)
    {
        _logger.LogInformation("OptimizeRouteByShiftIdsAsync called with {Count} shift IDs", shiftIds.Count);

        var shifts = await _shiftRepository.GetQueryWithClient()
            .Where(s => shiftIds.Contains(s.Id))
            .ToListAsync();

        _logger.LogInformation("Loaded {Count} shifts from DB", shifts.Count);

        var locations = ExtractLocationsFromShifts(shifts, transportMode);

        if (locations.Count < 2)
        {
            _logger.LogWarning("Not enough geocoded locations ({Count}). Need at least 2.", locations.Count);
            return new RouteOptimizationResult(new List<Location>(), 0.0, TimeSpan.Zero, new double[0, 0], new double[0, 0], TimeSpan.Zero, new List<int>(), 0.0, 0.0, TimeSpan.Zero, new List<int>());
        }

        var (distMatrix, durationMatrix, durationMatricesByProfile) = await _distanceMatrixBuilder.BuildDistanceMatrixAsync(locations, transportMode);
        var distanceMatrix = new DistanceMatrix(locations, distMatrix, durationMatrix, durationMatricesByProfile);

        distanceMatrix = await AddBranchesToDistanceMatrixAsync(distanceMatrix, startBase, endBase, transportMode);

        if (distanceMatrix.Locations.Count < 2)
        {
            return new RouteOptimizationResult(new List<Location>(), 0.0, TimeSpan.Zero, new double[0, 0], new double[0, 0], TimeSpan.Zero, new List<int>(), 0.0, 0.0, TimeSpan.Zero, new List<int>());
        }

        var optimizer = new AntColonyOptimizer(distanceMatrix, _logger);
        var startIndex = FindLocationIndex(distanceMatrix, startBase, "startBase");
        var endIndex = FindLocationIndex(distanceMatrix, endBase, "endBase");
        var route = optimizer.FindOptimalRoute(startIndex, endIndex);

        var totalDistance = TravelTimeCalculator.CalculateTotalDistance(route, distanceMatrix.Matrix);
        var (distanceFromStartBase, distanceToEndBase) = CalculateBaseDistances(distanceMatrix, route, startIndex, endIndex);
        totalDistance += distanceFromStartBase;
        if (endIndex.HasValue && route.Count > 0 && route.Last() != endIndex.Value)
        {
            totalDistance += distanceToEndBase;
        }

        var estimatedTime = TravelTimeCalculator.CalculateMixedTravelTime(route, distanceMatrix, transportMode);
        var (fullRoute, fullRouteIndices) = BuildFullRoute(distanceMatrix, route, startIndex, endIndex);
        var (travelTimeFromStartBase, travelTimeToEndBase) = TravelTimeCalculator.CalculateBaseTravelTimes(
            distanceMatrix, route, startIndex, endIndex, distanceFromStartBase, distanceToEndBase, transportMode);
        var totalTravelTime = estimatedTime + travelTimeFromStartBase + travelTimeToEndBase;

        var segmentDirections = await _routeDirectionsBuilder.GetRouteDirectionsAsync(fullRoute, distanceMatrix, transportMode);
        var totalBriefingDebriefingTime = CalculateTotalBriefingDebriefingTime(fullRoute);
        var totalTimeWithBriefing = totalTravelTime + totalBriefingDebriefingTime;

        _logger.LogInformation("Route optimized: {Count} stops, {Distance:F2} km, {Time}", fullRoute.Count, totalDistance, totalTimeWithBriefing);

        List<PlacedTimeBlock>? placedTimeBlocks = null;
        if (timeBlocks != null && timeBlocks.Count > 0)
        {
            var unmovable = timeBlocks.Where(b => !b.IsMovable).ToList();
            var movable = timeBlocks.Where(b => b.IsMovable).ToList();
            var startIdx = startIndex ?? 0;
            var endIdx = endIndex ?? (route.Count > 0 ? route.Last() : 0);
            var containerFromTimeSeconds = containerFromTime?.ToTimeSpan().TotalSeconds ?? 0.0;

            var placedUnmovable = TimeBlockScheduler.PlaceUnmovableBlocks(
                unmovable, route, distanceMatrix, startIdx, containerFromTimeSeconds);
            var placedMovable = TimeBlockScheduler.PlaceMovableBlocks(
                movable, route, distanceMatrix, startIdx, endIdx,
                containerFromTimeSeconds, placedUnmovable);
            placedTimeBlocks = placedUnmovable.Concat(placedMovable).ToList();
        }

        return new RouteOptimizationResult(
            fullRoute, totalDistance, totalTimeWithBriefing,
            distanceMatrix.Matrix, distanceMatrix.DurationMatrix,
            travelTimeFromStartBase, route, distanceFromStartBase, distanceToEndBase, travelTimeToEndBase,
            fullRouteIndices, distanceMatrix.DurationMatricesByProfile, transportMode,
            segmentDirections, totalBriefingDebriefingTime, placedTimeBlocks);
    }

    private static List<Location> ExtractLocationsFromShifts(List<Shift> shifts, ContainerTransportMode transportMode)
    {
        var locations = new List<Location>();
        var uniqueAddresses = new HashSet<string>();

        foreach (var shift in shifts)
        {
            if (shift.Client?.Addresses == null || !shift.Client.Addresses.Any())
            {
                continue;
            }

            var address = shift.Client.Addresses.First();
            if (string.IsNullOrEmpty(address.City) || !address.Latitude.HasValue || !address.Longitude.HasValue)
            {
                continue;
            }

            var addressKey = $"{address.City}_{address.Zip}_{address.Street}";
            if (uniqueAddresses.Contains(addressKey))
            {
                continue;
            }

            uniqueAddresses.Add(addressKey);
            var fullAddress = $"{address.Street}, {address.Zip} {address.City}";

            locations.Add(new Location
            {
                Name = shift.Client.Name ?? "Unknown",
                Address = fullAddress,
                Latitude = address.Latitude.Value,
                Longitude = address.Longitude.Value,
                ShiftId = shift.Id,
                TransportMode = transportMode == ContainerTransportMode.Mix
                    ? TransportMode.ByCar
                    : (TransportMode)(int)transportMode,
                BriefingTime = shift.BriefingTime.ToTimeSpan(),
                DebriefingTime = shift.DebriefingTime.ToTimeSpan(),
                WorkTime = TimeSpan.FromHours((double)shift.WorkTime),
                TimeRangeStart = shift.StartShift,
                TimeRangeEnd = shift.EndShift
            });
        }

        return locations;
    }

    public async Task<DistanceMatrix> CalculateDistanceMatrixForLocationsAsync(
        List<Location> locations,
        ContainerTransportMode transportMode)
    {
        if (locations.Count < 2)
        {
            return new DistanceMatrix(locations, new double[0, 0], new double[0, 0]);
        }

        var (distanceMatrix, durationMatrix, durationMatricesByProfile) = await _distanceMatrixBuilder.BuildDistanceMatrixAsync(locations, transportMode);
        return new DistanceMatrix(locations, distanceMatrix, durationMatrix, durationMatricesByProfile);
    }

    private int? FindLocationIndex(DistanceMatrix distanceMatrix, string? baseAddress, string label)
    {
        if (string.IsNullOrEmpty(baseAddress))
        {
            return null;
        }

        var index = distanceMatrix.Locations.FindIndex(l =>
            l.Address == baseAddress ||
            l.Address.Equals(baseAddress, StringComparison.OrdinalIgnoreCase) ||
            l.Address.Contains(baseAddress, StringComparison.OrdinalIgnoreCase) ||
            baseAddress.Contains(l.Address, StringComparison.OrdinalIgnoreCase));
        _logger.LogInformation("Looking for {Label} '{BaseAddress}' in locations, found index: {Index}", label, baseAddress, index);

        if (index == -1)
        {
            _logger.LogWarning("{Label} {BaseAddress} not found in locations. Available addresses: [{Addresses}]",
                label,
                baseAddress,
                string.Join(", ", distanceMatrix.Locations.Select(l => l.Address)));
            return null;
        }

        return index;
    }

    private (double distanceFromStart, double distanceToEnd) CalculateBaseDistances(
        DistanceMatrix distanceMatrix, List<int> route, int? startIndex, int? endIndex)
    {
        _logger.LogInformation("=== ROUTE DISTANCE CALCULATION ===");

        double distanceFromStartBase = 0;
        if (startIndex.HasValue && route.Count > 0 && route[0] != startIndex.Value)
        {
            distanceFromStartBase = distanceMatrix.Matrix[startIndex.Value, route[0]];
            _logger.LogInformation("  Start: [{FromIdx}]{FromName} -> [{ToIdx}]{ToName} = {Distance:F2} km",
                startIndex.Value, distanceMatrix.Locations[startIndex.Value].Name,
                route[0], distanceMatrix.Locations[route[0]].Name,
                distanceFromStartBase);
        }

        for (int i = 0; i < route.Count - 1; i++)
        {
            var fromIdx = route[i];
            var toIdx = route[i + 1];
            var segmentDistance = distanceMatrix.Matrix[fromIdx, toIdx];
            _logger.LogInformation("  Segment {I}: [{FromIdx}]{FromName} -> [{ToIdx}]{ToName} = {Distance:F2} km",
                i, fromIdx, distanceMatrix.Locations[fromIdx].Name,
                toIdx, distanceMatrix.Locations[toIdx].Name,
                segmentDistance);
        }

        double distanceToEndBase = 0;
        if (endIndex.HasValue && route.Count > 0)
        {
            if (route.Last() != endIndex.Value)
            {
                var lastStopIndex = route.Last();
                distanceToEndBase = distanceMatrix.Matrix[lastStopIndex, endIndex.Value];
                _logger.LogInformation("  Return: [{FromIdx}]{FromName} -> [{ToIdx}]{ToName} = {Distance:F2} km",
                    lastStopIndex, distanceMatrix.Locations[lastStopIndex].Name,
                    endIndex.Value, distanceMatrix.Locations[endIndex.Value].Name,
                    distanceToEndBase);
            }
            else if (route.Count >= 2)
            {
                var secondToLastIndex = route[route.Count - 2];
                distanceToEndBase = distanceMatrix.Matrix[secondToLastIndex, endIndex.Value];
                _logger.LogInformation("  Return (last segment): [{FromIdx}]{FromName} -> [{ToIdx}]{ToName} = {Distance:F2} km",
                    secondToLastIndex, distanceMatrix.Locations[secondToLastIndex].Name,
                    endIndex.Value, distanceMatrix.Locations[endIndex.Value].Name,
                    distanceToEndBase);
            }
        }

        return (distanceFromStartBase, distanceToEndBase);
    }

    private (List<Location> fullRoute, List<int> fullRouteIndices) BuildFullRoute(
        DistanceMatrix distanceMatrix, List<int> route, int? startIndex, int? endIndex)
    {
        var fullRoute = new List<Location>();
        var fullRouteIndices = new List<int>();

        if (startIndex.HasValue && route.Count > 0 && route[0] != startIndex.Value)
        {
            fullRoute.Add(distanceMatrix.Locations[startIndex.Value]);
            fullRouteIndices.Add(startIndex.Value);
        }

        fullRoute.AddRange(route.Select(index => distanceMatrix.Locations[index]));
        fullRouteIndices.AddRange(route);

        if (endIndex.HasValue && route.Count > 0 && route.Last() != endIndex.Value)
        {
            fullRoute.Add(distanceMatrix.Locations[endIndex.Value]);
            fullRouteIndices.Add(endIndex.Value);
        }

        return (fullRoute, fullRouteIndices);
    }

    private async Task<List<Location>> ExtractLocationsFromTemplateAsync(ContainerTemplate template)
    {
        var locations = new List<Location>();
        var uniqueAddresses = new HashSet<string>();

        _logger.LogInformation("Extracting locations from {Count} template items", template.ContainerTemplateItems.Count);

        foreach (var item in template.ContainerTemplateItems)
        {
            if (item.Shift == null)
            {
                _logger.LogWarning("Item {ItemId} has no Shift", item.Id);
                continue;
            }

            if (item.Shift.Client == null)
            {
                _logger.LogWarning("Shift {ShiftId} has no Client", item.ShiftId);
                continue;
            }

            if (item.Shift.Client.Addresses == null || !item.Shift.Client.Addresses.Any())
            {
                _logger.LogWarning("Client {ClientName} has no addresses", item.Shift.Client.Name);
                continue;
            }

            var address = item.Shift.Client.Addresses.First();
            var addressKey = $"{address.City}_{address.Zip}_{address.Street}";

            if (uniqueAddresses.Contains(addressKey))
            {
                _logger.LogDebug("Duplicate address skipped: {Address}", addressKey);
                continue;
            }

            if (string.IsNullOrEmpty(address.City))
            {
                _logger.LogWarning("Address has no city for client {ClientName}", item.Shift.Client.Name);
                continue;
            }

            var fullAddress = $"{address.Street}, {address.Zip} {address.City}";

            double? lat = address.Latitude;
            double? lon = address.Longitude;

            if (!lat.HasValue || !lon.HasValue)
            {
                _logger.LogInformation("Geocoding address (no stored coordinates): {FullAddress}", fullAddress);
                var coords = await _geocodingService.GeocodeAddressAsync(
                    fullAddress,
                    address.Country ?? "Switzerland");
                lat = coords.Latitude;
                lon = coords.Longitude;
            }

            if (lat.HasValue && lon.HasValue)
            {
                var briefingTime = item.BriefingTime.ToTimeSpan();
                var debriefingTime = item.DebriefingTime.ToTimeSpan();
                var workTime = TimeSpan.FromHours((double)item.Shift.WorkTime);

                locations.Add(new Location
                {
                    Name = item.Shift.Client.Name ?? "Unknown",
                    Address = fullAddress,
                    Latitude = lat.Value,
                    Longitude = lon.Value,
                    ShiftId = item.ShiftId ?? Guid.Empty,
                    TransportMode = item.TransportMode,
                    BriefingTime = briefingTime,
                    DebriefingTime = debriefingTime,
                    WorkTime = workTime
                });

                uniqueAddresses.Add(addressKey);
                _logger.LogInformation("Location added: {Name} at ({Lat}, {Lon}), TransportMode: {TransportMode}, TotalOnSiteTime: {TotalOnSiteTime} (Briefing: {BriefingTime}, Work: {WorkTime}, Debriefing: {DebriefingTime})",
                    item.Shift.Client.Name, lat.Value, lon.Value, item.TransportMode,
                    briefingTime + workTime + debriefingTime, briefingTime, workTime, debriefingTime);
            }
            else
            {
                _logger.LogWarning("Failed to geocode address: {City}, {Country}",
                    address.City, address.Country ?? "Switzerland");
            }
        }

        _logger.LogInformation("Extracted {Count} unique locations", locations.Count);
        return locations;
    }

    private TimeSpan CalculateTotalBriefingDebriefingTime(List<Location> locations)
    {
        var totalTime = TimeSpan.Zero;
        foreach (var location in locations)
        {
            if (location.ShiftId != Guid.Empty)
            {
                _logger.LogInformation(
                    "Location {Name}: TotalOnSiteTime={TotalOnSiteTime} (Briefing={BriefingTime}, Work={WorkTime}, Debriefing={DebriefingTime})",
                    location.Name, location.TotalOnSiteTime, location.BriefingTime, location.WorkTime, location.DebriefingTime);
                totalTime += location.TotalOnSiteTime;
            }
        }
        _logger.LogInformation("Total on-site time (briefing + work + debriefing) calculated: {TotalTime}", totalTime);
        return totalTime;
    }

    private async Task<DistanceMatrix> AddBranchesToDistanceMatrixAsync(
        DistanceMatrix existingMatrix,
        string? startBaseAddress,
        string? endBaseAddress,
        ContainerTransportMode transportMode)
    {
        var addressesToAdd = new List<string>();
        if (!string.IsNullOrEmpty(startBaseAddress) && !existingMatrix.Locations.Any(l => l.Address == startBaseAddress))
        {
            addressesToAdd.Add(startBaseAddress);
        }
        if (!string.IsNullOrEmpty(endBaseAddress) && endBaseAddress != startBaseAddress &&
            !existingMatrix.Locations.Any(l => l.Address == endBaseAddress))
        {
            addressesToAdd.Add(endBaseAddress);
        }

        if (addressesToAdd.Count == 0)
        {
            return existingMatrix;
        }

        var newLocations = new List<Location>(existingMatrix.Locations);

        foreach (var address in addressesToAdd)
        {
            _logger.LogInformation("Geocoding base address: {Address}", address);

            var coords = await _geocodingService.GeocodeAddressAsync(address, "Switzerland");

            if (coords.Latitude.HasValue && coords.Longitude.HasValue)
            {
                newLocations.Add(new Location
                {
                    Name = "Base",
                    Address = address,
                    Latitude = coords.Latitude.Value,
                    Longitude = coords.Longitude.Value,
                    ShiftId = Guid.Empty
                });
                _logger.LogInformation("Added base at {Address} to distance matrix with coords ({Lat}, {Lon})",
                    address, coords.Latitude.Value, coords.Longitude.Value);
            }
            else
            {
                _logger.LogWarning("Could not geocode base address {Address}",
                    address);
            }
        }

        var (distanceMatrix, durationMatrix, durationMatricesByProfile) = await _distanceMatrixBuilder.BuildDistanceMatrixAsync(newLocations, transportMode);
        return new DistanceMatrix(newLocations, distanceMatrix, durationMatrix, durationMatricesByProfile);
    }
}

public record Location
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public Guid ShiftId { get; init; }
    public TransportMode TransportMode { get; init; } = TransportMode.ByCar;
    public TimeSpan BriefingTime { get; init; } = TimeSpan.Zero;
    public TimeSpan DebriefingTime { get; init; } = TimeSpan.Zero;
    public TimeSpan WorkTime { get; init; } = TimeSpan.Zero;
    public TimeSpan TotalOnSiteTime => BriefingTime + WorkTime + DebriefingTime;
    public TimeOnly? TimeRangeStart { get; init; }
    public TimeOnly? TimeRangeEnd { get; init; }
}

public record DistanceMatrix(
    List<Location> Locations,
    double[,] Matrix,
    double[,] DurationMatrix,
    Dictionary<string, double[,]>? DurationMatricesByProfile = null);

public record RouteOptimizationResult(
    List<Location> OptimizedRoute,
    double TotalDistanceKm,
    TimeSpan EstimatedTravelTime,
    double[,] DistanceMatrix,
    double[,] DurationMatrix,
    TimeSpan TravelTimeFromStartBase,
    List<int> RouteIndices,
    double DistanceFromStartBaseKm,
    double DistanceToEndBaseKm,
    TimeSpan TravelTimeToEndBase,
    List<int> FullRouteIndices,
    Dictionary<string, double[,]>? DurationMatricesByProfile = null,
    ContainerTransportMode TransportMode = ContainerTransportMode.ByCar,
    List<RouteSegmentDirections>? SegmentDirections = null,
    TimeSpan TotalBriefingDebriefingTime = default,
    List<PlacedTimeBlock>? PlacedTimeBlocks = null);

public record RouteSegmentDirections(
    string FromName,
    string ToName,
    string TransportMode,
    double DistanceKm,
    TimeSpan Duration,
    List<DirectionStep> Steps);

public record DirectionStep(
    string Instruction,
    string StreetName,
    double DistanceMeters,
    int DurationSeconds,
    string ManeuverType);
