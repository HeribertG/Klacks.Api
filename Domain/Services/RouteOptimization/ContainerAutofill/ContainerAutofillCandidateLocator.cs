// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the TimeRange shifts of a container autofill into route candidates (one per distinct client address,
/// stored coordinates first, then freshly geocoded ones whose coordinates are written back) and geocodes the bases.
/// Addresses are geocoded with the name of their country (the company default country when they have none), the
/// bases with the company default country. Country lookups and coordinate writes share the scoped DbContext, so they
/// run one after another; only the geocoding requests themselves run in parallel. LocateAsync does everything except
/// the coordinate write and says per task why it has no location; ExtractLocationsFromShiftsAsync adds the write.
/// </summary>
/// <param name="geocodingService">Geocodes addresses without stored coordinates and the base addresses</param>
/// <param name="coordinateWriter">Persists freshly geocoded coordinates, awaited one by one on the request token</param>
/// <param name="countryResolver">Resolves the geocoding country name of an address and the company default country</param>
/// <param name="logger">Logs the extraction counts, base addresses that could not be geocoded and failed writes</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillCandidateLocator : IShiftLocationResolver
{
    private readonly IGeocodingService _geocodingService;
    private readonly IAddressCoordinateWriter _coordinateWriter;
    private readonly ICountryResolver _countryResolver;
    private readonly ILogger<ContainerAutofillCandidateLocator> _logger;

    public ContainerAutofillCandidateLocator(
        IGeocodingService geocodingService,
        IAddressCoordinateWriter coordinateWriter,
        ICountryResolver countryResolver,
        ILogger<ContainerAutofillCandidateLocator> logger)
    {
        _geocodingService = geocodingService;
        _coordinateWriter = coordinateWriter;
        _countryResolver = countryResolver;
        _logger = logger;
    }

    public async Task<List<Location>> ExtractLocationsFromShiftsAsync(
        List<Shift> shifts, ContainerTransportMode containerTransportMode, CancellationToken cancellationToken)
    {
        var located = await LocateAsync(shifts, containerTransportMode, null, cancellationToken);
        await WriteCoordinatesAsync(located.CoordinateUpdates, cancellationToken);
        return located.Locations;
    }

    /// <summary>
    /// Finds the route candidates of the shifts without storing anything: stored coordinates first, then geocoded ones.
    /// </summary>
    /// <param name="shifts">TimeRange shifts to place</param>
    /// <param name="containerTransportMode">Transport mode written onto the candidates</param>
    /// <param name="maxGeocodedAddresses">Upper limit of addresses sent to the geocoder; null = all. Addresses beyond it
    /// are reported as NotLookedUp.</param>
    public async Task<ContainerAutofillLocatedCandidates> LocateAsync(
        List<Shift> shifts,
        ContainerTransportMode containerTransportMode,
        int? maxGeocodedAddresses,
        CancellationToken cancellationToken)
    {
        var (locations, needsGeocoding, unlocated) = CollectDistinctAddresses(shifts, containerTransportMode);

        _logger.LogInformation("ExtractLocations: {Stored} from DB, {NeedsGeocoding} need geocoding, from {Total} shifts",
            locations.Count, needsGeocoding.Count, shifts.Count);

        var toGeocode = maxGeocodedAddresses is { } limit ? needsGeocoding.Take(limit).ToList() : needsGeocoding;
        unlocated.AddRange(needsGeocoding.Skip(toGeocode.Count).Select(request =>
            new ContainerAutofillUnlocatedTask(request.shift.Id, ContainerAutofillUnlocatedReason.NotLookedUp)));

        var coordinateUpdates = new List<ContainerAutofillCoordinateUpdate>();
        if (toGeocode.Count > 0)
        {
            await AddGeocodedLocationsAsync(locations, toGeocode, containerTransportMode, coordinateUpdates, unlocated, cancellationToken);
        }

        _logger.LogInformation("Extracted {Count} candidate locations from {Total} shifts", locations.Count, shifts.Count);
        return new ContainerAutofillLocatedCandidates(locations, coordinateUpdates, unlocated);
    }

    public async Task<IReadOnlyList<ShiftLocation>> LocateAsync(IReadOnlyList<Shift> shifts, CancellationToken cancellationToken)
    {
        var byAddress = shifts
            .Select(shift => (Shift: shift, Key: AddressKey(shift)))
            .Where(entry => entry.Key is not null)
            .GroupBy(entry => entry.Key!)
            .ToList();

        var locations = await ExtractLocationsFromShiftsAsync(
            byAddress.Select(group => group.First().Shift).ToList(), ContainerTransportMode.ByCar, cancellationToken);
        var locationByRepresentative = locations.ToDictionary(location => location.ShiftId);

        return byAddress
            .Where(group => locationByRepresentative.ContainsKey(group.First().Shift.Id))
            .SelectMany(group => group.Select(entry => new ShiftLocation(
                entry.Shift.Id,
                locationByRepresentative[group.First().Shift.Id].Latitude,
                locationByRepresentative[group.First().Shift.Id].Longitude)))
            .ToList();
    }

    public async Task<Location?> GeocodeBaseAddressAsync(string address, string label, CancellationToken cancellationToken)
    {
        var country = await _countryResolver.ResolveDefaultGeocodingNameAsync(cancellationToken);
        var coords = await _geocodingService.GeocodeAddressAsync(address, country, cancellationToken);
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

    private static string? AddressKey(Shift shift)
    {
        var address = shift.Client?.Addresses?.FirstOrDefault();
        return address is null || string.IsNullOrEmpty(address.City)
            ? null
            : $"{address.City}_{address.Zip}_{address.Street}";
    }

    private static (List<Location> locations, List<(Shift shift, Address address, string fullAddress)> needsGeocoding, List<ContainerAutofillUnlocatedTask> unlocated)
        CollectDistinctAddresses(List<Shift> shifts, ContainerTransportMode containerTransportMode)
    {
        var locations = new List<Location>();
        var uniqueAddresses = new HashSet<string>();
        var needsGeocoding = new List<(Shift shift, Address address, string fullAddress)>();
        var unlocated = new List<ContainerAutofillUnlocatedTask>();

        foreach (var shift in shifts)
        {
            if (shift.Client?.Addresses == null || !shift.Client.Addresses.Any())
            {
                unlocated.Add(new ContainerAutofillUnlocatedTask(shift.Id, ContainerAutofillUnlocatedReason.NoAddress));
                continue;
            }

            var address = shift.Client.Addresses.First();
            var addressKey = AddressKey(shift);

            if (addressKey is null)
            {
                unlocated.Add(new ContainerAutofillUnlocatedTask(shift.Id, ContainerAutofillUnlocatedReason.NoAddress));
                continue;
            }

            if (uniqueAddresses.Contains(addressKey))
            {
                unlocated.Add(new ContainerAutofillUnlocatedTask(shift.Id, ContainerAutofillUnlocatedReason.SameAddress));
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

        return (locations, needsGeocoding, unlocated);
    }

    private async Task AddGeocodedLocationsAsync(
        List<Location> locations,
        List<(Shift shift, Address address, string fullAddress)> needsGeocoding,
        ContainerTransportMode containerTransportMode,
        List<ContainerAutofillCoordinateUpdate> coordinateUpdates,
        List<ContainerAutofillUnlocatedTask> unlocated,
        CancellationToken cancellationToken)
    {
        var countryNames = await ResolveCountryNamesAsync(needsGeocoding.Select(req => req.address.Country), cancellationToken);

        var geocodeTasks = needsGeocoding.Select(async req =>
        {
            var coords = await _geocodingService.GeocodeAddressAsync(req.fullAddress, countryNames[req.address.Country], cancellationToken);
            return (req.shift, req.address, req.fullAddress, coords);
        });

        var results = await Task.WhenAll(geocodeTasks);

        foreach (var (shift, address, fullAddress, coords) in results)
        {
            if (coords.Latitude.HasValue && coords.Longitude.HasValue)
            {
                locations.Add(CreateLocationFromShift(shift, fullAddress, coords.Latitude.Value, coords.Longitude.Value, containerTransportMode));
                coordinateUpdates.Add(new ContainerAutofillCoordinateUpdate(address.Id, coords.Latitude.Value, coords.Longitude.Value));
            }
            else
            {
                unlocated.Add(new ContainerAutofillUnlocatedTask(shift.Id, ContainerAutofillUnlocatedReason.NotLocated));
            }
        }
    }

    private async Task<Dictionary<string, string>> ResolveCountryNamesAsync(
        IEnumerable<string> countryCodes, CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var code in countryCodes.Distinct(StringComparer.Ordinal))
        {
            names[code] = await _countryResolver.ResolveGeocodingNameAsync(code, cancellationToken);
        }

        return names;
    }

    private async Task WriteCoordinatesAsync(
        List<ContainerAutofillCoordinateUpdate> updates, CancellationToken cancellationToken)
    {
        foreach (var (addressId, latitude, longitude) in updates)
        {
            try
            {
                await _coordinateWriter.UpdateCoordinatesAsync(addressId, latitude, longitude, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not save the geocoded coordinates of address {AddressId}", addressId);
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
