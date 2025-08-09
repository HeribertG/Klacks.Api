using Klacks.Api.Queries;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CalendarSelectionsController : InputBaseController<CalendarSelectionResource>
{
    private readonly ILogger<CalendarSelectionsController> logger;

    public CalendarSelectionsController(IMediator Mediator, ILogger<CalendarSelectionsController> logger)
        : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarSelectionResource>>> GetCalendarSelections()
    {
        try
        {
            logger.LogInformation("Fetching calendar selections.");
            var calendarSelections = await Mediator.Send(new ListQuery<CalendarSelectionResource>());
            logger.LogInformation($"Retrieved {calendarSelections.Count()} calendar selections.");
            return Ok(calendarSelections);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching calendar selections.");
            throw;
        }
    }
}
