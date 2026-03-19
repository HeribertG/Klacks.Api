// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller fuer WorkChange-Verwaltung (CRUD-Operationen auf Dienstaenderungen).
/// </summary>
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[Route("api/backend/Works")]
public class WorkChangesController : BaseController
{
    private readonly IMediator _mediator;

    public WorkChangesController(IMediator mediator)
    {
        _mediator = mediator;
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
}
