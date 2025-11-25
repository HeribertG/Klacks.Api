using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public interface IRouteOptimizationService
{
    Task<DistanceMatrix> CalculateDistanceMatrixAsync(Guid containerId, int weekday, bool isHoliday);
    Task<RouteOptimizationResult> OptimizeRouteAsync(Guid containerId, int weekday, bool isHoliday, string? startBase = null, string? endBase = null);
}

public class RouteOptimizationService : IRouteOptimizationService
{
    private readonly IContainerTemplateRepository _containerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RouteOptimizationService> _logger;
    private const double EARTH_RADIUS_KM = 6371.0;

    public RouteOptimizationService(
        IContainerTemplateRepository containerRepository,
        IBranchRepository branchRepository,
        IGeocodingService geocodingService,
        IMemoryCache cache,
        ILogger<RouteOptimizationService> logger)
    {
        _containerRepository = containerRepository;
        _branchRepository = branchRepository;
        _geocodingService = geocodingService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DistanceMatrix> CalculateDistanceMatrixAsync(Guid containerId, int weekday, bool isHoliday)
    {
        _logger.LogInformation(
            "Calculating distance matrix for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}",
            containerId, weekday, isHoliday);

        var templates = await _containerRepository.GetTemplatesForContainer(containerId);
        var template = templates.FirstOrDefault(t => t.Weekday == weekday && t.IsHoliday == isHoliday);

        if (template == null)
        {
            _logger.LogWarning("No template found for given criteria");
            return new DistanceMatrix(new List<Location>(), new double[0, 0]);
        }

        var locations = await ExtractLocationsFromTemplateAsync(template);

        if (locations.Count < 2)
        {
            _logger.LogWarning("Not enough locations found ({Count}). Need at least 2.", locations.Count);
            return new DistanceMatrix(locations, new double[0, 0]);
        }

        var matrix = await BuildDistanceMatrixAsync(locations);

        _logger.LogInformation("Distance matrix calculated: {Size}x{Size}", locations.Count, locations.Count);
        return new DistanceMatrix(locations, matrix);
    }

    public async Task<RouteOptimizationResult> OptimizeRouteAsync(
        Guid containerId,
        int weekday,
        bool isHoliday,
        string? startBase = null,
        string? endBase = null)
    {
        var distanceMatrix = await CalculateDistanceMatrixAsync(containerId, weekday, isHoliday);

        if (distanceMatrix.Locations.Count < 2)
        {
            _logger.LogWarning("Cannot optimize route with less than 2 locations");
            return new RouteOptimizationResult(new List<Location>(), 0.0, TimeSpan.Zero, new double[0, 0], TimeSpan.Zero);
        }

        distanceMatrix = await AddBranchesToDistanceMatrixAsync(distanceMatrix, startBase, endBase);

        var optimizer = new AntColonyOptimizer(distanceMatrix, _logger);

        int? startIndex = null;
        int? endIndex = null;

        if (!string.IsNullOrEmpty(startBase))
        {
            startIndex = distanceMatrix.Locations.FindIndex(l => l.Address == startBase);
            if (startIndex == -1)
            {
                _logger.LogWarning("StartBase {StartBase} not found in locations", startBase);
                startIndex = null;
            }
        }

        if (!string.IsNullOrEmpty(endBase))
        {
            endIndex = distanceMatrix.Locations.FindIndex(l => l.Address == endBase);
            if (endIndex == -1)
            {
                _logger.LogWarning("EndBase {EndBase} not found in locations", endBase);
                endIndex = null;
            }
        }

        var route = optimizer.FindOptimalRoute(startIndex, endIndex);

        var totalDistance = CalculateTotalDistance(route, distanceMatrix.Matrix);
        var estimatedTime = EstimateTravelTime(totalDistance);

        var orderedLocations = route.Select(index => distanceMatrix.Locations[index]).ToList();

        TimeSpan travelTimeFromStartBase = TimeSpan.Zero;
        if (startIndex.HasValue && route.Count > 0)
        {
            _logger.LogInformation(
                "StartBase index: {StartIndex}, First route index: {FirstRoute}, StartBase address: {StartAddress}, First location name: {FirstName}",
                startIndex.Value, route[0],
                distanceMatrix.Locations[startIndex.Value].Address,
                distanceMatrix.Locations[route[0]].Name);

            var distanceFromStart = distanceMatrix.Matrix[startIndex.Value, route[0]];
            travelTimeFromStartBase = EstimateTravelTime(distanceFromStart);
            _logger.LogInformation(
                "Travel from StartBase to first location: {Distance:F2} km, {Time}",
                distanceFromStart, travelTimeFromStartBase);
        }

        _logger.LogInformation(
            "Route optimized: {LocationCount} locations, {Distance:F2} km, {Time}",
            orderedLocations.Count, totalDistance, estimatedTime);

        return new RouteOptimizationResult(orderedLocations, totalDistance, estimatedTime, distanceMatrix.Matrix, travelTimeFromStartBase);
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

            _logger.LogInformation("Geocoding address: {City}, {Country}", address.City, address.Country ?? "Switzerland");

            var coords = await _geocodingService.GeocodeAsync(
                address.City,
                address.Country ?? "Switzerland");

            if (coords.Latitude.HasValue && coords.Longitude.HasValue)
            {
                locations.Add(new Location
                {
                    Name = item.Shift.Client.Name ?? "Unknown",
                    Address = $"{address.Street}, {address.Zip} {address.City}",
                    Latitude = coords.Latitude.Value,
                    Longitude = coords.Longitude.Value,
                    ShiftId = item.ShiftId
                });

                uniqueAddresses.Add(addressKey);
                _logger.LogInformation("Location added: {Name} at ({Lat}, {Lon})",
                    item.Shift.Client.Name, coords.Latitude.Value, coords.Longitude.Value);
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

    private async Task<double[,]> BuildDistanceMatrixAsync(List<Location> locations)
    {
        var size = locations.Count;
        var matrix = new double[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i == j)
                {
                    matrix[i, j] = 0.0;
                }
                else
                {
                    var cacheKey = $"distance_{locations[i].Latitude}_{locations[i].Longitude}_{locations[j].Latitude}_{locations[j].Longitude}";

                    if (!_cache.TryGetValue(cacheKey, out double distance))
                    {
                        distance = CalculateHaversineDistance(
                            locations[i].Latitude, locations[i].Longitude,
                            locations[j].Latitude, locations[j].Longitude);

                        _cache.Set(cacheKey, distance, TimeSpan.FromDays(30));
                    }

                    matrix[i, j] = distance;
                }
            }
        }

        return matrix;
    }

    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EARTH_RADIUS_KM * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private double CalculateTotalDistance(List<int> route, double[,] matrix)
    {
        double total = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            total += matrix[route[i], route[i + 1]];
        }
        return total;
    }

