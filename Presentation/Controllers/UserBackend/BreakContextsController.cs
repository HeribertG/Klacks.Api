using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class BreakContextsController : InputBaseController<BreakContextResource>
{
    public BreakContextsController(IMediator mediator, ILogger<BreakContextsController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BreakContextResource>>> GetBreakContexts()
    {
        var breakContexts = await Mediator.Send(new ListQuery<BreakContextResource>());
        return Ok(breakContexts);
    }
}
