using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class WorkChangesController : InputBaseController<WorkChangeResource>
{
    public WorkChangesController(IMediator mediator, ILogger<WorkChangesController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkChangeResource>>> GetList()
    {
        var result = await Mediator.Send(new ListQuery<WorkChangeResource>());
        return Ok(result);
    }
}
