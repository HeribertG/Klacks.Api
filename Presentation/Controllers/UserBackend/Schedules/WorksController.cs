using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.PeriodHours;
using Klacks.Api.Application.Queries.ScheduleEntries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class WorksController : BaseController
{
    private readonly IMediator _mediator;

    public WorksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<WorkResource>(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<WorkResource>> Post([FromBody] WorkResource resource)
    {
        var model = await _mediator.Send(new PostCommand<WorkResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    public async Task<ActionResult<WorkResource>> Put([FromBody] WorkResource resource)
    {
        var model = await _mediator.Send(new PutCommand<WorkResource>(resource));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<WorkResource>> Delete(
        [FromRoute] Guid id,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd)
    {
        var model = await _mediator.Send(new DeleteWorkCommand(id, periodStart, periodEnd));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost("Bulk")]
    public async Task<ActionResult<BulkWorksResponse>> BulkAdd([FromBody] BulkAddWorksRequest request)
    {
        var response = await _mediator.Send(new BulkAddWorksCommand(request));
        return Ok(response);
    }

    [HttpDelete("Bulk")]
    public async Task<ActionResult<BulkWorksResponse>> BulkDelete([FromBody] BulkDeleteWorksRequest request)
    {
        var response = await _mediator.Send(new BulkDeleteWorksCommand(request));
        return Ok(response);
    }

    [HttpPost("Schedule")]
    public async Task<ActionResult<WorkScheduleResponse>> GetWorkSchedule([FromBody] WorkScheduleFilter filter)
    {
        var result = await _mediator.Send(new GetScheduleEntriesQuery(filter));
        return Ok(result);
    }

    [HttpPost("PeriodHours")]
    public async Task<ActionResult<Dictionary<Guid, PeriodHoursResource>>> GetPeriodHours([FromBody] PeriodHoursRequest request)
    {
        var result = await _mediator.Send(new GetPeriodHoursQuery(request));
        return Ok(result);
    }

    [HttpGet("Changes")]
    public async Task<ActionResult<IEnumerable<WorkChangeResource>>> GetChangesList()
    {
        var result = await _mediator.Send(new ListQuery<WorkChangeResource>());
        return Ok(result);
    }

    [HttpGet("Changes/{id}")]
    public async Task<ActionResult<WorkChangeResource>> GetChange([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<WorkChangeResource>(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost("Changes")]
    public async Task<ActionResult<WorkChangeResource>> PostChange([FromBody] WorkChangeResource resource)
    {
        var model = await _mediator.Send(new PostCommand<WorkChangeResource>(resource));
        return Ok(model);
    }

    [HttpPut("Changes")]
    public async Task<ActionResult<WorkChangeResource>> PutChange([FromBody] WorkChangeResource resource)
    {
        var model = await _mediator.Send(new PutCommand<WorkChangeResource>(resource));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpDelete("Changes/{id}")]
    public async Task<ActionResult<WorkChangeResource>> DeleteChange(Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<WorkChangeResource>(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost("{id}/Confirm")]
    public async Task<ActionResult<WorkResource>> Confirm([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new ConfirmWorkCommand(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost("{id}/Unconfirm")]
    public async Task<ActionResult<WorkResource>> Unconfirm([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new UnconfirmWorkCommand(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost("ApproveDay")]
    public async Task<ActionResult<int>> ApproveDay([FromBody] ApproveDayCommand command)
    {
        var count = await _mediator.Send(command);
        return Ok(count);
    }

    [HttpPost("RevokeDayApproval")]
    public async Task<ActionResult<int>> RevokeDayApproval([FromBody] RevokeDayApprovalCommand command)
    {
        var count = await _mediator.Send(command);
        return Ok(count);
    }

    [HttpPost("ClosePeriod")]
    public async Task<ActionResult<int>> ClosePeriod([FromBody] ClosePeriodCommand command)
    {
        var count = await _mediator.Send(command);
        return Ok(count);
    }

    [HttpPost("ReopenPeriod")]
    public async Task<ActionResult<int>> ReopenPeriod([FromBody] ReopenPeriodCommand command)
    {
        var count = await _mediator.Send(command);
        return Ok(count);
    }
}
