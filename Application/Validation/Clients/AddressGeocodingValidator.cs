// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// FluentValidation validator for address geocoding in client commands.
/// Validates addresses with zip+city via the geocoding service and sets coordinates on success.
/// Addresses that are unchanged compared to their stored version are skipped so that editing
/// other client data (e.g. qualifications) never re-validates an already-persisted address.
/// </summary>
/// <param name="geocodingService">Service for address geocoding</param>
/// <param name="stateResolver">Resolves state abbreviations from geocoding results</param>
/// <param name="countryResolver">Resolves the country name used for geocoding</param>
/// <param name="addressRepository">Loads the stored address to detect whether it changed</param>

using FluentValidation;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Validation.Clients;

public class AddressGeocodingValidator : AbstractValidator<ICollection<AddressResource>>
{
    private readonly IGeocodingService _geocodingService;
    private readonly StateAbbreviationResolver _stateResolver;
    private readonly ICountryResolver _countryResolver;
    private readonly IAddressRepository _addressRepository;

    public AddressGeocodingValidator(
        IGeocodingService geocodingService,
        StateAbbreviationResolver stateResolver,
        ICountryResolver countryResolver,
        IAddressRepository addressRepository)
    {
        _geocodingService = geocodingService;
        _stateResolver = stateResolver;
        _countryResolver = countryResolver;
        _addressRepository = addressRepository;

        RuleForEach(addresses => addresses)
            .MustAsync(ValidateAndGeocodeAsync)
            .When(addresses => addresses != null && addresses.Any())
            .WithMessage((_, address) =>
                $"address.validation.failed|{address.Street}, {address.Zip} {address.City}");
    }

    private async Task<bool> ValidateAndGeocodeAsync(AddressResource address, CancellationToken cancellationToken)
    {
        if (await IsUnchangedFromStoredAsync(address))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(address.City) || string.IsNullOrWhiteSpace(address.Zip))
        {
            return true;
        }

        // Resolve country: prefer the address's stored country code; fall back to the
        // configured default. Pass the German name to Nominatim's country= parameter —
        // Nominatim treats country= as free-text, not as an ISO code (ISO goes to countrycodes=).
        var resolvedCountry = await _countryResolver.ResolveAsync(address.Country, cancellationToken)
            ?? await _countryResolver.GetDefaultAsync(cancellationToken);

        var geocodingCountry = resolvedCountry?.Name.De
            ?? resolvedCountry?.Name.En
            ?? resolvedCountry?.Abbreviation
            ?? string.Empty;

        try
        {
            var result = await _geocodingService.ValidateExactAddressAsync(
                address.Street, address.Zip, address.City, geocodingCountry);

            var hasStreet = !string.IsNullOrWhiteSpace(address.Street);

            if (result.Found && (!hasStreet || result.ExactMatch || result.MatchType == "exact"))
            {
                var stateAbbreviation = await _stateResolver.ResolveAsync(result.State);

                if (!string.IsNullOrWhiteSpace(stateAbbreviation)
                    && !string.IsNullOrWhiteSpace(address.State)
                    && !string.Equals(address.State, stateAbbreviation, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(address.State) && !string.IsNullOrWhiteSpace(stateAbbreviation))
                {
                    address.State = stateAbbreviation;
                }

                address.Latitude = result.Latitude;
                address.Longitude = result.Longitude;
                return true;
            }

            return false;
        }
        catch
        {
            return true;
        }
    }

    private async Task<bool> IsUnchangedFromStoredAsync(AddressResource address)
    {
        if (address.Id == Guid.Empty)
        {
            return false;
        }

        var stored = await _addressRepository.GetNoTracking(address.Id);
        if (stored == null)
        {
            return false;
        }

        return string.Equals(stored.Street, address.Street, StringComparison.Ordinal)
            && string.Equals(stored.Zip, address.Zip, StringComparison.Ordinal)
            && string.Equals(stored.City, address.City, StringComparison.Ordinal)
            && string.Equals(stored.Country, address.Country, StringComparison.Ordinal);
    }
}
