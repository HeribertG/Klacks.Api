using Klacks.Api.Models.Schedules;
using Klacks.Api.Queries.Works;
using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend;

public class WorksController : InputBaseController<Work>
{
    private readonly ILogger<WorksController> logger;

    public WorksController(IMediator mediator, ILogger<WorksController> logger)
            : base(mediator, logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// List of all Clients and all their Works in the current Year and Month.
    /// </summary>
    /// <param name="filter">Current Filter.</param>
    [HttpPost("GetClientList")]
    public async Task<ActionResult<IEnumerable<ClientWorkResource>>> GetClientList([FromBody] WorkFilter filter)
    {
        return Ok(await Mediator.Send(new ListQuery(filter)));
    }
}
