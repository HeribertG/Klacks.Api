// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// FluentValidation-Validator fuer Adress-Geocoding bei Client-Commands.
/// Validiert alle Adressen mit Zip+City via Nominatim und setzt Koordinaten bei Erfolg.
/// </summary>
/// <param name="geocodingService">Service fuer Adress-Geocodierung</param>

using FluentValidation;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces.RouteOptimization;

namespace Klacks.Api.Application.Validation.Clients;

public class AddressGeocodingValidator : AbstractValidator<ICollection<AddressResource>>
{
    private readonly IGeocodingService _geocodingService;

    public AddressGeocodingValidator(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;

        RuleForEach(addresses => addresses)
            .MustAsync(ValidateAndGeocodeAsync)
            .When(addresses => addresses != null && addresses.Any())
            .WithMessage("address.validation.failed");
    }

    private async Task<bool> ValidateAndGeocodeAsync(AddressResource address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address.City) || string.IsNullOrWhiteSpace(address.Zip))
            return true;

        var country = string.IsNullOrWhiteSpace(address.Country) ? "CH" : address.Country;

        try
        {
            var result = await _geocodingService.ValidateExactAddressAsync(
                address.Street, address.Zip, address.City, country);

            if (result.Found)
            {
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
}
