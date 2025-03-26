using Klacks.Api.Queries;
using Klacks.Api.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.Backend;

public class CalendarSelectionsController : InputBaseController<CalendarSelectionResource>
{
    private readonly ILogger<CalendarSelectionsController> _logger;

    public CalendarSelectionsController(IMediator mediator, ILogger<CalendarSelectionsController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarSelectionResource>>> GetCalendarSelections()
    {
        try
        {
            _logger.LogInformation("Fetching calendar selections.");
            var calendarSelections = await mediator.Send(new ListQuery<CalendarSelectionResource>());
            _logger.LogInformation($"Retrieved {calendarSelections.Count()} calendar selections.");
            return Ok(calendarSelections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching calendar selections.");
            throw;
        }
    }
}
