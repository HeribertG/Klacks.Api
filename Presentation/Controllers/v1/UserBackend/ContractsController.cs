using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ContractsController : InputBaseController<ContractResource>
{
    public ContractsController(IMediator Mediator, ILogger<ContractsController> logger)
        : base(Mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractResource>>> GetContracts()
    {
        var contracts = await Mediator.Send(new ListQuery<ContractResource>());
        return Ok(contracts);
    }
}