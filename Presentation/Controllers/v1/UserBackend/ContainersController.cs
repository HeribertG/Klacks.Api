using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ContainersController : InputBaseController<ContainerTemplateResource>
{
    private readonly ILogger<ContainersController> _logger;

    public ContainersController(IMediator Mediator, ILogger<ContainersController> logger)
          : base(Mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet("available-tasks")]
    public async Task<ActionResult<IEnumerable<ShiftResource>>> GetAvailableTasks(
        [FromQuery] Guid containerId,
        [FromQuery] int weekday,
        [FromQuery] string fromTime,
        [FromQuery] string untilTime,
        [FromQuery] string? searchString = null,
        [FromQuery] Guid? excludeContainerId = null,
        [FromQuery] bool? isHoliday = null,
        [FromQuery] bool? isWeekdayOrHoliday = null)
    {
        _logger.LogInformation(
            "GetAvailableTasks called: containerId={ContainerId}, weekday={Weekday}, fromTime={FromTime}, untilTime={UntilTime}, isHoliday={IsHoliday}, isWeekdayOrHoliday={IsWeekdayOrHoliday}",
            containerId, weekday, fromTime, untilTime, isHoliday, isWeekdayOrHoliday);

        if (!TimeOnly.TryParse(fromTime, out var parsedFromTime))
        {
            return BadRequest($"Invalid fromTime format: {fromTime}. Expected format: HH:mm:ss or HH:mm");
        }

        if (!TimeOnly.TryParse(untilTime, out var parsedUntilTime))
        {
            return BadRequest($"Invalid untilTime format: {untilTime}. Expected format: HH:mm:ss or HH:mm");
        }

        var tasks = await Mediator.Send(new GetAvailableTasksQuery(
            containerId,
            weekday,
            parsedFromTime,
            parsedUntilTime,
            searchString,
            excludeContainerId,
            isHoliday,
            isWeekdayOrHoliday));
        return Ok(tasks);
    }

    [HttpGet("{containerId}/templates")]
    public async Task<ActionResult<IEnumerable<ContainerTemplateResource>>> GetTemplates([FromRoute] Guid containerId)
    {
        var templates = await Mediator.Send(new GetContainerTemplatesQuery(containerId));
        return Ok(templates);
    }

    [HttpPost("{containerId}/templates")]
    public async Task<ActionResult<IEnumerable<ContainerTemplateResource>>> PostTemplates(
        [FromRoute] Guid containerId,
        [FromBody] List<ContainerTemplateResource> resources)
    {
        var templates = await Mediator.Send(new PostContainerTemplatesCommand(containerId, resources));
        return Ok(templates);
    }

    [HttpPut("{containerId}/templates")]
    public async Task<ActionResult<IEnumerable<ContainerTemplateResource>>> PutTemplates(
        [FromRoute] Guid containerId,
        [FromBody] List<ContainerTemplateResource> resources)
    {
        var templates = await Mediator.Send(new PutContainerTemplatesCommand(containerId, resources));
        return Ok(templates);
    }

    [HttpDelete("{containerId}/templates")]
    public async Task<ActionResult<IEnumerable<ContainerTemplateResource>>> DeleteTemplates([FromRoute] Guid containerId)
    {
        var templates = await Mediator.Send(new DeleteContainerTemplatesCommand(containerId));
        return Ok(templates);
    }
}
