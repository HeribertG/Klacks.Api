// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.BreakPlaceholders;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class BreakPlaceholdersController : InputBaseController<BreakPlaceholderResource>
{
    public BreakPlaceholdersController(IMediator Mediator, ILogger<BreakPlaceholdersController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpPost("GetClientList")]
    public async Task<ActionResult<IEnumerable<ClientBreakPlaceholderResource>>> GetClientList([FromBody] BreakFilter filter)
    {
        var (clientList, totalCount) = await Mediator.Send(new ListQuery(filter));

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

        return Ok(clientList);
    }

    [HttpPost("GetScheduleList")]
    public async Task<ActionResult<IEnumerable<ClientBreakPlaceholderResource>>> GetScheduleList([FromBody] BreakFilter filter)
    {
        var clientList = await Mediator.Send(new GetScheduleListQuery(filter));
        return Ok(clientList);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<BreakPlaceholderResource>> Post([FromBody] BreakPlaceholderResource resource)
    {
        var model = await Mediator.Send(new PostCommand<BreakPlaceholderResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<BreakPlaceholderResource>> Put([FromBody] BreakPlaceholderResource resource)
    {
        var model = await Mediator.Send(new PutCommand<BreakPlaceholderResource>(resource));

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<BreakPlaceholderResource>> Delete(Guid id)
    {
        var model = await Mediator.Send(new DeleteCommand<BreakPlaceholderResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
