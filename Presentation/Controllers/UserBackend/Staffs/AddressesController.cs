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

    public override async Task<ActionResult<AddressResource>> Post([FromBody] AddressResource resource)
    {
        var result = await base.Post(resource);
        _ = GeocodeAddressInBackground(resource);
        return result;
    }

    public override async Task<ActionResult<AddressResource>> Put([FromBody] AddressResource resource)
    {
        var result = await base.Put(resource);
        _ = GeocodeAddressInBackground(resource);
        return result;
    }

    private async Task GeocodeAddressInBackground(AddressResource resource)
    {
        if (string.IsNullOrEmpty(resource.City) || string.IsNullOrEmpty(resource.Zip) || resource.Id == Guid.Empty)
        {
            return;
        }

        try
        {
            var fullAddress = $"{resource.Street}, {resource.Zip} {resource.City}";
            var coords = await _geocodingService.GeocodeAddressAsync(fullAddress, resource.Country ?? "CH");

            if (coords.Latitude.HasValue && coords.Longitude.HasValue)
            {
                await _coordinateWriter.UpdateCoordinatesAsync(resource.Id, coords.Latitude.Value, coords.Longitude.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background geocoding failed for address {AddressId}", resource.Id);
        }
    }
}
