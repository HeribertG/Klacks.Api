using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ContractsController : InputBaseController<ContractResource>
{
    private readonly ILogger<ContractsController> logger;

    public ContractsController(IMediator Mediator, ILogger<ContractsController> logger)
        : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractResource>>> GetContracts()
    {
        try
        {
            logger.LogInformation("Fetching all contracts.");
            var contracts = await Mediator.Send(new ListQuery<ContractResource>());
            logger.LogInformation($"Retrieved {contracts.Count()} contracts.");
            return Ok(contracts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching contracts.");
            throw;
        }
    }
}