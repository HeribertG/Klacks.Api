// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller fuer Adress-CRUD und Geocoding-Validierung.
/// </summary>
/// <param name="Mediator">Mediator fuer Query/Command-Handling</param>
/// <param name="_geocodingService">Service fuer Adress-Geocodierung und Validierung</param>
/// <param name="_coordinateWriter">Service zum Speichern von Koordinaten in der Address-Tabelle</param>

using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

public class AddressesController : InputBaseController<AddressResource>
{
    private readonly IGeocodingService _geocodingService;
    private readonly IAddressCoordinateWriter _coordinateWriter;
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(
        IMediator Mediator,
        ILogger<AddressesController> logger,
        IGeocodingService geocodingService,
        IAddressCoordinateWriter coordinateWriter)
        : base(Mediator, logger)
    {
        _geocodingService = geocodingService;
        _coordinateWriter = coordinateWriter;
        _logger = logger;
    }

    [HttpGet("ClientAddressList/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetList(Guid id)
    {
        var addresses = await Mediator.Send(new ClientAddressListQuery(id));
        return Ok(addresses);
    }

    [HttpGet("GetSimpleAddress/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetSimpleAddress(Guid id)
    {
        var addresses = await Mediator.Send(new GetSimpleAddressListQuery(id));
        return Ok(addresses);
    }

    [HttpPost("Validate")]
    public async Task<ActionResult<AddressValidationResponse>> Validate([FromBody] AddressResource resource)
    {
        var response = await ValidateAddressAsync(resource);
        if (!response.IsValid)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    public override async Task<ActionResult<AddressResource>> Post([FromBody] AddressResource resource)
    {
        var validation = await ValidateAddressAsync(resource);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = new { Address = new[] { "Address could not be geocoded. Please check street, ZIP and city." } }, validation });
        }

        resource.Latitude = validation.Latitude;
        resource.Longitude = validation.Longitude;

        return await base.Post(resource);
    }

    public override async Task<ActionResult<AddressResource>> Put([FromBody] AddressResource resource)
    {
        var validation = await ValidateAddressAsync(resource);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = new { Address = new[] { "Address could not be geocoded. Please check street, ZIP and city." } }, validation });
        }

        resource.Latitude = validation.Latitude;
        resource.Longitude = validation.Longitude;

        return await base.Put(resource);
    }

    private async Task<AddressValidationResponse> ValidateAddressAsync(AddressResource resource)
    {
        if (string.IsNullOrWhiteSpace(resource.City) || string.IsNullOrWhiteSpace(resource.Zip))
        {
            return new AddressValidationResponse
            {
                IsValid = false,
                MatchType = "missing_fields"
            };
        }

        var country = string.IsNullOrWhiteSpace(resource.Country) ? "CH" : resource.Country;

        try
        {
            var validationResult = await _geocodingService.ValidateExactAddressAsync(
                resource.Street, resource.Zip, resource.City, country);

            if (validationResult.Found)
            {
                return new AddressValidationResponse
                {
                    IsValid = true,
                    MatchType = validationResult.MatchType ?? "exact",
                    Latitude = validationResult.Latitude,
                    Longitude = validationResult.Longitude,
                    ReturnedAddress = validationResult.ReturnedAddress
                };
            }

            var suggestions = await _geocodingService.GetAddressSuggestionsAsync(
                resource.Street, resource.Zip, resource.City, country);

            return new AddressValidationResponse
            {
                IsValid = false,
                MatchType = validationResult.MatchType ?? "not_found",
                Suggestions = suggestions.Select(s => new AddressSuggestionDto
                {
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    DisplayName = s.DisplayName
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Address validation failed for {Street}, {Zip} {City}", resource.Street, resource.Zip, resource.City);

            return new AddressValidationResponse
            {
                IsValid = true,
                MatchType = "validation_error"
            };
        }
    }
}
