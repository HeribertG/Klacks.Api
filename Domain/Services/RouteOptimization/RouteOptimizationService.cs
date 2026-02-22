using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class RouteOptimizationService : IRouteOptimizationService
{
    private readonly IContainerTemplateRepository _containerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly IGeocodingService _geocodingService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RouteOptimizationService> _logger;
    private readonly HttpClient _httpClient;
    private const double EARTH_RADIUS_KM = 6371.0;
    private const string OSRM_BASE_URL = "https://router.project-osrm.org";
    private const string OPENROUTESERVICE_BASE_URL = "https://api.openrouteservice.org/v2";
    private const string OPENROUTESERVICE_API_KEY_SETTING = "OPENROUTESERVICE_API_KEY";

    public RouteOptimizationService(
        IContainerTemplateRepository containerRepository,
        IBranchRepository branchRepository,
        ISettingsRepository settingsRepository,
        ISettingsEncryptionService encryptionService,
        IGeocodingService geocodingService,
        IMemoryCache cache,
        ILogger<RouteOptimizationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _containerRepository = containerRepository;
        _branchRepository = branchRepository;
        _settingsRepository = settingsRepository;
        _encryptionService = encryptionService;
        _geocodingService = geocodingService;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    private async Task<string?> GetOpenRouteServiceApiKeyAsync()
    {
        var setting = await _settingsRepository.GetSetting(OPENROUTESERVICE_API_KEY_SETTING);
        if (setting == null || string.IsNullOrEmpty(setting.Value))
        {
            return null;
        }

        return _encryptionService.ProcessForReading(setting.Type, setting.Value);
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

        var (distanceMatrix, durationMatrix, durationMatricesByProfile) = await BuildDistanceMatrixAsync(locations, transportMode);

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

        int? startIndex = null;
        int? endIndex = null;

        if (!string.IsNullOrEmpty(startBase))
        {
            startIndex = distanceMatrix.Locations.FindIndex(l =>
                l.Address == startBase ||
                l.Address.Equals(startBase, StringComparison.OrdinalIgnoreCase) ||
                l.Address.Contains(startBase, StringComparison.OrdinalIgnoreCase) ||
                startBase.Contains(l.Address, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Looking for startBase '{StartBase}' in locations, found index: {Index}", startBase, startIndex);
            if (startIndex == -1)
            {
                _logger.LogWarning("StartBase {StartBase} not found in locations. Available addresses: [{Addresses}]",
                    startBase,
                    string.Join(", ", distanceMatrix.Locations.Select(l => l.Address)));
                startIndex = null;
            }
        }

        if (!string.IsNullOrEmpty(endBase))
        {
            endIndex = distanceMatrix.Locations.FindIndex(l =>
                l.Address == endBase ||
                l.Address.Equals(endBase, StringComparison.OrdinalIgnoreCase) ||
                l.Address.Contains(endBase, StringComparison.OrdinalIgnoreCase) ||
                endBase.Contains(l.Address, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Looking for endBase '{EndBase}' in locations, found index: {Index}", endBase, endIndex);
            if (endIndex == -1)
            {
                _logger.LogWarning("EndBase {EndBase} not found in locations. Available addresses: [{Addresses}]",
                    endBase,
                    string.Join(", ", distanceMatrix.Locations.Select(l => l.Address)));
                endIndex = null;
            }
        }

        var route = optimizer.FindOptimalRoute(startIndex, endIndex);

        _logger.LogInformation("=== OPTIMIZED ROUTE (indices) ===");
        _logger.LogInformation("Route indices: [{Indices}]", string.Join(", ", route));

        var totalDistance = CalculateTotalDistance(route, distanceMatrix.Matrix);

        _logger.LogInformation("=== ROUTE DISTANCE CALCULATION ===");

        double distanceFromStartBase = 0;
        if (startIndex.HasValue && route.Count > 0 && route[0] != startIndex.Value)
        {
            distanceFromStartBase = distanceMatrix.Matrix[startIndex.Value, route[0]];
            _logger.LogInformation("  Start: [{FromIdx}]{FromName} -> [{ToIdx}]{ToName} = {Distance:F2} km",
                startIndex.Value, distanceMatrix.Locations[startIndex.Value].Name,
                route[0], distanceMatrix.Locations[route[0]].Name,
                distanceFromStartBase);
            totalDistance += distanceFromStartBase;
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
                totalDistance += distanceToEndBase;
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

        _logger.LogInformation("Total calculated distance (including start and return): {Distance:F2} km", totalDistance);

        var estimatedTime = CalculateMixedTravelTime(route, distanceMatrix, transportMode);

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

        TimeSpan travelTimeFromStartBase = TimeSpan.Zero;
        if (distanceFromStartBase > 0 && startIndex.HasValue && route.Count > 0)
        {
            travelTimeFromStartBase = GetMixedTravelTimeForSegment(startIndex.Value, route[0], distanceMatrix, transportMode);
            _logger.LogInformation(
                "Travel from StartBase to first location: {Distance:F2} km, {Time} (from OSRM)",
                distanceFromStartBase, travelTimeFromStartBase);
        }

        TimeSpan travelTimeToEndBase = TimeSpan.Zero;
        if (distanceToEndBase > 0 && endIndex.HasValue && route.Count > 0)
        {
            var lastRouteIndex = route.Last() != endIndex.Value ? route.Last() : (route.Count >= 2 ? route[route.Count - 2] : route[0]);
            travelTimeToEndBase = GetMixedTravelTimeForSegment(lastRouteIndex, endIndex.Value, distanceMatrix, transportMode);
            _logger.LogInformation(
                "Travel from last location to EndBase: {Distance:F2} km, {Time} (from OSRM)",
                distanceToEndBase, travelTimeToEndBase);
        }

        var totalTravelTime = estimatedTime + travelTimeFromStartBase + travelTimeToEndBase;

        _logger.LogInformation(
            "Route optimized: {LocationCount} locations (full route), {Distance:F2} km, {Time} (real travel time from OSRM)",
            fullRoute.Count, totalDistance, totalTravelTime);

        var segmentDirections = await GetRouteDirectionsAsync(fullRoute, distanceMatrix, transportMode);
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
            _logger.LogInformation("Geocoding full address: {FullAddress}, Country: {Country}", fullAddress, address.Country ?? "Switzerland");

            var coords = await _geocodingService.GeocodeAddressAsync(
                fullAddress,
                address.Country ?? "Switzerland");

            if (coords.Latitude.HasValue && coords.Longitude.HasValue)
            {
                var briefingTime = item.BriefingTime.ToTimeSpan();
                var debriefingTime = item.DebriefingTime.ToTimeSpan();
                var workTime = TimeSpan.FromHours((double)item.Shift.WorkTime);

                locations.Add(new Location
                {
                    Name = item.Shift.Client.Name ?? "Unknown",
                    Address = fullAddress,
                    Latitude = coords.Latitude.Value,
                    Longitude = coords.Longitude.Value,
                    ShiftId = item.ShiftId,
                    TransportMode = item.TransportMode,
                    BriefingTime = briefingTime,
                    DebriefingTime = debriefingTime,
                    WorkTime = workTime
                });

                uniqueAddresses.Add(addressKey);
                _logger.LogInformation("Location added: {Name} at ({Lat}, {Lon}), TransportMode: {TransportMode}, TotalOnSiteTime: {TotalOnSiteTime} (Briefing: {BriefingTime}, Work: {WorkTime}, Debriefing: {DebriefingTime})",
                    item.Shift.Client.Name, coords.Latitude.Value, coords.Longitude.Value, item.TransportMode,
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

    private async Task<(double[,] distanceMatrix, double[,] durationMatrix, Dictionary<string, double[,]>? durationMatricesByProfile)> BuildDistanceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode)
    {
        _logger.LogInformation("=== BuildDistanceMatrixAsync START ===");
        _logger.LogInformation("TransportMode received: {TransportMode} (int value: {IntValue})", transportMode, (int)transportMode);

        var apiKey = await GetOpenRouteServiceApiKeyAsync();
        var isOpenRouteServiceConfigured = !string.IsNullOrEmpty(apiKey);
        _logger.LogInformation("OpenRouteService configured: {IsConfigured}", isOpenRouteServiceConfigured);

        var size = locations.Count;
        var distanceMatrix = new double[size, size];
        var durationMatrix = new double[size, size];

        if (transportMode == ContainerTransportMode.Mix)
        {
            _logger.LogInformation("TransportMode is Mix - building mixed matrices for all profiles");
            return await BuildMixedDistanceMatrixAsync(locations);
        }

        var servicePrefix = isOpenRouteServiceConfigured ? "ors" : "osrm";
        var cacheKey = $"{servicePrefix}_matrix_{transportMode}_{string.Join("_", locations.Select(l => $"{l.Latitude:F6}_{l.Longitude:F6}"))}";
        _logger.LogInformation("TransportMode: {TransportMode}, Cache key: {CacheKey}", transportMode, cacheKey);

        if (_cache.TryGetValue(cacheKey, out (double[,] dist, double[,] dur)? cached) && cached != null)
        {
            _logger.LogInformation("CACHE HIT - Using cached distance/duration matrix for TransportMode '{TransportMode}'", transportMode);
            return (cached.Value.dist, cached.Value.dur, null);
        }

        _logger.LogInformation("CACHE MISS - Fetching new data for TransportMode '{TransportMode}'", transportMode);

        try
        {
            (double[,] distanceMatrix, double[,] durationMatrix) result;

            if (isOpenRouteServiceConfigured)
            {
                _logger.LogInformation("Using OpenRouteService API");
                result = await GetOpenRouteServiceMatrixAsync(locations, transportMode, apiKey!);
            }
            else
            {
                _logger.LogInformation("Using OSRM (public server - only driving profile supported for accurate times)");
                result = await GetOsrmDistanceMatrixAsync(locations, transportMode);
            }

            _cache.Set(cacheKey, result, TimeSpan.FromDays(7));
            _logger.LogInformation("Successfully retrieved distance/duration matrix");
            return (result.distanceMatrix, result.durationMatrix, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get routing matrix, falling back to Haversine distances with estimated times");
            distanceMatrix = BuildHaversineDistanceMatrix(locations);
            durationMatrix = BuildEstimatedDurationMatrix(distanceMatrix, transportMode);
            return (distanceMatrix, durationMatrix, null);
        }
    }

    private async Task<(double[,] distanceMatrix, double[,] durationMatrix, Dictionary<string, double[,]> durationMatricesByProfile)> BuildMixedDistanceMatrixAsync(
        List<Location> locations)
    {
        var size = locations.Count;
        var profiles = new[] { "driving", "cycling", "foot" };
        var durationMatricesByProfile = new Dictionary<string, double[,]>();
        double[,] distanceMatrix = new double[size, size];
        double[,] defaultDurationMatrix = new double[size, size];

        _logger.LogInformation("Building mixed transport matrices - fetching all profiles (driving, cycling, foot)");

        foreach (var profile in profiles)
        {
            var cacheKey = $"osrm_matrix_{profile}_{string.Join("_", locations.Select(l => $"{l.Latitude:F6}_{l.Longitude:F6}"))}";

            if (_cache.TryGetValue(cacheKey, out (double[,] dist, double[,] dur)? cached) && cached != null)
            {
                _logger.LogInformation("Using cached OSRM matrix for profile '{Profile}'", profile);
                durationMatricesByProfile[profile] = cached.Value.dur;
                if (profile == "driving")
                {
                    distanceMatrix = cached.Value.dist;
                    defaultDurationMatrix = cached.Value.dur;
                }
                continue;
            }

            try
            {
                var transportMode = profile switch
                {
                    "driving" => ContainerTransportMode.ByCar,
                    "cycling" => ContainerTransportMode.ByBicycle,
                    "foot" => ContainerTransportMode.ByFoot,
                    _ => ContainerTransportMode.ByCar
                };

                var result = await GetOsrmDistanceMatrixAsync(locations, transportMode);
                _cache.Set(cacheKey, result, TimeSpan.FromDays(7));

                durationMatricesByProfile[profile] = result.durationMatrix;

                if (profile == "driving")
                {
                    distanceMatrix = result.distanceMatrix;
                    defaultDurationMatrix = result.durationMatrix;
                }

                _logger.LogInformation("Retrieved OSRM matrix for profile '{Profile}'", profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get OSRM matrix for profile '{Profile}', using fallback", profile);

                var haversineMatrix = BuildHaversineDistanceMatrix(locations);
                var transportMode = profile switch
                {
                    "driving" => ContainerTransportMode.ByCar,
                    "cycling" => ContainerTransportMode.ByBicycle,
                    "foot" => ContainerTransportMode.ByFoot,
                    _ => ContainerTransportMode.ByCar
                };
                var fallbackDuration = BuildEstimatedDurationMatrix(haversineMatrix, transportMode);
                durationMatricesByProfile[profile] = fallbackDuration;

                if (profile == "driving")
                {
                    distanceMatrix = haversineMatrix;
                    defaultDurationMatrix = fallbackDuration;
                }
            }
        }

        return (distanceMatrix, defaultDurationMatrix, durationMatricesByProfile);
    }

    private double[,] BuildEstimatedDurationMatrix(double[,] distanceMatrix, ContainerTransportMode transportMode)
    {
        var size = distanceMatrix.GetLength(0);
        var durationMatrix = new double[size, size];

        double speedKmh = transportMode switch
        {
            ContainerTransportMode.ByCar => 50.0,
            ContainerTransportMode.ByBicycle => 15.0,
            ContainerTransportMode.ByFoot => 5.0,
            ContainerTransportMode.Mix => 30.0,
            _ => 50.0
        };

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                var distanceKm = distanceMatrix[i, j];
                var hours = distanceKm / speedKmh;
                durationMatrix[i, j] = hours * 3600;
            }
        }

        return durationMatrix;
    }

    private string GetOsrmProfile(ContainerTransportMode transportMode)
    {
        return transportMode switch
        {
            ContainerTransportMode.ByCar => "driving",
            ContainerTransportMode.ByBicycle => "cycling",
            ContainerTransportMode.ByFoot => "foot",
            ContainerTransportMode.Mix => "driving",
            _ => "driving"
        };
    }

    private async Task<(double[,] distanceMatrix, double[,] durationMatrix)> GetOsrmDistanceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode)
    {
        var size = locations.Count;
        var distanceMatrix = new double[size, size];
        var durationMatrix = new double[size, size];

        var coordinates = string.Join(";", locations.Select(l => $"{l.Longitude:F6},{l.Latitude:F6}"));
        var url = $"{OSRM_BASE_URL}/table/v1/driving/{coordinates}?annotations=distance,duration";

        _logger.LogInformation("Requesting OSRM table API for distances (driving profile): {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.GetProperty("code").GetString() != "Ok")
        {
            throw new Exception($"OSRM API returned error: {root.GetProperty("code").GetString()}");
        }

        var distances = root.GetProperty("distances");
        var durations = root.GetProperty("durations");

        double speedKmh = GetSpeedForTransportMode(transportMode);
        _logger.LogInformation("Using speed {SpeedKmh} km/h for TransportMode {TransportMode}", speedKmh, transportMode);

        for (int i = 0; i < size; i++)
        {
            var distanceRow = distances[i];
            var durationRow = durations[i];
            for (int j = 0; j < size; j++)
            {
                var distanceMeters = distanceRow[j].GetDouble();
                var distanceKm = distanceMeters / 1000.0;
                distanceMatrix[i, j] = distanceKm;

                if (transportMode == ContainerTransportMode.ByCar)
                {
                    var durationSeconds = durationRow[j].GetDouble();
                    durationMatrix[i, j] = durationSeconds;
                }
                else
                {
                    var hours = distanceKm / speedKmh;
                    durationMatrix[i, j] = hours * 3600;
                }
            }
        }

        if (transportMode == ContainerTransportMode.ByCar)
        {
            _logger.LogInformation("Using OSRM driving times directly");
        }
        else
        {
            _logger.LogInformation(
                "Calculated travel times for {TransportMode} based on {SpeedKmh} km/h (OSRM public server only supports driving)",
                transportMode, speedKmh);
        }

        return (distanceMatrix, durationMatrix);
    }

    private double GetSpeedForTransportMode(ContainerTransportMode transportMode)
    {
        return transportMode switch
        {
            ContainerTransportMode.ByCar => 40.0,
            ContainerTransportMode.ByBicycle => 15.0,
            ContainerTransportMode.ByFoot => 5.0,
            ContainerTransportMode.Mix => 30.0,
            _ => 40.0
        };
    }

    private async Task<List<RouteSegmentDirections>> GetRouteDirectionsAsync(
        List<Location> fullRoute,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode)
    {
        var segmentDirections = new List<RouteSegmentDirections>();

        for (int i = 0; i < fullRoute.Count - 1; i++)
        {
            var fromLocation = fullRoute[i];
            var toLocation = fullRoute[i + 1];

            var transportMode = containerTransportMode == ContainerTransportMode.Mix
                ? toLocation.TransportMode
                : (TransportMode)(int)containerTransportMode;

            var profile = GetOsrmProfileFromTransportMode(transportMode);
            var transportModeText = GetTransportModeText(transportMode);

            try
            {
                var directions = await GetOsrmDirectionsAsync(fromLocation, toLocation, profile);
                directions = directions with
                {
                    FromName = fromLocation.Name,
                    ToName = toLocation.Name,
                    TransportMode = transportModeText
                };
                segmentDirections.Add(directions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get directions for segment {From} -> {To}", fromLocation.Name, toLocation.Name);
                segmentDirections.Add(new RouteSegmentDirections(
                    fromLocation.Name,
                    toLocation.Name,
                    transportModeText,
                    0,
                    TimeSpan.Zero,
                    new List<DirectionStep>()));
            }
        }

        return segmentDirections;
    }

    private async Task<RouteSegmentDirections> GetOsrmDirectionsAsync(Location from, Location to, string profile)
    {
        var coordinates = $"{from.Longitude:F6},{from.Latitude:F6};{to.Longitude:F6},{to.Latitude:F6}";
        var useProfile = "driving";
        var url = $"{OSRM_BASE_URL}/route/v1/{useProfile}/{coordinates}?steps=true&overview=false";

        _logger.LogInformation("Requesting OSRM directions: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.GetProperty("code").GetString() != "Ok")
        {
            throw new Exception($"OSRM API returned error: {root.GetProperty("code").GetString()}");
        }

        var route = root.GetProperty("routes")[0];
        var leg = route.GetProperty("legs")[0];
        var distanceMeters = leg.GetProperty("distance").GetDouble();
        var durationSeconds = leg.GetProperty("duration").GetDouble();

        var steps = new List<DirectionStep>();
        foreach (var step in leg.GetProperty("steps").EnumerateArray())
        {
            var maneuver = step.GetProperty("maneuver");
            var maneuverType = maneuver.GetProperty("type").GetString() ?? "";
            var modifier = maneuver.TryGetProperty("modifier", out var mod) ? mod.GetString() ?? "" : "";

            var streetName = step.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
            var stepDistance = step.GetProperty("distance").GetDouble();
            var stepDuration = (int)step.GetProperty("duration").GetDouble();

            var instruction = GenerateInstruction(maneuverType, modifier, streetName);

            if (!string.IsNullOrEmpty(instruction) && maneuverType != "arrive")
            {
                steps.Add(new DirectionStep(
                    instruction,
                    streetName,
                    stepDistance,
                    stepDuration,
                    maneuverType));
            }
        }

        return new RouteSegmentDirections(
            "",
            "",
            "",
            distanceMeters / 1000.0,
            TimeSpan.FromSeconds(durationSeconds),
            steps);
    }

    private string GenerateInstruction(string maneuverType, string modifier, string streetName)
    {
        var streetPart = string.IsNullOrEmpty(streetName) ? "" : $" auf {streetName}";

        return maneuverType switch
        {
            "depart" => $"Starten{streetPart}",
            "turn" => modifier switch
            {
                "left" => $"Links abbiegen{streetPart}",
                "right" => $"Rechts abbiegen{streetPart}",
                "slight left" => $"Leicht links abbiegen{streetPart}",
                "slight right" => $"Leicht rechts abbiegen{streetPart}",
                "sharp left" => $"Scharf links abbiegen{streetPart}",
                "sharp right" => $"Scharf rechts abbiegen{streetPart}",
                "uturn" => $"Wenden{streetPart}",
                _ => $"Abbiegen{streetPart}"
            },
            "new name" => $"Weiter{streetPart}",
            "continue" => $"Geradeaus{streetPart}",
            "merge" => $"Einfädeln{streetPart}",
            "on ramp" => $"Auffahrt nehmen{streetPart}",
            "off ramp" => $"Abfahrt nehmen{streetPart}",
            "fork" => modifier switch
            {
                "left" => $"Links halten{streetPart}",
                "right" => $"Rechts halten{streetPart}",
                _ => $"Gabelung{streetPart}"
            },
            "end of road" => modifier switch
            {
                "left" => $"Am Ende links{streetPart}",
                "right" => $"Am Ende rechts{streetPart}",
                _ => $"Straßenende{streetPart}"
            },
            "roundabout" => $"Im Kreisverkehr{streetPart}",
            "rotary" => $"Im Kreisverkehr{streetPart}",
            "roundabout turn" => $"Kreisverkehr verlassen{streetPart}",
            "exit roundabout" => $"Kreisverkehr verlassen{streetPart}",
            "notification" => "",
            "arrive" => "Ziel erreicht",
            _ => ""
        };
    }

    private string GetTransportModeText(TransportMode transportMode)
    {
        return transportMode switch
        {
            TransportMode.ByCar => "mit dem Auto",
            TransportMode.ByBicycle => "mit dem Fahrrad",
            TransportMode.ByFoot => "zu Fuss",
            _ => "mit dem Auto"
        };
    }

    private async Task<(double[,] distanceMatrix, double[,] durationMatrix)> GetOpenRouteServiceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode,
        string apiKey)
    {
        var size = locations.Count;
        var distanceMatrix = new double[size, size];
        var durationMatrix = new double[size, size];

        var profile = GetOpenRouteServiceProfile(transportMode);
        var url = $"{OPENROUTESERVICE_BASE_URL}/matrix/{profile}";

        _logger.LogInformation("Requesting OpenRouteService matrix API with profile '{Profile}': {Url}", profile, url);

        var locationsArray = locations.Select(l => new[] { l.Longitude, l.Latitude }).ToArray();
        var requestBody = new
        {
            locations = locationsArray,
            metrics = new[] { "distance", "duration" },
            units = "m"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = httpContent;
        request.Headers.Add("Authorization", apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenRouteService API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new Exception($"OpenRouteService API returned error: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var distances = root.GetProperty("distances");
        var durations = root.GetProperty("durations");

        for (int i = 0; i < size; i++)
        {
            var distanceRow = distances[i];
            var durationRow = durations[i];
            for (int j = 0; j < size; j++)
            {
                var distanceMeters = distanceRow[j].GetDouble();
                distanceMatrix[i, j] = distanceMeters / 1000.0;

                var durationSeconds = durationRow[j].GetDouble();
                durationMatrix[i, j] = durationSeconds;
            }
        }

        _logger.LogInformation(
            "OpenRouteService returned real travel times for profile '{Profile}'",
            profile);

        return (distanceMatrix, durationMatrix);
    }

    private string GetOpenRouteServiceProfile(ContainerTransportMode transportMode)
    {
        return transportMode switch
        {
            ContainerTransportMode.ByCar => "driving-car",
            ContainerTransportMode.ByBicycle => "cycling-regular",
            ContainerTransportMode.ByFoot => "foot-walking",
            ContainerTransportMode.Mix => "driving-car",
            _ => "driving-car"
        };
    }

    private double[,] BuildHaversineDistanceMatrix(List<Location> locations)
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
                    matrix[i, j] = CalculateHaversineDistance(
                        locations[i].Latitude, locations[i].Longitude,
                        locations[j].Latitude, locations[j].Longitude);
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

        var (distanceMatrix, durationMatrix, durationMatricesByProfile) = await BuildDistanceMatrixAsync(newLocations, transportMode);
        return new DistanceMatrix(newLocations, distanceMatrix, durationMatrix, durationMatricesByProfile);
    }

    private TimeSpan GetTravelTimeFromDuration(double[,] durationMatrix, int fromIndex, int toIndex)
    {
        var durationSeconds = durationMatrix[fromIndex, toIndex];
        return TimeSpan.FromSeconds(durationSeconds);
    }

    private TimeSpan CalculateTotalTravelTime(List<int> route, double[,] durationMatrix)
    {
        double totalSeconds = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            totalSeconds += durationMatrix[route[i], route[i + 1]];
        }
        return TimeSpan.FromSeconds(totalSeconds);
    }

    private TimeSpan CalculateMixedTravelTime(
        List<int> route,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode)
    {
        if (containerTransportMode != ContainerTransportMode.Mix || distanceMatrix.DurationMatricesByProfile == null)
        {
            return CalculateTotalTravelTime(route, distanceMatrix.DurationMatrix);
        }

        double totalSeconds = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            var fromIndex = route[i];
            var toIndex = route[i + 1];
            var toLocation = distanceMatrix.Locations[toIndex];
            var profile = GetOsrmProfileFromTransportMode(toLocation.TransportMode);

            if (distanceMatrix.DurationMatricesByProfile.TryGetValue(profile, out var durationMatrix))
            {
                var segmentDuration = durationMatrix[fromIndex, toIndex];
                totalSeconds += segmentDuration;
                _logger.LogDebug("Segment {From} -> {To}: using profile '{Profile}', duration {Duration}s",
                    distanceMatrix.Locations[fromIndex].Name,
                    toLocation.Name,
                    profile,
                    segmentDuration);
            }
            else
            {
                totalSeconds += distanceMatrix.DurationMatrix[fromIndex, toIndex];
            }
        }

        return TimeSpan.FromSeconds(totalSeconds);
    }

    private TimeSpan GetMixedTravelTimeForSegment(
        int fromIndex,
        int toIndex,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode)
    {
        if (containerTransportMode != ContainerTransportMode.Mix || distanceMatrix.DurationMatricesByProfile == null)
        {
            return GetTravelTimeFromDuration(distanceMatrix.DurationMatrix, fromIndex, toIndex);
        }

        var toLocation = distanceMatrix.Locations[toIndex];
        var profile = GetOsrmProfileFromTransportMode(toLocation.TransportMode);

        if (distanceMatrix.DurationMatricesByProfile.TryGetValue(profile, out var durationMatrix))
        {
            return TimeSpan.FromSeconds(durationMatrix[fromIndex, toIndex]);
        }

        return GetTravelTimeFromDuration(distanceMatrix.DurationMatrix, fromIndex, toIndex);
    }

    private string GetOsrmProfileFromTransportMode(TransportMode transportMode)
    {
        return transportMode switch
        {
            TransportMode.ByCar => "driving",
            TransportMode.ByBicycle => "cycling",
            TransportMode.ByFoot => "foot",
            _ => "driving"
        };
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
    TimeSpan TotalBriefingDebriefingTime = default);

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
