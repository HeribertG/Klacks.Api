using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class BreaksController : InputBaseController<BreakResource>
{
    public BreaksController(IMediator Mediator, ILogger<BreaksController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpPost("GetClientList")]
    public async Task<ActionResult<IEnumerable<ClientBreakResource>>> GetClientList([FromBody] BreakFilter filter)
    {
        var (clientList, totalCount) = await Mediator.Send(new ListQuery(filter));

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

        return Ok(clientList);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<BreakResource>> Post([FromBody] BreakResource resource)
    {
        var model = await Mediator.Send(new PostCommand<BreakResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<BreakResource>> Put([FromBody] BreakResource resource)
    {
        var model = await Mediator.Send(new PutCommand<BreakResource>(resource));

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<BreakResource>> Delete(Guid id)
    {
        var model = await Mediator.Send(new DeleteCommand<BreakResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
