using Klacks.Api.Models.Settings;
using Klacks.Api.Queries;
using Klacks.Api.Queries.Communications;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CommunicationsController : InputBaseController<CommunicationResource>
{
    private readonly ILogger<CommunicationsController> logger;

    public CommunicationsController(IMediator Mediator, ILogger<CommunicationsController> logger)
      : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommunicationResource>>> GetCommunication()
    {
        try
        {
            logger.LogInformation("Fetching communication resources.");
            var communications = await Mediator.Send(new ListQuery<CommunicationResource>());
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
            var communicationTypes = await Mediator.Send(new GetTypeQuery());
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
