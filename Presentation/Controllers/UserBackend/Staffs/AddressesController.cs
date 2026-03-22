// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for address CRUD and geocoding validation.
/// </summary>
/// <param name="Mediator">Mediator for query/command handling</param>
/// <param name="_geocodingService">Service for address geocoding and validation</param>
/// <param name="_coordinateWriter">Service zum Speichern von Koordinaten in der Address-Tabelle</param>

using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

public class AddressesController : InputBaseController<AddressResource>
{
    private readonly IGeocodingService _geocodingService;
    private readonly IAddressCoordinateWriter _coordinateWriter;
    private readonly StateAbbreviationResolver _stateResolver;
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(
        IMediator Mediator,
        ILogger<AddressesController> logger,
        IGeocodingService geocodingService,
        IAddressCoordinateWriter coordinateWriter,
        StateAbbreviationResolver stateResolver)
        : base(Mediator, logger)
    {
        _geocodingService = geocodingService;
        _coordinateWriter = coordinateWriter;
        _stateResolver = stateResolver;
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
    public async Task<ActionResult<AddressValidationResponse>> Validate([FromBody] AddressValidationRequest request)
    {
        var resource = new AddressResource
        {
            Street = request.Street ?? string.Empty,
            Zip = request.Zip ?? string.Empty,
            City = request.City ?? string.Empty,
            State = request.State ?? string.Empty,
            Country = request.Country ?? string.Empty
        };
        var response = await ValidateAddressAsync(resource);
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
                var stateAbbreviation = await _stateResolver.ResolveAsync(validationResult.State);

                if (!string.IsNullOrWhiteSpace(stateAbbreviation)
                    && !string.IsNullOrWhiteSpace(resource.State)
                    && !string.Equals(resource.State, stateAbbreviation, StringComparison.OrdinalIgnoreCase))
                {
                    return new AddressValidationResponse
                    {
                        IsValid = false,
                        MatchType = "state_mismatch",
                        ExpectedState = stateAbbreviation,
                        Latitude = validationResult.Latitude,
                        Longitude = validationResult.Longitude,
                        ReturnedAddress = validationResult.ReturnedAddress
                    };
                }

                if (string.IsNullOrWhiteSpace(resource.State) && !string.IsNullOrWhiteSpace(stateAbbreviation))
                {
                    resource.State = stateAbbreviation;
                }

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
