// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Geocodes one stored client address with the acceptance rule the interactive address validation uses
/// (AddressGeocodingRules). A hit fills latitude and longitude and, when the state is empty, the state;
/// no hit or an unavailable service leaves the address as it is. Addresses that already have coordinates or lack postcode or city are skipped.
/// </summary>
/// <param name="addressRepository">Loads and tracks the address</param>
/// <param name="geocodingService">Nominatim-backed geocoder (rate limited)</param>
/// <param name="countryResolver">Resolves the address country to its name</param>
/// <param name="stateResolver">Maps the geocoder's state name to the state abbreviation</param>
/// <param name="unitOfWork">Saves the coordinates</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Geocoding;

public class AddressGeocodingProcessor : IAddressGeocodingProcessor
{
    private readonly IAddressRepository _addressRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly ICountryResolver _countryResolver;
    private readonly StateAbbreviationResolver _stateResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddressGeocodingProcessor> _logger;

    public AddressGeocodingProcessor(
        IAddressRepository addressRepository,
        IGeocodingService geocodingService,
        ICountryResolver countryResolver,
        StateAbbreviationResolver stateResolver,
        IUnitOfWork unitOfWork,
        ILogger<AddressGeocodingProcessor> logger)
    {
        _addressRepository = addressRepository;
        _geocodingService = geocodingService;
        _countryResolver = countryResolver;
        _stateResolver = stateResolver;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid addressId, CancellationToken cancellationToken)
    {
        var address = await _addressRepository.Get(addressId);
        if (address == null || address.Latitude.HasValue || string.IsNullOrWhiteSpace(address.Zip) || string.IsNullOrWhiteSpace(address.City))
        {
            return;
        }

        var country = await _countryResolver.ResolveAsync(address.Country, cancellationToken)
            ?? await _countryResolver.GetDefaultAsync(cancellationToken);

        var result = await _geocodingService.ValidateExactAddressAsync(
            address.Street, address.Zip, address.City, AddressGeocodingRules.CountryQueryName(country));
        if (result.ServiceUnavailable)
        {
            _logger.LogWarning("Geocoding service unavailable for address {AddressId}; left unchanged", addressId);
            return;
        }

        if (!AddressGeocodingRules.IsAcceptedHit(result, address.Street))
        {
            _logger.LogInformation("No exact geocoding hit for address {AddressId}; left unchanged", addressId);
            return;
        }

        address.Latitude = result.Latitude;
        address.Longitude = result.Longitude;

        if (string.IsNullOrWhiteSpace(address.State))
        {
            address.State = await _stateResolver.ResolveAsync(result.State) ?? string.Empty;
        }

        await _unitOfWork.CompleteAsync();
    }
}
