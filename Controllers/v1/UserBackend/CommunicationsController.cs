using Klacks.Api.Models.Settings;
using Klacks.Api.Queries;
using Klacks.Api.Queries.Communications;
using Klacks.Api.Resources.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend;

public class CommunicationsController : InputBaseController<CommunicationResource>
{
    private readonly ILogger<CommunicationsController> logger;

    public CommunicationsController(IMediator mediator, ILogger<CommunicationsController> logger)
      : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommunicationResource>>> GetCommunication()
    {
        try
        {
            logger.LogInformation("Fetching communication resources.");
            var communications = await mediator.Send(new ListQuery<CommunicationResource>());
            logger.LogInformation($"Retrieved {communications.Count()} communication resources.");
            return Ok(communications);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching communication resources.");
            throw;
        }
    }

    [HttpGet("CommunicationTypes")]
    public async Task<ActionResult<IEnumerable<CommunicationType>>> GetCommunicationType()
    {
        try
        {
            logger.LogInformation("Fetching communication types.");
            var communicationTypes = await mediator.Send(new GetTypeQuery());
            logger.LogInformation($"Retrieved {communicationTypes.Count()} communication types.");
            return Ok(communicationTypes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching communication types.");
            throw;
        }
    }
}
