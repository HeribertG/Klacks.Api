// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Schedule notes. Recording, correcting and deleting them is open to every authenticated user (the planner
/// floor), like the schedule they annotate (owner decision 2026-10-12); group visibility is enforced by the
/// handlers. The controller is on BaseController rather than a generic CRUD base because an [Authorize] on an
/// override can only narrow the base gate, never widen it.
/// </summary>
/// <param name="mediator">Dispatches the generic CRUD commands and queries for ScheduleNoteResource</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[ApiController]
public class ScheduleNotesController : BaseController, ICrudResourceController<ScheduleNoteResource>
{
    private readonly IMediator _mediator;

    public ScheduleNotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleNoteResource>>> GetList()
    {
        var result = await _mediator.Send(new ListQuery<ScheduleNoteResource>());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleNoteResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<ScheduleNoteResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<ScheduleNoteResource>> Post([FromBody] ScheduleNoteResource resource)
    {
        var model = await _mediator.Send(new PostCommand<ScheduleNoteResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    public async Task<ActionResult<ScheduleNoteResource>> Put([FromBody] ScheduleNoteResource resource)
    {
        var model = await _mediator.Send(new PutCommand<ScheduleNoteResource>(resource));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ScheduleNoteResource>> Delete(Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<ScheduleNoteResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
