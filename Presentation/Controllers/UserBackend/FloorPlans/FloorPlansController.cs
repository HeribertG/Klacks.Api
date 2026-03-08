// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.FloorPlans;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.FloorPlans;

public class FloorPlansController : BaseController
{
    private readonly IMediator _mediator;

    public FloorPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FloorPlanResource>>> List()
    {
        var result = await _mediator.Send(new ListQuery<FloorPlanResource>());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FloorPlanResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetFloorPlanWithMarkersQuery(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<FloorPlanResource>> Post([FromBody] FloorPlanResource resource)
    {
        var model = await _mediator.Send(new PostCommand<FloorPlanResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<FloorPlanResource>> Put([FromBody] FloorPlanResource resource)
    {
        var model = await _mediator.Send(new PutCommand<FloorPlanResource>(resource));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<FloorPlanResource>> Delete([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<FloorPlanResource>(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }
}
