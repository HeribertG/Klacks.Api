using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Handlers.ScheduleChanges;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class ScheduleChangesController : BaseController
{
    private readonly IMediator _mediator;

    public ScheduleChangesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ScheduleChangeResource>>> GetChanges(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var result = await _mediator.Send(new GetListQuery
        {
            StartDate = startDate,
            EndDate = endDate
        });
        return Ok(result);
    }
}
