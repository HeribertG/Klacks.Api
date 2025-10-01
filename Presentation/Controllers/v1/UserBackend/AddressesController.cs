using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AddressesController : InputBaseController<AddressResource>
{
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(IMediator Mediator, ILogger<AddressesController> logger)
        : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet("ClientAddressList/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetList(Guid id)
    {
        _logger.LogInformation($"Fetching client address list for ID: {id}");
        var addresses = await Mediator.Send(new ClientAddressListQuery(id));
        _logger.LogInformation($"Retrieved {addresses.Count()} addresses for client ID: {id}");
        return Ok(addresses);
    }

    [HttpGet("GetSimpleAddress/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetSimpleAddress(Guid id)
    {
        _logger.LogInformation($"Fetching simple address list for ID: {id}");
        var addresses = await Mediator.Send(new GetSimpleAddressListQuery(id));
        _logger.LogInformation($"Retrieved {addresses.Count()} simple addresses for ID: {id}");
        return Ok(addresses);
    }
}
