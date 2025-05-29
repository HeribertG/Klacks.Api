using Klacks.Api.Queries.Addresses;
using Klacks.Api.Resources.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.Backend;

public class AddressesController : InputBaseController<AddressResource>
{
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(IMediator mediator, ILogger<AddressesController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet("ClientAddressList/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetList(Guid id)
    {
        try
        {
            _logger.LogInformation($"Fetching client address list for ID: {id}");
            var addresses = await mediator.Send(new ClientAddressListQuery(id));
            _logger.LogInformation($"Retrieved {addresses.Count()} addresses for client ID: {id}");
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while fetching client address list for ID: {id}");
            throw;
        }
    }

    [HttpGet("GetSimpleAddress/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetSimpleAddress(Guid id)
    {
        try
        {
            _logger.LogInformation($"Fetching simple address list for ID: {id}");
            var addresses = await mediator.Send(new GetSimpleAddressListQuery(id));
            _logger.LogInformation($"Retrieved {addresses.Count()} simple addresses for ID: {id}");
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while fetching simple address list for ID: {id}");
            throw;
        }
    }
}
