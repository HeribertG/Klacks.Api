using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CalendarSelectionsController : InputBaseController<CalendarSelectionResource>
{
    private readonly ILogger<CalendarSelectionsController> _logger;

    public CalendarSelectionsController(IMediator Mediator, ILogger<CalendarSelectionsController> logger)
        : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarSelectionResource>>> GetCalendarSelections()
    {
        _logger.LogInformation("Fetching calendar selections.");
        var calendarSelections = await Mediator.Send(new ListQuery<CalendarSelectionResource>());
        _logger.LogInformation($"Retrieved {calendarSelections.Count()} calendar selections.");
        return Ok(calendarSelections);
    }
}