    private TimeSpan EstimateTravelTime(double distanceKm)
    {
        const double AVERAGE_SPEED_KMH = 50.0;
        var hours = distanceKm / AVERAGE_SPEED_KMH;
        return TimeSpan.FromHours(hours);
    }

    private async Task<DistanceMatrix> AddBranchesToDistanceMatrixAsync(
        DistanceMatrix existingMatrix,
        string? startBaseAddress,
        string? endBaseAddress)
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
            var addressParts = address.Split(',');
            var city = addressParts.Length > 1 ? addressParts[addressParts.Length - 1].Trim() : address;

            _logger.LogInformation("Geocoding base address: {Address}, City: {City}", address, city);

            var coords = await _geocodingService.GeocodeAsync(city, "Switzerland");

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

        var newMatrix = await BuildDistanceMatrixAsync(newLocations);
        return new DistanceMatrix(newLocations, newMatrix);
    }
}

public record Location
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public Guid ShiftId { get; init; }
}

public record DistanceMatrix(List<Location> Locations, double[,] Matrix);

public record RouteOptimizationResult(
    List<Location> OptimizedRoute,
    double TotalDistanceKm,
    TimeSpan EstimatedTravelTime,
    double[,] DistanceMatrix,
    TimeSpan TravelTimeFromStartBase);
