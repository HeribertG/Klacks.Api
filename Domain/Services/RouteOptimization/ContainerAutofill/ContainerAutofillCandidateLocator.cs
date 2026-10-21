// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the TimeRange shifts of a container autofill into route candidates (one per distinct client address,
/// stored coordinates first, then freshly geocoded ones whose coordinates are written back) and geocodes the bases.
/// </summary>
/// <param name="geocodingService">Geocodes addresses without stored coordinates and the base addresses</param>
/// <param name="coordinateWriter">Persists freshly geocoded coordinates (fire-and-forget)</param>
/// <param name="logger">Logs the extraction counts and base addresses that could not be geocoded</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillCandidateLocator
{
    private readonly IGeocodingService _geocodingService;
    private readonly IAddressCoordinateWriter _coordinateWriter;
    private readonly ILogger<ContainerAutofillCandidateLocator> _logger;

    public ContainerAutofillCandidateLocator(
        IGeocodingService geocodingService,
        IAddressCoordinateWriter coordinateWriter,
        ILogger<ContainerAutofillCandidateLocator> logger)
    {
        _geocodingService = geocodingService;
        _coordinateWriter = coordinateWriter;
        _logger = logger;
    }

    public async Task<List<Location>> ExtractLocationsFromShiftsAsync(
        List<Shift> shifts, ContainerTransportMode containerTransportMode, CancellationToken cancellationToken)
    {
        var (locations, needsGeocoding) = CollectDistinctAddresses(shifts, containerTransportMode);

        _logger.LogInformation("ExtractLocations: {Stored} from DB, {NeedsGeocoding} need geocoding, from {Total} shifts",
            locations.Count, needsGeocoding.Count, shifts.Count);

        if (needsGeocoding.Count > 0)
        {
            await AddGeocodedLocationsAsync(locations, needsGeocoding, containerTransportMode, cancellationToken);
        }

        _logger.LogInformation("Extracted {Count} candidate locations from {Total} shifts", locations.Count, shifts.Count);
        return locations;
    }

    public async Task<Location?> GeocodeBaseAddressAsync(string address, string label, CancellationToken cancellationToken)
    {
        var coords = await _geocodingService.GeocodeAddressAsync(address, ContainerAutofillDefaults.GeocodingCountry, cancellationToken);
        if (!coords.Latitude.HasValue || !coords.Longitude.HasValue)
        {
            _logger.LogWarning("Could not geocode {Label} address: {Address}", label, address.ForLog());
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

    private static (List<Location> locations, List<(Shift shift, Klacks.Api.Domain.Models.Staffs.Address address, string fullAddress)> needsGeocoding)
        CollectDistinctAddresses(List<Shift> shifts, ContainerTransportMode containerTransportMode)
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

        return (locations, needsGeocoding);
    }

    private async Task AddGeocodedLocationsAsync(
        List<Location> locations,
        List<(Shift shift, Klacks.Api.Domain.Models.Staffs.Address address, string fullAddress)> needsGeocoding,
        ContainerTransportMode containerTransportMode,
        CancellationToken cancellationToken)
    {
        var geocodeTasks = needsGeocoding.Select(async req =>
        {
            var coords = await _geocodingService.GeocodeAddressAsync(req.fullAddress, req.address.Country ?? ContainerAutofillDefaults.GeocodingCountry, cancellationToken);
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

    private static Location CreateLocationFromShift(Shift shift, string fullAddress, double latitude, double longitude, ContainerTransportMode containerTransportMode)
    {
        return new Location
        {
            Name = shift.Client?.Name ?? ContainerAutofillDefaults.UnknownLocationName,
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
}
