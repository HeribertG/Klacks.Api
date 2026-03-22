// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for work management (CRUD, bulk operations, scheduling, period closing).
/// </summary>
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.PeriodHours;
using Klacks.Api.Application.Queries.ScheduleEntries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
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
    public async Task<ActionResult<WorkScheduleResponse>> GetWorkSchedule([FromBody] WorkScheduleFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetScheduleEntriesQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpPost("PeriodHours")]
    public async Task<ActionResult<Dictionary<Guid, PeriodHoursResource>>> GetPeriodHours([FromBody] PeriodHoursRequest request)
    {
        var result = await _mediator.Send(new GetPeriodHoursQuery(request));
        return Ok(result);
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

    [HttpPost("RecalculatePeriodHours")]
    public async Task<ActionResult<bool>> RecalculatePeriodHours([FromBody] RecalculatePeriodHoursCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
