using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AddressesController : InputBaseController<AddressResource>
{
    public AddressesController(IMediator Mediator, ILogger<AddressesController> logger)
        : base(Mediator, logger)
    {
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
}
