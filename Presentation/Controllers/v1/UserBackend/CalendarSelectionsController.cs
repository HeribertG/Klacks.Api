using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CalendarSelectionsController : InputBaseController<CalendarSelectionResource>
{
    public CalendarSelectionsController(IMediator Mediator, ILogger<CalendarSelectionsController> logger)
        : base(Mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarSelectionResource>>> GetCalendarSelections()
    {
        var calendarSelections = await Mediator.Send(new ListQuery<CalendarSelectionResource>());
        return Ok(calendarSelections);
    }
}
