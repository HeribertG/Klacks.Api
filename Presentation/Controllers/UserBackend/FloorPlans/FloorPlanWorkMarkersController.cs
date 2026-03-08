// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries.FloorPlans;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.FloorPlans;

public class FloorPlanWorkMarkersController : BaseController
{
    private readonly IMediator _mediator;

    public FloorPlanWorkMarkersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ByFloorPlan/{floorPlanId}")]
    public async Task<ActionResult<IEnumerable<FloorPlanWorkMarkerResource>>> GetByFloorPlan([FromRoute] Guid floorPlanId)
    {
        var result = await _mediator.Send(new GetMarkersByFloorPlanQuery(floorPlanId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<FloorPlanWorkMarkerResource>> Post([FromBody] FloorPlanWorkMarkerResource resource)
    {
        var model = await _mediator.Send(new PostCommand<FloorPlanWorkMarkerResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<FloorPlanWorkMarkerResource>> Put([FromBody] FloorPlanWorkMarkerResource resource)
    {
        var model = await _mediator.Send(new PutCommand<FloorPlanWorkMarkerResource>(resource));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<FloorPlanWorkMarkerResource>> Delete([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<FloorPlanWorkMarkerResource>(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }
}
