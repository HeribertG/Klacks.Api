using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ContractsController : InputBaseController<ContractResource>
{
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(IMediator Mediator, ILogger<ContractsController> logger)
        : base(Mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractResource>>> GetContracts()
    {
        _logger.LogInformation("Fetching all contracts.");
        var contracts = await Mediator.Send(new ListQuery<ContractResource>());
        _logger.LogInformation($"Retrieved {contracts.Count()} contracts.");
        return Ok(contracts);
    }
}