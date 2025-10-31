using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ContainersController : InputBaseController<ContainerTemplateResource>
{
    public ContainersController(IMediator Mediator, ILogger<ContainersController> logger)
          : base(Mediator, logger)
    {
    }

    [HttpGet("available-tasks")]
    public async Task<ActionResult<IEnumerable<ShiftResource>>> GetAvailableTasks(
        [FromQuery] Guid containerId,
        [FromQuery] int[] weekdays,
        [FromQuery] TimeOnly fromTime,
        [FromQuery] TimeOnly untilTime,
        [FromQuery] string? searchString = null,
        [FromQuery] Guid? excludeContainerId = null,
        [FromQuery] bool? isHoliday = null,
        [FromQuery] bool? isWeekdayOrHoliday = null)
    {
        var tasks = await Mediator.Send(new GetAvailableTasksQuery(containerId, weekdays, fromTime, untilTime, searchString, excludeContainerId, isHoliday, isWeekdayOrHoliday));
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
