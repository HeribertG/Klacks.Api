// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// FluentValidation validator for address geocoding in client commands.
/// Validates all addresses with zip+city via Nominatim and sets coordinates on success.
/// </summary>
/// <param name="geocodingService">Service for address geocoding</param>

using FluentValidation;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Validation.Clients;

public class AddressGeocodingValidator : AbstractValidator<ICollection<AddressResource>>
{
    private readonly IGeocodingService _geocodingService;
    private readonly StateAbbreviationResolver _stateResolver;
    private readonly ICountryResolver _countryResolver;

    public AddressGeocodingValidator(IGeocodingService geocodingService, StateAbbreviationResolver stateResolver, ICountryResolver countryResolver)
    {
        _geocodingService = geocodingService;
        _stateResolver = stateResolver;
        _countryResolver = countryResolver;

        Console.WriteLine("[AddressGeocodingValidator] CONSTRUCTOR called");

        RuleForEach(addresses => addresses)
            .MustAsync(ValidateAndGeocodeAsync)
            .When(addresses => addresses != null && addresses.Any())
            .WithMessage((_, address) =>
                $"address.validation.failed|{address.Street}, {address.Zip} {address.City}");
    }

    private async Task<bool> ValidateAndGeocodeAsync(AddressResource address, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[AddressGeocodingValidator] ValidateAndGeocodeAsync called: Street={address.Street}, Zip={address.Zip}, City={address.City}");

        if (string.IsNullOrWhiteSpace(address.City) || string.IsNullOrWhiteSpace(address.Zip))
        {
            Console.WriteLine("[AddressGeocodingValidator] Skipping - missing City or Zip");
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

            Console.WriteLine($"[AddressGeocodingValidator] Geocoding result: Found={result.Found}, ExactMatch={result.ExactMatch}, MatchType={result.MatchType}");

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

            if (result.Found && hasStreet && !result.ExactMatch)
            {
                Console.WriteLine($"[AddressGeocodingValidator] RETURNING FALSE - street not matched: {address.Street}");
                return false;
            }

            Console.WriteLine("[AddressGeocodingValidator] RETURNING FALSE - address not found");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AddressGeocodingValidator] EXCEPTION: {ex.Message}");
            return true;
        }
    }
}
