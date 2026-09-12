// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// CRUD for the employee absences drawn in the absence Gantt (entity BreakPlaceholder). A caller
/// without any role (Planer) must be able to write these, so the controller deliberately does not
/// derive from InputBaseController: attributes on an override are AND-combined with the base
/// method's, so the Admin/Authorised restriction of InputBaseController.Post/Put/Delete survived the
/// overrides that were meant to lift it and kept locking roleless planners out.
/// </summary>
/// <param name="mediator">Dispatches the break-placeholder queries and commands</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.BreakPlaceholders;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[ApiController]
public class BreakPlaceholdersController : BaseController, ICrudResourceController<BreakPlaceholderResource>
{
    private readonly IMediator _mediator;

    public BreakPlaceholdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("GetClientList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<ClientBreakPlaceholderResource>>> GetClientList([FromBody] BreakFilter filter)
    {
        var (clientList, totalCount) = await _mediator.Send(new ListQuery(filter));

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

        return Ok(clientList);
    }

    [HttpPost("GetScheduleList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<ClientBreakPlaceholderResource>>> GetScheduleList([FromBody] BreakFilter filter, CancellationToken cancellationToken)
    {
        var clientList = await _mediator.Send(new GetScheduleListQuery(filter), cancellationToken);
        return Ok(clientList);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BreakPlaceholderResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<BreakPlaceholderResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BreakPlaceholderResource>> Post([FromBody] BreakPlaceholderResource resource)
    {
        var model = await _mediator.Send(new PostCommand<BreakPlaceholderResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BreakPlaceholderResource>> Put([FromBody] BreakPlaceholderResource resource)
    {
        var model = await _mediator.Send(new PutCommand<BreakPlaceholderResource>(resource));

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BreakPlaceholderResource>> Delete(Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<BreakPlaceholderResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
