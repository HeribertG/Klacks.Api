using Klacks_api.Models.Settings;
using Klacks_api.Queries;
using Klacks_api.Queries.Communications;
using Klacks_api.Resources.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks_api.Controllers.V1.Backend;

public class CommunicationsController : InputBaseController<CommunicationResource>
{
  private readonly ILogger<CommunicationsController> _logger;

  public CommunicationsController(IMediator mediator, ILogger<CommunicationsController> logger)
    : base(mediator, logger)
  {
    _logger = logger;
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<CommunicationResource>>> GetCommunication()
  {
    try
    {
      _logger.LogInformation("Fetching communication resources.");
      var communications = await mediator.Send(new ListQuery<CommunicationResource>());
      _logger.LogInformation($"Retrieved {communications.Count()} communication resources.");
      return Ok(communications);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while fetching communication resources.");
      throw;
    }
  }

  [HttpGet("CommunicationTypes")]
  public async Task<ActionResult<IEnumerable<CommunicationType>>> GetCommunicationType()
  {
    try
    {
      _logger.LogInformation("Fetching communication types.");
      var communicationTypes = await mediator.Send(new GetTypeQuery());
      _logger.LogInformation($"Retrieved {communicationTypes.Count()} communication types.");
      return Ok(communicationTypes);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while fetching communication types.");
      throw;
    }
  }
}
