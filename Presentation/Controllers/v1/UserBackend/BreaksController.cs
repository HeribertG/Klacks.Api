using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
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
}
