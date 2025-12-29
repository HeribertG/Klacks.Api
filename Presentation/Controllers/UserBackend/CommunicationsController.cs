using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Communications;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class CommunicationsController : InputBaseController<CommunicationResource>
{
    public CommunicationsController(IMediator Mediator, ILogger<CommunicationsController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommunicationResource>>> GetCommunication()
    {
        var communications = await Mediator.Send(new ListQuery<CommunicationResource>());
        return Ok(communications);
    }

    [HttpGet("CommunicationTypes")]
    public async Task<ActionResult<IEnumerable<CommunicationType>>> GetCommunicationType()
    {
        var communicationTypes = await Mediator.Send(new GetTypeQuery());
        return Ok(communicationTypes);
    }
}
