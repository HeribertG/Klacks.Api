using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class CalendarSelectionsController : InputBaseController<CalendarSelectionResource>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;

    public CalendarSelectionsController(
        IMediator Mediator,
        ILogger<CalendarSelectionsController> logger,
        ICalendarSelectionRepository calendarSelectionRepository)
        : base(Mediator, logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarSelectionResource>>> GetCalendarSelections()
    {
        var calendarSelections = await Mediator.Send(new ListQuery<CalendarSelectionResource>());
        return Ok(calendarSelections);
    }

    [HttpGet("used-by-contracts")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetUsedByContracts()
    {
        var usedIds = await _calendarSelectionRepository.GetUsedByContractsAsync();
        return Ok(usedIds);
    }
}
