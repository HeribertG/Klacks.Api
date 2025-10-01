using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Communications;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CommunicationsController : InputBaseController<CommunicationResource>
{
    private readonly ILogger<CommunicationsController> _logger;

    public CommunicationsController(IMediator Mediator, ILogger<CommunicationsController> logger)
      : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommunicationResource>>> GetCommunication()
    {
        _logger.LogInformation("Fetching communication resources.");
        var communications = await Mediator.Send(new ListQuery<CommunicationResource>());
        _logger.LogInformation($"Retrieved {communications.Count()} communication resources.");
        return Ok(communications);
    }

    [HttpGet("CommunicationTypes")]
    public async Task<ActionResult<IEnumerable<CommunicationType>>> GetCommunicationType()
    {
        _logger.LogInformation("Fetching communication types.");
        var communicationTypes = await Mediator.Send(new GetTypeQuery());
        _logger.LogInformation($"Retrieved {communicationTypes.Count()} communication types.");
        return Ok(communicationTypes);
    }
}
