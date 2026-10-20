// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Geocodes every stored address that still has no coordinates (e.g. addresses saved while the geo services
/// were switched off) and writes each hit back. A disabled geocoder is reported instead of attempted.
/// </summary>
/// <param name="addressRepository">Lists the addresses without coordinates</param>
/// <param name="geocodingService">Rate-limited geocoder honouring the geo service policy</param>
/// <param name="countryResolver">Resolves the stored country code to the name the geocoder is queried with</param>
/// <param name="coordinateWriter">Writes the coordinates of a hit to the address</param>
/// <param name="logger">Batch progress and misses</param>

using Klacks.Api.Application.Commands.Geocoding;
using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Geocoding;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Geocoding;

public class GeocodeAllAddressesCommandHandler : IRequestHandler<GeocodeAllAddressesCommand, GeocodeAllAddressesResult>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly ICountryResolver _countryResolver;
    private readonly IAddressCoordinateWriter _coordinateWriter;
    private readonly ILogger<GeocodeAllAddressesCommandHandler> _logger;

    public GeocodeAllAddressesCommandHandler(
        IAddressRepository addressRepository,
        IGeocodingService geocodingService,
        ICountryResolver countryResolver,
        IAddressCoordinateWriter coordinateWriter,
        ILogger<GeocodeAllAddressesCommandHandler> logger)
    {
        _addressRepository = addressRepository;
        _geocodingService = geocodingService;
        _countryResolver = countryResolver;
        _coordinateWriter = coordinateWriter;
        _logger = logger;
    }

    public async Task<GeocodeAllAddressesResult> Handle(GeocodeAllAddressesCommand request, CancellationToken cancellationToken)
    {
        if (!_geocodingService.IsEnabled)
        {
            return GeocodeAllAddressesResult.Disabled;
        }

        var addresses = await _addressRepository.GetWithoutCoordinatesAsync(cancellationToken);
        _logger.LogInformation("Batch geocoding: {Count} addresses without coordinates", addresses.Count);

        var countryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var geocoded = 0;
        var failed = 0;

        foreach (var address in addresses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var country = await ResolveCountryNameAsync(address.Country, countryNames, cancellationToken);
            var fullAddress = $"{address.Street}, {address.Zip} {address.City}";
            var (latitude, longitude) = await _geocodingService.GeocodeAddressAsync(fullAddress, country, cancellationToken);

            if (latitude.HasValue && longitude.HasValue)
            {
                await _coordinateWriter.UpdateCoordinatesAsync(address.Id, latitude.Value, longitude.Value, cancellationToken);
                geocoded++;
            }
            else
            {
                failed++;
                _logger.LogWarning("Batch geocoding found no coordinates for address {AddressId}", address.Id);
            }
        }

        _logger.LogInformation("Batch geocoding complete: {Geocoded} geocoded, {Failed} failed", geocoded, failed);

        return new GeocodeAllAddressesResult(false, addresses.Count, geocoded, failed);
    }

    /// <summary>
    /// Same country naming as the interactive address validation (AddressGeocodingRules.CountryQueryName). An
    /// empty code falls back to the default country; a code the resolver does not know is passed on unchanged.
    /// </summary>
    private async Task<string> ResolveCountryNameAsync(
        string countryCode, Dictionary<string, string> resolvedNames, CancellationToken cancellationToken)
    {
        if (resolvedNames.TryGetValue(countryCode, out var cached))
        {
            return cached;
        }

        var country = await _countryResolver.ResolveAsync(countryCode, cancellationToken);
        if (country == null && string.IsNullOrWhiteSpace(countryCode))
        {
            country = await _countryResolver.GetDefaultAsync(cancellationToken);
        }

        var name = AddressGeocodingRules.CountryQueryName(country);
        var resolved = string.IsNullOrEmpty(name) ? countryCode : name;
        resolvedNames[countryCode] = resolved;
        return resolved;
    }
}
