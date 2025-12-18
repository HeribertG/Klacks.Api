using Klacks.Api.Application.Queries.WorkSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class WorkScheduleController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkScheduleController> _logger;

    public WorkScheduleController(IMediator mediator, ILogger<WorkScheduleController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<WorkScheduleResponse>> GetWorkSchedule([FromBody] WorkScheduleFilter filter)
    {
        var result = await _mediator.Send(new GetWorkScheduleQuery(filter));
        return Ok(result);
    }
}
