using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ShiftChangesController : InputBaseController<ShiftChangeResource>
{
    public ShiftChangesController(IMediator mediator, ILogger<ShiftChangesController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShiftChangeResource>>> GetList()
    {
        var result = await Mediator.Send(new ListQuery<ShiftChangeResource>());
        return Ok(result);
    }
}
