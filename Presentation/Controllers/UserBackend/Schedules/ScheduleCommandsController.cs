// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Controller for CRUD operations on schedule commands (FREE, EARLY, LATE, NIGHT keywords). Recording,
/// correcting and deleting them is open to every authenticated user (the planner floor), like the schedule they
/// steer (owner decision 2026-10-12); group visibility is enforced by the handlers. The controller is on
/// BaseController rather than a generic CRUD base because an [Authorize] on an override can only narrow the base
/// gate, never widen it.
/// </summary>
/// <param name="mediator">Dispatches the generic CRUD commands and queries for ScheduleCommandResource</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[ApiController]
public class ScheduleCommandsController : BaseController, ICrudResourceController<ScheduleCommandResource>
{
    private readonly IMediator _mediator;

    public ScheduleCommandsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleCommandResource>>> GetList()
    {
        var result = await _mediator.Send(new ListQuery<ScheduleCommandResource>());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleCommandResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<ScheduleCommandResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<ScheduleCommandResource>> Post([FromBody] ScheduleCommandResource resource)
    {
        var model = await _mediator.Send(new PostCommand<ScheduleCommandResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    public async Task<ActionResult<ScheduleCommandResource>> Put([FromBody] ScheduleCommandResource resource)
    {
        var model = await _mediator.Send(new PutCommand<ScheduleCommandResource>(resource));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ScheduleCommandResource>> Delete(Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<ScheduleCommandResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
