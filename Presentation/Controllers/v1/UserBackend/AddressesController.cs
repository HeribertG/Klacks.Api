using Klacks.Api.Exceptions;
using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AddressesController : InputBaseController<AddressResource>
{
    private readonly ILogger<AddressesController> logger;

    public AddressesController(IMediator Mediator, ILogger<AddressesController> logger)
        : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet("ClientAddressList/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetList(Guid id)
    {
        try
        {
            logger.LogInformation($"Fetching client address list for ID: {id}");
            var addresses = await Mediator.Send(new ClientAddressListQuery(id));
            logger.LogInformation($"Retrieved {addresses.Count()} addresses for client ID: {id}");
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching client address list for ID: {id}");
            throw new InvalidRequestException($"Error occurred while fetching client address list for ID: {id}. " + ex.Message);
        }
    }

    [HttpGet("GetSimpleAddress/{id}")]
    public async Task<ActionResult<IEnumerable<AddressResource>>> GetSimpleAddress(Guid id)
    {
        try
        {
            logger.LogInformation($"Fetching simple address list for ID: {id}");
            var addresses = await Mediator.Send(new GetSimpleAddressListQuery(id));
            logger.LogInformation($"Retrieved {addresses.Count()} simple addresses for ID: {id}");
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching simple address list for ID: {id}");
            throw new InvalidRequestException($"Error occurred while fetching simple address list for ID: {id}. " + ex.Message);
        }
    }
}
